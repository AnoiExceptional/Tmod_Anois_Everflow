using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs;
using everflow.Content.Items;

namespace everflow.Content.Players
{
    public sealed class BurgerKingMealPlayer : ModPlayer
    {
        private static readonly int[] DamageOverTimeDebuffs =
        {
            BuffID.Poisoned, BuffID.Venom, BuffID.OnFire, BuffID.OnFire3,
            BuffID.CursedInferno, BuffID.Frostburn, BuffID.Frostburn2,
            BuffID.ShadowFlame, BuffID.Daybreak, BuffID.Electrified,
            BuffID.Bleeding
        };

        private readonly int[] activeFillings = new int[BurgerKing.MaximumFillingLayers];
        private int activeFillingCount;
        private float lifeStealRemainder;

        public void ConsumeBurger(int[] fillings, int duration)
        {
            Array.Clear(activeFillings);
            activeFillingCount = Math.Min(fillings?.Length ?? 0, activeFillings.Length);
            int condimentCount = 0;
            for (int i = 0; i < activeFillingCount; i++)
            {
                activeFillings[i] = fillings[i];

                if (BurgerKingFillingCatalog.TryGet(activeFillings[i], out BurgerKingFillingDefinition filling) &&
                    (filling.Categories & BurgerKingFillingCategory.Condiment) != 0)
                {
                    condimentCount++;
                }
            }

            // 右键食用的馅料即时效果发生在本帧ResetEffects之后。
            // 必须先建立组合免疫，才能阻止同一次食用加入的带电等持续掉血Debuff。
            if (condimentCount >= 3)
            {
                ApplyDamageOverTimeImmunities();
                ClearDamageOverTimeDebuffs();
            }

            // 单个馅料提供的减益免疫也应像调味料组合一样，在食用当帧
            // 立刻终结已有状态，并先建立免疫以防环境于同帧重新施加。
            for (int i = 0; i < activeFillingCount; i++)
                ApplyImmediateFillingDebuffProtection(activeFillings[i]);

            ApplyImmediateEffects(duration);
        }

        public string GetActiveEffectsText()
        {
            if (!Player.HasBuff(ModContent.BuffType<BurgerKingMealBuff>()))
                return string.Empty;

            List<string> effects = new();
            int classic = 0;
            int condiment = 0;
            int vegetarian = 0;
            int carnivore = 0;
            int darkCuisine = 0;

            for (int i = 0; i < activeFillingCount; i++)
            {
                if (!BurgerKingFillingCatalog.TryGet(activeFillings[i], out BurgerKingFillingDefinition filling))
                    continue;

                CountCategories(filling.Categories, ref classic, ref condiment, ref vegetarian, ref carnivore, ref darkCuisine);
                string effect = GetPersistentEffectText(filling.FrameIndex);
                if (!string.IsNullOrEmpty(effect))
                    effects.Add($"{filling.ChineseName}：{effect}");
            }

            if (classic >= 3) effects.Add("经典组合：所有属性大幅提高");
            if (condiment >= 3) effects.Add("调味料组合：免疫所有持续掉血Debuff");
            if (vegetarian >= 3) effects.Add("素食主义组合：攻击速度提高20%");
            if (carnivore >= 3) effects.Add("肉食主义组合：暴击率提高20%");
            if (darkCuisine >= 3) effects.Add("黑暗料理组合：获得1%吸血");

            return string.Join("\n", effects);
        }

        private static string GetPersistentEffectText(int frameIndex) => frameIndex switch
        {
            3 => "防御力+8",
            4 => "免疫中毒与石化",
            5 => "移动速度提高10%",
            6 => "持续回复生命值",
            7 => "本次盛宴持续时间提高50%",
            8 => "所有属性小幅提高",
            10 => "获得一个随机增益",
            11 => "免疫减速与混乱",
            12 => "免疫冰冻与寒冷",
            13 => "免疫黑暗与诅咒",
            14 => "所有属性中幅提高",
            15 => "攻击速度提高15%",
            16 => "免疫岩浆与燃烧",
            17 => "获得水下呼吸",
            18 => "获得带电与灵液",
            19 => "移动速度提高50%",
            _ => string.Empty
        };

