namespace AesirLineage.starmap
{
    /// <summary>
    /// Every effect a star map node can grant. Oaths are powers too: the drawback that comes with a node.
    /// Values from all allocated nodes are summed, then capped (see <see cref="PowerInfo"/>).
    /// </summary>
    /// <remarks>
    /// Star map files refer to powers by name, so renaming one breaks existing files. Add new ones at the
    /// end of their group; the numeric values are never saved.
    /// </remarks>
    public enum Power
    {
        // Blessings
        HarvestBounty,
        FishingProsperity,
        FoodBonus,
        Armor,
        CarryWeight,
        SkillXp,
        SkillEcho,
        BladeboundKnowledge,
        RecipeInsight,
        KnowledgeIsPower,
        UndeadDamage,
        FireAndPoisonResist,
        BlockPower,
        ArrowCatcher,
        StormRage,
        ShadowSpeed,
        ArrowScavenge,
        StaggerThreshold,
        StaggeredDamageReduction,
        TowerBlockPower,
        LootWealth,
        MeleeDamage,
        RangedDamage,
        AllDamage,
        ExecuteDamage,
        FrostResist,
        EitrBase,
        EitrRegen,
        StaminaRegen,
        HealthRegen,
        PerfectForm,
        HealOnKill,
        HeatResistance,
        StaminaWhenHit,
        EitrWhenHit,
        SpeedOnKill,

        // Phase 4: Gefjun
        FarmingXp,
        SeedsOfTheGods,
        GatherYield,
        FoodDuration,
        HeartyMeal,
        FeastShare,
        BlockStamina,
        Shelter,
        Fortification,
        CheatDeath,
        FurrowSlam,
        HearthAura,
        MothersWard,
        // Vör
        NonCombatSkillXp,
        Eureka,
        ExploreRadius,
        Stealth,
        OreSense,
        SeeressMap,
        Tracks,
        RavensEye,
        CritChance,
        CritDamage,
        LearnedFromFallen,
        CertainBlow,
        Foresight,
        Unveiling,
        Voluspa,
        // Baldur
        SpiritDamage,
        Sunstrike,
        CleansingFlame,
        WetResist,
        CleanParry,
        PoisonImmunity,
        ParryWindow,
        ReturnedLight,
        AllThings,
        Invincible,
        Dawnbringer,
        BrightRetort,
        // Skaði
        BowDamage,
        DrawSpeed,
        PiercingShot,
        FieldDressing,
        ExtraArrows,
        FrostDamage,
        Shatter,
        ColdImmunity,
        JumpHeight,
        RunStamina,
        SureFooted,
        Skis,
        Leap,
        DoubleJump,
        HangOnTheWind,
        WintersShatter,
        WolfSister,
        // Syn
        Footing,
        Unstaggerable,
        BrutalDefiance,
        SealedWall,
        ThornsOnBlock,
        StrengthOfValkyries,
        Challenge,
        RuneDome,
        StoneSkin,
        LastDoor,
        // Freyja
        MagicDamage,
        SpellEcho,
        StormWeave,
        BloodSeidr,
        AttackStamina,
        BattleFury,
        DodgeStamina,
        JumpStamina,
        FalconGlide,
        Beloved,
        BattleMedic,
        ChargedGrace,
        Folkvangr,
        Brisingamen,
        FalconForm,
        // Hel
        FireResist,
        Retort,
        Wildfire,
        PyreOfHelheim,
        LifeLeech,
        StoredForLater,
        DeathsBargain,
        SilentPassing,
        OpenWound,
        SpiritImmunity,
        Naglfar,
        QueensDecree,

