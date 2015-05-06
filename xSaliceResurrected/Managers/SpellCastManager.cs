using LeagueSharp;
using LeagueSharp.Common;

namespace xSaliceResurrected.Managers
{
    class SpellCastManager
    {
        public static void CastBasicSkillShot(Spell spell, float range, TargetSelector.DamageType type, HitChance hitChance, bool towerCheck = false)
        {
            var target = TargetSelector.GetTarget(range, type);

            if (target == null || !spell.IsReady())
                return;

            if (towerCheck && target.UnderTurret(true))
                return;

            spell.UpdateSourcePosition();

            if (spell.GetPrediction(target).Hitchance >= hitChance)
                spell.Cast(target);
        }

        public static void CastBasicFarm(Spell spell)
        {
            if (!spell.IsReady())
                return;
            var minion = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spell.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minion.Count == 0)
                return;

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                spell.UpdateSourcePosition();

                var predPosition = spell.GetCircularFarmLocation(minion);

                if (predPosition.MinionsHit >= 2)
                {
                    spell.Cast(predPosition.Position);
                }
            }
            else if (spell.Type == SkillshotType.SkillshotLine || spell.Type == SkillshotType.SkillshotCone)
            {
                spell.UpdateSourcePosition();

                var predPosition = spell.GetLineFarmLocation(minion);

                if (predPosition.MinionsHit >= 2)
                    spell.Cast(predPosition.Position);
            }
        }
    }
}
