using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Evaisa.MoreShrines
{
    public static class EvaResources
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Evaisa.MoreShrines.bettershrines"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                bundle.LoadAllAssets();


                if (bundle != null)
                {
                    MoreShrines.Print("Bundle data: " + bundle);
                    MoreShrines.Print("Asset bundle loaded.");
                    ShrineFallenPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/ShrineOfTheFallen/ShrineFallen.prefab");
                    ShrineImpPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/ImpShrine/ShrineImp.prefab");
                    ShrineDisorderPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/ShrineOfDisorder/ShrineDisorder.prefab");
                    ShrineHeresyPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/ShrineOfHeresy/ShrineHeresy.prefab");
                    ShrineWispPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/WispShrine/ShrineWisp.prefab");
                    ShrineShieldPrefab = bundle.LoadAsset<Object>("Assets/BetterShrines/ShrineOfShielding/ShrineShielding.prefab");
                    ShieldBar = bundle.LoadAsset<Sprite>("Assets/BetterShrines/textures/texUINonsegmentedHealthbar.png");
                    var DebuffIconHP = bundle.LoadAsset<Texture2D>("Assets/BetterShrines/Buffs/texBuffHealthDownIcon.png");
                    MoreShrines.Print("Bundle assets loaded.");
                    HPDebuffIcon = Sprite.Create(DebuffIconHP, new Rect(0, 0, DebuffIconHP.width, DebuffIconHP.height), new Vector2(0.5f, 0.5f));

                }
            }
        }

        public static bool Loaded { get; private set; }

        public static Object ShrineFallenPrefab { get; private set; }
        public static Object ShrineImpPrefab { get; private set; }
        public static Object ShrineDisorderPrefab { get; private set; }
        public static Object ShrineWispPrefab { get; private set; }
        public static Object ShrineHeresyPrefab { get; private set; }

        public static Object ShrineShieldPrefab { get; private set; }
        public static Sprite HPDebuffIcon { get; private set; }

        public static Sprite ShieldBar { get; private set; }

    }
}
