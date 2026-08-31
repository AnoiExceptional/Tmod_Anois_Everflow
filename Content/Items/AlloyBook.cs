using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloyBook : ModItem
    {
        private const float DiamondBoltSpeed = 14.25f;
        private const float RubyBoltSpeed = 9f;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.damage = 25;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 5.5f;
            Item.mana = 7;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item43;
            Item.shoot = ProjectileID.DiamondBolt;
            Item.shootSpeed = DiamondBoltSpeed;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 60);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.mana = 15;
                Item.useTime = 35;
                Item.useAnimation = 35;
                Item.shoot = ModContent.ProjectileType<AlloyBookRubyBolt>();
                Item.shootSpeed = RubyBoltSpeed;
            }
            else
            {
                Item.mana = 7;
                Item.useTime = 15;
                Item.useAnimation = 15;
                Item.shoot = ProjectileID.DiamondBolt;
                Item.shootSpeed = DiamondBoltSpeed;
            }

            return true;
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
            // 左键保持钻石法杖式的直线发射，只修改弹速与穿透。
            if (player.altFunctionUse != 2)
            {
                int projectileIndex = Projectile.NewProjectile(
                    source,
                    position,
                    velocity.SafeNormalize(Vector2.UnitX * player.direction) * DiamondBoltSpeed,
                    ProjectileID.DiamondBolt,
                    damage,
                    knockback,
                    player.whoAmI);

                Main.projectile[projectileIndex].penetrate = 1;
                return false;
            }

            Vector2 target = Main.MouseWorld;
            int projectileCount = Main.rand.Next(3, 5);

            for (int i = 0; i < projectileCount; i++)
            {
                Vector2 spawnPosition = target + new Vector2(
                    Main.rand.NextFloat(-240f, 241f),
                    Main.rand.NextFloat(-760f, -600f));

                Vector2 targetOffset = target + new Vector2(
                    Main.rand.NextFloat(-28f, 29f),
                    Main.rand.NextFloat(-12f, 13f));

                Vector2 shotVelocity =
                    (targetOffset - spawnPosition).SafeNormalize(Vector2.UnitY) * RubyBoltSpeed;

                int projectileIndex = Projectile.NewProjectile(
                    source,
                    spawnPosition,
                    shotVelocity,
                    ModContent.ProjectileType<AlloyBookRubyBolt>(),
                    damage,
                    knockback,
                    player.whoAmI,
                    target.X,
                    target.Y);

                Main.projectile[projectileIndex].netUpdate = true;
            }

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(5)
                .AddIngredient(ItemID.Ruby, 5)
                .AddIngredient(ItemID.Book)
                .AddTile(TileID.Bookcases)
                .Register();
        }
    }
}
