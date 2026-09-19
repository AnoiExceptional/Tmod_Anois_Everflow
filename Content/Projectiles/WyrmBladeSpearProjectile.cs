using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladeSpearProjectile : ModProjectile
    {
        private static readonly Color SpearFlareColor = new Color(210, 75, 107, 0);

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeSpearProjectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.aiStyle = ProjAIStyleID.Spear;
            AIType = ProjectileID.TitaniumTrident;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.scale = 2f;
        }

        // 让原版长枪绘制器继续负责握持点、方向和重力翻转，只改变
        // 最终颜色以提供微弱自发光，避免普通射弹原点造成贴图离手。
        public override Color? GetAlpha(Color lightColor) =>
            Color.Lerp(lightColor, new Color(210, 75, 107), 0.18f);

        public override void PostDraw(Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.itemAnimationMax <= 0)
                return;

            // 只在长枪向外伸出的前2/3动作中播放枪芒；到达最远点后
            // 强度已经归零，回收阶段不会再次出现。
            float outwardProgress = Utils.Remap(
                player.itemAnimation,
                player.itemAnimationMax,
                player.itemAnimationMax / 3f,
                0f,
                1f,
                true);
            float flareStrength = Utils.Remap(
                outwardProgress,
                0f,
                0.3f,
                0f,
                1f,
                true) * Utils.Remap(
                    outwardProgress,
                    0.3f,
                    1f,
                    1f,
                    0f,
                    true);
            flareStrength = 1f - (1f - flareStrength) * (1f - flareStrength);
            if (flareStrength <= 0f)
                return;

            Texture2D flareTexture = TextureAssets.Extra[98].Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 原版长枪AI已将Projectile.Center放在伸缩枪尖处，不能再叠加
            // 半张贴图的长度，否则枪芒会被推到枪身之外。
            Vector2 tipPosition = Projectile.Center;
            Vector2 flareOrigin = flareTexture.Size() * 0.5f;
            float flareRotation = Projectile.rotation -
                MathHelper.PiOver4 * Projectile.spriteDirection + MathHelper.Pi;
            if (player.gravDir < 0f)
            {
                flareRotation -= MathHelper.PiOver2 *
                    Projectile.spriteDirection;
            }

            SpriteEffects effects = Projectile.spriteDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Color color = SpearFlareColor * flareStrength;
            const float doubledScale = 2f;

            // 两层核心枪芒，以及原版同款沿枪轴向后收束的锐光残影。
            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                color,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1f) * doubledScale,
                effects);
            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                color,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1.5f) * doubledScale,
                effects);

            for (float trail = 0.4f; trail <= 1f; trail += 0.1f)
            {
                Vector2 trailPosition = tipPosition -
                    direction * ((1f - trail) * 32f * doubledScale);
                Main.EntitySpriteDraw(
                    flareTexture,
                    trailPosition - Main.screenPosition,
                    null,
                    color * (0.75f * trail),
                    flareRotation,
                    flareOrigin,
                    new Vector2(
                        flareStrength * trail,
                        2f * flareStrength * trail) * doubledScale,
                    effects);
            }
        }
    }
}
