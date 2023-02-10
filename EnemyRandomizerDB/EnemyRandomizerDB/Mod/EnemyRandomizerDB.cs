#if !LIBRARY //don't build this file when building out the library

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Modding;
using UnityEngine.SceneManagement;
using UnityEngine;
using Language; 
using On;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;
using System.Xml.Serialization;
using EnemyRandomizerMod;

using Dev = EnemyRandomizerMod.Dev;
using DevLogger = EnemyRandomizerMod.DevLogger;

namespace EnemyRandomizerMod
{
    public class EnemyRandomizerDBSettings
    {
    }

    public class EnemyRandomizerDBPlayerSettings
    {
    }

    public partial class EnemyRandomizerDB : Mod, IGlobalSettings<EnemyRandomizerDBSettings>, ILocalSettings<EnemyRandomizerDBPlayerSettings>
    {
        //Settings objects provided by the mod base class
        public static EnemyRandomizerDBSettings GlobalSettings = new EnemyRandomizerDBSettings();
        public void OnLoadGlobal(EnemyRandomizerDBSettings s) => GlobalSettings = s;
        public EnemyRandomizerDBSettings OnSaveGlobal() => GlobalSettings;

        public static EnemyRandomizerDBPlayerSettings PlayerSettings = new EnemyRandomizerDBPlayerSettings();
        public void OnLoadLocal(EnemyRandomizerDBPlayerSettings s) => PlayerSettings = s;
        public EnemyRandomizerDBPlayerSettings OnSaveLocal() => PlayerSettings;

        static string currentVersion = Assembly.GetAssembly(typeof(EnemyRandomizerDB)).GetName().Version.ToString();

        public override string GetVersion()
        {
            return currentVersion;
        }

        const string databaseFileName = "EnemyRandomizerDatabase.xml";
        const string worldMapFilePrefix = "SceneData_";

        List<SceneData> mapFiles = new List<SceneData>();

        string CurrentFileName
        {
            get
            {
                return databaseFileName;
            }
        }

        public void SaveDatabase()
        {
            if (EnemyRandomizerDatabase.GetDatabaseFilePath(CurrentFileName).SerializeXMLToFile(database))
                Dev.Log("Saved database to : " + EnemyRandomizerDatabase.GetDatabaseFilePath(CurrentFileName));
        }

        public string GetMapPath(string fileName)
        {
            string path = EnemyRandomizerDatabase.GetDatabaseFilePath(fileName);
            return path;
        }

        public void SaveSceneData(SceneData data)
        {
            string path = GetMapPath(worldMapFilePrefix + data.name + ".xml");
            path.SerializeXMLToFile(data);
        }

        public SceneData LoadSceneData(string sceneName, bool generateIfMissing = false)
        {
            string path = GetMapPath(worldMapFilePrefix + sceneName + ".xml");
            if (path.DeserializeXMLFromFile<SceneData>(out SceneData data))
                return data;
            if(generateIfMissing)
            {
                SceneData newData = new SceneData();
                newData.sceneObjects = new List<SceneObject>();
                newData.name = sceneName;
                return newData;
            }
            return null;
        }

        public void LoadAllScenes()
        {
            GetAllScenes().ToList().ForEach(x =>
            {
                bool result = x.DeserializeXMLFromFile<SceneData>(out SceneData data);
                if(result)
                    mapFiles.Add(data);
            });
        }