        public override void ResetEffects()
        {
            if (!Player.HasBuff(ModContent.BuffType<BurgerKingMealBuff>()))
            {
                activeFillingCount = 0;
                lifeStealRemainder = 0f;
                return;
            }

            int classic = 0;
            int condiment = 0;
            int vegetarian = 0;
            int carnivore = 0;
            int darkCuisine = 0;

            for (int i = 0; i < activeFillingCount; i++)
            {
                if (!BurgerKingFillingCatalog.TryGet(activeFillings[i], out BurgerKingFillingDefinition filling))
                    continue;

                CountCategories(filling.Categories, ref classic, ref condiment, ref vegetarian, ref carnivore, ref darkCuisine);
                ApplyFillingEffect(filling.FrameIndex);
            }

            if (classic >= 3)
                ApplyAllStats(3);
            if (condiment >= 3)
                ApplyDamageOverTimeImmunities();
            if (vegetarian >= 3)
                Player.GetAttackSpeed(DamageClass.Generic) += 0.20f;
            if (carnivore >= 3)
                Player.GetCritChance(DamageClass.Generic) += 20f;
        }

        private void ApplyImmediateEffects(int duration)
        {
            int healing = 0;
            for (int i = 0; i < activeFillingCount; i++)
            {
                switch (activeFillings[i])
                {
                    case 2: healing += 15; break;
                    case 9: healing += 50; break;
                    case 10: AddRandomPositiveBuff(duration); break;
                    case 18:
                        Player.AddBuff(BuffID.Electrified, duration);
                        // AddBuff会按难度延长灵液，且已有灵液只取更长时间、不会缩短。
                        // 添加完成后直接校正实际Buff槽，保证所有难度与重复食用均为10秒。
                        Player.AddBuff(BuffID.Ichor, 600);
                        int ichorIndex = Player.FindBuffIndex(BuffID.Ichor);
                        if (ichorIndex >= 0)
                            Player.buffTime[ichorIndex] = 600;
                        break;
                    case 20: healing += 100; break;
                }
            }

            if (healing > 0 && Player.statLife < Player.statLifeMax2)
            {
                int healed = Math.Min(healing, Player.statLifeMax2 - Player.statLife);
                Player.statLife += healed;
                Player.HealEffect(healed, true);
            }
        }

        private void ApplyFillingEffect(int frameIndex)
        {
            switch (frameIndex)
            {
                case 3: Player.statDefense += 8; break;
                case 4:
                    Player.buffImmune[BuffID.Poisoned] = true;
                    Player.buffImmune[BuffID.Stoned] = true;
                    break;
                case 5: Player.moveSpeed += 0.10f; break;
                case 6: Player.lifeRegen += 4; break;
                case 8: ApplyAllStats(1); break;
                case 11:
                    Player.buffImmune[BuffID.Slow] = true;
                    Player.buffImmune[BuffID.Confused] = true;
                    break;
                case 12:
                    Player.buffImmune[BuffID.Frozen] = true;
                    Player.buffImmune[BuffID.Chilled] = true;
                    break;
                case 13:
                    Player.buffImmune[BuffID.Darkness] = true;
                    Player.buffImmune[BuffID.Cursed] = true;
                    break;
                case 14: ApplyAllStats(2); break;
                case 15: Player.GetAttackSpeed(DamageClass.Generic) += 0.15f; break;
                case 16:
                    Player.lavaImmune = true;
                    Player.fireWalk = true;
                    Player.buffImmune[BuffID.OnFire] = true;
                    Player.buffImmune[BuffID.Burning] = true;
                    break;
                case 17: Player.gills = true; break;
                case 19: Player.moveSpeed += 0.50f; break;
            }
        }

