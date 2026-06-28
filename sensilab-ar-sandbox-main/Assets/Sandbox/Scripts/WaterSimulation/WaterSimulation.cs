using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ARSandbox.WaterSimulation
{
    public class WaterSimulation : MonoBehaviour
    {
        public Sandbox Sandbox;
        public HandInput HandInput;
        public CalibrationManager CalibrationManager;
        public WaterDroplet WaterDroplet;
        public Camera MetaballCamera;
        public Shader MetaballShader;
        public ComputeShader WaterSurfaceComputeShader;
        
        // --- TEXTURA ---
        [Header("Controle de Textura")]
        public Texture2D[] WaterColorTextures;
        public TMP_Dropdown textureDropdown;
        private int selectedTextureIndex = 0;
        private Color selectedColor = Color.white;
        
        // --- AUDIO SETTINGS ---
        [Header("Audio Settings")]
        public bool EnableSounds = true;
        public float SoundVolume = 0.5f;
        public AudioClip RainSoundClip;
        public AudioClip WaterfallSoundClip;
        private AudioSource rainAudioSource;
        private AudioSource waterfallAudioSource;

        // --- CLOUD SETTINGS ---
        [Header("Cloud Settings")]
        public bool EnableClouds = true;
        public GameObject CloudPrefab;
        public float MinCloudParticleSize = 1.0f;
        public float MaxCloudParticleSize = 3.0f;
        public Color CloudColor = Color.white;
        private float lastCloudSpawnTime = 0f;

        // --- WETNESS SETTINGS ---
        [Header("Wet Terrain Settings")]
        [Range(0.0001f, 0.01f)]
        public float DryingSpeed = 0.001f;
        private RenderTexture wetnessRT;

        // --- FISICA DA ÁGUA ---
        [Header("Física da Água")]
        public Slider viscositySlider;  
        private float selectedViscosity = 0f; 
        public Slider absorptionSlider;
        private float absorptionSpeed = 0.5f;
        public Slider evaporationSlider;
        private float evaporationTime = 10.0f;

        // --- CONTROLE DE CASCATA ---
        [Header("Configuração de Cascata")]
        public Toggle waterfallToggle; 
        public Slider emissionRateSlider; 
        public GameObject WaterfallEmitterPrefab;
        public int MaxWaterfalls = 1;
        public float MinWaterfallDistance = 50.0f;
        
        private List<GameObject> activeWaterfallEmitters = new List<GameObject>();
        public bool isWaterfallActive = false;
        private float emissionRate = 1.0f;

        private SandboxDescriptor sandboxDescriptor;
        private List<WaterDroplet> waterDroplets;
        private RenderTexture metaballRT;
        private int currSubsection;
        private bool showParticles;

        private RenderTexture waterBufferRT0;
        private RenderTexture waterBufferRT1;
        private bool swapBuffers;

        private IEnumerator RunSimulationCoroutine;
        private bool initialised;
        private const int MaxMetaballs = 2000;
        
        // --- MECÂNICAS DE GESTO ---
        [Header("Configuração de Gestos (Vento e Pinça)")]
        public float GrabRadius = 40.0f; // Distância para conseguir pegar o cubo
        public float WindForceMultiplier = 0.03f; 
        public float MinimumWindSpeed = 200.0f; 
        private Vector3 previousHandPos = Vector3.zero; 
        private float lastGestureTime = 0f; 

        [Header("Objeto Interativo")]
        public GameObject MovableObjectPrefab;
        private GameObject currentMovableObject;
        private bool isHoldingObject = false; // Memória para saber se a mão está agarrando o objeto

        void Awake()
        {
            if (PlayerPrefs.HasKey("EnableSounds")) EnableSounds = PlayerPrefs.GetInt("EnableSounds") == 1;
            if (PlayerPrefs.HasKey("EnableClouds")) EnableClouds = PlayerPrefs.GetInt("EnableClouds") == 1;
            if (PlayerPrefs.HasKey("SoundVolume")) SoundVolume = PlayerPrefs.GetFloat("SoundVolume");
            if (PlayerPrefs.HasKey("MinCloudParticleSize")) MinCloudParticleSize = PlayerPrefs.GetFloat("MinCloudParticleSize");
            if (PlayerPrefs.HasKey("MaxCloudParticleSize")) MaxCloudParticleSize = PlayerPrefs.GetFloat("MaxCloudParticleSize");

            if (PlayerPrefs.HasKey("MaxWaterfalls")) MaxWaterfalls = PlayerPrefs.GetInt("MaxWaterfalls");
            
            ConfigureDropdown();
            ConfigureViscositySlider();
            ConfigureAbsorptionEvaporationSlider();
            ConfigureWaterfallControls();
            SetupAudio();
        }

        void SetupAudio()
        {
            rainAudioSource = gameObject.AddComponent<AudioSource>();
            rainAudioSource.clip = RainSoundClip;
            rainAudioSource.loop = true;
            rainAudioSource.volume = EnableSounds ? SoundVolume : 0f;
            rainAudioSource.playOnAwake = false;

            waterfallAudioSource = gameObject.AddComponent<AudioSource>();
            waterfallAudioSource.clip = WaterfallSoundClip;
            waterfallAudioSource.loop = true;
            waterfallAudioSource.volume = EnableSounds ? (SoundVolume + 0.2f) : 0f;
            waterfallAudioSource.playOnAwake = false;
        }

        // =========================================================
        // FUNÇÕES DA UI GERAIS E DA CASCATA
        // =========================================================
        public void UI_SetEnableSounds(bool isOn)
        {
            EnableSounds = isOn;
            PlayerPrefs.SetInt("EnableSounds", isOn ? 1 : 0);
            PlayerPrefs.Save();
            
            if (rainAudioSource != null) rainAudioSource.volume = EnableSounds ? SoundVolume : 0f;
            if (waterfallAudioSource != null) waterfallAudioSource.volume = EnableSounds ? (SoundVolume + 0.2f) : 0f;
        }

        public void UI_SetSoundVolume(float volume)
        {
            SoundVolume = volume;
            PlayerPrefs.SetFloat("SoundVolume", volume);
            PlayerPrefs.Save();
            
            if (EnableSounds)
            {
                if (rainAudioSource != null) rainAudioSource.volume = SoundVolume;
                if (waterfallAudioSource != null) waterfallAudioSource.volume = SoundVolume + 0.2f;
            }
        }

        public void UI_SetEnableClouds(bool isOn)
        {
            EnableClouds = isOn;
            PlayerPrefs.SetInt("EnableClouds", isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void UI_SetCloudMinSize(float size) 
        { 
            MinCloudParticleSize = size; 
            PlayerPrefs.SetFloat("MinCloudParticleSize", size);
            PlayerPrefs.Save();
        }

        public void UI_SetCloudMaxSize(float size) 
        { 
            MaxCloudParticleSize = size; 
            PlayerPrefs.SetFloat("MaxCloudParticleSize", size);
            PlayerPrefs.Save();
        }
        
        public void UI_SetMaxWaterfalls(float max)
        {
            MaxWaterfalls = (int)max;
            PlayerPrefs.SetInt("MaxWaterfalls", MaxWaterfalls);
            PlayerPrefs.Save();
        }

        void ConfigureWaterfallControls()
        {
            if(waterfallToggle != null) waterfallToggle.onValueChanged.AddListener(OnWaterfallToggleChanged);
            if(emissionRateSlider != null)
            {
                emissionRateSlider.minValue = 0.5f;
                emissionRateSlider.maxValue = 5.0f;
                emissionRateSlider.SetValueWithoutNotify(emissionRate);
            }
        }

        public void OnWaterfallToggleChanged(bool isOn) { isWaterfallActive = isOn; }

        private void OnEmissionRateChanged(float value) 
        { 
            emissionRate = value; 
            foreach (GameObject emitter in activeWaterfallEmitters)
            {
                if (emitter != null)
                {
                    var script = emitter.GetComponent<S_WaterfallEmitter>();
                    if (script != null) script.SetEmissionRate(emissionRate);
                }
            }
        }
        
        public void UI_SetEmissionRate(float value) 
        { 
            emissionRate = value; 
            PlayerPrefs.SetFloat("EmissionRate", value);
            PlayerPrefs.Save();
        }
        
        // =========================================================
        // GERADORES GLOBAIS (NUVENS E CACHOEIRAS)
        // =========================================================
        public void SpawnCloud(Vector3 position)
        {
            if (!EnableClouds || CloudPrefab == null) return;
            if (Time.time - lastCloudSpawnTime < 2.0f) return; 
            
            lastCloudSpawnTime = Time.time;
            Vector3 spawnPos = new Vector3(position.x, position.y, position.z - 150f);
            GameObject newCloud = Instantiate(CloudPrefab, spawnPos, Quaternion.identity);
            
            SimpleCloudBehavior cloudBehavior = newCloud.GetComponent<SimpleCloudBehavior>();
            if (cloudBehavior != null && cloudBehavior.CloudParts != null)
            {
                var main = cloudBehavior.CloudParts.main;
                main.startSize = new ParticleSystem.MinMaxCurve(MinCloudParticleSize, MaxCloudParticleSize);
                main.startColor = CloudColor;
            }
        }

        public void SpawnWaterfall(Vector3 position, bool checkDistance = true)
        {
            activeWaterfallEmitters.RemoveAll(item => item == null);

            if (checkDistance && activeWaterfallEmitters.Count > 0)
            {
                foreach (GameObject emitter in activeWaterfallEmitters)
                {
                    if (Vector3.Distance(position, emitter.transform.position) < MinWaterfallDistance) 
                        return; 
                }
            }

            GameObject newEmitter = Instantiate(WaterfallEmitterPrefab, new Vector3(position.x, position.y, position.z - 50f), Quaternion.identity);
            var emitterScript = newEmitter.GetComponent<S_WaterfallEmitter>();
            if (emitterScript != null)
            {
                emitterScript.Initialize(WaterDroplet, selectedViscosity, absorptionSpeed, evaporationTime, WaterColorTextures[selectedTextureIndex]);
                emitterScript.SetEmissionRate(emissionRate);
                emitterScript.SetShowMesh(showParticles);
            }

            if (EnableClouds && CloudPrefab != null)
            {
                GameObject cloudInstance = Instantiate(CloudPrefab, new Vector3(position.x, position.y, position.z - 60f), Quaternion.identity, newEmitter.transform);
                
                SimpleCloudBehavior cloudBehavior = cloudInstance.GetComponent<SimpleCloudBehavior>();
                if (cloudBehavior != null && cloudBehavior.CloudParts != null)
                {
                    var main = cloudBehavior.CloudParts.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(MinCloudParticleSize, MaxCloudParticleSize);
                    main.startColor = CloudColor;
                }
            }

            activeWaterfallEmitters.Add(newEmitter);

            if (activeWaterfallEmitters.Count > MaxWaterfalls)
            {
                Destroy(activeWaterfallEmitters[0]);
                activeWaterfallEmitters.RemoveAt(0);
            }
        }

        // =========================================================
        // CONFIGURAÇÕES PADRÕES E SIMULAÇÃO
        // =========================================================
        void ConfigureDropdown()
        {
            string[] textureNames = { "Água", "Lava", "Ácido", "Óleo", "Gelo" }; 
            List<string> options = new List<string>();
            for (int i = 0; i < WaterColorTextures.Length; i++) options.Add(textureNames[i]);
            textureDropdown.ClearOptions();
            textureDropdown.AddOptions(options);
            textureDropdown.onValueChanged.AddListener(delegate { OnTextureSelected(textureDropdown); });
        }

        void OnTextureSelected(TMP_Dropdown dropdown)
        {
            selectedTextureIndex = dropdown.value;
            Sandbox.SetShaderTexture("_WaterColorTex", WaterColorTextures[selectedTextureIndex]);
        }

        void ConfigureViscositySlider()
        {
            if(viscositySlider == null) return;
            viscositySlider.minValue = 0.0f;  
            viscositySlider.maxValue = 1.0f; 
            viscositySlider.value = selectedViscosity; 
            viscositySlider.onValueChanged.AddListener(OnViscosityChanged); 
        }

        public void OnViscosityChanged(float value) { selectedViscosity = value; }

        void ConfigureAbsorptionEvaporationSlider()
        {
            if (absorptionSlider != null)
            {
                absorptionSlider.minValue = 1.0f; 
                absorptionSlider.maxValue = 100.0f; 
                absorptionSlider.value = absorptionSpeed; 
                absorptionSlider.onValueChanged.AddListener(OnAbsorptionChanged);
            }
            if (evaporationSlider != null)
            {
                evaporationSlider.minValue = 1.0f; 
                evaporationSlider.maxValue = 100.0f; 
                evaporationSlider.value = evaporationTime; 
                evaporationSlider.onValueChanged.AddListener(OnEvaporationChanged);
            }
        }

        public void OnAbsorptionChanged(float value) { absorptionSpeed = value; }
        public void OnEvaporationChanged(float value) { evaporationTime = value; }

        void InitialiseSimulation()
        {
            if (!initialised)
            {
                waterDroplets = new List<WaterDroplet>();
                currSubsection = 0;
                showParticles = false;
                CreateWaterSurfaceRenderTextures();
                swapBuffers = false;
            }
            initialised = true;
        }

        IEnumerator RunSimulation()
        {
            while (true)
            {
                CullStrayMetaballs();
                KeepMetaballsAboveSandbox();
                StepWaterSurfaceSimulation();
                UpdateAudio();
                UpdateWetnessMap();
                
                if (Random.value < 2 / 60.0f) DisturbWaterSurfaceSimulation();
                yield return new WaitForSeconds(1 / 60.0f);
            }
        }
        
        private void UpdateAudio()
        {
            if (!EnableSounds) 
            {
                if(rainAudioSource != null && rainAudioSource.isPlaying) rainAudioSource.Stop();
                if(waterfallAudioSource != null && waterfallAudioSource.isPlaying) waterfallAudioSource.Stop();
                return;
            }

            if (HandInput != null && HandInput.GetCurrentGestures().Count > 0)
            {
                if (rainAudioSource != null && !rainAudioSource.isPlaying) rainAudioSource.Play();
            }
            else
            {
                if (rainAudioSource != null && rainAudioSource.isPlaying) rainAudioSource.Stop();
            }

            activeWaterfallEmitters.RemoveAll(item => item == null);
            if (activeWaterfallEmitters.Count > 0)
            {
                if (waterfallAudioSource != null && !waterfallAudioSource.isPlaying) waterfallAudioSource.Play();
            }
            else
            {
                if (waterfallAudioSource != null && waterfallAudioSource.isPlaying) waterfallAudioSource.Stop();
            }
        }

        private void UpdateWetnessMap()
        {
            RenderTexture currentWater = swapBuffers ? waterBufferRT0 : waterBufferRT1;
            WaterSurfaceCSHelper.Run_UpdateWetness(WaterSurfaceComputeShader, wetnessRT, currentWater, DryingSpeed);
            Sandbox.SetShaderTexture("_WetnessTex", wetnessRT);
        }

        void OnEnable()
        {
            InitialiseSimulation();
            HandInput.OnGesturesReady += OnGesturesReady;
            CalibrationManager.OnCalibration += OnCalibration;
            Sandbox.OnSandboxReady += OnSandboxReady;
            sandboxDescriptor = Sandbox.GetSandboxDescriptor();

            SetUpMetaballCamera();
            MetaballCamera.gameObject.SetActive(true);
            StartCoroutine(RunSimulationCoroutine = RunSimulation());
        }

        void OnDisable()
        {
            HandInput.OnGesturesReady -= OnGesturesReady;
            CalibrationManager.OnCalibration -= OnCalibration;
            Sandbox.OnSandboxReady -= OnSandboxReady;
            Sandbox.SetDefaultShader();
            MetaballCamera.gameObject.SetActive(false);

            DestroyWaterDroplets();
            if (RunSimulationCoroutine != null) StopCoroutine(RunSimulationCoroutine);
            
            if(rainAudioSource) rainAudioSource.Stop();
            if(waterfallAudioSource) waterfallAudioSource.Stop();
        }

        private void OnCalibration()
        {
            HandInput.OnGesturesReady -= OnGesturesReady;
            MetaballCamera.gameObject.SetActive(false);
            DestroyWaterDroplets();
            Sandbox.SetDefaultShader();
            if (RunSimulationCoroutine != null) StopCoroutine(RunSimulationCoroutine);
        }

        private void OnSandboxReady()
        {
            InitialiseSimulation();
            HandInput.OnGesturesReady += OnGesturesReady;
            SetUpMetaballCamera();
            sandboxDescriptor = Sandbox.GetSandboxDescriptor();
            StartCoroutine(RunSimulationCoroutine = RunSimulation());
        }

        private void CreateWaterSurfaceRenderTextures()
        {
            waterBufferRT0 = new RenderTexture(256, 256, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            waterBufferRT0.filterMode = FilterMode.Bilinear;
            waterBufferRT0.wrapMode = TextureWrapMode.Repeat;
            waterBufferRT0.enableRandomWrite = true;
            waterBufferRT0.Create();
            WaterSurfaceCSHelper.Run_FillRenderTexture(WaterSurfaceComputeShader, waterBufferRT0, 0.5f);

            waterBufferRT1 = new RenderTexture(256, 256, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            waterBufferRT1.filterMode = FilterMode.Bilinear;
            waterBufferRT1.wrapMode = TextureWrapMode.Repeat;
            waterBufferRT1.enableRandomWrite = true;
            waterBufferRT1.Create();
            WaterSurfaceCSHelper.Run_FillRenderTexture(WaterSurfaceComputeShader, waterBufferRT1, 0.5f);
            
            wetnessRT = new RenderTexture(256, 256, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            wetnessRT.filterMode = FilterMode.Bilinear;
            wetnessRT.enableRandomWrite = true;
            wetnessRT.Create();
            WaterSurfaceCSHelper.Run_FillRenderTexture(WaterSurfaceComputeShader, wetnessRT, 0.0f); 
        }

        private void StepWaterSurfaceSimulation()
        {
            if (swapBuffers)
            {
                WaterSurfaceCSHelper.Run_StepWaterSim(WaterSurfaceComputeShader, waterBufferRT0, waterBufferRT1, 1 / 60f, 1, 20, 0.999f, true);
                Sandbox.SetShaderTexture("_WaterSurfaceTex", waterBufferRT0);
            }
            else
            {
                WaterSurfaceCSHelper.Run_StepWaterSim(WaterSurfaceComputeShader, waterBufferRT1, waterBufferRT0, 1 / 60f, 1, 20, 0.999f, true);
                Sandbox.SetShaderTexture("_WaterSurfaceTex", waterBufferRT1);
            }
            swapBuffers = !swapBuffers;
        }

        private void DisturbWaterSurfaceSimulation()
        {
            Vector2 point0 = new Vector2(32 + Random.value * 224.0f, 32 + Random.value * 224.0f);
            Vector2 point1 = new Vector2(32 + Random.value * 224.0f, 32 + Random.value * 224.0f);
            Vector2 point2 = new Vector2(32 + Random.value * 224.0f, 32 + Random.value * 224.0f);
            Vector2 point3 = new Vector2(32 + Random.value * 224.0f, 32 + Random.value * 224.0f);

            Vector2[] centres = new Vector2[4] { point0, point1, point2, point3 };
            float[] radii = new float[4] { 5, 5, 10, 10 };
            float[] powers = new float[4] { 0.1f * Random.value, -0.1f * Random.value, 0.25f, -0.25f };

            WaterSurfaceCSHelper.Run_DisplaceWater(WaterSurfaceComputeShader, waterBufferRT0, waterBufferRT1, centres, radii, powers, 2);
        }

        private void KeepMetaballsAboveSandbox()
        {
            if (currSubsection == Sandbox.COLL_MESH_DELAY - 1)
            {
                if (waterDroplets.Count < 200)
                {
                    foreach (WaterDroplet droplet in waterDroplets)
                    {
                        float sandboxDepth = Sandbox.GetDepthFromWorldPos(droplet.transform.position);
                        if (droplet.transform.position.z > sandboxDepth) droplet.SetZPosition(sandboxDepth - WaterDroplet.DROPLET_RADIUS * 1.25f);
                    }
                }
                currSubsection = 0;
            }
            else
            {
                if (waterDroplets.Count >= 200)
                {
                    int dropletStep = waterDroplets.Count / Sandbox.COLL_MESH_DELAY;
                    for (int i = currSubsection * dropletStep; i < (currSubsection + 1) * dropletStep; i++)
                    {
                        if (i < waterDroplets.Count)
                        {
                            WaterDroplet droplet = waterDroplets[i];
                            float sandboxDepth = Sandbox.GetDepthFromWorldPos(droplet.transform.position);
                            if (droplet.transform.position.z > sandboxDepth - WaterDroplet.DROPLET_RADIUS + 10)
                            {
                                droplet.SetZPosition(sandboxDepth - WaterDroplet.DROPLET_RADIUS);
                            }
                        }
                    }
                }
                currSubsection += 1;
            }
        }

        private void CullStrayMetaballs()
        {
            float minX = sandboxDescriptor.MeshStart.x - 5;
            float minY = sandboxDescriptor.MeshStart.y - 5;
            float maxX = sandboxDescriptor.MeshEnd.x + 5;
            float maxY = sandboxDescriptor.MeshEnd.y + 5;
            int metaballCount = 0;

            for (int i = waterDroplets.Count - 1; i >= 0; i--)
            {
                if (waterDroplets[i] == null)
                {
                    waterDroplets.RemoveAt(i);
                    continue;
                }

                WaterDroplet droplet = waterDroplets[i];
                Vector3 position = droplet.transform.position;
                if (position.x < minX || position.y < minY || position.x > maxX || position.y > maxY)
                {
                    Destroy(droplet.gameObject);
                    waterDroplets.RemoveAt(i);
                } else
                {
                    metaballCount++;
                    if (metaballCount > MaxMetaballs)
                    {
                        Destroy(droplet.gameObject);
                        waterDroplets.RemoveAt(i);
                    }
                }
            }
        }
        
        private void SetUpMetaballCamera()
        {
            CalibrationManager.SetUpDataCamera(MetaballCamera);
            CreateMetaballRT();
            MetaballCamera.targetTexture = metaballRT;
            Sandbox.SetSandboxShader(MetaballShader);
            Sandbox.SetShaderTexture("_MetaballTex", metaballRT);
            if (selectedTextureIndex >= 0 && selectedTextureIndex < WaterColorTextures.Length)
            {
                Sandbox.SetShaderTexture("_WaterColorTex", WaterColorTextures[selectedTextureIndex]);
            }
            Vector2 waterSurfaceTexScaling = new Vector2((float)sandboxDescriptor.DataSize.x / (float)sandboxDescriptor.DataSize.y * 1f, 1f);
            Sandbox.SetTextureProperties("_WaterSurfaceTex", Vector2.zero, waterSurfaceTexScaling);
        }

        private void CreateMetaballRT()
        {
            float aspectRatio = (float)sandboxDescriptor.DataSize.x / (float)sandboxDescriptor.DataSize.y;
            if (metaballRT != null)
            {
                metaballRT.DiscardContents();
                metaballRT.Release();
            }
            metaballRT = new RenderTexture((int)(256.0f * aspectRatio), 256, 0);
        }

        private void OnGesturesReady()
        {
            KinectHandTracker tracker = FindObjectOfType<KinectHandTracker>();
            string currentGesture = tracker != null ? tracker.CurrentGesture : "Unknown";

            // Se o usuário não está fazendo a pinça, ele "solta" o objeto
            if (currentGesture != "Lasso_Grab") 
            {
                isHoldingObject = false;
            }

            foreach (HandInputGesture gesture in HandInput.GetCurrentGestures())
            {
                if (!gesture.OutOfBounds)
                {
                    Vector3 handPos = gesture.WorldPosition;

                    // 1. PROTEÇÃO DO MOUSE / TOUCH
                    if (gesture.IsUIGesture)
                    {
                        if (!Physics.CheckSphere(handPos + new Vector3(0, 0, -5), 1.0f))
                        {
                            if (isWaterfallActive) SpawnWaterfall(handPos, false); 
                            else SpawnDroplet(handPos);
                        }
                        continue; 
                    }

                    // 2. PODERES DA IA
                    
                    // GESTO: Limpeza Global (Victory / V)
                    if (currentGesture == "Victory")
                    {
                        UI_DestroyWaterfalls();
                        DestroyWaterDroplets(); 
                    }

                    // GESTO: Criar Objeto Interativo (ILoveYou / Homem-Aranha)
                    else if (currentGesture == "ILoveYou")
                    {
                        if (currentMovableObject == null && MovableObjectPrefab != null)
                        {
                            // Calcula o meio da areia e a altura física para nascer em cima das montanhas
                            float centerX = (sandboxDescriptor.MeshStart.x + sandboxDescriptor.MeshEnd.x) / 2f;
                            float centerY = (sandboxDescriptor.MeshStart.y + sandboxDescriptor.MeshEnd.y) / 2f;
                            float centerZ = Sandbox.GetDepthFromWorldPos(new Vector3(centerX, centerY, 0f));
                            
                            Vector3 centerPos = new Vector3(centerX, centerY, centerZ - 10f);
                            currentMovableObject = Instantiate(MovableObjectPrefab, centerPos, Quaternion.identity);
                        }
                    }

                    // GESTO: Vento e Chuva (Open_Palm)
                    else if (currentGesture == "Open_Palm")
                    {
                        SpawnDroplet(handPos);

                        float dt = Time.time - lastGestureTime;
                        if (dt > 0 && dt < 0.1f) 
                        {
                            Vector3 deltaP = handPos - previousHandPos;
                            Vector3 velocity = deltaP / dt;

                            if (velocity.magnitude > MinimumWindSpeed)
                            {
                                Vector2 windForce = new Vector2(velocity.x, velocity.y) * (WindForceMultiplier * 0.01f);
                                foreach (WaterDroplet droplet in waterDroplets)
                                {
                                    if (droplet != null) droplet.ApplyWindForce(windForce);
                                }
                            }
                        }
                    }
                    
                    // GESTO: Agarrar e Arrastar (Lasso_Grab / Pinça)
                    else if (currentGesture == "Lasso_Grab")
                    {
                        if (currentMovableObject != null)
                        {
                            // Calcula a distância entre a mão e o cubo
                            float distToHand = Vector2.Distance(new Vector2(handPos.x, handPos.y), new Vector2(currentMovableObject.transform.position.x, currentMovableObject.transform.position.y));
                            
                            // Se já estava segurando, OU acabou de chegar perto para "clicar"
                            if (isHoldingObject || distToHand < GrabRadius)
                            {
                                isHoldingObject = true; // Confirma que agarrou
                                float currentDepth = Sandbox.GetDepthFromWorldPos(handPos);
                                currentMovableObject.transform.position = new Vector3(handPos.x, handPos.y, currentDepth - 10f);
                            }
                        }
                    }
                    
                    // GESTO: Gerador de Cascata (Closed_Fist)
                    else if (currentGesture == "Closed_Fist")
                    {
                        SpawnWaterfall(handPos, true);
                    }
                    
                    else if (currentGesture == "Pointing")
                    {
                        // Não faz NADA com a água! 
                        // Deixa o HandInput.cs cuidar apenas de mostrar o texto de altitude na tela.
                    }
                    
                    // GESTO: Água Padrão
                    else 
                    {
                        SpawnDroplet(handPos);
                    }

                    previousHandPos = handPos;
                    lastGestureTime = Time.time;
                }
            }
        }

        private void SpawnDroplet(Vector3 pos)
        {
            WaterDroplet waterDroplet = Instantiate(WaterDroplet, pos, Quaternion.identity);
            waterDroplet.SetShowMesh(showParticles);
            waterDroplet.SetViscosity(selectedViscosity); 
            waterDroplet.SetAbsorptionSpeed(absorptionSpeed); 
            waterDroplet.SetEvaporationTime(evaporationTime); 
            waterDroplets.Add(waterDroplet);
        }

        public void UI_DestroyWaterfalls()
        {
            activeWaterfallEmitters.RemoveAll(item => item == null);
            foreach (GameObject emitter in activeWaterfallEmitters) Destroy(emitter);
            activeWaterfallEmitters.Clear();
        }

        private void DestroyWaterDroplets()
        {
            // Mata as águas
            WaterDroplet[] allDroplets = FindObjectsOfType<WaterDroplet>();
            foreach (WaterDroplet droplet in allDroplets)
            {
                if (droplet != null) Destroy(droplet.gameObject);
            }
            waterDroplets.Clear();

            // Mata o cubo também, unificando a limpeza do mapa!
            if (currentMovableObject != null)
            {
                Destroy(currentMovableObject);
                currentMovableObject = null;
            }
        }

        public void UI_DestroyWaterDroplets() { DestroyWaterDroplets(); }

        public void UI_ToggleShowParticles(bool showParticles)
        {
            this.showParticles = showParticles;
            foreach (WaterDroplet droplet in waterDroplets) droplet.SetShowMesh(showParticles);

            activeWaterfallEmitters.RemoveAll(item => item == null);
            foreach (GameObject emitter in activeWaterfallEmitters)
            {
                var emitterScript = emitter.GetComponent<S_WaterfallEmitter>();
                if (emitterScript != null) emitterScript.SetShowMesh(showParticles);
            }
        }
    }
}