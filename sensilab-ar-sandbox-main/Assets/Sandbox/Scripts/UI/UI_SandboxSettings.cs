using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARSandbox
{
    public class UI_SandboxSettings : MonoBehaviour
    {

        //SoftwareManager
        private S_LocalizationManager localizationManager;
        //

        public Sandbox Sandbox;
        public TopographyLabelManager TopographyLabelManager;
        public UI_MenuManager UI_MenuManager;
        public HandInput HandInput;        
        
        public GameObject UI_GeneralSettings;
        public GameObject UI_LabelSettings;
        public GameObject UI_GesturesSettings;
        
        // Switching Menu Buttons
        public Image UI_GeneralSettingsBtn;
        public Image UI_LabelSettingsBtn;
        public Image UI_GesturesSettingsBtn;

        // General Settings 
        public Slider UI_ResolutionSlider;
        public Slider UI_ContourSlider;
        public Slider UI_MinorContourSlider;
        public Slider UI_ContourWidthSlider;
        
        // Label Settings
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
        public Toggle UI_UseMediaPipeToggle;
        public Slider UI_HandSizeSlider;    // Slider para OpenHandThreshold
        public Slider UI_HandHeightSlider;  // Slider para HeightOffsetFromSand

        private bool contourLabelsEnabled;
        private enum MenuOpen
        {
            General,
            ContourLabels,
            Gestures
        }
        private MenuOpen currentMenuOpen;

        void Awake()
        {
            localizationManager = FindObjectOfType<S_LocalizationManager>();
        }

        //private RectTransform RectTransform, ExtraRectTransform;
        void Start()
        {
            //RectTransform = GetComponent<RectTransform>();
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

            UI_ResolutionSlider.value = (int)Sandbox.SandboxResolution;
            UI_ContourSlider.value = Sandbox.MajorContourSpacing * 2;
            UI_MinorContourSlider.value = Sandbox.MinorContours;
            UI_ContourWidthSlider.value = Sandbox.ContourThickness / 30.0f;
            
            if (HandInput != null)
            {
                if (UI_HandSizeSlider != null) 
                    UI_HandSizeSlider.value = HandInput.OpenHandThreshold;
            
                if (UI_HandHeightSlider != null) 
                    UI_HandHeightSlider.value = HandInput.HeightOffsetFromSand;
                
                if (UI_UseMediaPipeToggle != null)
                {
                    // Importante: Removemos o listener temporariamente para não disparar o som de clique ou loop infinito
                    UI_UseMediaPipeToggle.onValueChanged.RemoveAllListeners();
                    
                    // Define o visual do checkbox (Ligado/Desligado) baseado na variável real
                    UI_UseMediaPipeToggle.isOn = HandInput.UseMediaPipe;
                    
                    // Reconecta o evento para quando o usuário clicar
                    UI_UseMediaPipeToggle.onValueChanged.AddListener(delegate { HandInput.UI_SetUseMediaPipe(UI_UseMediaPipeToggle.isOn); });
                }
            }

            UI_DynamicLabelColouringToggle.isOn = Sandbox.DynamicLabelColouring;
            UI_LabelsEnabledToggle.isOn = TopographyLabelManager.ContourLabelsEnabled;
            UI_LabelDensitySlider.value = TopographyLabelManager.LabelDensity * 100.0f;
            
            
        }

        public void UI_OpenGeneralSettings()
        {
            currentMenuOpen = MenuOpen.General;
            UI_GeneralSettings.SetActive(true);
            UI_LabelSettings.SetActive(false);
            UI_GesturesSettings.SetActive(false);
            
            UI_GeneralSettingsBtn.color = new Color(0.5f, 1, 0);
            UI_LabelSettingsBtn.color = new Color(1, 1, 1);
            UI_GesturesSettingsBtn.color = new Color(1, 1, 1);
        }

        public void UI_OpenLabelSettings()
        {
            contourLabelsEnabled = TopographyLabelManager.ContourLabelsEnabled;

            currentMenuOpen = MenuOpen.ContourLabels;
            UI_GeneralSettings.SetActive(false);
            UI_LabelSettings.SetActive(true);
            UI_GesturesSettings.SetActive(false);
            
            UI_GeneralSettingsBtn.color = new Color(1, 1, 1);
            UI_LabelSettingsBtn.color = new Color(0.5f, 1, 0);
            UI_GesturesSettingsBtn.color = new Color(1, 1, 1);

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
            {
                UI_SetConstantSpacing();
            } else
            {
                UI_SetEndElevationSpacing();
            }
        }

        public void UI_OpenGesturesSettings()
        {
            currentMenuOpen = MenuOpen.Gestures;

            UI_GeneralSettings.SetActive(false);
            UI_LabelSettings.SetActive(false);
            UI_GesturesSettings.SetActive(true);

            UI_GeneralSettingsBtn.color = new Color(1, 1, 1);
            UI_LabelSettingsBtn.color = new Color(1, 1, 1);
            UI_GesturesSettingsBtn.color = new Color(0.5f, 1, 0); // Verde para indicar ativo
        }
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
