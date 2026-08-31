using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles;
using everflow.Content.Items.Materials;

namespace everflow.Content.Items
{
    public class AlloyBow : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 允许按住右键，在回旋镖返回后自动再次投掷
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            // 基础属性
            Item.damage = 18;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 1f;
            Item.crit = 4;

            // 使用速度
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;

            // 弓箭设置
            Item.useAmmo = AmmoID.Arrow;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 6.7f;

            // 防止弓本体造成近战接触伤害
            Item.noMelee = true;

            // 使用音效
            Item.UseSound = SoundID.Item5;

            // 物品大小
            Item.width = 16;
            Item.height = 64;

            // 稀有度与价值
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 36);
        }

        // 开启右键特殊攻击
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        // 右键投掷时不要求玩家拥有箭
        public override bool NeedsAmmo(Player player)
        {
            if (player.altFunctionUse == 2)
                return false;

            return true;
        }

        // 右键投掷时不消耗箭
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return player.altFunctionUse != 2;
        }

        public override bool CanUseItem(Player player)
        {
            int thrownType = ModContent.ProjectileType<AlloyBowThrown>();

            // 左键保持1击退，右键投掷提高到7击退。
            Item.knockBack = player.altFunctionUse == 2 ? 7f : 1f;

            // 右键时隐藏手里的弓，
            // 视觉上表现为“把弓本体扔出去”
            Item.noUseGraphic = player.altFunctionUse == 2;

            // 弓本体还在外面飞时，不能再次使用
            // 防止一把弓同时射箭/出现多个回旋镖
            if (player.ownedProjectileCounts[thrownType] > 0)
                return false;

            return base.CanUseItem(player);
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            // ==============================
            // 右键：投掷 Alloy Bow 本体
            // ==============================
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(
                    source,
                    position,
                    velocity,
                    ModContent.ProjectileType<AlloyBowThrown>(),
                    damage,
                    knockback,
                    player.whoAmI
                );

                // 阻止原本的箭矢生成
                return false;
            }

            // ==============================
            // 左键：正常弓射击
            // ==============================

            // type 会自动根据当前使用的箭变化，
            // 所以木箭、火箭、霜燃箭等都能正常使用
            return true;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, 0f);
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
