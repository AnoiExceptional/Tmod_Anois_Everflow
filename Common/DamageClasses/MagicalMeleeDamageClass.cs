using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.DamageClasses
{
    /// <summary>
    /// 通用的魔法近战伤害类型：完整继承近战与魔法两系的伤害、
    /// 暴击、攻速、护甲穿透和击退加成。
    /// </summary>
    public sealed class MagicalMeleeDamageClass : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(
            DamageClass damageClass)
        {
            if (damageClass == DamageClass.Generic)
                return StatInheritanceData.Full;

            if (damageClass == DamageClass.Melee)
                return StatInheritanceData.Full;

            if (damageClass == DamageClass.Magic)
                return StatInheritanceData.Full;

            return StatInheritanceData.None;
        }

        public override bool GetEffectInheritance(DamageClass damageClass) =>
            damageClass == DamageClass.Melee || damageClass == DamageClass.Magic;

        public override bool GetPrefixInheritance(DamageClass damageClass) =>
            damageClass == DamageClass.Melee;
    }
}
