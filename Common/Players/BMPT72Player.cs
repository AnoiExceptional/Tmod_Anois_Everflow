using System;
using everflow.Content.Mounts;
using everflow.Content.Buffs;
using everflow.Common.Projectiles;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using everflow.Common.DrawLayers;
using Terraria.GameInput;
using Terraria.GameContent;

namespace everflow.Common.Players
{
    public sealed class BMPT72Player : ModPlayer
    {
        private const int VehicleHitboxWidth = 9 * 16;
        private const int VehicleHitboxHeight = 5 * 16;
        private const int DefaultPlayerWidth = 20;
        private const int DefaultPlayerHeight = 42;

        public int CrewCount { get; set; }
        public int HullDirection { get; set; } = 1;
        private int cannonCooldown;
        private int rocketCooldown;
        private int rocketShotsInBurst;
        private bool vehicleHitboxApplied;
        private int enginePulseTimer;
        private bool engineWasAccelerating;
        private bool infraredThermalImagingEnabled;
        private bool wasDrivingBMPT72;

        public override void SaveData(TagCompound tag)
        {
            tag["BMPT72InfraredThermalImagingEnabled"] =
                infraredThermalImagingEnabled;
        }

        public override void LoadData(TagCompound tag)
        {
            infraredThermalImagingEnabled =
                tag.GetBool("BMPT72InfraredThermalImagingEnabled");
        }

        // 与合金二号坦克相同的低音钻头脉冲，形成活塞/履带发动机节拍。
        private static readonly SoundStyle PistonEnginePulseSound = SoundID.Item22 with
        {
            Volume = 1f,
            Pitch = -1f,
            PitchVariance = 0f,
            MaxInstances = 16
        };

        public override void PreUpdateMovement()
        {
            if (Player.mount.Active && Player.mount.Type == ModContent.MountType<BMPT72>())
                ApplyVehicleHitbox();
            else if (vehicleHitboxApplied)
                RestoreDefaultHitbox();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            bool drivingBMPT72 = Player.mount.Active &&
                Player.mount.Type == ModContent.MountType<BMPT72>();

            if (!drivingBMPT72)
            {
                if (wasDrivingBMPT72)
                {
                    wasDrivingBMPT72 = false;
                    SuspendInfraredThermalImaging();
                }
                return;
            }

            // infraredThermalImagingEnabled保存的是驾驶员偏好。重新上车时
            // PreUpdateBuffs会依据该偏好自动恢复效果，无需再次按键。
            wasDrivingBMPT72 = true;

            if (PlayerInput.Triggers.JustPressed.QuickHeal)
            {
                // 坐骑通过noItems禁止普通物品，但快速治疗应作为唯一例外。
                bool originalNoItems = Player.noItems;
                Player.noItems = false;
                Player.QuickHeal();
                Player.noItems = originalNoItems;

                // 已手动执行，阻止原版流程在同一按键中重复调用。
                triggersSet.QuickHeal = false;
                PlayerInput.Triggers.Current.QuickHeal = false;
            }

            if (PlayerInput.Triggers.JustPressed.QuickBuff)
            {
                infraredThermalImagingEnabled = !infraredThermalImagingEnabled;
                if (!infraredThermalImagingEnabled)
                    ClearInfraredBuffs();
            }

            // 驾驶BMPT-72时，快速增益键完全归红外开关使用。
            triggersSet.QuickBuff = false;
            PlayerInput.Triggers.Current.QuickBuff = false;
        }

        public override void PreUpdateBuffs()
        {
            if (!infraredThermalImagingEnabled || !Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<BMPT72>())
            {
                return;
            }

            Player.AddBuff(ModContent.BuffType<IRSightBuff>(), 2);
        }

        private void SuspendInfraredThermalImaging()
        {
            ClearInfraredBuffs();
        }

        private void ClearInfraredBuffs()
        {
            Player.ClearBuff(ModContent.BuffType<IRSightBuff>());
            // 清理由旧实现留下的两个原版Buff；新实现只显示一个红外热成像Buff。
            Player.ClearBuff(BuffID.NightOwl);
            Player.ClearBuff(BuffID.Hunter);
        }

        public void ApplyVehicleHitbox()
        {
            ResizeHitboxKeepingBottomCenter(VehicleHitboxWidth, VehicleHitboxHeight);
            vehicleHitboxApplied = true;
        }

        public void RestoreDefaultHitbox()
        {
            ResizeHitboxKeepingBottomCenter(DefaultPlayerWidth, DefaultPlayerHeight);
            vehicleHitboxApplied = false;
        }

        private void ResizeHitboxKeepingBottomCenter(int width, int height)
        {
            if (Player.width == width && Player.height == height)
                return;

            Vector2 bottomCenter = new(Player.Center.X, Player.Bottom.Y);
            Player.width = width;
            Player.height = height;
            Player.position = bottomCenter - new Vector2(width * 0.5f, height);
        }

