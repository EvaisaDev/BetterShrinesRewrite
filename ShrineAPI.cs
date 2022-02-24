using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Evaisa.MoreShrines
{
    class ShrineAPI
    {
        public enum ShrineBaseType
        {
            Chance,
            Healing,
            Order,
            Blood,
            Combat,
            Gold,
            None
        }

        static Dictionary<ShrineBaseType, string> shrineDict = new Dictionary<ShrineBaseType, string>()
        {
            {ShrineBaseType.Chance, "spawncards/interactablespawncard/iscShrineChance"},
            {ShrineBaseType.Healing, "spawncards/interactablespawncard/iscShrineHealing"},
            {ShrineBaseType.Order, "spawncards/interactablespawncard/iscShrineRestack"},
            {ShrineBaseType.Blood, "spawncards/interactablespawncard/iscShrineBlood"},
            {ShrineBaseType.Combat, "spawncards/interactablespawncard/iscShrineCombat"},
            {ShrineBaseType.Gold, "spawncards/interactablespawncard/iscShrineGoldshoresAccess"},
            {ShrineBaseType.None, "spawncards/interactablespawncard/iscShrineChance"},
        };

        public class ShrineInfo
        {
            public GameObject shrinePrefab;
            public CombatDirector combatDirector;
            public Transform modelTransform;
            public Transform symbolTransform;

            public void Create(ShrineBaseType baseShrine, GameObject shrinePrefab, Color? symbolColor = null, Shader overrideShader = null, Color ? materialColor = null)
            {

                var baseCard = Resources.Load<SpawnCard>(shrineDict[baseShrine]);
                var basePrefab = baseCard.prefab;


                var oldSymbol = basePrefab.transform.Find("Symbol");
                var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
                var oldSymbolMaterial = oldSymbolRenderer.material;


                if (shrinePrefab.GetComponent<ModelLocator>())
                {

                    modelTransform = shrinePrefab.GetComponent<ModelLocator>().modelTransform;
                    foreach (MeshRenderer renderer in modelTransform.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (overrideShader != null)
                        {
                            renderer.material.shader = overrideShader;

                        }
                        else
                        {
                            renderer.material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
                        }

                    }

                    foreach (MeshRenderer renderer in modelTransform.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (materialColor != null)
                        {
                            renderer.material.color = (Color)materialColor;
                        }
                        else
                        {
                            renderer.material.color = Color.white;
                        }
                    }
                }

                if (shrinePrefab.GetComponent<CombatDirector>())
                {
                    combatDirector = shrinePrefab.GetComponent<CombatDirector>();
                }



                if (shrinePrefab.transform.Find("Symbol"))
                {
                    symbolTransform = shrinePrefab.transform.Find("Symbol");

                    if (symbolTransform.GetComponent<MeshRenderer>())
                    {
                        var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

                        var texture = symbolMesh.material.mainTexture;
                        symbolMesh.material = new Material(oldSymbolMaterial.shader);
                        symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
                        symbolMesh.material.mainTexture = texture;
                        if (symbolColor != null)
                        {
                            symbolMesh.material.SetColor("_TintColor", (Color)symbolColor);
                        }
                    }
                }

                this.shrinePrefab = shrinePrefab;

            }

            public ShrineInfo(ShrineBaseType baseShrine, GameObject shrinePrefab, Color symbolColor, Shader overrideShader, Color materialColor)
            {
                Create(baseShrine, shrinePrefab, symbolColor, overrideShader, materialColor);
            }

            public ShrineInfo(ShrineBaseType baseShrine, GameObject shrinePrefab, Shader overrideShader, Color materialColor)
            {
                Create(baseShrine, shrinePrefab, null, overrideShader, materialColor);
            }

            public ShrineInfo(ShrineBaseType baseShrine, Color materialColor, GameObject shrinePrefab)
            {
                Create(baseShrine, shrinePrefab, null, null, materialColor);
            }

            public ShrineInfo(ShrineBaseType baseShrine, GameObject shrinePrefab, Color symbolColor, Shader overrideShader)
            {
                Create(baseShrine, shrinePrefab, symbolColor, overrideShader, null);
            }
            public ShrineInfo(ShrineBaseType baseShrine, GameObject shrinePrefab, Color symbolColor)
            {
                Create(baseShrine, shrinePrefab, symbolColor, null, null);
            }
            public ShrineInfo(ShrineBaseType baseShrine, GameObject shrinePrefab)
            {
                Create(baseShrine, shrinePrefab, null, null, null);
            }
        }
    }
}
