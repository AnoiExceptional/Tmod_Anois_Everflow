using System.Collections.Generic;
using everflow.Common.DamageClasses;
using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class ArcticPillarChargeProjectile : ModProjectile
    {
        private const int ChargeTime = 5 * 60;
        private const int ThrustTime = 15;
        private const float StartingGripRatio = 1f / 3f;
        private const float SpearVisualLength = 203.64676f;

        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_1";

        // 0：蓄力；1：蓄力完成、等待松开；2：突刺。
        private bool IsReady => Projectile.ai[1] == 1f;
        private bool IsThrusting => Projectile.ai[1] == 2f;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.timeLeft = ChargeTime + ThrustTime + 30;
            Projectile.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => IsThrusting ? null : false;

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            // 蓄满后可以一直保持待发，直到玩家主动松开右键。
            Projectile.timeLeft = 2;

            if (!IsThrusting)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    Vector2 aim = Main.MouseWorld - player.MountedCenter;
                    if (aim.LengthSquared() > 0.001f)
                    {
                        Projectile.velocity = aim.SafeNormalize(Vector2.UnitX);
                        Projectile.netUpdate = true;
                    }
                }

                if (!player.controlUseTile)
                {
                    if (!IsReady)
                    {
                        Projectile.Kill();
                        return;
                    }

                    Projectile.ai[1] = 2f;
                    Projectile.ai[0] = 0f;
                    Projectile.localAI[0] = 0f;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item71, player.Center);
                }
                else if (!IsReady)
                {
                    Projectile.ai[0]++;
                    if (Projectile.owner == Main.myPlayer &&
                        (int)Projectile.ai[0] % 3 == 0 &&
                        ConsumeOneOrbitingSnowflake(player))
                    {
                        // 满额14片时：90帧自然蓄力 + 14*15帧加速
                        // 正好完成300帧蓄力，即约1.5秒释放。
                        Projectile.ai[0] += 15f;
                        Projectile.netUpdate = true;
                    }

                    if (Projectile.ai[0] >= ChargeTime)
                    {
                        Projectile.ai[1] = 1f;
                        Projectile.ai[0] = ChargeTime;
                        Projectile.localAI[1] = 18f;
                        Projectile.netUpdate = true;
                        SoundEngine.PlaySound(SoundID.Item4, player.Center);
                    }
                }
            }
            else
            {
                Projectile.ai[0]++;
                if (Projectile.ai[0] >= ThrustTime)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;

            float extension = GetExtension();
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Projectile.Center = hand;
            Projectile.rotation = direction.ToRotation() +
                MathHelper.Pi * 0.75f;

            if (!IsThrusting)
            {
                float charge = MathHelper.Clamp(
                    Projectile.ai[0] / ChargeTime, 0f, 1f);
                SpawnChargeVortex(hand, direction, charge);

                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 48f)
                {
                    Projectile.localAI[0] = 0f;
                    SoundEngine.PlaySound(
                        SoundID.DD2_BetsyWindAttack with
                        {
                            Volume = 0.48f,
                            Pitch = -0.28f,
                            PitchVariance = 0.08f
                        },
                        hand);
                }

                if (!IsReady && (int)Projectile.ai[0] % 5 == 0)
                {
                    Vector2 dustPosition = hand + direction *
                        MathHelper.Lerp(50f, 105f, charge);
                    Dust dust = Dust.NewDustPerfect(
                        dustPosition + Main.rand.NextVector2Circular(28f, 28f),
                        DustID.IceTorch,
                        -direction * Main.rand.NextFloat(0.5f, 2f),
                        70,
                        default,
                        MathHelper.Lerp(0.8f, 1.5f, charge));
                    dust.noGravity = true;
                }

                if (Projectile.localAI[1] > 0f)
                    Projectile.localAI[1]--;
            }
            else if (Projectile.owner == Main.myPlayer &&
                Projectile.localAI[0] == 0f &&
                Projectile.ai[0] >= ThrustTime * 2f / 3f)
            {
                Projectile.localAI[0] = 1f;
                Vector2 spikeOrigin = player.MountedCenter;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spikeOrigin,
                    direction,
                    ModContent.ProjectileType<
                        ArcticPillarGiantIceSpikeProjectile>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
                // 与左键冰雪标枪碎裂时使用同一个冰裂音效。
                SoundEngine.PlaySound(SoundID.Item27, spikeOrigin);
            }

            Lighting.AddLight(hand, new Vector3(0.2f, 0.55f, 0.85f));
        }

        private static void SpawnChargeVortex(
            Vector2 hand,
            Vector2 direction,
            float charge)
        {
            Vector2 eye = GetSpearTip(hand, direction, 0f);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float time = (float)Main.GameUpdateCount *
                MathHelper.Lerp(0.13f, 0.24f, charge);
            int particleCount = charge > 0.7f ? 3 : 2;

            for (int i = 0; i < particleCount; i++)
            {
                float angle = time + MathHelper.TwoPi * i / particleCount +
                    Main.rand.NextFloat(-0.22f, 0.22f);
                float radius = Main.rand.NextFloat(55f, 125f) *
                    MathHelper.Lerp(0.75f, 1f, charge);
                Vector2 offset = direction *
                    (System.MathF.Cos(angle) * radius) + perpendicular *
                    (System.MathF.Sin(angle) * radius * 0.55f);
                Vector2 inward = -offset.SafeNormalize(Vector2.Zero);
                Vector2 tangent = new Vector2(-offset.Y, offset.X)
                    .SafeNormalize(Vector2.Zero);
                Vector2 velocity = inward *
                    MathHelper.Lerp(2.8f, 5.2f, charge) + tangent *
                    MathHelper.Lerp(1.6f, 3.2f, charge);

                Dust dust = Dust.NewDustPerfect(
                    eye + offset,
                    i == 0 ? DustID.IceTorch : DustID.Snow,
                    velocity,
                    80,
                    new Color(170, 225, 255),
                    Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = true;
                dust.fadeIn = 0.75f;
            }

            Lighting.AddLight(
                eye,
                new Vector3(0.15f, 0.38f, 0.62f) *
                MathHelper.Lerp(0.45f, 1f, charge));
        }

        private static bool ConsumeOneOrbitingSnowflake(Player player)
        {
            int visualType = ModContent.ProjectileType<
                ArcticPillarOrbitSnowflakeVisual>();
            Projectile oldest = null;
            float oldestAge = -1f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (!candidate.active || candidate.owner != player.whoAmI ||
                    candidate.type != visualType ||
                    candidate.localAI[0] <= oldestAge)
                {
                    continue;
                }

                oldest = candidate;
                oldestAge = candidate.localAI[0];
            }

            if (oldest is null)
                return false;

            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    oldest.Center,
                    DustID.IceTorch,
                    Main.rand.NextVector2Circular(2.5f, 2.5f),
                    80,
                    default,
                    1.1f);
                dust.noGravity = true;
            }

            oldest.Kill();
            return true;
        }

        private float GetExtension()
        {
            if (!IsThrusting)
                return 0f;

            float progress = Projectile.ai[0] / (ThrustTime - 1f);
            float extension = progress < 2f / 3f
                ? progress / (2f / 3f)
                : (1f - progress) / (1f / 3f);
            return MathHelper.SmoothStep(
                0f,
                1f,
                MathHelper.Clamp(extension, 0f, 1f));
        }

        private static Vector2 GetSpearTip(
            Vector2 hand,
            Vector2 direction,
            float extension)
        {
            float distance = SpearVisualLength * MathHelper.Lerp(
                StartingGripRatio,
                1f,
                extension);
            return hand + direction * distance;
        }

        public override bool? Colliding(
            Rectangle projHitbox,
            Rectangle targetHitbox)
        {
            if (!IsThrusting)
                return false;

            Player player = Main.player[Projectile.owner];
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                hand,
                GetSpearTip(hand, direction, GetExtension()),
                24f,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float extension = GetExtension();
            Vector2 origin = Vector2.Lerp(
                texture.Size() * StartingGripRatio,
                texture.Size(),
                extension);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None);

            DrawReadyFlare();
            return false;
        }

        private void DrawReadyFlare()
        {
            if (Projectile.localAI[1] <= 0f)
                return;

            Player player = Main.player[Projectile.owner];
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 tipPosition = GetSpearTip(hand, direction, 0f);
            Texture2D flare = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            float progress = 1f - Projectile.localAI[1] / 18f;
            float strength = System.MathF.Sin(progress * MathHelper.Pi);
            Color color = new Color(100, 190, 255, 0) * strength;
            Vector2 scale = new Vector2(1.8f, 1f) *
                MathHelper.Lerp(1.2f, 2.4f, strength);

            Main.EntitySpriteDraw(
                flare,
                tipPosition - Main.screenPosition,
                null,
                color,
                direction.ToRotation() + MathHelper.PiOver2,
                flare.Size() * 0.5f,
                scale,
                SpriteEffects.None);
        }
    }

    public sealed class ArcticPillarGiantIceSpikeProjectile : ModProjectile
    {
        private const int SegmentCount = 18;
        private const float DrawScale = 3f;
        // 贴图高32像素，放大3倍后使用96像素节距，首尾无缝衔接。
        private const float SegmentSpacing = 32f * DrawScale;

        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_5";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 42;
            Projectile.coldDamage = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
            Projectile.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            MonsterColdControlNPC.TryApplyChilled(target, 5 * 60);
            MonsterColdControlNPC.TryApplyFrozen(target, 3 * 60);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() +
                MathHelper.PiOver2;
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.25f, 0.65f, 1f));

            Projectile.localAI[0]++;
            if (Projectile.owner == Main.myPlayer &&
                Projectile.localAI[0] == 2f &&
                Projectile.ai[0] < SegmentCount - 1)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(
                    Vector2.UnitX);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * SegmentSpacing,
                    direction,
                    Type,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    Projectile.ai[0] + 1f);
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(
                    Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(38f, 38f),
                    DustID.IceTorch,
                    -direction * Main.rand.NextFloat(0.5f, 2f),
                    80,
                    default,
                    Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D middle = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/ArcticPillarProjectile_4").Value;
            Texture2D tip = TextureAssets.Projectile[Type].Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX);
            float rotation = direction.ToRotation() + MathHelper.PiOver2;
            Vector2 middleOrigin = middle.Size() * 0.5f;
            Vector2 tipOrigin = tip.Size() * 0.5f;
            Color color = Projectile.GetAlpha(Color.White);
            bool isTip = Projectile.ai[0] >= SegmentCount - 1;
            Texture2D texture = isTip ? tip : middle;
            Vector2 origin = isTip ? tipOrigin : middleOrigin;
            float appear = Utils.GetLerpValue(42f, 36f, Projectile.timeLeft, true);
            float disappear = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                color * (0.6f * appear * disappear),
                rotation,
                origin,
                DrawScale * MathHelper.Lerp(0.55f, 1f, appear),
                SpriteEffects.None);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.IceTorch,
                    0f,
                    0f,
                    80,
                    default,
                    1.6f);
                dust.noGravity = true;
                dust.velocity *= 1.8f;
            }
        }
    }

    public sealed class ArcticPillarHeldProjectile : ModProjectile
    {
        private const float StartingGripRatio = 1f / 3f;
        // 144x144贴图沿完整对角线的长度；枪尖、碰撞线和发射点共用此值。
        private const float SpearVisualLength = 203.64676f;

        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems ||
                player.CCed)
            {
                Projectile.Kill();
                return;
            }

            // 自动挥舞会在下一轮攻击开始时立即重置玩家的itemAnimation，
            // 因此枪身不能依赖玩家当前动画判断寿命。每枚枪身保存出生
            // 时的攻击总时长，并用自己的计时在这一轮结束时销毁。
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = System.Math.Max(1, player.itemAnimationMax);

            float attackDuration = Projectile.ai[1];
            float attackAge = Projectile.localAI[1]++;
            if (attackAge >= attackDuration)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation;

            float progress = attackDuration <= 1f
                ? 1f
                : attackAge / (attackDuration - 1f);
            float extension = MathHelper.Clamp(
                progress < 2f / 3f
                    ? progress / (2f / 3f)
                    : (1f - progress) / (1f / 3f),
                0f,
                1f);
            extension = MathHelper.SmoothStep(0f, 1f, extension);

            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            // 玩家手本身保持不动；绘制原点从枪身前1/3处移动到枪柄末端，
            // 让完整戳刺距离正好等于这两个握持点之间的距离。
            Projectile.Center = hand;
            Projectile.ai[0] = extension;
            Projectile.rotation = direction.ToRotation() +
                MathHelper.Pi * 0.75f;
            Projectile.timeLeft = 2;

            if ((int)attackAge % 2 == 0)
                SpawnThrustDust(hand, direction, extension);
            RefreshOrbitingSnowflakes();
            if (Projectile.owner == Main.myPlayer &&
                Main.GameUpdateCount % 15 == 0)
            {
                SpawnOrbitingSnowflake(player);
            }

            if (Projectile.owner == Main.myPlayer &&
                Projectile.localAI[0] == 0f && extension >= 0.995f)
            {
                Projectile.localAI[0] = 1f;
                Vector2 muzzle = GetSpearTip(hand, direction, extension);

                if (Collision.CanHitLine(
                    hand,
                    1,
                    1,
                    muzzle,
                    1,
                    1))
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        muzzle,
                        direction * Projectile.velocity.Length() * 3.6f,
                        ModContent.ProjectileType<ArcticPillarSpearProjectile>(),
                        Projectile.damage,
                        Projectile.knockBack * 0.85f,
                        Projectile.owner);
                }
            }

            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.24f, 0.55f, 0.75f));
        }

        private void RefreshOrbitingSnowflakes()
        {
            int visualType = ModContent.ProjectileType<
                ArcticPillarOrbitSnowflakeVisual>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile visual = Main.projectile[i];
                if (visual.active && visual.owner == Projectile.owner &&
                    visual.type == visualType)
                {
                    visual.timeLeft =
                        ArcticPillarOrbitSnowflakeVisual.PersistenceTime;
                }
            }
        }

        private void SpawnOrbitingSnowflake(Player player)
        {
            int visualType = ModContent.ProjectileType<
                ArcticPillarOrbitSnowflakeVisual>();
            if (player.ownedProjectileCounts[visualType] >= 14)
            {
                Projectile oldest = null;
                float oldestAge = -1f;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile candidate = Main.projectile[i];
                    if (!candidate.active || candidate.owner != player.whoAmI ||
                        candidate.type != visualType ||
                        candidate.localAI[0] <= oldestAge)
                    {
                        continue;
                    }

                    oldest = candidate;
                    oldestAge = candidate.localAI[0];
                }

                oldest?.Kill();
            }

            float radius = Main.rand.NextFloat(42f, 76f);
            float angularSpeed = Main.rand.NextFloat(0.045f, 0.075f) *
                (Main.rand.NextBool() ? 1f : -1f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                player.Center,
                new Vector2(radius, angularSpeed),
                visualType,
                0,
                0f,
                Projectile.owner,
                Main.rand.NextFloat(MathHelper.TwoPi),
                Main.rand.Next(3));
        }

        private void SpawnThrustDust(
            Vector2 hand,
            Vector2 direction,
            float extension)
        {
            Vector2 tipPosition = GetSpearTip(hand, direction, extension);
            Vector2 thrustVelocity = direction * Projectile.velocity.Length();
            const int dustPadding = 10;
            Dust dust = Dust.NewDustDirect(
                tipPosition - new Vector2(dustPadding),
                dustPadding * 2,
                dustPadding * 2,
                DustID.IceTorch,
                thrustVelocity.X * 0.4f,
                thrustVelocity.Y * 0.4f,
                50,
                default,
                1.2f);
            dust.velocity *= 0.5f;
            dust.velocity += thrustVelocity * 0.25f;
        }

        public override bool? Colliding(
            Rectangle projHitbox,
            Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            Vector2 spearTip = GetSpearTip(
                hand,
                direction,
                Projectile.ai[0]);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                hand,
                spearTip,
                18f,
                ref collisionPoint);
        }

        private static Vector2 GetSpearTip(
            Vector2 hand,
            Vector2 direction,
            float extension)
        {
            float tipDistance = SpearVisualLength * MathHelper.Lerp(
                StartingGripRatio,
                1f,
                MathHelper.Clamp(extension, 0f, 1f));
            return hand + direction * tipDistance;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 startGrip = texture.Size() * StartingGripRatio;
            Vector2 handleEnd = texture.Size();
            Vector2 drawOrigin = Vector2.Lerp(
                startGrip,
                handleEnd,
                MathHelper.Clamp(Projectile.ai[0], 0f, 1f));
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                drawOrigin,
                Projectile.scale,
                SpriteEffects.None);
            DrawSpearFlare();
            return false;
        }

        private void DrawSpearFlare()
        {
            if (Projectile.ai[1] <= 1f)
                return;

            float attackAge = System.Math.Max(
                0f,
                Projectile.localAI[1] - 1f);
            float attackProgress = attackAge /
                (Projectile.ai[1] - 1f);

            // 原版隐藏代码只在向外伸出的前2/3动作中播放；光芒先
            // 加速增强，再在长枪达到最远点前衰减消失。
            if (attackProgress > 2f / 3f)
                return;

            float outwardProgress = MathHelper.Clamp(
                attackProgress / (2f / 3f),
                0f,
                1f);
            float flareStrength = Utils.Remap(
                outwardProgress,
                0f,
                0.3f,
                0f,
                1f,
                true) * Utils.Remap(
                outwardProgress,
                0.3f,
                1f,
                1f,
                0f,
                true);
            flareStrength = 1f -
                (1f - flareStrength) * (1f - flareStrength);
            if (flareStrength <= 0f)
                return;

            Player player = Main.player[Projectile.owner];
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            Vector2 tipPosition = GetSpearTip(
                hand,
                direction,
                Projectile.ai[0]);
            Texture2D flareTexture =
                TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 flareOrigin = flareTexture.Size() * 0.5f;
            float flareRotation = direction.ToRotation() + MathHelper.PiOver2;
            Color flareColor = new Color(80, 140, 255, 0) * flareStrength;
            const float flareScale = 2f;

            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                flareColor,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1f) * flareScale,
                SpriteEffects.None);
            Main.EntitySpriteDraw(
                flareTexture,
                tipPosition - Main.screenPosition,
                null,
                flareColor,
                flareRotation,
                flareOrigin,
                new Vector2(flareStrength, 1.5f) * flareScale,
                SpriteEffects.None);

            for (float trail = 0.4f; trail <= 1f; trail += 0.1f)
            {
                Vector2 trailPosition = tipPosition - direction *
                    ((1f - trail) * 32f * flareScale);
                Main.EntitySpriteDraw(
                    flareTexture,
                    trailPosition - Main.screenPosition,
                    null,
                    flareColor * (0.75f * trail),
                    flareRotation,
                    flareOrigin,
                    new Vector2(
                        flareStrength * trail,
                        2f * flareStrength * trail) * flareScale,
                    SpriteEffects.None);
            }
        }
    }

    public sealed class ArcticPillarSpearProjectile : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_2";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 120;
            Projectile.alpha = 255;
            Projectile.coldDamage = true;
            Projectile.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() +
                MathHelper.PiOver2;
            Projectile.alpha = System.Math.Max(0, Projectile.alpha - 50);
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.2f, 0.5f, 0.75f));

            if (Projectile.owner != Main.myPlayer)
                return;

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Main.rand.Next(7) + 1;

            Projectile.localAI[0]++;
            int interval = Projectile.localAI[1] > 0f
                ? 8 + (int)Projectile.localAI[1] - 1
                : 8;
            if (Projectile.localAI[0] < interval)
                return;

            Projectile.localAI[0] = 0f;
            Projectile.localAI[1] = -1f;

            int snowflakeType =
                ModContent.ProjectileType<ArcticPillarSnowflakeProjectile>();
            int snowflakeDamage = (int)(Projectile.damage * 0.7f);
            float snowflakeKnockback = Projectile.knockBack * 0.55f;

            for (int verticalDirection = -1;
                verticalDirection <= 1;
                verticalDirection += 2)
            {
                Vector2 velocity = new Vector2(
                    Main.rand.NextFloat(-0.45f, 0.45f),
                    verticalDirection * Main.rand.NextFloat(0.8f, 1.4f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    snowflakeType,
                    snowflakeDamage,
                    snowflakeKnockback,
                    Projectile.owner,
                    verticalDirection,
                    Main.rand.Next(3));
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float opacity = Projectile.Opacity;
            return new Color(250, 250, 250, 100) * opacity;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.IceTorch,
                    Projectile.oldVelocity.X * 0.25f,
                    Projectile.oldVelocity.Y * 0.25f,
                    100,
                    default,
                    1.15f);
                dust.noGravity = true;
            }
        }
    }

    public sealed class ArcticPillarSnowflakeProjectile : ModProjectile
    {
        private ref float VerticalDirection => ref Projectile.ai[0];

        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_3";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 240;
            Projectile.alpha = 255;
            Projectile.coldDamage = true;
            Projectile.DamageType =
                ModContent.GetInstance<MagicalMeleeDamageClass>();
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.frame = (int)Projectile.ai[1];

            if (Projectile.localAI[0] > 5f)
                Projectile.alpha = System.Math.Max(0, Projectile.alpha - 50);

            if (Projectile.localAI[0] >= 20f)
            {
                Projectile.velocity.Y += 0.15f * VerticalDirection;
                Projectile.velocity.Y = MathHelper.Clamp(
                    Projectile.velocity.Y,
                    -6f,
                    6f);
                Projectile.velocity.X *= 0.96f;
            }

            ApplyHorizontalSeparation();
            CheckDirectionalTileCollision();
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.12f, 0.3f, 0.45f) * Projectile.Opacity);
        }

        private void ApplyHorizontalSeparation()
        {
            float push = 0f;
            const float separationRange = 60f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.whoAmI == Projectile.whoAmI ||
                    other.type != Projectile.type)
                {
                    continue;
                }

                Vector2 difference = other.position - Projectile.position;
                float distance = difference.Length();
                if (distance <= 0f || distance > separationRange)
                    continue;

                push -= System.Math.Sign(difference.X) *
                    Utils.Remap(distance, 0f, separationRange, 1f, 0f);
            }

            if (push != 0f)
                Projectile.velocity.X += System.Math.Sign(push) * 0.25f;
        }

        private void CheckDirectionalTileCollision()
        {
            int tileY = VerticalDirection > 0f
                ? (int)(Projectile.Bottom.Y + Projectile.velocity.Y) / 16
                : (int)(Projectile.Top.Y + Projectile.velocity.Y) / 16;
            int leftTileX = (int)Projectile.Left.X / 16;
            int rightTileX = (int)Projectile.Right.X / 16;

            if (WorldGen.SolidTile(leftTileX, tileY) ||
                WorldGen.SolidTile(rightTileX, tileY))
            {
                Projectile.Kill();
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float opacity = Projectile.Opacity;
            return new Color(250, 250, 250, 100) * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            Rectangle source = new Rectangle(
                0,
                frameHeight * Projectile.frame,
                texture.Width,
                frameHeight - 1);
            Vector2 origin = source.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition +
                new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = Projectile.GetAlpha(lightColor);
            float trailAge = MathHelper.Min(
                60f,
                Projectile.localAI[0]) / 2f;

            // 原版北极雪花的四层速度残影：远端残影更小、更透明。
            for (float trailFactor = 0.9f;
                trailFactor > 0f;
                trailFactor -= 0.25f)
            {
                Vector2 trailOffset = trailFactor *
                    (Projectile.velocity * 0.33f) * trailAge;
                float trailOpacity = (1f - trailFactor) * 0.75f;
                float trailScale = Projectile.scale *
                    (0.4f + (1f - trailFactor) * 0.6f);

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition - trailOffset,
                    source,
                    baseColor * trailOpacity,
                    Projectile.rotation,
                    origin,
                    trailScale,
                    SpriteEffects.None);
            }

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.IceTorch,
                    0f,
                    0f,
                    50);
                dust.noGravity = true;
                dust.velocity *= 0.75f;
            }
        }
    }

    public sealed class ArcticPillarOrbitSnowflakeVisual : ModProjectile
    {
        internal const int PersistenceTime = 30 * 60;

        public override string Texture =>
            "everflow/Content/Projectiles/ArcticPillarProjectile_3";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PersistenceTime;
            Projectile.netImportant = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            float age = Projectile.localAI[0]++;
            float angle = Projectile.ai[0] + age * Projectile.velocity.Y;
            float radius = Projectile.velocity.X;
            Vector2 orbitOffset = new Vector2(
                System.MathF.Cos(angle) * radius,
                System.MathF.Sin(angle) * radius * 0.62f);
            orbitOffset.Y += System.MathF.Sin(
                age * 0.16f + Projectile.ai[0]) * 5f;

            Projectile.Center = player.MountedCenter + orbitOffset;
            Projectile.rotation = angle + MathHelper.PiOver2;
            Projectile.frame = (int)Projectile.ai[1];

            float fadeIn = Utils.GetLerpValue(0f, 8f, age, true);
            float fadeOut = Utils.GetLerpValue(
                0f,
                30f,
                Projectile.timeLeft,
                true);
            Projectile.Opacity = fadeIn * fadeOut * 0.8f;
            Projectile.scale = MathHelper.Lerp(
                0.55f,
                0.9f,
                fadeIn) * MathHelper.Lerp(0.8f, 1f, fadeOut);
        }

        public override Color? GetAlpha(Color lightColor) =>
            new Color(210, 235, 255, 120) * Projectile.Opacity;
    }
}
