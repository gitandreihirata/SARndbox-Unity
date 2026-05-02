using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARSandbox
{
    public class KinectHandTracker : MonoBehaviour
    {
        public KinectManager KinectManager;

        [Header("ARRASTE O ARQUIVO .BYTES AQUI")]
        public TextAsset ModelFile; 

        [Header("Configuração de Montagem do Kinect")]
        [Tooltip("Se o Kinect estiver deitado ou montado invertido, use: 90, 180 ou 270")]
        public int CameraRotationDegrees = 0;

        public int NumHands = 2;
        public bool HandDetected = false;
        public string CurrentGesture = "None";
        public Vector2 HandCenter = Vector2.zero; 
        public Vector2 IndexTipPosition = Vector2.zero; 

        private Mediapipe.Tasks.Vision.HandLandmarker.HandLandmarker _handLandmarker;
        private bool _isProcessing = false;
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
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, 
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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro MediaPipe: {e.Message}");
            }
        }

        private void Update()
        {
            if (_handLandmarker == null || KinectManager == null) return;

            Texture2D kinectTex = KinectManager.GetIRColorizedTexture(); 

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
                _isProcessing = false;
                yield break;
            }

            long timestamp = (long)(Time.timeSinceLevelLoad * 1000);
            if (timestamp <= _nextTimestamp) timestamp = _nextTimestamp + 1;
            _nextTimestamp = timestamp;

            // Envia a imagem rotacionada para a IA ler de forma "confortável"
            var imageOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: CameraRotationDegrees);

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

                // Lê as coordenadas que o MediaPipe achou na imagem rotacionada
                Vector2 rawHandCenter = new Vector2((float)landmarks[9].x, (float)landmarks[9].y);
                Vector2 rawIndexTip = new Vector2((float)landmarks[8].x, (float)landmarks[8].y);

                // Executa a Matemática Reversa (Des-Rotação) para a água cair no lugar físico correto
                HandCenter = UnrotateCoordinates(rawHandCenter, CameraRotationDegrees);
                IndexTipPosition = UnrotateCoordinates(rawIndexTip, CameraRotationDegrees);

                CurrentGesture = CalculateGesture(landmarks);
            }
            else
            {
                HandDetected = false;
                CurrentGesture = "None";
            }
        }

        // Função mágica que devolve as coordenadas para o mundo real baseado em como a câmera girou
        private Vector2 UnrotateCoordinates(Vector2 normalizedPos, int rotation)
        {
            float nx = normalizedPos.x;
            float ny = normalizedPos.y;

            if (rotation == 90) return new Vector2(ny, 1.0f - nx);
            if (rotation == 180) return new Vector2(1.0f - nx, 1.0f - ny);
            if (rotation == 270) return new Vector2(1.0f - ny, nx);
            
            return normalizedPos; // 0 graus (Original)
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