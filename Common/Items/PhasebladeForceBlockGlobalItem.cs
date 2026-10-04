using System.Collections.Generic;
using everflow.Common.DamageClasses;
using everflow.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Common.Items
{
    public sealed class PhasebladeForceBlockGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        private int originalUseStyle;
        private int originalUseTime;
        private int originalUseAnimation;
        private bool originalAutoReuse;
        private bool originalNoMelee;
        private bool originalNoUseGraphic;
        private bool originalChannel;
        private int originalShoot;
        private float originalShootSpeed;
        private Terraria.Audio.SoundStyle? originalUseSound;

        public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
            TryGetShieldColorIndex(entity.type, out _);

        public override void SetDefaults(Item entity)
        {
            originalUseStyle = entity.useStyle;
            originalUseTime = entity.useTime;
            originalUseAnimation = entity.useAnimation;
            originalAutoReuse = entity.autoReuse;
            originalNoMelee = entity.noMelee;
            originalNoUseGraphic = entity.noUseGraphic;
            originalChannel = entity.channel;
            originalShoot = entity.shoot;
            originalShootSpeed = entity.shootSpeed;
            originalUseSound = entity.UseSound;

            entity.DamageType = ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        // 盾牌由独立的 ModPlayer 输入处理。左右键同时按住时，右键不应
        // 抢占物品的 altFunctionUse，让左键仍按普通挥砍执行。
        public override bool AltFunctionUse(Item item, Player player) =>
            !player.controlUseItem;

        public override void ModifyTooltips(
            Item item,
            List<TooltipLine> tooltips)
        {
            // SolarBlade 已在各自的物品本地化中包含同一句说明。
            if (!IsSolarBlade(item.type))
            {
                tooltips.Add(new TooltipLine(
                    Mod,
                    "ForceShield",
                    Language.GetTextValue("Mods.everflow.ForceShieldTooltip")));
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // 不启动任何物品动画，也就不会额外绘制剑身；盾牌仍会由
                // PhasebladeForceBlockPlayer 在本帧独立生成并维持。
                RestoreLeftClick(item);
                return false;
            }

            RestoreLeftClick(item);
            return true;
        }

        private void RestoreLeftClick(Item item)
        {
            item.useStyle = originalUseStyle;
            item.useTime = originalUseTime;
            item.useAnimation = originalUseAnimation;
            item.autoReuse = originalAutoReuse;
            item.noMelee = originalNoMelee;
            item.noUseGraphic = originalNoUseGraphic;
            item.channel = originalChannel;
            item.shoot = originalShoot;
            item.shootSpeed = originalShootSpeed;
            item.UseSound = originalUseSound;
            item.DamageType = ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        internal static bool TryGetShieldColorIndex(int itemType, out int colorIndex)
        {
            colorIndex = itemType switch
            {
                ItemID.BluePhaseblade or ItemID.BluePhasesaber => 0,
                ItemID.RedPhaseblade or ItemID.RedPhasesaber => 1,
                ItemID.GreenPhaseblade or ItemID.GreenPhasesaber => 2,
                ItemID.PurplePhaseblade or ItemID.PurplePhasesaber => 3,
                ItemID.WhitePhaseblade or ItemID.WhitePhasesaber => 4,
                ItemID.YellowPhaseblade or ItemID.YellowPhasesaber => 5,
                ItemID.OrangePhaseblade or ItemID.OrangePhasesaber => 6,
                var type when type == ModContent.ItemType<SolarBladeBlue>() => 0,
                var type when type == ModContent.ItemType<SolarBladeRed>() => 1,
                var type when type == ModContent.ItemType<SolarBladeGreen>() => 2,
                var type when type == ModContent.ItemType<SolarBladePurple>() => 3,
                var type when type == ModContent.ItemType<SolarBladeWhite>() => 4,
                var type when type == ModContent.ItemType<SolarBladeYellow>() => 5,
                var type when type == ModContent.ItemType<SolarBladeOrange>() => 6,
                _ => -1
            };
            return colorIndex >= 0;
        }

        internal static bool IsPhasesaber(int itemType) => itemType is
            ItemID.BluePhasesaber or
            ItemID.RedPhasesaber or
            ItemID.GreenPhasesaber or
            ItemID.PurplePhasesaber or
            ItemID.WhitePhasesaber or
            ItemID.YellowPhasesaber or
            ItemID.OrangePhasesaber;

        internal static bool IsSolarBlade(int itemType) => itemType ==
            ModContent.ItemType<SolarBladeBlue>() || itemType ==
            ModContent.ItemType<SolarBladeRed>() || itemType ==
            ModContent.ItemType<SolarBladeGreen>() || itemType ==
            ModContent.ItemType<SolarBladePurple>() || itemType ==
            ModContent.ItemType<SolarBladeWhite>() || itemType ==
            ModContent.ItemType<SolarBladeYellow>() || itemType ==
            ModContent.ItemType<SolarBladeOrange>();

        internal static int GetShieldTier(int itemType)
        {
            if (IsSolarBlade(itemType))
                return 2;
            if (IsPhasesaber(itemType))
                return 1;
            return 0;
        }
    }
}
