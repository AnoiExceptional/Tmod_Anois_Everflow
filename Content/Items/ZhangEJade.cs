using everflow.Content.NPCs.Bosses;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class ZhangEJade : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.sellPrice(gold: 5);
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) =>
            player.ZoneOverworldHeight &&
            !NPC.AnyNPCs(ModContent.NPCType<ZhengNing>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            int bossType = ModContent.NPCType<ZhengNing>();
            SoundEngine.PlaySound(SoundID.Roar, player.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(
                    MessageID.SpawnBossUseLicenseStartEvent,
                    number: player.whoAmI,
                    number2: bossType);
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, bossType);
            }

            return true;
        }
    }
}
