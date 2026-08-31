using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class SinBrandPlayer : ModPlayer
    {
        public bool Gluttony;
        public bool Pride;
        public bool Jealousy;
        public bool Lust;
        public bool Sloth;
        public bool ParadiseLost;
        public bool Wrath;

        private float lifeStealRemainder;

        public override void ResetEffects()
        {
            Gluttony = false;
            Pride = false;
            Jealousy = false;
            Lust = false;
            Sloth = false;
            ParadiseLost = false;
            Wrath = false;
        }

        public override void PostUpdateEquips()
        {
            if (!Jealousy && !Sloth && !ParadiseLost)
                return;

            Dictionary<int, int> summonTypeCounts = new();
            foreach (Projectile summon in Main.ActiveProjectiles)
            {
                if (summon.owner != Player.whoAmI || !IsSummonBody(summon))
                    continue;

                summonTypeCounts.TryGetValue(summon.type, out int count);
                summonTypeCounts[summon.type] = count + 1;
            }

            int bonusUnits = 0;
            if (Jealousy)
                bonusUnits += summonTypeCounts.Count;

            if (Sloth)
            {
                foreach (int count in summonTypeCounts.Values)
                {
                    // A type only counts as "the same" when at least two of
                    // that summon are active. Every member of that group adds 5%.
                    if (count >= 2)
                        bonusUnits += count;
                }
            }

            Player.GetDamage(DamageClass.Summon) += bonusUnits * 0.05f;

            if (ParadiseLost)
            {
                GetParadiseLostBonuses(out int defenseBonus, out int damagePercent);
                Player.statDefense += defenseBonus;
                Player.GetDamage(DamageClass.Summon) += damagePercent * 0.01f;
            }
        }

        public void GetParadiseLostBonuses(out int defenseBonus, out int damagePercent)
        {
            int nearbySummons = 0;
            int distantSummons = 0;
            foreach (Projectile summon in Main.ActiveProjectiles)
            {
                if (summon.owner != Player.whoAmI || !IsSummonBody(summon))
                    continue;

                if (Vector2.DistanceSquared(Player.Center, summon.Center) <= 320f * 320f)
                    nearbySummons++;
                else
                    distantSummons++;
            }

            defenseBonus = nearbySummons * 5;
            damagePercent = distantSummons * 5;
        }

        public override void OnHitNPCWithProj(
            Projectile proj,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (!Gluttony || proj.owner != Player.whoAmI ||
                !proj.DamageType.CountsAsClass(DamageClass.Summon) || damageDone <= 0)
                return;

            lifeStealRemainder += damageDone * 0.01f;
            int healing = Math.Min((int)lifeStealRemainder,
                Player.statLifeMax2 - Player.statLife);
            if (healing <= 0)
                return;

            lifeStealRemainder -= healing;
            Player.statLife += healing;
            Player.HealEffect(healing, true);
        }

        internal float GetSummonDamageMultiplier(Projectile projectile)
        {
            float bonus = 0f;
            float distanceInTiles = Vector2.Distance(Player.Center, projectile.Center) / 16f;

            if (Pride)
                bonus += MathHelper.Clamp(distanceInTiles / 30f, 0f, 1f) * 0.30f;
            if (Lust)
                bonus += (1f - MathHelper.Clamp(distanceInTiles / 30f, 0f, 1f)) * 0.30f;

            return 1f + bonus;
        }

        private static bool IsSummonBody(Projectile projectile) =>
            projectile.minion || projectile.sentry;
    }

    public sealed class SinBrandProjectile : GlobalProjectile
    {
        public override void ModifyHitNPC(
            Projectile projectile,
            NPC target,
            ref NPC.HitModifiers modifiers)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers ||
                !projectile.DamageType.CountsAsClass(DamageClass.Summon))
                return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active)
                return;

            SinBrandPlayer brandPlayer = owner.GetModPlayer<SinBrandPlayer>();
            modifiers.FinalDamage *= brandPlayer.GetSummonDamageMultiplier(projectile);

            // Vanilla continuously writes the owner's accumulated summon crit
            // chance into CritChance for minions and sentries, but Summon has
            // UseStandardCritCalcs disabled and therefore skips the final roll.
            // Restore that single roll against the accumulated projectile value
            // instead of making an independent fixed 25% Brand roll. This lets
            // the Brand stack additively with every other summon-crit source.
            bool vanillaCritDisabledSummon = projectile.minion || projectile.sentry ||
                ProjectileID.Sets.MinionShot[projectile.type] ||
                ProjectileID.Sets.SentryShot[projectile.type];
            int totalCritChance = Math.Clamp(projectile.CritChance, 0, 100);
            if (brandPlayer.Wrath && vanillaCritDisabledSummon &&
                Main.rand.Next(100) < totalCritChance)
                modifiers.SetCrit();
        }
    }
}
