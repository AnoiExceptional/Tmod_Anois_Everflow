using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using everflow.Common.Combat;

namespace everflow.Content.Projectiles
{
    public class ThousandBladesWhipProjectile : ChainSwordProjectile
    {
        private const int FrameWidth = 18;
        private const int FrameHeight = 30;
        private const int BodyFrameCount = 12;
        private static readonly Color ThemeColor = new(156, 139, 219);
        private float[] shardTriggerTimes = Array.Empty<float>();
        private bool[] shardTriggered = Array.Empty<bool>();

        public static readonly ChainSwordSettings ChainSwordStyle = new(
            NodeCount: 15,
            DrawBodyCurve: false,
            NodeScale: 0.5f,
            HandleScale: 1f,
            SwingAngle: 3.6f,
            TipLagAngle: 1.65f,
            MaxAttackDistance: 240f,
            AlternateReverse: true,
            NodesGlow: true);

        protected override ChainSwordSettings Settings => ChainSwordStyle;
        protected override float NodeGlowStrength => 0.65f;
        protected override Color NodeGlowColor => ThemeColor;

        public override string Texture =>
            "everflow/Content/Projectiles/ThousandBladesProjectile";

        protected override void ConfigureChainSwordProjectile()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ArmorPenetration = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            int count = Main.rand.Next(1, 4);
            shardTriggerTimes = new float[count];
            shardTriggered = new bool[count];

            // ChainSword使用 easeOutQuad 匀减速旋转。先在“角度进度”中
            // 围绕中心对称安排碎刃，再反算成时间，避免线性随机时间让
            // 散射整体偏向角速度更慢的挥舞后段。中心相较旧版略微提前。
            const float centerAngularProgress = 0.63f;
            // 对应总挥舞角度约±4°~±11°，给随机节点方向留出余量，
            // 使最终碎刃大体集中在鼠标方向±15°内。
            float angularSpread = Main.rand.NextFloat(0.02f, 0.055f);

            if (count == 1)
            {
                shardTriggerTimes[0] = InverseEaseOutQuad(centerAngularProgress);
            }
            else if (count == 2)
            {
                shardTriggerTimes[0] = InverseEaseOutQuad(centerAngularProgress - angularSpread);
                shardTriggerTimes[1] = InverseEaseOutQuad(centerAngularProgress + angularSpread);
            }
            else
            {
                shardTriggerTimes[0] = InverseEaseOutQuad(centerAngularProgress - angularSpread);
                shardTriggerTimes[1] = InverseEaseOutQuad(centerAngularProgress);
                shardTriggerTimes[2] = InverseEaseOutQuad(centerAngularProgress + angularSpread);
            }
            Array.Sort(shardTriggerTimes);
        }

        private static float InverseEaseOutQuad(float angularProgress) =>
            1f - MathF.Sqrt(1f - MathHelper.Clamp(angularProgress, 0f, 1f));

        protected override Rectangle GetHandleFrame(Texture2D texture) =>
            new(0, 0, FrameWidth, FrameHeight);

        protected override Rectangle GetNodeFrame(Texture2D texture, int nodeIndex)
        {
            int frameIndex = GetStableBodyFrame(nodeIndex);
            return new Rectangle(
                frameIndex % 2 * FrameWidth,
                (frameIndex / 2 + 1) * FrameHeight,
                FrameWidth,
                FrameHeight);
        }

        protected override void UpdateChainSword(IReadOnlyList<Vector2> controlPoints)
        {
            // 节点和剑柄使用主题色中等照明。
            for (int i = 0; i < controlPoints.Count - 1; i++)
                Lighting.AddLight(controlPoints[i], ThemeColor.ToVector3() * 0.55f);

            Player owner = Main.player[Projectile.owner];
            float duration = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (duration <= 0f || controlPoints.Count < 3)
                return;

            float progress = MathHelper.Clamp(Projectile.ai[0] / duration, 0f, 1f);
            for (int i = 0; i < shardTriggerTimes.Length; i++)
            {
                if (shardTriggered[i] || progress < shardTriggerTimes[i])
                    continue;

                shardTriggered[i] = true;
                if (Projectile.owner != Main.myPlayer)
                    continue;

                Vector2 rotationCenter = Main.GetPlayerArmPosition(Projectile);
                int nodeIndex = ChooseLaunchNode(controlPoints, rotationCenter);
                Vector2 spawnPosition = controlPoints[nodeIndex];
                Vector2 velocity = (spawnPosition - rotationCenter)
                    .SafeNormalize(Vector2.UnitX * owner.direction) * 30f;
                int frameIndex = GetStableBodyFrame(nodeIndex);
                int damage = (int)owner.GetTotalDamage(DamageClass.Melee).ApplyTo(50f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<ThousandBladesFlyingNodeProjectile>(),
                    damage,
                    Projectile.knockBack,
                    owner.whoAmI,
                    frameIndex);
            }
        }

        private int ChooseLaunchNode(IReadOnlyList<Vector2> controlPoints, Vector2 rotationCenter)
        {
            float aimAngle = Projectile.velocity.ToRotation();
            float limit = MathHelper.ToRadians(15f);
            int selected = -1;
            int eligibleCount = 0;
            int closest = 1;
            float closestDifference = float.MaxValue;

            for (int i = 1; i < controlPoints.Count - 1; i++)
            {
                float nodeAngle = (controlPoints[i] - rotationCenter).ToRotation();
                float difference = MathF.Abs(MathHelper.WrapAngle(nodeAngle - aimAngle));
                if (difference < closestDifference)
                {
                    closestDifference = difference;
                    closest = i;
                }

                if (difference > limit)
                    continue;

                eligibleCount++;
                // 蓄水池抽样：在所有符合±15°的节点中保持等概率随机。
                if (Main.rand.Next(eligibleCount) == 0)
                    selected = i;
            }

            return selected >= 0 ? selected : closest;
        }

        private int GetStableBodyFrame(int nodeIndex)
        {
            unchecked
            {
                int hash = Projectile.identity * 397;
                hash ^= nodeIndex * 7919;
                hash ^= Projectile.owner * 104729;
                hash ^= hash >> 16;
                return (hash & int.MaxValue) % BodyFrameCount;
            }
        }
    }
}
