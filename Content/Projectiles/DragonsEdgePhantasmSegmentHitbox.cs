using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class DragonsEdgePhantasmSegmentHitbox : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.usesIDStaticNPCImmunity = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile parent = null;
            int parentIdentity = (int)Projectile.ai[0];
            int parentType = ModContent.ProjectileType<DragonsEdgePhantasmProjectile>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (candidate.active &&
                    candidate.owner == Projectile.owner &&
                    candidate.identity == parentIdentity &&
                    candidate.type == parentType)
                {
                    parent = candidate;
                    break;
                }
            }

            if (parent?.ModProjectile is not DragonsEdgePhantasmProjectile dragon)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = dragon.GetSegmentCenter((int)Projectile.ai[1]);
            Projectile.velocity = parent.velocity;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
