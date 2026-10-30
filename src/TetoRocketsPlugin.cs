using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    [BepInPlugin(
        "somerandommaid.ultradroid.tetorockets",
        "ULTRADROID Teto Rockets",
        "0.1.0"
    )]
    [BepInDependency("somerandommaid.ultradroid.models")]
    public class TetoRocketsPlugin : BasePlugin
    {
        internal static ManualLogSource PluginLog;

        public override void Load()
        {
            PluginLog = Logger;

            Log.LogInfo("Initializing ULTRADROID Teto Rockets...");

            ClassInjector.RegisterTypeInIl2Cpp<RocketDetector>();

            GameObject runtime = new GameObject(
                "ULTRADROID_Teto_Rockets_Runtime"
            );

            Object.DontDestroyOnLoad(runtime);

            runtime.AddComponent<RocketDetector>();

            Log.LogInfo("ULTRADROID Teto Rockets initialized.");
        }
    }
}
