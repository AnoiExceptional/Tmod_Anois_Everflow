using System.Collections.Generic;
using everflow.Common.Combat;
using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class SethSwordProjectile : ChainSwordProjectile
    {
        private const int FrameWidth = 22;

        // NodeCount包含剑柄。曲线会缩短玩家到剑尖的直线距离，因此使用
        // 约453px路径和55个剑身节点，使视觉攻击半径达到约25格。
        // 满伸展时节点间距约8.1px，小于普通剑节10px长度，始终略有重叠。
        public static readonly ChainSwordSettings ChainSwordStyle = new(
            NodeCount: 56,
            DrawBodyCurve: false,
            NodeScale: 1f,
            HandleScale: 1f,
            MaxAttackDistance: 453f,
            AlternateReverse: false,
            NodesGlow: false);

        protected override ChainSwordSettings Settings => ChainSwordStyle;
        protected override Color NodeGlowColor => new(255, 206, 55);
        protected override float NodeGlowStrength => 0.3f;
        protected override float HandlePivotExtension => 5f;

        protected override void ConfigureChainSwordProjectile()
        {
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
        }

        protected override Rectangle GetHandleFrame(Texture2D texture) =>
            new(0, 0, FrameWidth, 28);

        protected override Rectangle GetNodeFrame(Texture2D texture, int nodeIndex)
        {
            if (nodeIndex >= Settings.NodeCount - 1)
                return new Rectangle(0, 60, FrameWidth, 12);
            if (nodeIndex >= Settings.NodeCount - 2)
                return new Rectangle(0, 46, FrameWidth, 10);
            return new Rectangle(0, 32, FrameWidth, 10);
        }

        protected override void UpdateChainSword(IReadOnlyList<Vector2> controlPoints)
        {
            for (int i = 0; i < controlPoints.Count; i += 5)
            {
                Lighting.AddLight(
                    controlPoints[i],
                    new Vector3(0.45f, 0.33f, 0.075f));
            }

            Lighting.AddLight(
                Main.player[Projectile.owner].Center,
                new Vector3(0.60f, 0.44f, 0.10f));

            if (Main.dedServ || controlPoints.Count < 2 ||
                !Main.rand.NextBool(2))
            {
                return;
            }

            int segment = Main.rand.Next(controlPoints.Count - 1);
            Vector2 position = Vector2.Lerp(
                controlPoints[segment],
                controlPoints[segment + 1],
                Main.rand.NextFloat());
            Dust dust = Dust.NewDustPerfect(
                position,
                DustID.TintableDust,
                Main.rand.NextVector2Circular(0.7f, 0.7f),
                100,
                new Color(255, 205, 55),
                Main.rand.NextFloat(0.7f, 1.05f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<SethSwordTag>(), 4 * 60);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
        }
    }
}
