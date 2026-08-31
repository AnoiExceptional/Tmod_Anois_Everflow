using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AncientTrooperMeleeSlash : ModProjectile
    {
        private const int SwingDuration = 17;

        public override string Texture => "everflow/Content/Items/AlloyDagger";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SwingDuration;
        }

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC owner = Main.npc[ownerIndex];
            if (!owner.active || owner.type != ModContent.NPCType<Content.NPCs.AncientTrooper_Melee>())
            {
                Projectile.Kill();
                return;
            }

            float progress = 1f - Projectile.timeLeft / (float)SwingDuration;
            float swingAngle = MathHelper.Lerp(-1.8f, 0.9f, progress);
            Vector2 bladeDirection = new Vector2(
                System.MathF.Cos(swingAngle) * owner.direction,
                System.MathF.Sin(swingAngle));

            Projectile.Center = owner.Center + new Vector2(0f, -5f) + bladeDirection * 30f;
            Projectile.rotation = bladeDirection.ToRotation() + MathHelper.PiOver4;
            if (owner.direction == -1)
                Projectile.rotation += MathHelper.PiOver2;
            Projectile.spriteDirection = owner.direction;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Main.npc[ownerIndex].Center + new Vector2(0f, -5f),
                Projectile.Center,
                18f,
                ref collisionPoint);
        }
    }
}
