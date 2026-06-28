using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSandbox
{
    public class UI_SandboxSettings : MonoBehaviour
    {
        private S_LocalizationManager localizationManager;

        public Sandbox Sandbox;
        public TopographyLabelManager TopographyLabelManager;
        public UI_MenuManager UI_MenuManager;
        public HandInput HandInput;        
        
        [Header("Menu Panels")]
        public GameObject UI_GeneralSettings;
        public GameObject UI_LabelSettings;
        public GameObject UI_GesturesSettings;
        public GameObject UI_CloudsSettings;
        public GameObject UI_SoundsSettings;
        public GameObject UI_DebugSettings; 
        
        [Header("Switching Menu Buttons")]
        public Image UI_GeneralSettingsBtn;
        public Image UI_LabelSettingsBtn;
        public Image UI_GesturesSettingsBtn;
        public Image UI_CloudsSettingsBtn;
        public Image UI_SoundsSettingsBtn; 
        public Image UI_DebugSettingsBtn; 

        [Header("General Settings")]
        public Slider UI_ResolutionSlider;
        public Slider UI_ContourSlider;
        public Slider UI_MinorContourSlider;
        public Slider UI_ContourWidthSlider;
        
        [Header("Label / Contour Settings")]
        public GameObject[] UI_ContourLabelMenuItems;
        public Slider UI_LabelDensitySlider;
        public Toggle UI_LabelsEnabledToggle;
        public Toggle UI_DynamicLabelColouringToggle;
        public Image UI_ConstSpacingModeBG;
        public Image UI_MaxElevationModeBG;
        public Button UI_ConstSpacingModeBtn;
        public Button UI_MaxElevationModeBtn;
        public Text UI_SpacingModeText;
        public RectTransform UI_SpacingModeBG;
        
        public UI_NumpadInput UI_StartingElevationBtn;
        public UI_NumpadInput UI_ElevationSpacingBtn;
        
        [Header("Gestures Settings")]
        public Toggle UI_EnableGesturesToggle; 
        public Toggle UI_UseMediaPipeToggle;
        public Slider UI_HandSizeSlider;
        public Slider UI_HandHeightSlider;
        public Slider UI_InteractionZoneSlider; 
        public TMP_Dropdown UI_CameraRotationDropdown;

        [Header("Clouds Settings")]
        public Toggle UI_EnableCloudsToggle;
        public Slider UI_MaxWaterfallsSlider;
        public Slider UI_CloudMinSizeSlider; 
        public Slider UI_CloudMaxSizeSlider; 

        [Header("Sounds Settings")]
        public Toggle UI_EnableSoundsToggle; 
        public Slider UI_SoundVolumeSlider; 

        [Header("Debug Settings")]
        public Toggle UI_ShowDebugToggle;        
        public Toggle UI_ShowPerformanceToggle;  
        public Toggle UI_ShowCalibrationToggle;  
        public Toggle UI_ShowGesturesToggle;     
        public Toggle UI_ShowWaterToggle;        

        private bool contourLabelsEnabled;
        private enum MenuOpen
        {
            General,
            ContourLabels,
            Gestures,
            Clouds,     
            Sounds,
            Debug       
        }
        private MenuOpen currentMenuOpen;

        void Awake()
        {
            localizationManager = FindObjectOfType<S_LocalizationManager>();
        }

        void Start()
        {
            currentMenuOpen = MenuOpen.General;
            contourLabelsEnabled = false;

            UI_StartingElevationBtn.SetAcceptAction(Accept_StartingElevation);
            UI_ElevationSpacingBtn.SetAcceptAction(Accept_ElevationSpacing);
        }

        private bool Accept_StartingElevation(int height)
        {
            if (TopographyLabelManager.ElevationSpacingMode == TopographyLabelManager.ElevationSpacingType.ConstantSpacing)
            {
                TopographyLabelManager.SetStartingElevation(height);
                return true;
            } else
            {
                if (height < TopographyLabelManager.EndingElevation)
                {
                    TopographyLabelManager.SetStartingElevation(height);
                    return true;
                }
                return false;
            }
        }

        private bool Accept_ElevationSpacing(int height)
        {
            if (TopographyLabelManager.ElevationSpacingMode == TopographyLabelManager.ElevationSpacingType.ConstantSpacing)
            {
                if (height > 0)
                {
                    TopographyLabelManager.SetElevationSpacing(height);
                    return true;
                }
                return false;
            } else
            {
                if (height > TopographyLabelManager.StartingElevation)
                {
                    TopographyLabelManager.SetEndingElevation(height);
                    return true;
                }
                return false;
            }
        }

        void Update()
        {
            /*foreach(Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, touch.position))
                    {
                        if (ExtraRectTransform == null || !RectTransformUtility.RectangleContainsScreenPoint(ExtraRectTransform, touch.position)) {
                            UI_MenuManager.CloseSandboxSettings();
                        }
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if(!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, Input.mousePosition))
                {
                    if (ExtraRectTransform == null || !RectTransformUtility.RectangleContainsScreenPoint(ExtraRectTransform, Input.mousePosition))
                    {
                        UI_MenuManager.CloseSandboxSettings();
                    }
                }
            }*/
        }

        public void SetExtraRectTransform(RectTransform ExtraRectTransform)
        {
            //this.ExtraRectTransform = ExtraRectTransform;
        }
        
        public void OpenSandboxSettings()
        {
            if (currentMenuOpen == MenuOpen.General) UI_OpenGeneralSettings();
            else if (currentMenuOpen == MenuOpen.ContourLabels) UI_OpenLabelSettings();
            else if (currentMenuOpen == MenuOpen.Gestures) UI_OpenGesturesSettings();
            else if (currentMenuOpen == MenuOpen.Clouds) UI_OpenCloudsSettings();
            else if (currentMenuOpen == MenuOpen.Sounds) UI_OpenSoundsSettings();
            else if (currentMenuOpen == MenuOpen.Debug) UI_OpenDebugSettings();

            UI_ResolutionSlider.value = (int)Sandbox.SandboxResolution;
            UI_ContourSlider.value = Sandbox.MajorContourSpacing * 2;
            UI_MinorContourSlider.value = Sandbox.MinorContours;
            UI_ContourWidthSlider.value = Sandbox.ContourThickness / 30.0f;
            
            if (HandInput != null)
            {
                if (UI_EnableGesturesToggle != null)
                {
                    UI_EnableGesturesToggle.onValueChanged.RemoveAllListeners();
                    UI_EnableGesturesToggle.isOn = HandInput.EnableGestures;
                    UI_EnableGesturesToggle.onValueChanged.AddListener(delegate { HandInput.UI_SetEnableGestures(UI_EnableGesturesToggle.isOn); });
                }

                if (UI_HandSizeSlider != null) 
                    UI_HandSizeSlider.value = HandInput.OpenHandThreshold;
            
                if (UI_HandHeightSlider != null) 
                    UI_HandHeightSlider.value = HandInput.HeightOffsetFromSand;
                
                if (UI_InteractionZoneSlider != null) 
                {
                    UI_InteractionZoneSlider.onValueChanged.RemoveAllListeners();
                    UI_InteractionZoneSlider.value = HandInput.InteractionZoneHeight;
                    UI_InteractionZoneSlider.onValueChanged.AddListener(delegate { HandInput.UI_SetInteractionZoneHeight(UI_InteractionZoneSlider.value); });
                }
                
                if (UI_UseMediaPipeToggle != null)
                {
                    UI_UseMediaPipeToggle.onValueChanged.RemoveAllListeners();
                    UI_UseMediaPipeToggle.isOn = HandInput.UseMediaPipe;
                    UI_UseMediaPipeToggle.onValueChanged.AddListener(delegate { HandInput.UI_SetUseMediaPipe(UI_UseMediaPipeToggle.isOn); });
                }
                
                if (UI_CameraRotationDropdown != null && HandInput.MediaPipeTracker != null)
                {
                    UI_CameraRotationDropdown.onValueChanged.RemoveAllListeners();
                    
                    int currentRot = HandInput.MediaPipeTracker.CameraRotationDegrees;
                    if (currentRot == 90) UI_CameraRotationDropdown.value = 1;
                    else if (currentRot == 180) UI_CameraRotationDropdown.value = 2;
                    else if (currentRot == 270) UI_CameraRotationDropdown.value = 3;
                    else UI_CameraRotationDropdown.value = 0;
                    
                    UI_CameraRotationDropdown.onValueChanged.AddListener(delegate { HandInput.UI_SetCameraRotation(UI_CameraRotationDropdown.value); });
                }
            }

            WaterSimulation.WaterSimulation[] waterSims = Resources.FindObjectsOfTypeAll<WaterSimulation.WaterSimulation>();
            if (waterSims != null && waterSims.Length > 0)
            {
                WaterSimulation.WaterSimulation waterSim = waterSims[0];


                if (PlayerPrefs.HasKey("EnableClouds")) waterSim.EnableClouds = PlayerPrefs.GetInt("EnableClouds") == 1;
                if (PlayerPrefs.HasKey("MinCloudParticleSize")) waterSim.MinCloudParticleSize = PlayerPrefs.GetFloat("MinCloudParticleSize");
                if (PlayerPrefs.HasKey("MaxCloudParticleSize")) waterSim.MaxCloudParticleSize = PlayerPrefs.GetFloat("MaxCloudParticleSize");
                if (PlayerPrefs.HasKey("EnableSounds")) waterSim.EnableSounds = PlayerPrefs.GetInt("EnableSounds") == 1;
                if (PlayerPrefs.HasKey("SoundVolume")) waterSim.SoundVolume = PlayerPrefs.GetFloat("SoundVolume");
                if (PlayerPrefs.HasKey("MaxWaterfalls")) waterSim.MaxWaterfalls = PlayerPrefs.GetInt("MaxWaterfalls");

                if (UI_EnableCloudsToggle != null) 
                {
                    UI_EnableCloudsToggle.onValueChanged.RemoveAllListeners();
                    UI_EnableCloudsToggle.isOn = waterSim.EnableClouds;
                    UI_EnableCloudsToggle.onValueChanged.AddListener(delegate { waterSim.UI_SetEnableClouds(UI_EnableCloudsToggle.isOn); });
                }
                if (UI_CloudMinSizeSlider != null)
                {
                    UI_CloudMinSizeSlider.onValueChanged.RemoveAllListeners();
                    UI_CloudMinSizeSlider.value = waterSim.MinCloudParticleSize;
                    UI_CloudMinSizeSlider.onValueChanged.AddListener(delegate { waterSim.UI_SetCloudMinSize(UI_CloudMinSizeSlider.value); });
                }
                if (UI_CloudMaxSizeSlider != null)
                {
                    UI_CloudMaxSizeSlider.onValueChanged.RemoveAllListeners();
                    UI_CloudMaxSizeSlider.value = waterSim.MaxCloudParticleSize;
                    UI_CloudMaxSizeSlider.onValueChanged.AddListener(delegate { waterSim.UI_SetCloudMaxSize(UI_CloudMaxSizeSlider.value); });
                }
                
                if (UI_MaxWaterfallsSlider != null)
                {
                    UI_MaxWaterfallsSlider.onValueChanged.RemoveAllListeners();
                    UI_MaxWaterfallsSlider.value = waterSim.MaxWaterfalls;
                    UI_MaxWaterfallsSlider.onValueChanged.AddListener(delegate { waterSim.UI_SetMaxWaterfalls(UI_MaxWaterfallsSlider.value); });
                }

                if (UI_EnableSoundsToggle != null) 
                {
                    UI_EnableSoundsToggle.onValueChanged.RemoveAllListeners();
                    UI_EnableSoundsToggle.isOn = waterSim.EnableSounds;
                    UI_EnableSoundsToggle.onValueChanged.AddListener(delegate { waterSim.UI_SetEnableSounds(UI_EnableSoundsToggle.isOn); });
                }
                if (UI_SoundVolumeSlider != null)
                {
                    UI_SoundVolumeSlider.onValueChanged.RemoveAllListeners();
                    UI_SoundVolumeSlider.value = waterSim.SoundVolume;
                    UI_SoundVolumeSlider.onValueChanged.AddListener(delegate { waterSim.UI_SetSoundVolume(UI_SoundVolumeSlider.value); });
                }
            }

            S_DebugManager[] debugManagers = Resources.FindObjectsOfTypeAll<S_DebugManager>();
            if (debugManagers != null && debugManagers.Length > 0)
            {
                S_DebugManager dbg = debugManagers[0];

                if (UI_ShowDebugToggle != null)
                {
                    UI_ShowDebugToggle.onValueChanged.RemoveAllListeners();
                    UI_ShowDebugToggle.isOn = dbg.ShowDebug;
                    UI_ShowDebugToggle.onValueChanged.AddListener(delegate { dbg.SetShowDebug(UI_ShowDebugToggle.isOn); });
                }
                if (UI_ShowPerformanceToggle != null)
                {
                    UI_ShowPerformanceToggle.onValueChanged.RemoveAllListeners();
                    UI_ShowPerformanceToggle.isOn = dbg.ShowPerformance;
                    UI_ShowPerformanceToggle.onValueChanged.AddListener(delegate { dbg.SetShowPerformance(UI_ShowPerformanceToggle.isOn); });
                }
                if (UI_ShowCalibrationToggle != null)
                {
                    UI_ShowCalibrationToggle.onValueChanged.RemoveAllListeners();
                    UI_ShowCalibrationToggle.isOn = dbg.ShowCalibration;
                    UI_ShowCalibrationToggle.onValueChanged.AddListener(delegate { dbg.SetShowCalibration(UI_ShowCalibrationToggle.isOn); });
                }
                if (UI_ShowGesturesToggle != null)
                {
                    UI_ShowGesturesToggle.onValueChanged.RemoveAllListeners();
                    UI_ShowGesturesToggle.isOn = dbg.ShowGestures;
                    UI_ShowGesturesToggle.onValueChanged.AddListener(delegate { dbg.SetShowGestures(UI_ShowGesturesToggle.isOn); });
                }
                if (UI_ShowWaterToggle != null)
                {
                    UI_ShowWaterToggle.onValueChanged.RemoveAllListeners();
                    UI_ShowWaterToggle.isOn = dbg.ShowWater;
                    UI_ShowWaterToggle.onValueChanged.AddListener(delegate { dbg.SetShowWater(UI_ShowWaterToggle.isOn); });
                }
            }

            UI_DynamicLabelColouringToggle.isOn = Sandbox.DynamicLabelColouring;
            UI_LabelsEnabledToggle.isOn = TopographyLabelManager.ContourLabelsEnabled;
            UI_LabelDensitySlider.value = TopographyLabelManager.LabelDensity * 100.0f;
        }

        // --- FUNÇÕES DE NAVEGAÇÃO ENTRE ABAS ---
        private void ResetTabs()
        {
            if (UI_GeneralSettings) UI_GeneralSettings.SetActive(false);
            if (UI_LabelSettings) UI_LabelSettings.SetActive(false);
            if (UI_GesturesSettings) UI_GesturesSettings.SetActive(false);
            if (UI_CloudsSettings) UI_CloudsSettings.SetActive(false);
            if (UI_SoundsSettings) UI_SoundsSettings.SetActive(false);
            if (UI_DebugSettings) UI_DebugSettings.SetActive(false);

            Color inactiveColor = new Color(1, 1, 1);
            if (UI_GeneralSettingsBtn) UI_GeneralSettingsBtn.color = inactiveColor;
            if (UI_LabelSettingsBtn) UI_LabelSettingsBtn.color = inactiveColor;
            if (UI_GesturesSettingsBtn) UI_GesturesSettingsBtn.color = inactiveColor;
            if (UI_CloudsSettingsBtn) UI_CloudsSettingsBtn.color = inactiveColor;
            if (UI_SoundsSettingsBtn) UI_SoundsSettingsBtn.color = inactiveColor;
            if (UI_DebugSettingsBtn) UI_DebugSettingsBtn.color = inactiveColor;
        }

        public void UI_OpenGeneralSettings()
        {
            currentMenuOpen = MenuOpen.General;
            ResetTabs();
            if (UI_GeneralSettings) UI_GeneralSettings.SetActive(true);
            if (UI_GeneralSettingsBtn) UI_GeneralSettingsBtn.color = new Color(0.5f, 1, 0);
        }

        public void UI_OpenLabelSettings() 
        {
            contourLabelsEnabled = TopographyLabelManager.ContourLabelsEnabled;
            currentMenuOpen = MenuOpen.ContourLabels;
            ResetTabs();
            if (UI_LabelSettings) UI_LabelSettings.SetActive(true);
            if (UI_LabelSettingsBtn) UI_LabelSettingsBtn.color = new Color(0.5f, 1, 0);

            foreach(GameObject menuItem in UI_ContourLabelMenuItems)
            {
                menuItem.SetActive(contourLabelsEnabled);
            }

            UI_MaxElevationModeBtn.interactable = !TopographyLabelManager.ElevationLabelsForced;
            UI_ConstSpacingModeBtn.interactable = !TopographyLabelManager.ElevationLabelsForced;
            UI_LabelsEnabledToggle.interactable = true;
            UI_StartingElevationBtn.SetInteractable(!TopographyLabelManager.ElevationLabelsForced);
            UI_ElevationSpacingBtn.SetInteractable(!TopographyLabelManager.ElevationLabelsForced);

            UI_StartingElevationBtn.SetNumber(TopographyLabelManager.StartingElevation);

            if (TopographyLabelManager.ElevationSpacingMode == TopographyLabelManager.ElevationSpacingType.ConstantSpacing)
                UI_SetConstantSpacing();
            else
                UI_SetEndElevationSpacing();
        }

        public void UI_OpenGesturesSettings()
        {
            currentMenuOpen = MenuOpen.Gestures;
            ResetTabs();
            if (UI_GesturesSettings) UI_GesturesSettings.SetActive(true);
            if (UI_GesturesSettingsBtn) UI_GesturesSettingsBtn.color = new Color(0.5f, 1, 0);
        }

        public void UI_OpenCloudsSettings()
        {
            currentMenuOpen = MenuOpen.Clouds;
            ResetTabs();
            if (UI_CloudsSettings) UI_CloudsSettings.SetActive(true);
            if (UI_CloudsSettingsBtn) UI_CloudsSettingsBtn.color = new Color(0.5f, 1, 0);
        }

        public void UI_OpenSoundsSettings()
        {
            currentMenuOpen = MenuOpen.Sounds;
            ResetTabs();
            if (UI_SoundsSettings) UI_SoundsSettings.SetActive(true);
            if (UI_SoundsSettingsBtn) UI_SoundsSettingsBtn.color = new Color(0.5f, 1, 0);
        }

        public void UI_OpenDebugSettings()
        {
            currentMenuOpen = MenuOpen.Debug;
            ResetTabs();
            if (UI_DebugSettings) UI_DebugSettings.SetActive(true);
            if (UI_DebugSettingsBtn) UI_DebugSettingsBtn.color = new Color(0.5f, 1, 0);
        }

        // --- MÉTODOS ORIGINAIS MANTIDOS ---
        public void UI_ToggleContourLabels(bool toggleVal)
        {
            contourLabelsEnabled = toggleVal;
            foreach (GameObject menuItem in UI_ContourLabelMenuItems)
            {
                menuItem.SetActive(contourLabelsEnabled);
            }
            TopographyLabelManager.UI_ToggleContourLabels(toggleVal);
        }

        public void UI_SetConstantSpacing()
        {
            UI_ConstSpacingModeBG.color = new Color(0.5f, 1, 0);
            UI_MaxElevationModeBG.color = new Color(1, 1, 1);

            TopographyLabelManager.SetElevationSpacingMode(TopographyLabelManager.ElevationSpacingType.ConstantSpacing);
            UI_ElevationSpacingBtn.SetNumber(TopographyLabelManager.ElevationConstSpacing);

            UI_SpacingModeText.text = localizationManager.LocateStringtoDatabase("SandboxSettings_StartingElevationStepSize");
            UI_SpacingModeBG.sizeDelta = new Vector2(283, UI_SpacingModeBG.sizeDelta.y);
        }

        public void UI_SetEndElevationSpacing()
        {
            if (TopographyLabelManager.EndingElevation < TopographyLabelManager.StartingElevation)
            {
                TopographyLabelManager.SetEndingElevation(TopographyLabelManager.StartingElevation + 1000);
            }
            UI_ConstSpacingModeBG.color = new Color(1, 1, 1); 
            UI_MaxElevationModeBG.color = new Color(0.5f, 1, 0);

            TopographyLabelManager.SetElevationSpacingMode(TopographyLabelManager.ElevationSpacingType.EndElevationSpacing);
            UI_ElevationSpacingBtn.SetNumber(TopographyLabelManager.EndingElevation);

            UI_SpacingModeText.text = localizationManager.LocateStringtoDatabase("SandboxSettings_StartingElevationEndingElevation");
            UI_SpacingModeBG.sizeDelta = new Vector2(342, UI_SpacingModeBG.sizeDelta.y);
        }
    }
}