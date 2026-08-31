using System;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Mounts;
using everflow.Common.DrawLayers;

namespace everflow.Common.Players
{
    public class AlloyTank02Player : ModPlayer
    {
        public int CrewCount { get; set; }
        public int HullDirection { get; set; } = 1;
        private int cannonCooldown;
        private int enginePulseTimer;
        private bool engineWasAccelerating;

        // Reserved for a later high-RPM gas-turbine vehicle.
        private static readonly SoundStyle GasTurbineEngineLoopSound =
            SoundID.DD2_EtherianPortalIdleLoop with
            {
                Volume = 0.70f,
                Pitch = -0.35f,
                IsLooped = true
            };

        // A lowered drill pulse gives this early tank a slow piston/tractor cadence.
        private static readonly SoundStyle PistonEnginePulseSound = SoundID.Item22 with
        {
            Volume = 1f,
            Pitch = -1f,
            PitchVariance = 0f,
            // Pitch -1 makes each drill sample substantially longer. Keep
            // enough overlapping instances so the fixed 12-frame cadence is
            // not suppressed after every third pulse.
            MaxInstances = 16
        };

        public override void PostUpdate()
        {
            UpdateEngineSound();

            if (cannonCooldown > 0)
                cannonCooldown--;

            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<AlloyTank02>())
            {
                cannonCooldown = 0;
                return;
            }

            // Hull facing belongs to the movement controls, independently of
            // the mouse-facing turret and driver.
            if (Player.controlLeft && !Player.controlRight)
                HullDirection = -1;
            else if (Player.controlRight && !Player.controlLeft)
                HullDirection = 1;

            // The local mouse decides the turret side. Player.direction is
            // synchronized by Terraria, so the driver and turret share one
            // mirror state and remain fixed relative to one another.
            if (Player.whoAmI == Main.myPlayer)
            {
                float aimX = Main.MouseWorld.X - Player.Center.X;
                if (Math.Abs(aimX) > 0.01f)
                    Player.ChangeDir(aimX >= 0f ? 1 : -1);
            }

            // 只由本机玩家读取鼠标并生成射弹；Projectile.NewProjectile 会负责同步。
            if (Player.whoAmI != Main.myPlayer ||
                !Player.controlUseItem ||
                cannonCooldown > 0 ||
                Main.blockMouse ||
                Player.mouseInterface)
            {
                return;
            }

            Vector2 pivotWorld = AlloyTank02.GetWeaponPivotWorld(Player);
            Vector2 aimDirection = Main.MouseWorld - pivotWorld;
            if (aimDirection.LengthSquared() < 0.001f)
                aimDirection = Vector2.UnitX * Player.direction;
            aimDirection.Normalize();

            Vector2 muzzleWorld = pivotWorld +
                aimDirection * AlloyTank02.WeaponLength;
            const float bulletSpeed = 16f;
            int damage = (int)Player.GetTotalDamage(DamageClass.Ranged).ApplyTo(10f);
            float knockback = Player.GetTotalKnockback(DamageClass.Ranged).ApplyTo(3f);

