﻿using System;
using LeagueSharp;
using LeagueSharp.Common;
using xSaliceResurrected.Base;
using xSaliceResurrected.Managers;

namespace xSaliceResurrected
{
    class Champion : SpellBase
    {
        protected readonly Obj_AI_Hero Player = ObjectManager.Player;

        protected Champion()
        {
            //Events
            Game.OnUpdate += Game_OnGameUpdateEvent;
            Drawing.OnDraw += Drawing_OnDrawEvent;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPosibleToInterruptEvent;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloserEvent;
            GameObject.OnCreate += GameObject_OnCreateEvent;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCastEvent;
            GameObject.OnDelete += GameObject_OnDeleteEvent;
            Obj_AI_Base.OnIssueOrder += ObjAiHeroOnOnIssueOrderEvent;
            Spellbook.OnUpdateChargedSpell += Spellbook_OnUpdateChargedSpellEvent;
            Orbwalking.AfterAttack += AfterAttackEvent;
            Orbwalking.BeforeAttack += BeforeAttackEvent;
        }

        public Champion(bool load)
        {
            if (load)
                GameOnLoad();
        }

        //Orbwalker instance
        protected static Orbwalking.Orbwalker Orbwalker;

        //Menu
        public static Menu menu;
        private static readonly Menu OrbwalkerMenu = new Menu("Orbwalker", "Orbwalker");

        private void GameOnLoad()
        {
            Game.PrintChat("<font color = \"#FFB6C1\">xSalice's Ressurected AIO</font> by <font color = \"#00FFFF\">xSalice</font>");
            Game.PrintChat("<font color = \"#87CEEB\">Feel free to donate via Paypal to:</font> <font color = \"#FFFF00\">xSalicez@gmail.com</font>");

            menu = new Menu(Player.ChampionName, Player.ChampionName, true);

            //Info
            menu.AddSubMenu(new Menu("Info", "Info"));
            menu.SubMenu("Info").AddItem(new MenuItem("Author", "By xSalice"));
            menu.SubMenu("Info").AddItem(new MenuItem("Paypal", "Donate: xSalicez@gmail.com"));

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            menu.AddSubMenu(OrbwalkerMenu);
            Orbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);
            
            //Item Menu
            var itemMenu = new Menu("Items and Summoners", "Items");
            ItemManager.AddToMenu(itemMenu);
            menu.AddSubMenu(itemMenu);
            
            //Lag Manager
            LagManager.AddLagManager(menu);

            menu.AddToMainMenu();

            new PluginLoader();
        }

        protected Orbwalking.Orbwalker GetorbOrbwalker()
        {
            return Orbwalker;
        }

        private void Drawing_OnDrawEvent(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;

            Drawing_OnDraw(args);
        }

        protected virtual void Drawing_OnDraw(EventArgs args)
        {
            //for champs to use
        }

        private void AntiGapcloser_OnEnemyGapcloserEvent(ActiveGapcloser gapcloser)
        {
            AntiGapcloser_OnEnemyGapcloser(gapcloser);
        }

        protected virtual void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //for champs to use
        }

        private void Interrupter_OnPosibleToInterruptEvent(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs spell)
        {
            Interrupter_OnPosibleToInterrupt(unit, spell);
        }

        protected virtual void Interrupter_OnPosibleToInterrupt(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs spell)
        {
            //for champs to use
        }

        private void Game_OnGameUpdateEvent(EventArgs args)
        {
            if (LagManager.Enabled)
                if (!LagManager.ReadyState)
                    return;

            //check if player is dead
            if (Player.IsDead) return;

            Game_OnGameUpdate(args);
        }

        protected virtual void Game_OnGameUpdate(EventArgs args)
        {
            //for champs to use
        }

        private void GameObject_OnCreateEvent(GameObject sender, EventArgs args)
        {
            GameObject_OnCreate(sender, args);
        }

        protected virtual void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        private void GameObject_OnDeleteEvent(GameObject sender, EventArgs args)
        {
            GameObject_OnDelete(sender, args);
        }

        protected virtual void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        private void Obj_AI_Base_OnProcessSpellCastEvent(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            Obj_AI_Base_OnProcessSpellCast(unit, args);
        }

        protected virtual void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            //for champ use
        }

        private void AfterAttackEvent(AttackableUnit unit, AttackableUnit target)
        {
            AfterAttack(unit, target);
        }

        protected virtual void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            //for champ use
        }

        private void BeforeAttackEvent(Orbwalking.BeforeAttackEventArgs args)
        {
            BeforeAttack(args);
        }

        protected virtual void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //for champ use
        }

        private void ObjAiHeroOnOnIssueOrderEvent(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            ObjAiHeroOnOnIssueOrder(sender, args);
        }

        protected virtual void ObjAiHeroOnOnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            //for champ use
        }

        private void Spellbook_OnUpdateChargedSpellEvent(Spellbook sender, SpellbookUpdateChargedSpellEventArgs args)
        {
            Spellbook_OnUpdateChargedSpell(sender, args);
        }

        protected virtual void Spellbook_OnUpdateChargedSpell(Spellbook sender, SpellbookUpdateChargedSpellEventArgs args)
        {
            //for champ use
        }
    }
}