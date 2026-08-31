using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public class CommanderEmblem : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.accessory = true;

            // 与原版职业徽章保持接近的稀有度
            Item.rare = ItemRarityID.LightRed;

            Item.value = Item.sellPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 近战伤害 +12%
            player.GetDamage(DamageClass.Melee) += 0.12f;

            // 召唤伤害 +12%
            player.GetDamage(DamageClass.Summon) += 0.12f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.WarriorEmblem, 1)
                .AddIngredient(ItemID.SummonerEmblem, 1)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}