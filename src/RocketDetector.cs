using System.Collections.Generic;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public class RocketDetector : MonoBehaviour
    {
        private readonly HashSet<int> processedRockets =
            new HashSet<int>();

        private float scanTimer;
        private const float ScanInterval = 0.5f;

        public RocketDetector(System.IntPtr ptr) : base(ptr)
        {
        }

        private void Update()
        {
            scanTimer += Time.deltaTime;

            if (scanTimer < ScanInterval)
                return;

            scanTimer = 0f;

            ScanForRockets();
        }

        private void ScanForRockets()
        {
            Grenade[] grenades =
            UnityEngine.Object.FindObjectsOfType<Grenade>();
            
            foreach (Grenade grenade in grenades)
            {
                if (grenade == null)
                    continue;

                if (!grenade.rocket)
                    continue;

                int instanceId = grenade.gameObject.GetInstanceID();

                if (processedRockets.Contains(instanceId))
                    continue;

                processedRockets.Add(instanceId);

                bool isEnemy = grenade.enemy;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Rocket detected: " +
                    grenade.gameObject.name +
                    " | Enemy: " + isEnemy +
                    " | Instance ID: " + instanceId
                );

                TetoRocketReplacer.ProcessRocket(grenade);
            }
        }
    }
}
