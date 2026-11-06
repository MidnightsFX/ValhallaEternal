using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.ui;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Powers that reach other players or the creatures they control. Each player publishes what others
    /// need to know as small values on their own ZDO; every client reads those for its own player and
    /// the creatures it owns. The Last Door, which moves a killing blow onto another player, uses a
    /// routed RPC to the guardian.
    /// </summary>
    internal static class AllyPowers {
        /// <summary>Float: the rune dome's protection, in percent, while this player blocks.</summary>
        internal const string DomeKey = "al_dome";
        /// <summary>Bool: this player's Last Door is ready to take an ally's killing blow.</summary>
        internal const string LastDoorKey = "al_lastdoor";
        /// <summary>Bool: creatures this player hits turn on them.</summary>
        internal const string ChallengeKey = "al_challenge";
        /// <summary>Float: food health this player shares with allies nearby.</summary>
        internal const string HearthKey = "al_hearth";

        // Read by whoever owns the creature, station, building or ally they touch.
        internal const string TamingKey = "al_taming";
        internal const string TameGuardKey = "al_tameguard";
        internal const string FruitfulKey = "al_fruitful";
        internal const string HerdFedKey = "al_herdfed";
        internal const string StationKey = "al_station";
        internal const string HallWardKey = "al_hallward";
        /// <summary>String: the trophy whose creature this player has banned from their buildings.</summary>
        internal const string BannedKey = "al_banned";
        internal const string BarredKey = "al_barred";
        internal const string SanctuaryKey = "al_sanctuary";
        internal const string HavenKey = "al_haven";
        /// <summary>Float: the damage and experience bonus this player's oaths lend those nearby (Vár's Witness).</summary>
        internal const string VowKey = "al_vow";
        internal const string PackDamageKey = "al_packdmg";
        internal const string PackGuardKey = "al_packguard";
        internal const string SummonDamageKey = "al_summondmg";
        internal const string EndlessKey = "al_endless";
        internal const string HuntKey = "al_hunt";
        internal const string TrophyKey = "al_trophy";
        internal const string SpoilsKey = "al_spoils";
        /// <summary>ZDOID and time: the creature this player last hit, which their pack goes for.</summary>
        internal const string PackTargetKey = "al_packtarget";
        internal const string PackTargetTimeKey = "al_packtime";
        /// <summary>ZDOID: the player this one is bonded to (Freyja's Bond).</summary>
        internal const string BondKey = "al_bond";
        internal const string BondSpeedKey = "al_bondspeed";
        internal const string BurdenKey = "al_burden";
        /// <summary>Long: until when this player's rainbow blesses those nearby.</summary>
        internal const string BifrostKey = "al_bifrost";
        /// <summary>ZDOID and time: the creature this player's Dread sends running.</summary>
        internal const string DreadKey = "al_dread";
        internal const string DreadUntilKey = "al_dreaduntil";
        /// <summary>Bool: this player's Fimbulvetr blizzard keeps allies inside it from freezing.</summary>
        internal const string BlizzardKey = "al_blizzard";
        /// <summary>Float: how far from this player snow stops piling on buildings, in metres.</summary>
        internal const string SnowShedKey = "al_snowshed";

        private const string LastDoorRpc = "AL_LastDoor";
        private const float LastDoorCooldown = 300f;
        private const float HearthRange = 8f;
        private const float DomeRadius = 4f;

        private static float lastDoorReadyAt;
        private static readonly Dictionary<int, GameObject> domes = new Dictionary<int, GameObject>();
        private static readonly List<Player> scratch = new List<Player>();
        private static readonly List<Player> nearby = new List<Player>();

        internal static void Reset(bool newCharacter) {
            // A guardian who died taking the blow still waits out the Last Door's cooldown.
            if (newCharacter) { lastDoorReadyAt = 0f; }
        }

        /// <summary>Writes the local player's shared values, only when they change.</summary>
        internal static void Publish(Player player) {
            ZDO zdo = player.m_nview?.GetZDO();
            if (zdo == null) { return; }
            float dome = PowerSet.Has(Power.RuneDome, out float protection) && player.IsBlocking() ? protection : 0f;
            if (zdo.GetFloat(DomeKey, 0f) != dome) { zdo.Set(DomeKey, dome); }
            bool door = PowerSet.Has(Power.LastDoor) && Time.time >= lastDoorReadyAt && !player.IsDead();
            if (zdo.GetBool(LastDoorKey, false) != door) { zdo.Set(LastDoorKey, door); }
            bool challenge = PowerSet.Has(Power.Challenge);
            if (zdo.GetBool(ChallengeKey, false) != challenge) { zdo.Set(ChallengeKey, challenge); }
            // Rounded, so small food changes don't send an update every second.
            float hearth = PowerSet.Has(Power.HearthAura, out float share) ? Mathf.Round(FoodPowers.FoodHealth(player) * share / 100f) : 0f;
            if (zdo.GetFloat(HearthKey, 0f) != hearth) { zdo.Set(HearthKey, hearth); }

            Publish(zdo, TamingKey, PowerSet.Get(Power.TamingSpeed));
            Publish(zdo, TameGuardKey, PowerSet.Get(Power.TameGuard));
            Publish(zdo, FruitfulKey, PowerSet.Get(Power.FruitfulHerd));
            Publish(zdo, HerdFedKey, PowerSet.Has(Power.HerdFed));
            Publish(zdo, StationKey, PowerSet.Get(Power.StationSpeed));
            Publish(zdo, HallWardKey, PowerSet.Get(Power.HallWard));
            string banned = PowerSet.Has(Power.BanTheKind) ? HomePowers.BannedTrophy(player) : "";
            if (zdo.GetString(BannedKey, "") != banned) { zdo.Set(BannedKey, banned); }
            Publish(zdo, BarredKey, PowerSet.Has(Power.BarredHall));
            Publish(zdo, SanctuaryKey, PowerSet.Get(Power.Sanctuary));
            Publish(zdo, HavenKey, PowerSet.Get(Power.SafeHaven));
            Publish(zdo, VowKey, PowerSet.Has(Power.VowWitness) ? StrikePowers.VowBonus() / 2f : 0f);
            Publish(zdo, PackDamageKey, PowerSet.Get(Power.CompanionDamage));
            Publish(zdo, PackGuardKey, PowerSet.Get(Power.CompanionGuard));
            Publish(zdo, SummonDamageKey, PowerSet.Get(Power.SummonDamage));
            Publish(zdo, EndlessKey, PowerSet.Has(Power.EndlessPack));
            Publish(zdo, HuntKey, PowerSet.Has(Power.CallOfTheHunt));
            Publish(zdo, TrophyKey, PowerSet.Get(Power.TrophyChance));
            Publish(zdo, SpoilsKey, PowerSet.Get(Power.HideYield));
            Publish(zdo, BondSpeedKey, PowerSet.Get(Power.Bonded));
            Publish(zdo, BurdenKey, PowerSet.Get(Power.SharedBurden));
            Publish(zdo, BlizzardKey, PowerSet.Has(Power.Fimbulvetr));
            Publish(zdo, SnowShedKey, PowerSet.Get(Power.SnowShed));
            ZDOID bond = BondPowers.BondedPlayerId();
            if (zdo.GetZDOID(BondKey) != bond) { zdo.Set(BondKey, bond); }
        }

        private static void Publish(ZDO zdo, string key, float value) {
            if (zdo.GetFloat(key, 0f) != value) { zdo.Set(key, value); }
        }

        private static void Publish(ZDO zdo, string key, bool value) {
            if (zdo.GetBool(key, false) != value) { zdo.Set(key, value); }
        }

        /// <summary>Marks a creature for a moment on the local player's ZDO, for whoever owns it to act on.</summary>
        internal static void PublishTarget(Player player, string idKey, string timeKey, ZDOID target, long until) {
            ZDO zdo = player.m_nview?.GetZDO();
            if (zdo == null) { return; }
            zdo.Set(idKey, target);
            zdo.Set(timeKey, until);
        }

        /// <summary>The largest value any player within range publishes under a key, the local player included.</summary>
        internal static float Strongest(Vector3 at, float range, string key) {
            float best = 0f;
            nearby.Clear();
            Player.GetPlayersInRange(at, range, nearby);
            foreach (Player player in nearby) {
                if (player == null || player.IsDead()) { continue; }
                best = Mathf.Max(best, player.m_nview?.GetZDO()?.GetFloat(key, 0f) ?? 0f);
            }
            return best;
        }

        /// <summary>Whether any player within range publishes a key as on, the local player included.</summary>
        internal static bool AnyNear(Vector3 at, float range, string key) {
            nearby.Clear();
            Player.GetPlayersInRange(at, range, nearby);
            foreach (Player player in nearby) {
                if (player != null && !player.IsDead() && player.m_nview?.GetZDO()?.GetBool(key, false) == true) { return true; }
            }
            return false;
        }

        /// <summary>The loaded player a tame follows, from the name vanilla keeps on its ZDO.</summary>
        internal static Player FollowedPlayer(Character tame) {
            string name = tame?.m_nview?.GetZDO()?.GetString(ZDOVars.s_follow, "") ?? "";
            if (name.Length == 0) { return null; }
            foreach (Player player in Player.GetAllPlayers()) {
                if (player != null && player.GetPlayerName() == name) { return player; }
            }
            return null;
        }

        /// <summary>Tames within range that follow the local player.</summary>
        internal static List<Character> MyTamesNear(Vector3 at, float range) {
            List<Character> tames = new List<Character>();
            Player local = Player.m_localPlayer;
            if (local == null) { return tames; }
            List<Character> around = new List<Character>();
            Character.GetCharactersInRange(at, range, around);
            foreach (Character character in around) {
                if (character != null && !character.IsPlayer() && character.IsTamed() && !character.IsDead() && FollowedPlayer(character) == local) {
                    tames.Add(character);
                }
            }
            return tames;
        }

        /// <summary>Living tames within range, whoever they follow.</summary>
        internal static List<Character> TamesNear(Vector3 at, float range) {
            List<Character> tames = new List<Character>();
            List<Character> around = new List<Character>();
            Character.GetCharactersInRange(at, range, around);
            foreach (Character character in around) {
                if (character != null && !character.IsPlayer() && character.IsTamed() && !character.IsDead()) { tames.Add(character); }
            }
            return tames;
        }

        /// <summary>The largest food-health share offered by another player close enough.</summary>
        internal static float HearthShareFor(Player local) {
            float best = 0f;
            foreach (Player ally in PowerFx.AlliesNear(local.transform.position, HearthRange)) {
                best = Mathf.Max(best, ally.m_nview?.GetZDO()?.GetFloat(HearthKey, 0f) ?? 0f);
            }
            return best;
        }

        // ---- The Last Door ----

        internal static Player ReadyGuardianNear(Player victim, float range) {
            foreach (Player ally in PowerFx.AlliesNear(victim.transform.position, range)) {
                if (ally.m_nview?.GetZDO()?.GetBool(LastDoorKey, false) == true) { return ally; }
            }
            return null;
        }

        internal static void SendBlowToGuardian(Player guardian, float damage) {
            ZDO zdo = guardian.m_nview?.GetZDO();
            if (zdo == null || ZRoutedRpc.instance == null) { return; }
            ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), LastDoorRpc, damage);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_lastdoor_saved", guardian.GetPlayerName()));
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<float>(LastDoorRpc, OnBlowTaken);
            }
        }

        // Runs on the guardian's machine: they take the blow meant for their ally.
        private static void OnBlowTaken(long sender, float damage) {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead()) { return; }
            lastDoorReadyAt = Time.time + LastDoorCooldown;
            Publish(player);
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_lastdoor_taken"));
            PowerFx.Effect(Power.LastDoor, VfxRole.Burst, player.transform.position, 0.6f, shared: true);
            HitData hit = new HitData() { m_hitType = HitData.HitType.Self };
            hit.m_damage.m_damage = damage;
            player.Damage(hit);
        }

        // ---- Challenge ----

        /// <summary>
        /// Runs on the machine that owns the creature, whoever hit it: a challenger it is hit by becomes its
        /// target, replacing whoever it was after.
        /// </summary>
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.OnDamaged))]
        private static class Taunt {
            private static void Postfix(MonsterAI __instance, Character attacker) {
                if (!(attacker is Player challenger) || __instance.m_character == null || __instance.m_character.IsTamed()) { return; }
                if (challenger.m_nview?.GetZDO()?.GetBool(ChallengeKey, false) != true) { return; }
                __instance.m_targetCreature = attacker;
                __instance.m_targetStatic = null;
            }
        }

        // ---- Rune dome visuals, for every player blocking under one ----

        internal static void UpdateDomes() {
            scratch.Clear();
            scratch.AddRange(Player.GetAllPlayers());
            HashSet<int> seen = new HashSet<int>();
            foreach (Player player in scratch) {
                if (player == null) { continue; }
                int id = player.GetInstanceID();
                seen.Add(id);
                bool active = (player.m_nview?.GetZDO()?.GetFloat(DomeKey, 0f) ?? 0f) > 0f;
                domes.TryGetValue(id, out GameObject dome);
                if (active && dome == null) {
                    domes[id] = CreateDome(player.transform);
                } else if (!active && dome != null) {
                    Object.Destroy(dome);
                    domes.Remove(id);
                }
            }
            // Players who left.
            List<int> gone = new List<int>();
            foreach (KeyValuePair<int, GameObject> entry in domes) {
                if (!seen.Contains(entry.Key)) { gone.Add(entry.Key); }
            }
            foreach (int id in gone) {
                if (domes[id] != null) { Object.Destroy(domes[id]); }
                domes.Remove(id);
            }
        }

        // The Sunfire rune circle drawn at this scale covers the dome's reach.
        private const float DomeCircleRadius = 1.25f;

        private static GameObject CreateDome(Transform parent) {
            GameObject circle = DeityVfx.Attach(PowerFx.DeityOf(Power.RuneDome), VfxRole.Dome, parent, Vector3.up * 0.05f, Quaternion.identity);
            if (circle != null) {
                circle.transform.localScale = Vector3.one * (DomeRadius / DomeCircleRadius);
                return circle;
            }
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "AL_RuneDome";
            Object.DestroyImmediate(dome.GetComponent<Collider>());
            dome.transform.SetParent(parent, false);
            dome.transform.localPosition = Vector3.up * 0.8f;
            dome.transform.localScale = Vector3.one * DomeRadius * 2f;
            MeshRenderer renderer = dome.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = DeityVfx.GlowMaterial(Texture2D.whiteTexture, new Color(1f, 0.75f, 0.35f, 0.12f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return dome;
        }
    }
}
