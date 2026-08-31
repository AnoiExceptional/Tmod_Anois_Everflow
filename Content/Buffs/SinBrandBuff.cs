using everflow.Content.Players;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class SinBrandBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.pvpBuff[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            Main.LocalPlayer.GetModPlayer<SinBrandPlayer>()
                .GetParadiseLostBonuses(out int defenseBonus, out int damagePercent);
            tip += "\n" + Language.GetTextValue(
                "Mods.everflow.Buffs.SinBrandBuff.CurrentBonus",
                defenseBonus,
                damagePercent);
        }
    }
}
