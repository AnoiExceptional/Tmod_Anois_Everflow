using everflow.Content.Tiles.Relics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Relics
{
    public sealed class AncientTroopRelic : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(
                ModContent.TileType<AncientTroopRelicTile>());
            Item.width = 30;
            Item.height = 50;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.sellPrice(gold: 1);
        }
    }

    public sealed class YaZiRelic : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<YaZiRelicTile>());
            Item.width = 30;
            Item.height = 50;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.sellPrice(gold: 1);
        }
    }
}
