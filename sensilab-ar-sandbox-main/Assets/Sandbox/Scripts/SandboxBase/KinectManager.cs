using UnityEngine;
using System.Collections;
using System.IO;
using Windows.Kinect;

namespace ARSandbox
{
    public class KinectManager : MonoBehaviour
    {
        public bool UseSavedData;
        public TextAsset SavedData;

        public delegate void OnDataStarted_Delegate();
        public static event OnDataStarted_Delegate OnDataStarted;

        private FrameDescription kinectFrameDesc;
        private KinectSensor kinectSensor;
        
        // --- Leitura de Profundidade ---
        private DepthFrameReader depthFrameReader;
        private ushort[] depthData;

        // --- Leitura de Corpos ---
        private BodyFrameReader bodyFrameReader;
        private Body[] bodies;

        // --- Leitura de Infravermelho Colorizado (A MÁGICA ACONTECE AQUI) ---
        private InfraredFrameReader irFrameReader;
        private ushort[] irData;
        private byte[] colorizedIrData;
        private Texture2D irColorizedTexture;

        public CoordinateMapper CoordinateMapper { get; private set; }

        private bool dataReady = false;
        private bool newData = false;

        void Start()
        {
            if (GetFrameDescriptor())
            {
                if (UseSavedData)
                {
                    LoadDepthData();
                    StartCoroutine(Emulate30Hz());
                }
                else
                {
                    SetUpKinectBuffer();
                }
            }
        }

        void Update()
        {
            if (!UseSavedData)
            {
                // --- 1. Leitura de Profundidade ---
                if (depthFrameReader != null)
                {
                    DepthFrame frame = depthFrameReader.AcquireLatestFrame();
                    if (frame != null)
                    {
                        if (!dataReady)
                        {
                            dataReady = true;
                            if (OnDataStarted != null) OnDataStarted();
                        }
                        frame.CopyFrameDataToArray(depthData);
                        newData = true;
                        
                        frame.Dispose();
                        frame = null;
                    }
                }

                // --- 2. Leitura de Corpos ---
                if (bodyFrameReader != null)
                {
                    BodyFrame bodyFrame = bodyFrameReader.AcquireLatestFrame();
                    if (bodyFrame != null)
                    {
                        if (bodies == null) bodies = new Body[bodyFrame.BodyCount];
                        bodyFrame.GetAndRefreshBodyData(bodies);
                        bodyFrame.Dispose();
                        bodyFrame = null;
                    }
                }

                // --- 3. Leitura de IR e "Banho de Cor de Pele" ---
                if (irFrameReader != null)
                {
                    using (InfraredFrame irFrame = irFrameReader.AcquireLatestFrame())
                    {
                        if (irFrame != null)
                        {
                            irFrame.CopyFrameDataToArray(irData);
                            
                            for (int i = 0; i < irData.Length; i++)
                            {
                                // Kinect IR vai de 0 a ~65000, mas o brilho útil fica baixo.
                                // Dividir por 32 (>> 5) ajuda a mapear para a escala de cores.
                                int irValue = irData[i] >> 5; 
                                float intensity = Mathf.Clamp01((float)irValue / 255f);

                                // Filtro Cor de Pele (Engana o MediaPipe)
                                colorizedIrData[i * 4] = (byte)(240 * intensity);     // R (Vermelho alto)
                                colorizedIrData[i * 4 + 1] = (byte)(180 * intensity); // G (Verde médio)
                                colorizedIrData[i * 4 + 2] = (byte)(150 * intensity); // B (Azul baixo)
                                colorizedIrData[i * 4 + 3] = 255;                     // Alpha (Sólido)
                            }
                            
                            if (irColorizedTexture != null)
                            {
                                irColorizedTexture.LoadRawTextureData(colorizedIrData);
                                irColorizedTexture.Apply();
                            }
                        }
                    }
                }

                if (Input.GetKeyUp(KeyCode.S)) SaveDepthData();
            }
        }

        void OnApplicationQuit()
        {
            if (!UseSavedData)
            {
                if (depthFrameReader != null) { depthFrameReader.Dispose(); depthFrameReader = null; }
                if (bodyFrameReader != null) { bodyFrameReader.Dispose(); bodyFrameReader = null; }
                if (irFrameReader != null) { irFrameReader.Dispose(); irFrameReader = null; }

                if (kinectSensor != null)
                {
                    if (kinectSensor.IsOpen) kinectSensor.Close();
                    kinectSensor = null;
                }
            }
        }

        private IEnumerator Emulate30Hz()
        {
            while (true)
            {
                newData = true;
                yield return new WaitForSeconds(1 / 30.0f);
                if (!dataReady) { dataReady = true; if (OnDataStarted != null) OnDataStarted(); }
            }
        }
        
        public FrameDescription GetKinectFrameDescriptor() { return kinectFrameDesc; }
        
        public Point GetKinectFrameSize()
        {
            if (kinectFrameDesc == null) return new Point(512, 424);
            return new Point(kinectFrameDesc.Width, kinectFrameDesc.Height);
        }
        
        public ushort[] GetCurrentData() { newData = false; return depthData; }
        public Body[] GetBodies() { return bodies; }

        // Manda a textura falsa colorida pro MediaPipe
        public Texture2D GetIRColorizedTexture() { return irColorizedTexture; }
        public bool StreamStarted() { return UseSavedData || dataReady; }
        public bool NewDataReady() { return newData; }

        private bool GetFrameDescriptor()
        {
            kinectSensor = KinectSensor.GetDefault();
            if (kinectSensor != null)
            {
                CoordinateMapper = kinectSensor.CoordinateMapper;
                kinectFrameDesc = kinectSensor.DepthFrameSource.FrameDescription;
                return true;
            }
            return false;
        }

        private void SetUpKinectBuffer()
        {
            if (kinectSensor != null)
            {
                if (!kinectSensor.IsOpen) kinectSensor.Open();

                depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
                depthData = new ushort[kinectSensor.DepthFrameSource.FrameDescription.LengthInPixels];
                bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

                // Inicializa o sensor IR em vez do Color
                irFrameReader = kinectSensor.InfraredFrameSource.OpenReader();
                var irDesc = kinectSensor.InfraredFrameSource.FrameDescription;
                
                irData = new ushort[irDesc.LengthInPixels];
                colorizedIrData = new byte[irDesc.LengthInPixels * 4]; // RGBA
                irColorizedTexture = new Texture2D(irDesc.Width, irDesc.Height, TextureFormat.RGBA32, false);
            }
        }

        private void LoadDepthData()
        {
            using (Stream s = new MemoryStream(SavedData.bytes))
            {
                using (BinaryReader br = new BinaryReader(s))
                {
                    int length = br.ReadInt32();
                    depthData = new ushort[length];
                    for (int i = 0; i < length; i++) depthData[i] = br.ReadUInt16();
                }
            }
        }
        
        private void SaveDepthData()
        {
            using (FileStream fs = new FileStream(Application.dataPath + "/Depth.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(depthData.Length);
                    foreach (ushort value in depthData) bw.Write(value);
                }
            }
        }
    }
}