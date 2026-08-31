using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.Players
{
    public class AlloyPlayer : ModPlayer
    {
        public bool alloyCrownSetBonus;

        public override void ResetEffects()
        {
            alloyCrownSetBonus = false;
        }

        public override void UpdateLifeRegen()
        {
            if (alloyCrownSetBonus)
            {
                // +2 lifeRegen = +1 HP / 秒
                // 与心灯的生命恢复量一致
                Player.lifeRegen += 2;
            }
        }
    }
}