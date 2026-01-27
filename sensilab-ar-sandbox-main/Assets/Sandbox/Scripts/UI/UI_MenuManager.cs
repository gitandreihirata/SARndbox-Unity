//  
//  UI_MenuManager.cs
//
//	Copyright 2021 SensiLab, Monash University <sensilab@monash.edu>
//
//  This file is part of sensilab-ar-sandbox.
//
//  sensilab-ar-sandbox is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  sensilab-ar-sandbox is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with sensilab-ar-sandbox.  If not, see <https://www.gnu.org/licenses/>.
//

using System;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using static UnityEngine.EventSystems.EventTrigger;

namespace ARSandbox
{
    public class UI_MenuManager : MonoBehaviour
    {
        public CalibrationManager CalibrationManager;
        public ModeSelector ModeSelector;

        public GameObject UI_MainMenu;
        public GameObject UI_CalibrationMenu;
        public UI_ConfirmationPanel UI_ConfirmationPanel;
        public UI_SandboxSettings UI_SandboxSettings;
        public UI_SharingMenu UI_SharingMenu;
        public GameObject UI_WaterSimulation;
        public GameObject UI_GeologySimulation;
        public GameObject UI_FireSimulation;
        public FireSimulation.UI_FireSimulationMenu UI_FireSimulationMenu;
        public GameObject UI_WindSimulationMenu;
        public GameObject UI_CalibrationExitButton;

        public Text UI_MenuTitle;
        public GameObject UI_MenuTitlePanel;
        public GameObject UI_SandboxUIVisual;

        public GameObject UI_OnScreenKeyboardMenu;
        public GameObject UI_OnScreenNumpadMenu;
        public GameObject UI_TopographyBuilderMenu;
        public UI_OnScreenKeyboard UI_OnScreenKeyboard;
        public UI_OnScreenNumpad UI_OnScreenNumpad;

        public bool SandboxSettingsOpen { get; private set; }
        public bool SharingMenuOpen { get; private set; }

        private GameObject activeMenu;
        private bool initialCalibrationComplete;

        private Action<string> Action_AcceptKeyboardInput;
        private Action<int> Action_AcceptNumpadInput;

        private Action Action_CancelInput;

        //Software Manager
        public GameObject UI_KinectDetect;
        private S_LocalizationManager localizationManager;

        void Awake()
        {
            localizationManager = FindObjectOfType<S_LocalizationManager>();
        }
        void Start()
        {
            UI_MainMenu.SetActive(false);
            UI_CalibrationMenu.SetActive(false);
            UI_ConfirmationPanel.gameObject.SetActive(false);
            UI_SandboxSettings.gameObject.SetActive(false);
            UI_SharingMenu.gameObject.SetActive(false);
            UI_WaterSimulation.SetActive(false);
            UI_GeologySimulation.SetActive(false);
            UI_FireSimulation.SetActive(false);
            UI_OnScreenKeyboardMenu.SetActive(false);
            UI_OnScreenNumpadMenu.SetActive(false);
            UI_TopographyBuilderMenu.SetActive(false);
            UI_MenuTitlePanel.SetActive(false);
            UI_SandboxUIVisual.SetActive(false);
            UI_WindSimulationMenu.SetActive(false);
            UI_CalibrationExitButton.SetActive(false);
            //SoftwareManager
            UI_KinectDetect.SetActive(false);
            //
            CalibrationManager.OnCalibration += OnCalibration;
            CalibrationManager.OnCalibrationComplete += OnCalibrationComplete;

            initialCalibrationComplete = false;
        }

        private void Update()
        {
            CheckKinectConnection();
        }

        void CheckKinectConnection()
        {
            KinectSensor sensor = KinectSensor.GetDefault();
            if (sensor == null)
            {
                //Debug.LogError("Kinect sensor not found. Make sure Kinect is connected properly.");
                UI_KinectDetect.SetActive(true);
            }
            else
            {
                if (!sensor.IsAvailable)
                {
                    //Debug.LogError("Kinect sensor is not available. Make sure it's properly connected and powered on.");
                    UI_KinectDetect.SetActive(true);
                }
                else
                {
                    //Debug.Log("Kinect sensor is connected and available.");
                    UI_KinectDetect.SetActive(false);
                }
            }
        }

        private void OpenMenu(GameObject newActiveMenu)
        {

            activeMenu.SetActive(false);
            newActiveMenu.SetActive(true);
            activeMenu = newActiveMenu;
            if (SandboxSettingsOpen) CloseSandboxSettings();
            if (SharingMenuOpen) CloseSharingMenu();

            UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            if (newActiveMenu == UI_MainMenu)
            {
                UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.AddListener(CalibrationManager.StartCalibration);
                UI_CalibrationExitButton.GetComponentInChildren<Text>().text = localizationManager.LocateStringtoDatabase("Calibration_Start");
            } else
            {
                UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.AddListener(OpenMainMenu);
                UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.AddListener(ModeSelector.DisableCurrentMode);
                UI_CalibrationExitButton.GetComponentInChildren<Text>().text = localizationManager.LocateStringtoDatabase("ExitSimulation");
            }
        }

        public void OpenWindSimulation()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("Simulations_WindSimulation");
            OpenMenu(UI_WindSimulationMenu);
        }

