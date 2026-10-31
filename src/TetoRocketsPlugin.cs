
using System;
using System.IO;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;

using ULTRADROID.Models;
using ULTRADROID.Sound;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        internal static ModelInstance TetoTemplate;

        internal static string ModelPath;
        internal static string SoundPath;

        internal static bool GameplayActive;
        internal static bool SoundInitialized;
        internal static bool TetoSoundReady;

        private static bool detectorTypeRegistered;
        private static bool runtimeCreated;
        private static bool sceneEventRegistered;

        private static bool templateLoading;
        private static int templateLoadGeneration;

        private static UnityAction<Scene, LoadSceneMode> sceneLoadedHandler;

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

            ModelOwner = ModelAPI.CreateOwner(
                "somerandommaid.ultradroid.tetorockets"
            );

            ClassInjector.RegisterTypeInIl2Cpp<RocketDetector>();
            detectorTypeRegistered = true;

            RegisterSceneEvent();

            PluginLog.LogInfo(
                "Initialization complete. Waiting for the first scene to load."
            );
        }

        private static void RegisterSceneEvent()
        {
            if (sceneEventRegistered)
                return;

            try
            {
                sceneLoadedHandler =
                    DelegateSupport.ConvertDelegate<
                        UnityAction<Scene, LoadSceneMode>
                    >(
                        new Action<Scene, LoadSceneMode>(OnSceneLoaded)
                    );

                SceneManager.sceneLoaded += sceneLoadedHandler;
                sceneEventRegistered = true;

                PluginLog.LogInfo(
                    "[Runtime] SceneLoaded callback registered successfully."
                );
            }
            catch (Exception ex)
            {
                PluginLog.LogError(
                    "[Runtime] SceneLoaded registration failed: " + ex
                );
            }
        }

        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            PluginLog.LogInfo(
                "[Runtime] Scene loaded: " + scene.name +
                " | Mode: " + mode
            );

            if (runtimeCreated)
                return;

            CreateRuntime();
        }

        private static void CreateRuntime()
        {
            if (runtimeCreated || !detectorTypeRegistered)
                return;

            try
            {
                GameObject runtime = new GameObject(
                    "ULTRADROID_Teto_Rockets_Runtime"
                );

                UnityEngine.Object.DontDestroyOnLoad(runtime);

                RocketDetector detector =
                    runtime.AddComponent<RocketDetector>();

                if (detector == null)
                {
                    PluginLog.LogError(
                        "[Runtime] Failed to add RocketDetector."
                    );

                    UnityEngine.Object.Destroy(runtime);
                    return;
                }

                runtimeCreated = true;

                PluginLog.LogInfo(
                    "[Runtime] Persistent runtime created successfully."
                );

                PluginLog.LogInfo(
                    "[Runtime] Runtime scene: " + runtime.scene.name
                );
            }
            catch (Exception ex)
            {
                PluginLog.LogError(
                    "[Runtime] Creation failed: " + ex
                );
            }
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
            PreloadTetoTemplate();
        }

        internal static void OnGameplayStopped()
        {
            if (!GameplayActive)
                return;

            GameplayActive = false;

            templateLoadGeneration++;
            templateLoading = false;

            PluginLog.LogInfo(
                "[Gameplay] Gameplay ended. Cleaning up."
            );

            TetoRocketReplacer.CleanupAll();

            if (TetoTemplate != null)
            {
                TetoTemplate.Destroy();
                TetoTemplate = null;
            }
        }

        private static void PreloadTetoTemplate()
        {
            if (!GameplayActive)
                return;

            if (TetoTemplate != null && TetoTemplate.IsValid)
            {
                TetoRocketReplacer.OnTemplateReady();
                return;
            }

            if (templateLoading)
                return;

            if (ModelOwner == null || ModelOwner.IsDisposed)
            {
                PluginLog.LogError(
                    "[Model] Cannot preload Teto: ModelOwner is unavailable."
                );

                return;
            }

            templateLoading = true;

            int generation = ++templateLoadGeneration;

            ModelLoadOptions options = new ModelLoadOptions
            {
                Name = "ULTRADROID_Teto_Template",
                Position = Vector3.zero,
                RotationEuler = Vector3.zero,
                Scale = new Vector3(0.5f, 0.5f, 0.5f),
                LoadTextures = true,
                CreateColliders = false,
                FlipUvV = true
            };

            try
            {
                ModelAPI.LoadGLBAsync(
                    ModelPath,

                    instance =>
                    {
                        templateLoading = false;

                        if (generation != templateLoadGeneration ||
                            !GameplayActive)
                        {
                            if (instance != null)
                                instance.Destroy();

                            return;
                        }

                        if (instance == null ||
                            !instance.IsValid ||
                            instance.Root == null)
                        {
                            if (instance != null)
                                instance.Destroy();

                            PluginLog.LogError(
                                "[Model] Teto template load returned an invalid instance."
                            );

                            return;
                        }

                        TetoTemplate = instance;

                        UnityEngine.Object.DontDestroyOnLoad(
                            TetoTemplate.Root
                        );

                        Renderer[] renderers =
                            TetoTemplate.Root.GetComponentsInChildren<Renderer>(true);

                        foreach (Renderer renderer in renderers)
                        {
                            if (renderer != null)
                                renderer.enabled = false;
                        }

                        TetoRocketReplacer.OnTemplateReady();
                    },

                    exception =>
                    {
                        templateLoading = false;

                        if (generation != templateLoadGeneration ||
                            !GameplayActive)
                            return;

                        PluginLog.LogError(
                            "[Model] Failed to preload Teto.glb: " + exception
                        );
                    },

                    options,

                    ModelOwner
                );
            }
            catch (Exception ex)
            {
                templateLoading = false;

                PluginLog.LogError(
                    "[Model] Template preload exception: " + ex
                );
            }
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
                    message => PluginLog.LogInfo(
                        "[Sound API] " + message
                    ),
                    message => PluginLog.LogError(
                        "[Sound API] " + message
                    )
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
                                "[Sound] Failed to load Teto.wav: " +
                                error
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
                        "[Sound] SoundAPI.Play returned NULL!"
                    );
                }
                else
                {
                    PluginLog.LogInfo(
                        "[Sound] Playback handle created. IsPlaying: " +
                        handle.IsPlaying
                    );
                }
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
