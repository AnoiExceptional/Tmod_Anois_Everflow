using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.Items
{
    /// <summary>
    /// 军团权柄饰品的公共类型；所有等级之间互斥。
    /// </summary>
    public abstract class LegionAuthorityAccessory : ModItem
    {
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) =>
            !(equippedItem.ModItem is LegionAuthorityAccessory && incomingItem.ModItem is LegionAuthorityAccessory);
    }
}
