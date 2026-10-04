using everflow.Common.DamageClasses;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class ArcticPillar : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 允许玩家在左键动作尚未结束时提前按住右键；当前动作结束后，
            // 原版物品输入系统会立刻接续触发替代攻击，无需重新按键。
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.damage = 120;
            Item.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
            Item.crit = 4;
            Item.knockBack = 7.5f;
            Item.mana = 12;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<ArcticPillarHeldProjectile>();
            Item.shootSpeed = 4.75f;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 12);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? CanAutoReuseItem(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.damage = 150;
                Item.knockBack = 3f;
                Item.crit = 4;
                Item.useTime = 15;
                Item.useAnimation = 15;
                Item.channel = true;
                Item.autoReuse = false;
                Item.shoot = ModContent.ProjectileType<
                    ArcticPillarChargeProjectile>();
                Item.shootSpeed = 1f;

                return player.ownedProjectileCounts[Item.shoot] == 0;
            }

            Item.damage = 120;
            Item.knockBack = 7.5f;
            Item.crit = 4;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.channel = false;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<
                ArcticPillarHeldProjectile>();
            Item.shootSpeed = 4.75f;
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.NorthPole)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddIngredient(ItemID.FragmentNebula, 5)
                .AddIngredient(ItemID.FrostCore, 2)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
