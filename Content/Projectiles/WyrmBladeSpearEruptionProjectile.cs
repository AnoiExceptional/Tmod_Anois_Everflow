using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladeSpearEruptionProjectile : ModProjectile
    {
        private static readonly Color SpearFlareColor =
            new Color(210, 75, 107, 0);
        private static readonly Color SegmentLinkColor =
            new Color(255, 236, 167);
        private const int SegmentCount = 5;
        private const int FrameWidth = 30;
        private const int FrameHeight = 46;
        private const float SegmentGap = 2f;
        private const int SourceTopPadding = 2;
        private const float DrawScale = 2f;
        private const float MaximumSegmentGap = FrameHeight * DrawScale * 0.5f;
        private const float MaximumSegmentPitch =
            FrameHeight * DrawScale + MaximumSegmentGap;
        private const float WeaponForwardOffset = -72f;
        private const float TipCollisionExtension = 30f;
        private const float CollisionWidth = 48f;

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeSpearProjectile_3";

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        private float Extension
        {
            get
            {
                float age = Projectile.localAI[0];
                int attackDuration = System.Math.Max(2, (int)Projectile.localAI[1]);
                int extendDuration = System.Math.Max(
                    1, (int)System.MathF.Round(attackDuration * 0.6f));
                int holdDuration = System.Math.Max(
                    1, (int)System.MathF.Round(attackDuration * 0.08f));
                if (extendDuration + holdDuration >= attackDuration)
                    holdDuration = 0;

                if (age < extendDuration)
                {
                    float progress = age /
                        System.Math.Max(1f, extendDuration - 1f);
                    return 1f - (1f - progress) * (1f - progress);
                }

                if (age < extendDuration + holdDuration)
                    return 1f;

                float retractProgress =
                    (age - extendDuration - holdDuration) /
                    System.Math.Max(
                        1f,
                        attackDuration - extendDuration - holdDuration - 1f);
                return 1f - MathHelper.Clamp(retractProgress, 0f, 1f);
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.localAI[1] <= 0f)
                Projectile.localAI[1] = System.Math.Max(2, player.itemAnimationMax);
            int attackDuration = (int)Projectile.localAI[1];
            int age = (int)Projectile.localAI[0];
            if (!player.active || player.dead || player.noItems || player.CCed ||
                age >= attackDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            Projectile.velocity = direction;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
            Projectile.rotation = direction.ToRotation() - MathHelper.PiOver2;

            // 右键长枪沿各节提供微弱暖黄色照明。
            float pitch = MaximumSegmentPitch * Extension;
            Vector2 weaponBase =
                Projectile.Center + direction * WeaponForwardOffset;
            for (int i = 0; i < SegmentCount; i++)
            {
                Vector2 lightPosition = weaponBase + direction *
                    (pitch * i + FrameHeight * DrawScale * 0.5f);
                Lighting.AddLight(
                    lightPosition,
                    SegmentLinkColor.ToVector3() * 0.18f);
            }

            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation;
            player.SetCompositeArmFront(
                true,
                Player.CompositeArmStretchAmount.Full,
                direction.ToRotation() - MathHelper.PiOver2 - player.fullRotation);

            Projectile.localAI[0]++;
        }

        private void GetSpearLine(out Vector2 start, out Vector2 end)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pitch = MaximumSegmentPitch * Extension;
            start = Projectile.Center + direction * WeaponForwardOffset;
            end = start + direction * (pitch * (SegmentCount - 1) +
                FrameHeight * DrawScale + TipCollisionExtension);
        }

        public override bool? Colliding(
            Rectangle projectileHitbox,
            Rectangle targetHitbox)
        {
            GetSpearLine(out Vector2 start, out Vector2 end);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                CollisionWidth,
                ref collisionPoint);
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            // 每穿过并命中一个敌人，后续命中的基础伤害衰减25%。
            Projectile.damage = System.Math.Max(
                1,
                (int)(Projectile.damage * 0.75f));
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pitch = MaximumSegmentPitch * Extension;
            Vector2 origin = new Vector2(FrameWidth * 0.5f, 0f);
            Vector2 weaponBase =
                Projectile.Center + direction * WeaponForwardOffset;

            DrawSegmentLinks(direction, pitch, weaponBase);

            for (int i = 0; i < SegmentCount; i++)
            {
                // 30x240贴图顶部有2像素留白，之后依次存放五个
                // 互不重复的30x46节段，相邻源区域之间留2像素。
                Rectangle source = new Rectangle(
                    0,
                    SourceTopPadding + i * (FrameHeight + (int)SegmentGap),
                    FrameWidth,
                    FrameHeight);
                Vector2 position = weaponBase + direction * (pitch * i);
                Main.EntitySpriteDraw(
                    texture,
                    position - Main.screenPosition,
                    source,
                    Color.Lerp(lightColor, SegmentLinkColor, 0.48f),
                    Projectile.rotation,
                    origin,
                    DrawScale,
                    SpriteEffects.None,
                    0f);
            }

            DrawSpearFlare(direction, pitch, weaponBase);

            return false;
        }

        private static void DrawSegmentLinks(
            Vector2 direction,
            float pitch,
            Vector2 weaponBase)
        {
            float drawnSegmentLength = FrameHeight * DrawScale;
            float linkLength = pitch - drawnSegmentLength;
            if (linkLength <= 0f)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            float rotation = direction.ToRotation();
            Vector2 origin = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < SegmentCount - 1; i++)
            {
                Vector2 linkStart = weaponBase + direction *
                    (pitch * i + drawnSegmentLength);
                Vector2 linkCenter =
                    linkStart + direction * (linkLength * 0.5f);
                Main.EntitySpriteDraw(
                    pixel,
                    linkCenter - Main.screenPosition,
                    pixelSource,
                    SegmentLinkColor,
                    rotation,
                    origin,
                    new Vector2(linkLength, 4f),
                    SpriteEffects.None,
                    0f);
            }
        }

        private void DrawSpearFlare(
            Vector2 direction,
            float pitch,
            Vector2 weaponBase)
        {
            float flareStrength = Extension;
            if (flareStrength <= 0f)
                return;

            Texture2D flareTexture = TextureAssets.Extra[98].Value;
            Vector2 tipPosition = weaponBase + direction *
                (pitch * (SegmentCount - 1) + FrameHeight * DrawScale);
            Vector2 flareOrigin = flareTexture.Size() * 0.5f;
            int spriteDirection = direction.X >= 0f ? 1 : -1;
            // 角度只跟随枪轴；若把spriteDirection混入角度，接近垂直时
            // direction.X跨过0会令枪芒瞬间横转90度。
            float flareRotation = direction.ToRotation() + MathHelper.PiOver2;
            SpriteEffects effects = spriteDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Color color = SpearFlareColor * flareStrength;

            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                color,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1f) * DrawScale,
                effects);
            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                color,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1.5f) * DrawScale,
                effects);

            for (float trail = 0.4f; trail <= 1f; trail += 0.1f)
            {
                Vector2 trailPosition = tipPosition - direction *
                    ((1f - trail) * 32f * DrawScale);
                Main.EntitySpriteDraw(
                    flareTexture,
                    trailPosition - Main.screenPosition,
                    null,
                    color * (0.75f * trail),
                    flareRotation,
                    flareOrigin,
                    new Vector2(
                        flareStrength * trail,
                        2f * flareStrength * trail) * DrawScale,
                    effects);
            }
        }
    }
}
