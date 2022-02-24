using RoR2.UI;
using RoR2;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace Evaisa.MoreShrines
{
    public class HealthBarAPI
    {
        //public static List<HealthBarInfo> healthBars = new List<HealthBarInfo>();

        public static ConditionalWeakTable<HealthBar, HealthBarInfo> healthBars = new ConditionalWeakTable<HealthBar, HealthBarInfo>();

        public static void Init()
        {
            On.RoR2.UI.HealthBar.Awake += HealthBar_Awake;
        }

        public class HealthBarInfo
        {
            public GameObject healthBarObject;
            public HealthBar healthBar;
        }

        private static void HealthBar_Awake(On.RoR2.UI.HealthBar.orig_Awake orig, HealthBar self)
        {
            healthBars.Add(self, new HealthBarInfo { healthBarObject = self.gameObject, healthBar = self });
            orig(self);
        }

    }
}
