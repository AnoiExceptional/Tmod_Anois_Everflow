using everflow.Common.Players;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class SethSword : ModItem
    {
        private bool rightAttackActive;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 96;
            Item.height = 98;
            Item.scale = 0.8f;
            Item.damage = 180;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<SethSwordProjectile>();
            Item.shootSpeed = 8f;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 15);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();
            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
                SetRightAttack();
            else
                ResetToMelee();

            return true;
        }

        public override void UseAnimation(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();
            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
                SetRightAttack();
            else
                ResetToMelee();
        }

        public override void HoldItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();
            if (rightAttackActive &&
                input.Mode != AlloySwordInputPlayer.SwordInputMode.Right &&
                player.itemAnimation <= 0)
            {
                ResetToMelee();
            }

            if (!rightAttackActive && player.itemAnimation > 0 &&
                !Main.dedServ)
            {
                Vector2 direction = player.itemRotation.ToRotationVector2() *
                    player.direction;
                Vector2 hand = player.RotatedRelativePoint(
                    player.MountedCenter);
                Lighting.AddLight(
                    hand + direction * 34f * Item.scale,
                    new Vector3(0.85f, 0.62f, 0.14f));
                Lighting.AddLight(
                    player.Center,
                    new Vector3(0.55f, 0.40f, 0.09f));

                if (Main.rand.NextBool(2))
                {
                    Vector2 position = hand + direction *
                        Main.rand.NextFloat(10f, 72f) * Item.scale;
                    Dust dust = Dust.NewDustPerfect(
                        position,
                        DustID.TintableDust,
                        Main.rand.NextVector2Circular(0.7f, 0.7f),
                        100,
                        new Color(255, 205, 55),
                        Main.rand.NextFloat(0.7f, 1.05f));
                    dust.noGravity = true;
                }
            }
        }

        public override void ModifyWeaponDamage(
            Player player,
            ref StatModifier damage)
        {
            damage *= 1f + player.GetModPlayer<SethSwordPlayer>()
                .AppliedDamageBonus;
        }

        public override void OnHitNPC(
            Player player,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (!rightAttackActive)
                player.GetModPlayer<SethSwordPlayer>().AddRapidAttackHit();
        }

        public override void UseItemHitbox(
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            if (rightAttackActive)
                noHitbox = true;
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
            return rightAttackActive;
        }

        private void SetRightAttack()
        {
            rightAttackActive = true;
            Item.scale = 1f;
            Item.damage = 150;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.knockBack = 10f;
            Item.crit = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item71;
            Item.noMelee = false;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<SethSwordProjectile>();
            Item.shootSpeed = 8f;
        }

        private void ResetToMelee()
        {
            rightAttackActive = false;
            Item.scale = 0.8f;
            Item.damage = 180;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<SethSwordProjectile>();
            Item.shootSpeed = 8f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Cutlass)
                .AddIngredient(ItemID.FragmentStardust, 20)
                .AddIngredient(ItemID.AncientBattleArmorMaterial)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
