using everflow.Common.Items;
using everflow.Content.Players;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class AquaVirtue : FoundationAccessory
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            // Inventory slots already scale oversized textures to fit. Keeping
            // the item itself at full scale lets item frames display it clearly.
            Item.scale = 1f;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AquaVirtuePlayer virtuePlayer = player.GetModPlayer<AquaVirtuePlayer>();
            virtuePlayer.MarkEquipped();
            virtuePlayer.EffectVisible = !hideVisual;
            player.GetDamage(DamageClass.Magic) += 0.25f;
            player.GetCritChance(DamageClass.Magic) += 25f;
            player.statManaMax2 += 100;

            // 与魔力再生手环相同：延迟流逝速度 +1、恢复速度 +25。
            player.manaRegenDelayBonus += 1f;
            player.manaRegenBonus += 25;

            player.manaFlower = true;
            player.manaMagnet = true;
            player.buffImmune[BuffID.ManaSickness] = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            int foundationIndex = tooltips.FindIndex(line =>
                line.Mod == Mod.Name && line.Name == "Foundation");
            TooltipLine quote = new(Mod, "AquaVirtueQuote",
                Language.GetTextValue("Mods.everflow.UI.AquaVirtueQuote"))
            {
                OverrideColor = new Color(0x97, 0xED, 0xCA)
            };

            if (foundationIndex >= 0)
                tooltips.Insert(foundationIndex, quote);
            else
                tooltips.Add(quote);
        }
    }
}
