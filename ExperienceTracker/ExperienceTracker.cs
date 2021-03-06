﻿namespace ExperienceTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Objects.UtilityObjects;

    using SharpDX;

    internal class ExperienceTracker
    {
        private readonly Dictionary<Creep, int> deadCreeps = new Dictionary<Creep, int>();

        private readonly List<Enemy> enemies = new List<Enemy>();

        private Hero hero;

        private Team heroTeam;

        private MenuManager menu;

        private MultiSleeper sleeper;

        public ExperienceTracker()
        {
            Events.OnLoad += EventsOnLoad;
        }

        private void DrawingOnDraw(EventArgs args)
        {
            foreach (var enemy in enemies.Where(x => x.WarningIsActive()))
            {
                var warningText = enemy.EnemiesWarning.ToString();

                if (!menu.SimplifiedWarning)
                {
                    warningText += " enem" + (enemy.EnemiesWarning > 1 ? "ies" : "y") + " near";
                }

                Drawing.DrawText(
                    warningText,
                    "Arial",
                    HUDInfo.GetHPbarPosition(enemy.Hero) + new Vector2(menu.WarningX, menu.WarningY),
                    new Vector2(menu.WarningSize),
                    new Color(menu.WarningRedColor, menu.WarningGreenColor, menu.WarningBlueColor),
                    FontFlags.None);
            }
        }

        private void EntityOnInt32PropertyChange(Entity sender, Int32PropertyChangeEventArgs args)
        {
            if (args.PropertyName == "m_iCurrentXP")
            {
                enemies.FirstOrDefault(x => x.Handle == sender.Handle)?.SetExperience(args.OldValue, args.NewValue);
                return;
            }

            if (!menu.Enabled || args.NewValue > 0 || args.OldValue <= 0 || args.PropertyName != "m_iHealth")
            {
                return;
            }

            var creep = sender as Creep;
            if (creep == null || !creep.IsValid || creep.Team != heroTeam)
            {
                return;
            }

            var exp = creep.GetGrantedExperience();
            if (exp <= 0 || deadCreeps.ContainsKey(creep))
            {
                return;
            }

            //delay check to prevent incorrect information
            //when multiple creeps die at the same time
            deadCreeps.Add(creep, exp);
            sleeper.Sleep(150, deadCreeps);
        }

        private void EventsOnClose(object sender, EventArgs eventArgs)
        {
            Entity.OnInt32PropertyChange -= EntityOnInt32PropertyChange;
            Game.OnIngameUpdate -= GameOnIngameUpdate;
            Events.OnClose -= EventsOnClose;
            Drawing.OnDraw -= DrawingOnDraw;

            menu.OnClose();
            enemies.Clear();
            deadCreeps.Clear();
        }

        private void EventsOnLoad(object sender, EventArgs eventArgs)
        {
            hero = ObjectManager.LocalHero;
            heroTeam = hero.Team;
            sleeper = new MultiSleeper();
            menu = new MenuManager();

            Entity.OnInt32PropertyChange += EntityOnInt32PropertyChange;
            Game.OnIngameUpdate += GameOnIngameUpdate;
            Events.OnClose += EventsOnClose;
            Drawing.OnDraw += DrawingOnDraw;
        }

        private void GameOnIngameUpdate(EventArgs args)
        {
            if (!sleeper.Sleeping(deadCreeps) && deadCreeps.Any())
            {
                foreach (var pair in deadCreeps)
                {
                    var deadCreep = pair.Key;
                    var enemiesInExpRange = enemies.Where(x => x.IsInExperienceRange(deadCreep)).ToList();

                    foreach (var enemy in enemiesInExpRange)
                    {
                        var totalExp = enemy.CalculateGainedExperience(
                            deadCreeps.Where(x => enemy.IsInExperienceRange(x.Key)).Sum(x => x.Value));
                        if (enemy.OldExperience + totalExp / enemiesInExpRange.Count != enemy.NewExperience)
                        {
                            enemy.SetWarning(totalExp, enemiesInExpRange.Count, menu.WarningTime);
                        }
                    }
                }

                deadCreeps.Clear();
            }

            if (sleeper.Sleeping(enemies))
            {
                return;
            }

            foreach (var enemy in ObjectManager.GetEntitiesParallel<Hero>()
                .Where(
                    x => x.IsValid && !enemies.Exists(z => z.Handle == x.Handle) && x.Team != heroTeam
                         && !x.IsIllusion))
            {
                enemies.Add(new Enemy(enemy));
            }

            sleeper.Sleep(2000, enemies);
        }
    }
}