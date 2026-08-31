using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AncientTrooperArrow : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WoodenArrowFriendly}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.friendly = false;
            Projectile.hostile = true;
        }
    }

    public sealed class AncientTrooperRangedBow : ModProjectile
    {
        private const int DisplayTime = 25;

        public override string Texture => "everflow/Content/Items/AlloyBow";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = DisplayTime;
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
            if (!owner.active || owner.type != ModContent.NPCType<Content.NPCs.AncientTrooper_Ranged>() ||
                !owner.HasValidTarget)
            {
                Projectile.Kill();
                return;
            }

            Vector2 aimDirection = (Main.player[owner.target].Center - owner.Center)
                .SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.Center = owner.Center + new Vector2(0f, -6f) + aimDirection * 13f;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.spriteDirection = aimDirection.X >= 0f ? 1 : -1;
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;
        }
    }
}
