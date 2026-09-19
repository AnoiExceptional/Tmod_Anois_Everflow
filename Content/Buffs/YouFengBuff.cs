using everflow.Content.Players;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class YouFengBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void ModifyBuffText(
            ref string buffName,
            ref string tip,
            ref int rare)
        {
            float currentBonus = Main.LocalPlayer
                .GetModPlayer<YouFengArmorPlayer>()
                .CurrentDamageBonusPercent;
            tip += "\n" + Language.GetTextValue(
                "Mods.everflow.Buffs.YouFengBuff.CurrentBonus",
                currentBonus);
        }
    }
}
