using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;
using everflow.Content.Buffs;

namespace everflow.Content.Projectiles
{
    public class AlloyChainswordProjectile : ChainSwordProjectile
    {
        public static readonly ChainSwordSettings ChainSwordStyle = new(
            NodeCount: 10,
            DrawBodyCurve: true,
            MaxAttackDistance: 240f,
            BodyCurveGlows: false,
            NodesGlow: true);

        protected override ChainSwordSettings Settings => ChainSwordStyle;
        protected override Color BodyCurveColor => new(139, 147, 175, 190);
        protected override float BodyCurveWidth => 2f;

        protected override void ConfigureChainSwordProjectile()
        {
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
        }

        protected override Rectangle GetHandleFrame(Texture2D texture) =>
            new(0, 0, 22, 24);

        protected override Rectangle GetNodeFrame(Texture2D texture, int nodeIndex)
        {
            if (nodeIndex >= Settings.NodeCount - 1)
                return new Rectangle(0, 118, 22, 20); // 链刃尖端

            float progress = nodeIndex / (float)(Settings.NodeCount - 1);
            if (progress > 2f / 3f)
                return new Rectangle(0, 90, 22, 20);
            if (progress > 1f / 3f)
                return new Rectangle(0, 62, 22, 20);
            return new Rectangle(0, 34, 22, 20);
        }

        protected override void UpdateChainSword(IReadOnlyList<Vector2> controlPoints)
        {
            Color effectColor = new(139, 147, 175);

            // 沿链身提供较弱照明，不把整条链刃照成高亮光源。
            for (int i = 0; i < controlPoints.Count; i += 2)
                Lighting.AddLight(controlPoints[i], effectColor.ToVector3() * 0.18f);

            // 使用可染色的发光粒子，颜色严格跟随鞭身曲线。链剑具有额外
            // 更新，因此二分之一概率既能清晰可见，也不会形成粒子幕墙。
            if (Main.dedServ || controlPoints.Count < 2 || !Main.rand.NextBool(2))
                return;

            int segment = Main.rand.Next(controlPoints.Count - 1);
            Vector2 position = Vector2.Lerp(
                controlPoints[segment],
                controlPoints[segment + 1],
                Main.rand.NextFloat());
            Dust dust = Dust.NewDustPerfect(
                position,
                DustID.TintableDustLighted,
                Main.rand.NextVector2Circular(0.45f, 0.45f),
                110,
                effectColor,
                Main.rand.NextFloat(0.65f, 0.9f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AlloyChainswordTag>(), 240);

            Player owner = Main.player[Projectile.owner];
            owner.MinionAttackTargetNPC = target.whoAmI;

            Projectile.damage = (int)(Projectile.damage * 0.5f);
        }
    }
}
