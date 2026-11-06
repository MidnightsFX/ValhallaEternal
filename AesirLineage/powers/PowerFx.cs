using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.ui;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.powers {
    /// <summary>
    /// Helpers the powers share: dealing extra hits without them triggering on-hit powers again,
    /// finding enemies and allies nearby, and the status effects that show a timed power on the HUD.
    /// </summary>
    internal static class PowerFx {
        // Set while a power deals its own hit, so DamagePowers.Outgoing doesn't treat it as a weapon hit.
        internal static bool DealingSecondary;

        private static readonly List<Character> nearbyCharacters = new List<Character>();
        private static readonly List<Player> nearbyPlayers = new List<Player>();

        /// <summary>Living, untamed, non-player characters within range.</summary>
        internal static List<Character> EnemiesNear(Vector3 point, float radius, Character except = null) {
            nearbyCharacters.Clear();
            Character.GetCharactersInRange(point, radius, nearbyCharacters);
            nearbyCharacters.RemoveAll(c => c == null || c == except || c.IsPlayer() || c.IsTamed() || c.IsDead());
            return nearbyCharacters;
        }

        /// <summary>Other living players within range, not counting the local one.</summary>
        internal static List<Player> AlliesNear(Vector3 point, float radius) {
            nearbyPlayers.Clear();
            Player.GetPlayersInRange(point, radius, nearbyPlayers);
            nearbyPlayers.RemoveAll(p => p == null || p == Player.m_localPlayer || p.IsDead());
            return nearbyPlayers;
        }

        /// <summary>Hits a character as the local player with the given damage, outside the on-hit powers.</summary>
        internal static void Strike(Character target, HitData.DamageTypes damage, Vector3 from, float pushForce = 0f) {
            Player local = Player.m_localPlayer;
            if (target == null || local == null || target.IsDead()) { return; }
            HitData hit = new HitData() {
                m_damage = damage,
                m_point = target.GetCenterPoint(),
                m_dir = (target.transform.position - from).normalized,
                m_pushForce = pushForce,
                m_hitType = HitData.HitType.PlayerHit,
            };
            hit.SetAttacker(local);
            DealingSecondary = true;
            try {
                target.Damage(hit);
            } finally {
                DealingSecondary = false;
            }
        }

        /// <summary>
        /// Plays a power's effect in the look of the deity it came from, so a build reads at a glance.
        /// Shared effects are seen by every player nearby; keep those to what happens a few times a minute.
        /// </summary>
        internal static void Effect(Power power, VfxRole role, Vector3 position, float scale = 1f, bool shared = false) {
            Deity deity = DeityOf(power);
            if (shared) {
                DeityVfx.Share(deity, role, position, scale);
            } else {
                DeityVfx.Play(deity, role, position, scale);
            }
        }

        private static readonly Dictionary<Power, Deity> owners = new Dictionary<Power, Deity>();
        private static int ownersRevision = -1;

        /// <summary>
        /// The deity behind a power: the first allocated star granting it, or else the first map that offers
        /// it. Several deities share some powers (Fortification, poison immunity, armor).
        /// </summary>
        internal static Deity DeityOf(Power power) {
            IReadOnlyList<StarNode> sources = PowerSet.SourcesOf(power);
            if (sources.Count > 0) { return sources[0].Deity; }
            if (ownersRevision != StarMaps.Revision) {
                owners.Clear();
                foreach (StarMap map in StarMaps.All) {
                    foreach (StarNode node in map.Nodes.Values) {
                        foreach (KeyValuePair<Power, float> grant in node.Grants) {
                            if (!owners.ContainsKey(grant.Key)) { owners[grant.Key] = map.Deity; }
                        }
                    }
                }
                ownersRevision = StarMaps.Revision;
            }
            return owners.TryGetValue(power, out Deity owner) ? owner : Deity.Gefjun;
        }

        // ---- Status effects that show timed powers on the HUD ----

        internal const string Fallen = "AL_Fallen";
        internal const string Voluspa = "AL_Voluspa";
        internal const string Bargain = "AL_Bargain";
        internal const string Invincible = "AL_Invincible";
        internal const string Silent = "AL_Silent";
        internal const string Retort = "AL_Retort";
        internal const string Charged = "AL_Charged";
        internal const string Fortified = "AL_Fortified";
        /// <summary>Applied to enemies: no movement for a few seconds.</summary>
        internal const string Rooted = "AL_Rooted";
        /// <summary>Applied to enemies: half speed for a few seconds.</summary>
        internal const string Slowed = "AL_Slowed";
        internal const string Elixir = "AL_Elixir";
        /// <summary>The rainbow's blessing: its regen is the status effect's own.</summary>
        internal const string Bifrost = "AL_Bifrost";
        internal const string Longing = "AL_Longing";
        internal const string Terror = "AL_Terror";
        internal const string SoulEater = "AL_SoulEater";
        /// <summary>Applied to enemies: held under Víðarr's foot, taking more from every hit.</summary>
        internal const string Pinned = "AL_Pinned";
        internal const string PinnedBrief = "AL_PinnedBrief";
        /// <summary>Applied to enemies in Fimbulvetr's blizzard: slower, and their hits softer.</summary>
        internal const string Chilled = "AL_Chilled";
        internal const string Winters = "AL_Winters";
        internal const string Debt = "AL_Debt";
        internal const string Golden = "AL_Golden";

        private static readonly Dictionary<string, int> hashes = new Dictionary<string, int>();

        /// <summary>
        /// Registers the display effects, each with its deity's rune as the icon. The powers keep their own
        /// timers; these only show the player what is active.
        /// </summary>
        internal static void RegisterStatusEffects() {
            Register(Fallen, "ansuz", 20f);
            Register(Voluspa, "ansuz", 10f);
            Register(Bargain, "hagalaz", 15f);
            Register(Invincible, "sowilo", 3f);
            Register(Silent, "hagalaz", 5f);
            Register(Retort, "hagalaz", 5f);
            Register(Charged, "fehu", 10f);
            Register(Fortified, "gebo", 10f);
            Register(Rooted, "gebo", 3f, speedModifier: -1f);
            Register(Slowed, "algiz", 3f, speedModifier: -0.5f);
            Register(Elixir, "fehu", 10f);
            Register(Bifrost, "sowilo", 300f, healthRegen: 1.2f, staminaRegen: 1.3f);
            Register(Longing, "fehu", 20f);
            Register(Terror, "hagalaz", 10f);
            Register(SoulEater, "hagalaz", 10f);
            Register(Pinned, "eihwaz", 3f, speedModifier: -1f);
            Register(PinnedBrief, "eihwaz", 1f, speedModifier: -1f);
            Register(Chilled, "eihwaz", 2f, speedModifier: -0.2f, damageModifier: 0.85f);
            Register(Winters, "eihwaz", 60f);
            Register(Debt, "eihwaz", 10f);
            Register(Golden, "eihwaz", 600f, staminaRegen: 1.2f);
        }

        internal static void Register(string name, string rune, float seconds, float speedModifier = 0f, float healthRegen = 1f, float staminaRegen = 1f, float damageModifier = 1f) {
            SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = name;
            effect.m_name = $"$al_se_{name}";
            effect.m_ttl = seconds;
            // SE_Stats adds base speed times this: -1 stops the character.
            effect.m_speedModifier = speedModifier;
            effect.m_healthRegenMultiplier = healthRegen;
            effect.m_staminaRegenMultiplier = staminaRegen;
            if (damageModifier != 1f) {
                effect.m_modifyAttackSkill = Skills.SkillType.All;
                effect.m_damageModifier = damageModifier;
            }
            Texture2D icon = RuneArt.Texture(rune);
            if (icon != null) {
                effect.m_icon = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
            }
            CustomStatusEffect custom = new CustomStatusEffect(effect, fixReference: false);
            ItemManager.Instance.AddStatusEffect(custom);
            hashes[name] = custom.StatusEffect.NameHash();
        }

        /// <summary>Shows (or refreshes) a timed power on a character; the time is taken from the call.</summary>
        internal static void Show(Character character, string name, float seconds) {
            if (character == null || !hashes.TryGetValue(name, out int hash)) { return; }
            StatusEffect shown = character.GetSEMan().AddStatusEffect(hash, resetTime: true);
            if (shown != null) { shown.m_ttl = seconds; }
        }

        internal static void Hide(Character character, string name) {
            if (character == null || !hashes.TryGetValue(name, out int hash)) { return; }
            character.GetSEMan().RemoveStatusEffect(hash, quiet: true);
        }

        internal static int Hash(string name) => hashes.TryGetValue(name, out int hash) ? hash : 0;

        internal static bool Has(Character character, string name) => character != null && hashes.TryGetValue(name, out int hash) && character.GetSEMan().HaveStatusEffect(hash);
    }
}
