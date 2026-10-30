
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public class RocketDetector : MonoBehaviour
    {
        private float scanTimer;
        private float diagnosticTimer;

        private const float ScanInterval = 0.5f;
        private const float DiagnosticInterval = 5f;

        public RocketDetector(System.IntPtr ptr) : base(ptr)
        {
        }

        private void Update()
        {
            scanTimer += Time.deltaTime;
            diagnosticTimer += Time.deltaTime;

            if (scanTimer >= ScanInterval)
            {
                scanTimer = 0f;
                ScanForRockets();
            }

            if (diagnosticTimer >= DiagnosticInterval)
            {
                diagnosticTimer = 0f;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Rocket detector is alive. Scene: " +
                    UnityEngine.SceneManagement.SceneManager
                        .GetActiveScene().name
                );
            }
        }

        private void ScanForRockets()
        {
            Grenade[] grenades =
                UnityEngine.Object.FindObjectsOfType<Grenade>();

            if (grenades == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "FindObjectsOfType returned null."
                );
                return;
            }

            if (grenades.Length > 0)
            {
                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Grenade scan found " + grenades.Length +
                    " component(s)."
                );
            }

            foreach (Grenade grenade in grenades)
            {
                if (grenade == null)
                    continue;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Grenade candidate: " +
                    grenade.gameObject.name +
                    " | Rocket: " + grenade.rocket +
                    " | Enemy: " + grenade.enemy
                );

                if (!grenade.rocket)
                    continue;

                TetoRocketReplacer.ProcessRocket(grenade);
            }
        }
    }
}
