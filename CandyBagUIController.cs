using BepInEx.Logging;
using GameNetcodeStuff;
using LethalLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
//using TMPro;

namespace SCP956
{
    public class CandyBagUIController : MonoBehaviour
    {
        private static ManualLogSource logger = SCP956.Plugin.LoggerInstance;

        public static CandyBagUIController Instance;

        public VisualElement veMain;
        private bool showingUI = false;

        public Button btnBlue;
        public Button btnGreen;
        public Button btnPink;
        public Button btnPurple;
        public Button btnRainbow;
        public Button btnRed;
        public Button btnYellow;
        public Button btnOther;

        private void Start()
        {
            logger.LogDebug("UIController: Start()");

            if (Instance == null)
            {
                Instance = this;
            }

            // Get UIDocument
            logger.LogDebug("Getting UIDocument");
            UIDocument uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) { logger.LogError("uiDocument not found."); return; }

            // Get VisualTreeAsset
            logger.LogDebug("Getting visual tree asset");
            if (uiDocument.visualTreeAsset == null) { logger.LogError("visualTreeAsset not found."); return; }

            // Instantiate root
            VisualElement root = uiDocument.visualTreeAsset.Instantiate();
            if (root == null) { logger.LogError("root is null!"); return; }
            logger.LogDebug("Adding root");
            uiDocument.rootVisualElement.Add(root);
            if (uiDocument.rootVisualElement == null) { logger.LogError("uiDocument.rootVisualElement not found."); return; }
            logger.LogDebug("Got root");
            root = uiDocument.rootVisualElement;

            veMain = uiDocument.rootVisualElement.Q<VisualElement>("veMain");
            veMain.style.display = DisplayStyle.None;
            if (veMain == null) { logger.LogError("veMain not found."); return; }

            // Find elements

            btnBlue = root.Q<Button>("btnBlue");
            if (btnBlue == null) { logger.LogError("btnBlue not found."); return; }

            btnGreen = root.Q<Button>("btnGreen");
            if (btnGreen == null) { logger.LogError("btnGreen not found."); return; }

            btnPink = root.Q<Button>("btnPink");
            if (btnPink == null) { logger.LogError("btnPink not found."); return; }

            btnPurple = root.Q<Button>("btnPurple");
            if (btnPurple == null) { logger.LogError("btnPurple not found."); return; }

            btnRainbow = root.Q<Button>("btnRainbow");
            if (btnRainbow == null) { logger.LogError("btnRainbow not found."); return; }

            btnRed = root.Q<Button>("btnRed");
            if (btnRed == null) { logger.LogError("btnRed not found."); return; }

            btnYellow = root.Q<Button>("btnYellow");
            if (btnYellow == null) { logger.LogError("btnYellow not found."); return; }

            btnOther = root.Q<Button>("btnOther");
            if (btnOther == null) { logger.LogError("btnOther not found."); return; }

            logger.LogDebug("Got Controls for UI");

            // Add event handlers
            btnBlue.clickable.clicked += () => ButtonClicked("Blue Candy", Convert.ToInt32(btnBlue.text));
            btnGreen.clickable.clicked += () => ButtonClicked("Green Candy", Convert.ToInt32(btnGreen.text));
            btnPink.clickable.clicked += () => ButtonClicked("Pink Candy", Convert.ToInt32(btnPink.text));
            btnPurple.clickable.clicked += () => ButtonClicked("Purple Candy", Convert.ToInt32(btnPurple.text));
            btnRainbow.clickable.clicked += () => ButtonClicked("Rainbow Candy", Convert.ToInt32(btnRainbow.text));
            btnRed.clickable.clicked += () => ButtonClicked("Red Candy", Convert.ToInt32(btnRed.text));
            btnYellow.clickable.clicked += () => ButtonClicked("Yellow Candy", Convert.ToInt32(btnYellow.text));
            btnOther.clickable.clicked += () => ButtonClicked("Other", Convert.ToInt32(btnOther.text));

            logger.LogDebug("UIControllerScript: Start() complete");
        }

        private void Update()
        {
            if (veMain.style.display == DisplayStyle.Flex && Keyboard.current.escapeKey.wasPressedThisFrame) { HideUI(); }
            if (showingUI)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                StartOfRound.Instance.localPlayerController.disableLookInput = true;
            }
        }

        public void ShowUI(Dictionary<string, int> CandyBag)
        {
            btnBlue.text = CandyBag["Blue Candy"].ToString();
            btnBlue.text = CandyBag["Green Candy"].ToString();
            btnBlue.text = CandyBag["Pink Candy"].ToString();
            btnBlue.text = CandyBag["Purple Candy"].ToString();
            btnBlue.text = CandyBag["Rainbow Candy"].ToString();
            btnBlue.text = CandyBag["Red Candy"].ToString();
            btnBlue.text = CandyBag["Yellow Candy"].ToString();
            btnBlue.text = CandyBag["Other Candy"].ToString();

            logger.LogDebug("Showing UI");
            showingUI = true;
            veMain.style.display = DisplayStyle.Flex;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            IngamePlayerSettings.Instance.playerInput.DeactivateInput();
            StartOfRound.Instance.localPlayerController.disableLookInput = true;
        }

        public void HideUI()
        {
            logger.LogDebug("Hiding UI");
            showingUI = false;
            veMain.style.display = DisplayStyle.None;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            IngamePlayerSettings.Instance.playerInput.ActivateInput();
            StartOfRound.Instance.localPlayerController.disableLookInput = false;
        }

        private void ButtonClicked(string candyName, int amount)
        {
            if (amount > 0)
            {
                GetComponent<CandyBagBehavior>().CandySelected(candyName);
            }
            HideUI();
        }
    }
}