        public void OpenTopographyBuilder()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("TopographyBuilder");
            OpenMenu(UI_TopographyBuilderMenu);
        }

        public void OpenGeologySimulation()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("Simulations_GeologySimulation");
            OpenMenu(UI_GeologySimulation);
        }

        public void OpenWaterSimulation()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("Simulations_WaterSimulation");
            OpenMenu(UI_WaterSimulation);
        }

        public void OpenFireSimulation()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("Simulations_FireSimulation");
            OpenMenu(UI_FireSimulation);
            UI_FireSimulationMenu.OpenMenu();
        }

        public void OpenMainMenu()
        {
            UI_MenuTitle.text = localizationManager.LocateStringtoDatabase("MainMenu");
            OpenMenu(UI_MainMenu);
        }

        public void OpenSandboxSettings(RectTransform RectTransform)
        {
            UI_SandboxSettings.SetExtraRectTransform(RectTransform);
            OpenSandboxSettings();
        }
        public void OpenSandboxSettings()
        {
            CloseSharingMenu();
            UI_SandboxSettings.OpenSandboxSettings();
            UI_SandboxSettings.gameObject.SetActive(true);
            SandboxSettingsOpen = true;
        }

        public void CloseSandboxSettings()
        {
            UI_SandboxSettings.gameObject.SetActive(false);
            SandboxSettingsOpen = false;
        }
        public void OpenSharingMenu()
        {
            CloseSandboxSettings();
            UI_SharingMenu.OpenSharingMenu();
            UI_SharingMenu.gameObject.SetActive(true);
            SharingMenuOpen = true;
        }
        public void CloseSharingMenu()
        {
            UI_SharingMenu.gameObject.SetActive(false);
            SharingMenuOpen = false;
        }
        public void OpenConfirmDesktopSavePanel(string captureName)
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpConfirmDesktopSavePanel(captureName);
        }
        public void OpenConfirmEmailPanel(string emailAddress, string captureName)
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpConfirmEmailPanel(emailAddress, captureName);
        }
        public void OpenCalibrationCancelPanel()
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpCalibrationCancelPanel();
        }

        public void OpenTopographyDeletePanel()
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpTopographyDeletePanel();
        }
        public void OpenGeologyFileDeletePanel(string filename)
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpGeologyFileDeletePanel(filename);
        }
        public void OpenGeologyFileSelectPanel(string filename)
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpGeologyFileSelectPanel(filename);
        }
        public void OpenGeologyFileReplacePanel(string filename)
        {
            UI_ConfirmationPanel.gameObject.SetActive(true);
            UI_ConfirmationPanel.SetUpGeologyFileReplacePanel(filename);
        }
        private void OnCalibration()
        {
            activeMenu.SetActive(false);
            UI_MenuTitlePanel.SetActive(false);
            UI_SandboxUIVisual.SetActive(false);
            UI_CalibrationExitButton.SetActive(false);

            UI_CalibrationMenu.SetActive(true);
            if (SandboxSettingsOpen) CloseSandboxSettings();
            if (SharingMenuOpen) CloseSharingMenu();
        }

        private void OnCalibrationComplete()
        {
            if (!initialCalibrationComplete)
            {
                UI_MainMenu.SetActive(true);

                activeMenu = UI_MainMenu;
                initialCalibrationComplete = true;

                UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
                UI_CalibrationExitButton.GetComponentInChildren<Button>().onClick.AddListener(CalibrationManager.StartCalibration);
                UI_CalibrationExitButton.GetComponentInChildren<Text>().text = localizationManager.LocateStringtoDatabase("Calibration_Start");

                //SoftwareManager
                UI_KinectDetect.SetActive(true);

            }
            else
            {
                UI_CalibrationMenu.SetActive(false);
                activeMenu.SetActive(true);
            }
            UI_MenuTitlePanel.SetActive(true);
            UI_SandboxUIVisual.SetActive(true);
            UI_CalibrationExitButton.SetActive(true);
            //SoftwareManager
            UI_KinectDetect.SetActive(false);
        }

        public void OpenOnScreenKeyboard(string inputTitleString, string inputFieldString, Action<string> Action_AcceptInput, Action Action_CancelInput, 
                                                                UI_OnScreenKeyboard.KeyboardType keyboardMode = UI_OnScreenKeyboard.KeyboardType.Normal,
                                                                int characterLimit = 32)
        {
            this.Action_AcceptKeyboardInput = Action_AcceptInput;
            this.Action_CancelInput = Action_CancelInput;

            UI_OnScreenKeyboard.SetUpKeyboard(inputTitleString, inputFieldString, 
                        Action_KeyboardAccept, Action_KeyboardCancel, keyboardMode, characterLimit);

            UI_OnScreenKeyboardMenu.SetActive(true);
        }

        public void OpenOnScreenNumpad(string inputTitleString, int inputNumber, Action<int> Action_AcceptInput, Action Action_CancelInput)
        {
            this.Action_AcceptNumpadInput = Action_AcceptInput;
            this.Action_CancelInput = Action_CancelInput;

            UI_OnScreenNumpad.SetUpNumpad(inputTitleString, inputNumber.ToString(), Action_NumpadAccept, Action_NumpadCancel);
            UI_OnScreenNumpadMenu.SetActive(true);
        }

        private void Action_KeyboardAccept(string inputString)
        {
            Action_AcceptKeyboardInput(inputString);
            UI_OnScreenKeyboardMenu.SetActive(false);
        }

        private void Action_KeyboardCancel()
        {
            Action_CancelInput();
            UI_OnScreenKeyboardMenu.SetActive(false);
        }

        private void Action_NumpadAccept(int outputNumber)
        {
            Action_AcceptNumpadInput(outputNumber);
            UI_OnScreenNumpadMenu.SetActive(false);
        }

        private void Action_NumpadCancel()
        {
            Action_CancelInput();
            UI_OnScreenNumpadMenu.SetActive(false);
        }
    }
}
