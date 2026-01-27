using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using TMPro;
using UnityEngine.UI;

namespace ARSandbox
{
    public class HandInput : MonoBehaviour
    {

        [Header("Modo de Operação")]
        public bool UseMediaPipe = false; 
        
        [Header("Referências")]
        public Sandbox Sandbox;
        public CalibrationManager CalibrationManager;
        public KinectManager KinectManager;
        public Camera SandboxUICamera;
        public LayerMask SandboxLayerMask;
        
        public KinectHandTracker MediaPipeTracker; 

        [Header("Interações IA (MediaPipe)")]
        public GameObject CloudPrefab;
        public float CloudSpawnCooldown = 2.0f;

        [Header("Configuração Automática (Modo Nativo)")]
        public float HeightOffsetFromSand = 50.0f; 
        public float InteractionZoneHeight = 400.0f;

        [Header("Calibração de Gesto (Modo Nativo)")]
        public int MinPixelsPresent = 50; 
        public int OpenHandThreshold = 350;

        [Header("Debug Info (Read Only)")] 
        public TMP_Text  UITextDebugCurrentGesturel;
        public string DebugCurrentGesture = "Nenhum";
        public float DebugHandHeight = 0;
        public float DetectedSandDepth;
        public int CurrentPixelCount;

        private List<HandInputGesture> CurrentGestures;
        private float _cloudTimer = 0f;
        private GameObject _grabbedObject = null;

        public delegate void OnGesturesReady_Delegate();
        public static OnGesturesReady_Delegate OnGesturesReady;
        private bool IsCalibrating;
        
        void Start()
        {
            // --- CARREGAR CONFIGURAÇÃO SALVA ---
            // Verifica se existe uma configuração salva. Se for 1, liga. Se 0, desliga.
            if (PlayerPrefs.HasKey("UseMediaPipe"))
            {
                UseMediaPipe = PlayerPrefs.GetInt("UseMediaPipe") == 1;
            }

            IsCalibrating = true;
            CalibrationManager.OnCalibration += OnCalibration;
            Sandbox.OnSandboxReady += OnSandboxReady;
            Sandbox.OnNewProcessedData += OnNewProcessedData;
            CurrentGestures = new List<HandInputGesture>();
        }
        
        private void OnCalibration() { IsCalibrating = true; }
        private void OnSandboxReady() { IsCalibrating = false; }

        private void OnNewProcessedData()
        {
            if (!IsCalibrating)
            {
                CurrentGestures.RemoveAll(x => !x.IsUIGesture);

                // --- AQUI ESTÁ A "CHAVE DE DESVIO" ---
                // Se um está ligado, o outro NÃO roda.
                if (UseMediaPipe)
                {
                    ProcessMediaPipeDetection();
                }
                else
                {
                    ProcessAutomaticHandDetection();
                }

                if (OnGesturesReady != null) OnGesturesReady();
            }
        }

