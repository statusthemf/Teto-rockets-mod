
using System;
using System.IO;

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
        "0.5.0"
    )]
    [BepInDependency("somerandommaid.ultradroid.models")]
    public class TetoRocketsPlugin : BasePlugin
    {
        internal static ManualLogSource PluginLog;

        internal static ModelOwner ModelOwner;

        internal static string ModelPath;
        internal static string SoundPath;

        internal static bool GameplayActive;
        internal static bool SoundInitialized;
        internal static bool TetoSoundReady;

        public override void Load()
        {
            PluginLog = Log;

            PluginLog.LogInfo("Initializing Teto Rockets 0.5.0...");

            string pluginDirectory = Path.GetDirectoryName(
                typeof(TetoRocketsPlugin).Assembly.Location
            );

            string assetDirectory = Path.Combine(
                pluginDirectory,
                "assets"
            );

            ModelPath = Path.Combine(assetDirectory, "Teto.glb");
            SoundPath = Path.Combine(assetDirectory, "Teto.wav");

            if (!File.Exists(ModelPath))
            {
                PluginLog.LogError("Teto.glb not found: " + ModelPath);
                return;
            }

            if (!File.Exists(SoundPath))
            {
                PluginLog.LogError("Teto.wav not found: " + SoundPath);
                return;
            }

            PluginLog.LogInfo(
                "Teto.glb size: " + new FileInfo(ModelPath).Length + " bytes"
            );

            PluginLog.LogInfo(
                "Teto.wav size: " + new FileInfo(SoundPath).Length + " bytes"
            );

            ModelOwner = ModelAPI.CreateOwner(
                "somerandommaid.ultradroid.tetorockets"
            );

            ClassInjector.RegisterTypeInIl2Cpp<RocketDetector>();

            GameObject runtime = new GameObject(
                "ULTRADROID_Teto_Rockets_Runtime"
            );

            UnityEngine.Object.DontDestroyOnLoad(runtime);
            runtime.AddComponent<RocketDetector>();

            PluginLog.LogInfo(
                "Runtime created. Waiting for gameplay before initializing systems."
            );
        }

        internal static void OnGameplayStarted()
        {
            if (GameplayActive)
                return;

            GameplayActive = true;

            PluginLog.LogInfo(
                "[Gameplay] Gameplay detected. Starting Teto systems."
            );

            InitializeSound();
        }

        internal static void OnGameplayStopped()
        {
            if (!GameplayActive)
                return;

            GameplayActive = false;

            PluginLog.LogInfo(
                "[Gameplay] Gameplay ended. Stopping rocket detection and cleaning up."
            );

            TetoRocketReplacer.CleanupAll();
        }

        private static void InitializeSound()
        {
            if (SoundInitialized && SoundAPI.IsRuntimeAvailable)
            {
                PluginLog.LogInfo(
                    "[Sound] Sound API already initialized."
                );

                return;
            }

            SoundInitialized = true;
            TetoSoundReady = false;

            try
            {
                SoundAPI.SetLogger(
                    message => PluginLog.LogInfo("[Sound API] " + message),
                    message => PluginLog.LogError("[Sound API] " + message)
                );

                if (!SoundAPI.IsRuntimeAvailable)
                    SoundAPI.Initialize();

                PluginLog.LogInfo(
                    "[Sound] Runtime available: " +
                    SoundAPI.IsRuntimeAvailable
                );

                SoundAPI.Load(
                    SoundPath,
                    (clip, error) =>
                    {
                        if (clip == null)
                        {
                            TetoSoundReady = false;

                            PluginLog.LogError(
                                "[Sound] Failed to load Teto.wav: " + error
                            );

                            return;
                        }

                        TetoSoundReady = true;

                        PluginLog.LogInfo(
                            "[Sound] Teto.wav loaded successfully."
                        );
                    }
                );
            }
            catch (Exception ex)
            {
                PluginLog.LogError(
                    "[Sound] Initialization exception: " + ex
                );
            }
        }

        internal static void PlayTetoSound()
        {
            if (!GameplayActive || !TetoSoundReady)
                return;

            try
            {
                if (!SoundAPI.IsRuntimeAvailable)
                {
                    PluginLog.LogWarning(
                        "[Sound] Runtime unavailable. Not playing sound."
                    );

                    return;
                }

                SoundAPI.Play(
                    SoundPath,
                    new SoundOptions
                    {
                        Volume = 1f,
                        Pitch = 1f,
                        Loop = false,
                        PlayOnLoad = true
                    }
                );
            }
            catch (Exception ex)
            {
                PluginLog.LogError(
                    "[Sound] Playback exception: " + ex
                );
            }
        }
    }
}
