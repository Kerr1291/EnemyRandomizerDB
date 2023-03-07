using UnityEngine.SceneManagement;
using UnityEngine;
using Language;
using On;
using EnemyRandomizerMod;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Collections;
using System;
using HutongGames.PlayMaker;
using Modding;
#if !LIBRARY
using Dev = EnemyRandomizerMod.Dev;
#else
using Dev = Modding.Logger;
#endif
namespace EnemyRandomizerMod
{
    public class GiantFlyControl : MonoBehaviour
    {
        //default
        public static int babiesToSpawn = 7;

        static string MODHOOK_BeforeSceneLoad(string sceneName)
        {
            ModHooks.BeforeSceneLoadHook -= MODHOOK_BeforeSceneLoad;
            On.HutongGames.PlayMaker.Actions.ActivateAllChildren.OnEnter -= ActivateAllChildren_OnEnter;
            return sceneName;
        }

        void OnEnable()
        {
            ModHooks.BeforeSceneLoadHook -= MODHOOK_BeforeSceneLoad;
            ModHooks.BeforeSceneLoadHook += MODHOOK_BeforeSceneLoad;
            On.HutongGames.PlayMaker.Actions.ActivateAllChildren.OnEnter -= ActivateAllChildren_OnEnter;
            On.HutongGames.PlayMaker.Actions.ActivateAllChildren.OnEnter += ActivateAllChildren_OnEnter;
        }

        static void ActivateAllChildren_OnEnter(On.HutongGames.PlayMaker.Actions.ActivateAllChildren.orig_OnEnter orig, HutongGames.PlayMaker.Actions.ActivateAllChildren self)
        {
            orig(self);

            bool isGiantFlyFSM = self.State.Name == "Spawn Flies 2";

            if (!isGiantFlyFSM)
                return;

            try
            {
                if (EnemyRandomizerDatabase.GetDatabase != null)
                {
                    for (int i = 0; i < babiesToSpawn; ++i)
                    {
                        GameObject result = null;
                        if (EnemyRandomizerDatabase.GetDatabase().Enemies.TryGetValue("Fly", out var src))
                        {
                            result = EnemyRandomizerDatabase.GetDatabase().Spawn(src);
                        }
                        else
                        {
                            result = EnemyRandomizerDatabase.GetDatabase().Spawn("Fly");
                        }

                        if (result != null && self.Owner != null)
                        {
                            result.transform.position = self.Owner.transform.position;
                            result.SetActive(true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Dev.LogError($"Caught exception trying to spawn a custom hatcher child! {e.Message} STACKTRACE:{e.StackTrace}");
            }
        }
    }

    internal class GiantFlyPrefabConfig : IPrefabConfig
    {
        public virtual void SetupPrefab(PrefabObject p)
        {
            var control = p.prefab.AddComponent<GiantFlyControl>();
        }
    }
}
