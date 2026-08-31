using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Mounts;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items
{
    public class AlloyCrossMedal02 : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToMount(ModContent.MountType<AlloyTank02>());
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool CanUseItem(Player player)
        {
            if (player.mount.Active &&
                player.mount.Type == ModContent.MountType<AlloyTank02>())
            {
                return true;
            }

            return player.maxMinions - player.slotsMinions >= 0.999f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(50)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
