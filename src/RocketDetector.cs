using System;
using System.Collections;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ULTRADROID.TetoRockets
{
    public sealed class RocketDetector : MonoBehaviour
    {
        public RocketDetector(IntPtr ptr) : base(ptr) { }

        private float readinessTimer;
        private float rocketScanTimer;
        private float diagnosticsTimer;

        private bool gameplayActive;

        private const float ReadinessInterval = 0.5f;
        private const float RocketScanInterval = 0.25f;
        private const float DiagnosticsInterval = 5f;

        private void Update()
        {
            readinessTimer -= Time.unscaledDeltaTime;
            rocketScanTimer -= Time.unscaledDeltaTime;
            diagnosticsTimer -= Time.unscaledDeltaTime;

            if (readinessTimer <= 0f)
            {
                readinessTimer = ReadinessInterval;
                CheckGameplayState();
            }

            if (diagnosticsTimer <= 0f)
            {
                diagnosticsTimer = DiagnosticsInterval;

                Debug.Log(
                    $"[ULTRADROID Teto Rockets] Detector alive | " +
                    $"Scene: {SceneManager.GetActiveScene().name} | " +
                    $"Gameplay active: {gameplayActive}");
            }

            if (!gameplayActive || rocketScanTimer > 0f)
                return;

            rocketScanTimer = RocketScanInterval;

            ScanForRockets();
        }

        private void CheckGameplayState()
        {
            bool ready = IsGameplayReady();

            if (ready == gameplayActive)
                return;

            gameplayActive = ready;

            if (gameplayActive)
            {
                Debug.Log(
                    "[ULTRADROID Teto Rockets] Gameplay detected. " +
                    "Initializing gameplay systems.");

                TetoRocketsPlugin.OnGameplayStarted();
            }
            else
            {
                Debug.Log(
                    "[ULTRADROID Teto Rockets] Gameplay stopped.");

                TetoRocketsPlugin.OnGameplayStopped();
            }
        }

        private bool IsGameplayReady()
        {
            GunControl gunControl =
                UnityEngine.Object.FindObjectOfType<GunControl>();

            if (gunControl == null)
                return false;

            if (gunControl.slots == null)
                return false;

            return true;
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

                TetoRocketReplacer.CleanupDestroyedRockets();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ULTRADROID Teto Rockets] Rocket scan error: " + ex);
            }
        }

        private void OnDestroy()
        {
            if (gameplayActive)
            {
                gameplayActive = false;
                TetoRocketsPlugin.OnGameplayStopped();
            }

            Debug.Log(
                "[ULTRADROID Teto Rockets] Rocket detector destroyed.");
        }
    }
}