        // Second three arms: Gefjun
        TamingSpeed,
        TameGuard,
        FruitfulHerd,
        DraughtOx,
        HerdFed,
        FourOxen,
        RestedDuration,
        Comfort,
        StationSpeed,
        HearthStrength,
        Hermit,
        Swarm,
        ChopDamage,
        MineDamage,
        ToolStamina,
        PathSpeed,
        OverCarry,
        LoadStrength,
        // Vör
        DurabilityLoss,
        MaxDurability,
        CraftRefund,
        Masterwork,
        WeaponLore,
        SneakAttack,
        Weakness,
        QuietKill,
        FirstCut,
        ReadTheFight,
        VowKeeper,
        OathEase,
        Grudge,
        VowWitness,
        OathRing,
        // Baldur
        HealingReceived,
        Sanctuary,
        Mercy,
        SafeHaven,
        Selfless,
        AllThingsWept,
        SailSpeed,
        SwimStamina,
        SwimSpeed,
        SteadyKeel,
        FairWind,
        ShipCaller,
        DaylightDamage,
        DaylightRegen,
        Torchbearer,
        MistBurner,
        Bifrost,
        RadiantJudgment,
        // Skaði
        HideYield,
        TrophyChance,
        MeatFood,
        CallOfTheHunt,
        TrophyLore,
        SpearDamage,
        ThrowSpeed,
        ReturningSpear,
        PinningThrow,
        SpearPierce,
        WinterSpear,
        CompanionDamage,
        CompanionGuard,
        PackTactics,
        LickWounds,
        EndlessPack,
        // Syn
        HallWard,
        RaidDefender,
        MendWalls,
        BarredHall,
        BanTheKind,
        PolearmDamage,
        PolearmStamina,
        SetAgainstCharge,
        SweepFortify,
        WallOfSpears,
        HoldTheGate,
        Refuse,
        ArmourBreak,
        Interrupt,
        Reflect,
        Verdict,
        // Freyja
        MeadDuration,
        MeadStrength,
        MeadCooldown,
        MeadSave,
        TwiceBrewed,
        Elixir,
        BondDamage,
        BondRegen,
        Bonded,
        SharedBurden,
        SeidrLink,
        Inseparable,
        Longing,
        TradePrice,
        CoinFind,
        Gersemi,
        Hoard,
        GoldHunger,
        RedGoldTears,
        // Hel
        PoisonOnHit,
        PoisonDamage,
        Contagion,
        GraspOfTheDead,
        PoisonDrain,
        RottingGround,
        SummonDamage,
        SummonDuration,
        AscendantThralls,
        ThrallsBurden,
        Legion,
        SoulEater,
        NightDamage,
        NightStealth,
        Dread,
        HelsGaze,
        ChildOfNight,
        Terror,

        // Víðarr
        UnarmedDamage,
        SnowStride,
        HeelStamp,
        IronGloves,
        ShoeOfScraps,
        FootOnTheJaw,
        ColdEndurance,
        SnowShed,
        ThreeWinters,
        WinterBound,
        Fimbulvetr,
        GreatFoeDamage,
        Unbound,
        LongFight,
        WolfSworn,
        TearTheJaws,
        WrathDamage,
        BloodDebt,
        DebtHeal,
        KinVengeance,
        Unatoned,
        Avenger,
        Patience,
        Stillness,
        OneWord,
        SilenceVow,
        SingleBlow,
        Relearn,
        LastStand,
        KindDeath,
        Outlast,
        GoldenPieces,

        // Oaths
        DamageTaken,
        LessDamageDealt,
        LessBluntDamage,
        LessPierceDamage,
        LessSlashDamage,
        SkillGainPenalty,
        CombatSkillGainPenalty,
        DeathSkillLoss,
        MaxHealthPenalty,
        MaxStaminaPenalty,
        MaxEitrPenalty,
        CarryWeightPenalty,
        FoodDurationPenalty,
        NoBows,
        NoCrossbows,
        // Phase 4
        MoveSpeedPenalty,
        StaminaRegenPenalty,
        EitrRegenPenalty,
        HealthRegenPenalty,
        NightDamagePenalty,
        NoPoisonDealt,
        PierceDamageTaken,
        FireDamageTaken,
        NoSneak,
        BowDamagePenalty,
        DrawSpeedPenalty,
        MeleeDamagePenalty,
        ArmorPenalty,
        // Second three arms
        CraftCostPenalty,
        ForeignGearPenalty,
        AlertedDamagePenalty,
        NightDamageTaken,
        NoBlock,
        UnbannedDamagePenalty,
        DodgeStaminaPenalty,
        AloneDamagePenalty,
        LoseCoinsOnDeath,
        FoodValuePenalty,
        DayDamagePenalty,
        // Víðarr
        ArmedDamagePenalty,
        WarmDamagePenalty,
        LesserFoeDamagePenalty,
        ImpatiencePenalty,
    }

