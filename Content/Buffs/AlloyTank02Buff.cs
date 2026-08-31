using Terraria;
using Terraria.ModLoader;
using everflow.Content.Mounts;
using everflow.Common.Players;

namespace everflow.Content.Buffs
{
    public class AlloyTank02Buff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<AlloyTank02>(), player);
            player.buffTime[buffIndex] = 10;
        }

        public override void ModifyBuffText(
            ref string buffName,
            ref string tip,
            ref int rare)
        {
            int crewCount = Main.LocalPlayer
                .GetModPlayer<AlloyTank02Player>()
                .CrewCount;

            tip = Description.Format(crewCount, crewCount * 5);
        }
    }
}
