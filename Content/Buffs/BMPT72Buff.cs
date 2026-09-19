using everflow.Common.Players;
using everflow.Content.Mounts;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class BMPT72Buff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<BMPT72>(), player);
            player.buffTime[buffIndex] = 10;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int crewCount = Main.LocalPlayer.GetModPlayer<BMPT72Player>().CrewCount;
            tip = Description.Format(crewCount, crewCount * 5);
        }
    }
}
