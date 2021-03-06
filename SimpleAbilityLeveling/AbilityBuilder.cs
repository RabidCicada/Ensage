﻿namespace SimpleAbilityLeveling
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;

    using Ensage;
    using Ensage.Common.Extensions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class AbilityBuilder
    {
        private readonly Dictionary<string, string> abilityNames = new Dictionary<string, string>();

        private readonly Hero hero;

        private readonly List<Tuple<float, Dictionary<uint, string>>> rawBuilds =
            new List<Tuple<float, Dictionary<uint, string>>>();

        private Dictionary<uint, Ability> bestBuild = new Dictionary<uint, Ability>();

        private bool error;

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public AbilityBuilder(Hero hero)
        {
            this.hero = hero;

            JToken @object;
            if (JObject.Parse(Encoding.Default.GetString(Resource.Names)).TryGetValue("AbilityNames", out @object))
            {
                abilityNames = JsonConvert.DeserializeObject<AbilityNames[]>(@object.ToString())
                    .ToDictionary(x => x.Name, x => x.DotaName);
            }

            SaveAbilityBuild(GetDotabuffName(hero.ClassId));
        }

        public string BestBuildWinRate { get; private set; }

        public Ability GetAbility()
        {
            if (error)
            {
                return null;
            }

            Ability ability;

            var abilityLevels = (uint)hero.Spellbook.Spells
                                    .Where(x => !x.IsHidden && !IgnoredAbilities.List.Contains(x.Name))
                                    .Sum(x => x.Level) + 1;

            return bestBuild.TryGetValue(abilityLevels, out ability) ? ability : hero.Spellbook.Spells.FirstOrDefault();
        }

        public IEnumerable<Ability> GetBestBuild()
        {
            return bestBuild.OrderBy(x => x.Key).Select(x => x.Value);
        }

        private static string GetDotabuffName(ClassId classId)
        {
            switch (classId)
            {
                case ClassId.CDOTA_Unit_Hero_DoomBringer:
                    return "doom";
                case ClassId.CDOTA_Unit_Hero_Furion:
                    return "natures-prophet";
                case ClassId.CDOTA_Unit_Hero_Magnataur:
                    return "magnus";
                case ClassId.CDOTA_Unit_Hero_Necrolyte:
                    return "necrophos";
                case ClassId.CDOTA_Unit_Hero_Nevermore:
                    return "shadow-fiend";
                case ClassId.CDOTA_Unit_Hero_Obsidian_Destroyer:
                    return "outworld-devourer";
                case ClassId.CDOTA_Unit_Hero_Rattletrap:
                    return "clockwerk";
                case ClassId.CDOTA_Unit_Hero_Shredder:
                    return "timbersaw";
                case ClassId.CDOTA_Unit_Hero_SkeletonKing:
                    return "wraith-king";
                case ClassId.CDOTA_Unit_Hero_Wisp:
                    return "io";
                case ClassId.CDOTA_Unit_Hero_Zuus:
                    return "zeus";
                case ClassId.CDOTA_Unit_Hero_Windrunner:
                    return "windranger";
                case ClassId.CDOTA_Unit_Hero_Life_Stealer:
                    return "lifestealer";
                case ClassId.CDOTA_Unit_Hero_Treant:
                    return "treant-protector";
                case ClassId.CDOTA_Unit_Hero_MonkeyKing:
                    return "monkey-king";
                case ClassId.CDOTA_Unit_Hero_AbyssalUnderlord:
                    return "underlord";
            }

            var name = classId.ToString().Substring("CDOTA_Unit_Hero_".Length).Replace("_", string.Empty);
            var newName = new StringBuilder(name[0].ToString());

            foreach (var ch in name.Skip(1))
            {
                if (char.IsUpper(ch))
                {
                    newName.Append('-');
                }
                newName.Append(ch);
            }

            return newName.ToString().ToLower();
        }

        private void GetBestWinRateBuild()
        {
            var best = rawBuilds.OrderByDescending(x => x.Item1).First();
            bestBuild = best.Item2.ToDictionary(x => x.Key, x => hero.FindSpell(x.Value));
            BestBuildWinRate = best.Item1 + "%";
        }

        private void SaveAbilityBuild(string heroName)
        {
            try
            {
                string html;
                var webRequest = WebRequest.CreateHttp("http://www.dotabuff.com/heroes/" + heroName + "/builds");
                webRequest.UserAgent = "Nokia 3310";

                using (var responseStream = webRequest.GetResponse().GetResponseStream())
                {
                    using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        html = streamReader.ReadToEnd();
                    }
                }

                var abilityBuild = Regex.Match(html, @"<div class=""skill-build"">.+?</div></div></div></div>");

                while (abilityBuild.Success)
                {
                    var winRate = Regex.Match(abilityBuild.Value, @"\>(\d{1,2}\.\d{1,2})\%\<").NextMatch();
                    var ability = Regex.Match(abilityBuild.Value, @"<div class=""skill"">.+?</div></div></div>");

                    var saveBuild = new Dictionary<uint, string>();

                    while (ability.Success)
                    {
                        var abilityName = Regex.Match(ability.Value, @"<img alt=""(.+?)""");
                        var dotaAbilityName = string.Empty;

                        if (abilityName.Success)
                        {
                            var name = abilityName.Groups[1].Value.Replace("&#39;", "\'");

                            if (name.Contains("Talent:"))
                            {
                                break;
                            }

                            if (heroName == "queen-of-pain" && name == "Blink")
                            {
                                // Anti-Mage Blink conflict fix
                                name = "Queen of Pain Blink";
                            }
                            else if (heroName == "shadow-shaman" && name == "Hex")
                            {
                                // Lion Hex conflict fix
                                name = "Shadow Shaman Hex";
                            }

                            abilityNames.TryGetValue(name, out dotaAbilityName);
                        }

                        if (string.IsNullOrEmpty(dotaAbilityName))
                        {
                            Game.PrintMessage(
                                "<font color='#FF0000'>[Simple Ability Leveling] Ability " + abilityName.Groups[1].Value
                                + " not found<br>[Simple Ability Leveling] Report this on forum please</font>");
                            error = true;
                        }

                        var level = Regex.Match(ability.Value, @"\>(\d{1,2})\<");

                        while (level.Success)
                        {
                            saveBuild.Add(uint.Parse(level.Groups[1].Value), dotaAbilityName);
                            level = level.NextMatch();
                        }

                        ability = ability.NextMatch();
                    }

                    rawBuilds.Add(Tuple.Create(float.Parse(winRate.Groups[1].Value), saveBuild));
                    abilityBuild = abilityBuild.NextMatch();
                }
                GetBestWinRateBuild();
            }
            catch (Exception)
            {
                Game.PrintMessage(
                    "<font color='#FF0000'>[Simple Ability Leveling] Something went wrong with " + hero.GetRealName()
                    + " build<br>[Simple Ability Leveling] Report this on forum please</font>");
                error = true;
            }
        }
    }
}