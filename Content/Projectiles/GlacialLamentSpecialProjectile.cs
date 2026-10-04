using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class GlacialLamentSpecialProjectile : ModProjectile
    {
        private const int FrozenDuration = 3 * 60;
        private const int ExplosionSize = 10 * 16;
        private static readonly Color LakeBlue = new Color(68, 232, 218);
        private static readonly Color IceBlue = new Color(154, 224, 255);

        public override string Texture =>
            "everflow/Content/Projectiles/GlacialLamentProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 40;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.28f, 0.5f, 0.8f);

            // 以下显形与飞行尘埃逐项复刻原版 Projectile 661。
            if (Projectile.alpha <= 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    int dustIndex = Dust.NewDust(
                        Projectile.position,
                        Projectile.width,
                        Projectile.height,
                        DustID.TintableDustLighted,
                        0f,
                        0f,
                        0,
                        LakeBlue);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 0.3f;
                    Main.dust[dustIndex].noLight = true;
                }
            }

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 55;
                Projectile.scale = 1.3f;
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                    const float dustCount = 16f;
                    for (int i = 0; i < dustCount; i++)
                    {
                        Vector2 offset = -Vector2.UnitY
                            .RotatedBy(i * (MathHelper.TwoPi / dustCount)) *
                            new Vector2(1f, 4f);
                        offset = offset.RotatedBy(Projectile.velocity.ToRotation());
                        int dustIndex = Dust.NewDust(
                            Projectile.Center,
                            0,
                            0,
                            DustID.TintableDustLighted,
                            0f,
                            0f,
                            0,
                            IceBlue);
                        Main.dust[dustIndex].scale = 1.5f;
                        Main.dust[dustIndex].noLight = true;
                        Main.dust[dustIndex].noGravity = true;
                        Main.dust[dustIndex].position = Projectile.Center + offset;
                        Main.dust[dustIndex].velocity =
                            Main.dust[dustIndex].velocity * 4f +
                            Projectile.velocity * 0.3f;
                    }
                }
            }
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            MonsterColdControlNPC.TryApplyFrozen(target, FrozenDuration);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            Projectile.position = center - new Vector2(ExplosionSize * 0.5f);
            Projectile.width = ExplosionSize;
            Projectile.height = ExplosionSize;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
            Projectile.tileCollide = false;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item14, center);

            // 以下四组爆炸尘埃逐项复刻原版 Projectile 661 的 OnKill。
            Vector2 dustArea = center - Vector2.One * 20f;
            const int dustAreaSize = 40;

            for (int i = 0; i < 4; i++)
            {
                int dustIndex = Dust.NewDust(
                    dustArea, dustAreaSize, dustAreaSize,
                    DustID.TintableDustLighted,
                    0f, 0f, 100, LakeBlue, 1.5f);
                Main.dust[dustIndex].position = center +
                    Vector2.UnitY.RotatedByRandom(MathHelper.Pi) *
                    (float)Main.rand.NextDouble() * dustAreaSize / 2f;
            }

            for (int i = 0; i < 20; i++)
            {
                int dustIndex = Dust.NewDust(
                    dustArea, dustAreaSize, dustAreaSize,
                    DustID.TintableDustLighted,
                    0f, 0f, 200, IceBlue, 3.7f);
                Main.dust[dustIndex].position = center +
                    Vector2.UnitY.RotatedByRandom(MathHelper.Pi) *
                    (float)Main.rand.NextDouble() * dustAreaSize / 2f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].velocity *= 3f;
                Main.dust[dustIndex].velocity +=
                    center.DirectionTo(Main.dust[dustIndex].position) *
                    (2f + Main.rand.NextFloat() * 4f);

                dustIndex = Dust.NewDust(
                    dustArea, dustAreaSize, dustAreaSize,
                    DustID.TintableDustLighted,
                    0f, 0f, 100, IceBlue * 0.5f, 1.5f);
                Main.dust[dustIndex].position = center +
                    Vector2.UnitY.RotatedByRandom(MathHelper.Pi) *
                    (float)Main.rand.NextDouble() * dustAreaSize / 2f;
                Main.dust[dustIndex].velocity *= 2f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].fadeIn = 1f;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].velocity +=
                    center.DirectionTo(Main.dust[dustIndex].position) * 8f;
            }

            for (int i = 0; i < 20; i++)
            {
                int dustIndex = Dust.NewDust(
                    dustArea, dustAreaSize, dustAreaSize,
                    DustID.TintableDustLighted,
                    0f, 0f, 0, IceBlue, 2.7f);
                Main.dust[dustIndex].position = center +
                    Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                        .RotatedBy(Projectile.velocity.ToRotation()) *
                    dustAreaSize / 2f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].velocity *= 3f;
                Main.dust[dustIndex].velocity +=
                    center.DirectionTo(Main.dust[dustIndex].position) * 2f;
            }

            for (int i = 0; i < 70; i++)
            {
                int dustIndex = Dust.NewDust(
                    dustArea, dustAreaSize, dustAreaSize,
                    DustID.TintableDustLighted,
                    0f, 0f, 0, LakeBlue, 1.5f);
                Main.dust[dustIndex].position = center +
                    Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                        .RotatedBy(Projectile.velocity.ToRotation()) *
                    dustAreaSize / 2f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 3f;
                Main.dust[dustIndex].velocity +=
                    center.DirectionTo(Main.dust[dustIndex].position) * 3f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D aura = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/GlacialLamentProjectile_Aura").Value;
            Texture2D eye = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/GlacialLamentProjectile_Eye").Value;
            Vector2 auraOrigin = aura.Size() * 0.5f;

            // 原版 Extra_75 构成特殊弹外围的大型暗焰。先绘制缓存位置，
            // 再在弹体处叠加湖蓝外焰和冰蓝内焰，恢复原版醒目的体积感。
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float progress = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f -
                                    Main.screenPosition;
                Main.EntitySpriteDraw(
                    aura,
                    oldCenter,
                    null,
                    LakeBlue * (0.08f + progress * 0.22f),
                    Projectile.oldRot[i],
                    auraOrigin,
                    1.45f + progress * 0.2f,
                    SpriteEffects.None);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(
                aura,
                drawPosition,
                null,
                LakeBlue * 0.72f,
                Projectile.rotation,
                auraOrigin,
                2f,
                SpriteEffects.None);
            Main.EntitySpriteDraw(
                aura,
                drawPosition,
                null,
                IceBlue * 0.72f,
                Projectile.rotation,
                auraOrigin,
                1.25f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                eye,
                drawPosition,
                null,
                IceBlue,
                Projectile.rotation,
                eye.Size() * 0.5f,
                1.35f,
                SpriteEffects.None);
            return false;
        }
    }
}