        // =================================================================================
        // MODO 1: INTELIGÊNCIA ARTIFICIAL (MEDIAPIPE)
        // =================================================================================
        private void ProcessMediaPipeDetection()
        {
            // 1. Validação Básica
            if (MediaPipeTracker == null || !MediaPipeTracker.HandDetected) 
            {
                DebugCurrentGesture = "Mão não detectada";
                UITextDebugCurrentGesturel.text = DebugCurrentGesture;
                _grabbedObject = null;
                return;
            }

            // 2. Obter Dados do Kinect (Para checar altura)
            ushort[] depthData = KinectManager.GetCurrentData();
            if (depthData == null || depthData.Length == 0) return;
            Point frameSize = KinectManager.GetKinectFrameSize();
            
            // 3. Converter Posição da Palma (Normalizado -> Pixels)
            Vector2 palmNorm = MediaPipeTracker.HandCenter;
            int palmX = (int)(palmNorm.x * frameSize.x);
            int palmY = (int)(palmNorm.y * frameSize.y);

            // Clamp para evitar erro de array fora do limite
            palmX = Mathf.Clamp(palmX, 0, frameSize.x - 1);
            palmY = Mathf.Clamp(palmY, 0, frameSize.y - 1);

            // --- FILTRO DE ALTURA (NOVO) ---
            
            // Ler a profundidade real no pixel onde a mão está
            int index = palmY * frameSize.x + palmX;
            ushort handDepth = depthData[index];
            DebugHandHeight = handDepth; // Mostra no Inspector

            // Calcular os limites baseados nos Sliders
            float sandDepth = CalibrationManager.GetCalibrationDescriptor().MaxDepth;
            float floorLimit = sandDepth - HeightOffsetFromSand; // Perto da areia
            float ceilingLimit = floorLimit - InteractionZoneHeight; // Perto da câmera

            // Se a profundidade for 0 (erro de leitura) ou fora dos limites, IGNORA
            // Nota: Depth menor = mais alto (perto da câmera). Depth maior = mais baixo.
            if (handDepth == 0 || handDepth > floorLimit || handDepth < ceilingLimit)
            {
                DebugCurrentGesture = "Mão Fora da Zona (Altura)";
                UITextDebugCurrentGesturel.text = DebugCurrentGesture;
                _grabbedObject = null;
                return; // Sai da função aqui
            }

            // ----------------------------------------------------

            string gesture = MediaPipeTracker.CurrentGesture;
            DebugCurrentGesture = gesture; // Mostra o gesto real
            UITextDebugCurrentGesturel.text = DebugCurrentGesture;
            
            Vector3 palmWorldPos = GetWorldPosFromDepth(palmX, palmY);

            // Posição do Indicador (Para Lasso)
            Vector2 tipNorm = MediaPipeTracker.IndexTipPosition;
            int tipX = Mathf.Clamp((int)(tipNorm.x * frameSize.x), 0, frameSize.x - 1);
            int tipY = Mathf.Clamp((int)(tipNorm.y * frameSize.y), 0, frameSize.y - 1);
            Vector3 tipWorldPos = GetWorldPosFromDepth(tipX, tipY);

            // A) OPEN PALM -> Água
            if (gesture == "Open_Palm")
            {
                _grabbedObject = null;
                CreateGestureAtDepthPoint(palmX, palmY); 
            }
            // B) CLOSED FIST -> Nuvem
            else if (gesture == "Closed_Fist")
            {
                _grabbedObject = null;
                _cloudTimer -= Time.deltaTime;

                if (_cloudTimer <= 0)
                {
                    SpawnCloud(palmWorldPos);
                    _cloudTimer = CloudSpawnCooldown;
                }
            }
            // C) LASSO -> Mover Objetos
            else if (gesture == "Lasso_Grab")
            {
                if (_grabbedObject == null)
                {
                    TryGrabObject(tipWorldPos);
                }
                else
                {
                    _grabbedObject.transform.position = new Vector3(tipWorldPos.x, tipWorldPos.y, _grabbedObject.transform.position.z);
                }
            }
            else
            {
                _grabbedObject = null;
            }
        }

        private void SpawnCloud(Vector3 position)
        {
            if (CloudPrefab != null)
            {
                Vector3 spawnPos = new Vector3(position.x, position.y, position.z - 30);
                Instantiate(CloudPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void TryGrabObject(Vector3 grabPosition)
        {
            Collider[] hitColliders = Physics.OverlapSphere(grabPosition, 15.0f);
            float closestDist = float.MaxValue;
            GameObject closestObj = null;

            foreach (var hit in hitColliders)
            {
                if (hit.CompareTag("DynamicObject")) 
                {
                    float dist = Vector3.Distance(grabPosition, hit.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestObj = hit.gameObject;
                    }
                }
            }
            if (closestObj != null) _grabbedObject = closestObj;
        }

        // =================================================================================
        // MODO 2: NATIVO (ALTURA)
        // =================================================================================
        private void ProcessAutomaticHandDetection()
        {
            if (KinectManager == null || CalibrationManager == null) return;
            ushort[] depthData = KinectManager.GetCurrentData();
            if (depthData == null || depthData.Length == 0) return;

            float sandDepth = CalibrationManager.GetCalibrationDescriptor().MaxDepth;
            float maxLimit = sandDepth - HeightOffsetFromSand;
            float minLimit = maxLimit - InteractionZoneHeight;
            DetectedSandDepth = sandDepth;

            Point frameSize = KinectManager.GetKinectFrameSize();
            int width = frameSize.x;
            int height = frameSize.y;
            long sumX = 0; long sumY = 0; int pixelCount = 0;
            int step = 2; 

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int index = y * width + x;
                    ushort depth = depthData[index];

                    if (depth > minLimit && depth < maxLimit)
                    {
                        sumX += x; sumY += y; pixelCount++;
                    }
                }
            }
            CurrentPixelCount = pixelCount;
            if (pixelCount > MinPixelsPresent)
            {
                if (pixelCount > OpenHandThreshold)
                {
                    int avgX = (int)(sumX / pixelCount);
                    int avgY = (int)(sumY / pixelCount);
                    CreateGestureAtDepthPoint(avgX, avgY);
                }
            }
        }

        private Vector3 GetWorldPosFromDepth(int x, int y)
        {
            Point dataPos = new Point(x, y);
            return Sandbox.DataPosToWorldPos(dataPos);
        }

        private void CreateGestureAtDepthPoint(int x, int y)
        {
            Point dataPos = new Point(x, y);
            Vector3 worldPos = Sandbox.DataPosToWorldPos(dataPos);
            float terrainDepth = Sandbox.GetDepthFromWorldPos(worldPos);
            worldPos.z = terrainDepth; 
            CurrentGestures.Add(new HandInputGesture(999, worldPos, Vector2.zero, terrainDepth, dataPos, false, false));
        }

        // --- FUNÇÃO ATUALIZADA PARA SALVAR ---
        public void UI_SetUseMediaPipe(bool value) 
        { 
            UseMediaPipe = value;
            
            // SALVA NO DISCO: 1 para True, 0 para False
            PlayerPrefs.SetInt("UseMediaPipe", value ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        public void UI_SetOpenHandThreshold(float value) { OpenHandThreshold = (int)value; }
        public void UI_SetHeightOffset(float value) { HeightOffsetFromSand = value; }

        // --- MÉTODOS MOUSE (UI) ---
        public void OnUITouchDown(int touchID, Vector2 screenSpacePoint)
        {
            if (!CurrentGestures.Exists((gesture) => gesture.GestureID == touchID))
            {
                Vector3 worldPosition = SandboxUICamera.ViewportToWorldPoint(new Vector3(screenSpacePoint.x, screenSpacePoint.y));
                bool outOfBounds = false;
                float depth = -1;
                Ray ray = SandboxUICamera.ViewportPointToRay(screenSpacePoint);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, 1000, SandboxLayerMask)) depth = hitInfo.point.z / Sandbox.MESH_Z_SCALE;
                else outOfBounds = true;
                
                worldPosition.z = depth * Sandbox.MESH_Z_SCALE - 5;
                Point dataPosition = Sandbox.WorldPosToDataPos(worldPosition);
                Vector2 normalisedPosition = Sandbox.WorldPosToNormalisedPos(worldPosition);

                CurrentGestures.Add(new HandInputGesture(touchID, worldPosition, normalisedPosition, depth, dataPosition, outOfBounds, true));
            }
        }

        public void OnUITouchMove(int touchID, Vector2 screenSpacePoint)
        {
            HandInputGesture UIGesture = CurrentGestures.Find((gesture) => gesture.GestureID == touchID);
            if (UIGesture != null)
            {
                Vector3 worldPosition = SandboxUICamera.ViewportToWorldPoint(new Vector3(screenSpacePoint.x, screenSpacePoint.y));
                bool outOfBounds = false;
                float depth = -1;
                Ray ray = SandboxUICamera.ViewportPointToRay(screenSpacePoint);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, 1000, SandboxLayerMask)) depth = hitInfo.point.z / Sandbox.MESH_Z_SCALE - 5;
                else outOfBounds = true;

                worldPosition.z = depth * Sandbox.MESH_Z_SCALE;
                Point dataPosition = Sandbox.WorldPosToDataPos(worldPosition);
                Vector2 normalisedPosition = Sandbox.WorldPosToNormalisedPos(worldPosition);

                UIGesture.UpdatePosition(worldPosition, normalisedPosition, depth, dataPosition, outOfBounds);
            }
        }

