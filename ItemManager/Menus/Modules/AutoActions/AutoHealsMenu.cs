﻿namespace ItemManager.Menus.Modules.AutoActions
{
    using Ensage.Common.Menu;

    using HpMpRestore;

    internal class AutoHealsMenu
    {
        public AutoHealsMenu(Menu rootMenu)
        {
            var menu = new Menu("HP/MP restore", "autoHealUsage");

            AutoBottleMenu = new AutoBottleMenu(menu);
            AutoArcaneBootsMenu = new AutoArcaneBootsMenu(menu);
            LivingArmorMenu = new LivingArmorMenu(menu);

            rootMenu.AddSubMenu(menu);
        }

        public AutoArcaneBootsMenu AutoArcaneBootsMenu { get; }

        public AutoBottleMenu AutoBottleMenu { get; }

        public LivingArmorMenu LivingArmorMenu { get; }
    }
}