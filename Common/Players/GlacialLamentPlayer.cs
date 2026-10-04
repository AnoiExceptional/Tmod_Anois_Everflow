using Terraria.ModLoader;

namespace everflow.Common.Players
{
    /// <summary>为每名玩家分别记录悲鸣冰川的射击次数。</summary>
    public sealed class GlacialLamentPlayer : ModPlayer
    {
        private int shotCounter;

        public bool AdvanceShotCounter()
        {
            shotCounter++;
            if (shotCounter < 6)
                return false;

            shotCounter = 0;
            return true;
        }
    }
}