        public override void PostUpdate()
        {
            UpdateEngineSound();

            if (cannonCooldown > 0)
                cannonCooldown--;
            if (rocketCooldown > 0)
                rocketCooldown--;

            if (!Player.mount.Active || Player.mount.Type != ModContent.MountType<BMPT72>())
            {
                cannonCooldown = 0;
                rocketCooldown = 0;
                rocketShotsInBurst = 0;
                return;
            }

            if (Player.controlLeft && !Player.controlRight)
                HullDirection = -1;
            else if (Player.controlRight && !Player.controlLeft)
                HullDirection = 1;

            if (Player.whoAmI == Main.myPlayer)
            {
                float aimX = Main.MouseWorld.X - Player.Center.X;
                if (Math.Abs(aimX) > 0.01f)
                    Player.ChangeDir(aimX >= 0f ? 1 : -1);
            }

            if (Player.whoAmI != Main.myPlayer || Main.blockMouse || Player.mouseInterface)
            {
                return;
            }

            bool fireCannon = Main.mouseLeft;
            bool fireRocket = Main.mouseRight;

            if (fireRocket && rocketCooldown <= 0)
                TryFireRocket();

            if (!fireCannon || cannonCooldown > 0)
                return;

            Vector2 pivotWorld = BMPT72.GetWeaponPivotWorld(Player);
            Vector2 aimDirection = Main.MouseWorld - pivotWorld;
            if (aimDirection.LengthSquared() < 0.001f)
                aimDirection = Vector2.UnitX * Player.direction;
            aimDirection.Normalize();

            float spreadRadians = MathHelper.ToRadians(
                Main.rand.NextFloat(-1.5f, 1.5f));
            aimDirection = aimDirection.RotatedBy(spreadRadians);

            Vector2 muzzleWorld = pivotWorld + aimDirection * BMPT72.WeaponLength;
            int damage = (int)Player.GetTotalDamage(DamageClass.Ranged).ApplyTo(200f);
            float knockback = Player.GetTotalKnockback(DamageClass.Ranged).ApplyTo(3f);
            int projectileIndex = Projectile.NewProjectile(
                Player.GetSource_Misc("BMPT72Autocannon"),
                muzzleWorld,
                aimDirection * 27f,
                ModContent.ProjectileType<BMPT72AutocannonBullet>(),
                damage,
                knockback,
                Player.whoAmI);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Projectile projectile = Main.projectile[projectileIndex];
                // 原版玩家拥有4%基础暴击；武器面板15%应只比该基准高11点。
                projectile.CritChance = (int)Player.GetTotalCritChance(DamageClass.Ranged) + 11;
                projectile.netUpdate = true;
            }

            Item onyxBlaster = new(ItemID.OnyxBlaster);
            if (onyxBlaster.UseSound.HasValue)
                SoundEngine.PlaySound(onyxBlaster.UseSound.Value, muzzleWorld);
            cannonCooldown = 8;
        }

        private void UpdateEngineSound()
        {
            if (Main.dedServ)
                return;

            bool mounted = Player.active && !Player.dead &&
                Player.mount.Active &&
                Player.mount.Type == ModContent.MountType<BMPT72>();

            if (!mounted)
            {
                enginePulseTimer = 0;
                engineWasAccelerating = false;
                return;
            }

            bool accelerating = Player.controlLeft || Player.controlRight;
            if (accelerating && !engineWasAccelerating)
                enginePulseTimer = 0;
            engineWasAccelerating = accelerating;

            if (enginePulseTimer > 0)
            {
                enginePulseTimer--;
                return;
            }

            float speedRatio = MathHelper.Clamp(
                Math.Abs(Player.velocity.X) / BMPT72.MaximumSpeed,
                0f,
                1f);
            float engineVolume = accelerating
                ? MathHelper.Lerp(0.30f, 0.80f, speedRatio)
                : 0.15f;

            SoundEngine.PlaySound(
                PistonEnginePulseSound with { Volume = engineVolume },
                Player.Center);
            enginePulseTimer = accelerating ? 11 : 17;
        }

        private void TryFireRocket()
        {
            // 交给原版火箭发射器选弹管线决定火箭种类并消耗弹药。
            // 手工读取ammo.shoot会绕过原版对各种火箭的射弹转换逻辑。
            Item rocketWeapon = new();
            rocketWeapon.SetDefaults(ItemID.RocketLauncher);
            rocketWeapon.damage = 300;
            rocketWeapon.knockBack = 6f;
            rocketWeapon.shootSpeed = 45f;
            rocketWeapon.DamageType = DamageClass.Ranged;

            if (!Player.PickAmmo(
                    rocketWeapon,
                    out int projectileType,
                    out _,
                    out int damage,
                    out float knockback,
                    out _,
                    false))
            {
                return;
            }

            Vector2 pivotWorld = BMPT72.GetRocketWeaponPivotWorld(Player);
            Vector2 aimDirection = Main.MouseWorld - pivotWorld;
            if (aimDirection.LengthSquared() < 0.001f)
                aimDirection = Vector2.UnitX * Player.direction;
            aimDirection.Normalize();

            Vector2 muzzleWorld = pivotWorld +
                aimDirection * BMPT72.RocketWeaponLength;
            int projectileIndex = Projectile.NewProjectile(
                Player.GetSource_Misc("BMPT72RocketLauncher"),
                muzzleWorld,
                aimDirection * 45f,
                projectileType,
                damage,
                knockback,
                Player.whoAmI);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Projectile projectile = Main.projectile[projectileIndex];
                // 45px/tick足以整帧跨过小目标。每帧拆成3次15px移动，
                // 同时等比例延长timeLeft，保持实际弹速、射程和寿命不变。
                projectile.extraUpdates = 2;
                projectile.velocity /= 3f;
                projectile.timeLeft *= 3;
                projectile.CritChance = (int)Player.GetTotalCritChance(DamageClass.Ranged);
                projectile.GetGlobalProjectile<BMPT72GuidedRocket>()
                    .EnableMouseGuidance();
                projectile.netUpdate = true;
            }

