using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class AlloyHeadgear : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 轻胄不遮住角色头发
            ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
        }
        
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;

            Item.defense = 5;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 18);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.10f;
            player.GetDamage(DamageClass.Ranged) += 0.10f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AlloyChestplate>()
                && legs.type == ModContent.ItemType<AlloyGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Value;

            player.GetAttackSpeed(DamageClass.Ranged) += 0.15f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}