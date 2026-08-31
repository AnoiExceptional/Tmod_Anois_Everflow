using everflow.Common.Items;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class LegionAuthority_3 : LegionAuthorityAccessory
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 3);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions += 3;
            player.GetModPlayer<LegionAuthorityPlayer>().NonSummonDamageMultiplier *= 0.75f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<LegionAuthority_2>()
                .AddIngredient(ItemID.HallowedBar, 25)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
