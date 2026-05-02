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
        public bool EnableGestures = true; 
        public bool UseMediaPipe = false; 
        
        [Header("Referências")]
        public Sandbox Sandbox;
        public CalibrationManager CalibrationManager;
        public KinectManager KinectManager;
        public Camera SandboxUICamera;
        public LayerMask SandboxLayerMask;
        
        public KinectHandTracker MediaPipeTracker; 

        [Header("Comunicação Master")]
        private WaterSimulation.WaterSimulation _waterSim;

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
        private GameObject _grabbedObject = null;

        public delegate void OnGesturesReady_Delegate();
        public static OnGesturesReady_Delegate OnGesturesReady;
        private bool IsCalibrating;
        
        void Start()
        {
            _waterSim = FindObjectOfType<WaterSimulation.WaterSimulation>();

            if (PlayerPrefs.HasKey("EnableGestures")) EnableGestures = PlayerPrefs.GetInt("EnableGestures") == 1;
            if (PlayerPrefs.HasKey("UseMediaPipe")) UseMediaPipe = PlayerPrefs.GetInt("UseMediaPipe") == 1;
            if (PlayerPrefs.HasKey("InteractionZoneHeight")) InteractionZoneHeight = PlayerPrefs.GetFloat("InteractionZoneHeight");

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

                if (EnableGestures)
                {
                    if (UseMediaPipe) ProcessMediaPipeDetection();
                    else ProcessAutomaticHandDetection();
                }

                if (OnGesturesReady != null) OnGesturesReady();
            }
        }

        // =================================================================================
        // MODO 1: INTELIGÊNCIA ARTIFICIAL (MEDIAPIPE)
        // =================================================================================
        private void ProcessMediaPipeDetection()
        {
            if (MediaPipeTracker == null || !MediaPipeTracker.HandDetected) 
            {
                DebugCurrentGesture = "Mão não detectada";
                if(UITextDebugCurrentGesturel != null) UITextDebugCurrentGesturel.text = DebugCurrentGesture;
                _grabbedObject = null;
                return;
            }

            ushort[] depthData = KinectManager.GetCurrentData();
            if (depthData == null || depthData.Length == 0) return;
            Point frameSize = KinectManager.GetKinectFrameSize();
            
            // Pega o pixel absoluto do Kinect
            Vector2 palmNorm = MediaPipeTracker.HandCenter;
            int palmX = Mathf.Clamp((int)(palmNorm.x * frameSize.x), 0, frameSize.x - 1);
            int palmY = Mathf.Clamp((int)(palmNorm.y * frameSize.y), 0, frameSize.y - 1);

            int index = palmY * frameSize.x + palmX;
            ushort handDepth = depthData[index];
            DebugHandHeight = handDepth; 

            float sandDepth = CalibrationManager.GetCalibrationDescriptor().MaxDepth;
            float floorLimit = sandDepth - HeightOffsetFromSand; 
            float ceilingLimit = floorLimit - InteractionZoneHeight; 

            if (handDepth == 0 || handDepth > floorLimit || handDepth < ceilingLimit)
            {
                DebugCurrentGesture = "Mão Fora da Zona (Altura)";
                if(UITextDebugCurrentGesturel != null) UITextDebugCurrentGesturel.text = DebugCurrentGesture;
                _grabbedObject = null;
                return; 
            }

            string gesture = MediaPipeTracker.CurrentGesture;
            DebugCurrentGesture = gesture; 
            if(UITextDebugCurrentGesturel != null) UITextDebugCurrentGesturel.text = DebugCurrentGesture;
            
            // CORREÇÃO MESTRE DA SINCRONIA DE CALIBRAÇÃO (Recorte Relativo)
            Point dataStart = CalibrationManager.GetCalibrationDescriptor().DataStart;
            Point dataEnd = CalibrationManager.GetCalibrationDescriptor().DataEnd;

            int relPalmX = Mathf.Clamp(palmX - dataStart.x, 0, dataEnd.x - dataStart.x - 1);
            int relPalmY = Mathf.Clamp(palmY - dataStart.y, 0, dataEnd.y - dataStart.y - 1);

            Vector3 palmWorldPos = GetWorldPosFromDepth(relPalmX, relPalmY);
            palmWorldPos.z = Sandbox.GetDepthFromWorldPos(palmWorldPos);

            if (gesture == "Open_Palm")
            {
                _grabbedObject = null;
                CreateGestureAtDepthPoint(relPalmX, relPalmY); 
            }
            else if (gesture == "Closed_Fist")
            {
                _grabbedObject = null;
                if (_waterSim == null) _waterSim = FindObjectOfType<WaterSimulation.WaterSimulation>();
                if (_waterSim != null) _waterSim.SpawnWaterfall(palmWorldPos);
            }
            else if (gesture == "Lasso_Grab")
            {
                if (_grabbedObject == null) TryGrabObject(palmWorldPos);
                else _grabbedObject.transform.position = new Vector3(palmWorldPos.x, palmWorldPos.y, _grabbedObject.transform.position.z);
            }
            else if (gesture == "Pointing")
            {
                Vector2 tipNorm = MediaPipeTracker.IndexTipPosition;
                int tipX = Mathf.Clamp((int)(tipNorm.x * frameSize.x), 0, frameSize.x - 1);
                int tipY = Mathf.Clamp((int)(tipNorm.y * frameSize.y), 0, frameSize.y - 1);
                Vector3 tipWorldPos = GetWorldPosFromDepth(tipX, tipY);

                if (_grabbedObject == null) TryGrabObject(tipWorldPos);
                else _grabbedObject.transform.position = new Vector3(tipWorldPos.x, tipWorldPos.y, _grabbedObject.transform.position.z);
            }
            else if (gesture == "Victory")
            {
                _grabbedObject = null;
                if (_waterSim == null) _waterSim = FindObjectOfType<WaterSimulation.WaterSimulation>();
                if (_waterSim != null) 
                {
                    _waterSim.UI_DestroyWaterfalls();   
                    _waterSim.UI_DestroyWaterDroplets(); 
                }
            }
            else
            {
                _grabbedObject = null;
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
                int avgX = (int)(sumX / pixelCount);
                int avgY = (int)(sumY / pixelCount);

                // CORREÇÃO MESTRE DA SINCRONIA NO MODO NATIVO TAMBÉM
                Point dataStart = CalibrationManager.GetCalibrationDescriptor().DataStart;
                Point dataEnd = CalibrationManager.GetCalibrationDescriptor().DataEnd;

                int relAvgX = Mathf.Clamp(avgX - dataStart.x, 0, dataEnd.x - dataStart.x - 1);
                int relAvgY = Mathf.Clamp(avgY - dataStart.y, 0, dataEnd.y - dataStart.y - 1);

                Vector3 worldPos = GetWorldPosFromDepth(relAvgX, relAvgY);
                worldPos.z = Sandbox.GetDepthFromWorldPos(worldPos);

                if (pixelCount > OpenHandThreshold)
                {
                    CreateGestureAtDepthPoint(relAvgX, relAvgY);
                }
                else
                {
                    if (_waterSim == null) _waterSim = FindObjectOfType<WaterSimulation.WaterSimulation>();
                    if (_waterSim != null) _waterSim.SpawnWaterfall(worldPos);
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

        // =================================================================================
        // FUNÇÕES DA UI
        // =================================================================================
        public void UI_SetEnableGestures(bool value) { EnableGestures = value; PlayerPrefs.SetInt("EnableGestures", value ? 1 : 0); PlayerPrefs.Save(); }
        public void UI_SetUseMediaPipe(bool value) { UseMediaPipe = value; PlayerPrefs.SetInt("UseMediaPipe", value ? 1 : 0); PlayerPrefs.Save(); }
        public void UI_SetOpenHandThreshold(float value) { OpenHandThreshold = (int)value; }
        public void UI_SetHeightOffset(float value) { HeightOffsetFromSand = value; }
        public void UI_SetInteractionZoneHeight(float value) { InteractionZoneHeight = value; PlayerPrefs.SetFloat("InteractionZoneHeight", value); PlayerPrefs.Save(); }

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