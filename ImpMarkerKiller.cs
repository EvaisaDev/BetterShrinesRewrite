using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Evaisa.MoreShrines
{
    public class ImpMarkerKiller : MonoBehaviour
    {
        public void Update()
        {
            var markerComponent = GetComponent<PositionIndicator>();
            if (!markerComponent.targetTransform)
            {
                Evaisa.MoreShrines.MoreShrines.Print("Destroyed indicator!");
                DestroyImmediate(this.gameObject);
            }
        }
    }
}
