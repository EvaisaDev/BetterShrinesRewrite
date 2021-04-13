using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using BetterAPI;
using UnityEngine;
using RoR2.UI;

namespace Evaisa.MoreShrines
{
   
    internal class InitBuffs
    {
        public static BuffDef maxHPDown;
        public static BuffDef maxHPDownStage;
        public static List<CharacterBody> players = new List<CharacterBody>();

        public static void Add()
        {
            maxHPDown = ScriptableObject.CreateInstance<BuffDef>();

            maxHPDown.name = "Max HP Down";
            maxHPDown.isDebuff = true;
            maxHPDown.canStack = true;
            maxHPDown.iconSprite = EvaResources.HPDebuffIcon;

            maxHPDown.buffColor = Color.red;

            BetterAPI.Buffs.Add(maxHPDown);

            maxHPDownStage = ScriptableObject.CreateInstance<BuffDef>();

            maxHPDownStage.name = "Stage Max HP Down";
            maxHPDownStage.isDebuff = true;
            maxHPDownStage.canStack = true;
            maxHPDownStage.iconSprite = EvaResources.HPDebuffIcon;

            maxHPDownStage.buffColor = Color.red;

            BetterAPI.Buffs.Add(maxHPDownStage);

            // On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            Stats.health.collectMultipliers += HealthMultiplier_getStatModifiers;
            On.RoR2.Stage.BeginAdvanceStage +=Stage_BeginAdvanceStage;

            On.RoR2.UI.BuffIcon.UpdateIcon += BuffIcon_UpdateIcon;
        }

        private static void BuffIcon_UpdateIcon(On.RoR2.UI.BuffIcon.orig_UpdateIcon orig, RoR2.UI.BuffIcon self)
        {
            if (!self.buffDef)
            {
                self.iconImage.sprite = null;
                return;
            }
            if (self.buffDef == maxHPDown || self.buffDef == maxHPDownStage)
            {
                self.iconImage.sprite = self.buffDef.iconSprite;
                self.iconImage.color = self.buffDef.buffColor;
                if (self.buffDef.canStack)
                {
                    BuffIcon.sharedStringBuilder.Clear();
                    BuffIcon.sharedStringBuilder.AppendInt(self.buffCount, 1U, uint.MaxValue);
                    BuffIcon.sharedStringBuilder.Append("%");
                    self.stackCount.enabled = true;
                    self.stackCount.SetText(BuffIcon.sharedStringBuilder);
                    return;
                }
                self.stackCount.enabled = false;
            }
            else
            {
                orig(self);
            }
        }

        private static void HealthMultiplier_getStatModifiers(CharacterBody sender, Stats.StatsEventHandler.StatsEventArgs e)
        {
            if (sender is CharacterBody characterBody)
            {
                e.stat *= 1f - characterBody.GetBuffCount(maxHPDownStage) / 100f;
            }
        }

        /*
                 if (!players.Contains(self))
                {
                    players.Add(self);
                }
        */

        private static void Stage_BeginAdvanceStage(On.RoR2.Stage.orig_BeginAdvanceStage orig, Stage self, SceneDef destinationStage)
        {
            foreach(var player in players)
            {
                for(var i = 0; i < player.GetBuffCount(maxHPDownStage); i++)
                {
                    player.RemoveBuff(maxHPDownStage);
                }
            }
            orig(self, destinationStage);
        }

        /*
        private static void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            
            orig(self);

            var originalMaxHealth = self.maxHealth;

            var maxHPDowns = self.GetBuffCount(maxHPDown);
            var stageMaxHPDowns = self.GetBuffCount(maxHPDownStage);
           
            if (stageMaxHPDowns > 0)
            {
                Debug.Log("rawr2");
                self.maxHealth = originalMaxHealth - (originalMaxHealth * ((float)stageMaxHPDowns / 100f));
            }

            //
            if (maxHPDowns > 0)
            {
                Debug.Log("rawr1");
                self.maxHealth = originalMaxHealth - (originalMaxHealth * ((float)maxHPDowns / 100f));
            }

        }*/

    }


}
