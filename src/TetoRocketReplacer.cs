
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
                    "[Rocket] Registered rocket for Teto replacement: " +
                    grenade.gameObject.name
                );

                TrySpawnReplacement(id, replacement);
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Rocket] ProcessRocket exception: " + ex
                );
            }
        }

        internal static void OnTemplateReady()
        {
            if (!TetoRocketsPlugin.GameplayActive)
                return;

            TetoRocketsPlugin.PluginLog.LogInfo(
                "[Rocket] Teto template ready. Processing queued rockets."
            );

            List<KeyValuePair<int, RocketReplacement>> pending =
                new List<KeyValuePair<int, RocketReplacement>>();

            foreach (KeyValuePair<int, RocketReplacement> pair in replacements)
            {
                if (pair.Value.Loading && !pair.Value.Replaced)
                    pending.Add(pair);
            }

            foreach (KeyValuePair<int, RocketReplacement> pair in pending)
                TrySpawnReplacement(pair.Key, pair.Value);
        }

        private static void TrySpawnReplacement(
            int id,
            RocketReplacement replacement)
        {
            try
            {
                if (!TetoRocketsPlugin.GameplayActive)
                    return;

                if (replacement == null ||
                    replacement.Grenade == null ||
                    replacement.ModelTransform == null)
                {
                    replacements.Remove(id);
                    return;
                }

                ModelInstance template = TetoRocketsPlugin.TetoTemplate;

                if (template == null ||
                    !template.IsValid ||
                    template.Root == null)
                {
                    // Keep the rocket queued until the template is ready.
                    replacement.Loading = true;
                    return;
                }

                ModelLoadOptions options = new ModelLoadOptions
                {
                    Name = "TetoRocket_" + id,
                    Position = Vector3.zero,
                    RotationEuler = new Vector3(180f, 180f, 0f),
                    Scale = new Vector3(0.5f, 0.5f,0.5f),
                    LoadTextures = true,
                    CreateColliders = false,
                    FlipUvV = true
                };

                ModelInstance instance =
                    TetoRocketsPlugin.ModelOwner.InstantiateModel(
                        template,
                        options
                    );

                if (instance == null ||
                    !instance.IsValid ||
                    instance.Root == null)
                {
                    if (instance != null)
                        instance.Destroy();

                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] Models API returned an invalid Teto instance."
                    );

                    replacement.Loading = false;
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
                instance.SetLocalRotation(new Vector3(0f, 180f, 0f));
                instance.SetScale(Vector3.one);

                // The template's renderers are disabled to keep it invisible.
                // Enable the renderers on this independent rocket instance.
                Renderer[] renderers =
                    instance.Root.GetComponentsInChildren<Renderer>(true);

                int meshRendererCount = 0;
                int skinnedRendererCount = 0;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    renderer.enabled = true;

                    if (renderer is MeshRenderer)
                        meshRendererCount++;

                    if (renderer is SkinnedMeshRenderer)
                        skinnedRendererCount++;
                }

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket] Spawned Teto instance. MeshRenderers: " +
                    meshRendererCount +
                    ", SkinnedMeshRenderers: " +
                    skinnedRendererCount
                );

                if (renderers.Length == 0)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] Teto instance contains no renderers."
                    );

                    instance.Destroy();
                    replacements.Remove(id);
                    return;
                }

                replacement.ModelInstance = instance;
                replacement.Loading = false;
                replacement.Replaced = true;

                // Hide the original rocket model only after Teto is ready.
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
                    "[Rocket] TrySpawnReplacement exception: " + ex
                );

                replacement.Loading = false;
                replacements.Remove(id);
            }
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
                {
                    replacement.ModelInstance.Destroy();
                    replacement.ModelInstance = null;
                }

                if (replacement.OriginalRenderer != null)
                    replacement.OriginalRenderer.enabled = true;

                replacement.Replaced = false;
                replacement.Loading = false;
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
