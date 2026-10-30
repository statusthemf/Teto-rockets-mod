using System;

using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public class RocketDetector : MonoBehaviour
    {
        public RocketDetector(IntPtr ptr) : base(ptr) { }

        private float readinessTimer;
        private float rocketScanTimer;
        private float diagnosticTimer;

        private const float ReadinessInterval = 0.5f;
        private const float RocketScanInterval = 0.25f;
        private const float DiagnosticInterval = 5f;

        private bool wasGameplayReady;

        private void Update()
        {
            try
            {
                readinessTimer += Time.deltaTime;
                diagnosticTimer += Time.deltaTime;

                if (readinessTimer >= ReadinessInterval)
                {
                    readinessTimer = 0f;
                    CheckGameplayReadiness();
                }

                if (diagnosticTimer >= DiagnosticInterval)
                {
                    diagnosticTimer = 0f;

                    TetoRocketsPlugin.PluginLog.LogInfo(
                        "[Detector] Alive. Gameplay active: " +
                        TetoRocketsPlugin.GameplayActive
                    );
                }

                // Do not scan for Grenade objects outside gameplay.
                if (!TetoRocketsPlugin.GameplayActive)
                    return;

                rocketScanTimer += Time.deltaTime;

                if (rocketScanTimer >= RocketScanInterval)
                {
                    rocketScanTimer = 0f;
                    ScanForRockets();
                }

                TetoRocketReplacer.CleanupDestroyedRockets();
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Detector] Update exception: " + ex
                );
            }
        }

        private void CheckGameplayReadiness()
        {
            bool ready = IsGameplayReady();

            if (ready && !wasGameplayReady)
            {
                wasGameplayReady = true;
                TetoRocketsPlugin.OnGameplayStarted();
            }
            else if (!ready && wasGameplayReady)
            {
                wasGameplayReady = false;
                TetoRocketsPlugin.OnGameplayStopped();
            }
        }

        private bool IsGameplayReady()
        {
            try
            {
                // GunControl is only used as a readiness signal here.
                // We do not scan Grenade objects until this succeeds.
                GunControl gunControl =
                    UnityEngine.Object.FindObjectOfType<GunControl>();

                if (gunControl == null)
                    return false;

                if (gunControl.slots == null)
                    return false;

                if (gunControl.slots.Count < 6)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "[Gameplay] Readiness check failed: " + ex.Message
                );

                return false;
            }
        }

        private void ScanForRockets()
        {
            try
            {
                Grenade[] grenades =
                    UnityEngine.Object.FindObjectsOfType<Grenade>();

                foreach (Grenade grenade in grenades)
                {
                    if (grenade == null)
                        continue;

                    if (!grenade.rocket)
                        continue;

                    TetoRocketReplacer.ProcessRocket(grenade);
                }
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Detector] Rocket scan exception: " + ex
                );
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (wasGameplayReady)
                {
                    wasGameplayReady = false;
                    TetoRocketsPlugin.OnGameplayStopped();
                }
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Detector] OnDestroy exception: " + ex
                );
            }
        }
    }
}
