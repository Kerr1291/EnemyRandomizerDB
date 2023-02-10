#define UNITY_EDITOR
#if !LIBRARY
using UnityEngine.SceneManagement;
using UnityEngine;
using Language;
using On;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System;
using Dev = EnemyRandomizerMod.Dev;
using System.Reflection;

namespace EnemyRandomizerMod
{
    //Just a bunch of junk I was using to test/develop things that I'm too lazy to refactor or trim out.
    //I really didn't want this stuff in the final build and I didn't want others using this stuff so I intentionally crammed it all into this
    //large, ugly, internal, utility class to keep the file clutter to a minimum
    internal static class ModUtils
    {
        public static bool SerializeXMLToFile<T>(this string path, T data) where T : class
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            FileStream fstream = null;
            try
            {
                fstream = new FileStream(path, FileMode.Create);
                serializer.Serialize(fstream, data);
                result = true;
            }
            catch (System.Exception e)
            {
                Dev.LogError(e.Message);
                //System.Windows.Forms.MessageBox.Show("Error creating/saving file "+ e.Message);
            }
            finally
            {
                fstream.Close();
            }
            return result;
        }

        public static int GetGeoSmall(this HealthManager healthManager)
        {
            FieldInfo fi = healthManager.GetType().GetField("smallGeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object temp = fi.GetValue(healthManager);
            int value = (temp == null ? 0 : (int)temp);
            return value;
        }

        public static int GetGeoMedium(this HealthManager healthManager)
        {
            FieldInfo fi = healthManager.GetType().GetField("mediumGeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object temp = fi.GetValue(healthManager);
            int value = (temp == null ? 0 : (int)temp);
            return value;
        }

        public static int GetGeoLarge(this HealthManager healthManager)
        {
            FieldInfo fi = healthManager.GetType().GetField("largeGeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object temp = fi.GetValue(healthManager);
            int value = (temp == null ? 0 : (int)temp);
            return value;
        }

        public static GameObject GetBattleScene(this HealthManager healthManager)
        {
            FieldInfo fi = typeof(HealthManager).GetField("battleScene", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
            {
                return fi.GetValue(healthManager) as GameObject;
            }

            return null;
        }


        public static GameObject FindGameObject(string pathName)
        {
            string[] path = pathName.Trim('/').Split('/');

            if (path.Length <= 0)
                return null;

            GameObject root = null;

            //search for a game object with a name that matches the first string
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            {
                Scene s = (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));

                if (!s.IsValid() || !s.isLoaded)
                {
                    Dev.Log($"INVALID SCENE STATE - [{i}][{s.name}] - VALID:{s.IsValid()} LOADED:{s.isLoaded}");
                    continue;
                }

                //Dev.Log("Searching " + s.name);
                root = s.GetRootGameObjects().FirstOrDefault(x => string.Compare(x.name, path[0]) == 0);
                //Dev.LogVarArray("root scene object", s.GetRootGameObjects().Select(x => x.name).ToArray());

                if (root != null)
                    break;
            }

            if (root == null)
                return null;

            //if (root == null)
            //{
            //    if (root == null)
            //    {
            //        Dev.Log("===========================================================");
            //        Dev.Log($"Was searching for {pathName} in loaded scenes and could not find it. Dumping possible matches");
            //        possiblePrefabs.ToList().ForEach(x => Dev.Log($"[OBJ:{x} SCENE:{x.scene.name} NAME:{x.name} PATH:{x.GetSceneHierarchyPath()}]"));
            //        Dev.Log("===========================================================");
            //    }
            //    return null;
            //}

            return root.FindGameObject(pathName);
        }

        public static GameObject FindResource(string pathName)
        {
            string[] path = pathName.Trim('/').Split('/');

            if (path.Length <= 0)
                return null;

            var possiblePrefabs = Resources.FindObjectsOfTypeAll<GameObject>();
            var found = possiblePrefabs.FirstOrDefault(x => x.GetSceneHierarchyPath() == pathName);

            //if(found == null)
            //{
            //    Dev.Log("===========================================================");
            //    Dev.Log($"Was searching for {pathName} in resources and could not find it. Dumping possible matches");
            //    possiblePrefabs.ToList().ForEach(x => Dev.Log($"[OBJ:{x} SCENE:{x.scene.name} NAME:{x.name} PATH:{x.GetSceneHierarchyPath()}]"));
            //    Dev.Log("===========================================================");
            //}

            return found;
        }

        public static GameObject FindGameObject(this GameObject gameObject, string pathName)
        {
            string[] path = pathName.Trim('/').Split('/');

            if (gameObject.name != path[0])
                return null;

            List<string> remainingPath = new List<string>(path);
            remainingPath.RemoveAt(0);

            if (remainingPath.Count <= 0)
                return gameObject;

            string subPath = string.Join("/", remainingPath.ToArray());

            var children = gameObject.GetDirectChildren();

            foreach (var child in children)
            {
                GameObject found = child.FindGameObject(subPath);
                if (found != null)
                    return found;
            }

            return null;
        }

        //public static string GetSceneHierarchyPath(this GameObject gameObject)
        //{
        //    if (gameObject == null)
        //        return "null";

        //    string objStr = gameObject.name;

        //    if (gameObject.transform.parent != null)
        //        objStr = gameObject.transform.parent.gameObject.GetSceneHierarchyPath() + "/" + gameObject.name;

        //    return objStr;
        //}

        public static List<GameObject> GetDirectChildren(this GameObject gameObject)
        {
            List<GameObject> children = new List<GameObject>();
            if (gameObject == null)
                return children;

            for (int k = 0; k < gameObject.transform.childCount; ++k)
            {
                Transform child = gameObject.transform.GetChild(k);
                children.Add(child.gameObject);
            }
            return children;
        }


        public static void PrintSceneHierarchyTree(this GameObject gameObject, bool printComponents = false, System.IO.StreamWriter file = null)
        {
            if (gameObject == null)
                return;

            if (file != null)
            {
                file.WriteLine("START =====================================================");
                file.WriteLine("Printing scene hierarchy for game object: " + gameObject.name);
            }
            else
            {
                Dev.Log("START =====================================================");
                Dev.Log("Printing scene hierarchy for game object: " + gameObject.name);
            }

            foreach (Transform t in gameObject.GetComponentsInChildren<Transform>(true))
            {
                string objectNameAndPath = t.gameObject.GetSceneHierarchyPath();

                string inactiveString = string.Empty;
                if (t != null && t.gameObject != null && !t.gameObject.activeInHierarchy)
                    inactiveString = " (inactive)";

                if (file != null)
                {
                    file.WriteLine(objectNameAndPath + inactiveString);
                }
                else
                {
                    Dev.Log(objectNameAndPath + inactiveString);
                }


                if (printComponents)
                {
                    string componentHeader = "";
                    for (int i = 0; i < (objectNameAndPath.Length - t.gameObject.name.Length); ++i)
                        componentHeader += " ";

                    foreach (Component c in t.GetComponents<Component>())
                    {
                        c.PrintComponentType(componentHeader, file);

                        if (c is Transform)
                            c.PrintTransform(componentHeader, file);
                        else
                            c.PrintComponentWithReflection(componentHeader, file);
                    }
                }
            }

            if (file != null)
            {
                file.WriteLine("END +++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            }
            else
            {
                Dev.Log("END +++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            }
        }

        public static void PrintComponentType(this Component c, string componentHeader = "", System.IO.StreamWriter file = null)
        {
            if (c == null)
                return;

            if (file != null)
            {
                file.WriteLine(componentHeader + @" \--Component: " + c.GetType().Name);
            }
            else
            {
                Dev.Log(componentHeader + @" \--Component: " + c.GetType().Name);
            }
        }

        public static void PrintTransform(this Component c, string componentHeader = "", System.IO.StreamWriter file = null)
        {
            if (c as Transform != null)
            {
                if (file != null)
                {
                    file.WriteLine(componentHeader + @" \--GameObject layer: " + (c as Transform).gameObject.layer);
                    file.WriteLine(componentHeader + @" \--GameObject tag: " + (c as Transform).gameObject.tag);
                    file.WriteLine(componentHeader + @" \--Transform Position: " + (c as Transform).position);
                    file.WriteLine(componentHeader + @" \--Transform Rotation: " + (c as Transform).rotation.eulerAngles);
                    file.WriteLine(componentHeader + @" \--Transform LocalScale: " + (c as Transform).localScale);
                }
                else
                {
                    Dev.Log(componentHeader + @" \--GameObject layer: " + (c as Transform).gameObject.layer);
                    Dev.Log(componentHeader + @" \--GameObject tag: " + (c as Transform).gameObject.tag);
                    Dev.Log(componentHeader + @" \--Transform Position: " + (c as Transform).position);
                    Dev.Log(componentHeader + @" \--Transform Rotation: " + (c as Transform).rotation.eulerAngles);
                    Dev.Log(componentHeader + @" \--Transform LocalScale: " + (c as Transform).localScale);
                }
            }
        }

        public static void PrintBoxCollider2D(this Component c, string componentHeader = "", System.IO.StreamWriter file = null)
        {
            if (c as BoxCollider2D != null)
            {
                if (file != null)
                {
                    file.WriteLine(componentHeader + @" \--BoxCollider2D Size: " + (c as BoxCollider2D).size);
                    file.WriteLine(componentHeader + @" \--BoxCollider2D Offset: " + (c as BoxCollider2D).offset);
                    file.WriteLine(componentHeader + @" \--BoxCollider2D Bounds-Min: " + (c as BoxCollider2D).bounds.min);
                    file.WriteLine(componentHeader + @" \--BoxCollider2D Bounds-Max: " + (c as BoxCollider2D).bounds.max);
                    file.WriteLine(componentHeader + @" \--BoxCollider2D isTrigger: " + (c as BoxCollider2D).isTrigger);
                }
                else
                {
                    Dev.Log(componentHeader + @" \--BoxCollider2D Size: " + (c as BoxCollider2D).size);
                    Dev.Log(componentHeader + @" \--BoxCollider2D Offset: " + (c as BoxCollider2D).offset);
                    Dev.Log(componentHeader + @" \--BoxCollider2D Bounds-Min: " + (c as BoxCollider2D).bounds.min);
                    Dev.Log(componentHeader + @" \--BoxCollider2D Bounds-Max: " + (c as BoxCollider2D).bounds.max);
                    Dev.Log(componentHeader + @" \--BoxCollider2D isTrigger: " + (c as BoxCollider2D).isTrigger);
                }
            }
        }

        public static void PrintComponentWithReflection(this Component c, string componentHeader = "", System.IO.StreamWriter file = null)
        {
            Type cType = c.GetType();
            var bflags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var mflags = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method;

            var members = cType.GetMembers(bflags);

            foreach (var m in members)
            {
                string label = m.Name;
                string data = "Not a field or property!";

                if (m is FieldInfo)
                {
                    try
                    {
                        object fo = (m as FieldInfo).GetValue(c);
                        data = fo == null ? "null" : fo.ToString();
                    }
                    catch (Exception e)
                    {
                        Dev.Log("Failed to get field value from member field " + label);
                    }
                }
                else if (m is PropertyInfo)
                {
                    try
                    {
                        object po = (m as PropertyInfo).GetValue(c, null);
                        data = po == null ? "null" : po.ToString();
                    }
                    catch (Exception e)
                    {
                        Dev.Log("Failed to get property value from member property " + label);
                    }
                }

                Print(componentHeader, label, data, file);
            }
        }

        private static void Print(string header, string label, string data, System.IO.StreamWriter file = null)
        {
            if (file != null)
            {
                file.WriteLine(header + @" \--" + label + ": " + data);
            }
            else
            {
                Dev.Log(header + @" \--" + label + ": " + data);
            }
        }

        public static T GetOrAddComponent<T>(this GameObject source) where T : UnityEngine.Component
        {
            T result = source.GetComponent<T>();
            if (result != null)
                return result;
            result = source.AddComponent<T>();
            return result;
        }

        /// <summary>
        /// Mathematical modulus, different from the % operation that returns the remainder.
        /// This performs a "wrap around" of the given value assuming the range [0, mod)
        /// Define mod 0 to return the value unmodified
        /// </summary>
        public static int Modulus(this int value, int mod)
        {
            if (value > 0)
                return (value % mod);
            else if (value < 0)
                return (value % mod + mod) % mod;
            else
                return value;
        }

        public static TComponent FindObjectOfType<TComponent>(bool includeInactive = true)
            where TComponent : Component
        {
            return FindObjectsOfType<TComponent>(includeInactive).FirstOrDefault();
        }

        public static List<TComponent> FindObjectsOfType<TComponent>(bool includeInactive = true)
            where TComponent : Component
        {
            List<TComponent> components = new List<TComponent>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            {
                Scene s = (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
                if (!s.IsValid())
                    continue;
                if (!s.isLoaded)
                    continue;
                var rootObjects = s.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    var objectsOfType = rootObject.GetComponentsInChildren<TComponent>(includeInactive);
                    if (objectsOfType.Length > 0)
                        components.AddRange(objectsOfType);
                }
            }
            return components;
        }

        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        //public static void SetRect(this RectTransform rt, Rect rect, bool centerIsBottomLeft = false)
        //{
        //    if(centerIsBottomLeft)
        //    {
        //        rt.SetLeft(rect.x);
        //        rt.SetRight(rect.x + rect.size.x);
        //        rt.SetTop(rect.y);
        //        rt.SetBottom(rect.y + rect.size.y);
        //    }
        //    else
        //    {
        //        var bl = rect.BottomLeft();
        //        var tr = rect.TopRight();
        //        rt.SetLeft(bl.x);
        //        rt.SetRight(tr.x);
        //        rt.SetTop(tr.y);
        //        rt.SetBottom(bl.y);
        //    }
        //}

        public static bool IsMouseOn(this RectTransform rectTransform)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
        }

        public static bool IsPointOn(this RectTransform rectTransform, Vector2 screenPoint)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint);
        }

        public static Rect SetMinMax(this Rect input, Vector2 min, Vector2 max)
        {
            Vector2 rMin = Mathnv.Min(min, max);
            Vector2 rMax = Mathnv.Max(min, max);

            Rect r = new Rect((rMax - rMin) * .5f + rMin, rMax - rMin);
            return r;
        }

        public static Vector2 TopLeft(this Rect input, bool flipYAxis = true)
        {
            return new Vector2(input.xMin, flipYAxis ? input.yMin : input.yMax);
        }

        public static Vector2 TopRight(this Rect input, bool flipYAxis = true)
        {
            return new Vector2(input.xMax, flipYAxis ? input.yMin : input.yMax);
        }

        public static Vector2 BottomRight(this Rect input, bool flipYAxis = true)
        {
            return new Vector2(input.xMax, flipYAxis ? input.yMax : input.yMin);
        }

        public static Vector2 BottomLeft(this Rect input, bool flipYAxis = true)
        {
            return new Vector2(input.xMin, flipYAxis ? input.yMax : input.yMin);
        }

        public static void Clamp(this Rect area, Vector2 pos, Vector2 size)
        {
            Mathnv.Clamp(ref area, pos, size);
        }

        public static void Clamp(this Rect area, Rect min_max)
        {
            Mathnv.Clamp(ref area, min_max);
        }

        internal static Range GetXRange(this Rect r)
        {
            return new Range(r.xMin, r.xMax);
        }

        internal static Range GetYRange(this Rect r)
        {
            return new Range(r.yMin, r.yMax);
        }

        public static bool GetIntersectionRect(this Rect r1, Rect r2, out Rect area)
        {
            area = default(Rect);
            if (r2.Overlaps(r1))
            {
                float num = Mathf.Min(r1.xMax, r2.xMax);
                float num2 = Mathf.Max(r1.xMin, r2.xMin);
                float num3 = Mathf.Min(r1.yMax, r2.yMax);
                float num4 = Mathf.Max(r1.yMin, r2.yMin);
                area.x = Mathf.Min(num, num2);
                area.y = Mathf.Min(num3, num4);
                area.width = Mathf.Max(0f, num - num2);
                area.height = Mathf.Max(0f, num3 - num4);
                return true;
            }
            return false;
        }
    }
}
#endif