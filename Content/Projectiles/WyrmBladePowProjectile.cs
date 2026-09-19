using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladePowProjectile : ModProjectile
    {
        private static readonly Color HeadGlowColor = new(255, 232, 70);
        private int previousFlailState;
        private Vector2 lastLaunchVelocity;
        private float eyeRotation;
        private int spinShotTimer;
        private int trackedTargetIndex = -1;
        private int volleyShotsRemaining;
        private int volleyShotTimer;
        private Vector2 volleyOrigin;
        private Vector2 volleyDirection;

        private static readonly Vector2 HeadFrameOrigin = new(22f, 22f);
        // _3的非透明范围为x=8..35、y=7..34，实心像素中心为(22,21)。
        private static readonly Vector2 EyeSolidOrigin = new(22f, 21f);
        private const float DefaultBeamSpeed = 34.5f;
        private const int SpinShotInterval = 60;
        private const int VolleyShotInterval = 5;

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladePowProjectile_1";

        public override void SetDefaults()
        {
            // 只继承原版链锤的实体属性；AI在本类中完整实现，避免
            // 执行原版末尾按射弹类型生成额外弹幕的专属分支。
            Projectile.CloneDefaults(ProjectileID.FlowerPow);
            Projectile.aiStyle = 0;
            AIType = ProjectileID.None;
            // 花之力使用34x34物理碰撞箱，贴图仍按44x44原尺寸绘制。
            // 避免从玩家中心水平抛出时碰撞箱下缘嵌入脚下物块。
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void FlailStats(
            ref int launchTimeLimit,
            ref float launchSpeed,
            ref float maxLaunchLength,
            ref float retractAcceleration,
            ref float maxRetractSpeed,
            ref float forcedRetractAcceleration,
            ref float maxForcedRetractSpeed,
            ref int ricochetTimeLimit,
            ref float spinVisualDistance)
        {
            launchSpeed *= 1.5f;
            maxLaunchLength *= 1.5f;
            retractAcceleration *= 1.5f;
            maxRetractSpeed *= 1.5f;
            forcedRetractAcceleration *= 1.5f;
            maxForcedRetractSpeed *= 1.5f;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed ||
                Vector2.Distance(Projectile.Center, player.Center) > 900f)
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner && Main.mapFullscreen)
            {
                Projectile.Kill();
                return;
            }

            Vector2 mountedCenter = player.MountedCenter;
            int launchTimeLimit = 13;
            float launchSpeed = 23f;
            float maxLaunchLength = 800f;
            float retractAcceleration = 3f;
            float maxRetractSpeed = 16f;
            float forcedRetractAcceleration = 6f;
            float maxForcedRetractSpeed = 48f;
            int ricochetTimeLimit = launchTimeLimit + 5;
            float spinVisualDistance = 30f;

            FlailStats(
                ref launchTimeLimit,
                ref launchSpeed,
                ref maxLaunchLength,
                ref retractAcceleration,
                ref maxRetractSpeed,
                ref forcedRetractAcceleration,
                ref maxForcedRetractSpeed,
                ref ricochetTimeLimit,
                ref spinVisualDistance);

            // 原版内部使用1 / inverseMeleeSpeed；模组侧通过公开接口
            // 取得等价的近战总攻速倍率。
            float attackSpeedFactor = System.Math.Max(
                0.1f,
                player.GetTotalAttackSpeed(DamageClass.Melee));
            launchSpeed *= attackSpeedFactor;
            retractAcceleration *= attackSpeedFactor;
            maxRetractSpeed *= attackSpeedFactor;
            forcedRetractAcceleration *= attackSpeedFactor;
            maxForcedRetractSpeed *= attackSpeedFactor;
            float looseChainLength = launchSpeed * launchTimeLimit;
            float maximumDropLength = looseChainLength + 160f;

            Projectile.localNPCHitCooldown = 10;
            bool ownerHitCheck = false;

            switch ((int)Projectile.ai[0])
            {
                case 0: // 按住使用键旋转
                {
                    ownerHitCheck = true;
                    if (Projectile.owner == Main.myPlayer)
                    {
                        Vector2 aim = mountedCenter.DirectionTo(Main.MouseWorld)
                            .SafeNormalize(Vector2.UnitX * player.direction);
                        player.ChangeDir(aim.X > 0f ? 1 : -1);
                        if (!player.channel)
                        {
                            Projectile.ai[0] = 1f;
                            Projectile.ai[1] = 0f;
                            Projectile.velocity = aim * launchSpeed + player.velocity;
                            Projectile.Center = mountedCenter;
                            Projectile.netUpdate = true;
                            Projectile.ResetLocalNPCHitImmunity();
                            break;
                        }
                    }

                    Projectile.localAI[1]++;
                    Vector2 spinOffset = new Vector2(player.direction, 0f).RotatedBy(
                        MathHelper.TwoPi * 5f *
                        (Projectile.localAI[1] / 60f) * player.direction);
                    spinOffset.Y *= 0.8f;
                    if (spinOffset.Y * player.gravDir > 0f)
                        spinOffset.Y *= 0.5f;
                    Projectile.Center = mountedCenter +
                        spinOffset * spinVisualDistance;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.localNPCHitCooldown = 12;
                    break;
                }

                case 1: // 向前抛出
                {
                    bool reachedLimit = Projectile.ai[1]++ >= launchTimeLimit ||
                        Projectile.Distance(mountedCenter) >= maxLaunchLength;
                    if (player.controlUseItem)
                    {
                        Projectile.ai[0] = 6f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.2f;
                        break;
                    }
                    if (reachedLimit)
                    {
                        Projectile.ai[0] = 2f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.3f;
                    }
                    player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    break;
                }

                case 2: // 正常回收
                {
                    Vector2 retractDirection = Projectile.DirectionTo(mountedCenter)
                        .SafeNormalize(Vector2.Zero);
                    if (Projectile.Distance(mountedCenter) <= maxRetractSpeed)
                    {
                        Projectile.Kill();
                        return;
                    }
                    if (player.controlUseItem)
                    {
                        Projectile.ai[0] = 6f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.2f;
                    }
                    else
                    {
                        Projectile.velocity *= 0.98f;
                        Projectile.velocity = Projectile.velocity.MoveTowards(
                            retractDirection * maxRetractSpeed,
                            retractAcceleration);
                        player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    }
                    break;
                }

                case 3: // 松链状态
                {
                    if (!player.controlUseItem)
                    {
                        Projectile.ai[0] = 4f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                        break;
                    }

                    float distance = Projectile.Distance(mountedCenter);
                    Projectile.tileCollide = Projectile.ai[1] == 1f;
                    bool shouldCollide = distance <= looseChainLength;
                    if (shouldCollide != Projectile.tileCollide)
                    {
                        Projectile.tileCollide = shouldCollide;
                        Projectile.ai[1] = shouldCollide ? 1f : 0f;
                        Projectile.netUpdate = true;
                    }

                    if (distance > 60f)
                    {
                        Vector2 towardPlayer = Projectile.DirectionTo(mountedCenter)
                            .SafeNormalize(Vector2.Zero);
                        if (distance >= looseChainLength)
                        {
                            Projectile.velocity *= 0.5f;
                            Projectile.velocity = Projectile.velocity.MoveTowards(
                                towardPlayer * 14f * attackSpeedFactor,
                                14f * attackSpeedFactor);
                        }
                        Projectile.velocity *= 0.98f;
                        Projectile.velocity = Projectile.velocity.MoveTowards(
                            towardPlayer * 14f * attackSpeedFactor,
                            attackSpeedFactor);
                    }
                    else
                    {
                        if (Projectile.velocity.Length() < 6f)
                        {
                            Projectile.velocity.X *= 0.96f;
                            Projectile.velocity.Y += 0.2f;
                        }
                        if (player.velocity.X == 0f)
                            Projectile.velocity.X *= 0.96f;
                    }
                    player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    break;
                }

                case 4: // 强制回收
                {
                    Projectile.tileCollide = false;
                    Vector2 retractDirection = Projectile.DirectionTo(mountedCenter)
                        .SafeNormalize(Vector2.Zero);
                    if (Projectile.Distance(mountedCenter) <= maxForcedRetractSpeed)
                    {
                        Projectile.Kill();
                        return;
                    }
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity = Projectile.velocity.MoveTowards(
                        retractDirection * maxForcedRetractSpeed,
                        forcedRetractAcceleration);
                    Vector2 nextCenter = Projectile.Center + Projectile.velocity;
                    Vector2 pastPlayer = mountedCenter.DirectionFrom(nextCenter)
                        .SafeNormalize(Vector2.Zero);
                    if (Vector2.Dot(retractDirection, pastPlayer) < 0f)
                    {
                        Projectile.Kill();
                        return;
                    }
                    player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    break;
                }

                case 5: // 碰撞反弹
                    if (Projectile.ai[1]++ >= ricochetTimeLimit)
                    {
                        Projectile.ai[0] = 6f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        Projectile.velocity.Y += 0.6f;
                        Projectile.velocity.X *= 0.95f;
                        player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    }
                    break;

                case 6: // 自由垂落
                    if (!player.controlUseItem ||
                        Projectile.Distance(mountedCenter) > maximumDropLength)
                    {
                        Projectile.ai[0] = 4f;
                        Projectile.ai[1] = 0f;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        if (!Projectile.shimmerWet)
                            Projectile.velocity.Y += 0.8f;
                        Projectile.velocity.X *= 0.95f;
                        player.ChangeDir(player.Center.X < Projectile.Center.X ? 1 : -1);
                    }
                    break;
            }

            Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.ownerHitCheck = ownerHitCheck;
            Projectile.timeLeft = 2;
            player.heldProj = Projectile.whoAmI;
            player.SetDummyItemTime(2);
            player.itemRotation = Projectile.DirectionFrom(mountedCenter).ToRotation();
            if (Projectile.Center.X < mountedCenter.X)
                player.itemRotation += MathHelper.Pi;
            player.itemRotation = MathHelper.WrapAngle(player.itemRotation);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int state = (int)Projectile.ai[0];
            float bounceFactor = 0.2f;
            if (state == 1 || state == 5)
                bounceFactor = 0.4f;
            else if (state == 6)
                bounceFactor = 0f;

            int strongCollisionEffects = 0;
            if (oldVelocity.X != Projectile.velocity.X)
            {
                if (System.Math.Abs(oldVelocity.X) > 4f)
                    strongCollisionEffects = 1;
                Projectile.velocity.X = -oldVelocity.X * bounceFactor;
                Projectile.localAI[0]++;
            }
            if (oldVelocity.Y != Projectile.velocity.Y)
            {
                if (System.Math.Abs(oldVelocity.Y) > 4f)
                    strongCollisionEffects = 1;
                Projectile.velocity.Y = -oldVelocity.Y * bounceFactor;
                Projectile.localAI[0]++;
            }

            if (state == 1)
            {
                Projectile.ai[0] = 5f;
                Projectile.ai[1] = 0f;
                Projectile.localNPCHitCooldown = 10;
                Projectile.netUpdate = true;
                strongCollisionEffects = 2;

                // 原版会撤销撞击这一帧的位移，防止锤头留在物块内
                // 连续碰撞并被多次反向加速。
                Projectile.position -= oldVelocity;
            }

            if (strongCollisionEffects > 0)
            {
                Projectile.netUpdate = true;
                for (int i = 0; i < strongCollisionEffects; i++)
                {
                    Collision.HitTiles(
                        Projectile.position,
                        oldVelocity,
                        Projectile.width,
                        Projectile.height);
                }
                SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            }

            if (state != 3 && state != 0 && state != 5 && state != 6 &&
                Projectile.localAI[0] >= 10f)
            {
                Projectile.ai[0] = 4f;
                Projectile.netUpdate = true;
            }

            if (strongCollisionEffects == 0)
            {
                Collision.HitTiles(
                    Projectile.position,
                    oldVelocity,
                    Projectile.width,
                    Projectile.height);
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawChain();

            Texture2D frameTexture = TextureAssets.Projectile[Type].Value;
            Texture2D eyeTexture = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/WyrmBladePowProjectile_3").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 eyePivot = GetEyePivotWorld() - Main.screenPosition;

            // 眼球先画在下层，并围绕非透明像素中心独立转向。
            Main.EntitySpriteDraw(
                eyeTexture,
                eyePivot,
                null,
                Color.White,
                eyeRotation,
                EyeSolidOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                eyeTexture,
                eyePivot,
                null,
                new Color(HeadGlowColor.R, HeadGlowColor.G, HeadGlowColor.B, 0)
                    * 0.42f,
                eyeRotation,
                EyeSolidOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            // 外框沿用旧锤头方向，并最后绘制以稳定覆盖眼球边缘。
            Main.EntitySpriteDraw(
                frameTexture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                HeadFrameOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            Main.EntitySpriteDraw(
                frameTexture,
                drawPosition,
                null,
                new Color(HeadGlowColor.R, HeadGlowColor.G, HeadGlowColor.B, 0)
                    * 0.42f,
                Projectile.rotation,
                HeadFrameOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        // aiStyle 15会先绘制一条原版细链；返回false，只保留自定义锁链。
        public override bool PreDrawExtras() => false;

        public override void PostAI()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 throwDirection = (Projectile.Center - player.MountedCenter)
                .SafeNormalize(Vector2.UnitX * player.direction);

            // 贴图上方始终朝向锤头相对玩家的掷出方向，不继承链锤自转。
            Projectile.rotation = throwDirection.ToRotation() + MathHelper.PiOver2;

            // 锤头自身是明黄色高热源，并提供中等范围的真实照明。
            Lighting.AddLight(
                Projectile.Center,
                HeadGlowColor.ToVector3() * 0.65f);

            int currentFlailState = (int)Projectile.ai[0];
            if (currentFlailState == 0)
            {
                lastLaunchVelocity = Vector2.Zero;
                trackedTargetIndex = -1;
                UpdateSpinningEye(player);
            }
            else if (currentFlailState == 1 &&
                Projectile.velocity.LengthSquared() > 0.001f)
            {
                // 记录状态机施加攻速修正后的真实抛投速度。
                lastLaunchVelocity = Projectile.velocity;
                UpdateThrownEye(player, throwDirection);
            }
            else
            {
                // 除挥舞抡圈外，抛出、回收、松链、反弹及沉底状态
                // 全部持续追踪距离锤头最近的敌怪，不限制方位。
                UpdateReturningEye(player, throwDirection);
            }

            // 链锤从状态1（抛出）自然切换到状态2（到达最大链长
            // 后回收）。强制回收和撞墙会进入其他状态。
            if (previousFlailState == 1 &&
                currentFlailState == 2)
            {
                BeginThreeShotVolley(throwDirection);
            }

            UpdateThreeShotVolley();

            previousFlailState = currentFlailState;
        }

        private Vector2 GetEyePivotWorld()
        {
            Vector2 solidCenterOffset = EyeSolidOrigin - HeadFrameOrigin;
            return Projectile.Center +
                solidCenterOffset.RotatedBy(Projectile.rotation) * Projectile.scale;
        }

        private void UpdateSpinningEye(Player player)
        {
            Vector2 aimDirection = Projectile.rotation.ToRotationVector2();
            if (Projectile.owner == Main.myPlayer)
            {
                aimDirection = (Main.MouseWorld - GetEyePivotWorld())
                    .SafeNormalize(Vector2.UnitX * player.direction);
            }

            eyeRotation = aimDirection.ToRotation() + MathHelper.PiOver2;
            spinShotTimer++;
            if (spinShotTimer < SpinShotInterval)
                return;

            spinShotTimer = 0;
            SpawnEnchantedBeam(
                GetEyePivotWorld(),
                aimDirection,
                GetActualBeamSpeed());
        }

        private void UpdateThrownEye(Player player, Vector2 throwDirection)
        {
            spinShotTimer = 0;
            trackedTargetIndex = FindNearestTarget();

            Vector2 aimDirection = throwDirection;
            if (TryGetTrackedTarget(out NPC target))
            {
                aimDirection = (target.Center - GetEyePivotWorld())
                    .SafeNormalize(throwDirection);
            }

            eyeRotation = aimDirection.ToRotation() + MathHelper.PiOver2;
        }

        private void UpdateReturningEye(Player player, Vector2 fallbackDirection)
        {
            spinShotTimer = 0;
            Vector2 originalThrowDirection = lastLaunchVelocity.SafeNormalize(
                fallbackDirection);
            trackedTargetIndex = FindNearestTarget();

            Vector2 aimDirection = originalThrowDirection;
            if (TryGetTrackedTarget(out NPC target))
            {
                aimDirection = (target.Center - GetEyePivotWorld())
                    .SafeNormalize(originalThrowDirection);
            }

            eyeRotation = aimDirection.ToRotation() + MathHelper.PiOver2;
        }

        private int FindNearestTarget()
        {
            int nearestIndex = -1;
            float nearestDistanceSquared = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(
                    Projectile.Center,
                    npc.Center);
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestIndex = i;
            }

            return nearestIndex;
        }

        private bool TryGetTrackedTarget(out NPC target)
        {
            if (trackedTargetIndex >= 0 && trackedTargetIndex < Main.maxNPCs)
            {
                target = Main.npc[trackedTargetIndex];
                if (target.CanBeChasedBy(Projectile, false))
                    return true;
            }

            target = null;
            return false;
        }

        private void BeginThreeShotVolley(Vector2 fallbackDirection)
        {
            volleyOrigin = GetEyePivotWorld();
            // 三连射严格沿眼球此刻的视觉朝向发射，而不是在发射时
            // 再读取敌怪坐标，保证视觉瞄准与弹道完全一致。
            volleyDirection = (eyeRotation - MathHelper.PiOver2)
                .ToRotationVector2()
                .SafeNormalize(fallbackDirection);

            volleyShotsRemaining = 3;
            volleyShotTimer = 0;
        }

        private void UpdateThreeShotVolley()
        {
            if (volleyShotsRemaining <= 0)
                return;

            if (volleyShotTimer > 0)
            {
                volleyShotTimer--;
                return;
            }

            SpawnEnchantedBeam(
                volleyOrigin,
                volleyDirection,
                GetActualBeamSpeed());
            volleyShotsRemaining--;
            volleyShotTimer = VolleyShotInterval - 1;
        }

        private float GetActualBeamSpeed()
        {
            float speed = lastLaunchVelocity.Length();
            return float.IsFinite(speed) && speed > 0.01f
                ? speed
                : DefaultBeamSpeed;
        }

        private void SpawnEnchantedBeam(
            Vector2 position,
            Vector2 direction,
            float speed)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                position,
                direction.SafeNormalize(Vector2.UnitX) * speed,
                ModContent.ProjectileType<WyrmBladePowEnchantedBeam>(),
                System.Math.Max(1, (int)(Projectile.damage * 0.5f)),
                Projectile.knockBack,
                Projectile.owner);
        }

        private void DrawChain()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active)
                return;

            Texture2D chainTexture = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/WyrmBladePowProjectile_2").Value;
            // 从锤头端向手部铺链：锤头侧始终固定，伸长时新增的
            // 链节只会出现在手部一端，与原版链锤的视觉逻辑一致。
            Vector2 chainStart = Projectile.Center;
            Vector2 toPlayer = player.MountedCenter - chainStart;
            float remainingLength = toPlayer.Length();
            // 状态切换期间若坐标短暂成为NaN/Infinity，不能进入按距离
            // 递减的循环，否则Infinity永远减不到0并直接卡死主线程。
            if (!float.IsFinite(remainingLength) || remainingLength <= 2f)
                return;

            // 16x108素材由6个16x18区域构成；每个区域的末2行透明，
            // 实际链节长度为16px。逐节裁切可让首节恰好从手中开始。
            const int sourceFrameHeight = 18;
            const int segmentLength = 16;
            if (!float.IsFinite(segmentLength) || segmentLength <= 0f)
                return;

            Vector2 direction = toPlayer / remainingLength;
            if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y))
                return;

            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            Vector2 origin = new(chainTexture.Width * 0.5f, 0f);

            // 原版链锤的锁链长度受状态机约束；这里再加硬上限，确保
            // 即使其他Mod修改射弹坐标，也不可能形成无界绘制循环。
            const int MaximumDrawnSegments = 64;
            for (int segment = 0;
                segment < MaximumDrawnSegments && remainingLength > 0f;
                segment++)
            {
                int drawnLength = (int)MathHelper.Min(
                    segmentLength,
                    System.MathF.Ceiling(remainingLength));
                int frame = segment % 6;
                Rectangle source = new(
                    0,
                    frame * sourceFrameHeight,
                    chainTexture.Width,
                    drawnLength);

                Color chainColor = Lighting.GetColor(
                    chainStart.ToTileCoordinates());
                Main.EntitySpriteDraw(
                    chainTexture,
                    chainStart - Main.screenPosition,
                    source,
                    chainColor,
                    rotation,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f);

                chainStart += direction * drawnLength;
                remainingLength -= drawnLength;
            }
        }
    }
}
