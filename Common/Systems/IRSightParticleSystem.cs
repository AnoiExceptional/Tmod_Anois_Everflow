using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Systems
{
    [Autoload(Side = ModSide.Client)]
    public sealed class IRSightParticleSystem : ModSystem
    {
        private readonly struct DustState
        {
            public readonly int Type;
            public readonly bool NoLight;
            public readonly bool NoLightEmittence;
            public readonly Color Color;

            public DustState(Dust dust)
            {
                Type = dust.type;
                NoLight = dust.noLight;
                NoLightEmittence = dust.noLightEmittence;
                Color = dust.color;
            }
        }

        private static readonly HashSet<int> LavaDust = new();
        private static readonly HashSet<int> FireDust = new();
        private static readonly HashSet<int> ShimmerDust = new();
        private static readonly Dictionary<int, DustState> ModifiedDust = new();

        public override void Load()
        {
            foreach (FieldInfo field in typeof(DustID).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.FieldType != typeof(short))
                    continue;

                int type = Convert.ToInt32(field.GetRawConstantValue());
                string name = field.Name;
                if (Contains(name, "Shimmer"))
                    ShimmerDust.Add(type);
                else if (Contains(name, "Lava"))
                    LavaDust.Add(type);
                else if (Contains(name, "Torch") || Contains(name, "Fire") ||
                         Contains(name, "Flame") || Contains(name, "Ember") ||
                         Contains(name, "Smoke"))
                    FireDust.Add(type);
            }
        }

        public override void Unload()
        {
            RestoreModifiedDust();
            LavaDust.Clear();
            FireDust.Clear();
            ShimmerDust.Clear();
        }

        public override void PreUpdateDusts() => ProcessDusts();

        public override void PostUpdateDusts() => ProcessDusts();

        private static void ProcessDusts()
        {
            if (!InfraredIsActive())
            {
                RestoreModifiedDust();
                return;
            }

            for (int i = 0; i < Main.dust.Length; i++)
            {
                Dust dust = Main.dust[i];
                if (!dust.active)
                {
                    ModifiedDust.Remove(i);
                    continue;
                }

                bool lava = LavaDust.Contains(dust.type);
                bool fire = FireDust.Contains(dust.type);
                bool shimmer = ShimmerDust.Contains(dust.type);
                // 所有粒子先统一禁止环境照明；岩浆粒子在下方唯一例外。
                // 未分类粒子的外观不变，因此金属锭闪光仍然可见。
                CaptureOrRefreshState(i, dust);
                dust.noLightEmittence = true;

                if (shimmer)
                {
                    // 微光粒子按普通水花处理：不自亮，也不发光。
                    dust.noLight = false;
                    dust.color = new Color(42, 42, 42, dust.color.A);
                }
                else if (lava)
                {
                    // 岩浆粒子既是强白热目标，也确实照亮环境。
                    dust.noLight = true;
                    dust.noLightEmittence = false;
                    dust.color = Color.White;
                    float strength = 1.25f * MathHelper.Clamp(dust.scale, 0.5f, 1.35f);
                    Lighting.AddLight(dust.position, strength, strength, strength);
                }
                else if (fire)
                {
                    // 火焰粒子只显示自身热量，禁止任何环境照明。
                    dust.noLight = true;
                    dust.noLightEmittence = true;
                    dust.color = new Color(220, 220, 220, 255);
                }
            }
        }

        private static void CaptureOrRefreshState(int index, Dust dust)
        {
            if (ModifiedDust.TryGetValue(index, out DustState state) && state.Type == dust.type)
                return;

            ModifiedDust[index] = new DustState(dust);
        }

        private static void RestoreModifiedDust()
        {
            foreach (KeyValuePair<int, DustState> pair in ModifiedDust)
            {
                Dust dust = Main.dust[pair.Key];
                DustState state = pair.Value;
                if (!dust.active || dust.type != state.Type)
                    continue;

                dust.noLight = state.NoLight;
                dust.noLightEmittence = state.NoLightEmittence;
                dust.color = state.Color;
            }

            ModifiedDust.Clear();
        }

        private static bool Contains(string value, string fragment) =>
            value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool InfraredIsActive() =>
            !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());
    }
}
