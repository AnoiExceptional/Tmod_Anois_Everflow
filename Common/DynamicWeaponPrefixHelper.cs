using System;
using System.Collections.Generic;
using Terraria;

namespace everflow.Common
{
    /// <summary>
    /// Reapplies vanilla prefix multipliers after a progression weapon replaces
    /// its base stats. Prefixes are normally applied only once after SetDefaults;
    /// writing stage stats later would otherwise erase most of their effects.
    /// </summary>
    internal static class DynamicWeaponPrefixHelper
    {
        private const int ProbeValue = 1000;

        private readonly record struct CacheKey(int ItemType, int Prefix);

        private readonly record struct Multipliers(
            float Damage,
            float Knockback,
            float UseSpeed,
            float ShootSpeed,
            float ManaCost,
            int CritBonus);

        private static readonly Dictionary<CacheKey, Multipliers> Cache = new();

        public static void Apply(
            Item item,
            int baseDamage,
            float baseKnockback,
            int baseUseTime,
            int baseMana,
            float baseShootSpeed)
        {
            Apply(item, baseDamage, baseKnockback, baseUseTime, baseMana,
                baseShootSpeed, null);
        }

        public static void Apply(
            Item item,
            int baseDamage,
            float baseKnockback,
            int baseUseTime,
            int baseMana,
            float baseShootSpeed,
            int? baseCrit)
        {
            item.damage = baseDamage;
            item.knockBack = baseKnockback;
            item.useTime = baseUseTime;
            item.useAnimation = baseUseTime;
            item.mana = baseMana;
            item.shootSpeed = baseShootSpeed;
            if (baseCrit.HasValue)
                item.crit = baseCrit.Value;

            if (item.prefix <= 0)
                return;

            Multipliers multipliers = GetMultipliers(item.type, item.prefix);
            item.damage = Math.Max(1, (int)Math.Round(baseDamage * multipliers.Damage));
            item.knockBack = baseKnockback * multipliers.Knockback;
            item.useTime = Math.Max(1, (int)Math.Round(baseUseTime * multipliers.UseSpeed));
            item.useAnimation = item.useTime;
            item.mana = baseMana <= 0
                ? 0
                : Math.Max(1, (int)Math.Round(baseMana * multipliers.ManaCost));
            item.shootSpeed = baseShootSpeed * multipliers.ShootSpeed;
            if (baseCrit.HasValue)
                item.crit = baseCrit.Value + multipliers.CritBonus;
        }

        private static Multipliers GetMultipliers(int itemType, int prefix)
        {
            CacheKey key = new(itemType, prefix);
            if (Cache.TryGetValue(key, out Multipliers cached))
                return cached;

            Item probe = new();
            probe.SetDefaults(itemType);

            // Large neutral values make every percentage change survive integer
            // rounding, allowing the vanilla prefix implementation to reveal its
            // exact multipliers without duplicating Terraria's prefix table.
            probe.damage = ProbeValue;
            probe.knockBack = ProbeValue;
            probe.useTime = ProbeValue;
            probe.useAnimation = ProbeValue;
            probe.mana = ProbeValue;
            probe.shootSpeed = ProbeValue;
            probe.crit = 0;
            probe.prefix = 0;

            if (!probe.Prefix(prefix))
            {
                cached = new Multipliers(1f, 1f, 1f, 1f, 1f, 0);
            }
            else
            {
                cached = new Multipliers(
                    probe.damage / (float)ProbeValue,
                    probe.knockBack / ProbeValue,
                    probe.useAnimation / (float)ProbeValue,
                    probe.shootSpeed / ProbeValue,
                    probe.mana / (float)ProbeValue,
                    probe.crit);
            }

            Cache[key] = cached;
            return cached;
        }
    }
}
