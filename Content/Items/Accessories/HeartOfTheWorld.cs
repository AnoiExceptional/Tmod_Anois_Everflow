using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class HeartOfTheWorld : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.accessory = true;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.sellPrice(gold: 8);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<HeartOfTheWorldPlayer>().Equipped = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 100)
                .AddIngredient(ItemID.StoneBlock, 100)
                .AddIngredient(ItemID.MudBlock, 100)
                .AddIngredient(ItemID.IceBlock, 100)
                .AddIngredient(ItemID.SandBlock, 100)
                .AddIngredient(ItemID.GraniteBlock, 100)
                .AddIngredient(ItemID.MarbleBlock, 100)
                .AddIngredient(ItemID.AshBlock, 100)
                .AddIngredient(ItemID.Cloud, 100)
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}
