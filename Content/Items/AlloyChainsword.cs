using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Players;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloyChainsword : ModItem
    {
        // 锁定当前这一次攻击，不能依赖攻击过程中会被重置的 altFunctionUse。
        private bool rightAttackActive;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 58;
            Item.damage = 25;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<AlloyChainswordProjectile>();
            Item.shootSpeed = 4f;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
            {
                rightAttackActive = true;
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.UseSound = SoundID.Item71;
            }
            else
            {
                ResetToMelee();
            }

            return true;
        }

        public override void UseAnimation(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            rightAttackActive =
                input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;

            if (rightAttackActive)
            {
                Item.noUseGraphic = true;
                Item.useStyle = ItemUseStyleID.Swing;
            }
            else
            {
                ResetToMelee();
            }
        }

        public override void HoldItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            // 右键动作结束的第一帧就恢复近战状态，不能等下一次左键再恢复。
            if (rightAttackActive &&
                input.Mode != AlloySwordInputPlayer.SwordInputMode.Right &&
                player.itemAnimation <= 0)
                ResetToMelee();
        }

        public override void UseItemHitbox(
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            // Item.noMelee 永久保持 false，避免状态残留吃掉下一次左键判定；
            // 右键这一次攻击单独关闭物品本体碰撞箱。
            if (rightAttackActive)
                noHitbox = true;
        }

        private void ResetToMelee()
        {
            rightAttackActive = false;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<AlloyChainswordProjectile>();
            Item.shootSpeed = 4f;
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
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            // 弹幕类型永久保留；左键只在这里阻止鞭子生成。
            return input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;
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
