using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class DragonsEdgeSlashProjectile : ModProjectile
    {
        private static readonly Color SlashColor = new(246, 162, 168);
        private Vector2 launchCenter;
        private bool launchCenterInitialized;
        private bool expiredByRange;
        private float rangeOpacity = 1f;

        private float MaxRange => Projectile.ai[0];

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile_4";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            // 实际穿透数会在首次 AI 中按当前成长阶段覆盖。
            Projectile.penetrate = 5;
            Projectile.maxPenetrate = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.scale = 2f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.usesIDStaticNPCImmunity = false;
        }

        public override void AI()
        {
            if (!launchCenterInitialized)
            {
                launchCenterInitialized = true;
                launchCenter = Projectile.Center;

                // 表格中的“穿透敌人数”表示穿过这些敌人后，命中下一名时消失。
                int totalHits = System.Math.Max(1, (int)Projectile.ai[1] + 1);
                Projectile.penetrate = totalHits;
                Projectile.maxPenetrate = totalHits;

                if (MaxRange > 0f)
                {
                    int requiredLifetime =
                        (int)System.Math.Ceiling(MaxRange /
                            System.Math.Max(Projectile.velocity.Length(), 0.01f)) + 2;
                    Projectile.timeLeft = System.Math.Max(
                        Projectile.timeLeft,
                        requiredLifetime);
                }
                else
                {
                    // “无限”阶段仍保留安全清理时间，避免永久弹幕堆积。
                    Projectile.timeLeft = 300;
                }
            }

            if (MaxRange > 0f)
            {
                float travelled = Vector2.Distance(launchCenter, Projectile.Center);
                float fadeStart = MaxRange * 0.8f;
                rangeOpacity = 1f - MathHelper.Clamp(
                    (travelled - fadeStart) / (MaxRange - fadeStart),
                    0f,
                    1f);

                if (travelled >= MaxRange)
                {
                    expiredByRange = true;
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;

            Lighting.AddLight(
                Projectile.Center,
                SlashColor.ToVector3() * (0.55f * rangeOpacity));

            for (int i = 0; i < 2 && Main.rand.NextFloat() < rangeOpacity; i++)
            {
                Vector2 position = Projectile.Center +
                    Main.rand.NextVector2Circular(
                        Projectile.width * 0.4f,
                        Projectile.height * 0.4f);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Enchanted_Pink,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.10f) +
                        Main.rand.NextVector2Circular(0.8f, 0.8f),
                    80,
                    SlashColor,
                    Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
                dust.fadeIn = 1.05f;
            }
        }

        public override bool? CanDamage() =>
            MaxRange <= 0f ||
            !launchCenterInitialized ||
            Vector2.DistanceSquared(launchCenter, Projectile.Center) < MaxRange * MaxRange;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (MaxRange > 0f && launchCenterInitialized)
            {
                Vector2 targetCenter = targetHitbox.Center.ToVector2();
                if (Vector2.DistanceSquared(launchCenter, targetCenter) > MaxRange * MaxRange)
                    return false;
            }

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            // 射程末端已经完成渐隐，不再额外爆出一团粒子破坏消失效果。
            if (expiredByRange)
                return;

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Enchanted_Pink,
                    Main.rand.NextVector2Circular(2.5f, 2.5f),
                    80,
                    SlashColor,
                    Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DragonsEdgeHitSlashProjectile>(),
                0,
                0f,
                Projectile.owner,
                1f,
                Main.rand.NextFloat(-MathHelper.ToRadians(12f),
                    MathHelper.ToRadians(12f)));
        }

        public override Color? GetAlpha(Color lightColor) =>
            Color.White * (0.5f * rangeOpacity);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();

            // 碰撞箱为视觉尺寸160x96，源贴图为80x48并以2倍绘制。
            // 默认绘制会让大碰撞箱参与原点偏移，因此显式使用源贴图中心，
            // 确保剑气图像、粒子轨迹和伤害判定共享同一个中心点。
            Vector2 origin = frame.Size() * 0.5f;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White * (0.5f * rangeOpacity),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
