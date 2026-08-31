using Terraria.ModLoader;

namespace everflow.Common.Players
{
    public class DragonsEdgePlayer : ModPlayer
    {
        public int RightWhipHitCount { get; set; }
        public int StardustWhipHitCount { get; set; }
        public int PhantasmWhipHitCount { get; set; }

        public override void UpdateDead()
        {
            RightWhipHitCount = 0;
            StardustWhipHitCount = 0;
            PhantasmWhipHitCount = 0;
        }
    }
}
