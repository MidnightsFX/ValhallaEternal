using System;
using System.Reflection;
using HarmonyLib;

namespace ValhallEternal.common
{
    // TODO: Make this a more generic compat class later
    internal static class ProgressionCompat
    {
        private static bool progressionResolved;
        private static MethodInfo ProgressionRemovePrivateKey;
        private static object PRPInstance;

        private static void Resolve() {
            if (progressionResolved) { return; }
            progressionResolved = true;

            Type keyManagerType = AccessTools.TypeByName("VentureValheim.Progression.KeyManager");
            if (keyManagerType == null) { return; }

            PropertyInfo instanceProp = AccessTools.Property(keyManagerType, "Instance");
            PRPInstance = instanceProp?.GetValue(null);
            if (PRPInstance == null) { return; }

            ProgressionRemovePrivateKey = AccessTools.Method(keyManagerType, "RemovePrivateKey", new[] { typeof(string) });
        }

        public static bool TryRemovePrivateKey(string key) {
            Resolve();
            if (ProgressionRemovePrivateKey == null || PRPInstance == null) { return false; }
            try {
                ProgressionRemovePrivateKey.Invoke(PRPInstance, new object[] { key });
                return true;
            } catch (Exception e) {
                Logger.LogWarning($"Failed to remove VentureValheim private key '{key}': {e.Message}");
                return false;
            }
        }
    }
}
