using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloySpearProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            // Projectile碰撞箱
            // 注意这不是贴图大小
            Projectile.width = 18;
            Projectile.height = 18;

            // 原版长矛AI
            Projectile.aiStyle = ProjAIStyleID.Spear;

            // 参考钛金三叉戟的原版长矛行为
            AIType = ProjectileID.TitaniumTrident;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.DamageType = DamageClass.Melee;

            // 长矛在一次戳刺过程中可以持续判定
            Projectile.penetrate = -1;

            // 长矛不会撞墙消失
            Projectile.tileCollide = false;

            // 防止隔墙攻击
            Projectile.ownerHitCheck = true;

            // 原版长矛Projectile通常由玩家持有
            Projectile.hide = true;

            Projectile.scale = 1f;
        }
    }
}