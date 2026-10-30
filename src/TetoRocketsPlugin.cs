using System;
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
        "0.4.0"
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
            PluginLog = Log;

            PluginLog.LogInfo("Initializing Teto Rockets 0.4.0...");

            try
            {
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

                PluginLog.LogInfo("Model path: " + ModelPath);
                PluginLog.LogInfo("Sound path: " + SoundPath);

                if (!File.Exists(ModelPath))
                {
                    PluginLog.LogError(
                        "Teto.glb does not exist: " + ModelPath
                    );
                    return;
                }

                if (!File.Exists(SoundPath))
                {
                    PluginLog.LogError(
                        "Teto.wav does not exist: " + SoundPath
                    );
                    return;
                }

                PluginLog.LogInfo(
                    "Teto.glb size: " +
                    new FileInfo(ModelPath).Length + " bytes"
                );

                PluginLog.LogInfo(
                    "Teto.wav size: " +
                    new FileInfo(SoundPath).Length + " bytes"
                );

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

                PluginLog.LogInfo(
                    "Rocket detector runtime created."
                );

                PluginLog.LogInfo(
                    "Teto Rockets initialization complete."
                );
            }
            catch (Exception exception)
            {
                PluginLog.LogError(
                    "Plugin initialization failed: " + exception
                );
            }
        }

        private void InitializeSound()
        {
            try
            {
                SoundAPI.SetLogger(
                    message =>
                        PluginLog.LogInfo("[Sound API] " + message),
                    message =>
                        PluginLog.LogError("[Sound API] " + message)
                );

                SoundAPI.Initialize();

                PluginLog.LogInfo(
                    "Sound API initialized. Runtime available: " +
                    SoundAPI.IsRuntimeAvailable
                );

                SoundAPI.Load(
                    SoundPath,
                    (clip, error) =>
                    {
                        if (!string.IsNullOrEmpty(error) || clip == null)
                        {
                            TetoSoundReady = false;

                            PluginLog.LogError(
                                "Teto.wav loading failed: " + error
                            );

                            return;
                        }

                        TetoSoundReady = true;

                        PluginLog.LogInfo(
                            "Teto.wav loaded. Length: " +
                            clip.length + " seconds."
                        );
                    }
                );
            }
            catch (Exception exception)
            {
                PluginLog.LogError(
                    "Sound initialization exception: " + exception
                );
            }
        }

        internal static void RecoverSoundRuntime()
        {
            try
            {
                if (SoundAPI.IsRuntimeAvailable)
                    return;

                PluginLog.LogWarning(
                    "Sound runtime unavailable. Attempting recovery."
                );

                SoundAPI.Initialize();

                PluginLog.LogInfo(
                    "Sound runtime recovery attempted. Available: " +
                    SoundAPI.IsRuntimeAvailable
                );
            }
            catch (Exception exception)
            {
                PluginLog.LogError(
                    "Sound runtime recovery failed: " + exception
                );
            }
        }

        internal static void PlayTetoSound()
        {
            if (!TetoSoundReady)
            {
                PluginLog.LogWarning(
                    "Cannot play Teto.wav: audio is not loaded."
                );
                return;
            }

            try
            {
                if (!SoundAPI.IsRuntimeAvailable)
                {
                    RecoverSoundRuntime();
                }

                if (!SoundAPI.IsRuntimeAvailable)
                {
                    PluginLog.LogWarning(
                        "Cannot play Teto.wav: runtime is unavailable."
                    );
                    return;
                }

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

                PluginLog.LogInfo("Played Teto.wav.");
            }
            catch (Exception exception)
            {
                PluginLog.LogError(
                    "Teto.wav playback failed: " + exception
                );
            }
        }
    }
}
