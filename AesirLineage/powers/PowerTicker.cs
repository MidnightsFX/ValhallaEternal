using System;

namespace AesirLineage.powers {
    /// <summary>Runs the powers that work on a clock, from the plugin's Update and FixedUpdate, and clears their state when a character loads.</summary>
    internal static class PowerTicker {
        // One warning per kind of failure, so a broken power can't fill the log every frame.
        private static string lastFailure;

        internal static void Update() {
            Player player = Player.m_localPlayer;
            if (player == null) { return; }
            try {
                // Other players' domes are drawn whatever state the local player is in.
                AllyPowers.UpdateDomes();
                if (player.IsDead()) { return; }
                modules.Boons.Tick(player);
                modules.Deeds.Tick(player);
                modules.Quests.Tick(player);
                modules.ProgressionLock.Tick(player);
                DamagePowers.Tick();
                SurvivalPowers.Tick(player);
                AuraPowers.Tick(player);
                AllyPowers.Publish(player);
                SensePowers.Tick(player);
                KillPowers.Tick(player);
                PloughPowers.Tick();
                TamePowers.Tick(player);
                HomePowers.Tick(player);
                CraftPowers.Tick(player);
                SeaPowers.Tick(player);
                LightPowers.Tick(player);
                BondPowers.Tick(player);
                StrikePowers.Tick(player);
                ShoePowers.Tick(player);
                WinterPowers.Tick(player);
                WolfPowers.Tick(player);
                SilencePowers.Tick(player);
                AfterPowers.Tick(player);
            } catch (Exception e) {
                Warn(e);
            }
        }

        internal static void FixedUpdate() {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead()) { return; }
            try {
                MovementPowers.FixedTick(player);
            } catch (Exception e) {
                Warn(e);
            }
        }

        /// <summary>
        /// Forgets every timer, stack and pin from the last character, or from before a respawn. The death
        /// saves' cooldowns carry over a respawn, since dying is what would otherwise reset them.
        /// </summary>
        internal static void ResetAll(bool newCharacter) {
            DamagePowers.Reset();
            BlockPowers.Reset();
            SurvivalPowers.Reset(newCharacter);
            AuraPowers.Reset();
            AllyPowers.Reset(newCharacter);
            SensePowers.Reset();
            KillPowers.Reset();
            SkillPowers.Reset();
            MovementPowers.Reset();
            PloughPowers.Reset();
            TamePowers.Reset();
            HomePowers.Reset();
            CraftPowers.Reset();
            SeaPowers.Reset();
            LightPowers.Reset();
            BondPowers.Reset();
            StrikePowers.Reset();
            HuntPowers.Reset();
            MeadPowers.Reset();
            ShoePowers.Reset();
            WinterPowers.Reset();
            WolfPowers.Reset();
            SilencePowers.Reset();
            AfterPowers.Reset();
        }

        private static void Warn(Exception e) {
            string failure = e.GetType().Name + e.Message + e.TargetSite?.Name;
            if (failure == lastFailure) { return; }
            lastFailure = failure;
            Logger.LogWarning($"A power failed and was skipped this frame: {e}");
        }
    }
}
