using Terraria;
using Terraria.ModLoader;
using everflow.Common.Players;

namespace everflow.Content.Buffs
{
    public class AlloyConduct : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int slots = player.GetModPlayer<AlloyConductPlayer>().ReservedMinionSlots;
            if (slots <= 0)
                return;

            // 占用施法瞬间的空闲召唤栏，并按照每栏5%提高魔法伤害。
            player.maxMinions = System.Math.Max(0, player.maxMinions - slots);
            player.GetDamage(DamageClass.Magic) += slots * 0.05f;
        }

        public override void ModifyBuffText(
            ref string buffName,
            ref string tip,
            ref int rare)
        {
            int slots = Main.LocalPlayer
                .GetModPlayer<AlloyConductPlayer>()
                .ReservedMinionSlots;

            tip = Description.Format(slots, slots * 5);
        }
    }
}
