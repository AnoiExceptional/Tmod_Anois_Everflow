using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles.Minions
{
    public sealed class PhantomYaZiSwordIdleVisual : ModProjectile
    {
        private const int FrameCount = 3;
        private const int FrameDuration = 10;
        private const int AnimationDuration = FrameCount * FrameDuration;
        private const int PauseDuration = 30;
        private const int CycleDuration = AnimationDuration + PauseDuration;

        public override string Texture =>
            "everflow/Content/Projectiles/Minions/Phantom_YaZi_Sword_2";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 32;
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
            if (parent is null || parent.ai[0] != 0f ||
                parent.ModProjectile is not PhantomYaZiSwordSentry sword)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = sword.GroundVisualPosition;

            int cycleFrame = (int)Projectile.localAI[0] % CycleDuration;
            bool animationVisible = cycleFrame < AnimationDuration;
            Projectile.frame = animationVisible
                ? cycleFrame / FrameDuration
                : FrameCount - 1;
            Projectile.localAI[1] = animationVisible ? 1f : 0f;
            Projectile.localAI[0]++;
        }

        private Projectile FindParent()
        {
            int parentIdentity = (int)Projectile.ai[0];
            int parentType = ModContent.ProjectileType<PhantomYaZiSwordSentry>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (candidate.active &&
                    candidate.owner == Projectile.owner &&
                    candidate.identity == parentIdentity &&
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
            if (Projectile.localAI[1] == 0f)
                return false;

            float opacity = Projectile.frame switch
            {
                0 => 1f,
                1 => 0.67f,
                _ => 0.33f
            };

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle source = texture.Frame(1, FrameCount, 0, Projectile.frame);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                source,
                Projectile.GetAlpha(lightColor) * opacity,
                Projectile.rotation,
                source.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }
    }
}
