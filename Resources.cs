using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Evaisa.BetterShrines
{
    public static class EvaResources
    {
        public static void Init()
        {
            if (Loaded)
                return;

            Loaded = true;
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream("Evaisa.BetterShrines.shrinebundle"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);

                ShrineFallenPrefab = bundle.LoadAsset<Object>("Assets/ShrineOfTheFallen/ShrineFallen.prefab");
                BetterShrines.Print(ShrineFallenPrefab.name + " was loaded!");
            }
        }

        public static bool Loaded { get; private set; }

        public static Object ShrineFallenPrefab { get; private set; }

    }
}
