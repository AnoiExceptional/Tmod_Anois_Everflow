using everflow.Common.DamageClasses;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public abstract class WyrmBladeGreatswordProjectileBase : ModProjectile
    {
        protected abstract float DamageDistanceTiles { get; }
        protected abstract float FadeStartTiles { get; }
        protected abstract float FadeEndTiles { get; }
        protected abstract Color LightColor { get; }
        protected abstract DamageClass ProjectileDamageType { get; }
        protected virtual float SizeScale => 1f;

        private float TravelledPixels
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(64f * SizeScale);
            Projectile.height = (int)(28f * SizeScale);
            Projectile.scale = SizeScale;
            Projectile.DamageType = ProjectileDamageType;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.Opacity = 0.8f;
        }

        public override void AI()
        {
            TravelledPixels += Projectile.velocity.Length();
            // 两张贴图的上方是剑气正方向；速度角默认以贴图右方为0°，
            // 因此绘制时顺时针补偿90°。
            Projectile.rotation = Projectile.velocity.ToRotation() +
                MathHelper.PiOver2;

            float travelledTiles = TravelledPixels / 16f;
            float fade = 1f - Utils.GetLerpValue(
                FadeStartTiles,
                FadeEndTiles,
                travelledTiles,
                true);
            Projectile.Opacity = 0.8f * fade;

            // 贴图仍使用环境光颜色绘制；这里只添加中等强度的周边照明，
            // 不使用任何GlowMask或忽略环境光的自发光绘制。
            Lighting.AddLight(
                Projectile.Center,
                LightColor.ToVector3() * 0.6f * fade);

            if (travelledTiles >= FadeEndTiles)
                Projectile.Kill();
        }

        public override bool? CanDamage()
        {
            return TravelledPixels / 16f <= DamageDistanceTiles
                ? null
                : false;
        }

        public override bool TileCollideStyle(
            ref int width,
            ref int height,
            ref bool fallThrough,
            ref Vector2 hitboxCenterFrac)
        {
            // 只压缩地形碰撞所使用的宽度。Projectile本身的width/height
            // 完全不改，因此NPC伤害判定箱仍保持原来的大小。
            // 普通剑气原宽64px，半宽为32px；强化剑气虽然视觉与伤害
            // 判定放大1.5倍，地形碰撞宽度仍与普通剑气统一为32px。
            width = 32;
            // 地形碰撞使用不随贴图旋转的世界坐标AABB。强化剑气原本
            // 还会把垂直厚度从28px放大到42px，水平发射时因此擦到地面。
            // 地形碰撞厚度统一为普通剑气的28px，伤害框不受影响。
            height = 28;
            hitboxCenterFrac = new Vector2(0.5f, 0.5f);
            return true;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return lightColor * Projectile.Opacity;
        }
    }

    public sealed class WyrmBladeGreatswordProjectile1 :
        WyrmBladeGreatswordProjectileBase
    {
        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeGreatswordProjectile_1";

        protected override float DamageDistanceTiles => 15f;
        protected override float FadeStartTiles => 10f;
        protected override float FadeEndTiles => 18f;
        protected override Color LightColor => new Color(255, 188, 204);
        protected override DamageClass ProjectileDamageType => DamageClass.Melee;
    }

    public sealed class WyrmBladeGreatswordProjectile2 :
        WyrmBladeGreatswordProjectileBase
    {
        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeGreatswordProjectile_2";

        protected override float DamageDistanceTiles => 35f;
        protected override float FadeStartTiles => 30f;
        protected override float FadeEndTiles => 40f;
        protected override Color LightColor => new Color(255, 213, 65);
        protected override DamageClass ProjectileDamageType =>
            ModContent.GetInstance<MagicalMeleeDamageClass>();
        protected override float SizeScale => 1.5f;
    }
}
