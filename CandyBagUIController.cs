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

        public Button btnBlue;
        public Button btnGreen;
        public Button btnPink;
        public Button btnPurple;
        public Button btnRainbow;
        public Button btnRed;
        public Button btnYellow;
        public Button btnBlack;

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

            btnBlack = root.Q<Button>("btnBlack");
            if (btnBlack == null) { logger.LogError("btnBlack not found."); return; }

            logger.LogDebug("Got Controls for UI");

            // Add event handlers
            btnBlue.clickable.clicked += () => ButtonClicked("Blue Candy", Convert.ToInt32(btnBlue.text));
            btnGreen.clickable.clicked += () => ButtonClicked("Green Candy", Convert.ToInt32(btnGreen.text));
            btnPink.clickable.clicked += () => ButtonClicked("Pink Candy", Convert.ToInt32(btnPink.text));
            btnPurple.clickable.clicked += () => ButtonClicked("Purple Candy", Convert.ToInt32(btnPurple.text));
            btnRainbow.clickable.clicked += () => ButtonClicked("Rainbow Candy", Convert.ToInt32(btnRainbow.text));
            btnRed.clickable.clicked += () => ButtonClicked("Red Candy", Convert.ToInt32(btnRed.text));
            btnYellow.clickable.clicked += () => ButtonClicked("Yellow Candy", Convert.ToInt32(btnYellow.text));
            btnBlack.clickable.clicked += () => ButtonClicked("Black Candy", Convert.ToInt32(btnBlack.text));

            btnBlue.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Blue Candy", Convert.ToInt32(btnBlue.text));
                }
            });

            btnGreen.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Green Candy", Convert.ToInt32(btnGreen.text));
                }
            });

            btnPink.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Pink Candy", Convert.ToInt32(btnPink.text));
                }
            });

            btnPurple.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Purple Candy", Convert.ToInt32(btnPurple.text));
                }
            });

            btnRainbow.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Rainbow Candy", Convert.ToInt32(btnRainbow.text));
                }
            });

            btnRed.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Red Candy", Convert.ToInt32(btnRed.text));
                }
            });

            btnYellow.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Yellow Candy", Convert.ToInt32(btnYellow.text));
                }
            });

            btnBlack.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    ButtonRightClicked("Black Candy", Convert.ToInt32(btnBlack.text));
                }
            });

            logger.LogDebug("UIControllerScript: Start() complete");
        }

        private void Update()
        {
            if (veMain.style.display == DisplayStyle.Flex && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)) { HideUI(); }
            /*if (showingUI)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                StartOfRound.Instance.localPlayerController.disableLookInput = true;
            }*/
        }

        public void ShowUI(Dictionary<string, int> CandyBag)
        {
            logger.LogDebug("Showing UI");
            veMain.style.display = DisplayStyle.Flex;

            btnBlue.text = CandyBag["Blue Candy"].ToString();
            btnGreen.text = CandyBag["Green Candy"].ToString();
            btnPink.text = CandyBag["Pink Candy"].ToString();
            btnPurple.text = CandyBag["Purple Candy"].ToString();
            btnRainbow.text = CandyBag["Rainbow Candy"].ToString();
            btnRed.text = CandyBag["Red Candy"].ToString();
            btnYellow.text = CandyBag["Yellow Candy"].ToString();
            btnBlack.text = CandyBag["Black Candy"].ToString();

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            StartOfRound.Instance.localPlayerController.disableMoveInput = true;
            StartOfRound.Instance.localPlayerController.disableInteract = true;
            StartOfRound.Instance.localPlayerController.disableLookInput = true;
        }

        public void HideUI()
        {
            logger.LogDebug("Hiding UI");
            veMain.style.display = DisplayStyle.None;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            StartOfRound.Instance.localPlayerController.disableMoveInput = false;
            StartOfRound.Instance.localPlayerController.disableInteract = false;
            StartOfRound.Instance.localPlayerController.disableLookInput = false;
        }

        private void ButtonClicked(string candyName, int amount)
        {
            logger.LogDebug("Button clicked");

            if (amount > 0)
            {
                GetComponent<CandyBagBehavior>().CandySelected(candyName, false);
                HideUI();
            }
        }

        private void ButtonRightClicked(string candyName, int amount)
        {
            logger.LogDebug("Button right clicked");

            if (amount > 0)
            {
                GetComponent<CandyBagBehavior>().CandySelected(candyName, true);
                HideUI();
            }
        }
    }
}
