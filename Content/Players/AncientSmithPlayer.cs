using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace everflow.Content.Players
{
    public sealed class AncientSmithPlayer : ModPlayer
    {
        public bool HasMetAncientSmith;

        public override void SaveData(TagCompound tag)
        {
            if (HasMetAncientSmith)
                tag["HasMetAncientSmith"] = true;
        }

        public override void LoadData(TagCompound tag)
        {
            HasMetAncientSmith = tag.GetBool("HasMetAncientSmith");
        }
    }
}
