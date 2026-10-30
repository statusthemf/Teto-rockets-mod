
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

            Transform modelTransform =
                grenade.transform.Find("Model");

            if (modelTransform == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Rocket Model child not found: " +
                    grenade.gameObject.name
                );
                return;
            }

            MeshRenderer originalRenderer =
                modelTransform.GetComponent<MeshRenderer>();

            if (originalRenderer == null)
            {
                TetoRocketsPlugin.PluginLog.LogWarning(
                    "Rocket MeshRenderer not found."
                );
                return;
            }

            RocketReplacement replacement =
                new RocketReplacement
                {
                    Grenade = grenade,
                    ModelTransform = modelTransform,
                    OriginalRenderer = originalRenderer
                };

            Replacements.Add(instanceId, replacement);

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

            TetoRocketsPlugin.PluginLog.LogInfo(
                "Loading Teto model for " +
                grenade.gameObject.name
            );

            ModelAPI.LoadGLBAsync(
                TetoRocketsPlugin.ModelPath,

                instance =>
                {
                    if (instance == null || !instance.IsValid)
                    {
                        TetoRocketsPlugin.PluginLog.LogError(
                            "Teto model returned an invalid instance."
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

                    instance.SetParent(modelTransform, false);

                    if (originalRenderer != null)
                        originalRenderer.enabled = false;

                    TetoRocketsPlugin.PluginLog.LogInfo(
                        "Teto rocket replacement successful: " +
                        instanceId
                    );

                    // Play the sound once for this rocket.
                    TetoRocketsPlugin.PlayTetoSound();
                },

                exception =>
                {
                    TetoRocketsPlugin.PluginLog.LogError(
                        "Teto loading failed: " + exception
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
