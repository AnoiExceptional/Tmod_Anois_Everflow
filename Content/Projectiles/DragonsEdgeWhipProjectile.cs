using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Players;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    // 原版鞭打动作完整备份。需要回退时，将 DragonsEdge 物品的 shoot
    // 类型改为此类即可，不必还原文件历史。
    public class DragonsEdgeLegacyWhipProjectile : ModProjectile
    {
        private bool countedThisSwing;

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.SwordWhip);
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 避开原版 AI_165 在挥舞中点强制播放的 Item153 鞭梢爆响。
            Projectile.ai[0] = 0.001f;

            Player owner = Main.player[Projectile.owner];
            int rightBaseDamage = DragonsEdge.GetStageStats().RightDamage;

            // 物品来源会把 originalDamage 替换为动态物品当前的 Item.damage。
            // 龙意剑可能已恢复成左键面板，因此在鞭子生成后锁定右键基础伤害，
            // 并只应用召唤伤害加成。
            Projectile.originalDamage = rightBaseDamage;
            Projectile.damage = (int)owner
                .GetTotalDamage(DamageClass.Summon)
                .ApplyTo(rightBaseDamage);

            // 原版控制点公式已经会乘以弹幕速度；这里仅设置武器自身的
            // 固定射程校准值，避免将阶段弹速重复作为倍率再乘一次。
            // 玩家身上的原版 whipRangeMultiplier 仍会在控制点公式中生效。
            Projectile.WhipSettings.RangeMultiplier = 0.9f;
        }

        public override void AI()
        {
            List<Vector2> controlPoints = new();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);
            if (controlPoints.Count == 0)
                return;

            // 沿整个鞭身提供与左键相同的较弱粉色光照。
            for (int i = 0; i < controlPoints.Count; i += 4)
            {
                Lighting.AddLight(
                    controlPoints[i],
                    new Vector3(0.28f, 0.07f, 0.20f));
            }

            // 鞭子具有额外更新，四分之一概率约等于每游戏帧二分之一概率。
            if (Main.rand.NextBool(4))
            {
                Vector2 dustPosition = controlPoints[Main.rand.Next(controlPoints.Count)];
                Dust dust = Dust.NewDustPerfect(
                    dustPosition,
                    DustID.Enchanted_Pink,
                    Projectile.velocity * 0.03f +
                        Main.rand.NextVector2Circular(0.8f, 0.8f),
                    80,
                    Color.LightPink,
                    Main.rand.NextFloat(0.7f, 1.0f));

                dust.noGravity = true;
                dust.fadeIn = 1.05f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            owner.MinionAttackTargetNPC = target.whoAmI;

            // Reuse the exact Biancheng form hit-slash animation, selecting
            // the Dragon's Edge whip-line pink through ai[2]. This projectile
            // is visual only and therefore does not add another damage hit.
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

            // 每一枚鞭子弹幕代表一次挥鞭；即使同时扫中多个目标，
            // 也只允许第一次有效命中增加一次飞龙召唤计数。
            if (!countedThisSwing)
            {
                countedThisSwing = true;
                DragonsEdgePlayer edgePlayer = owner.GetModPlayer<DragonsEdgePlayer>();
                int progressionStage = DragonsEdge.GetProgressionStage();

                if (progressionStage >= 4)
                    edgePlayer.RightWhipHitCount++;
                else
                    edgePlayer.RightWhipHitCount = 0;

                if (progressionStage >= 7)
                    edgePlayer.StardustWhipHitCount++;
                else
                    edgePlayer.StardustWhipHitCount = 0;

                if (progressionStage >= 8)
                    edgePlayer.PhantasmWhipHitCount++;
                else
                    edgePlayer.PhantasmWhipHitCount = 0;

                if (edgePlayer.RightWhipHitCount >= 7)
                {
                    edgePlayer.RightWhipHitCount = 0;

                    if (Projectile.owner == Main.myPlayer)
                    {
                        Vector2 spawnPosition = new(
                            owner.Center.X,
                            Main.screenPosition.Y - 120f);
                        Vector2 chargeDirection = (target.Center - spawnPosition)
                            .SafeNormalize(Vector2.UnitY);
                        int summonDamage = (int)owner
                            .GetTotalDamage(DamageClass.Summon)
                            .ApplyTo(50f);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            spawnPosition,
                            chargeDirection * 27f,
                            ModContent.ProjectileType<DragonsEdgeWyvernProjectile>(),
                            summonDamage,
                            3f,
                            owner.whoAmI,
                            target.whoAmI);
                    }
                }

                if (edgePlayer.StardustWhipHitCount >= 5)
                {
                    edgePlayer.StardustWhipHitCount = 0;

                    if (Projectile.owner == Main.myPlayer)
                    {
                        bool spawnFromLeft = Main.rand.NextBool();
                        Vector2 spawnPosition = new(
                            spawnFromLeft
                                ? Main.screenPosition.X - 120f
                                : Main.screenPosition.X + Main.screenWidth + 120f,
                            Main.screenPosition.Y - 120f);
                        Vector2 chargeDirection = (target.Center - spawnPosition)
                            .SafeNormalize(Vector2.UnitY);
                        int summonDamage = (int)owner
                            .GetTotalDamage(DamageClass.Summon)
                            .ApplyTo(100f);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            spawnPosition,
                            chargeDirection * 35.1f,
                            ModContent.ProjectileType<DragonsEdgeStardustProjectile>(),
                            summonDamage,
                            3f,
                            owner.whoAmI,
                        target.whoAmI);
                    }
                }

                if (edgePlayer.PhantasmWhipHitCount >= 3)
                {
                    edgePlayer.PhantasmWhipHitCount = 0;

                    if (Projectile.owner == Main.myPlayer)
                    {
                        Vector2 screenCenter = Main.screenPosition +
                            new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                        float spawnRadius = new Vector2(
                            Main.screenWidth,
                            Main.screenHeight).Length() * 0.5f + 220f;
                        Vector2 randomDirection = Main.rand.NextVector2Unit();
                        Vector2 spawnPosition = screenCenter +
                            randomDirection * spawnRadius;
                        Vector2 chargeDirection = (target.Center - spawnPosition)
                            .SafeNormalize(Vector2.UnitY);
                        int summonDamage = (int)owner
                            .GetTotalDamage(DamageClass.Summon)
                            .ApplyTo(200f);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            spawnPosition,
                            chargeDirection * 54f,
                            ModContent.ProjectileType<DragonsEdgePhantasmProjectile>(),
                            summonDamage,
                            3f,
                            owner.whoAmI,
                            target.whoAmI);
                    }
                }
            }

            Projectile.damage = (int)(Projectile.damage * 0.8f);
        }

        private static void DrawConnectingLine(List<Vector2> controlPoints)
        {
            Texture2D lineTexture = TextureAssets.FishingLine.Value;
            Rectangle frame = lineTexture.Frame();
            Vector2 origin = new(frame.Width * 0.5f, 2f);
            Vector2 drawPosition = controlPoints[0];
            Color lineColor = new(255, 155, 210);

            // 尖端贴图绘制在倒数第二个控制点；连接线在此处结束，
            // 避免继续延伸到最后一个控制点并从尖端露出线头。
            for (int i = 0; i < controlPoints.Count - 2; i++)
            {
                Vector2 point = controlPoints[i];
                Vector2 difference = controlPoints[i + 1] - point;
                float rotation = difference.ToRotation() - MathHelper.PiOver2;
                Vector2 scale = new(1f, (difference.Length() + 2f) / frame.Height);
                Color color = Lighting.GetColor(
                    point.ToTileCoordinates(),
                    lineColor);

                Main.EntitySpriteDraw(
                    lineTexture,
                    drawPosition - Main.screenPosition,
                    frame,
                    color,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f);

                drawPosition += difference;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> controlPoints = new();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);
            DrawConnectingLine(controlPoints);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = Projectile.spriteDirection < 0
                ? SpriteEffects.None
                : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector2 difference = controlPoints[i + 1] - controlPoints[i];
                float rotation = difference.ToRotation() - MathHelper.PiOver2;
                Rectangle frame;

                if (i == controlPoints.Count - 2)
                    frame = new Rectangle(0, 118, texture.Width, 20);
                else if (i > 10)
                    frame = new Rectangle(0, 90, texture.Width, 20);
                else if (i > 5)
                    frame = new Rectangle(0, 62, texture.Width, 20);
                else if (i > 0)
                    frame = new Rectangle(0, 34, texture.Width, 20);
                else
                    frame = new Rectangle(0, 0, texture.Width, 24);

                Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);
                Color color = Lighting.GetColor(controlPoints[i].ToTileCoordinates());
                Main.EntitySpriteDraw(
                    texture,
                    controlPoints[i] - Main.screenPosition,
                    frame,
                    color,
                    rotation,
                    origin,
                    Projectile.scale,
                    effects);
            }

            return false;
        }
    }
}
