
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

            Transform rocketRoot = grenade.transform;

            Transform modelTransform =
                rocketRoot.Find("Model");

            if (modelTransform == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Could not find Model child on " +
                    rocketRoot.name
                );

                return;
            }

            MeshRenderer originalRenderer =
                modelTransform.GetComponent<MeshRenderer>();

            if (originalRenderer == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Could not find original MeshRenderer on " +
                    rocketRoot.name
                );

                return;
            }

            RocketReplacement replacement =
                new RocketReplacement
                {
                    Grenade = grenade,
                    ModelTransform = modelTransform,
                    OriginalRenderer = originalRenderer,
                    TetoModel = null
                };

            Replacements.Add(instanceId, replacement);

            TetoRocketsPlugin.PluginLog.LogInfo(
                "Loading Teto for rocket: " + rocketRoot.name
            );

            ModelLoadOptions options = new ModelLoadOptions
            {
                Name = "Teto_Rocket_Visual",
                Parent = modelTransform,
                Position = Vector3.zero,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one,
                LoadTextures = true,
                CreateColliders = false
            };

            ModelAPI.LoadGLBAsync(
                TetoRocketsPlugin.ModelPath,

                instance =>
                {
                    if (instance == null || !instance.IsValid)
                    {
                        TetoRocketsPlugin.PluginLog.LogError(
                            "Teto model loading returned an invalid instance."
                        );

                        Replacements.Remove(instanceId);
                        return;
                    }

                    if (!Replacements.ContainsKey(instanceId) ||
                        grenade == null ||
                        modelTransform == null)
                    {
                        instance.Destroy();
                        Replacements.Remove(instanceId);
                        return;
                    }

                    replacement.TetoModel = instance;

                    // Ensure the imported model stays attached
                    // to the rocket's visual transform.
                    instance.SetParent(modelTransform, false);

                    // Hide the original rocket only after
                    // the replacement has loaded successfully.
                    if (originalRenderer != null)
                        originalRenderer.enabled = false;

                    TetoRocketsPlugin.PluginLog.LogInfo(
                        "Successfully replaced rocket with Teto! ID: " +
                        instanceId
                    );
                },

                exception =>
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "Failed to load Teto model for rocket " +
                        instanceId + ": " + exception
                    );

                    Replacements.Remove(instanceId);
                },

                options,

                TetoRocketsPlugin.ModelOwner
            );
        }

        public static void CleanupDestroyedRockets()
        {
            List<int> removeIds = new List<int>();

            foreach (KeyValuePair<int, RocketReplacement> pair
                in Replacements)
            {
                RocketReplacement replacement = pair.Value;

                if (replacement.Grenade == null)
                {
                    if (replacement.TetoModel != null)
                        replacement.TetoModel.Destroy();

                    removeIds.Add(pair.Key);
                }
            }

            foreach (int id in removeIds)
                Replacements.Remove(id);
        }
    }
}
