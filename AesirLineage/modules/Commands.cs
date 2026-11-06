using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AesirLineage.common;
using AesirLineage.starmap;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules
{
    internal static class Commands
    {
        internal static void AddCommands() {
            CommandManager.Instance.AddConsoleCommand(new SetAscension());
            CommandManager.Instance.AddConsoleCommand(new ClearPlayerData());
            CommandManager.Instance.AddConsoleCommand(new ListStarMap());
            CommandManager.Instance.AddConsoleCommand(new AllocateNode());
            CommandManager.Instance.AddConsoleCommand(new RefundNode());
            CommandManager.Instance.AddConsoleCommand(new ListPowers());
            CommandManager.Instance.AddConsoleCommand(new GrantPoints());
            CommandManager.Instance.AddConsoleCommand(new GivePlayerPoints());
            CommandManager.Instance.AddConsoleCommand(new RemovePlayerPoints());
            CommandManager.Instance.AddConsoleCommand(new ResetStarMap());
            CommandManager.Instance.AddConsoleCommand(new StartBoon());
            CommandManager.Instance.AddConsoleCommand(new ShrineQuest());
            CommandManager.Instance.AddConsoleCommand(new ShowDeeds());
            CommandManager.Instance.AddConsoleCommand(new ForceDream());
            CommandManager.Instance.AddConsoleCommand(new ShowLocks());
        }

        // Names players might type for a deity, beyond the enum names.
        private static readonly Dictionary<string, Deity> DeityAliases = new Dictionary<string, Deity>(StringComparer.OrdinalIgnoreCase) {
            { "skadi", Deity.Skaldi },
            { "skaði", Deity.Skaldi },
            { "freyja", Deity.Freya },
            { "vör", Deity.Vor },
        };

        private static bool TryParseDeity(string text, out Deity deity) {
            if (DeityAliases.TryGetValue(text, out deity)) { return true; }
            return Enum.TryParse(text, ignoreCase: true, out deity) && Enum.IsDefined(typeof(Deity), deity);
        }

        private static List<string> DeityNames() => Enum.GetNames(typeof(Deity)).ToList();

        private static void Print(string text) {
            if (global::Console.instance != null) { global::Console.instance.Print(Localization.instance.Localize(text)); }
            Logger.LogDebug(text);
        }

        // Parses "<deity> <node>" and finds the node, printing why not when it can't.
        private static bool TryFindNode(string[] args, out StarNode node) {
            node = null;
            if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return false; }
            if (args.Length < 2 || !TryParseDeity(args[0], out Deity deity)) { Print("$al_cmd_usage_node"); return false; }
            if (!StarMaps.TryGet(deity, out StarMap map) || !map.Nodes.TryGetValue(args[1], out node)) {
                Print(Localization.instance.Localize("$al_cmd_unknown_node", args[1], deity.ToString()));
                return false;
            }
            return true;
        }

        private static List<string> OnlinePlayerNames() => ZNet.instance != null ? ZNet.instance.GetPlayerList().Select(info => info.m_name).ToList() : new List<string>();

        private static List<string> Numbers(int from, int to) => Enumerable.Range(from, Math.Max(0, to - from + 1)).Select(n => n.ToString()).ToList();

        // For completing a node: the ids of the nodes, in the star map of the deity typed before, that pass the test.
        private static List<string> NodeIds(string[] before, Func<StarNode, bool> test) {
            if (Player.m_localPlayer == null || before.Length < 1 || !TryParseDeity(before[0], out Deity deity) || !StarMaps.TryGet(deity, out StarMap map)) { return null; }
            return map.Nodes.Values.Where(test).Select(node => node.Id).ToList();
        }

        // Parses "<player> <deity> <amount>", reading from the end so a name may hold spaces.
        private static bool TryParsePlayerPoints(string[] args, out string player, out Deity deity, out int amount) {
            player = null;
            deity = default;
            amount = 0;
            if (args.Length < 3 || !int.TryParse(args[args.Length - 1], out amount) || amount <= 0 || !TryParseDeity(args[args.Length - 2], out deity)) { return false; }
            player = string.Join(" ", args.Take(args.Length - 2));
            return true;
        }

        private static string DescribeGrants(StarNode node) {
            return string.Join(", ", node.Grants.Select(g => Compendium.DescribePower(g.Key, g.Value)));
        }

        internal class SetAscension : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-ascension";
            public override string Help => "<deity> <level> - set your Ascension with a deity, without its costs or rewards";

            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => DeityNames(),
                1 => Numbers(0, Ascension.Max),
                _ => null,
            };

            protected override void Execute(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length < 2 || !TryParseDeity(args[0], out Deity deity) || !int.TryParse(args[1], out int level) || level < 0) {
                    Print("$al_cmd_usage_ascension");
                    return;
                }
                PlayerData.SetAscension(deity, level);
                PrestigeDisplays.UpdateLocalPlayerLevelDisplay();
                Print(Localization.instance.Localize("$al_cmd_ascension_set", Compendium.DeityName(deity), level.ToString()));
            }
        }

        internal class ClearPlayerData : AdminConsoleCommand {
            public override string Name => "al-remove-player-data";
            public override string Help => "This clears ALL star points, nodes, looks and Ascensions. REQUIRES A CLIENT RESTART";
            //public override bool IsNetwork => false;

            protected override void Execute(string[] args) {
                Player.m_localPlayer.m_customData.Remove(DataObjects.CustomDataKey);
            }
        }

        internal class ListStarMap : ConsoleCommand, ICompletesArguments {
            public override string Name => "al-list";
            public override string Help => "[deity] - your points per deity, or one deity's nodes: [x] taken, [+] can take, [ ] locked";

            public List<string> ArgumentOptions(int index, string[] before) => index == 0 ? DeityNames() : null;

            public override void Run(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length == 0) {
                    foreach (StarMap map in StarMaps.All) {
                        Print(Localization.instance.Localize("$al_cmd_points_line", Compendium.DeityName(map.Deity),
                            PlayerData.PointsEarned(map.Deity).ToString(), PlayerData.PointsSpent(map.Deity).ToString(), PlayerData.PointsAvailable(map.Deity).ToString()));
                    }
                    return;
                }
                if (!TryParseDeity(args[0], out Deity deity) || !StarMaps.TryGet(deity, out StarMap starMap)) {
                    Print("$al_cmd_usage_deity");
                    return;
                }
                Print(Localization.instance.Localize("$al_cmd_points_line", Compendium.DeityName(deity),
                    PlayerData.PointsEarned(deity).ToString(), PlayerData.PointsSpent(deity).ToString(), PlayerData.PointsAvailable(deity).ToString()));
                foreach (StarNode node in starMap.Nodes.Values.OrderBy(n => n.Definition.Ring).ThenBy(n => n.Definition.Angle)) {
                    if (node == starMap.Root) { continue; }
                    string state = PlayerData.IsAllocated(node) ? "[x]" : PlayerData.CanAllocate(node, out _) ? "[+]" : "[ ]";
                    Print($"{state} {node.Id} - {node.DisplayName} ({node.Tier}, {node.Cost}): {DescribeGrants(node)}");
                }
            }
        }

        internal class AllocateNode : ConsoleCommand, ICompletesArguments {
            public override string Name => "al-allocate";
            public override string Help => "<deity> <node> - spend points on a star map node";

            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => DeityNames(),
                1 => NodeIds(before, node => PlayerData.CanAllocate(node, out _)),
                _ => null,
            };

            public override void Run(string[] args) {
                if (!TryFindNode(args, out StarNode node)) { return; }
                if (PlayerData.Allocate(node, out string reason)) {
                    Print(Localization.instance.Localize("$al_cmd_allocated", node.DisplayName, DescribeGrants(node)));
                } else {
                    Print(reason);
                }
            }
        }

        // Spent points are locked for players, who reset a whole map at the deity's shrine; giving back
        // one node is left to admins.
        internal class RefundNode : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-refund";
            public override string Help => "<deity> <node> - give back a node, if nothing else depends on it";

            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => DeityNames(),
                1 => NodeIds(before, node => PlayerData.CanRefund(node, out _)),
                _ => null,
            };

            protected override void Execute(string[] args) {
                if (!TryFindNode(args, out StarNode node)) { return; }
                if (PlayerData.Refund(node, out string reason)) {
                    Print(Localization.instance.Localize("$al_cmd_refunded", node.DisplayName));
                } else {
                    Print(reason);
                }
            }
        }

        internal class ListPowers : ConsoleCommand {
            public override string Name => "al-powers";
            public override string Help => "- every power and oath your star maps give you now";

            public override void Run(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < PowerInfo.Count; i++) {
                    Power power = (Power)i;
                    float value = PowerSet.Get(power);
                    float dormant = PowerSet.Dormant(power);
                    if (value > 0f) {
                        sb.AppendLine($"{power}: {Compendium.DescribePower(power, value)}");
                    } else if (dormant > 0f) {
                        sb.AppendLine($"{power}: {Localization.instance.Localize("$al_power_dormant", dormant.ToString("0.##"))}");
                    }
                }
                Print(sb.Length > 0 ? sb.ToString() : "$al_cmd_no_powers");
            }
        }

        internal class GrantPoints : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-points";
            public override string Help => "<deity> <amount> - add (or with a negative amount, remove) your own star points";

            public List<string> ArgumentOptions(int index, string[] before) => index == 0 ? DeityNames() : null;

            protected override void Execute(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length < 2 || !TryParseDeity(args[0], out Deity deity) || !int.TryParse(args[1], out int amount)) {
                    Print("$al_cmd_usage_points");
                    return;
                }
                if (amount >= 0) {
                    PlayerData.AddPoints(deity, amount);
                } else if (PlayerData.TakePoints(deity, -amount)) {
                    Print(Localization.instance.Localize("$al_points_map_reset", Compendium.DeityName(deity)));
                }
                Print(Localization.instance.Localize("$al_cmd_points_line", Compendium.DeityName(deity),
                    PlayerData.PointsEarned(deity).ToString(), PlayerData.PointsSpent(deity).ToString(), PlayerData.PointsAvailable(deity).ToString()));
            }
        }

        /// <summary>Changes another player's points, through <see cref="AdminPoints"/>. Give and remove differ only in the sign.</summary>
        internal abstract class PlayerPointsCommand : AdminConsoleCommand, ICompletesArguments {
            protected abstract int Sign { get; }

            // Counted from the start, so completion assumes a name without spaces; running the command doesn't.
            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => OnlinePlayerNames(),
                1 => DeityNames(),
                _ => null,
            };

            protected override void Execute(string[] args) {
                if (!TryParsePlayerPoints(args, out string player, out Deity deity, out int amount)) {
                    Print(Localization.instance.Localize("$al_cmd_usage_player_points", Name));
                    return;
                }
                AdminPoints.Send(player, deity, Sign * amount);
            }
        }

        internal class GivePlayerPoints : PlayerPointsCommand {
            public override string Name => "al-give-points";
            public override string Help => "<player> <deity> <amount> - give star points to a player who is online";
            protected override int Sign => 1;
        }

        internal class RemovePlayerPoints : PlayerPointsCommand {
            public override string Name => "al-remove-points";
            public override string Help => "<player> <deity> <amount> - take star points from a player who is online; if they no longer cover the stars taken, that star map is reset";
            protected override int Sign => -1;
        }

        internal class ResetStarMap : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-reset";
            public override string Help => "<deity|all> - refund every node in a star map";

            public List<string> ArgumentOptions(int index, string[] before) => index == 0 ? DeityNames().Append("all").ToList() : null;

            protected override void Execute(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length < 1) { Print("$al_cmd_usage_deity"); return; }
                if (string.Equals(args[0], "all", StringComparison.OrdinalIgnoreCase)) {
                    foreach (Deity each in Enum.GetValues(typeof(Deity))) { PlayerData.ResetDeity(each); }
                } else if (TryParseDeity(args[0], out Deity deity)) {
                    PlayerData.ResetDeity(deity);
                } else {
                    Print("$al_cmd_usage_deity");
                    return;
                }
                Print("$al_cmd_reset");
            }
        }

        internal class StartBoon : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-boon";
            public override string Help => "<deity> [blessing | dream [n]] | clear - start a deity's blessing or a dream's inspiration (the nth, or a random one) without the shrine or sleep, or end every boon";

            public List<string> ArgumentOptions(int index, string[] before) {
                if (index == 0) { return DeityNames().Append("clear").ToList(); }
                if (!TryParseDeity(before[0], out Deity deity)) { return null; }
                if (index == 1) { return new List<string>() { "blessing", "dream" }; }
                if (index == 2 && string.Equals(before[1], "dream", StringComparison.OrdinalIgnoreCase) && ShrineData.All.TryGetValue(deity, out DeityShrine shrine) && shrine?.Inspirations != null) {
                    return Numbers(1, shrine.Inspirations.Count);
                }
                return null;
            }

            protected override void Execute(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length >= 1 && string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase)) {
                    Boons.EndAll();
                    Print("$al_cmd_boons_cleared");
                    return;
                }
                if (args.Length < 1 || !TryParseDeity(args[0], out Deity deity)) { Print("$al_cmd_usage_boon"); return; }
                BoonKind kind = BoonKind.Blessing;
                if (args.Length >= 2) {
                    if (string.Equals(args[1], "dream", StringComparison.OrdinalIgnoreCase)) {
                        kind = BoonKind.Inspiration;
                    } else if (!string.Equals(args[1], "blessing", StringComparison.OrdinalIgnoreCase)) {
                        Print("$al_cmd_usage_boon");
                        return;
                    }
                }

                ShrineData.All.TryGetValue(deity, out DeityShrine shrine);
                Boon boon;
                if (kind == BoonKind.Blessing) {
                    boon = shrine?.Blessing;
                } else {
                    List<Boon> inspirations = shrine?.Inspirations ?? new List<Boon>();
                    int index = args.Length >= 3 && int.TryParse(args[2], out int n) ? n - 1 : UnityEngine.Random.Range(0, inspirations.Count);
                    boon = index >= 0 && index < inspirations.Count ? inspirations[index] : null;
                }
                if (boon == null) {
                    Print(Localization.instance.Localize("$al_cmd_no_boon", Compendium.DeityName(deity)));
                    return;
                }
                Boons.Start(deity, kind, boon);
                Print(Localization.instance.Localize("$al_cmd_boon_started", Boons.DisplayName(boon), Boons.Minutes(kind).ToString(), Boons.Describe(boon)));
            }
        }

        internal class ShrineQuest : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-quest";
            public override string Help => "<deity> take|complete - take the deity's shrine quest without waiting, or complete it without the boss, with its points";

            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => DeityNames(),
                1 => new List<string>() { "take", "complete" },
                _ => null,
            };

            protected override void Execute(string[] args) {
                Player player = Player.m_localPlayer;
                if (player == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length < 2 || !TryParseDeity(args[0], out Deity deity)) {
                    Print("$al_cmd_usage_quest");
                    return;
                }
                string boss = Quests.BossFor(deity);
                if (boss == null) {
                    Print(Localization.instance.Localize("$al_cmd_no_quest", Compendium.DeityName(deity)));
                    return;
                }
                if (string.Equals(args[1], "take", StringComparison.OrdinalIgnoreCase)) {
                    Quests.Take(deity);
                    Print(Localization.instance.Localize("$al_cmd_quest_taken", Compendium.DeityName(deity), Quests.BossName(boss)));
                } else if (string.Equals(args[1], "complete", StringComparison.OrdinalIgnoreCase)) {
                    Quests.Complete(player, deity);
                    Print(Localization.instance.Localize("$al_cmd_quest_done", Compendium.DeityName(deity), PlayerData.QuestsDone(deity).ToString()));
                } else {
                    Print("$al_cmd_usage_quest");
                }
            }
        }

        internal class ForceDream : AdminConsoleCommand {
            public override string Name => "al-dream";
            public override string Help => "- your next sleep brings a deity's dream whatever the chance, if a deity can send one";

            protected override void Execute(string[] args) {
                if (Player.m_localPlayer == null) { Print("$al_cmd_no_player"); return; }
                List<(Deity deity, int weight)> eligible = Dreams.Eligible();
                if (eligible.Count == 0) {
                    Print(Localization.instance.Localize("$al_cmd_dream_none", ValConfig.InspirationMinPoints.Value.ToString()));
                    return;
                }
                Dreams.ForceNext = true;
                Print(Localization.instance.Localize("$al_cmd_dream_forced", string.Join(", ", eligible.Select(entry => $"{Compendium.DeityName(entry.deity)} ({entry.weight})"))));
            }
        }

        internal class ShowDeeds : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-deeds";
            public override string Help => "[chance <percent|off>] - your biome level, lucky points and minibosses this run; or try every lucky chance at one percent, for testing";

            public List<string> ArgumentOptions(int index, string[] before) => index switch {
                0 => new List<string>() { "chance" },
                1 when string.Equals(before[0], "chance", StringComparison.OrdinalIgnoreCase) => new List<string>() { "off" },
                _ => null,
            };

            protected override void Execute(string[] args) {
                Player player = Player.m_localPlayer;
                if (player == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length >= 1) {
                    if (!string.Equals(args[0], "chance", StringComparison.OrdinalIgnoreCase) || args.Length < 2) {
                        Print("$al_cmd_usage_deeds");
                    } else if (string.Equals(args[1], "off", StringComparison.OrdinalIgnoreCase)) {
                        Deeds.ForcedChance = -1f;
                        Print("$al_cmd_deeds_chance_off");
                    } else if (float.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float percent) && percent >= 0f) {
                        Deeds.ForcedChance = percent;
                        Print(Localization.instance.Localize("$al_cmd_deeds_chance", percent.ToString("0.##")));
                    } else {
                        Print("$al_cmd_usage_deeds");
                    }
                    return;
                }
                Print(Localization.instance.Localize("$al_cmd_deeds_level", Deeds.BiomeAtLevel(Deeds.BiomeLevel(player)).ToString()));
                string limit = ValConfig.LuckyPointsPerDeity.Value.ToString();
                foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                    Print(Localization.instance.Localize("$al_cmd_deeds_line", Compendium.DeityName(deity), Deeds.LuckyPoints(deity).ToString(), limit));
                }
                List<string> killed = PlayerData.Save.Run.MinibossesKilled;
                Print(Localization.instance.Localize("$al_cmd_deeds_minibosses", killed.Count > 0 ? string.Join(", ", killed) : Localization.instance.Localize("$al_cmd_none")));
                if (Deeds.ForcedChance >= 0f) { Print(Localization.instance.Localize("$al_cmd_deeds_chance", Deeds.ForcedChance.ToString("0.##"))); }
            }
        }

        internal class ShowLocks : AdminConsoleCommand, ICompletesArguments {
            public override string Name => "al-locks";
            public override string Help => "[<prefab> | dump] - the progression lock and the last boss you have defeated; which boss an item or build piece needs; or every sealed item and piece, by boss, written to the log";

            public List<string> ArgumentOptions(int index, string[] before) => index == 0 ? new List<string>() { "dump" } : null;

            protected override void Execute(string[] args) {
                Player player = Player.m_localPlayer;
                if (player == null) { Print("$al_cmd_no_player"); return; }
                if (args.Length == 0) {
                    if (!ProgressionLock.Active) { Print("$al_cmd_locks_off"); }
                    int level = ProgressionLock.Level(player);
                    string last = level > 0 ? ProgressionLock.BossName(level) : Localization.instance.Localize("$al_cmd_none");
                    bool world = string.Equals(ValConfig.ProgressionLockSource.Value, "World", StringComparison.OrdinalIgnoreCase);
                    Print(Localization.instance.Localize(world ? "$al_cmd_locks_level_world" : "$al_cmd_locks_level_personal", last));
                    return;
                }
                if (string.Equals(args[0], "dump", StringComparison.OrdinalIgnoreCase)) {
                    var (byBoss, open) = ProgressionLock.Survey();
                    foreach (KeyValuePair<int, List<string>> boss in byBoss) {
                        Logger.LogInfo($"Sealed until {Deeds.BossOrder[boss.Key - 1]} ({boss.Value.Count}): {string.Join(", ", boss.Value)}");
                    }
                    Logger.LogInfo($"Never sealed ({open.Count}): {string.Join(", ", open)}");
                    Print(Localization.instance.Localize("$al_cmd_locks_dump", byBoss.Values.Sum(list => list.Count).ToString(), open.Count.ToString()));
                    return;
                }
                string prefab = args[0];
                Piece piece = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab)?.GetComponent<Piece>() : null;
                if (piece == null && (ObjectDB.instance == null || ObjectDB.instance.GetItemPrefab(prefab) == null)) {
                    Print(Localization.instance.Localize("$al_cmd_locks_unknown", prefab));
                    return;
                }
                int needed = piece != null ? ProgressionLock.Needed(piece) : ProgressionLock.Needed(prefab, 1);
                if (needed <= 0) {
                    Print(Localization.instance.Localize("$al_cmd_locks_free", prefab));
                    return;
                }
                bool opened = ProgressionLock.Level(player) >= needed;
                Print(Localization.instance.Localize(opened ?"$al_cmd_locks_item_open" : "$al_cmd_locks_item_sealed", prefab, ProgressionLock.BossName(needed)));
            }
        }
    }
}
