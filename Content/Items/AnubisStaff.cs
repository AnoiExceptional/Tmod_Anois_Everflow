using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class AnubisStaff : ModItem
    {
        private int phantomShotCounter;

        public override bool AltFunctionUse(Player player) => true;

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.damage = 120;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 15;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 5f;
            Item.crit = 9;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item20;
            Item.shoot = ModContent.ProjectileType<AnubisStaffProjectile>();
            Item.shootSpeed = 30f;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 12);
        }

        public override bool CanUseItem(Player player)
        {
            bool summoningPhantoms = player.altFunctionUse == 2;

            Item.mana = summoningPhantoms ? 50 : 15;
            Item.useTime = summoningPhantoms ? 30 : 10;
            Item.useAnimation = summoningPhantoms ? 30 : 10;
            Item.UseSound = summoningPhantoms ? SoundID.Item44 : SoundID.Item20;

            if (!summoningPhantoms)
                return true;

            int phantomType = ModContent.ProjectileType<AnubisForbiddenPhantom>();
            // 允许基础召唤栏不足2格的角色主动透支召唤栏召出这一对幻影。
            // 生成后两个射弹仍各自占用1格，合计正确计入2格召唤栏。
            return player.ownedProjectileCounts[phantomType] == 0;
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
            int phantomType = ModContent.ProjectileType<AnubisForbiddenPhantom>();

            if (player.altFunctionUse == 2)
            {
                phantomShotCounter = 0;

                for (int side = -1; side <= 1; side += 2)
                {
                    int index = Projectile.NewProjectile(
                        source,
                        player.Center + new Vector2(side * 52f, -10f),
                        Vector2.Zero,
                        phantomType,
                        0,
                        0f,
                        player.whoAmI,
                        side);

                    Main.projectile[index].originalDamage = Item.damage;
                }

                return false;
            }

            if (player.ownedProjectileCounts[phantomType] != 2)
            {
                phantomShotCounter = 0;
                return true;
            }

            phantomShotCounter++;
            if (phantomShotCounter < 5)
                return true;

            phantomShotCounter = 0;
            if (player.whoAmI != Main.myPlayer)
                return true;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile phantom = Main.projectile[i];
                if (!phantom.active || phantom.owner != player.whoAmI ||
                    phantom.type != phantomType)
                {
                    continue;
                }

                Vector2 phantomVelocity = phantom.Center.DirectionTo(Main.MouseWorld) *
                                          Item.shootSpeed;
                int projectileIndex = Projectile.NewProjectile(
                    source,
                    phantom.Center,
                    phantomVelocity,
                    ModContent.ProjectileType<AnubisStaffProjectile>(),
                    damage,
                    knockback,
                    player.whoAmI,
                    0f,
                    1f);

                Projectile phantomBlade = Main.projectile[projectileIndex];
                // 每个分身镰刃分别记录命中过的敌怪，不占用玩家通用无敌帧，
                // 因而不会与本体镰刃或另一个分身镰刃互相吞伤害。
                phantomBlade.usesLocalNPCImmunity = true;
                phantomBlade.localNPCHitCooldown = -1;
            }

            return true;
        }

        public override Vector2? HoldoutOffset()
        {
            // 让权杖柄端贴合玩家手部，不改变射弹生成位置。
            return new Vector2(-4f, -2f);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.UnholyTrident)
                .AddIngredient(ItemID.FragmentNebula, 15)
                .AddIngredient(ItemID.AncientBattleArmorMaterial)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
