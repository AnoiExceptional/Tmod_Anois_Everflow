using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Common.Items
{
    /// <summary>
    /// Gives vanilla Kaleidoscope's fixed summon-tag crit line the Bonded Crit
    /// terminology. No text search or replacement is performed.
    /// </summary>
    public sealed class KaleidoscopeBondedCritTooltipGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type != ItemID.RainbowWhip)
                return;

            TooltipLine bondedCritLine = tooltips.Find(line =>
                line.Mod == "Terraria" && line.Name == "Tooltip2");
            if (bondedCritLine == null)
                return;

            bool chinese = Language.ActiveCulture.Name.StartsWith(
                "zh", StringComparison.OrdinalIgnoreCase);
            bondedCritLine.Text = chinese
                ? "10%召唤标记亲密暴击率"
                : "10% summon tag Bonded Crit chance";
        }
    }
}
