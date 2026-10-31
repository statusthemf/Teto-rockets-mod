
using HarmonyLib;

namespace ULTRADROID.TetoRockets
{
    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.Shoot))]
    internal static class RocketSoundPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!TetoRocketsPlugin.GameplayActive)
                return;

            TetoRocketsPlugin.PlayTetoSound();
        }
    }
}
