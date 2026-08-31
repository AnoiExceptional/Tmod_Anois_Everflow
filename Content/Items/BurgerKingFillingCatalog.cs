using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace everflow.Content.Items
{
    [Flags]
    internal enum BurgerKingFillingCategory
    {
        None = 0,
        Classic = 1 << 0,
        DarkCuisine = 1 << 1,
        Vegetarian = 1 << 2,
        Carnivore = 1 << 3,
        Condiment = 1 << 4
    }

    internal enum BurgerKingFillingDebuff
    {
        None,
        Poisoned,
        CursedInferno,
        Hellfire,
        Ichor
    }

    internal enum BurgerKingUnlockCondition
    {
        SlimeKing,
        EyeOfCthulhu,
        EaterOfWorlds,
        BrainOfCthulhu,
        QueenBee,
        Deerclops,
        Skeletron,
        WallOfFlesh,
        QueenSlime,
        Spazmatism,
        Retinazer,
        Destroyer,
        SkeletronPrime,
        Plantera,
        Golem,
        DukeFishron,
        EmpressOfLight,
        LunaticCultist,
        MoonLord
    }

    /// <summary>
    /// Inactive design data read from rows 3-21 of the Burger King worksheet.
    /// Nothing in the current weapon reads this catalog yet, so all fillings
    /// remain hidden candidates until their gameplay system is implemented.
    /// FrameIndex is zero-based: worksheet row 3 uses sprite-sheet frame 3,
    /// represented here as index 2.
    /// </summary>
    internal readonly record struct BurgerKingFillingDefinition(
        string InternalName,
        string ChineseName,
        int FrameIndex,
        BurgerKingFillingCategory Categories,
        int DamageDelta,
        int UseTimePercentDelta,
        float KnockbackDelta,
        int CritDelta,
        int ProjectileCountDelta,
        int PenetrationDelta,
        int ExplosionSize,
        Color? ThemeColor,
        BurgerKingFillingDebuff Debuff,
        BurgerKingUnlockCondition UnlockCondition);

    internal static class BurgerKingFillingCatalog
    {
        internal static IReadOnlyList<BurgerKingFillingDefinition> HiddenCandidates { get; } =
            new BurgerKingFillingDefinition[]
            {
                new("AmericanCheese", "美式奶酪片", 2,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Condiment,
                    5, 0, 0f, 0, 0, 0, 0, Hex(0xFFB210),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.SlimeKing),

                new("Onion", "洋葱", 3,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Vegetarian,
                    5, 0, 0f, 0, 0, 0, 0, Hex(0xFCD2FF),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.EyeOfCthulhu),

                new("PickleSlice", "酸黄瓜片", 4,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Vegetarian,
                    5, 0, 0f, 0, 0, 0, 0, Hex(0xBFBC69),
                    BurgerKingFillingDebuff.Poisoned, BurgerKingUnlockCondition.EaterOfWorlds),

                new("FreshCucumberSlice", "鲜黄瓜片", 5,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Vegetarian,
                    5, -5, 0f, 0, 0, 0, 0, Hex(0xD0EC9C),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.BrainOfCthulhu),

                new("HoneyMustard", "蜂蜜芥末酱", 6,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Condiment,
                    5, 0, 0f, 5, 0, 0, 0, Hex(0xFFE84E),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.QueenBee),

                new("MiddleBun", "中层面饼", 7,
                    BurgerKingFillingCategory.Classic,
                    10, 0, 0f, 0, 0, 0, 96, Hex(0xC58855),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.Deerclops),

                new("Lettuce", "生菜", 8,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Vegetarian,
                    5, -10, 0f, 0, 0, 0, 0, Hex(0xA8D683),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.Skeletron),

                new("AngusBeefPatty", "安格斯牛肉饼", 9,
                    BurgerKingFillingCategory.Classic | BurgerKingFillingCategory.Carnivore,
                    25, 0, 5f, 0, 0, 0, 0, Hex(0x743820),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.WallOfFlesh),

                new("RainbowMapleSyrup", "彩虹枫糖浆", 10,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Condiment,
                    10, 0, 0f, 15, 0, 0, 0, Hex(0xC1B5E5),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.QueenSlime),

                new("Jalapeno", "墨西哥辣椒", 11,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Vegetarian,
                    10, -8, 0f, 0, 0, 0, 0, Hex(0x2A9238),
                    BurgerKingFillingDebuff.CursedInferno, BurgerKingUnlockCondition.Spazmatism),

                new("FacingHeavenPepper", "朝天椒", 12,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Vegetarian,
                    10, -8, 0f, 0, 0, 0, 0, Hex(0xA11717),
                    BurgerKingFillingDebuff.Hellfire, BurgerKingUnlockCondition.Retinazer),

                new("GuaranaJam", "瓜拉那果酱", 13,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Vegetarian |
                    BurgerKingFillingCategory.Condiment,
                    15, 0, 0f, 5, 0, 0, 0, Hex(0xDD5959),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.Destroyer),

                new("PurpleCabbage", "紫甘蓝", 14,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Vegetarian,
                    5, -12, 0f, 0, 0, 0, 0, Hex(0x9F44B0),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.SkeletronPrime),

                new("VeganMayonnaise", "素食蛋黄酱", 15,
                    BurgerKingFillingCategory.Vegetarian | BurgerKingFillingCategory.Condiment,
                    20, 0, 0f, 0, 0, 0, 0, Hex(0xFBEE96),
                    BurgerKingFillingDebuff.Ichor, BurgerKingUnlockCondition.Plantera),

                new("RockGrilledLizard", "岩烤蜥蜴", 16,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Carnivore,
                    35, 0, 5f, 0, 0, 0, 0, Hex(0x4B4B4B),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.Golem),

                new("SalmonSashimi", "三文鱼刺身", 17,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Carnivore,
                    40, 0, 0f, 0, 0, 0, 0, Hex(0xFF8452),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.DukeFishron),

                new("PrismaticLacewingScalePowder", "光棱蛾鳞粉", 18,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Condiment,
                    -10, 0, 0f, 0, 2, 0, 0, Hex(0x94EAFF),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.EmpressOfLight),

                new("SpectreTartarSauce", "幽魂塔塔酱", 19,
                    BurgerKingFillingCategory.DarkCuisine | BurgerKingFillingCategory.Condiment,
                    15, 0, 0f, 0, 0, 4, 0, Hex(0xC1E7FF),
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.LunaticCultist),

                new("Mooncake", "月饼", 20,
                    BurgerKingFillingCategory.DarkCuisine,
                    60, 0, 0f, 0, 0, 0, 188, null,
                    BurgerKingFillingDebuff.None, BurgerKingUnlockCondition.MoonLord)
            };

        private static Color Hex(int rgb) => new(
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));

        internal static bool TryGet(int frameIndex, out BurgerKingFillingDefinition definition)
        {
            foreach (BurgerKingFillingDefinition candidate in HiddenCandidates)
            {
                if (candidate.FrameIndex == frameIndex)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        internal static bool IsUnlocked(BurgerKingUnlockCondition condition) => condition switch
        {
            BurgerKingUnlockCondition.SlimeKing => NPC.downedSlimeKing,
            BurgerKingUnlockCondition.EyeOfCthulhu => NPC.downedBoss1,
            BurgerKingUnlockCondition.EaterOfWorlds => NPC.downedBoss2,
            BurgerKingUnlockCondition.BrainOfCthulhu => NPC.downedBoss2,
            BurgerKingUnlockCondition.QueenBee => NPC.downedQueenBee,
            BurgerKingUnlockCondition.Deerclops => NPC.downedDeerclops,
            BurgerKingUnlockCondition.Skeletron => NPC.downedBoss3,
            BurgerKingUnlockCondition.WallOfFlesh => Main.hardMode,
            BurgerKingUnlockCondition.QueenSlime => NPC.downedQueenSlime,
            BurgerKingUnlockCondition.Spazmatism => NPC.downedMechBoss2,
            BurgerKingUnlockCondition.Retinazer => NPC.downedMechBoss2,
            BurgerKingUnlockCondition.Destroyer => NPC.downedMechBoss1,
            BurgerKingUnlockCondition.SkeletronPrime => NPC.downedMechBoss3,
            BurgerKingUnlockCondition.Plantera => NPC.downedPlantBoss,
            BurgerKingUnlockCondition.Golem => NPC.downedGolemBoss,
            BurgerKingUnlockCondition.DukeFishron => NPC.downedFishron,
            BurgerKingUnlockCondition.EmpressOfLight => NPC.downedEmpressOfLight,
            BurgerKingUnlockCondition.LunaticCultist => NPC.downedAncientCultist,
            BurgerKingUnlockCondition.MoonLord => NPC.downedMoonlord,
            _ => false
        };
    }
}
