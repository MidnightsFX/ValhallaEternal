using UnityEngine;

namespace AesirLineage.ragnarok.bosses {
    // Móðguðr, warden of the bridge to Hel: a crowned skull in a violet shroud, built on the swamp crypts' ghost. She
    // fights from range: a scream that blasts everyone around her, fans of purple bolts, and skeletons raised to fight
    // for her.
    internal static class Modgud {
        private static readonly Tint Body = new Tint(0.78f, 0.35f);
        // Her shroud's faint glow.
        private static readonly Color Glow = Color.HSVToRGB(0.78f, 0.8f, 1f) * 0.5f;

        internal static void Build(GameObject prefab) {
            float scale = prefab.transform.localScale.x;
            Humanoid ghost = prefab.GetComponent<Humanoid>();
            BossKit.Look(prefab, Glow);
            BossKit.Attach(prefab, (BossKit.Attachment("Ghost", "black_smoke", Body.Glow), "Hips"));
            BossKit.MakeHeavy(prefab, 400f);
            ghost.m_health = 3000f;
            ghost.m_deathEffects = BossKit.WithCorpse(BossKit.Effects(BossKit.Recolored("AL_WorldBoss_Modgud_DeathFx", "vfx_ghost_death", Body.Glow, scale).name, "sfx_ghosts_death"), "Modgud", Glow);
            foreach (Light light in prefab.GetComponentsInChildren<Light>(true)) { light.range *= scale; }

            MonsterAI ai = prefab.GetComponent<MonsterAI>();
            ai.m_viewRange = 50f;
            ai.m_hearRange = 50f;
            ai.m_alertRange = 40f;
            // Keeps her moving between casts rather than standing still to be hit.
            ai.m_circulateWhileCharging = true;
            ai.m_randomMoveRange = 12f;
            ai.m_pathAgentType = Pathfinding.AgentType.GoblinBruteSize;

            GameObject thrall = BossKit.Minion("AL_WorldBoss_Modgud_Thrall", "Skeleton", 1.1f, minion => BossKit.Recolor(minion, Body));
            ghost.m_defaultItems = new[] {
                BossKit.Attack("AL_WorldBoss_Modgud_Scream", "Ghost_attack", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_spirit = 35f, m_blunt = 15f, m_frost = 15f };
                    shared.m_attackForce = 90f;
                    shared.m_aiAttackRange = 9f;
                    shared.m_aiAttackInterval = 9f;
                    shared.m_aiAttackMaxAngle = 180f;
                    // Her answer to anyone who comes close, whenever it is ready.
                    shared.m_aiPrioritized = true;
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.Scream, "sfx_wraith_alerted");
                    shared.m_triggerEffect = BossKit.Effects(BossKit.Recolored("AL_WorldBoss_Modgud_ScreamFx", BossKit.Vfx.Shockwave, Body.Glow, 1f).name);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Area;
                    attack.m_attackAnimation = "modgud_scream";
                    attack.m_attackRange = 0f;
                    attack.m_attackHeight = 1f * scale;
                    attack.m_attackRayWidth = 10f;
                }),
                BossKit.Attack("AL_WorldBoss_Modgud_Bolt", "Ghost_attack", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_spirit = 30f, m_frost = 20f };
                    shared.m_attackForce = 30f;
                    shared.m_aiAttackRange = 35f;
                    shared.m_aiAttackRangeMin = 0f;
                    shared.m_aiAttackInterval = 4f;
                    shared.m_aiAttackMaxAngle = 15f;
                    shared.m_hitEffect = new EffectList();
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.BoltLaunch);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Projectile;
                    attack.m_attackAnimation = "modgud_bolt";
                    attack.m_attackProjectile = BossKit.Projectile("AL_WorldBoss_Modgud_BoltProjectile", BossKit.Vfx.MagicProjectile, Body.Glow, 1.5f, gravity: 0f);
                    attack.m_projectileVel = 22f;
                    attack.m_projectileVelMin = 22f;
                    attack.m_projectileAccuracy = 7f;
                    attack.m_projectileAccuracyMin = 7f;
                    attack.m_projectiles = 3;
                    attack.m_attackRange = 1f * scale;
                    attack.m_attackHeight = 1.3f * scale;
                }),
                BossKit.Attack("AL_WorldBoss_Modgud_Raise", "Ghost_attack", shared => {
                    shared.m_aiAttackRange = 40f;
                    shared.m_aiAttackInterval = 25f;
                    shared.m_aiAttackMaxAngle = 180f;
                    shared.m_aiPrioritized = true;
                    shared.m_hitEffect = new EffectList();
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.Summon);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Projectile;
                    attack.m_attackAnimation = "modgud_raise";
                    attack.m_attackProjectile = BossKit.Summon("AL_WorldBoss_Modgud_RaiseSpawn", thrall, 2, 3, 5, 6f,
                        BossKit.Effects(BossKit.Recolored("AL_WorldBoss_Modgud_RaiseFx", BossKit.Vfx.SummonTelegraph, Body.Glow, 1f).name), 1.5f,
                        BossKit.Effects(BossKit.Vfx.SummonRise, BossKit.Sound.SummonRise));
                    attack.m_projectiles = 1;
                }),
            };

            BossKit.SetDrops(prefab, ("Ectoplasm", 10, 15), ("WitheredBone", 5, 8), ("Coins", 30, 50));
        }
    }
}
