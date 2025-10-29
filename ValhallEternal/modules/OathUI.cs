using JetBrains.Annotations;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ValhallEternal.modules
{
    internal class OathUI : MonoBehaviour
    {
        // UI instance for this player
        public static OathUI Instance => _instance ??= new OathUI();
        private static OathUI _instance;

        private static GameObject OathPanel;

        public void Awake()
        {
        
        }

        public void Show()
        {
            if (OathPanel == null)
            {
                CreateStaticUIObjects();
            }
            OathPanel.SetActive(true);
        }

        public void Hide()
        {
            // Logger.LogDebug("Closing");
            if (OathPanel != null)
            {
                OathPanel.SetActive(false);
            }
            GUIManager.BlockInput(false);
        }

        private void CreateStaticUIObjects()
        {
            if (GUIManager.Instance == null || !GUIManager.CustomGUIFront)
            {
                Logger.LogWarning("GUIManager not setup, skipping static object creation.");
                return;
            }

            // Create the panel object
            OathPanel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 800,
                height: 800,
                draggable: true);
            // Hide it right away
            OathPanel.SetActive(false);

            var textHeader = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$selection_header"),
                parent: OathPanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(50f, 360f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            textHeader.name = "DLHeader";
        }
    }
}
