using UnityEngine;
using TMPro; // Usado se você usar TextMeshPro
using UnityEngine.UI;

namespace ARSandbox
{
    public class S_DebugManager : MonoBehaviour
    {
        [Header("Referências dos Sistemas")]
        public HandInput handInput;
        public CalibrationManager calibrationManager;

        [Header("Painel Principal")]
        public GameObject DebugPanelRoot; 

        [Header("Textos na Tela")]
        public TMP_Text PerformanceText;
        public TMP_Text CalibrationText;
        public TMP_Text GesturesText;
        public TMP_Text WaterText;

        // Variáveis de Estado
        public bool ShowDebug { get; private set; }
        public bool ShowPerformance { get; private set; }
        public bool ShowCalibration { get; private set; }
        public bool ShowGestures { get; private set; }
        public bool ShowWater { get; private set; }

        // Variáveis para cálculo de FPS
        private float _minFps = Mathf.Infinity;
        private float _maxFps = 0f;
        private float _fpsTimer = 0f;

        void Start()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            ShowDebug = PlayerPrefs.GetInt("Debug_Master", 0) == 1;
            ShowPerformance = PlayerPrefs.GetInt("Debug_Perf", 0) == 1;
            ShowCalibration = PlayerPrefs.GetInt("Debug_Calib", 0) == 1;
            ShowGestures = PlayerPrefs.GetInt("Debug_Gest", 0) == 1;
            ShowWater = PlayerPrefs.GetInt("Debug_Water", 0) == 1;

            UpdateUI();
        }

        public void UpdateUI()
        {
            if (DebugPanelRoot != null) DebugPanelRoot.SetActive(ShowDebug);

            if (PerformanceText != null) PerformanceText.gameObject.SetActive(ShowPerformance);
            if (CalibrationText != null) CalibrationText.gameObject.SetActive(ShowCalibration);
            if (GesturesText != null) GesturesText.gameObject.SetActive(ShowGestures);
            if (WaterText != null) WaterText.gameObject.SetActive(ShowWater);

            // Reseta o FPS Min/Max quando liga/desliga o menu de performance
            if (ShowPerformance) 
            {
                _minFps = Mathf.Infinity;
                _maxFps = 0f;
            }
        }

        // --- FUNÇÕES CHAMADAS PELO MENU ---
        public void SetShowDebug(bool val) { ShowDebug = val; PlayerPrefs.SetInt("Debug_Master", val ? 1 : 0); PlayerPrefs.Save(); UpdateUI(); }
        public void SetShowPerformance(bool val) { ShowPerformance = val; PlayerPrefs.SetInt("Debug_Perf", val ? 1 : 0); PlayerPrefs.Save(); UpdateUI(); }
        public void SetShowCalibration(bool val) { ShowCalibration = val; PlayerPrefs.SetInt("Debug_Calib", val ? 1 : 0); PlayerPrefs.Save(); UpdateUI(); }
        public void SetShowGestures(bool val) { ShowGestures = val; PlayerPrefs.SetInt("Debug_Gest", val ? 1 : 0); PlayerPrefs.Save(); UpdateUI(); }
        public void SetShowWater(bool val) { ShowWater = val; PlayerPrefs.SetInt("Debug_Water", val ? 1 : 0); PlayerPrefs.Save(); UpdateUI(); }

        // --- ATUALIZAÇÃO DOS VALORES NA TELA EM TEMPO REAL ---
        void Update()
        {
            if (!ShowDebug) return;

            // 1. PERFORMANCE (FPS)
            if (ShowPerformance && PerformanceText != null)
            {
                float fps = 1.0f / Time.unscaledDeltaTime;
                
                // Ignora quedas absurdas nos primeiros frames de carregamento
                if (Time.timeSinceLevelLoad > 2.0f) 
                {
                    if (fps < _minFps) _minFps = fps;
                    if (fps > _maxFps) _maxFps = fps;
                }

                _fpsTimer += Time.unscaledDeltaTime;
                if (_fpsTimer > 0.5f) // Atualiza o texto apenas 2 vezes por segundo para não piscar
                {
                    PerformanceText.text = $"[PERFORMANCE]\nFPS Atual: {Mathf.Round(fps)}\nFPS Min: {Mathf.Round(_minFps == Mathf.Infinity ? 0 : _minFps)}\nFPS Max: {Mathf.Round(_maxFps)}";
                    _fpsTimer = 0f;
                }
            }

            // 2. GESTURES
            if (ShowGestures && GesturesText != null)
            {
                if (handInput != null)
                {
                    string modo = handInput.UseMediaPipe ? "MediaPipe (IA)" : "Nativo (Altura/Blobo)";
                    GesturesText.text = $"[GESTURES]\nModo Ativo: {modo}\nGesto Atual: {handInput.DebugCurrentGesture}\nAltura da Mão: {handInput.DebugHandHeight} mm\nPixels Detectados: {handInput.CurrentPixelCount}";
                }
                else
                {
                    GesturesText.text = "[GESTURES]\nAviso: HandInput não vinculado no Inspector!";
                }
            }

            // 3. CALIBRATION
            if (ShowCalibration && CalibrationText != null)
            {
                if (calibrationManager != null && handInput != null)
                {
                    float maxDepth = calibrationManager.GetCalibrationDescriptor().MaxDepth;
                    float baseChao = maxDepth - handInput.HeightOffsetFromSand;
                    float limiteTeto = baseChao - handInput.InteractionZoneHeight;

                    CalibrationText.text = $"[CALIBRATION]\nFundo da Caixa: {maxDepth} mm\nZona de Gestos Começa em: {baseChao} mm\nZona de Gestos Termina em: {limiteTeto} mm";
                }
                else
                {
                    CalibrationText.text = "[CALIBRATION]\nAviso: Managers não vinculados!";
                }
            }
        }
    }
}