using everflow.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public sealed class YouFengBreastplate : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 20;
            Item.defense = 20;
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.10f;
            player.GetCritChance(DamageClass.Generic) += 7f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(12)
                // 秘银砧与山铜砧共用此制作站ID。
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
