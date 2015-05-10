using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using xSaliceResurrected.Utilities;
using Color = System.Drawing.Color;

namespace xSaliceResurrected.Managers
{
    class SpellCastManager
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

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

        public static void CastSingleLine(Spell spell, Spell spell2, bool wallCheck, float extraPrerange = 1)
        {

            //-------------------------------Single---------------------------
            var target = TargetSelector.GetTarget(spell.Range + spell2.Range, TargetSelector.DamageType.Magical);

            if (target == null)
                return;

            var vector1 = Player.ServerPosition + Vector3.Normalize(target.ServerPosition - Player.ServerPosition) * (spell.Range * extraPrerange);

            spell2.UpdateSourcePosition(vector1, vector1);

            var pred = spell2.GetPrediction(target, true);
            Geometry.Polygon.Rectangle rec1 = new Geometry.Polygon.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

            if (Player.Distance(target) < spell.Range)
            {
                vector1 = pred.CastPosition.Extend(target.ServerPosition, spell2.Range*.3f);
                spell2.UpdateSourcePosition(vector1, vector1);
                Geometry.Polygon.Rectangle rec2 = new Geometry.Polygon.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

                if ((!rec2.Points.Exists(Util.IsWall) || !wallCheck) && pred.Hitchance >= HitChance.Medium)
                    CastLineSpell(spell2, vector1, vector1.Extend(pred.CastPosition, spell2.Range));

            }
            else if (!rec1.Points.Exists(Util.IsWall) || !wallCheck)
            {
                //wall check
                if (pred.Hitchance >= HitChance.Medium)
                    CastLineSpell(spell2, vector1, vector1.Extend(pred.CastPosition, spell2.Range));
            }
        }

