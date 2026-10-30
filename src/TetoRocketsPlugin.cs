
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using ULTRADROID.Models;
using ULTRADROID.Sound;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    [BepInPlugin(
        "somerandommaid.ultradroid.tetorockets",
        "ULTRADROID Teto Rockets",
        "0.3.0"
    )]
    [BepInDependency("somerandommaid.ultradroid.models")]
    public class TetoRocketsPlugin : BasePlugin
    {
        internal static ManualLogSource PluginLog;

        internal static ModelOwner ModelOwner;

        internal static string ModelPath;
        internal static string SoundPath;

        internal static bool TetoSoundReady;

        public override void Load()
        {
            PluginLog = base.Log;

            PluginLog.LogInfo("Initializing Teto Rockets...");

            string pluginFolder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            );

            ModelPath = Path.Combine(
                pluginFolder,
                "assets",
                "Teto.glb"
            );

            SoundPath = Path.Combine(
                pluginFolder,
                "assets",
                "Teto.wav"
            );

            if (!File.Exists(ModelPath))
            {
                PluginLog.LogError("Teto.glb missing: " + ModelPath);
                return;
            }

            if (!File.Exists(SoundPath))
            {
                PluginLog.LogError("Teto.wav missing: " + SoundPath);
                return;
            }

            ModelOwner = ModelAPI.CreateOwner(
                "somerandommaid.ultradroid.tetorockets"
            );

            InitializeSound();

            ClassInjector.RegisterTypeInIl2Cpp<RocketDetector>();

            GameObject runtime = new GameObject(
                "ULTRADROID_Teto_Rockets_Runtime"
            );

            UnityEngine.Object.DontDestroyOnLoad(runtime);

            runtime.AddComponent<RocketDetector>();

            PluginLog.LogInfo("Teto Rockets initialized.");
            PluginLog.LogInfo("Model: " + ModelPath);
            PluginLog.LogInfo("Sound: " + SoundPath);
        }

        private void InitializeSound()
        {
            try
            {
                SoundAPI.SetLogger(
                    message => PluginLog.LogInfo("[Sound API] " + message),
                    message => PluginLog.LogError("[Sound API] " + message)
                );

                SoundAPI.Initialize();

                PluginLog.LogInfo("Sound API initialized.");

                SoundAPI.Load(SoundPath, (clip, error) =>
                {
                    if (!string.IsNullOrEmpty(error) || clip == null)
                    {
                        PluginLog.LogError(
                            "Could not preload Teto.wav: " + error
                        );

                        return;
                    }

                    TetoSoundReady = true;

                    PluginLog.LogInfo(
                        "Teto.wav loaded successfully. Length: " +
                        clip.length + " seconds."
                    );
                });
            }
            catch (System.Exception e)
            {
                PluginLog.LogError(
                    "Sound initialization failed: " + e
                );
            }
        }

        internal static void PlayTetoSound()
        {
            if (!TetoSoundReady)
            {
                PluginLog.LogWarning(
                    "Teto sound is not ready yet."
                );

                return;
            }

            try
            {
                SoundHandle handle = SoundAPI.Play(
                    SoundPath,
                    new SoundOptions
                    {
                        Volume = 1f,
                        Pitch = 1f,
                        Loop = false,
                        PlayOnLoad = true
                    }
                );

                if (handle == null)
                {
                    PluginLog.LogError(
                        "SoundAPI.Play returned null."
                    );
                    return;
                }

                PluginLog.LogInfo("Played Teto.wav!");
            }
            catch (System.Exception e)
            {
                PluginLog.LogError(
                    "Could not play Teto.wav: " + e
                );
            }
        }
    }
}