        private void ApplyAllStats(int tier)
        {
            float damage = tier switch { 1 => 0.05f, 2 => 0.075f, _ => 0.10f };
            float speed = tier switch { 1 => 0.05f, 2 => 0.075f, _ => 0.10f };
            float movement = tier switch { 1 => 0.20f, 2 => 0.30f, _ => 0.40f };
            int defense = tier switch { 1 => 2, 2 => 3, _ => 4 };
            float crit = tier switch { 1 => 2f, 2 => 3f, _ => 4f };

            Player.GetDamage(DamageClass.Generic) += damage;
            Player.GetAttackSpeed(DamageClass.Generic) += speed;
            Player.GetCritChance(DamageClass.Generic) += crit;
            Player.GetKnockback(DamageClass.Summon) += tier * 0.20f;
            Player.moveSpeed += movement;
            Player.statDefense += defense;
        }

        private void ApplyDamageOverTimeImmunities()
        {
            foreach (int debuff in DamageOverTimeDebuffs)
                Player.buffImmune[debuff] = true;
        }

        private void ClearDamageOverTimeDebuffs()
        {
            foreach (int debuff in DamageOverTimeDebuffs)
                Player.ClearBuff(debuff);
        }

        private void ApplyImmediateFillingDebuffProtection(int frameIndex)
        {
            switch (frameIndex)
            {
                case 4:
                    SetImmunityAndClear(BuffID.Poisoned, BuffID.Stoned);
                    break;
                case 11:
                    SetImmunityAndClear(BuffID.Slow, BuffID.Confused);
                    break;
                case 12:
                    SetImmunityAndClear(BuffID.Frozen, BuffID.Chilled);
                    break;
                case 13:
                    SetImmunityAndClear(BuffID.Darkness, BuffID.Cursed);
                    break;
                case 16:
                    SetImmunityAndClear(BuffID.OnFire, BuffID.Burning);
                    break;
            }
        }

        private void SetImmunityAndClear(params int[] debuffs)
        {
            foreach (int debuff in debuffs)
            {
                Player.buffImmune[debuff] = true;
                Player.ClearBuff(debuff);
            }
        }

        private bool HasDarkCuisineSet()
        {
            int count = 0;
            for (int i = 0; i < activeFillingCount; i++)
            {
                if (BurgerKingFillingCatalog.TryGet(activeFillings[i], out BurgerKingFillingDefinition filling) &&
                    (filling.Categories & BurgerKingFillingCategory.DarkCuisine) != 0)
                    count++;
            }
            return count >= 3;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) => TryLifeSteal(damageDone);

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner == Player.whoAmI)
                TryLifeSteal(damageDone);
        }

        private void TryLifeSteal(int damageDone)
        {
            if (!Player.HasBuff(ModContent.BuffType<BurgerKingMealBuff>()) || !HasDarkCuisineSet() || damageDone <= 0)
                return;

            lifeStealRemainder += damageDone * 0.01f;
            int healing = (int)lifeStealRemainder;
            if (healing <= 0 || Player.statLife >= Player.statLifeMax2)
                return;

            lifeStealRemainder -= healing;
            healing = Math.Min(healing, Player.statLifeMax2 - Player.statLife);
            Player.statLife += healing;
            Player.HealEffect(healing, true);
        }

        private void AddRandomPositiveBuff(int duration)
        {
            int[] choices =
            {
                BuffID.Ironskin, BuffID.Regeneration, BuffID.Swiftness,
                BuffID.Endurance, BuffID.Wrath, BuffID.Rage,
                BuffID.Lifeforce, BuffID.MagicPower, BuffID.Summoning
            };
            Player.AddBuff(choices[Main.rand.Next(choices.Length)], duration);
        }

        private static void CountCategories(BurgerKingFillingCategory categories, ref int classic, ref int condiment, ref int vegetarian, ref int carnivore, ref int darkCuisine)
        {
            if ((categories & BurgerKingFillingCategory.Classic) != 0) classic++;
            if ((categories & BurgerKingFillingCategory.Condiment) != 0) condiment++;
            if ((categories & BurgerKingFillingCategory.Vegetarian) != 0) vegetarian++;
            if ((categories & BurgerKingFillingCategory.Carnivore) != 0) carnivore++;
            if ((categories & BurgerKingFillingCategory.DarkCuisine) != 0) darkCuisine++;
        }
    }
}
