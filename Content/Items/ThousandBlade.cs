using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;
using everflow.Common.Players;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class ThousandBlade : ModItem
    {
        private bool rightAttackActive;
        private ChainSwordSwingState chainSwordSwingState;
        private bool currentRightSwingReversed;

        public bool IsReversedRightSwing =>
            rightAttackActive && currentRightSwingReversed;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 49;
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.crit = 9;
            Item.knockBack = 5f;
            Item.ArmorPenetration = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<ThousandBladeWhipProjectile>();
            Item.shootSpeed = 8f;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Pink;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input = player.GetModPlayer<AlloySwordInputPlayer>();
            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
            {
                currentRightSwingReversed = chainSwordSwingState.PreviewNext(
                    ThousandBladeWhipProjectile.ChainSwordStyle.AlternateReverse);
                SetRightAttack();
            }
            else
                ResetToMelee();

            return true;
        }

        public override void UseAnimation(Player player)
        {
            AlloySwordInputPlayer input = player.GetModPlayer<AlloySwordInputPlayer>();
            rightAttackActive = input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;

            if (rightAttackActive)
                SetRightAttack();
            else
                ResetToMelee();
        }

        public override void HoldItem(Player player)
        {
            AlloySwordInputPlayer input = player.GetModPlayer<AlloySwordInputPlayer>();
            if (rightAttackActive &&
                input.Mode != AlloySwordInputPlayer.SwordInputMode.Right &&
                player.itemAnimation <= 0)
                ResetToMelee();
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            if (rightAttackActive)
                noHitbox = true;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (!rightAttackActive)
                Lighting.AddLight(hitbox.Center.ToVector2(), new Vector3(0.18f, 0.15f, 0.30f));
        }

        private void SetRightAttack()
        {
            rightAttackActive = true;
            Item.damage = 75;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 7f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item71;
            Item.noMelee = false;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<ThousandBladeWhipProjectile>();
            Item.shootSpeed = 8f;
        }

        private void ResetToMelee()
        {
            rightAttackActive = false;
            currentRightSwingReversed = false;
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<ThousandBladeWhipProjectile>();
            Item.shootSpeed = 8f;
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
            AlloySwordInputPlayer input = player.GetModPlayer<AlloySwordInputPlayer>();
            if (input.Mode != AlloySwordInputPlayer.SwordInputMode.Right)
                return false;

            currentRightSwingReversed = chainSwordSwingState.Advance(
                ThousandBladeWhipProjectile.ChainSwordStyle.AlternateReverse);

            Projectile.NewProjectile(
                source,
                position,
                velocity,
                type,
                damage,
                knockback,
                player.whoAmI,
                ai0: 0f,
                ai1: 0f,
                ai2: currentRightSwingReversed ? 1f : 0f);

            return false;
        }
    }
}
