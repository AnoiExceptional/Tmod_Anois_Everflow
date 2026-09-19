using everflow.Content.Buffs;
using everflow.Content.Gores;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladeDaggerProjectile : ModProjectile
    {
        private const int GravityDelay = 30;
        private const float HorizontalDrag = 0.99f;
        private const float Gravity = 0.5f;
        private const float SpinSpeed = 0.32f;

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeDagger_Projectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            // 原版暗影焰刀为30x30、aiStyle 2；运动参数在AI中明确复刻。
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.maxPenetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Projectile.ai[0]++;

            if (Projectile.ai[0] < GravityDelay)
            {
                // 枪尖沿飞行方向；直线飞行阶段不转动。
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                return;
            }

            Projectile.velocity.X *= HorizontalDrag;
            Projectile.velocity.Y += Gravity;

            float spinDirection = Projectile.velocity.X < 0f ? -1f : 1f;
            Projectile.rotation += SpinSpeed * spinDirection;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            Color trailColor = new Color(255, 235, 70);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float opacity =
                    (Projectile.oldPos.Length - i) /
                    (float)(Projectile.oldPos.Length + 1) * 0.45f;

                Vector2 newerPosition = i == 0
                    ? Projectile.position
                    : Projectile.oldPos[i - 1];
                Vector2 trailVelocity = newerPosition - Projectile.oldPos[i];
                float trailRotation = trailVelocity.LengthSquared() > 0.001f
                    ? trailVelocity.ToRotation() + MathHelper.PiOver2
                    : Projectile.rotation;

                // 受重力后本体开始翻转，拖尾也沿用各帧缓存的实际旋转角度。
                if (Projectile.ai[0] >= GravityDelay && Projectile.oldRot[i] != 0f)
                    trailRotation = Projectile.oldRot[i];

                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    trailColor * opacity,
                    trailRotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None);
            }

            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AlloyDaggerMark>(), 300);

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

            SpawnFragments();
            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 保留撞墙前的飞行方向，让碎片沿撞击方向散开。
            Projectile.velocity = oldVelocity;
            SpawnFragments();
            return true;
        }

        private void SpawnFragments()
        {
            if (Main.dedServ || Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            int goreType = ModContent.GoreType<WyrmBladeDagger_Gore01>();
            int count = Main.rand.Next(3, 6);

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Projectile.velocity * 0.12f
                    + Main.rand.NextVector2Circular(3.5f, 3.5f);
                Gore.NewGore(
                    Projectile.GetSource_Death(),
                    Projectile.Center - new Vector2(4f),
                    velocity,
                    goreType,
                    Main.rand.NextFloat(0.85f, 1.15f));
            }

            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
        }
    }
}