    public enum PowerKind
    {
        /// <summary>A plain number that works from any node.</summary>
        Stat,
        /// <summary>
        /// A mechanic: minors can feed its value, but it only works once a major, oath-bound or apex node
        /// grants it. Until then the fed value is dormant.
        /// </summary>
        Mechanic,
        /// <summary>On or off: any value above zero turns it on.</summary>
        Flag,
    }

    public sealed class PowerInfo
    {
        public readonly Power Id;
        public readonly PowerKind Kind;
        public readonly bool IsOath;
        public readonly float Cap;

        private PowerInfo(Power id, PowerKind kind, float cap, bool isOath = false)
        {
            Id = id;
            Kind = kind;
            Cap = cap;
            IsOath = isOath;
        }

        public string NameKey => $"$al_power_{Id}";
        public string DescriptionKey => $"$al_power_{Id}_desc";
        /// <summary>How the power works, in a few sentences, shown under its value on the star map.</summary>
        public string DetailsKey => $"$al_power_{Id}_details";

        public static readonly int Count = System.Enum.GetValues(typeof(Power)).Length;

        public static PowerInfo Of(Power power) => All[(int)power];

        // Indexed by (int)Power; the static check in StarMaps.Setup confirms every power has an entry.
        // Caps are on the summed value. Percentages are in percent (15 = 15%).
        internal static readonly PowerInfo[] All = Build();

