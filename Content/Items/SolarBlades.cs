using everflow.Common.DamageClasses;
using everflow.Common.Items;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public abstract class SolarBladeBase : ModItem
    {
        private int attackSequence;

        protected abstract int BasePhasesaberType { get; }
        protected abstract string ColorName { get; }

        public override string Texture =>
            $"everflow/Content/Items/SolarBlade_{ColorName}";

        public override void SetDefaults()
        {
            Item.CloneDefaults(BasePhasesaberType);
            Item.damage = 130;
            Item.DamageType = ModContent.GetInstance<MagicalMeleeDamageClass>();
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.knockBack = 3f;
            Item.crit = 4;
            Item.rare = ItemRarityID.Red;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.channel = false;
            Item.shoot = ModContent.ProjectileType<SolarBladeAttackProjectile>();
            Item.shootSpeed = 1f;
            // 保留晶光刃原版的光剑挥舞音效。
            Item.UseSound = SoundID.Item15;
        }

        public override bool CanUseItem(Player player) =>
            player.altFunctionUse == 2 ||
            player.ownedProjectileCounts[
                ModContent.ProjectileType<SolarBladeAttackProjectile>()] <= 0;

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
                return false;

            int attackType = attackSequence++ % 4;
            Vector2 direction = velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            PhasebladeForceBlockGlobalItem.TryGetShieldColorIndex(
                Item.type,
                out int colorIndex);
            Projectile.NewProjectile(
                source,
                player.RotatedRelativePoint(player.MountedCenter),
                direction,
                ModContent.ProjectileType<SolarBladeAttackProjectile>(),
                damage,
                knockback,
                player.whoAmI,
                attackType,
                colorIndex,
                Item.type);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(BasePhasesaberType)
                .AddIngredient(ItemID.FragmentSolar, 10)
                .AddIngredient(ItemID.LihzahrdPowerCell)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public sealed class SolarBladeBlue : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.BluePhasesaber;
        protected override string ColorName => "Blue";
    }

    public sealed class SolarBladeRed : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.RedPhasesaber;
        protected override string ColorName => "Red";
    }

    public sealed class SolarBladeGreen : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.GreenPhasesaber;
        protected override string ColorName => "Green";
    }

    public sealed class SolarBladePurple : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.PurplePhasesaber;
        protected override string ColorName => "Purple";
    }

    public sealed class SolarBladeWhite : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.WhitePhasesaber;
        protected override string ColorName => "White";
    }

    public sealed class SolarBladeYellow : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.YellowPhasesaber;
        protected override string ColorName => "Yellow";
    }

    public sealed class SolarBladeOrange : SolarBladeBase
    {
        protected override int BasePhasesaberType => ItemID.OrangePhasesaber;
        protected override string ColorName => "Orange";
    }
}
