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

        // --- Leitura de Corpos (Mantido) ---
        private BodyFrameReader bodyFrameReader;
        private Body[] bodies;

        // --- Leitura de Cor (NOVO - Para MediaPipe) ---
        private ColorFrameReader colorFrameReader;
        private byte[] colorData;
        private Texture2D colorTexture;

        // Mapper para converter posição 3D do esqueleto para 2D da tela de profundidade
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

                // --- 2. Leitura de Corpos / Mãos ---
                if (bodyFrameReader != null)
                {
                    BodyFrame bodyFrame = bodyFrameReader.AcquireLatestFrame();
                    if (bodyFrame != null)
                    {
                        if (bodies == null)
                        {
                            bodies = new Body[bodyFrame.BodyCount];
                        }
                        bodyFrame.GetAndRefreshBodyData(bodies);

                        bodyFrame.Dispose();
                        bodyFrame = null;
                    }
                }

                // --- 3. Leitura de Cor (NOVO - ADICIONADO AQUI) ---
                if (colorFrameReader != null)
                {
                    // Usamos 'using' para garantir o Dispose automático do frame de cor
                    using (ColorFrame colorFrame = colorFrameReader.AcquireLatestFrame())
                    {
                        if (colorFrame != null)
                        {
                            // Converte para RGBA (formato que Unity e MediaPipe gostam)
                            colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Rgba);
                            
                            if (colorTexture != null)
                            {
                                colorTexture.LoadRawTextureData(colorData);
                                colorTexture.Apply();
                            }
                        }
                    }
                }

                // --- 4. Funcionalidade de Debug ---
                if (Input.GetKeyUp(KeyCode.S))
                {
                    SaveDepthData();
                }
            }
        }

        void OnApplicationQuit()
        {
            if (!UseSavedData)
            {
                // Limpeza do Depth Reader
                if (depthFrameReader != null)
                {
                    depthFrameReader.Dispose();
                    depthFrameReader = null;
                }

                // Limpeza do Body Reader
                if (bodyFrameReader != null)
                {
                    bodyFrameReader.Dispose();
                    bodyFrameReader = null;
                }

                // Limpeza do Color Reader (NOVO)
                if (colorFrameReader != null)
                {
                    colorFrameReader.Dispose();
                    colorFrameReader = null;
                }

                if (kinectSensor != null)
                {
                    if (kinectSensor.IsOpen)
                    {
                        kinectSensor.Close();
                    }

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

                if (!dataReady)
                {
                    dataReady = true;
                    if (OnDataStarted != null) OnDataStarted();
                }
            }
        }
        
        public FrameDescription GetKinectFrameDescriptor()
        {
            return kinectFrameDesc;
        }
        
        public Point GetKinectFrameSize()
        {
            if (kinectFrameDesc == null) return new Point(512, 424);
            return new Point(kinectFrameDesc.Width, kinectFrameDesc.Height);
        }
        
        public ushort[] GetCurrentData()
        {
            newData = false;
            return depthData;
        }

        public Body[] GetBodies()
        {
            return bodies;
        }

        // --- MÉTODO PÚBLICO NOVO PARA PEGAR A TEXTURA ---
        public Texture2D GetColorTexture()
        {
            return colorTexture;
        }

        public bool StreamStarted()
        {
            if (UseSavedData)
                return true;

            return dataReady;
        }

        public bool NewDataReady()
        {
            return newData;
        }

        private bool GetFrameDescriptor()
        {
            kinectSensor = KinectSensor.GetDefault();
            if (kinectSensor != null)
            {
                CoordinateMapper = kinectSensor.CoordinateMapper;
                kinectFrameDesc = kinectSensor.DepthFrameSource.FrameDescription;
                return true;
            }
            else
            {
                print("Error: KinectSensor not found. Make sure Kinect has been installed correctly");
                return false;
            }
        }

        private void SetUpKinectBuffer()
        {
            if (kinectSensor != null)
            {
                if (!kinectSensor.IsOpen)
                {
                    kinectSensor.Open();
                }

                // Configura Depth
                depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
                depthData = new ushort[kinectSensor.DepthFrameSource.FrameDescription.LengthInPixels];

                // Configura Body
                bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

                // Configura Color (NOVO - ADICIONADO AQUI)
                colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
                
                // Cria a descrição do frame colorido (RGBA)
                var colorDesc = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
                
                // Aloca o buffer de bytes (Largura * Altura * 4 bytes por pixel)
                colorData = new byte[colorDesc.LengthInPixels * 4];
                
                // Cria a textura Unity (RGBA32)
                colorTexture = new Texture2D(colorDesc.Width, colorDesc.Height, TextureFormat.RGBA32, false);
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
                    for (int i = 0; i < length; i++)
                    {
                        depthData[i] = br.ReadUInt16();
                    }
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
                    foreach (ushort value in depthData)
                    {
                        bw.Write(value);
                    }
                }
            }
        }
    }
}