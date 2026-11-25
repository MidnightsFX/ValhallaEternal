using UnityEngine;

namespace AesirLineage.ragnarok.bosses {
    // Sinmara, keeper of the fire that will burn the world: a horned brute of cracked black rock burning blue inside,
    // built on the surtling. She fights up close: bursts of fire in front of her, a nova that sets everything around
    // her alight, and a blink that drops her beside whoever keeps their distance, in a burst of flame.
    internal static class Sinmara {
        private static readonly Tint Body = new Tint(0.58f, 0.3f);
        // The fire in her cracks.
        private static readonly Color Glow = Color.HSVToRGB(0.56f, 0.75f, 1f) * 2.5f;

        internal static void Build(GameObject prefab) {
            float scale = prefab.transform.localScale.x;
            Humanoid surtling = prefab.GetComponent<Humanoid>();
            BossKit.Look(prefab, Glow);
            // The surtling's flames turned blue, and its own blue ones, which it never shows, burning over her body.
            BossKit.Attach(prefab,
                (BossKit.Attachment("Surtling", "flames", Body), "Spine2"),
                (BossKit.Attachment("Surtling", "flames_world_BLUE", Body), "Spine2"),
                (BossKit.Attachment("Surtling", "SFX", null), "Hips"));
            BossKit.MakeHeavy(prefab, 400f);
            foreach (Light light in prefab.GetComponentsInChildren<Light>(true)) { light.range *= scale; }

            surtling.m_health = 3500f;
            // A surtling dies in water; a boss that drowned in the first lake would end Ragnarok for nothing.
            surtling.m_tolerateWater = true;
            surtling.m_walkSpeed = 5f;
            surtling.m_runSpeed = 7f;
            surtling.m_deathEffects = BossKit.WithCorpse(BossKit.Effects(BossKit.Recolored("AL_WorldBoss_Sinmara_DeathFx", "vfx_ImpDeath", Body.Glow, scale).name, "sfx_imp_death"), "Sinmara", Glow);

            MonsterAI ai = prefab.GetComponent<MonsterAI>();
            ai.m_viewRange = 50f;
            ai.m_alertRange = 40f;
            ai.m_fleeIfHurtWhenTargetCantBeReached = false;
            ai.m_pathAgentType = Pathfinding.AgentType.GoblinBruteSize;

            // Brenna's nova is sized for 3m; each use is scaled to its own reach.
            GameObject slamFx = BossKit.Recolored("AL_WorldBoss_Sinmara_SlamFx", BossKit.Vfx.FireNova, Body.Glow, 1.2f);
            GameObject novaFx = BossKit.Recolored("AL_WorldBoss_Sinmara_NovaFx", BossKit.Vfx.FireNova, Body.Glow, 3f);
            GameObject landFx = BossKit.Recolored("AL_WorldBoss_Sinmara_LandFx", BossKit.Vfx.FireNova, Body.Glow, 1.7f);
            GameObject blinkFx = BossKit.Recolored("AL_WorldBoss_Sinmara_BlinkFx", BossKit.Vfx.Teleport, Body.Glow, 1.5f);
            GameObject landFlame = BossKit.Recolored("AL_WorldBoss_Sinmara_LandAoe", BossKit.Vfx.FireAoe, Body.Glow, 1f, aoe => {
                Aoe area = aoe.GetComponent<Aoe>();
                area.m_radius = 5f;
                area.m_hitOwner = false;
                area.m_hitSame = false;
            });
            surtling.m_defaultItems = new[] {
                BossKit.Attack("AL_WorldBoss_Sinmara_Slam", "imp_fireball_attack", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_fire = 45f, m_blunt = 25f };
                    shared.m_attackForce = 60f;
                    shared.m_aiAttackRange = 4.5f;
                    shared.m_aiAttackInterval = 2.5f;
                    shared.m_aiAttackMaxAngle = 25f;
                    shared.m_triggerEffect = BossKit.Effects(slamFx.name);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Area;
                    attack.m_attackAnimation = "sinmara_burst";
                    attack.m_attackProjectile = null;
                    attack.m_attackRange = 2.5f;
                    attack.m_attackHeight = 0.5f;
                    attack.m_attackRayWidth = 3.5f;
                }),
                BossKit.Attack("AL_WorldBoss_Sinmara_Nova", "imp_fireball_attack", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_fire = 60f, m_blunt = 20f };
                    shared.m_attackForce = 110f;
                    shared.m_aiAttackRange = 8f;
                    shared.m_aiAttackInterval = 11f;
                    shared.m_aiAttackMaxAngle = 180f;
                    shared.m_startEffect = BossKit.Effects(BossKit.Sound.FireCharge);
                    shared.m_triggerEffect = BossKit.Effects(novaFx.name, BossKit.Sound.FireNova);
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Area;
                    attack.m_attackAnimation = "sinmara_nova";
                    attack.m_attackProjectile = null;
                    attack.m_attackRange = 0f;
                    attack.m_attackHeight = 0.5f;
                    attack.m_attackRayWidth = 9f;
                }),
                BossKit.Attack("AL_WorldBoss_Sinmara_Blink", "imp_fireball_attack", shared => {
                    shared.m_damages = new HitData.DamageTypes() { m_fire = 40f, m_blunt = 10f };
                    shared.m_attackForce = 50f;
                    // Only for someone keeping their distance, and whenever it is ready then.
                    shared.m_aiAttackRangeMin = 9f;
                    shared.m_aiAttackRange = 45f;
                    shared.m_aiAttackInterval = 8f;
                    shared.m_aiAttackMaxAngle = 180f;
                    shared.m_aiPrioritized = true;
                    shared.m_startEffect = new EffectList();
                    shared.m_hitEffect = new EffectList();
                    Attack attack = shared.m_attack;
                    attack.m_attackType = Attack.AttackType.Projectile;
                    attack.m_attackAnimation = "sinmara_blink";
                    attack.m_attackProjectile = BossKit.Blink("AL_WorldBoss_Sinmara_BlinkCast",
                        BossKit.Effects(blinkFx.name, BossKit.Sound.Teleport),
                        BossKit.Effects(blinkFx.name, landFx.name, BossKit.Sound.Teleport),
                        landFlame);
                }),
            };

            BossKit.SetDrops(prefab, ("SurtlingCore", 4, 6), ("Coal", 10, 15), ("Coins", 30, 50));
        }
    }
}
