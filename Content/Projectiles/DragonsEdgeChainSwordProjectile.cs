using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;
using everflow.Common.Players;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    /// <summary>龙意剑右键的链剑挥舞版本；旧鞭打版保留在 Legacy 类中。</summary>
    public class DragonsEdgeWhipProjectile : ChainSwordProjectile
    {
        private bool countedThisSwing;

        protected override ChainSwordSettings Settings
        {
            get
            {
                float rangePixels = DragonsEdge.GetStageStats().RightShootSpeed * 48f;
                return new ChainSwordSettings(
                    NodeCount: 20,
                    DrawBodyCurve: true,
                    NodeScale: 1f,
                    HandleScale: 1f,
                    SwingAngle: 3.6f,
                    TipLagAngle: 1.65f,
                    MaxAttackDistance: rangePixels,
                    AlternateReverse: false,
                    BodyCurveGlows: false,
                    NodesGlow: false);
            }
        }

        protected override Color BodyCurveColor => new(255, 155, 210);

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile";

        protected override void ConfigureChainSwordProjectile()
        {
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            int rightBaseDamage = DragonsEdge.GetStageStats().RightDamage;
            Projectile.originalDamage = rightBaseDamage;
            Projectile.damage = (int)owner
                .GetTotalDamage(DamageClass.Summon)
                .ApplyTo(rightBaseDamage);
        }

        protected override Rectangle GetHandleFrame(Texture2D texture) =>
            new(0, 0, texture.Width, 24);

        protected override Rectangle GetNodeFrame(Texture2D texture, int nodeIndex)
        {
            if (nodeIndex >= Settings.NodeCount - 1)
                return new Rectangle(0, 118, texture.Width, 20);
            if (nodeIndex > 10)
                return new Rectangle(0, 90, texture.Width, 20);
            if (nodeIndex > 5)
                return new Rectangle(0, 62, texture.Width, 20);
            return new Rectangle(0, 34, texture.Width, 20);
        }

        protected override void UpdateChainSword(IReadOnlyList<Vector2> controlPoints)
        {
            if (controlPoints.Count == 0)
                return;

            for (int i = 0; i < controlPoints.Count; i += 4)
                Lighting.AddLight(controlPoints[i], new Vector3(0.28f, 0.07f, 0.20f));

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 dustPosition = controlPoints[Main.rand.Next(controlPoints.Count)];
                Dust dust = Dust.NewDustPerfect(
                    dustPosition,
                    DustID.Enchanted_Pink,
                    Projectile.velocity * 0.03f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    80,
                    Color.LightPink,
                    Main.rand.NextFloat(0.7f, 1f));
                dust.noGravity = true;
                dust.fadeIn = 1.05f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            owner.MinionAttackTargetNPC = target.whoAmI;

            // 保留原有右键命中的粉色锋锐斩切特效。
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SpiralHellNightsEdgeHit>(),
                    0,
                    0f,
                    Projectile.owner,
                    Projectile.rotation,
                    Projectile.ai[0],
                    1f);
            }

            // 一次完整挥舞最多计数一次，三龙阈值、伤害和生成方式保持原样。
            if (!countedThisSwing)
            {
                countedThisSwing = true;
                DragonsEdgePlayer edgePlayer = owner.GetModPlayer<DragonsEdgePlayer>();
                int stage = DragonsEdge.GetProgressionStage();

                if (stage >= 4) edgePlayer.RightWhipHitCount++;
                else edgePlayer.RightWhipHitCount = 0;
                if (stage >= 7) edgePlayer.StardustWhipHitCount++;
                else edgePlayer.StardustWhipHitCount = 0;
                if (stage >= 8) edgePlayer.PhantasmWhipHitCount++;
                else edgePlayer.PhantasmWhipHitCount = 0;

                TrySpawnWyvern(owner, target, edgePlayer);
                TrySpawnStardust(owner, target, edgePlayer);
                TrySpawnPhantasm(owner, target, edgePlayer);
            }

            Projectile.damage = (int)(Projectile.damage * 0.8f);
        }

        private void TrySpawnWyvern(Player owner, NPC target, DragonsEdgePlayer edgePlayer)
        {
            if (edgePlayer.RightWhipHitCount < 7)
                return;
            edgePlayer.RightWhipHitCount = 0;
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 spawn = new(owner.Center.X, Main.screenPosition.Y - 120f);
            Vector2 direction = (target.Center - spawn).SafeNormalize(Vector2.UnitY);
            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(50f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * 27f,
                ModContent.ProjectileType<DragonsEdgeWyvernProjectile>(), damage, 3f,
                owner.whoAmI, target.whoAmI);
        }

        private void TrySpawnStardust(Player owner, NPC target, DragonsEdgePlayer edgePlayer)
        {
            if (edgePlayer.StardustWhipHitCount < 5)
                return;
            edgePlayer.StardustWhipHitCount = 0;
            if (Projectile.owner != Main.myPlayer)
                return;

            bool left = Main.rand.NextBool();
            Vector2 spawn = new(
                left ? Main.screenPosition.X - 120f : Main.screenPosition.X + Main.screenWidth + 120f,
                Main.screenPosition.Y - 120f);
            Vector2 direction = (target.Center - spawn).SafeNormalize(Vector2.UnitY);
            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(100f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * 35.1f,
                ModContent.ProjectileType<DragonsEdgeStardustProjectile>(), damage, 3f,
                owner.whoAmI, target.whoAmI);
        }

        private void TrySpawnPhantasm(Player owner, NPC target, DragonsEdgePlayer edgePlayer)
        {
            if (edgePlayer.PhantasmWhipHitCount < 3)
                return;
            edgePlayer.PhantasmWhipHitCount = 0;
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 screenCenter = Main.screenPosition +
                new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            float radius = new Vector2(Main.screenWidth, Main.screenHeight).Length() * 0.5f + 220f;
            Vector2 spawn = screenCenter + Main.rand.NextVector2Unit() * radius;
            Vector2 direction = (target.Center - spawn).SafeNormalize(Vector2.UnitY);
            int damage = (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(200f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * 54f,
                ModContent.ProjectileType<DragonsEdgePhantasmProjectile>(), damage, 3f,
                owner.whoAmI, target.whoAmI);
        }
    }
}
