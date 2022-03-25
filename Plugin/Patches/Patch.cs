using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace LordAshes
{
    public partial class HSVPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(CreatureManager), "AddCreature")]
        public static class PatchCreateAndAddNewCreature
        {
            public static void Postfix(CreatureDataV2 creatureData, PlayerGuid[] owners, bool spawnedByLoad)
            {
                Debug.Log("HSV Plugin: Patch: Triggering HSV Refresh");

                if (useAutomaticTriggers)
                {
                    string HSV = StatMessaging.ReadInfo(creatureData.CreatureId, HSVPlugin.Guid);
                    if (HSV != null && HSV.Trim() != "")
                    {
                        string[] values = HSV.Split(',');
                        Debug.Log("HSV Plugin: Patch: Enqueing Transformation.");
                        transformations.Enqueue(new object[] { creatureData.CreatureId, float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]) });
                    }
                }
            }
        }
    }
}