            int projectileIndex = Projectile.NewProjectile(
                Player.GetSource_Misc("AlloyTank02Cannon"),
                muzzleWorld,
                aimDirection * bulletSpeed,
                ProjectileID.Bullet,
                damage,
                knockback,
                Player.whoAmI);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Main.projectile[projectileIndex].scale = 1.5f;
                Main.projectile[projectileIndex].netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.Item36, muzzleWorld);
            cannonCooldown = 5;
        }

        private void UpdateEngineSound()
        {
            if (Main.dedServ)
                return;

            bool mounted = Player.active && !Player.dead &&
                Player.mount.Active &&
                Player.mount.Type == ModContent.MountType<AlloyTank02>();

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

            float crewSpeedMultiplier = 1f + CrewCount * 0.05f;
            float maximumSpeed = 4.9f * crewSpeedMultiplier;
            float speedRatio = MathHelper.Clamp(
                Math.Abs(Player.velocity.X) / maximumSpeed,
                0f,
                1f);
            float engineVolume = accelerating
                ? MathHelper.Lerp(0.30f, 0.80f, speedRatio)
                : 0.15f;

            SoundEngine.PlaySound(
                PistonEnginePulseSound with { Volume = engineVolume },
                Player.Center);
            // 12 frames while accelerating; idle cadence is 50% slower at
            // exactly 18 frames. Pressing A/D resets the timer above, so the
            // normal cadence begins immediately.
            enginePulseTimer = accelerating ? 11 : 17;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<AlloyTank02>() ||
                CrewCount <= 0)
            {
                return;
            }

            float speedMultiplier = 1f + CrewCount * 0.05f;
            Player.maxRunSpeed *= speedMultiplier;
            Player.accRunSpeed *= speedMultiplier;
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<AlloyTank02>())
            {
                return;
            }

            // playerYOffsets 只移动人物的视觉位置，原版仍会从人物实际碰撞位置取光。
            // 将人物颜色从旧采光位置校正到车顶上的实际显示位置；不修改 colorMount。
            Vector2 oldLightPosition = Player.Center;
            Vector2 displayedPlayerPosition = oldLightPosition -
                new Vector2(0f, Player.mount.PlayerOffset);
            Color oldLight = Lighting.GetColor(oldLightPosition.ToTileCoordinates());
            Color displayedLight = Lighting.GetColor(displayedPlayerPosition.ToTileCoordinates());

            drawInfo.colorArmorHead = Relight(drawInfo.colorArmorHead, oldLight, displayedLight);
            drawInfo.colorArmorBody = Relight(drawInfo.colorArmorBody, oldLight, displayedLight);
            drawInfo.colorArmorLegs = Relight(drawInfo.colorArmorLegs, oldLight, displayedLight);
            drawInfo.colorBodySkin = Relight(drawInfo.colorBodySkin, oldLight, displayedLight);
            drawInfo.colorDisplayDollSkin = Relight(drawInfo.colorDisplayDollSkin, oldLight, displayedLight);
            drawInfo.colorEyes = Relight(drawInfo.colorEyes, oldLight, displayedLight);
            drawInfo.colorEyeWhites = Relight(drawInfo.colorEyeWhites, oldLight, displayedLight);
            drawInfo.colorHair = Relight(drawInfo.colorHair, oldLight, displayedLight);
            drawInfo.colorHead = Relight(drawInfo.colorHead, oldLight, displayedLight);
            drawInfo.colorLegs = Relight(drawInfo.colorLegs, oldLight, displayedLight);
            drawInfo.colorPants = Relight(drawInfo.colorPants, oldLight, displayedLight);
            drawInfo.colorShirt = Relight(drawInfo.colorShirt, oldLight, displayedLight);
            drawInfo.colorShoes = Relight(drawInfo.colorShoes, oldLight, displayedLight);
            drawInfo.colorUnderShirt = Relight(drawInfo.colorUnderShirt, oldLight, displayedLight);
        }

        private static Color Relight(Color color, Color oldLight, Color newLight)
        {
            static byte Correct(byte value, byte oldChannel, byte newChannel)
            {
                if (oldChannel == 0)
                    return value;

                return (byte)Math.Clamp(
                    (int)Math.Round(value * newChannel / (float)oldChannel),
                    0,
                    255);
            }

            return new Color(
                Correct(color.R, oldLight.R, newLight.R),
                Correct(color.G, oldLight.G, newLight.G),
                Correct(color.B, oldLight.B, newLight.B),
                color.A);
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (!Player.mount.Active ||
                Player.mount.Type != ModContent.MountType<AlloyTank02>())
            {
                return;
            }

            if (Math.Abs(Player.velocity.X) <= 0.1f)
                return;

            // 只隐藏驾驶员自身的身体、盔甲、时装与直接穿戴饰品。
            // 不再遍历并隐藏所有未知图层：Buff特效、召唤关联特效、独立模组特效
            // （如星尘守卫和坎水玉简阵列）均应默认保留。
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
