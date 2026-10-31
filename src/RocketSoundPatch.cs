
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.Shoot))]
internal static class RocketSoundPatch
{
    // Runs immediately BEFORE the original Shoot() method.
    // Remember every Grenade that already exists.
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
                if (grenade == null)
                    continue;

                __state.Add(grenade.GetInstanceID());
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[ULTRADROID Teto Rockets] Rocket snapshot failed: " + ex);
        }
    }

    // Runs immediately AFTER the original Shoot() method.
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

                int instanceId = grenade.GetInstanceID();

                // It existed before firing, so it isn't our new projectile.
                if (__state != null && __state.Contains(instanceId))
                    continue;

                // New rocket: replace it immediately.
                TetoRocketReplacer.ProcessRocket(grenade);
                detected++;
            }

            if (detected > 0)
            {
                Debug.Log(
                    "[ULTRADROID Teto Rockets] Firing hook detected "
                    + detected + " new rocket(s).");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[ULTRADROID Teto Rockets] Firing detection failed: " + ex);
        }

        // Keep the existing Teto firing sound.
        TetoRocketsPlugin.PlayTetoSound();
    }
}
