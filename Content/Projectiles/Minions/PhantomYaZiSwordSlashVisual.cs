using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles.Minions
{
    public sealed class PhantomYaZiSwordSlashVisual : ModProjectile
    {
        private const int SlashDuration = 30;
        private const int FirstVisibleFrame = SlashDuration / 2;
        private const int VisualFrameDuration = 5;
        private const int FrameCount = 3;
        private const float SlashArc = MathHelper.Pi * 4f / 3f;
        private const float BladeLength = 112f;
        private const float MaximumSwordScale = 1.5f;
        private const float EffectScale = 2f;
        private const float EffectOpacity = 0.5f;

        private int ParentIdentity => (int)Projectile.ai[0];
        private int SlashDirection => Math.Sign(Projectile.ai[1]);
        private float SlashStartRotation => Projectile.ai[2];
        private int Age => (int)Projectile.localAI[0];

        public override string Texture =>
            "everflow/Content/Projectiles/YaZi_Sword_Slash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 80;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile parent = FindParent();
            bool hasStartedDrawing = Projectile.localAI[1] == 1f;

            if (Age > SlashDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            if (!hasStartedDrawing)
            {
                if (parent is null ||
                    parent.ModProjectile is not PhantomYaZiSwordSentry sword ||
                    !sword.IsPerformingSlash)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = parent.Center;
                if (Age >= FirstVisibleFrame)
                    Projectile.localAI[1] = 1f;
            }

            // 特效始终对准本轮斩击的中线，不随剑身继续旋转。
            Projectile.rotation = SlashStartRotation - MathHelper.PiOver4 +
                SlashDirection * SlashArc * 0.5f;
            Projectile.scale = EffectScale;

            if (Age >= FirstVisibleFrame)
            {
                Projectile.frame = Math.Min(
                    (Age - FirstVisibleFrame) / VisualFrameDuration,
                    FrameCount - 1);
            }

            Projectile.localAI[0]++;
        }

        private Projectile FindParent()
        {
            int parentType = ModContent.ProjectileType<PhantomYaZiSwordSentry>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (candidate.active &&
                    candidate.owner == Projectile.owner &&
                    candidate.identity == ParentIdentity &&
                    candidate.type == parentType)
                {
                    return candidate;
                }
            }

            return null;
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Age <= FirstVisibleFrame)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, FrameCount, 0, Projectile.frame);
            SpriteEffects effects = SlashDirection > 0
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;
            // 右边缘与剑身经过中线、放大到150%时的最外缘重合。
            float slashOuterRadius = BladeLength * MaximumSwordScale;
            float originX = source.Width - slashOuterRadius / EffectScale;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                source,
                Color.White * EffectOpacity,
                Projectile.rotation,
                new Vector2(originX, source.Height * 0.5f),
                Projectile.scale,
                effects,
                0f);
            return false;
        }
    }
}
