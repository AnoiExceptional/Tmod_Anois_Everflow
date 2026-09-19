using everflow.Content.Items.Materials;
using everflow.Content.Projectiles.Minions;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class WyrmBladeDagger : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Type] = true;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 180;
            Item.DamageType = DamageClass.Summon;
            Item.knockBack = 6f;
            Item.mana = 0;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item44;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.sellPrice(gold: 10);
            Item.shoot = ModContent.ProjectileType<PhantomYaZiSwordSentry>();
            Item.shootSpeed = 0f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.damage = 50;
                Item.DamageType = DamageClass.Ranged;
                Item.knockBack = 3f;
                Item.crit = 4;
                Item.useTime = 8;
                Item.useAnimation = 8;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.UseSound = SoundID.Item1;
                Item.shoot = ModContent.ProjectileType<WyrmBladeDaggerProjectile>();
                Item.shootSpeed = 13f;
            }
            else
            {
                Item.damage = 180;
                Item.DamageType = DamageClass.Summon;
                Item.knockBack = 6f;
                Item.crit = 0;
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.noMelee = true;
                Item.noUseGraphic = false;
                Item.UseSound = SoundID.Item44;
                Item.shoot = ModContent.ProjectileType<PhantomYaZiSwordSentry>();
                Item.shootSpeed = 0f;
            }

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
            if (player.altFunctionUse == 2)
                return true;

            player.FindSentryRestingSpot(
                type,
                out int worldX,
                out int worldY,
                out _);

            // 射弹坐标是剑柄锚点；下移112px为剑尖，并给4px浮动预留空间。
            Vector2 spawnPosition = new(worldX, worldY - 116f);
            Projectile.NewProjectile(
                source,
                spawnPosition,
                Vector2.Zero,
                type,
                damage,
                knockback,
                player.whoAmI);

            player.UpdateMaxTurrets();
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<YaZiBrokenDagger>()
                .AddIngredient<WyrmBladeBar>(10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
