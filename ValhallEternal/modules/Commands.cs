using Jotunn.Entities;
using Jotunn.Managers;
using ValhallEternal.common;

namespace ValhallEternal.modules
{
    internal static class Commands
    {
        internal static void AddCommands() {
            CommandManager.Instance.AddConsoleCommand(new SetPlayerEternalLevel());
        }

        internal class SetPlayerEternalLevel : ConsoleCommand
        {
            public override string Name => "VE-Set-Player-Level";
            public override string Help => "Format: [level]";
            //public override bool IsNetwork => false;
            //public override bool IsCheat => true;

            public override void Run(string[] args)
            {
                if (args.Length < 1)
                {
                    Logger.LogInfo("Level required.");
                }
                int.TryParse(args[0].Trim(), out int result);
                if (result != 0) {
                    Logger.LogDebug($"Setting player prestige level {result}");
                    Player.m_localPlayer.m_nview.GetZDO().Set(DataObjects.CustomLevelZKey, result);
                    Player.m_localPlayer.PlayerRemoveUniqueKey(DataObjects.CustomLevelZKey);
                    Player.m_localPlayer.AddUniqueKeyValue(DataObjects.CustomLevelZKey, $"{result}");
                    PlayerLevelDisplays.UpdateLocalLevelDisplay(Player.m_localPlayer);
                }
            }
        }
    }
}
