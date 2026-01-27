using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// NÃO coloque "using Mediapipe.Unity" aqui para evitar conflito com UI

namespace ARSandbox
{
    public class KinectHandTracker : MonoBehaviour
    {
        public KinectManager KinectManager;

        [Header("ARRASTE O ARQUIVO .BYTES AQUI")]
        public TextAsset ModelFile; 

        public int NumHands = 2;

        public bool HandDetected = false;
        public string CurrentGesture = "None";
        public Vector2 HandCenter = Vector2.zero; 
        public Vector2 IndexTipPosition = Vector2.zero; 

        private Mediapipe.Tasks.Vision.HandLandmarker.HandLandmarker _handLandmarker;
        private bool _isProcessing = false;
        
        // CORREÇÃO: Variável para controlar o tempo manualmente
        private long _nextTimestamp = 0; 

        private IEnumerator Start()
        {
            if (ModelFile == null)
            {
                Debug.LogError("ERRO: Arraste o arquivo 'hand_landmarker.bytes' para o Inspector!");
                yield break;
            }

            while (KinectManager == null || !KinectManager.StreamStarted()) yield return null;

            try 
            {
                var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, // Mantendo CPU por segurança
                    modelAssetBuffer: ModelFile.bytes
                );

                var runningMode = Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM;

                var options = new Mediapipe.Tasks.Vision.HandLandmarker.HandLandmarkerOptions(
                    baseOptions,
                    runningMode: runningMode,
                    numHands: NumHands,
                    minHandDetectionConfidence: 0.5f,
                    minHandPresenceConfidence: 0.5f,
                    minTrackingConfidence: 0.5f,
                    resultCallback: OnResultCallback
                );

                _handLandmarker = Mediapipe.Tasks.Vision.HandLandmarker.HandLandmarker.CreateFromOptions(options);
                Debug.Log("MediaPipe Carregado!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro MediaPipe: {e.Message}");
            }
        }

        private void Update()
        {
            if (_handLandmarker == null || KinectManager == null) return;

            Texture2D kinectTex = KinectManager.GetColorTexture(); 

            if (kinectTex != null && !_isProcessing)
            {
                StartCoroutine(ProcessFrame(kinectTex));
            }
        }

        private IEnumerator ProcessFrame(Texture2D texture)
        {
            _isProcessing = true;
            Mediapipe.Image image = null;

            try 
            {
                image = new Mediapipe.Image(texture);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro conversão imagem: {e.Message}");
                _isProcessing = false;
                yield break;
            }

            // --- CORREÇÃO DO TIMESTAMP ---
            // Em vez de usar DateTime.Now (que pode oscilar), incrementamos manualmente.
            // O MediaPipe só precisa que o número cresça.
            // Usamos milissegundos simulados baseados no tempo da Unity desde o início.
            long timestamp = (long)(Time.timeSinceLevelLoad * 1000);

            // Proteção extra: Se o frame for muito rápido e o tempo for igual ao anterior, adiciona 1ms
            if (timestamp <= _nextTimestamp)
            {
                timestamp = _nextTimestamp + 1;
            }
            _nextTimestamp = timestamp;

            var imageOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            // Bloco try-catch para capturar erros específicos de execução
            try 
            {
                _handLandmarker.DetectAsync(image, timestamp, imageOptions);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro no DetectAsync: {e.Message}");
            }

            yield return new WaitForEndOfFrame();

            if (image != null) ((System.IDisposable)image).Dispose();
            
            _isProcessing = false;
        }

        private void OnResultCallback(
            Mediapipe.Tasks.Vision.HandLandmarker.HandLandmarkerResult result, 
            Mediapipe.Image image, 
            long timestamp)
        {
            if (result.handLandmarks != null && result.handLandmarks.Count > 0)
            {
                HandDetected = true;
                var landmarks = result.handLandmarks[0].landmarks;

                HandCenter = new Vector2((float)landmarks[9].x, (float)landmarks[9].y);
                IndexTipPosition = new Vector2((float)landmarks[8].x, (float)landmarks[8].y);

                CurrentGesture = CalculateGesture(landmarks);
                Debug.Log(CurrentGesture);
            }
            else
            {
                HandDetected = false;
                CurrentGesture = "None";
            }
        }

        private void OnDestroy()
        {
            if (_handLandmarker != null)
            {
                _handLandmarker.Close();
                ((System.IDisposable)_handLandmarker).Dispose();
            }
        }

        private string CalculateGesture(List<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> lm)
        {
            bool indexOpen = lm[8].y < lm[6].y;
            bool middleOpen = lm[12].y < lm[10].y;
            bool ringOpen = lm[16].y < lm[14].y;
            bool pinkyOpen = lm[20].y < lm[18].y;

            float pinchDist = Vector2.Distance(new Vector2(lm[4].x, lm[4].y), new Vector2(lm[8].x, lm[8].y));
            if (pinchDist < 0.05f) return "Lasso_Grab";

            if (!indexOpen && !middleOpen && !ringOpen && !pinkyOpen) return "Closed_Fist";
            if (indexOpen && !middleOpen && !ringOpen && !pinkyOpen) return "Pointing";
            if (indexOpen && middleOpen && ringOpen && pinkyOpen) return "Open_Palm";
            if (indexOpen && middleOpen && !ringOpen && !pinkyOpen) return "Victory";
            if (indexOpen && !middleOpen && !ringOpen && pinkyOpen) return "ILoveYou";

            return "Unknown";
        }
    }
}