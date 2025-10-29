using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValhallEternal.common
{
    internal static class Utils
    {
        public static bool PlayerHasUniqueKey(this Player player, string key) {
            foreach (string pkey in player.GetUniqueKeys()) {
                if (pkey.StartsWith(key)) { return true; }
            }
            return false;
        }

        public static bool PlayerRemoveUniqueKey(this Player player, string key) {
            List<string> keys = player.GetUniqueKeys();
            foreach (string pkey in keys) {
                if (pkey.StartsWith(key)) {
                    player.RemoveUniqueKey(pkey);
                    return true;
                }
            }
            return false;
        }
    }
}
