
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.Shoot))]
    internal static class RocketSoundPatch
    {
        [HarmonyPrefix]
        private static void Prefix(out HashSet<int> __state)
        {
            __state = new HashSet<int>();

            try
            {
                Grenade[] existing =
                    UnityEngine.Object.FindObjectsOfType<Grenade>();

                foreach (Grenade grenade in existing)
                {
                    if (grenade != null)
                        __state.Add(grenade.GetInstanceID());
                }
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "[Firing Detection] Snapshot failed: " + ex
                );
            }
        }

        [HarmonyPostfix]
        private static void Postfix(HashSet<int> __state)
        {
            if (!TetoRocketsPlugin.GameplayActive)
                return;

            try
            {
                Grenade[] current =
                    UnityEngine.Object.FindObjectsOfType<Grenade>();

                int detected = 0;

                foreach (Grenade grenade in current)
                {
                    if (grenade == null || !grenade.rocket)
                        continue;

                    int id = grenade.GetInstanceID();

                    if (__state != null && __state.Contains(id))
                        continue;

                    TetoRocketReplacer.ProcessRocket(grenade);
                    detected++;
                }

                if (detected > 0)
                {
                    TetoRocketsPlugin.PluginLog.LogInfo(
                        "[Firing Detection] Detected " +
                        detected + " new rocket(s) immediately after Shoot()."
                    );
                }
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Firing Detection] Failed: " + ex
                );
            }

            TetoRocketsPlugin.PlayTetoSound();
        }
    }
}
