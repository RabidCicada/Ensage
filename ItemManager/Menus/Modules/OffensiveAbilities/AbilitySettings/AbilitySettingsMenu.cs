﻿namespace ItemManager.Menus.Modules.OffensiveAbilities.AbilitySettings
{
    using System.Collections.Generic;

    using Ensage.Common.Menu;
    using Ensage.Common.Menu.MenuItems;

    internal class AbilitySettingsMenu
    {
        protected Menu Menu;

        private readonly Dictionary<string, bool> heroToggler = new Dictionary<string, bool>();

        public AbilitySettingsMenu(Menu mainMenu, string name, string texture = null)
        {
            var simpleName = name.ToLower().Replace(" ", string.Empty);
            Menu = new Menu(" " + name, simpleName + "OffensiveSettings", false, texture, true);

            var enabled = new MenuItem(simpleName + "OffEnabled", "Enabled").SetValue(false);
            Menu.AddItem(enabled);
            enabled.ValueChanged += (sender, args) => Enabled = args.GetNewValue<bool>();
            Enabled = enabled.IsActive();

            var alwaysUse = new MenuItem(simpleName + "OffAlways", "Always use").SetValue(false);
            alwaysUse.SetTooltip("Will use item whenever possible, like auto disable");
            Menu.AddItem(alwaysUse);
            alwaysUse.ValueChanged += (sender, args) => AlwaysUse = args.GetNewValue<bool>();
            AlwaysUse = alwaysUse.IsActive();

            var delay = new MenuItem(simpleName + "OffDelay", "Delay (ms)").SetValue(new Slider(300, 0, 1000));
            delay.SetTooltip("Delay before use");
            Menu.AddItem(delay);
            delay.ValueChanged += (sender, args) => Delay = args.GetNewValue<Slider>().Value;
            Delay = delay.GetValue<Slider>().Value;

            var hexStack = new MenuItem(simpleName + "OffHex", "Stack with hex").SetValue(false);
            Menu.AddItem(hexStack);
            hexStack.ValueChanged += (sender, args) => HexStack = args.GetNewValue<bool>();
            HexStack = hexStack.IsActive();

            var rootStack = new MenuItem(simpleName + "OffRoot", "Stack with root").SetValue(false);
            Menu.AddItem(rootStack);
            rootStack.ValueChanged += (sender, args) => RootStack = args.GetNewValue<bool>();
            RootStack = rootStack.IsActive();

            var silenceStack = new MenuItem(simpleName + "OffSilence", "Stack with silence").SetValue(false);
            Menu.AddItem(silenceStack);
            silenceStack.ValueChanged += (sender, args) => SilenceStack = args.GetNewValue<bool>();
            SilenceStack = silenceStack.IsActive();

            var stunStack = new MenuItem(simpleName + "OffStun", "Stack with stun").SetValue(false);
            Menu.AddItem(stunStack);
            stunStack.ValueChanged += (sender, args) => StunStack = args.GetNewValue<bool>();
            StunStack = stunStack.IsActive();

            var disarmStack = new MenuItem(simpleName + "OffDisarm", "Stack with disarm").SetValue(false);
            Menu.AddItem(disarmStack);
            disarmStack.ValueChanged += (sender, args) => DisarmStack = args.GetNewValue<bool>();
            DisarmStack = disarmStack.IsActive();

            Menu.AddItem(new EnemyHeroesToggler(simpleName + "enabledFor", "Use on:", heroToggler));

            mainMenu.AddSubMenu(Menu);
        }

        public bool AlwaysUse { get; private set; }

        public int Delay { get; protected set; }

        public bool DisarmStack { get; private set; }

        public bool Enabled { get; private set; }

        public bool HexStack { get; private set; }

        public bool RootStack { get; private set; }

        public bool SilenceStack { get; private set; }

        public bool StunStack { get; private set; }

        public bool IsEnabled(string heroName)
        {
            bool enabled;
            heroToggler.TryGetValue(heroName, out enabled);
            return enabled;
        }
    }
}