        //rumble = R, R2, 600
        public static void CastBestLine(bool forceUlt, Spell spell, Spell spell2, int midPointRange, Menu menu, float extraPrerange = 1, bool wallCheck = true)
        {
            int maxHit = 0;
            Vector3 start = Vector3.Zero;
            Vector3 end = Vector3.Zero;

            //loop one
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range)))
            {
               //loop 2
                var target1 = target;
                var target2 = target;
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target1.NetworkId
                    && x.Distance(target1.Position) < spell2.Range - 100).OrderByDescending(x => x.Distance(target2.Position)))
                {
                    int hit = 2;

                    var targetPred = Prediction.GetPrediction(target, spell.Delay);
                    var enemyPred = Prediction.GetPrediction(enemy, spell.Delay);

                    var midpoint = (enemyPred.CastPosition + targetPred.CastPosition) / 2;

                    var startpos = midpoint + Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;
                    var endPos = midpoint - Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;

                    Geometry.Polygon.Rectangle rec1 = new Geometry.Polygon.Rectangle(startpos, endPos, spell.Width);

                    if (!rec1.Points.Exists(Util.IsWall) && Player.CountEnemiesInRange(spell.Range + spell2.Range) > 2)
                    {
                        //loop 3
                        var target3 = target;
                        var enemy1 = enemy;
                        foreach (var enemy2 in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target3.NetworkId && x.NetworkId != enemy1.NetworkId && x.Distance(target3.Position) < 1000))
                        {
                            var enemy2Pred = Prediction.GetPrediction(enemy2, spell.Delay);
                            Object[] obj = Util.VectorPointProjectionOnLineSegment(startpos.To2D(), endPos.To2D(), enemy2Pred.CastPosition.To2D());
                            var isOnseg = (bool)obj[2];
                            var pointLine = (Vector2)obj[1];

                            if (pointLine.Distance(enemy2Pred.CastPosition.To2D()) < spell.Width && isOnseg)
                            {
                                hit++;
                            }
                        }
                    }

                    if (hit > maxHit && hit > 1 && !rec1.Points.Exists(Util.IsWall))
                    {
                        maxHit = hit;
                        start = startpos;
                        end = endPos;
                    }
                }
            }

            if (start != Vector3.Zero && end != Vector3.Zero && spell.IsReady())
            {
                spell2.UpdateSourcePosition(start, start);
                if (forceUlt)
                    CastLineSpell(spell2, start, end);
                if (menu.Item("ComboActive", true).GetValue<KeyBind>().Active && maxHit >= menu.Item("Line_If_Enemy_Count_Combo", true).GetValue<Slider>().Value)
                    CastLineSpell(spell2, start, end);
                if (maxHit >= menu.Item("Line_If_Enemy_Count", true).GetValue<Slider>().Value)
                    CastLineSpell(spell2, start, end);
            }            

            //check if only one target
            if (forceUlt)
            {
                CastSingleLine(spell, spell2, wallCheck, extraPrerange);
            }
        }

        public static void DrawBestLine(Spell spell, Spell spell2, int midPointRange, float extraPrerange = 1, bool wallCheck = true)
        {
            //---------------------------------MEC----------------------------
            int maxHit = 0;
            Vector3 start = Vector3.Zero;
            Vector3 end = Vector3.Zero;

            //loop one
            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range)))
            {
                //loop 2
                var target1 = target;
                var target2 = target;
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target1.NetworkId
                    && x.Distance(target1.Position) < spell2.Range - 100).OrderByDescending(x => x.Distance(target2.Position)))
                {
                    int hit = 2;

                    var targetPred = Prediction.GetPrediction(target, spell.Delay);
                    var enemyPred = Prediction.GetPrediction(enemy, spell.Delay);

                    var midpoint = (enemyPred.CastPosition + targetPred.CastPosition) / 2;

                    var startpos = midpoint + Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;
                    var endPos = midpoint - Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;

                    Geometry.Polygon.Rectangle rec1 = new Geometry.Polygon.Rectangle(startpos, endPos, spell.Width);

                    if (!rec1.Points.Exists(Util.IsWall) && Player.CountEnemiesInRange(spell.Range + spell2.Range) > 2)
                    {
                        //loop 3
                        var target3 = target;
                        var enemy1 = enemy;
                        foreach (var enemy2 in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target3.NetworkId && x.NetworkId != enemy1.NetworkId && x.Distance(target3.Position) < spell2.Range))
                        {
                            var enemy2Pred = Prediction.GetPrediction(enemy2, spell.Delay);
                            Object[] obj = Util.VectorPointProjectionOnLineSegment(startpos.To2D(), endPos.To2D(), enemy2Pred.CastPosition.To2D());
                            var isOnseg = (bool)obj[2];
                            var pointLine = (Vector2)obj[1];

                            if (pointLine.Distance(enemy2Pred.CastPosition.To2D()) < spell.Width && isOnseg)
                            {
                                hit++;
                            }
                        }
                    }

                    if (hit > maxHit && hit > 1 && !rec1.Points.Exists(Util.IsWall))
                    {
                        maxHit = hit;
                        start = startpos;
                        end = endPos;
                    }
                }
            }

            if (maxHit >= 2)
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + maxHit);

                Geometry.Polygon.Rectangle rec1 = new Geometry.Polygon.Rectangle(start, end, spell.Width);
                rec1.Draw(Color.Blue, 4);
                return;
            }

            //-------------------------------Single---------------------------
            var unit = TargetSelector.GetTarget(spell.Range + spell2.Range, TargetSelector.DamageType.Magical);

            if (unit == null)
                return;

            var vector1 = Player.ServerPosition + Vector3.Normalize(unit.ServerPosition - Player.ServerPosition) * (spell.Range * extraPrerange);

            var pred = spell2.GetPrediction(unit, true);
            Geometry.Polygon.Rectangle rectangle = new Geometry.Polygon.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

            if (Player.Distance(unit) < spell.Range)
            {
                vector1 = pred.CastPosition.Extend(unit.ServerPosition, spell2.Range * .3f);
                Geometry.Polygon.Rectangle rec2 = new Geometry.Polygon.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

                if ((!rec2.Points.Exists(Util.IsWall) || !wallCheck) && pred.Hitchance >= HitChance.Medium)
                {
                    Vector2 wts = Drawing.WorldToScreen(Player.Position);
                    Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + 1);

                    rec2.Draw(Color.Blue, 4);
                }

            }
            else if (!rectangle.Points.Exists(Util.IsWall) || !wallCheck)
            {
                //wall check
                if (pred.Hitchance >= HitChance.Medium)
                {
                    Vector2 wts = Drawing.WorldToScreen(Player.Position);
                    Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + 1);

                    rectangle.Draw(Color.Blue, 4);
                }
            }
        }

        public static void CastLineSpell(Spell spell, Vector3 start, Vector3 end)
        {
            if (!spell.IsReady())
                return;

            spell.Cast(start, end);
        }
    }
}
