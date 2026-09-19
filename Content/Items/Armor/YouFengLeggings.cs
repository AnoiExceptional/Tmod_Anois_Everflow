using everflow.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public sealed class YouFengLeggings : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.10f;
            player.runAcceleration *= 1.15f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(10)
                // 秘银砧与山铜砧共用此制作站ID。
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
