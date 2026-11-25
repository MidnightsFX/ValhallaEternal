using UnityEngine;

namespace AesirLineage.ragnarok.bosses {
    // Hræsvelgr, the corpse-swallower whose wings make the winds: a huge storm-grey bat with a skull for a face,
    // built on the cave bat. It keeps calling bats to swarm whoever it hunts, bites hard up close, and from afar flings
    // gusts that hurl people off their feet.
    internal static class Hraesvelgr {
        // The gusts are pale, almost white.
        private static readonly Tint Wind = new Tint(0.55f, 0f, 0.12f);

        internal static void Build(GameObject prefab) {
            float scale = prefab.transform.localScale.x;
            Humanoid bat = prefab.GetComponent<Humanoid>();
            BossKit.Look(prefab, Color.black);
            BossKit.Attach(prefab, (BossKit.Attachment("Bat", "SFX_Wingsloop", null), "Chest"));
            bat.m_deathEffects = BossKit.WithCorpse(BossKit.Effects("fx_bat_death"), "Hraesvelgr", Color.black);
            BossKit.MakeHeavy(prefab, 300f);
            bat.m_health = 2500f;
            bat.m_flySlowSpeed = 6f;
            bat.m_flyFastSpeed = 11f;
            bat.m_flyTurnSpeed = 120f;

            MonsterAI ai = prefab.GetComponent<MonsterAI>();
            ai.m_viewRange = 50f;
            ai.m_alertRange = 40f;
            ai.m_flyAltitudeMin = 3f;
            ai.m_flyAltitudeMax = 10f;
            ai.m_circleTargetDistance = 8f;
            ai.m_randomMoveRange = 12f;

            GameObject brood = BossKit.Minion("AL_WorldBoss_Hraesvelgr_Brood", "Bat", 1.3f, null);
            bat.m_defaultItems = new[] {
                BossKit.Attack("AL_WorldBoss_Hraesvelgr_Bite", "bat_melee", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_slash = 45f, m_pierce = 25f };
                    shared.m_attackForce = 40f;
                    shared.m_aiAttackRange = 1.2f * scale;
                    shared.m_aiAttackInterval = 3f;
                    shared.m_aiAttackMaxAngle = 40f;
                    Attack attack = shared.m_attack;
                    attack.m_attackAnimation = "hraesvelgr_bite";
                    attack.m_attackRange = 1.5f * scale;
                    attack.m_attackHeight = 0.42f * scale;
                    attack.m_attackRayWidth = 0.4f * scale;
                }),
                BossKit.Attack("AL_WorldBoss_Hraesvelgr_Gale", "bat_melee", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_blunt = 30f, m_frost = 10f };
                    shared.m_attackForce = 160f;
                    shared.m_aiAttackRangeMin = 7f;
                    shared.m_aiAttackRange = 30f;
                    shared.m_aiAttackInterval = 5f;
                    shared.m_aiAttackMaxAngle = 20f;
                    shared.m_hitEffect = new EffectList();
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.Wind);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Projectile;
                    attack.m_attackAnimation = "hraesvelgr_gale";
                    attack.m_attackProjectile = BossKit.Projectile("AL_WorldBoss_Hraesvelgr_GaleProjectile", BossKit.Vfx.WindProjectile, Wind, 1f,
                        gravity: 0f, edit: gust => BossKit.AttachLook(gust, BossKit.Vfx.WindSwirl, 0.3f));
                    attack.m_projectileVel = 26f;
                    attack.m_projectileVelMin = 26f;
                    attack.m_projectileAccuracy = 6f;
                    attack.m_projectileAccuracyMin = 6f;
                    attack.m_projectiles = 3;
                    attack.m_attackRange = 1f * scale;
                    attack.m_attackHeight = 0.4f * scale;
                }),
                BossKit.Attack("AL_WorldBoss_Hraesvelgr_Call", "bat_melee", shared => {
                    shared.m_aiAttackRange = 40f;
                    shared.m_aiAttackInterval = 12f;
                    shared.m_aiAttackMaxAngle = 180f;
                    shared.m_aiPrioritized = true;
                    shared.m_hitEffect = new EffectList();
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.Screech);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Projectile;
                    attack.m_attackAnimation = "hraesvelgr_call";
                    attack.m_attackProjectile = BossKit.Summon("AL_WorldBoss_Hraesvelgr_CallSpawn", brood, 2, 3, 6, 5f,
                        new EffectList(), 0f, BossKit.Effects("vfx_ghost_hit", "sfx_bat_alerted"));
                    attack.m_projectiles = 1;
                }),
            };

            BossKit.SetDrops(prefab, ("LeatherScraps", 10, 15), ("Feathers", 8, 12), ("Coins", 30, 50));
        }
    }
}
