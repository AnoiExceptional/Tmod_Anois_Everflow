using everflow.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Systems
{
    /// <summary>
    /// 在所有玩家、物品和投射物 AI 均完成后结算本帧魔力变化。
    /// 这能捕获发生在 Player.PostUpdate 之后的直接扣魔逻辑。
    /// </summary>
    public sealed class AquaVirtueManaTrackingSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
            {
                Player player = Main.player[playerIndex];
                if (player.active)
                    player.GetModPlayer<AquaVirtuePlayer>().FinalizeManaChangesForTick();
            }
        }
    }
}