        private static PowerInfo[] Build()
        {
            PowerInfo[] all = new PowerInfo[Count];
            void Add(PowerInfo info) { all[(int)info.Id] = info; }

            Add(new PowerInfo(Power.HarvestBounty, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.FishingProsperity, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.FoodBonus, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.Armor, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.CarryWeight, PowerKind.Stat, 300));
            Add(new PowerInfo(Power.SkillXp, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SkillEcho, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.BladeboundKnowledge, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.RecipeInsight, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.KnowledgeIsPower, PowerKind.Mechanic, 25));
            Add(new PowerInfo(Power.UndeadDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.FireAndPoisonResist, PowerKind.Stat, 60));
            Add(new PowerInfo(Power.BlockPower, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.ArrowCatcher, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.StormRage, PowerKind.Mechanic, 30));
            Add(new PowerInfo(Power.ShadowSpeed, PowerKind.Mechanic, 25));
            Add(new PowerInfo(Power.ArrowScavenge, PowerKind.Mechanic, 25));
            Add(new PowerInfo(Power.StaggerThreshold, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.StaggeredDamageReduction, PowerKind.Stat, 60));
            Add(new PowerInfo(Power.TowerBlockPower, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.LootWealth, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.MeleeDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.RangedDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.AllDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.ExecuteDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.FrostResist, PowerKind.Stat, 60));
            Add(new PowerInfo(Power.EitrBase, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.EitrRegen, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.StaminaRegen, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.HealthRegen, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.PerfectForm, PowerKind.Mechanic, 200));
            Add(new PowerInfo(Power.HealOnKill, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.HeatResistance, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.StaminaWhenHit, PowerKind.Stat, 20));
            Add(new PowerInfo(Power.EitrWhenHit, PowerKind.Stat, 20));
            Add(new PowerInfo(Power.SpeedOnKill, PowerKind.Mechanic, 30));

            // Gefjun
            Add(new PowerInfo(Power.FarmingXp, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SeedsOfTheGods, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.GatherYield, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.FoodDuration, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.HeartyMeal, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.FeastShare, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.BlockStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.Shelter, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Fortification, PowerKind.Mechanic, 6));
            Add(new PowerInfo(Power.CheatDeath, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.FurrowSlam, PowerKind.Mechanic, 300));
            Add(new PowerInfo(Power.HearthAura, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.MothersWard, PowerKind.Flag, 1));
            // Vör
            Add(new PowerInfo(Power.NonCombatSkillXp, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Eureka, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.ExploreRadius, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.Stealth, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.OreSense, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.SeeressMap, PowerKind.Mechanic, 150));
            Add(new PowerInfo(Power.Tracks, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.RavensEye, PowerKind.Mechanic, 500));
            Add(new PowerInfo(Power.CritChance, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.CritDamage, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.LearnedFromFallen, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.CertainBlow, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Foresight, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Unveiling, PowerKind.Mechanic, 5));
            Add(new PowerInfo(Power.Voluspa, PowerKind.Mechanic, 50));
            // Baldur
            Add(new PowerInfo(Power.SpiritDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.Sunstrike, PowerKind.Mechanic, 200));
            Add(new PowerInfo(Power.CleansingFlame, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.WetResist, PowerKind.Stat, 90));
            Add(new PowerInfo(Power.CleanParry, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.PoisonImmunity, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ParryWindow, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.ReturnedLight, PowerKind.Mechanic, 200));
            Add(new PowerInfo(Power.AllThings, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Invincible, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Dawnbringer, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.BrightRetort, PowerKind.Flag, 1));
            // Skaði
            Add(new PowerInfo(Power.BowDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.DrawSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.PiercingShot, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.FieldDressing, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ExtraArrows, PowerKind.Mechanic, 4));
            Add(new PowerInfo(Power.FrostDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.Shatter, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.ColdImmunity, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.JumpHeight, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.RunStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.SureFooted, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Skis, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.Leap, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.DoubleJump, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.HangOnTheWind, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.WintersShatter, PowerKind.Mechanic, 150));
            Add(new PowerInfo(Power.WolfSister, PowerKind.Flag, 1));
            // Syn
            Add(new PowerInfo(Power.Footing, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Unstaggerable, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.BrutalDefiance, PowerKind.Mechanic, 300));
            Add(new PowerInfo(Power.SealedWall, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.ThornsOnBlock, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.StrengthOfValkyries, PowerKind.Mechanic, 25));
            Add(new PowerInfo(Power.Challenge, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.RuneDome, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.StoneSkin, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.LastDoor, PowerKind.Flag, 1));
            // Freyja
            Add(new PowerInfo(Power.MagicDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SpellEcho, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.StormWeave, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.BloodSeidr, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.AttackStamina, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.BattleFury, PowerKind.Mechanic, 3));
            Add(new PowerInfo(Power.DodgeStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.JumpStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.FalconGlide, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Beloved, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.BattleMedic, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.ChargedGrace, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.Folkvangr, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.Brisingamen, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.FalconForm, PowerKind.Flag, 1));
            // Hel
            Add(new PowerInfo(Power.FireResist, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.Retort, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Wildfire, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.PyreOfHelheim, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.LifeLeech, PowerKind.Stat, 10));
            Add(new PowerInfo(Power.StoredForLater, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.DeathsBargain, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.SilentPassing, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.OpenWound, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.SpiritImmunity, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Naglfar, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.QueensDecree, PowerKind.Mechanic, 25));

            // Second three arms: Gefjun
            Add(new PowerInfo(Power.TamingSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.TameGuard, PowerKind.Stat, 60));
            Add(new PowerInfo(Power.FruitfulHerd, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.DraughtOx, PowerKind.Mechanic, 60));
            Add(new PowerInfo(Power.HerdFed, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.FourOxen, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.RestedDuration, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.Comfort, PowerKind.Stat, 5));
            Add(new PowerInfo(Power.StationSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.HearthStrength, PowerKind.Mechanic, 2));
            Add(new PowerInfo(Power.Hermit, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Swarm, PowerKind.Mechanic, 30));
            Add(new PowerInfo(Power.ChopDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.MineDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.ToolStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.PathSpeed, PowerKind.Mechanic, 30));
            Add(new PowerInfo(Power.OverCarry, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.LoadStrength, PowerKind.Mechanic, 3));
            // Vör
            Add(new PowerInfo(Power.DurabilityLoss, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.MaxDurability, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.CraftRefund, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Masterwork, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.WeaponLore, PowerKind.Mechanic, 3));
            Add(new PowerInfo(Power.SneakAttack, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.Weakness, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.QuietKill, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.FirstCut, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ReadTheFight, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.VowKeeper, PowerKind.Mechanic, 10));
            Add(new PowerInfo(Power.OathEase, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.Grudge, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.VowWitness, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.OathRing, PowerKind.Flag, 1));
            // Baldur
            Add(new PowerInfo(Power.HealingReceived, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Sanctuary, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Mercy, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.SafeHaven, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.Selfless, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.AllThingsWept, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.SailSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SwimStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.SwimSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SteadyKeel, PowerKind.Mechanic, 90));
            Add(new PowerInfo(Power.FairWind, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ShipCaller, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.DaylightDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.DaylightRegen, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Torchbearer, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.MistBurner, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.Bifrost, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.RadiantJudgment, PowerKind.Mechanic, 150));
            // Skaði
            Add(new PowerInfo(Power.HideYield, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.TrophyChance, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.MeatFood, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.CallOfTheHunt, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.TrophyLore, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.SpearDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.ThrowSpeed, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.ReturningSpear, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.PinningThrow, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.SpearPierce, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.WinterSpear, PowerKind.Mechanic, 150));
            Add(new PowerInfo(Power.CompanionDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.CompanionGuard, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.PackTactics, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.LickWounds, PowerKind.Mechanic, 10));
            Add(new PowerInfo(Power.EndlessPack, PowerKind.Flag, 1));
            // Syn
            Add(new PowerInfo(Power.HallWard, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.RaidDefender, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.MendWalls, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.BarredHall, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.BanTheKind, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.PolearmDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.PolearmStamina, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.SetAgainstCharge, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.SweepFortify, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.WallOfSpears, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.HoldTheGate, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Refuse, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.ArmourBreak, PowerKind.Mechanic, 10));
            Add(new PowerInfo(Power.Interrupt, PowerKind.Mechanic, 150));
            Add(new PowerInfo(Power.Reflect, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Verdict, PowerKind.Mechanic, 50));
            // Freyja
            Add(new PowerInfo(Power.MeadDuration, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.MeadStrength, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.MeadCooldown, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.MeadSave, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.TwiceBrewed, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Elixir, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.BondDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.BondRegen, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Bonded, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.SharedBurden, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.SeidrLink, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.Inseparable, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Longing, PowerKind.Mechanic, 100));
            Add(new PowerInfo(Power.TradePrice, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.CoinFind, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Gersemi, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.Hoard, PowerKind.Mechanic, 3));
            Add(new PowerInfo(Power.GoldHunger, PowerKind.Mechanic, 3));
            Add(new PowerInfo(Power.RedGoldTears, PowerKind.Mechanic, 75));
            // Hel
            Add(new PowerInfo(Power.PoisonOnHit, PowerKind.Stat, 30));
            Add(new PowerInfo(Power.PoisonDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Contagion, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.GraspOfTheDead, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.PoisonDrain, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.RottingGround, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.SummonDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SummonDuration, PowerKind.Stat, 200));
            Add(new PowerInfo(Power.AscendantThralls, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ThrallsBurden, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Legion, PowerKind.Mechanic, 4));
            Add(new PowerInfo(Power.SoulEater, PowerKind.Mechanic, 5));
            Add(new PowerInfo(Power.NightDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.NightStealth, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.Dread, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.HelsGaze, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.ChildOfNight, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Terror, PowerKind.Mechanic, 5));
            // Víðarr
            Add(new PowerInfo(Power.UnarmedDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.SnowStride, PowerKind.Stat, 90));
            Add(new PowerInfo(Power.HeelStamp, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.IronGloves, PowerKind.Mechanic, 60));
            Add(new PowerInfo(Power.ShoeOfScraps, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.FootOnTheJaw, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.ColdEndurance, PowerKind.Stat, 90));
            Add(new PowerInfo(Power.SnowShed, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.ThreeWinters, PowerKind.Mechanic, 6));
            Add(new PowerInfo(Power.WinterBound, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Fimbulvetr, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.GreatFoeDamage, PowerKind.Stat, 100));
            Add(new PowerInfo(Power.Unbound, PowerKind.Stat, 75));
            Add(new PowerInfo(Power.LongFight, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.WolfSworn, PowerKind.Mechanic, 60));
            Add(new PowerInfo(Power.TearTheJaws, PowerKind.Mechanic, 20));
            Add(new PowerInfo(Power.WrathDamage, PowerKind.Stat, 50));
            Add(new PowerInfo(Power.BloodDebt, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.DebtHeal, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.KinVengeance, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.Unatoned, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.Avenger, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Patience, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.Stillness, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.OneWord, PowerKind.Mechanic, 150));
            Add(new PowerInfo(Power.SilenceVow, PowerKind.Mechanic, 40));
            Add(new PowerInfo(Power.SingleBlow, PowerKind.Flag, 1));
            Add(new PowerInfo(Power.Relearn, PowerKind.Stat, 150));
            Add(new PowerInfo(Power.LastStand, PowerKind.Mechanic, 50));
            Add(new PowerInfo(Power.KindDeath, PowerKind.Mechanic, 75));
            Add(new PowerInfo(Power.Outlast, PowerKind.Mechanic, 5));
            Add(new PowerInfo(Power.GoldenPieces, PowerKind.Flag, 1));

            Add(new PowerInfo(Power.DamageTaken, PowerKind.Stat, 100, isOath: true));
            Add(new PowerInfo(Power.LessDamageDealt, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.LessBluntDamage, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.LessPierceDamage, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.LessSlashDamage, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.SkillGainPenalty, PowerKind.Stat, 90, isOath: true));
            Add(new PowerInfo(Power.CombatSkillGainPenalty, PowerKind.Stat, 90, isOath: true));
            Add(new PowerInfo(Power.DeathSkillLoss, PowerKind.Stat, 200, isOath: true));
            Add(new PowerInfo(Power.MaxHealthPenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.MaxStaminaPenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.MaxEitrPenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.CarryWeightPenalty, PowerKind.Stat, 300, isOath: true));
            Add(new PowerInfo(Power.FoodDurationPenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.NoBows, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.NoCrossbows, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.MoveSpeedPenalty, PowerKind.Stat, 50, isOath: true));
            Add(new PowerInfo(Power.StaminaRegenPenalty, PowerKind.Stat, 90, isOath: true));
            Add(new PowerInfo(Power.EitrRegenPenalty, PowerKind.Stat, 90, isOath: true));
            Add(new PowerInfo(Power.HealthRegenPenalty, PowerKind.Stat, 100, isOath: true));
            Add(new PowerInfo(Power.NightDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.NoPoisonDealt, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.PierceDamageTaken, PowerKind.Stat, 200, isOath: true));
            Add(new PowerInfo(Power.FireDamageTaken, PowerKind.Stat, 200, isOath: true));
            Add(new PowerInfo(Power.NoSneak, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.BowDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.DrawSpeedPenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.MeleeDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.ArmorPenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.CraftCostPenalty, PowerKind.Stat, 100, isOath: true));
            Add(new PowerInfo(Power.ForeignGearPenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.AlertedDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.NightDamageTaken, PowerKind.Stat, 100, isOath: true));
            Add(new PowerInfo(Power.NoBlock, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.UnbannedDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.DodgeStaminaPenalty, PowerKind.Stat, 200, isOath: true));
            Add(new PowerInfo(Power.AloneDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.LoseCoinsOnDeath, PowerKind.Flag, 1, isOath: true));
            Add(new PowerInfo(Power.FoodValuePenalty, PowerKind.Stat, 75, isOath: true));
            Add(new PowerInfo(Power.DayDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.ArmedDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.WarmDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.LesserFoeDamagePenalty, PowerKind.Stat, 80, isOath: true));
            Add(new PowerInfo(Power.ImpatiencePenalty, PowerKind.Stat, 80, isOath: true));
            return all;
        }
    }
}
