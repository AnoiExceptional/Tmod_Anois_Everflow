using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Common.Items
{
    /// <summary>
    /// 基石饰品的公共类型。功能饰品栏内同时只能存在一件基石饰品。
    /// </summary>
    public abstract class FoundationAccessory : ModItem
    {
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) =>
            !(equippedItem.ModItem is FoundationAccessory && incomingItem.ModItem is FoundationAccessory);

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.25f) % 1f;
            Color cyclingColor = Main.hslToRgb(hue, 1f, 0.65f);
            tooltips.Add(new TooltipLine(Mod, "Foundation",
                Language.GetTextValue("Mods.everflow.UI.Foundation"))
            {
                OverrideColor = cyclingColor
            });
        }
    }
}
