using everflow.Common.Items;
using everflow.Common.Projectiles;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class PhasebladeForceShieldProjectile : ModProjectile
    {
        private const float ShieldDistance = 43f;
        private const float BlockHalfAngle = MathHelper.Pi / 3f;
        private const float ReflectHalfAngle = MathHelper.PiOver4;
        private const int ManaPerFrame = 2;

        // 仅收录离散、可被安全改为友方的一次性攻击。持续光束、死亡射线、
        // 喷火、范围判定体、生成器和Boss机制弹幕均有意排除。
        private static readonly HashSet<int> ReflectableVanillaProjectiles = new()
        {
            // 困难模式前常规敌怪、陷阱与Boss。
            ProjectileID.HarpyFeather,
            ProjectileID.DemonSickle,
            ProjectileID.Stinger,
            ProjectileID.WoodenArrowHostile,
            ProjectileID.FlamingArrow,
            ProjectileID.EyeLaser,
            ProjectileID.PinkLaser,
            ProjectileID.CursedFlameHostile,
            ProjectileID.PoisonDart,
            ProjectileID.SnowBallHostile,
            ProjectileID.UnholyTridentHostile,
            ProjectileID.FrostBlastHostile,
            ProjectileID.IcewaterSpit,
            ProjectileID.Skull,
            ProjectileID.BookOfSkullsSkull,
            ProjectileID.DeerclopsIceSpike,
            ProjectileID.DeerclopsRangedProjectile,

            // 困难模式常规敌怪与机械Boss。
            ProjectileID.DeathLaser,
            ProjectileID.BulletSnowman,
            ProjectileID.GoldenShowerHostile,
            ProjectileID.ShadowBeamHostile,
            ProjectileID.InfernoHostileBolt,
            ProjectileID.LostSoulHostile,
            ProjectileID.PaladinsHammerHostile,
            ProjectileID.SniperBullet,
            ProjectileID.RocketSkeleton,
            ProjectileID.BombSkeletronPrime,
            ProjectileID.RuneBlast,
            ProjectileID.RainNimbus,

            // 世纪之花至石巨人。
            ProjectileID.SeedPlantera,
            ProjectileID.PoisonSeedPlantera,
            ProjectileID.ThornBall,
            ProjectileID.Fireball,
            ProjectileID.EyeBeam,

            // 地牢、日食、南瓜月与霜月。
            ProjectileID.FlamingScythe,
            ProjectileID.GreekFire1,
            ProjectileID.GreekFire2,
            ProjectileID.GreekFire3,
            ProjectileID.PineNeedleHostile,
            ProjectileID.OrnamentHostile,
            ProjectileID.OrnamentHostileShrapnel,
            ProjectileID.FrostWave,
            ProjectileID.FrostShard,
            ProjectileID.Missile,
            ProjectileID.Present,
            ProjectileID.Spike,

            // 海盗、火星暴乱与其他事件敌怪。
            ProjectileID.CannonballHostile,
            ProjectileID.MartianTurretBolt,
            ProjectileID.BrainScramblerBolt,
            ProjectileID.GigaZapperSpear,
            ProjectileID.RayGunnerLaser,
            ProjectileID.SaucerMissile,
            ProjectileID.SaucerLaser,
            ProjectileID.SaucerScrap,
            ProjectileID.MartianWalkerLaser,
            ProjectileID.ScutlixLaser,
            ProjectileID.WebSpit,
            ProjectileID.JavelinHostile,
            ProjectileID.SalamanderSpit,
            ProjectileID.SpikedSlimeSpike,
            ProjectileID.RockGolemRock,

            // 远古军团。
            ProjectileID.DD2JavelinHostile,
            ProjectileID.DD2JavelinHostileT3,
            ProjectileID.DD2DrakinShot,
            ProjectileID.DD2DarkMageBolt,
            ProjectileID.DD2OgreSpit,
            ProjectileID.DD2LightningBugZap,
            ProjectileID.DD2BetsyFireball,
            ProjectileID.DD2BetsyArrow,

            // 天界事件、教徒与月亮领主；明确不含死亡射线和月蛭。
            ProjectileID.NebulaBolt,
            ProjectileID.NebulaEye,
            ProjectileID.NebulaSphere,
            ProjectileID.NebulaLaser,
            ProjectileID.VortexLaser,
            ProjectileID.VortexAcid,
            ProjectileID.BlackBolt,
            ProjectileID.CultistBossFireBall,
            ProjectileID.CultistBossFireBallClone,
            ProjectileID.PhantasmalEye,
            ProjectileID.PhantasmalSphere,
            ProjectileID.PhantasmalBolt,

            // 血月、女皇与史莱姆皇后。
            ProjectileID.BloodShot,
            ProjectileID.BloodNautilusShot,
            ProjectileID.BloodArrow,
            ProjectileID.DandelionSeed,
            ProjectileID.HallowBossSplitShotCore,
            ProjectileID.FairyQueenMagicItemShot,
            ProjectileID.FairyQueenRangedItemShot,
            ProjectileID.QueenSlimeMinionBlueSpike,
            ProjectileID.QueenSlimeMinionPinkBall,
            ProjectileID.QueenSlimeGelAttack,

            // 两类机关飞镖在当前版本使用不同的原版ID。
            ProjectileID.PoisonDartTrap,
            ProjectileID.VenomDartTrap
        };

        public override string Texture => "everflow/Content/Projectiles/Phaseblades_Block";

        private Color ShieldColor => GetShieldColor((int)Projectile.ai[0]);
        private int ShieldTier => (int)Projectile.ai[1];
        private bool IsPhasesaberShield => ShieldTier == 1;
        private bool IsSolarBladeShield => ShieldTier == 2;
        private Texture2D ShieldTexture => IsSolarBladeShield
            ? ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/SolarBlades_Block").Value
            : TextureAssets.Projectile[Type].Value;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(
                    SoundID.Item15 with
                    {
                        Volume = 0.55f,
                        Pitch = 0.25f,
                        PitchVariance = 0.04f
                    },
                    Projectile.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(
                    SoundID.Item15 with
                    {
                        Volume = 0.38f,
                        Pitch = -0.35f,
                        PitchVariance = 0.03f
                    },
                    Projectile.Center);
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!CanMaintainShield(player))
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 newDirection = (Main.MouseWorld - player.MountedCenter)
                    .SafeNormalize(new Vector2(player.direction, 0f));
                if (Vector2.Dot(newDirection, Projectile.velocity.SafeNormalize(Vector2.UnitX)) < 0.9995f)
                    Projectile.netUpdate = true;
                Projectile.velocity = newDirection;

                if (!player.CheckMana(ManaPerFrame, true, false))
                {
                    Projectile.Kill();
                    return;
                }
                player.manaRegenDelay = System.Math.Max(player.manaRegenDelay, 30);
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f));
            Projectile.scale = ShieldTier >= 1 ? 1.25f : 1f;
            Projectile.Center = player.MountedCenter + direction * ShieldDistance;
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.timeLeft = 2;

            Color color = ShieldColor;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 1.15f);

            if (Projectile.owner == Main.myPlayer)
            {
                BlockHostileProjectiles(player, direction);
                BlockProjectileNPCs(player, direction);
            }
        }

        private bool CanMaintainShield(Player player)
        {
            if (!player.active || player.dead || !player.controlUseTile ||
                player.HasBuff(BuffID.ManaSickness))
                return false;

            return PhasebladeForceBlockGlobalItem.TryGetShieldColorIndex(
                player.HeldItem.type,
                out int colorIndex) &&
                colorIndex == (int)Projectile.ai[0] &&
                PhasebladeForceBlockGlobalItem.GetShieldTier(player.HeldItem.type) ==
                    ShieldTier;
        }

        private void BlockHostileProjectiles(Player player, Vector2 shieldDirection)
        {
            Texture2D texture = ShieldTexture;
            float shieldHalfWidth = texture.Width * Projectile.scale * 0.5f;
            Vector2 tangent = shieldDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 lineStart = Projectile.Center - tangent * shieldHalfWidth;
            Vector2 lineEnd = Projectile.Center + tangent * shieldHalfWidth;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hostile = Main.projectile[i];
                if (!hostile.active || !hostile.hostile || hostile.damage <= 0 ||
                    hostile.whoAmI == Projectile.whoAmI)
                {
                    continue;
                }

                PhasebladePartialBlockGlobalProjectile partialBlock =
                    hostile.GetGlobalProjectile<PhasebladePartialBlockGlobalProjectile>();
                if (partialBlock.IsPendingFor(player))
                    continue;

                Vector2 fromPlayer = hostile.Center - player.MountedCenter;
                if (fromPlayer.LengthSquared() < 0.001f)
                    continue;

                Vector2 projectileDirectionFromPlayer =
                    fromPlayer.SafeNormalize(Vector2.Zero);
                float facingDot = Vector2.Dot(
                    projectileDirectionFromPlayer,
                    shieldDirection);
                if (facingDot < (float)System.Math.Cos(BlockHalfAngle))
                {
                    continue;
                }

                float collisionPoint = 0f;
                if (!Collision.CheckAABBvLineCollision(
                    hostile.position,
                    hostile.Size,
                    lineStart,
                    lineEnd,
                    14f,
                    ref collisionPoint))
                {
                    continue;
                }

                int manaCost = GetBlockManaCost(
                    hostile.damage,
                    IsPhasesaberShield,
                    IsSolarBladeShield);
                if (player.statMana >= manaCost)
                {
                    PayManaDirectly(player, manaCost);
                    player.manaRegenDelay = System.Math.Max(player.manaRegenDelay, 60);
                    SpawnBlockEffects(hostile.Center);

                    if (IsSolarBladeShield &&
                        facingDot >= (float)System.Math.Cos(ReflectHalfAngle) &&
                        CanReflectProjectile(hostile))
                    {
                        PlayReflectSound(hostile.Center);
                        ReflectProjectile(hostile, player, shieldDirection);
                    }
                    else
                    {
                        PlayBlockSound(hostile.Center);
                        hostile.Kill();
                    }
                    continue;
                }

                // 尽可能支付现有魔力。射弹继续飞行，并在正常命中该玩家时
                // 按“未支付魔力 / 应付魔力”保留伤害；命中效果不做削减。
                int paidMana = player.statMana;
                if (paidMana > 0)
                    PayManaDirectly(player, paidMana);
                float unpaidRatio = (manaCost - paidMana) / (float)manaCost;
                partialBlock.SetPartialBlock(player, unpaidRatio);
                player.manaRegenDelay = System.Math.Max(player.manaRegenDelay, 60);
                SpawnBlockEffects(hostile.Center);
            }
        }

        private static bool CanReflectProjectile(Projectile projectile)
        {
            if (projectile.velocity.LengthSquared() < 0.001f)
                return false;

            if (ReflectableVanillaProjectiles.Contains(projectile.type))
                return true;

            // 本模组的敌对离散射弹。爆炸判定、持续火焰和Boss常驻武器
            // 不在此列。
            return projectile.type == ModContent.ProjectileType<AncientTrooperArrow>() ||
                projectile.type == ModContent.ProjectileType<JadeArrow>() ||
                projectile.type == ModContent.ProjectileType<JadeRocket>();
        }

        private static void ReflectProjectile(
            Projectile projectile,
            Player player,
            Vector2 shieldDirection)
        {
            Vector2 normal = shieldDirection.SafeNormalize(Vector2.UnitX);
            projectile.velocity -=
                2f * Vector2.Dot(projectile.velocity, normal) * normal;

            // hostile射弹的damage保存的是难度换算前的内部基础值。原版命中
            // 玩家时会固定乘2，再乘世界的EnemyDamageMultiplier；转为友方
            // 后不会自动经过该流程，因此在反射瞬间补回一次实际威力。
            double reflectedDamage = projectile.damage * 2.0 *
                Main.GameModeInfo.EnemyDamageMultiplier;
            projectile.damage = System.Math.Max(
                1,
                (int)System.Math.Min(
                    System.Math.Round(reflectedDamage),
                    int.MaxValue));
            projectile.originalDamage = projectile.damage;

            projectile.hostile = false;
            projectile.friendly = true;
            projectile.owner = player.whoAmI;
            projectile.npcProj = false;
            projectile.trap = false;
            projectile.aiStyle = 0;
            projectile.ai[0] = 0f;
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.GetGlobalProjectile<
                ForceReflectedGlobalProjectile>().IsReflected = true;
            projectile.netUpdate = true;
        }

        private void BlockProjectileNPCs(
            Player player,
            Vector2 shieldDirection)
        {
            Texture2D texture = ShieldTexture;
            float shieldHalfWidth = texture.Width * Projectile.scale * 0.5f;
            Vector2 tangent = shieldDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 lineStart = Projectile.Center - tangent * shieldHalfWidth;
            Vector2 lineEnd = Projectile.Center + tangent * shieldHalfWidth;
            float minimumFacingDot =
                (float)System.Math.Cos(BlockHalfAngle);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.damage <= 0 ||
                    npc.dontTakeDamage ||
                    !NPCID.Sets.ProjectileNPC[npc.type])
                {
                    continue;
                }

                Vector2 fromPlayer = npc.Center - player.MountedCenter;
                if (fromPlayer.LengthSquared() < 0.001f ||
                    Vector2.Dot(
                        fromPlayer.SafeNormalize(Vector2.Zero),
                        shieldDirection) < minimumFacingDot)
                {
                    continue;
                }

                float collisionPoint = 0f;
                if (!Collision.CheckAABBvLineCollision(
                    npc.position,
                    npc.Size,
                    lineStart,
                    lineEnd,
                    14f,
                    ref collisionPoint))
                {
                    continue;
                }

                int manaCost = GetBlockManaCost(
                    npc.damage,
                    IsPhasesaberShield,
                    IsSolarBladeShield);
                if (player.statMana < manaCost)
                {
                    SpawnBlockEffects(npc.Center);
                    continue;
                }

                PayManaDirectly(player, manaCost);
                player.manaRegenDelay =
                    System.Math.Max(player.manaRegenDelay, 60);
                SpawnBlockEffects(npc.Center);
                PlayBlockSound(npc.Center);
                npc.StrikeInstantKill();
            }
        }

        private static void PlayBlockSound(Vector2 position)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            SoundEngine.PlaySound(
                SoundID.Tink with
                {
                    Volume = 0.7f,
                    Pitch = -0.12f,
                    PitchVariance = 0.08f
                },
                position);
        }

        private static void PlayReflectSound(Vector2 position)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // 原版专用于宝箱怪、日耀碑牌怪等成功反射射弹的声音。
            SoundEngine.PlaySound(
                SoundID.Item150 with
                {
                    Volume = 0.85f,
                    PitchVariance = 0.05f
                },
                position);
        }

        private static void PayManaDirectly(Player player, int amount)
        {
            // CheckMana允许快速魔力药水介入，而且同一帧连续处理多枚射弹时
            // 可能反复从物品使用流程的状态结算。直接修改当前魔力，确保每枚
            // 射弹都只使用上一枚结算后的余额，无法复用同一份魔力。
            int paid = System.Math.Min(System.Math.Max(amount, 0), player.statMana);
            if (paid <= 0)
                return;

            player.statMana -= paid;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerMana, number: player.whoAmI);
        }

        private static int GetBlockManaCost(
            int projectileDamage,
            bool phasesaberShield,
            bool solarBladeShield)
        {
            if (solarBladeShield)
                return GetSolarBladeBlockManaCost(projectileDamage);
            if (phasesaberShield)
                return GetPhasesaberBlockManaCost(projectileDamage);

            double rawCost;
            if (projectileDamage <= 100)
            {
                rawCost = projectileDamage * 0.25;
            }
            else
            {
                // 指数曲线严格经过(伤害100, 魔力25)与(伤害250, 魔力200)。
                rawCost = 25.0 * System.Math.Pow(
                    8.0,
                    (projectileDamage - 100.0) / 150.0);
            }

            return System.Math.Max(
                1,
                (int)System.Math.Ceiling(
                    System.Math.Min(rawCost, int.MaxValue) - 1e-9));
        }

        private static int GetPhasesaberBlockManaCost(int projectileDamage)
        {
            double rawCost;
            if (projectileDamage <= 150)
            {
                // 晶光刃在0至150伤害的整个区间均只消耗伤害值20%的魔力。
                rawCost = projectileDamage * 0.20;
            }
            else
            {
                // 指数曲线严格经过(伤害150, 魔力30)与(伤害500, 魔力250)。
                rawCost = 30.0 * System.Math.Pow(
                    250.0 / 30.0,
                    (projectileDamage - 150.0) / 350.0);
            }

            return System.Math.Max(
                1,
                (int)System.Math.Ceiling(
                    System.Math.Min(rawCost, int.MaxValue) - 1e-9));
        }

        private static int GetSolarBladeBlockManaCost(int projectileDamage)
        {
            double rawCost;
            if (projectileDamage <= 200)
            {
                rawCost = projectileDamage * 0.20;
            }
            else
            {
                // 指数曲线严格经过(伤害200, 魔力40)与(伤害500, 魔力150)。
                rawCost = 40.0 * System.Math.Pow(
                    150.0 / 40.0,
                    (projectileDamage - 200.0) / 300.0);
            }

            return System.Math.Max(
                1,
                (int)System.Math.Ceiling(
                    System.Math.Min(rawCost, int.MaxValue) - 1e-9));
        }

        private void SpawnBlockEffects(Vector2 position)
        {
            Color color = ShieldColor;
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4.5f, 4.5f);
                Dust dust = Dust.NewDustPerfect(position, DustID.RainbowMk2, velocity, 0, color, 1.15f);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ShieldTexture;
            Color color = ShieldColor;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            // 这张素材保存的是“纯白RGB + 独立Alpha渐变”。Terraria默认的
            // 预乘Alpha批次会让低Alpha像素仍以全白亮度参与混合，因此盾面
            // 看起来近似实色。单独切换到NonPremultiplied后，源Alpha会正确
            // 控制每个像素的覆盖率，同时仍可用统一颜色动态染色。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                color,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        private static Color GetShieldColor(int colorIndex) => colorIndex switch
        {
            0 => new Color(40, 130, 255),
            1 => new Color(255, 45, 35),
            2 => new Color(45, 255, 95),
            3 => new Color(190, 70, 255),
            4 => new Color(235, 245, 255),
            5 => new Color(255, 230, 45),
            6 => new Color(255, 130, 25),
            _ => Color.White
        };
    }
}