        public IEnumerable<string> GetAllScenes()
        {
            int totalScenes = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < totalScenes; ++i)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = Path.GetFileNameWithoutExtension(scenePath);
                yield return name;
            }
        }

        public static EnemyRandomizerDB Instance
        {
            get 
            {
                if (instance == null)
                    instance = new EnemyRandomizerDB();
                return instance;
            }
        }
        static EnemyRandomizerDB instance;

        public EnemyRandomizerDatabase database;

        public List<string> scenesScraped = new List<string>();

        //only used to parse every scene in the game and dump themp into scene-named xml files
        bool buildWorldMap = false;

        //set this to true if you want to load and work on the database, false to start building a new database
        bool checkSkipPreload = true;

        //leave default -- never change this
        bool skipPreload;

        //probably should never use, this was used once only when I was first making the database and ended up with a bunch of junk
        bool rebuildDatabase = false;

        //if generating a new database, set this to TRUE so that it can build a list of unique names for you
        bool compileUniqueNames = false;

        //should the database file save itself regardless of anything else after loading is complete?
        bool saveOnLoad = false;

        //usually you will set checkSkipPreload=true, compileUniqueNames=false, and runTestAll=true  when doing debugging and development on enemy prefabs and spawners
        //this will start the test runner but not do anything until the game is entered and P is pressed to start the tests (which much of the time you might not need to do)
        bool runTestAll = true;

        //DON"T USE THIS -- currently will purge a bunch of stuff from the database using test results -- could be used to batch process different sets of failed tests later on though...
        bool updateDatabaseWithFalseTestData = false;

        //demove duplicate data from scenes to cut down on the amount of scenes and prefabs that need to be loaded
        bool trimDuplicates = false;

        //part of runTestAll -- will ignore pass/fail status and just get to the most recent untested thing from the objects list if this is true. useful when trying to test a lot of things and some have errors that will make you restart the tests/rebuild
        bool devSkipUntilAllTested = false;

        public virtual void PreInitialize()
        {
            //enable debugging
            Dev.Logger.LoggingEnabled = true;
            Dev.Logger.GuiLoggingEnabled = true;
            DevLogger.Instance.ShowSlider();
            DevLogger.Instance.Show(true);

            if (checkSkipPreload)
            {
                database = EnemyRandomizerDatabase.Create(CurrentFileName);
                if (database != null && database.scenes.Count > 0)
                {
                    skipPreload = true;
                }
            }
            else
            {
                database = EnemyRandomizerDatabase.Create(null);
            }
        }

        public override List<(string, string)> GetPreloadNames()
        {
            PreInitialize();

            if (skipPreload && !rebuildDatabase)
            {
                if (database.scenes.Count <= 0)
                    return null;

                if(trimDuplicates)
                {
                    var worst = database.scenes.Where(x => x.name.Contains("GG_")).ToList();
                    var bad = database.scenes.Where(x => x.name.Contains("Colosseum")).ToList();

                    var worstobjs = worst.SelectMany(x => x.sceneObjects).ToList();
                    var badobjs = bad.SelectMany(x => x.sceneObjects).ToList();

                    var ok = database.scenes.Where(x => !worst.Contains(x) && !bad.Contains(x)).ToList();

                    var battlePaths = ok.SelectMany(x => x.sceneObjects).Where(x => x.path.Contains("Battle ")).ToList();
                    var normalPaths = ok.SelectMany(x => x.sceneObjects).Where(x => !x.path.Contains("Battle ")).ToList();

                    HashSet<string> uniqueNames = new HashSet<string>();

                    normalPaths.ForEach(x =>
                    {
                        if (uniqueNames.Contains(EnemyRandomizerDatabase.ToDatabaseKey(x.Name)))
                        {
                            Dev.Log($"Trimming duplicate in {x.Scene.name} with path {x.path}");
                            x.Scene.sceneObjects.Remove(x);
                        }
                        else
                        {
                            uniqueNames.Add(EnemyRandomizerDatabase.ToDatabaseKey(x.Name));
                        }
                    });

                    battlePaths.ForEach(x =>
                    {
                        if (uniqueNames.Contains(EnemyRandomizerDatabase.ToDatabaseKey(x.Name)))
                        {
                            Dev.Log($"Trimming duplicate in {x.Scene.name} with path {x.path}");
                            x.Scene.sceneObjects.Remove(x);
                        }
                        else
                        {
                            uniqueNames.Add(EnemyRandomizerDatabase.ToDatabaseKey(x.Name));
                        }
                    });

                    badobjs.ForEach(x =>
                    {
                        if (uniqueNames.Contains(EnemyRandomizerDatabase.ToDatabaseKey(x.Name)))
                        {
                            Dev.Log($"Trimming duplicate in {x.Scene.name} with path {x.path}");
                            x.Scene.sceneObjects.Remove(x);
                        }
                        else
                        {
                            uniqueNames.Add(EnemyRandomizerDatabase.ToDatabaseKey(x.Name));
                        }
                    });

                    worstobjs.ForEach(x =>
                    {
                        if (uniqueNames.Contains(EnemyRandomizerDatabase.ToDatabaseKey(x.Name)))
                        {
                            Dev.Log($"Trimming duplicate in {x.Scene.name} with path {x.path}");
                            x.Scene.sceneObjects.Remove(x);
                        }
                        else
                        {
                            uniqueNames.Add(EnemyRandomizerDatabase.ToDatabaseKey(x.Name));
                        }
                    });

                    //update trims
                    GenerateDatabase();

                    //reload
                    database = EnemyRandomizerDatabase.Create(CurrentFileName);
                }

                return database.GetPreloadNames();
            }


            int totalScenes = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

            int firstSceneToLoad = 0;

            IEnumerable<string> GetAllScenes(int fromIndex, int toIndex)
            {
                for (int i = fromIndex; i < toIndex; ++i)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = Path.GetFileNameWithoutExtension(scenePath);
                    yield return name;
                }
            };
            string[] scenesToCompile = GetAllScenes(firstSceneToLoad, totalScenes).ToArray();

            (string, string)[] sceneDataPairs = new (string, string)[scenesToCompile.Length];
            for (int k = 0; k < sceneDataPairs.Length; ++k)
            {
                sceneDataPairs[k] = (scenesToCompile[k], "_Enemies");
            }
            return sceneDataPairs.ToList();
        }

        ////NOTE: Executes AFTER get preload names
        ///NOTE2: A scene must be present in the GetPreloadNames() AND here in PreloadSceneHooks() in order for the Func<IEnumerator> to run....
        public override (string, Func<IEnumerator>)[] PreloadSceneHooks()
        {
            if (skipPreload && !rebuildDatabase)
            {
                if (database.scenes.Count <= 0)
                    return new (string, Func<IEnumerator>)[0];

                return base.PreloadSceneHooks();
                //return database.PreloadSceneHooks();
            }

            int totalScenes = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;

            int firstSceneToLoad = 0;

            IEnumerable<string> GetAllScenes(int fromIndex, int toIndex)
            {
                for (int i = fromIndex; i < toIndex; ++i)
                {
                    var scenes = new List<string>();
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = Path.GetFileNameWithoutExtension(scenePath);
                    yield return name;
                }
            };
            string[] scenesToCompile = GetAllScenes(firstSceneToLoad, totalScenes).ToArray();

            //Dev.Log("Scenes");
            //for (int i = 0; i < scenesToCompile.Length; ++i)
            //{
            //    Dev.Log($"{scenesToCompile[i]} - {SceneUtility.GetScenePathByBuildIndex(i + firstSceneToLoad)}");
            //}

            Func<IEnumerator> GetSceneCompilationMethod(string s)
            {
                IEnumerator CaptureSceneAndCompile()
                {
                    yield return CompileScene(s);
                }

                return CaptureSceneAndCompile;
            }

            IEnumerable<Func<IEnumerator>> GetAllMethods(string[] scenes) { for (int i = 0; i < scenes.Length; ++i) yield return GetSceneCompilationMethod(scenes[i]); };
            Func<IEnumerator>[] compilationMethods = GetAllMethods(scenesToCompile).ToArray();

            (string, Func<IEnumerator>)[] sceneMethodPairs = new (string, Func<IEnumerator>)[scenesToCompile.Length];
            for (int k = 0; k < sceneMethodPairs.Length; ++k)
            {
                sceneMethodPairs[k] = (scenesToCompile[k], compilationMethods[k]);
            }

            //Dev.Log("Preload Hooks");
            //for (int i = 0; i < sceneMethodPairs.Length; ++i)
            //{
            //    Dev.Log($"{sceneMethodPairs[i].Item1} - {sceneMethodPairs[i].Item2}");
            //}

            return sceneMethodPairs;
        }

        IEnumerator CompileScene(string sceneData)
        {
            //Dev.Log("Compiling " + sceneData);
            
            //Get the loaded scene since the input scene is pulled from the build data list
            Scene loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneData);

            var rootGameObjects = loadedScene.GetRootGameObjects();

            if(buildWorldMap)
            {
                var sceneMap = LoadSceneData(loadedScene.name, true);

                //process literally everything
                sceneMap.sceneObjects = rootGameObjects.
                    SelectMany(x => x.GetComponentsInChildren<Transform>(true)).
                    Where(x => x != null).
                    Select(x =>
                    {
                        var components = x.gameObject.GetComponents(typeof(Component));
                        return new SceneObject()
                        {
                            components = components.Select(c => c.GetType().Name).ToList(),
                            path = x.gameObject.GetSceneHierarchyPath()
                        };
                    }).ToList();

                SaveSceneData(sceneMap);
                yield return null;
            }


            if(rebuildDatabase)
            {
                CompileGameObjectsFromSceneObjects(rootGameObjects);
            }
            else
            {
                CompileGameObjectsFromResources();
            }

            //CompileGameObjects(rootGameObjects.Where(x => x != null));

            yield return null;
        }

        //void CompileGameObjects(IEnumerable<GameObject> rootObjects)
        //{
        //    //process literally everything
        //    rootObjects.SelectMany(x => x.GetComponentsInChildren<Transform>()
        //    //.Where(s =>
        //    //{
        //    //    return true;
        //    //    //return s.gameObject.IsGameEnemy();
        //    //})
        //    .Select(y => y.gameObject)).ToList().ForEach(x =>CreateDatabaseEntry(x));
        //}

        void CompileGameObjectsFromSceneObjects(GameObject[] rootGameObjects)
        {
            var possiblePrefabs = rootGameObjects;
            IEnumerable<GameObject> result = new List<GameObject>();

            //process literally everything
            foreach (var t in EnemyRandomizerDatabase.dataBaseObjectComponents)
            {
                result = result.Concat(
                    possiblePrefabs.SelectMany(x => x.GetComponentsInChildren(t))
                    .Where(s =>
                    {
                        return EnemyRandomizerDatabase.IsDatabaseObject(s.gameObject);
                    })
                    .Select(y => y.gameObject)).Distinct();
            }

            result.Distinct().ToList().ForEach(x => CreateDatabaseEntry(x));
        }

        void CompileGameObjectsFromResources()
        {
            var possiblePrefabs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            IEnumerable<GameObject> result = new List<GameObject>();

            //process literally everything
            foreach (var t in EnemyRandomizerDatabase.dataBaseObjectComponents)
            {
                result = result.Concat(
                    possiblePrefabs.SelectMany(x => x.GetComponentsInChildren(t))
                    .Where(s =>
                    {
                        return EnemyRandomizerDatabase.IsDatabaseObject(s.gameObject);
                    })
                    .Select(y => y.gameObject)).Distinct();
            }

            result.Distinct().ToList().ForEach(x => CreateDatabaseEntry(x));

            //possiblePrefabs.ToList().ForEach(x => CreateDatabaseEntry(x));
            //GenerateDatabase();
        }

        //void CompileObjectsWithComponent<T>(IEnumerable<GameObject> gameObjects)
        //    where T : MonoBehaviour
        //{
        //}

        //void CompileObjectsWithComponents(IEnumerable<GameObject> gameObjects, List<Type> componentTypes)
        //{

        //}

        HashSet<string> trackedPrefab = new HashSet<string>();
        List<SceneObject> queuedAdd = new List<SceneObject>();

        void CreateDatabaseEntry(GameObject gameObject)
        {
            string sceneName = null;
            if(gameObject.scene.IsValid())
                sceneName = gameObject.scene.name;

            bool isDatabaseObject = EnemyRandomizerDatabase.IsDatabaseObject(gameObject);

            if (!isDatabaseObject)
                return;

            string name = EnemyRandomizerDatabase.ToDatabaseKey(gameObject.name);

            //only care about rebuilding enemies
            if (rebuildDatabase)
            {
                if (!database.enemyNames.Contains(name))
                    return;
            }
            else
            {
                if (trackedPrefab.Contains(name))
                    return;
            }

            string path = gameObject.GetSceneHierarchyPath();

            //validate that the new entry will actually work
            if (rebuildDatabase)
            {
                GameObject go = null;
                {
                    go = ModUtils.FindGameObject(path);

                    if (go == null)
                    {
                        Dev.LogError($"[REBUILD] Validation failed: SCENE:{sceneName} PATH:{path}" );
                        gameObject.PrintSceneHierarchyTree(true, file:null);
                        return;
                    }
                }
            }

            SceneData scene = null;
            if (string.IsNullOrEmpty(sceneName))
            {
                if(rebuildDatabase)
                {
                    Dev.LogError("INVALID SCENES NOT ALLOWED DURING REBUILD");
                    return;
                }
                else
                {
                    if (scene == null)
                    {
                        sceneName = "RESOURCES";
                    }
                }
            }

            var foundBadScene = database.badSceneData.FirstOrDefault(x => x.name == sceneName);
            if (foundBadScene != null)
            {
                var foundData = foundBadScene.sceneObjects.FirstOrDefault(x => x.path == path);
                if(foundData != null)
                {
                    Dev.LogError("Cannot build scene object that exists in known bad scene data list");
                    return;
                }
            }

            scene = GetOrAddToScenes(sceneName);

            if (!path.Contains("Battle ") && !path.Contains("Colosseum "))
            {
                var foundGoodData = scene.sceneObjects.FirstOrDefault(x => x.path == path);
                if (foundGoodData != null)
                {
                    trackedPrefab.Add(name);
                    Dev.LogWarning($"{foundGoodData.Name} already exists in this scene {scene.name} -- skipping object creation");
                    return;
                }
            }

            List<string> components = new List<string>();
            components = EnemyRandomizerDatabase.dataBaseObjectComponents.Select(x => gameObject.GetComponent(x)).Where(x => x != null).Select(x => x.GetType().Name).ToList();

            SceneObject newObject = new SceneObject()
            {
                path = path,
                Scene = scene,
                components = components
            };

            if (path.Contains("Battle ") || path.Contains("Colosseum "))
            {
                queuedAdd.Add(newObject);
                return;
            }

            AddSceneObjectToDatabase(scene, newObject);
        }

        void AddSceneObjectToDatabase(SceneData scene, SceneObject newObject)
        {
            string name = EnemyRandomizerDatabase.ToDatabaseKey(newObject.Name);
            if (trackedPrefab.Contains(name))
            {
                var foundGoodData = scene.sceneObjects.FirstOrDefault(x => x.path == newObject.path);
                if (foundGoodData != null)
                {
                    Dev.LogWarning($"{foundGoodData.Name} already exists in another scene. removing the old entry...");
                    scene.sceneObjects = scene.sceneObjects.Where(x => x.path != newObject.path).ToList();
                }
            }
            else
            {
                trackedPrefab.Add(name);
            }

            newObject.Scene = scene;
            scene.sceneObjects.Add(newObject);
        }

        SceneData GetOrAddToScenes(string sceneName)
        {
            SceneData found = database.scenes.FirstOrDefault(x => x.name == sceneName);
            if (found == null)
            {
                found = new SceneData();
                found.name = sceneName;
                found.sceneObjects = new List<SceneObject>();

                database.scenes.Add(found);
            }
            return found;
        }

        IEnumerator CompileUniqueNames()
        {
            if (rebuildDatabase)
                yield break;
            Dev.Log("Compiling unique names...");

            var allEnemyNames = database.scenes.SelectMany(x => x.sceneObjects).Where(x => !x.components.Contains("ParticleSystem") && x.components.Contains("HealthManager")).Select(x => x.Name).Distinct().ToList();
            var allEffectNames = database.scenes.SelectMany(x => x.sceneObjects).Where(x => x.components.Contains("ParticleSystem")).Select(x => x.Name).Distinct().ToList();
            var allHazardNames = database.scenes.SelectMany(x => x.sceneObjects).Where(x => !x.components.Contains("ParticleSystem") && x.components.Contains("DamageHero")).Select(x => x.Name).Distinct().ToList();

            {
                int count = allEnemyNames.Count;

                HashSet<string> uniqueNames = new HashSet<string>();
                for (int i = 0; i < count; ++i)
                {
                    string objectName = allEnemyNames[i];
                    string key = EnemyRandomizerDatabase.ToDatabaseKey(objectName);

                    //bool isLowerItem = key.IsLowercaseSceneItem();
                    bool isBadKeyItem = string.IsNullOrEmpty(key);

                    if (!isBadKeyItem && uniqueNames.Add(key))
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > ADDED {objectName} with key {key}");
                    }
                    else
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > SKIPPED {objectName} with key {key} -- Bad:{isBadKeyItem}");
                    }
                }

                yield return null;

                database.enemyNames = uniqueNames.ToList();
                Dev.Log("Enemy list generated");
            }

            {
                int count = allEffectNames.Count;

                HashSet<string> uniqueNames = new HashSet<string>();
                for (int i = 0; i < count; ++i)
                {
                    string objectName = allEffectNames[i];
                    string key = EnemyRandomizerDatabase.ToDatabaseKey(objectName);

                    //bool isLowerItem = key.IsLowercaseSceneItem();
                    bool isBadKeyItem = string.IsNullOrEmpty(key);

                    if (!isBadKeyItem && uniqueNames.Add(key))
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > ADDED {objectName} with key {key}");
                    }
                    else
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > SKIPPED {objectName} with key {key} -- Bad:{isBadKeyItem}");
                    }
                }

                yield return null;

                database.effectNames = uniqueNames.ToList();
                Dev.Log("Effect list generated");
            }

            {
                int count = allHazardNames.Count;

                HashSet<string> uniqueNames = new HashSet<string>();
                for (int i = 0; i < count; ++i)
                {
                    string objectName = allHazardNames[i];
                    string key = EnemyRandomizerDatabase.ToDatabaseKey(objectName);

                    //bool isLowerItem = key.IsLowercaseSceneItem();
                    bool isBadKeyItem = string.IsNullOrEmpty(key);

                    if (!isBadKeyItem && uniqueNames.Add(key))
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > ADDED {objectName} with key {key}");
                    }
                    else
                    {
                        Dev.Log($"Progress:{((float)i / (float)count)} > SKIPPED {objectName} with key {key} -- Bad:{isBadKeyItem}");
                    }
                }

                yield return null;

                database.hazardNames = uniqueNames.Except(database.enemyNames).ToList();
                Dev.Log("Hazard list generated");
            }

            //CompilePrefabs();
            //if (skipPreload && database.uniqueNames.Count > 0)
            //    GenerateDatabase();
            //else if (!skipPreload)
            //
            GenerateDatabase();
        }

        void CompileQueuedNames()
        {
            if (queuedAdd.Count > 0)
            {
                var battle = queuedAdd.Where(x => x.path.Contains("Battle "));
                var colo = queuedAdd.Where(x => x.path.Contains("Colosseum "));

                foreach (var s in battle)
                {
                    //add remaining objects that are less "easy" to load (things that only exist in battle scenes etc)
                    AddSceneObjectToDatabase(s.Scene, s);
                }

                foreach (var s in colo)
                {
                    //add remaining objects that are less "easy" to load (things that only exist in battle scenes etc)
                    AddSceneObjectToDatabase(s.Scene, s);
                }
            }
        }

        void BuildDatabase()
        {
            if (saveOnLoad || rebuildDatabase)
                GenerateDatabase();

            if (compileUniqueNames)
            {
                GameManager.instance.StartCoroutine(CompileUniqueNames());
            }
        }

        void RunTests()
        {
            Dev.Log("Starting test mode");
            GameManager.instance.StartCoroutine(TestRunner(skipPassed: true));
        }

        SceneData GetOrAddToBadScenes(string sceneName)
        {
            SceneData found = database.badSceneData.FirstOrDefault(x => x.name == sceneName);
            if (found == null)
            {
                found = new SceneData();
                found.name = sceneName;
                found.sceneObjects = new List<SceneObject>();

                database.badSceneData.Add(found);
            }
            return found;
        }

        void ScanFalseTestsAndUpdateObjectsInDatabase()
        {
            Dev.Where();
            Test test;
            string file = EnemyRandomizerDatabase.GetDatabaseFilePath("Tests.xml");
            if (!file.DeserializeXMLFromFile<Test>(out test))
            {
                return;
            }

            Dev.Log("updating from tests");

            foreach (var t in test.tests)
            {
                if (!t.result)
                {
                    var prefabObject = database.Objects[t.name];
                    var sceneObject = prefabObject.source;
                    MoveSceneObjectToBadScenes(sceneObject);
                }
            }

            Dev.Log("updating from broken/unloadable");

            var unloadedObjects = database.scenes.SelectMany(s => s.sceneObjects).Where(x => x.Loaded == false).ToList();

            foreach (var sceneObject in unloadedObjects)
            {
                MoveSceneObjectToBadScenes(sceneObject);
            }

            GenerateDatabase();
        }

        bool MoveSceneObjectToBadScenes(SceneObject sceneObject)
        {
            string key = EnemyRandomizerDatabase.ToDatabaseKey(sceneObject.Name);

            //is already a bad object
            if (!database.scenes.Contains(sceneObject.Scene))
                return false;

            var goodScene = sceneObject.Scene;
            var badScene = GetOrAddToBadScenes(sceneObject.Scene.name);

            //remove from good scene, add to bad
            goodScene.sceneObjects.Remove(sceneObject);
            badScene.sceneObjects.Add(sceneObject);

            //don't care about recovering bad hazards or effects
            if (database.hazardNames.Contains(key))
            {
                database.hazardNames.Remove(key);
            }
            if (database.effectNames.Contains(key))
            {
                database.effectNames.Remove(key);
            }

            return true;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            if (instance == null)
                instance = this;

            base.Initialize(preloadedObjects);

            database.Initialize(preloadedObjects);

            //Dev.Log("Preload loaded for us:");
            //preloadedObjects.ToList().ForEach(s =>
            //{
            //    s.Value.ToList().ForEach(x =>
            //    {
            //        var go = x.Value != null ? x.Value.name : "null";
            //        Dev.Log($"SCENE:{s.Key} PATH:{x.Key} OBJECT:{go}");
            //    });
            //});
            //Dev.Log("END HOOK PRELOAD DEBUG");

            if (saveOnLoad || compileUniqueNames || rebuildDatabase)
                BuildDatabase();

            if (updateDatabaseWithFalseTestData)
            {
                database.Finalize(ScanFalseTestsAndUpdateObjectsInDatabase);
            }
            else
            {
                if (runTestAll)
                    database.Finalize(RunTests);
                else
                    database.Finalize(null);
            }
        }

        public void GenerateDatabase()
        {
            CompileQueuedNames();

            //cull empty scenes
            database.scenes = database.scenes.Where(x => x.sceneObjects.Count > 0).ToList();

            Dev.Log("Writing database...");
            SaveDatabase();
        }

        IEnumerator TestRunner(bool skipPassed)
        {
            Dev.Log("Waiting to run tests until not in the menu scene");
            yield return new WaitUntil(() => !GameManager.instance.IsMenuScene());
            Dev.Log("Starting TEST SPAWN ALL mode -- press P to start the tests");
            Test test;
            GameObject spawnedTestObject = null;
            string file = EnemyRandomizerDatabase.GetDatabaseFilePath("Tests.xml");
            if (!file.DeserializeXMLFromFile<Test>(out test))
            {
                test = new Test();
                test.tests = new List<TestCase>();
                file.SerializeXMLToFile<Test>(test);
            }

            Dev.Log($"Gathering enemy objects to test...");
            bool waitingOnInput = true;

            //Update/change the filter on this "objects" to select different things to test
            var objects = database.Objects.Where(x => x.Value.prefabType == PrefabObject.PrefabType.Enemy).Select(x => x.Value).ToList();
            Dev.Log($"{objects.Count} objects to test...");

            int i = 0;
            bool doSave = false;
            bool doReset = false;
            bool skipping = false;

            Dev.Log("Press P to start first test");

            for (; ;)
            {
                yield return new WaitForEndOfFrame();

                if (i > 0)
                {
                    //press Y to mark the test as passed
                    if (Input.GetKeyDown(KeyCode.Y))
                    {
                        Dev.Log("[TESTALL] PASSING "+ test.tests[i - 1].name);
                        test.tests[i - 1].result = true;
                        waitingOnInput = false;
                        doSave = true;
                    }
                    //press N to mark the test as failed
                    if (Input.GetKeyDown(KeyCode.N))
                    {
                        Dev.Log("[TESTALL] FAILING " + test.tests[i - 1].name);
                        test.tests[i - 1].result = false;
                        waitingOnInput = false;
                        doSave = true;
                    }

                    if (skipPassed)
                    {
                        if (i < objects.Count && i < test.tests.Count)
                        {
                            if (test.tests[i].result)
                            {
                                Dev.Log("[TESTALL] SKIPPING PREVIOUSLY PASSED " + test.tests[i - 1].name);
                                waitingOnInput = false;
                                skipping = true;
                            }

                            else if(devSkipUntilAllTested && test.tests.Count < objects.Count)
                            {
                                waitingOnInput = false;
                                skipping = true;
                            }
                        }
                    }
                }


                //press P to advance to the next test
                if (Input.GetKeyDown(KeyCode.P))
                {
                    if(i > 0)
                        Dev.Log("[TESTALL] SKIPPING " + test.tests[i - 1].name);
                    waitingOnInput = false;
                }

                //press R to restart
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Dev.Log("[TESTALL] RESET TO BEGINNING");
                    i = 0;
                    file.DeserializeXMLFromFile<Test>(out test);
                    waitingOnInput = false;
                    doReset = true;
                }

                if (waitingOnInput)
                    continue;

                if (spawnedTestObject != null)
                {
                    Dev.Log("Cleaning old object");
                    GameObject.Destroy(spawnedTestObject);
                    spawnedTestObject = null;
                }

                if (doSave)
                {
                    Dev.Log("[TESTALL] SAVING TESTS");
                    file.SerializeXMLToFile<Test>(test);
                    doSave = false;
                }

                if (i >= objects.Count)
                {
                    Dev.Log("[TESTALL] FINISHED");
                    i = 0;
                    yield break;
                }

                if(doReset)
                {
                    //Dev.Log("Press P to start first test");
                    doReset = false;
                    continue;
                }

                if (!skipping)
                {
                    Dev.Log("[TESTALL] SPAWNING " + objects[i].prefabName);

                    spawnedTestObject = DebugSpawnObjectAt(objects[i].prefabName, HeroController.instance.transform.position + Vector3.right * 10f);

                    Dev.Log("[TESTALL] SPAWNED " + objects[i].prefabName);

                    if (i >= test.tests.Count)
                    {
                        Dev.Log("Adding new blank test...");
                        test.tests.Add(new TestCase() { name = objects[i].prefabName });
                    }

                }

                if(!skipping || ((i+1) < test.tests.Count && !test.tests[i].result))
                    Dev.Log("[TESTALL] Press Y:Pass N:Fail P:Pass R:Reset&Reload -- NOW TESTING " + test.tests[i].name);

                skipping = false;
                ++i;

                Dev.Log($"[TESTALL] PROGRESS: {(float)i/(float)objects.Count}");

                waitingOnInput = true;
            }


            yield break;
        }

        //Tests serve entirely no purpose on their own. If you run the tests then it's up to you to then do something meaningful with the results
        [XmlRoot]
        public class Test
        {
            [XmlArray]
            public List<TestCase> tests;
        }

        [XmlRoot]
        public class TestCase
        {
            [XmlElement]
            public string name;

            [XmlElement]
            public bool result;
        }


        //The methods below here are for debugging, mostly for use with the REPL console in UnityExplorer (lets you run code in-game so you can use this to spawn and test individual enemies without needing to write/compile a bunch of code)
        //EnemyRandomizerMod.EnemyRandomizerDB.DebugSpawnObjectHere("Mawlek Turret");
        //EnemyRandomizerMod.EnemyRandomizerDB.DebugSpawnObjectHere("Mawlek Turret Ceiling");

        public static GameObject DebugSpawnObject(string name)
        {
            var thing = EnemyRandomizerDB.Instance.database.Spawn(name);
            thing.SetActive(true);
            return thing;
        }

        public static GameObject DebugSpawnObjectHere(string name)
        {
            
            return DebugSpawnObjectAt(name, HeroController.instance.transform.position + Vector3.right * 10f);
        }

        public static GameObject DebugSpawnObjectAt(string name, Vector3 pos)
        {
            Dev.Where();
            if (EnemyRandomizerDB.Instance.database == null)
            {
                Dev.Log("NO DATABASE LOADED -- CANNOT SPAWN ANYTHING");
                return null;
            }

            GameObject thing = EnemyRandomizerDB.Instance.database.Spawn(name);
            if(thing == null)
            {
                Dev.Log("FAILED TO SPAWN " + name);
                return null;
            }
            thing.transform.position = pos;
            thing.SetActive(true);
            return thing;
        }
    }
}

#endif