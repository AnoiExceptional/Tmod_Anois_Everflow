using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class AnubisForbiddenPhantom : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/ForbiddenPhantom";

        private int Side => Projectile.ai[0] < 0f ? -1 : 1;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 50;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            int phantomType = ModContent.ProjectileType<AnubisForbiddenPhantom>();

            // ownedProjectileCounts 在弹幕生成当帧尚未必刷新。若立刻检查是否
            // 恰好存在两个分身，会让刚召出的两个分身在首帧一起自毁。
            Projectile.localAI[0]++;
            bool pairMissingAfterSpawnGrace =
                Projectile.localAI[0] > 10f &&
                owner.ownedProjectileCounts[phantomType] != 2;

            if (!owner.active || owner.dead ||
                owner.HeldItem.type != ModContent.ItemType<AnubisStaff>() ||
                pairMissingAfterSpawnGrace)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.spriteDirection = owner.direction;

            float bob = MathF.Sin((float)Main.GameUpdateCount * 0.055f +
                                  (Side < 0 ? 0f : MathHelper.Pi)) * 5f;
            Vector2 destination = owner.Center + new Vector2(Side * 52f, -14f + bob);

            if (Projectile.Distance(destination) > 240f)
                Projectile.Center = destination;
            else
                Projectile.Center = Vector2.Lerp(Projectile.Center, destination, 0.24f);

            Projectile.velocity = Vector2.Zero;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            string texturePath = owner.Male
                ? "everflow/Content/Projectiles/ForbiddenPhantom"
                : "everflow/Content/Projectiles/ForbiddenPhantom_2";
            Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // 三层很淡的残影让分身接近神圣套幻影的视觉质感。
            for (int i = 3; i >= 1; i--)
            {
                float phase = (float)Main.GameUpdateCount * 0.05f + i * 1.8f;
                Vector2 offset = new Vector2(MathF.Cos(phase), MathF.Sin(phase)) * i;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    Color.White * (0.055f * i),
                    0f,
                    origin,
                    1f,
                    effects);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White * 0.55f,
                0f,
                origin,
                1f,
                effects);
            return false;
        }
    }
}
