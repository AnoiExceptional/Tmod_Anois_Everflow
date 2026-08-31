using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace everflow.Content.Systems
{
    public sealed class AncientSmithWorldFlags : ModSystem
    {
        public static bool DownedAncientArmy;
        public static bool BurgerCookerClaimed;

        public override void ClearWorld()
        {
            DownedAncientArmy = false;
            BurgerCookerClaimed = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (DownedAncientArmy)
                tag["DownedAncientArmy"] = true;
            if (BurgerCookerClaimed)
                tag["BurgerCookerClaimed"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            DownedAncientArmy = tag.GetBool("DownedAncientArmy");
            BurgerCookerClaimed = tag.GetBool("BurgerCookerClaimed");
        }
    }
}
