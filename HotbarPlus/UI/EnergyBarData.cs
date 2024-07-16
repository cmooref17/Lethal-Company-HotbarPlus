using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HotbarPlus.UI
{
    internal class EnergyBarData
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public RectTransform rectTransform { get { return transform as RectTransform; } }
        public Slider slider;
        public Image energyBarImage;


        public EnergyBarData(GameObject gameObject)
        {
            this.gameObject = gameObject;
            slider = gameObject.GetComponentInChildren<Slider>();
            energyBarImage = transform.Find("FillMask/Fill")?.GetComponent<Image>();
            if (!energyBarImage)
                Plugin.LogWarning("Failed to find image for energy bar. This is okay.");
        }

        public void SetEnergyBarColor(Color color)
        {
            if (!energyBarImage)
                return;

            energyBarImage.color = color;
        }
    }
}