        public void OnUITouchUp(int touchID)
        {
            HandInputGesture UIGesture = CurrentGestures.Find((gesture) => gesture.GestureID == touchID);
            if (UIGesture != null) CurrentGestures.Remove(UIGesture);
        }

        public List<HandInputGesture> GetCurrentGestures()
        {
            return CurrentGestures.GetRange(0, CurrentGestures.Count);
        }
    }

    public class HandInputGesture
    {
        public int GestureID { get; private set; }
        public bool IsUIGesture { get; private set; }
        public bool OutOfBounds { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public Vector2 NormalisedPosition { get; private set; }
        public Point DataPosition { get; private set; }
        public Point DataPosition_DS { get; private set; }
        public Point DataPosition_DS2 { get; private set; }
        public float SandboxDepth { get; private set; }
        public int Age { get; private set; }

        public HandInputGesture(int GestureID, Vector3 WorldPosition, Vector2 NormalisedPosition, float SandboxDepth, Point DataPosition, bool OutOfBounds, bool IsUIGesture)
        {
            this.GestureID = GestureID;
            this.WorldPosition = WorldPosition;
            this.NormalisedPosition = NormalisedPosition;
            this.SandboxDepth = SandboxDepth;
            this.DataPosition = DataPosition;
            this.OutOfBounds = OutOfBounds;
            this.IsUIGesture = IsUIGesture;
            Age = 1;
            DataPosition_DS = new Point(DataPosition.x / 2, DataPosition.y / 2);
            DataPosition_DS2 = new Point(DataPosition_DS.x / 2, DataPosition_DS.y / 2);
        }

        public void UpdatePosition(Vector3 WorldPosition, Vector2 NormalisedPosition, float SandboxDepth, Point DataPosition, bool OutOfBounds)
        {
            this.WorldPosition = WorldPosition;
            this.SandboxDepth = SandboxDepth;
            this.DataPosition = DataPosition;
            this.OutOfBounds = OutOfBounds;
            this.NormalisedPosition = NormalisedPosition;
            DataPosition_DS = new Point(DataPosition.x / 2, DataPosition.y / 2);
            DataPosition_DS2 = new Point(DataPosition_DS.x / 2, DataPosition_DS.y / 2);
            Age += 1;
        }
    }
}