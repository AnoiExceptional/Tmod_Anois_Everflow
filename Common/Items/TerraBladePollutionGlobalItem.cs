using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using everflow.Content.Players;

namespace everflow.Common.Items
{
    /// <summary>
    /// 泰拉刃的自然力量会被世界邪恶侵蚀。污染倍率通过UseSpeedMultiplier
    /// 独立参与计算，因此与玩家的近战及全局攻速加成保持乘算关系。
    /// </summary>
    public sealed class TerraBladePollutionGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
            entity.type == ItemID.TerraBlade;

        public override float UseSpeedMultiplier(Item item, Player player)
        {
            HeartOfTheWorldPlayer worldHeart =
                player.GetModPlayer<HeartOfTheWorldPlayer>();
            if (worldHeart.PureHeartEquipped)
                return 1f;

            if (worldHeart.CanPurifyNextTerraBladeUse())
                return 1f;

            int evilPercent = WorldGen.tEvil + WorldGen.tBlood;
            float pollutionRatio = MathHelper.Clamp(evilPercent / 100f, 0f, 0.99f);
            return 1f - pollutionRatio;
        }

        public override void UseAnimation(Item item, Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            HeartOfTheWorldPlayer worldHeart =
                player.GetModPlayer<HeartOfTheWorldPlayer>();
            if (worldHeart.PureHeartEquipped)
                return;

            if (!worldHeart.Equipped)
                return;

            worldHeart.TerraBladeUsePurified =
                player.ConsumeItem(ItemID.GreenSolution);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            HeartOfTheWorldPlayer worldHeart =
                player.GetModPlayer<HeartOfTheWorldPlayer>();
            string localizationKey = "Mods.everflow.TerraBladePollutionTooltip";
            if (worldHeart.PureHeartEquipped)
            {
                localizationKey = "Mods.everflow.TerraBladePureWorldTooltip";
            }
            else if (worldHeart.Equipped)
            {
                localizationKey = player.HasItem(ItemID.GreenSolution)
                    ? "Mods.everflow.TerraBladeWateredTooltip"
                    : "Mods.everflow.TerraBladeThirstyTooltip";
            }

            tooltips.Add(new TooltipLine(
                Mod,
                "PollutionWeakened",
                Language.GetTextValue(localizationKey)));
        }
    }
}
