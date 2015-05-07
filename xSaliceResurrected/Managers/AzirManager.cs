using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace xSaliceResurrected.Managers
{
    class AzirManager : Orbwalking.Orbwalker
    {
        private static readonly Obj_AI_Hero MyHero = ObjectManager.Player;
        public static readonly List<GameObject> Soilders = new List<GameObject>();
        private static readonly IEnumerable<Obj_AI_Hero> AllEnemys = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy);

        public AzirManager(Menu attachToMenu) : base(attachToMenu)
        {
        }

        public static double GetAzirAaSandwarriorDamage(AttackableUnit target)
        {
            var unit = (Obj_AI_Base)target;
            var dmg = MyHero.GetSpellDamage(unit, SpellSlot.W);

            var count = Soilders.Count(obj => obj.Position.Distance(unit.Position) < 380);

            if (count > 1)
                return dmg + dmg * (count - 1);

            return dmg;
        }

        private static double CountKillhits(Obj_AI_Base enemy)
        {
            return enemy.Health / MyHero.GetAutoAttackDamage(enemy);
        }

        private static bool InSoldierAttackRange(AttackableUnit target)
        {
            return Soilders.Count(obj => obj.Position.Distance(target.Position) < 390 && MyHero.Distance(target) < 1000) > 0;
        }

        private static float GetAutoAttackRange(Obj_AI_Base source = null, AttackableUnit target = null)
        {
            if (source == null)
                source = MyHero;
            var ret = source.AttackRange + MyHero.BoundingRadius;
            if (target != null)
                ret += target.BoundingRadius;
            return ret;
        }

        public override bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.IsValidTarget())
                return false;
            if (Orbwalking.InAutoAttackRange(target))
                return true;
            if (!(target is Obj_AI_Base))
                return false;
            if (InSoldierAttackRange(target))
            {
                return true;
            }
            return false;
        }

        public override AttackableUnit GetTarget()
        {
            AttackableUnit tempTarget = null;

            if ((ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.Combo))
            {
                tempTarget = GetBestHeroTarget();
                if (tempTarget != null)
                    return tempTarget;
            }

            //last hit
            if (ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.LastHit || ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                foreach (var minion in from minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget() && minion.Name != "Beacon" && InAutoAttackRange(minion)
                && minion.Health < 2 * (MyHero.BaseAttackDamage + MyHero.FlatPhysicalDamageMod))
                                       let t = (int)(MyHero.AttackCastDelay * 1000) - 100 + Game.Ping / 2
                                       let predHealth = HealthPrediction.GetHealthPrediction(minion, t, 0)
                                       where minion.Team != GameObjectTeam.Neutral && predHealth > 0 &&
                                             predHealth <= MyHero.GetAutoAttackDamage(minion, true)
                                       select minion)
                    return minion;
            }

            //turret
            if (ActiveMode == Orbwalking.OrbwalkingMode.Mixed || ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {

                foreach (
                    var turret in
                        ObjectManager.Get<Obj_AI_Turret>().Where(turret => turret.IsValidTarget(GetAutoAttackRange(MyHero, turret))))
                    return turret;
            }

            //jungle
            if (ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                float[] maxhealth;
                if (MyHero.ChampionName == "Azir" && Soilders.Count > 0)
                {
                    maxhealth = new float[] { 0 };
                    var maxhealth1 = maxhealth;
                    var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral);
                    foreach (
                        var minion in
                            minions
                                .Where(minion => InSoldierAttackRange(minion) && minion.Name != "Beacon" && minion.IsValidTarget())
                                .Where(minion => minion.MaxHealth >= maxhealth1[0] || Math.Abs(maxhealth1[0] - float.MaxValue) < float.Epsilon))
                    {
                        tempTarget = minion;
                        maxhealth[0] = minion.MaxHealth;
                    }
                    if (tempTarget != null)
                        return tempTarget;
                }

                maxhealth = new float[] { 0 };
                var maxhealth2 = maxhealth;
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget(GetAutoAttackRange(MyHero, minion)) && minion.Name != "Beacon" && minion.Team == GameObjectTeam.Neutral).Where(minion => minion.MaxHealth >= maxhealth2[0] || Math.Abs(maxhealth2[0] - float.MaxValue) < float.Epsilon))
                {
                    tempTarget = minion;
                    maxhealth[0] = minion.MaxHealth;
                }
                if (tempTarget != null)
                    return tempTarget;
            }

            //lane clear
            if (ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                return (ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValidTarget() && InAutoAttackRange(minion))).MaxOrDefault(x => x.Health);
            }

            return null;
        }

        private Obj_AI_Hero GetBestHeroTarget()
        {
            Obj_AI_Hero killableEnemy = null;
            var hitsToKill = double.MaxValue;

            foreach (var enemy in AllEnemys.Where(hero => hero.IsValidTarget() && InAutoAttackRange(hero)))
            {
                var killHits = CountKillhits(enemy);
                if (killableEnemy != null && (!(killHits < hitsToKill) || enemy.HasBuffOfType(BuffType.Invulnerability)))
                    continue;
                hitsToKill = killHits;
                killableEnemy = enemy;
            }
            var bestTarget = HeroManager.Enemies.Where(InAutoAttackRange).OrderByDescending(GetAzirAaSandwarriorDamage).FirstOrDefault();

            return hitsToKill <= 3 ? killableEnemy : bestTarget ?? TargetSelector.GetTarget(GetAutoAttackRange(), TargetSelector.DamageType.Magical);
        }

        public static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Azir_Base_P_Soldier_Ring.troy" && Soilders.Count > 0)
            {
                //Game.PrintChat("Solider Deleted" + sender.NetworkId);
                foreach (var minion in Soilders.ToList())
                {
                    if (minion.NetworkId == sender.NetworkId)
                    {
                        Soilders.Remove(minion);
                    }
                }
            }
        }

        public static void Obj_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Azir_Base_P_Soldier_Ring.troy")
            {
                Console.WriteLine("added");
                Soilders.Add(sender);
            }
        }
    }
}
