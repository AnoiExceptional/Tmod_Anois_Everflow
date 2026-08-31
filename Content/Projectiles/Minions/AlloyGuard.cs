using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs.Minions;

namespace everflow.Content.Projectiles.Minions
{
    public class AlloyGuard : ModProjectile
    {
        private Vector2 velocityBeforeAI;
        private bool wasFlyingBeforeAI;
        private bool forceAttackMode;
        private bool wasForcedToAttack;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Main.projFrames[ProjectileID.OneEyedPirate];
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.OneEyedPirate);
            AIType = ProjectileID.OneEyedPirate;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;

            // 贴图帧高42，原版海盗碰撞箱更矮；上移贴图使脚底与地面重合。
            DrawOriginOffsetY = -8;
        }

        public override bool MinionContactDamage() => true;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 原版海盗随从依靠物块碰撞落地和跳跃，自定义弹幕默认会在这里销毁。
            // 保留碰撞后的速度结果，但阻止弹幕死亡。
            return false;
        }

        public override bool PreAI()
        {
            Player owner = Main.player[Projectile.owner];

            // AI_067_FreakingPirates 专门用 localAI[0] 累计排泄计时。
            // 每帧在原版 AI 运行前清零，使无机生命体永远不会进入该分支。
            Projectile.localAI[0] = 0f;

            wasFlyingBeforeAI = !Projectile.tileCollide;
            forceAttackMode = TryFindTarget(owner, out int targetIndex);

            if (forceAttackMode)
            {
                // 只在刚从飞行/回归状态切入索敌时清一次状态。
                // 如果每帧清零，原版海盗 AI 的攻击计时永远无法推进到挥刀动作。
                if (!wasForcedToAttack || wasFlyingBeforeAI)
                {
                    Projectile.ai[0] = 0f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                }

                Projectile.tileCollide = true;
                owner.MinionAttackTargetNPC = targetIndex;
            }

            if (owner.dead)
                owner.ClearBuff(ModContent.BuffType<AlloyGuardBuff>());

            if (owner.HasBuff<AlloyGuardBuff>())
                Projectile.timeLeft = 2;

            velocityBeforeAI = Projectile.velocity;
            return true;
        }

        public override void PostAI()
        {
            Vector2 velocityChange = Projectile.velocity - velocityBeforeAI;

            if (forceAttackMode)
            {
                // 原版 AI 后只恢复物块碰撞，不再破坏其攻击状态与攻击计时。
                Projectile.tileCollide = true;
                Projectile.velocity = velocityBeforeAI + velocityChange * 0.5f;
                Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X, -3f, 3f);
            }
            else if (!Projectile.tileCollide || wasFlyingBeforeAI)
            {
                // 飞行水平速度为步行的2倍；不再逐帧乘2。
                Projectile.velocity = velocityBeforeAI + velocityChange * 0.5f;
                Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X, -6f, 6f);
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y, -10f, 10f);
            }
            else
            {
                // 步行仍维持原有的50%加速度与3点水平极速。
                Projectile.velocity = velocityBeforeAI + velocityChange * 0.5f;
                Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X, -3f, 3f);
            }

            wasForcedToAttack = forceAttackMode;
        }

        private bool TryFindTarget(Player owner, out int targetIndex)
        {
            targetIndex = -1;

            if (owner.HasMinionAttackTargetNPC)
            {
                NPC priorityTarget = Main.npc[owner.MinionAttackTargetNPC];
                if (priorityTarget.CanBeChasedBy(Projectile) &&
                    Projectile.Distance(priorityTarget.Center) <= 800f)
                {
                    targetIndex = priorityTarget.whoAmI;
                    return true;
                }
            }

            float closestDistance = 800f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetIndex = i;
                }
            }

            return targetIndex >= 0;
        }
    }

}
