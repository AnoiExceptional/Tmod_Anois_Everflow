using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Players;
using everflow.Content.Buffs.Minions;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;
using everflow.Content.Projectiles.Minions;

namespace everflow.Content.Items
{
    public class AlloyDagger : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 40;
            // 默认左键直接走标准召唤流程。
            Item.damage = 25;
            Item.DamageType = DamageClass.Summon;
            Item.knockBack = 1.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item44;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AlloyGuard>();
            Item.shootSpeed = 10f;
            // 左键召唤消耗10魔力；右键投刀通过 ModifyManaCost 免除。
            Item.mana = 10;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Left)
            {
                Item.damage = 25;
                Item.DamageType = DamageClass.Summon;
                // 原版海盗法杖使用挥动动作；Shoot 样式会让匕首偏离手部。
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.UseSound = SoundID.Item44;
                Item.noUseGraphic = false;
                Item.shoot = ModContent.ProjectileType<AlloyGuard>();
                // Terraria 的标准召唤射弹流程需要非零 shootSpeed。
                Item.shootSpeed = 10f;
            }
            else
            {
                Item.damage = 20;
                // 物品本体保持召唤类别，使原版界面绘制 Extra[199] 召唤目标标记。
                // 投出的 AlloyDaggerProjectile 自身仍是 DamageClass.Ranged。
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.UseSound = SoundID.Item1;
                Item.noUseGraphic = true;
                Item.shoot = ModContent.ProjectileType<AlloyDaggerProjectile>();
                Item.shootSpeed = 10f;
            }

            return true;
        }

        public override void ModifyManaCost(
            Player player,
            ref float reduce,
            ref float mult)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
                mult = 0f;
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

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
                return true;

            player.AddBuff(ModContent.BuffType<AlloyGuardBuff>(), 2);

            Vector2 spawnPosition = Main.MouseWorld;
            int projectileIndex = Projectile.NewProjectile(
                source,
                spawnPosition,
                Vector2.Zero,
                ModContent.ProjectileType<AlloyGuard>(),
                damage,
                knockback,
                player.whoAmI);

            Main.projectile[projectileIndex].originalDamage = Item.damage;
            return false;
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
