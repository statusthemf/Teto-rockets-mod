using System;
using System.Collections.Generic;
using ULTRADROID.Models;
using UnityEngine;

namespace ULTRADROID.TetoRockets
{
    public static class TetoRocketReplacer
    {
        private sealed class RocketReplacement
        {
            public Grenade Grenade;
            public Transform ModelTransform;
            public MeshRenderer OriginalRenderer;
            public ModelInstance TetoModel;

            public bool Loading;
            public bool Replaced;
        }

        private static readonly Dictionary<int, RocketReplacement>
            Replacements =
                new Dictionary<int, RocketReplacement>();

        public static void ProcessRocket(Grenade grenade)
        {
            if (grenade == null)
                return;

            int instanceId = grenade.gameObject.GetInstanceID();

            if (Replacements.ContainsKey(instanceId))
                return;

            try
            {
                Transform modelTransform =
                    grenade.transform.Find("Model");

                if (modelTransform == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "Rocket has no Model child: " +
                        grenade.gameObject.name
                    );
                    return;
                }

                MeshRenderer originalRenderer =
                    modelTransform.GetComponent<MeshRenderer>();

                if (originalRenderer == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "Rocket Model has no MeshRenderer: " +
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

                Replacements.Add(instanceId, replacement);

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Starting Teto GLB load for rocket " +
                    instanceId
                );

                // Do not pass a Unity Transform as Parent here.
                // The asynchronous API prepares the GLB on a worker task.
                ModelLoadOptions options = new ModelLoadOptions
                {
                    Name = "Teto_Rocket_" + instanceId,
                    Position = Vector3.zero,
                    RotationEuler = Vector3.zero,
                    Scale = Vector3.one,
                    LoadTextures = true,
                    CreateColliders = false,
                    FlipUvV = false
                };

                ModelAPI.LoadGLBAsync(
                    TetoRocketsPlugin.ModelPath,

                    instance =>
                    {
                        OnModelLoaded(
                            instanceId,
                            replacement,
                            instance
                        );
                    },

                    exception =>
                    {
                        OnModelLoadFailed(
                            instanceId,
                            exception
                        );
                    },

                    options,

                    TetoRocketsPlugin.ModelOwner
                );
            }
            catch (Exception exception)
            {
                Replacements.Remove(instanceId);

                TetoRocketsPlugin.PluginLog.LogError(
                    "ProcessRocket exception: " + exception
                );
            }
        }

        private static void OnModelLoaded(
            int instanceId,
            RocketReplacement replacement,
            ModelInstance instance)
        {
            try
            {
                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Models API completed GLB load for rocket " +
                    instanceId
                );

                if (instance == null)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "Models API returned a null ModelInstance."
                    );

                    Replacements.Remove(instanceId);
                    return;
                }

                if (!instance.IsValid || instance.Root == null)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "Models API returned an invalid model root."
                    );

                    instance.Destroy();
                    Replacements.Remove(instanceId);
                    return;
                }

                if (!Replacements.ContainsKey(instanceId) ||
                    replacement.Grenade == null ||
                    replacement.ModelTransform == null)
                {
                    TetoRocketsPlugin.PluginLog.LogWarning(
                        "Rocket disappeared before Teto finished loading."
                    );

                    instance.Destroy();
                    Replacements.Remove(instanceId);
                    return;
                }

                // Attach the model after asynchronous loading.
                instance.SetParent(
                    replacement.ModelTransform,
                    false
                );

                // Explicit local transform.
                instance.SetLocalPosition(Vector3.zero);
                instance.SetLocalRotation(Vector3.zero);
                instance.SetScale(Vector3.one);

                // Count renderers so we can verify that the GLB
                // actually contains visible geometry.
                MeshRenderer[] meshRenderers =
                    instance.Root.GetComponentsInChildren<MeshRenderer>(
                        true
                    );

                SkinnedMeshRenderer[] skinnedRenderers =
                    instance.Root.GetComponentsInChildren<SkinnedMeshRenderer>(
                        true
                    );

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Teto model root: " + instance.Root.name +
                    " | MeshRenderers: " + meshRenderers.Length +
                    " | SkinnedMeshRenderers: " +
                    skinnedRenderers.Length
                );

                if (meshRenderers.Length == 0 &&
                    skinnedRenderers.Length == 0)
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "Teto GLB loaded, but it contains no visible " +
                        "MeshRenderer or SkinnedMeshRenderer components."
                    );

                    instance.Destroy();
                    Replacements.Remove(instanceId);
                    return;
                }

                replacement.TetoModel = instance;
                replacement.Loading = false;
                replacement.Replaced = true;

                // Only hide the original rocket mesh after the
                // replacement has loaded and has renderers.
                if (replacement.OriginalRenderer != null)
                {
                    replacement.OriginalRenderer.enabled = false;
                }

                TetoRocketsPlugin.PluginLog.LogInfo(
                    "Teto rocket replacement successful: " +
                    instanceId
                );

                TetoRocketsPlugin.PlayTetoSound();
            }
            catch (Exception exception)
            {
                TetoRocketsPlugin.PluginLog.LogError(
                    "OnModelLoaded exception: " + exception
                );

                if (instance != null)
                    instance.Destroy();

                Replacements.Remove(instanceId);
            }
        }

        private static void OnModelLoadFailed(
            int instanceId,
            Exception exception)
        {
            TetoRocketsPlugin.PluginLog.LogError(
                "GLB load failed for rocket " + instanceId +
                ": " + exception
            );

            Replacements.Remove(instanceId);
        }

        public static void CleanupDestroyedRockets()
        {
            List<int> removeIds = new List<int>();

            foreach (
                KeyValuePair<int, RocketReplacement> pair
                in Replacements
            )
            {
                RocketReplacement replacement = pair.Value;

                if (replacement.Grenade != null)
                    continue;

                if (replacement.TetoModel != null)
                {
                    replacement.TetoModel.Destroy();
                }

                removeIds.Add(pair.Key);
            }

            foreach (int id in removeIds)
            {
                Replacements.Remove(id);
            }
        }
    }
}
