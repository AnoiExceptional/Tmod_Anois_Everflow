using Terraria;
using Terraria.ModLoader;
using everflow.Content.Players;

namespace everflow.Content.Buffs
{
    public sealed class BurgerKingMealBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            string effects = Main.LocalPlayer
                .GetModPlayer<BurgerKingMealPlayer>()
                .GetActiveEffectsText();

            tip = string.IsNullOrEmpty(effects)
                ? "当前汉堡没有提供持续增益"
                : effects;
        }
    }

    public sealed class BurgerKingEatingCooldown : ModBuff
    {
        public override string Texture => "everflow/Content/Buffs/BurgerMealCD";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
