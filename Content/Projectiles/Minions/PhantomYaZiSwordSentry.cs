using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles.Minions
{
    public sealed class PhantomYaZiSwordSentry : ModProjectile
    {
        private enum SwordState { Idle, PreTurn, Approach, Slash, Return }

        private const int PreTurnDuration = 10;
        private const int SlashDuration = 30;
        // 下列配置均表示从中心向每个方向延伸的格数，而不是方框总宽度。
        private const float DetectionHalfExtent = 18f * 16f;
        private const float LeashHalfExtent = 24f * 16f;
        private const float AttackHalfExtent = 6f * 16f;
        private const float HoverAmplitude = 4f;
        private const float HoverSpeed = 0.05f;
        private const float InitialFlightSpeed = 30f;
        private const float FollowSpeed = 4f;
        private const float ReturnSpeed = 24f;
        private const float IdleRotation = MathHelper.Pi * 0.75f;
        private const float PreTurnAngle = MathHelper.Pi / 6f;
        private const float SlashArc = MathHelper.Pi * 4f / 3f;
        private const float BladeLength = 112f;
        private const float BladeCollisionWidth = 18f;
        private static readonly Color EmissiveColor = new(255, 230, 65);

        // Phantom_YaZi_Sword_1中剑柄末端的像素位置。
        private static readonly Vector2 HandleOrigin = new(8f, 88f);

        private Vector2 homeHandlePosition;
        private float approachStartDistance;
        private float slashStartRotation;
        private int slashDirection = 1;
        private bool initialized;
        private float visualTimer;

        private ref float StateValue => ref Projectile.ai[0];
        private ref float TargetValue => ref Projectile.ai[1];
        private ref float StateTimer => ref Projectile.ai[2];

        private SwordState State
        {
            get => (SwordState)(int)StateValue;
            set => StateValue = (float)value;
        }

        private int TargetIndex
        {
            get => (int)TargetValue - 1;
            set => TargetValue = value + 1;
        }

        public Vector2 GroundVisualPosition =>
            homeHandlePosition + Vector2.UnitY * 116f;

        public bool IsPerformingSlash => State == SwordState.Slash;

        public override string Texture =>
            "everflow/Content/Projectiles/Minions/Phantom_YaZi_Sword_1";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            // 实体中心代表剑柄；伤害使用从剑柄伸向剑尖的线段判定。
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.sentry = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = Projectile.SentryLifeTime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SlashDuration;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(homeHandlePosition.X);
            writer.Write(homeHandlePosition.Y);
            writer.Write(approachStartDistance);
            writer.Write(slashStartRotation);
            writer.Write(slashDirection);
            writer.Write(initialized);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            homeHandlePosition.X = reader.ReadSingle();
            homeHandlePosition.Y = reader.ReadSingle();
            approachStartDistance = reader.ReadSingle();
            slashStartRotation = reader.ReadSingle();
            slashDirection = reader.ReadInt32();
            initialized = reader.ReadBoolean();
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                homeHandlePosition = Projectile.Center;
                Projectile.rotation = IdleRotation;
                TargetIndex = -1;
                Projectile.netUpdate = true;
            }

            visualTimer++;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = State == SwordState.Slash;
            if (State != SwordState.Slash)
                Projectile.scale = 1f;

            // 剑身始终是较强的明黄色热源与光源。
            Lighting.AddLight(
                Projectile.Center,
                EmissiveColor.ToVector3() * 1.25f);

            if (State == SwordState.Slash)
                SpawnSlashParticles();

            switch (State)
            {
                case SwordState.Idle:
                    UpdateIdle();
                    break;
                case SwordState.PreTurn:
                    UpdatePreTurn();
                    break;
                case SwordState.Approach:
                    UpdateApproach();
                    break;
                case SwordState.Slash:
                    UpdateSlash();
                    break;
                case SwordState.Return:
                    UpdateReturn();
                    break;
            }
        }

        private void SpawnSlashParticles()
        {
            // 平均每帧约1.5粒，形成清晰但不过密的斩击粒子带。
            int count = Main.rand.NextBool() ? 2 : 1;
            Vector2 bladeDirection =
                (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();

            for (int i = 0; i < count; i++)
            {
                Vector2 position = Projectile.Center + bladeDirection *
                    Main.rand.NextFloat(12f, BladeLength * Projectile.scale);
                Vector2 velocity = bladeDirection.RotatedBy(MathHelper.PiOver2) *
                    Main.rand.NextFloat(-1.8f, 1.8f) +
                    Main.rand.NextVector2Circular(0.65f, 0.65f);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.TintableDustLighted,
                    velocity,
                    70,
                    EmissiveColor,
                    Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }
        }

        private void UpdateIdle()
        {
            StateTimer = 0f;
            Projectile.rotation = IdleRotation;
            Projectile.Center = homeHandlePosition +
                Vector2.UnitY * MathF.Sin(visualTimer * HoverSpeed) * HoverAmplitude;
            EnsureIdleVisual();

            if (Projectile.localAI[0] > 0f)
            {
                Projectile.localAI[0]--;
                return;
            }

            Projectile.localAI[0] = 3f;
            if (Projectile.owner != Main.myPlayer)
                return;

            Rectangle detectionArea = Utils.CenteredRectangle(
                Projectile.Center,
                new Vector2(DetectionHalfExtent * 2f));
            int nearestTarget = -1;
            float nearestDistanceSquared = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false) ||
                    !detectionArea.Intersects(npc.Hitbox))
                {
                    continue;
                }

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestTarget = i;
            }

            if (nearestTarget == -1)
                return;

            TargetIndex = nearestTarget;
            NPC target = Main.npc[nearestTarget];
            Projectile.direction = target.Center.X >= Projectile.Center.X ? 1 : -1;
            State = SwordState.PreTurn;
            StateTimer = 0f;
            Projectile.netUpdate = true;
        }

        private void EnsureIdleVisual()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int visualType = ModContent.ProjectileType<PhantomYaZiSwordIdleVisual>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile visual = Main.projectile[i];
                if (visual.active &&
                    visual.owner == Projectile.owner &&
                    visual.type == visualType &&
                    (int)visual.ai[0] == Projectile.identity)
                {
                    return;
                }
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GroundVisualPosition,
                Vector2.Zero,
                visualType,
                0,
                0f,
                Projectile.owner,
                Projectile.identity);
        }

        private void UpdatePreTurn()
        {
            NPC target = GetLivingTarget();
            if (target is null)
            {
                BeginReturn();
                return;
            }

            StateTimer++;
            float progress = MathHelper.Clamp(StateTimer / PreTurnDuration, 0f, 1f);
            // 剑柄朝目标倾斜，让剑柄拖着位于后方的剑刃飞行。
            float tiltedRotation = IdleRotation - Projectile.direction * PreTurnAngle;
            Projectile.rotation = MathHelper.Lerp(
                IdleRotation,
                tiltedRotation,
                SmoothStep(progress));

            if (StateTimer < PreTurnDuration)
                return;

            approachStartDistance = Math.Max(
                Vector2.Distance(Projectile.Center, GetDesiredHandlePosition(target)),
                1f);
            State = SwordState.Approach;
            StateTimer = 0f;
            Projectile.netUpdate = true;
        }

        private void UpdateApproach()
        {
            NPC target = GetLivingTarget();
            if (target is null)
            {
                BeginReturn();
                return;
            }

            Vector2 destination = GetDesiredHandlePosition(target);
            Vector2 toDestination = destination - Projectile.Center;
            float remainingDistance = toDestination.Length();
            if (IsHandleInsideAttackArea(target))
            {
                BeginSlash(target, Projectile.direction);
                return;
            }

            float halfwayDistance = approachStartDistance * 0.5f;
            float speed = InitialFlightSpeed;
            if (remainingDistance < halfwayDistance)
            {
                speed *= MathHelper.Clamp(
                    remainingDistance / halfwayDistance,
                    0f,
                    1f);
            }

            speed = Math.Max(speed, 1f);
            Projectile.Center += toDestination.SafeNormalize(Vector2.Zero) *
                Math.Min(speed, remainingDistance);
        }

        private void BeginSlash(NPC target, int direction)
        {
            slashDirection = direction == 0 ? 1 : Math.Sign(direction);
            Vector2 aimDirection = (target.Center - Projectile.Center)
                .SafeNormalize(Vector2.UnitX);
            float aimRotation = aimDirection.ToRotation() + MathHelper.PiOver4;

            // 目标位于240度弧线中央，保证剑刃在斩击中扫过目标。
            slashStartRotation = aimRotation - slashDirection * SlashArc * 0.5f;
            Projectile.rotation = slashStartRotation;
            Projectile.scale = 1f;
            State = SwordState.Slash;
            StateTimer = 0f;
            Projectile.friendly = true;
            Projectile.netUpdate = true;
            SpawnSlashVisual();
        }

        private void SpawnSlashVisual()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PhantomYaZiSwordSlashVisual>(),
                0,
                0f,
                Projectile.owner,
                Projectile.identity,
                slashDirection,
                slashStartRotation);
        }

        private void UpdateSlash()
        {
            NPC target = GetLivingTarget();
            if (target is null)
            {
                BeginReturn();
                return;
            }

            StateTimer++;
            Projectile.Center = MoveTowards(
                Projectile.Center,
                GetDesiredHandlePosition(target),
                FollowSpeed);
            float progress = MathHelper.Clamp(StateTimer / SlashDuration, 0f, 1f);
            float easedProgress = PowerfulEaseInOut(progress);
            Projectile.rotation = slashStartRotation +
                slashDirection * SlashArc * easedProgress;
            Projectile.scale = 1f + 0.5f * MathF.Sin(MathHelper.Pi * progress);

            if (StateTimer < SlashDuration)
                return;

            // 每轮斩击正好30帧；目标仍存活时立刻按相反方向开始下一轮。
            BeginSlash(target, -slashDirection);
        }

        private void BeginReturn()
        {
            TargetIndex = -1;
            State = SwordState.Return;
            StateTimer = 0f;
            Projectile.friendly = false;
            Projectile.netUpdate = true;
        }

        private void UpdateReturn()
        {
            Vector2 toHome = homeHandlePosition - Projectile.Center;
            float distance = toHome.Length();
            if (distance <= 1f)
            {
                Projectile.Center = homeHandlePosition;
                Projectile.rotation = IdleRotation;
                State = SwordState.Idle;
                StateTimer = 0f;
                Projectile.localAI[0] = 3f;
                Projectile.netUpdate = true;
                return;
            }

            float speed = Math.Min(ReturnSpeed, Math.Max(1f, distance * 0.2f));
            Projectile.Center += toHome.SafeNormalize(Vector2.Zero) *
                Math.Min(speed, distance);
            Projectile.rotation = Utils.AngleLerp(
                Projectile.rotation,
                IdleRotation,
                0.15f);
        }

        private NPC GetLivingTarget()
        {
            int index = TargetIndex;
            if (index < 0 || index >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[index];
            if (!target.active || target.life <= 0 || target.friendly)
                return null;

            Rectangle leashArea = Utils.CenteredRectangle(
                homeHandlePosition,
                new Vector2(LeashHalfExtent * 2f));
            return leashArea.Intersects(target.Hitbox) ? target : null;
        }

        private bool IsHandleInsideAttackArea(NPC target)
        {
            Vector2 offset = Projectile.Center - target.Center;
            return Math.Abs(offset.X) <= AttackHalfExtent &&
                Math.Abs(offset.Y) <= AttackHalfExtent;
        }

        private Vector2 GetDesiredHandlePosition(NPC target)
        {
            Vector2 targetToHome = (homeHandlePosition - target.Center)
                .SafeNormalize(-Vector2.UnitX * Projectile.direction);
            return target.Center + targetToHome * (AttackHalfExtent - 8f);
        }

        private static Vector2 MoveTowards(
            Vector2 current,
            Vector2 destination,
            float maxDistance)
        {
            Vector2 offset = destination - current;
            float distance = offset.Length();
            if (distance <= maxDistance || distance == 0f)
                return destination;

            return current + offset / distance * maxDistance;
        }

        private static float SmoothStep(float progress) =>
            progress * progress * (3f - 2f * progress);

        private static float PowerfulEaseInOut(float progress)
        {
            // 三次幂比例曲线：起止角速度接近0，中点角速度约为平均值的3倍。
            float accelerated = progress * progress * progress;
            float remaining = 1f - progress;
            float decelerated = remaining * remaining * remaining;
            return accelerated / (accelerated + decelerated);
        }

        public override bool? CanDamage() =>
            State == SwordState.Slash ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 bladeDirection =
                (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
            Vector2 lineStart = Projectile.Center;
            Vector2 lineEnd = lineStart +
                bladeDirection * BladeLength * Projectile.scale;
            float collisionPoint = 0f;

            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                lineStart,
                lineEnd,
                BladeCollisionWidth * Projectile.scale,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // 不采用环境光色，保证黑暗处剑身仍保留贴图细节并呈现自发光。
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                HandleOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            // 低透明度的明黄色叠层提供自发光色调，不会抹掉剑身纹理。
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                EmissiveColor * 0.32f,
                Projectile.rotation,
                HandleOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }
    }
}