            // 庆典Mk2的发射声由原版专用声音路径提供，不能依赖物品
            // UseSound字段判空；Item156即庆典武器的使用音效。
            SoundEngine.PlaySound(
                SoundID.Item156 with { Pitch = -0.25f },
                muzzleWorld);
            rocketShotsInBurst++;
            if (rocketShotsInBurst >= 4)
            {
                rocketShotsInBurst = 0;
                rocketCooldown = 3 * 60;
            }
            else
            {
                rocketCooldown = 20;
            }
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<BMPT72>() ||
                CrewCount <= 0)
                return;

            float crewSpeedMultiplier = 1f + CrewCount * 0.05f;
            Player.maxRunSpeed *= crewSpeedMultiplier;
            Player.accRunSpeed *= crewSpeedMultiplier;

            float speed = Math.Abs(Player.velocity.X);
            if (speed <= BMPT72.ThirtyMphSpeed)
                return;

            float progress = MathHelper.Clamp(
                (speed - BMPT72.ThirtyMphSpeed) /
                (BMPT72.MaximumSpeed - BMPT72.ThirtyMphSpeed),
                0f,
                1f);
            // 30 mph以上立刻进入重型车辆的高档阻力区，并随着速度
            // 接近40 mph继续显著衰减，形成明显的渐近式提速过程。
            Player.runAcceleration *= MathHelper.Lerp(0.35f, 0.05f, progress);
        }

        public override void TransformDrawData(ref PlayerDrawSet drawInfo)
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<BMPT72>() ||
                Player.balloon <= 0 ||
                Player.balloon >= TextureAssets.AccBalloon.Length)
            {
                return;
            }

            // 大型碰撞箱只会令原版BalloonAcc使用的横向左上角基准偏移；
            // 它的纵向定位原本正确。直接修改已经生成的气球DrawData，
            // 不再临时移动整个PlayerDrawSet，避免驾驶员及后续图层受影响。
            var balloonTexture = TextureAssets.AccBalloon[Player.balloon].Value;
            float horizontalCorrection =
                (Player.width - DefaultPlayerWidth) * 0.5f * Player.direction;

            for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
            {
                DrawData data = drawInfo.DrawDataCache[i];
                if (!ReferenceEquals(data.texture, balloonTexture))
                    continue;

                data.position.X += horizontalCorrection;
                drawInfo.DrawDataCache[i] = data;
            }
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<BMPT72>() ||
                Math.Abs(Player.velocity.X) <= 0.1f)
            {
                return;
            }

            // 与合金二号坦克相同：移动时只隐藏驾驶员自身的身体、装备、
            // 时装和直接穿戴饰品，保留Buff、召唤物及其他独立模组特效。
            PlayerDrawLayer[] driverAppearanceLayers =
            {
                PlayerDrawLayers.JimsCloak,
                PlayerDrawLayers.Carpet,
                PlayerDrawLayers.PortableStool,
                PlayerDrawLayers.Tails,
                PlayerDrawLayers.Wings,
                PlayerDrawLayers.HairBack,
                PlayerDrawLayers.BackAcc,
                PlayerDrawLayers.HeadBack,
                PlayerDrawLayers.BalloonAcc,
                PlayerDrawLayers.Skin,
                PlayerDrawLayers.Leggings,
                PlayerDrawLayers.Shoes,
                PlayerDrawLayers.Robe,
                PlayerDrawLayers.SkinLongCoat,
                PlayerDrawLayers.ArmorLongCoat,
                PlayerDrawLayers.Torso,
                PlayerDrawLayers.OffhandAcc,
                PlayerDrawLayers.WaistAcc,
                PlayerDrawLayers.NeckAcc,
                PlayerDrawLayers.Head,
                PlayerDrawLayers.FaceAcc,
                PlayerDrawLayers.FrontAccBack,
                PlayerDrawLayers.Shield,
                PlayerDrawLayers.ArmOverItem,
                PlayerDrawLayers.HandOnAcc,
                PlayerDrawLayers.FrontAccFront
            };

            foreach (PlayerDrawLayer layer in driverAppearanceLayers)
                layer.Hide();
        }

    }
}
