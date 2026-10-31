
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
            if (!TetoRocketsPlugin.GameplayActive || grenade == null)
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
                    "[Rocket] Registered rocket: " +
                    grenade.gameObject.name +
                    " | ID: " + id
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
                "[Rocket] Teto template ready. Processing pending rockets."
            );

            List<KeyValuePair<int, RocketReplacement>> pending =
                new List<KeyValuePair<int, RocketReplacement>>();

            foreach (KeyValuePair<int, RocketReplacement> pair in replacements)
            {
                if (pair.Value != null &&
                    pair.Value.Loading &&
                    !pair.Value.Replaced)
                {
                    pending.Add(pair);
                }
            }

            foreach (KeyValuePair<int, RocketReplacement> pair in pending)
            {
                TrySpawnReplacement(pair.Key, pair.Value);
            }
        }

        private static void TrySpawnReplacement(
            int id,
            RocketReplacement replacement)
        {
            if (replacement == null)
                return;

            if (replacement.Replaced || !replacement.Loading)
                return;

            try
            {
                if (!TetoRocketsPlugin.GameplayActive)
                    return;

                if (replacement.Grenade == null ||
                    replacement.ModelTransform == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "[Rocket Debug] Rocket " + id +
                        " was destroyed before its Teto instance spawned."
                    );

                    replacements.Remove(id);
                    return;
                }

                ModelInstance template = TetoRocketsPlugin.TetoTemplate;

                bool templateExists = template != null;
                bool templateValid =
                    templateExists && template.IsValid;
                bool templateHasRoot =
                    templateExists && template.Root != null;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket Debug] Template status for rocket " + id +
                    " | Exists: " + templateExists +
                    " | Valid: " + templateValid +
                    " | HasRoot: " + templateHasRoot
                );

                if (!templateExists ||
                    !templateValid ||
                    !templateHasRoot)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "[Rocket Debug] Template is not ready. " +
                        "Rocket " + id + " remains queued."
                    );

                    return;
                }

                if (TetoRocketsPlugin.ModelOwner == null ||
                    TetoRocketsPlugin.ModelOwner.IsDisposed)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] ModelOwner is unavailable or disposed."
                    );

                    replacement.Loading = false;
                    replacements.Remove(id);
                    return;
                }

                ModelLoadOptions options = new ModelLoadOptions
                {
                    Name = "TetoRocket_" + id,
                    Position = Vector3.zero,
                    RotationEuler = new Vector3(180f, 180f, 0f),
                    Scale = new Vector3(0.5f, 0.5f, 0.5f),
                    LoadTextures = true,
                    CreateColliders = false,
                    FlipUvV = true
                };

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket Debug] Calling InstantiateModel for rocket " +
                    id + "."
                );

                ModelInstance instance =
                    TetoRocketsPlugin.ModelOwner.InstantiateModel(
                        template,
                        options
                    );

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket Debug] InstantiateModel returned. Instance exists: " +
                    (instance != null)
                );

                if (instance == null ||
                    !instance.IsValid ||
                    instance.Root == null)
                {
                    if (instance != null)
                        instance.Destroy();

                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] InstantiateModel returned an invalid instance for rocket " +
                        id + "."
                    );

                    replacement.Loading = false;
                    replacements.Remove(id);
                    return;
                }

                // Check again because the rocket could have disappeared
                // while the instance was being created.
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
                instance.SetLocalRotation(
                    new Vector3(0f, 180f, 0f)
                );

                // The source template was imported at scale 0.5.
                // The clone keeps that imported scale.
                instance.SetScale(Vector3.one);

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
                    "[Rocket] Teto instance renderer count for rocket " +
                    id +
                    " | MeshRenderers: " + meshRendererCount +
                    " | SkinnedMeshRenderers: " + skinnedRendererCount
                );

                if (meshRendererCount == 0 &&
                    skinnedRendererCount == 0)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "[Rocket] Teto instance has no MeshRenderer or SkinnedMeshRenderer."
                    );

                    instance.Destroy();
                    replacement.Loading = false;
                    replacements.Remove(id);
                    return;
                }

                replacement.ModelInstance = instance;
                replacement.Loading = false;
                replacement.Replaced = true;

                // Hide the original rocket model only after the
                // independent Teto instance is ready.
                if (replacement.OriginalRenderer != null)
                    replacement.OriginalRenderer.enabled = false;

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "[Rocket] Rocket successfully replaced with Teto. ID: " +
                    id
                );

                TetoRocketsPlugin.PlayTetoSound();
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "[Rocket] TrySpawnReplacement exception for rocket " +
                    id + ": " + ex
                );

                replacement.Loading = false;

                if (replacement.ModelInstance != null)
                {
                    replacement.ModelInstance.Destroy();
                    replacement.ModelInstance = null;
                }

                if (replacement.OriginalRenderer != null)
                    replacement.OriginalRenderer.enabled = true;

                replacements.Remove(id);
            }
        }

        internal static void CleanupDestroyedRockets()
        {
            List<int> remove = new List<int>();

            foreach (KeyValuePair<int, RocketReplacement> pair in replacements)
            {
                RocketReplacement replacement = pair.Value;

                if (replacement == null ||
                    replacement.Grenade == null)
                {
                    if (replacement != null)
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
            {
                if (replacement != null)
                    DestroyReplacement(replacement);
            }

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

                replacement.Loading = false;
                replacement.Replaced = false;
            }
            catch (Exception ex)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "[Rocket] Cleanup exception: " + ex
                );
            }
        }
    }
}
