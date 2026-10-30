using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public static class TetoRocketReplacer
    {
        public static void ProcessRocket(Grenade grenade)
        {
            if (grenade == null)
                return;

            Transform rocketRoot = grenade.transform;

            Transform modelTransform =
                rocketRoot.Find("Model");

            if (modelTransform == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Rocket has no Model child: " +
                    rocketRoot.name
                );

                return;
            }

            MeshRenderer originalRenderer =
                modelTransform.GetComponent<MeshRenderer>();

            if (originalRenderer == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Rocket Model has no MeshRenderer: " +
                    rocketRoot.name
                );

                return;
            }

            TetoRocketsPlugin.PluginLog.LogInfo(
                "Rocket visual found. Ready for Teto replacement: " +
                rocketRoot.name
            );

            // Teto GLB loading and attachment will go here.
            // Do not disable the original renderer until
            // the replacement model has loaded successfully.
        }
    }
}
