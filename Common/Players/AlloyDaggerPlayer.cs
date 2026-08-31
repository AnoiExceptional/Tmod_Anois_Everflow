using Terraria;
using Terraria.ModLoader;
using everflow.Content.Buffs;
using everflow.Content.Projectiles;

namespace everflow.Common.Players
{
    public class AlloyDaggerPlayer : ModPlayer
    {
        public override void OnHitNPCWithProj(
            Projectile proj,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (proj.owner != Player.whoAmI ||
                proj.type != ModContent.ProjectileType<AlloyDaggerProjectile>())
            {
                return;
            }

            target.AddBuff(ModContent.BuffType<AlloyDaggerMark>(), 300);
            Player.MinionAttackTargetNPC = target.whoAmI;
        }
    }
}
