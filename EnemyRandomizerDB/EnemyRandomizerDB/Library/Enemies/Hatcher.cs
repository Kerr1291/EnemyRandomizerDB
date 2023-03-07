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
using HutongGames.PlayMaker.Actions;
#if !LIBRARY
using Dev = EnemyRandomizerMod.Dev;
#else
using Dev = Modding.Logger;
#endif
namespace EnemyRandomizerMod
{
    public class HatcherControl : MonoBehaviour
    {
        public int maxBabies = 3;
        public int babiesRemaining = 3;

        void OnEnable()
        {
            babiesRemaining = maxBabies;

            On.HutongGames.PlayMaker.Actions.GetRandomChild.DoGetRandomChild -= GetRandomChild_DoGetRandomChild;
            On.HutongGames.PlayMaker.Actions.GetRandomChild.DoGetRandomChild += GetRandomChild_DoGetRandomChild;


            var fsm = gameObject.LocateMyFSM("Hatcher");

            //replace get child count with "set int value" to manually set the value for cage children
            fsm.Fsm.GetState("Hatched Max Check").Actions = fsm.Fsm.GetState("Hatched Max Check").Actions.Select(x => { 
            if(x.GetType() == typeof(HutongGames.PlayMaker.Actions.GetChildCount))
            {
                var action = new HutongGames.PlayMaker.Actions.SetIntValue();
                action.Init(x.State);
                action.intVariable = new FsmInt("Cage Children");
                action.intValue = new FsmInt();
                action.intValue = babiesRemaining;
                return action;
            }
            else
            {
                return x;
            }
            }).ToArray();
        }

        void OnDsiable()
        {
            On.HutongGames.PlayMaker.Actions.GetRandomChild.DoGetRandomChild -= GetRandomChild_DoGetRandomChild;
        }

        void GetRandomChild_DoGetRandomChild(On.HutongGames.PlayMaker.Actions.GetRandomChild.orig_DoGetRandomChild orig, HutongGames.PlayMaker.Actions.GetRandomChild self)
        {
            orig(self);

            //don't run this logic
            var owner = self.Fsm.GetOwnerDefaultTarget(self.gameObject);
            if (owner != gameObject)
                return;

            try
            {
                GameObject result = null;

                if (babiesRemaining > 0)
                {
                    if (EnemyRandomizerDatabase.GetDatabase().Enemies.TryGetValue("Hatcher Baby", out var src))
                    {
                        result = EnemyRandomizerDatabase.GetDatabase().Spawn(src);
                    }
                    else
                    {
                        result = EnemyRandomizerDatabase.GetDatabase().Spawn("Hatcher Baby");
                    }

                    if (result != null && self.Owner != null)
                    {
                        babiesRemaining--;
                        (self.Fsm.GetState("Hatched Max Check").Actions.FirstOrDefault(x => x is SetIntValue) as SetIntValue).intValue.Value = babiesRemaining;
                        result.transform.position = self.Owner.transform.position;
                        result.SetActive(true);
                    }
                }

                self.storeResult.Value = result;
            }
            catch(Exception e)
            {
                Dev.LogError($"Caught exception trying to spawn a custom hatcher child! {e.Message} STACKTRACE:{e.StackTrace}");
            }
        }
    }

    internal class HatcherPrefabConfig : IPrefabConfig
    {
        public virtual void SetupPrefab(PrefabObject p)
        {
            var control = p.prefab.AddComponent<HatcherControl>();
        }
    }
}
