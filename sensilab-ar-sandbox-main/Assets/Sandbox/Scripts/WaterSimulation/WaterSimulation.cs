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
        
        //Controle Cor da Textura
        public Texture2D[] WaterColorTextures; // Array para armazenar 5 texturas diferentes
        public TMP_Dropdown textureDropdown; // Dropdown para escolher a textura
        private int selectedTextureIndex = 0; // Índice da textura selecionada
        private Color selectedColor = Color.white; // Cor padrão selecionada
        
        // --- AUDIO SETTINGS ---
        [Header("Audio Settings")]
        public AudioClip RainSoundClip;
        public AudioClip WaterfallSoundClip;
        private AudioSource rainAudioSource;
        private AudioSource waterfallAudioSource;

        // --- WETNESS SETTINGS ---
        [Header("Wet Terrain Settings")]
        [Range(0.0001f, 0.01f)]
        public float DryingSpeed = 0.001f; // Velocidade que a terra seca
        private RenderTexture wetnessRT; // Textura que guarda onde está molhado

        //Controle Viscosidade
        public Slider viscositySlider;  // Referência ao slider de viscosidade
        private float selectedViscosity = 0f; // Valor padrão de viscosidade
        public Slider absorptionSlider;
        private float absorptionSpeed = 0.5f;
        public Slider evaporationSlider;
        private float evaporationTime = 10.0f;

        // Controle de Cascata
        public Toggle waterfallToggle;
        public Slider emissionRateSlider;
        public GameObject WaterfallEmitterPrefab;
        private List<GameObject> activeWaterfallEmitters = new List<GameObject>();
        private bool isWaterfallActive = false;
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


        void Awake()
        {
            // Configura o TMP_Dropdown da Textura
            ConfigureDropdown();
            // Configura Slides Viscosidade
            ConfigureViscositySlider();
            // Configura Slides Absorção e Evaporação
            ConfigureAbsorptionEvaporationSlider();
            // Configura Waterfall
            ConfigureWaterfallControls();
            // Inicializar Áudio
            SetupAudio();

        }
        
        void SetupAudio()
        {
            // Cria os componentes de áudio via código para não precisar arrastar no editor
            rainAudioSource = gameObject.AddComponent<AudioSource>();
            rainAudioSource.clip = RainSoundClip;
            rainAudioSource.loop = true;
            rainAudioSource.volume = 0.5f;
            rainAudioSource.playOnAwake = false;

            waterfallAudioSource = gameObject.AddComponent<AudioSource>();
            waterfallAudioSource.clip = WaterfallSoundClip;
            waterfallAudioSource.loop = true;
            waterfallAudioSource.volume = 0.7f;
            waterfallAudioSource.playOnAwake = false;
        }

        void ConfigureDropdown()
        {
            // Cria uma lista com os nomes das texturas
            string[] textureNames = { "Água", "Lava", "Ácido", "Óleo", "Gelo" }; 

            // Configura o TMP_Dropdown com opções de texturas
            List<string> options = new List<string>();
            // Adiciona os nomes predefinidos ao dropdown
            for (int i = 0; i < WaterColorTextures.Length; i++)
            {
                options.Add(textureNames[i]);
            }
            textureDropdown.ClearOptions();
            textureDropdown.AddOptions(options);
            textureDropdown.onValueChanged.AddListener(delegate { OnTextureSelected(textureDropdown); });
        }


        void OnTextureSelected(TMP_Dropdown dropdown)
        {
            selectedTextureIndex = dropdown.value;
            // Muda a textura global para o que foi selecionado
            Sandbox.SetShaderTexture("_WaterColorTex", WaterColorTextures[selectedTextureIndex]);
            //Debug.Log("Textura selecionada: " + WaterColorTextures[selectedTextureIndex].name);
        }

        void ConfigureViscositySlider()
        {
            viscositySlider.minValue = 0.0f;  // Defina o valor mínimo da viscosidade
            viscositySlider.maxValue = 1.0f; // Defina o valor máximo da viscosidade
            viscositySlider.value = selectedViscosity; // Defina o valor inicial do slider
            viscositySlider.onValueChanged.AddListener(OnViscosityChanged); // Associa o método ao evento de alteração
        }

        // Método chamado quando o valor do slider é alterado
        public void OnViscosityChanged(float value)
        {
            selectedViscosity = value; // Atualiza a viscosidade selecionada
            //Debug.Log("Viscosidade alterada para: " + selectedViscosity);
        }

        void ConfigureAbsorptionEvaporationSlider()
        {
            // Configuração do slider de absorção
            if (absorptionSlider != null)
            {
                absorptionSlider.minValue = 1.0f; // Valor mínimo da velocidade de absorção
                absorptionSlider.maxValue = 100.0f; // Valor máximo da velocidade de absorção
                absorptionSlider.value = absorptionSpeed; // Valor inicial
                absorptionSlider.onValueChanged.AddListener(OnAbsorptionChanged);
            }

            // Configuração do slider de evaporação
            if (evaporationSlider != null)
            {
                evaporationSlider.minValue = 1.0f; // Valor mínimo do tempo de evaporação
                evaporationSlider.maxValue = 100.0f; // Valor máximo do tempo de evaporação
                evaporationSlider.value = evaporationTime; // Valor inicial
                evaporationSlider.onValueChanged.AddListener(OnEvaporationChanged);
            }
        }

        // Método chamado quando o slider de absorção é alterado
        public void OnAbsorptionChanged(float value)
        {
            absorptionSpeed = value; // Atualiza a velocidade de absorção
            //Debug.Log("Velocidade de absorção alterada para: " + absorptionSpeed);
        }

        // Método chamado quando o slider de evaporação é alterado
        public void OnEvaporationChanged(float value)
        {
            evaporationTime = value; // Atualiza o tempo de evaporação
            //Debug.Log("Tempo de evaporação alterado para: " + evaporationTime);
        }

        void ConfigureWaterfallControls()
        {
            waterfallToggle.onValueChanged.AddListener(OnWaterfallToggleChanged);
            emissionRateSlider.minValue = 0.5f;
            emissionRateSlider.maxValue = 5.0f;
            emissionRateSlider.value = emissionRate;
            emissionRateSlider.onValueChanged.AddListener(OnEmissionRateChanged);
        }

        //ta errado
        public void OnWaterfallToggleChanged(bool isOn)
        {
            Debug.Log("OnWaterfallToggleChanged");
            isWaterfallActive = isOn;

    
        }

        private void OnEmissionRateChanged(float value)
        {
            Debug.Log("OnEmissionRateChanged");
            emissionRate = value;
        }

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
                
                // --- ATUALIZAÇÃO DO AUDIO ---
                UpdateAudio();

                // --- ATUALIZAÇÃO DA UMIDADE (WETNESS) ---
                UpdateWetnessMap();
                
                if (Random.value < 2 / 60.0f)
                {
                    DisturbWaterSurfaceSimulation();
                }
                yield return new WaitForSeconds(1 / 60.0f);
            }
        }
        
        private void UpdateAudio()
        {
            // 1. Som de Chuva (Mão Aberta)
            // Se tivermos gestos da mão detectados pelo HandInput, toca o som
            if (HandInput.GetCurrentGestures().Count > 0)
            {
                if (!rainAudioSource.isPlaying) rainAudioSource.Play();
            }
            else
            {
                if (rainAudioSource.isPlaying) rainAudioSource.Stop();
            }

            // 2. Som de Cascata (Waterfall)
            // Se tiver emissores de cascata ativos
            if (activeWaterfallEmitters.Count > 0)
            {
                if (!waterfallAudioSource.isPlaying) waterfallAudioSource.Play();
            }
            else
            {
                if (waterfallAudioSource.isPlaying) waterfallAudioSource.Stop();
            }
        }

        private void UpdateWetnessMap()
        {
            // Define qual textura de água usar (a atual do buffer swap)
            RenderTexture currentWater = swapBuffers ? waterBufferRT0 : waterBufferRT1;
            
            // Roda o Compute Shader para calcular onde está molhado
            WaterSurfaceCSHelper.Run_UpdateWetness(WaterSurfaceComputeShader, wetnessRT, currentWater, DryingSpeed);

            // Envia essa textura para o Shader do Terreno (Sandbox)
            // O nome "_WetnessTex" deve existir no Shader do terreno para funcionar visualmente
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

            StopCoroutine(RunSimulationCoroutine);
            
            // Para os sons
            if(rainAudioSource) rainAudioSource.Stop();
            if(waterfallAudioSource) waterfallAudioSource.Stop();
        }

        private void OnCalibration()
        {
            HandInput.OnGesturesReady -= OnGesturesReady;
            MetaballCamera.gameObject.SetActive(false);
            DestroyWaterDroplets();
            Sandbox.SetDefaultShader();

            StopCoroutine(RunSimulationCoroutine);
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
            
            // Criar textura de Molhado (Wetness)
            wetnessRT = new RenderTexture(256, 256, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            wetnessRT.filterMode = FilterMode.Bilinear;
            wetnessRT.enableRandomWrite = true;
            wetnessRT.Create();
            WaterSurfaceCSHelper.Run_FillRenderTexture(WaterSurfaceComputeShader, wetnessRT, 0.0f); // Começa seco
            
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
                        if (droplet.transform.position.z > sandboxDepth)
                        {
                            droplet.SetZPosition(sandboxDepth - WaterDroplet.DROPLET_RADIUS * 1.25f);
                        }
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
                            // Constant of 10 is a small buffer to allow stacked water to rest.
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
                // Checa se o droplet ainda existe antes de continuar
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
                    Destroy(droplet);
                    waterDroplets.RemoveAt(i);
                } else
                {
                    metaballCount++;
                    if (metaballCount > MaxMetaballs)
                    {
                        Destroy(droplet.gameObject);
                        Destroy(droplet);
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
            // Aqui, utilizamos a textura selecionada pelo usuário, referenciada pelo índice selectedTextureIndex
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
            foreach (HandInputGesture gesture in HandInput.GetCurrentGestures())
            {
                if (!gesture.OutOfBounds)
                {
                    if (!Physics.CheckSphere(gesture.WorldPosition + new Vector3(0, 0, -5), 1.0f))
                    {
                        if (isWaterfallActive)
                        {
                            GameObject newEmitter = Instantiate(WaterfallEmitterPrefab, gesture.WorldPosition, Quaternion.identity);
                            var emitterScript = newEmitter.GetComponent<S_WaterfallEmitter>();
                            emitterScript.Initialize(WaterDroplet, selectedViscosity, absorptionSpeed, evaporationTime, WaterColorTextures[selectedTextureIndex]);
                            emitterScript.SetEmissionRate(emissionRate);
                            emitterScript.SetShowMesh(showParticles); // Passa o estado atual de showParticles
                            activeWaterfallEmitters.Add(newEmitter);
                        }
                        else
                        {
                            WaterDroplet waterDroplet = Instantiate(WaterDroplet, gesture.WorldPosition, Quaternion.identity);
                            waterDroplet.SetShowMesh(showParticles);
                            waterDroplet.SetViscosity(selectedViscosity); // Aplica a viscosidade
                            waterDroplet.SetAbsorptionSpeed(absorptionSpeed); // Aplica a absorção
                            waterDroplet.SetEvaporationTime(evaporationTime); // Aplica a evaporação
                            waterDroplets.Add(waterDroplet);
                        }
                    }
                }
            }
        }

        public void UI_DestroyWaterfalls()
        {
            foreach (GameObject emitter in activeWaterfallEmitters)
            {
                if (emitter != null) Destroy(emitter);
            }
            activeWaterfallEmitters.Clear();
        }

        private void DestroyWaterDroplets()
        {
            foreach (WaterDroplet droplet in waterDroplets)
            {
                Destroy(droplet);
            }
            waterDroplets.Clear();
        }

        public void UI_DestroyWaterDroplets()
        {
            DestroyWaterDroplets();
        }

        public void UI_ToggleShowParticles(bool showParticles)
        {
            this.showParticles = showParticles;
            foreach (WaterDroplet droplet in waterDroplets)
            {
                droplet.SetShowMesh(showParticles);
            }

            // Aplica o efeito às gotas da cascata
            foreach (GameObject emitter in activeWaterfallEmitters)
            {
                if (emitter != null)
                {
                    var emitterScript = emitter.GetComponent<S_WaterfallEmitter>();
                    if (emitterScript != null)
                    {
                        emitterScript.SetShowMesh(showParticles);
                    }
                }
            }
        }
    }
}
