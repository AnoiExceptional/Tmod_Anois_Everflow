using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AnubisStaffProjectile : ModProjectile
    {
        private bool spawnedManaReturn;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            // ai[1] 标记由禁戒幻影发射的镰刃。通过同步标记在服务端和
            // 其他客户端同样恢复独立无敌帧设置，避免联机时重新共享判定。
            if (Projectile.ai[1] == 1f)
            {
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
            }

            Projectile.rotation += 0.63f * Projectile.direction;

            // 高强度黄色照明与中等密度的黄色全亮粒子。
            Lighting.AddLight(Projectile.Center, 1.15f, 0.92f, 0.2f);
            if (Main.rand.NextBool(2))
            {
                Color color = new Color(255, 220, 64);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.09f),
                    70,
                    color,
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool TileCollideStyle(
            ref int width,
            ref int height,
            ref bool fallThrough,
            ref Vector2 hitboxCenterFrac)
        {
            // 仅缩小物块碰撞箱，避免水平射击时镰刃下缘被脚下地面截停；
            // 对敌伤害仍使用 Projectile 本身的 40x40 判定箱。
            width = 18;
            height = 18;
            hitboxCenterFrac = new Vector2(0.5f, 0.5f);
            return true;
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 230, 96);

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (spawnedManaReturn || target.life > 0 || Projectile.owner != Main.myPlayer)
                return;

            spawnedManaReturn = true;
            Player owner = Main.player[Projectile.owner];
            if (owner.statMana >= owner.statManaMax2)
                return;

            Vector2 velocity = target.Center.DirectionTo(owner.MountedCenter) * 8f;
            Projectile.NewProjectile(
                Projectile.GetSource_OnHit(target),
                target.Center,
                velocity,
                ModContent.ProjectileType<AnubisManaReturnProjectile>(),
                0,
                0f,
                Projectile.owner,
                50f);
        }
    }
}
