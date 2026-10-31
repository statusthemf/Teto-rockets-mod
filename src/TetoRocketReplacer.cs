using System;
using System.Collections.Generic;

using ULTRADROID.Models;

using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    internal static class TetoRocketReplacer
    {
        private sealed class RocketReplacement
        {
            public Grenade Grenade;
            public Transform ModelTransform;
            public MeshRenderer OriginalRenderer;
            public ModelInstance ModelInstance;

            public bool Loading;
            public bool Replaced;
        }

        private static readonly Dictionary<int, RocketReplacement>
            replacements = new Dictionary<int, RocketReplacement>();

        internal static void ProcessRocket(Grenade grenade)
        {
            if (!TetoRocketsPlugin.GameplayActive)
                return;

            if (grenade == null)
                return;

            try
            {
                int id = grenade.gameObject.GetInstanceID();

                if (replacements.ContainsKey(id))
                    return;

                Transform modelTransform =
                    grenade.transform.Find("Model");

                if (modelTransform == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "[Rocket] No Model child found on " +
                        grenade.gameObject.name
                    );

                    return;
                }

                MeshRenderer originalRenderer =
                    modelTransform.GetComponent<MeshRenderer>();

                if (originalRenderer == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "[Rocket] No original MeshRenderer found on " +
                        grenade.gameObject.name
                    );

                    return;
                }

                RocketReplacement replacement =
                    new RocketReplacement
                    {
                        Grenade = grenade,
                        ModelTransform = modelTransform,
                        OriginalRenderer = originalRenderer,
                        Loading = true,
                        Replaced = false
                    };

                replacements.Add(id, replacement);

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket] Starting Teto GLB load for " +
                    grenade.gameObject.name
                );

                ModelLoadOptions options = new ModelLoadOptions
                {
                    Name = "TetoRocket",
                    Position = Vector3.zero,
                    RotationEuler = Vector3.zero,
                    Scale = Vector3.one,
                    LoadTextures = true,
                    CreateColliders = false,
                    FlipUvV = true
                };

                ModelAPI.LoadGLBAsync(
                    TetoRocketsPlugin.ModelPath,

                    instance =>
                    {
                        OnModelLoaded(id, replacement, instance);
                    },

                    exception =>
                    {
                        OnModelLoadFailed(id, exception);
                    },

                    options,

                    TetoRocketsPlugin.ModelOwner
                );
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Rocket] ProcessRocket exception: " + ex
                );
            }
        }

        private static void OnModelLoaded(
            int id,
            RocketReplacement replacement,
            ModelInstance instance)
        {
            try
            {
                if (!replacements.ContainsKey(id))
                {
                    if (instance != null)
                        instance.Destroy();

                    return;
                }

                if (instance == null ||
                    !instance.IsValid ||
                    instance.Root == null)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] Model API returned an invalid model."
                    );

                    replacements.Remove(id);
                    return;
                }

                if (!TetoRocketsPlugin.GameplayActive ||
                    replacement.Grenade == null ||
                    replacement.ModelTransform == null)
                {
                    instance.Destroy();
                    replacements.Remove(id);
                    return;
                }

                instance.SetParent(
                    replacement.ModelTransform,
                    false
                );

                instance.SetLocalPosition(Vector3.zero);
                instance.SetLocalRotation(Vector3.zero);
                instance.SetScale(Vector3.one);

                int meshRendererCount =
                    instance.Root.GetComponentsInChildren<MeshRenderer>(true).Length;

                int skinnedRendererCount =
                    instance.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket] Teto model loaded. MeshRenderers: " +
                    meshRendererCount +
                    ", SkinnedMeshRenderers: " +
                    skinnedRendererCount
                );

                if (meshRendererCount == 0 &&
                    skinnedRendererCount == 0)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] Loaded GLB contains no visible renderers."
                    );

                    instance.Destroy();
                    replacements.Remove(id);
                    return;
                }

                replacement.ModelInstance = instance;
                replacement.Loading = false;
                replacement.Replaced = true;

                // Hide the original only after the new model is ready.
                if (replacement.OriginalRenderer != null)
                    replacement.OriginalRenderer.enabled = false;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket] Rocket successfully replaced with Teto."
                );

                TetoRocketsPlugin.PlayTetoSound();
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Rocket] OnModelLoaded exception: " + ex
                );

                if (instance != null)
                    instance.Destroy();

                replacements.Remove(id);
            }
        }

        private static void OnModelLoadFailed(
            int id,
            Exception exception)
        {
            TetoRocketsPlugin.PluginLog.LogError(
                "[Rocket] GLB load failed: " + exception
            );

            replacements.Remove(id);
        }

        internal static void CleanupDestroyedRockets()
        {
            List<int> remove = new List<int>();

            foreach (KeyValuePair<int, RocketReplacement> pair in replacements)
            {
                RocketReplacement replacement = pair.Value;

                if (replacement.Grenade == null)
                {
                    DestroyReplacement(replacement);
                    remove.Add(pair.Key);
                }
            }

            foreach (int id in remove)
                replacements.Remove(id);
        }

        internal static void CleanupAll()
        {
            foreach (RocketReplacement replacement in replacements.Values)
                DestroyReplacement(replacement);

            replacements.Clear();

            TetoRocketsPlugin.PluginLog.LogInfo(
                "[Rocket] All replacement models cleaned up."
            );
        }

        private static void DestroyReplacement(
            RocketReplacement replacement)
        {
            try
            {
                if (replacement.ModelInstance != null)
                    replacement.ModelInstance.Destroy();

                if (replacement.OriginalRenderer != null)
                    replacement.OriginalRenderer.enabled = true;
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "[Rocket] Cleanup exception: " + ex.Message
                );
            }
        }
    }
}
