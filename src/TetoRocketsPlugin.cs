
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using ULTRADROID.Models;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    [BepInPlugin(
        "somerandommaid.ultradroid.tetorockets",
        "ULTRADROID Teto Rockets",
        "0.2.0"
    )]
    [BepInDependency("somerandommaid.ultradroid.models")]
    public class TetoRocketsPlugin : BasePlugin
    {
        internal static ManualLogSource PluginLog;
        internal static ModelOwner ModelOwner;
        internal static string ModelPath;

        public override void Load()
        {
            PluginLog = base.Log;

            PluginLog.LogInfo("Initializing Teto Rockets...");

            PluginLog.LogInfo(
                "Detected ULTRADROID Models API version: " +
                ModelAPI.Version
            );

            string pluginFolder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            );

            ModelPath = Path.Combine(
                pluginFolder,
                "assets",
                "Teto.glb"
            );

            if (!File.Exists(ModelPath))
            {
                PluginLog.LogError(
                    "Teto.glb was not found: " + ModelPath
                );
                return;
            }

            ModelOwner = ModelAPI.CreateOwner(
                "somerandommaid.ultradroid.tetorockets"
            );

            ClassInjector.RegisterTypeInIl2Cpp<RocketDetector>();

            GameObject runtime = new GameObject(
                "ULTRADROID_Teto_Rockets_Runtime"
            );

            UnityEngine.Object.DontDestroyOnLoad(runtime);

            runtime.AddComponent<RocketDetector>();

            PluginLog.LogInfo("Teto Rockets initialized.");
            PluginLog.LogInfo("Model path: " + ModelPath);
        }
    }
}
