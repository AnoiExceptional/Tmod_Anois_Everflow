using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Projectiles
{
    public sealed class IRSightGlobalProjectile : GlobalProjectile
    {
        private static readonly HashSet<int> BallisticHeatProjectiles = new();
        private bool lightSuppressed;
        private float originalLight;

        public override bool InstancePerEntity => true;

        public override void Load()
        {
            // 原版没有完整的“所有子弹/火箭”集合；直接从ProjectileID的官方
            // 名称建立集合，可覆盖友方、敌方、不同弹药和庆典火箭等变体。
            foreach (FieldInfo field in typeof(ProjectileID).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.FieldType != typeof(short))
                    continue;

                string name = field.Name;
                if (Contains(name, "Bullet") || Contains(name, "Rocket") || Contains(name, "Missile"))
                    BallisticHeatProjectiles.Add(Convert.ToInt32(field.GetRawConstantValue()));
            }
        }

        public override void Unload() => BallisticHeatProjectiles.Clear();

        public override bool PreAI(Projectile projectile)
        {
            if (InfraredIsActive())
            {
                if (!lightSuppressed)
                {
                    originalLight = projectile.light;
                    lightSuppressed = true;
                }

                // 荧光棒及其他射弹自带的冷光不进入环境光照。
                projectile.light = 0f;
            }
            else if (lightSuppressed)
            {
                projectile.light = originalLight;
                lightSuppressed = false;
            }

            return true;
        }

        public override void PostAI(Projectile projectile)
        {
            // 部分原版AI会在更新过程中重新赋值light，更新后再次清零。
            if (InfraredIsActive())
                projectile.light = 0f;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!InfraredIsActive() || !IsBulletOrRocket(projectile))
                return true;

            // 中热源：只提高射弹自身的绘制亮度，本系统不向Lighting添加光。
            lightColor = new Color(168, 168, 168, lightColor.A);
            return true;
        }

        private static bool IsBulletOrRocket(Projectile projectile)
        {
            if (BallisticHeatProjectiles.Contains(projectile.type))
                return true;

            // 同时覆盖名称规范的模组射弹，例如BMPT72AutocannonBullet和
            // BMPT72GuidedRocket，不需要为每件武器写专用判断。
            string modName = projectile.ModProjectile?.Name;
            return !string.IsNullOrEmpty(modName) &&
                (Contains(modName, "Bullet") || Contains(modName, "Rocket") || Contains(modName, "Missile"));
        }

        private static bool Contains(string value, string fragment) =>
            value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool InfraredIsActive() =>
            !Main.dedServ && !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());
    }
}
