using everflow.Content.Mounts;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class MetalTerminator : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToMount(ModContent.MountType<BMPT72>());
            Item.width = 30;
            Item.height = 36;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.sellPrice(gold: 10);
        }

        public override bool CanUseItem(Player player)
        {
            if (player.mount.Active && player.mount.Type == ModContent.MountType<BMPT72>())
                return true;

            return player.maxMinions - player.slotsMinions >= 0.999f;
        }
    }
}
