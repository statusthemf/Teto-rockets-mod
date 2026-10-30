using System;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public class RocketDetector : MonoBehaviour
    {
        private float scanTimer;
        private float diagnosticTimer;
        private float soundCheckTimer;

        private const float ScanInterval = 0.5f;
        private const float DiagnosticInterval = 5f;
        private const float SoundCheckInterval = 3f;

        private readonly HashSet<int> loggedGrenades =
            new HashSet<int>();

        public RocketDetector(IntPtr ptr) : base(ptr)
        {
        }

        private void Update()
        {
            try
            {
                float delta = Time.deltaTime;

                scanTimer += delta;
                diagnosticTimer += delta;
                soundCheckTimer += delta;

                if (scanTimer >= ScanInterval)
                {
                    scanTimer = 0f;
                    ScanForRockets();
                }

                if (diagnosticTimer >= DiagnosticInterval)
                {
                    diagnosticTimer = 0f;

                    TetoRocketsPlugin.PluginLog.LogInfo(
                        "Detector alive. Scene: " +
                        UnityEngine.SceneManagement.SceneManager
                            .GetActiveScene().name
                    );
                }

                if (soundCheckTimer >= SoundCheckInterval)
                {
                    soundCheckTimer = 0f;
                    TetoRocketsPlugin.RecoverSoundRuntime();
                }

                TetoRocketReplacer.CleanupDestroyedRockets();
            }
            catch (Exception exception)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "RocketDetector.Update exception: " + exception
                );
            }
        }

        private void ScanForRockets()
        {
            try
            {
                Grenade[] grenades =
                    UnityEngine.Object.FindObjectsOfType<Grenade>();

                if (grenades == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "FindObjectsOfType<Grenade> returned null."
                    );
                    return;
                }

                foreach (Grenade grenade in grenades)
                {
                    if (grenade == null)
                        continue;

                    int instanceId =
                        grenade.gameObject.GetInstanceID();

                    if (loggedGrenades.Add(instanceId))
                    {
                        TetoRocketsPlugin.PluginLog.LogInfo(
                            "Grenade found: " +
                            grenade.gameObject.name +
                            " | Rocket: " + grenade.rocket +
                            " | Enemy: " + grenade.enemy
                        );
                    }

                    if (!grenade.rocket)
                        continue;

                    TetoRocketReplacer.ProcessRocket(grenade);
                }
            }
            catch (Exception exception)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "Rocket scan exception: " + exception
                );
            }
        }
    }
}
