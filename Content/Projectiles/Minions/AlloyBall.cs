using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs.Minions;

namespace everflow.Content.Projectiles.Minions
{
    public class AlloyBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Main.projFrames[ProjectileID.DeadlySphere];
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            // 让原版召唤武器右键流程选择敌人并显示 Extra[199] 标记圈。
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.DeadlySphere);
            AIType = ProjectileID.DeadlySphere;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
        }

        public override bool MinionContactDamage() => true;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 原版致命球由自身 AI 处理物块接触；自定义弹幕默认返回 true 会被直接销毁。
            return false;
        }

        public override bool PreAI()
        {
            Player owner = Main.player[Projectile.owner];

            if (owner.dead)
                owner.ClearBuff(ModContent.BuffType<AlloyBallBuff>());

            if (owner.HasBuff<AlloyBallBuff>())
                Projectile.timeLeft = 2;

            return true;
        }
    }
}
