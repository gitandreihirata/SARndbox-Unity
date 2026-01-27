using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARSandbox.WaterSimulation
{
    public class S_WaterfallEmitter : MonoBehaviour
    {
        private WaterDroplet WaterDropletPrefab;
        private float emissionRate = 1.0f;
        private float viscosity;
        private float absorptionSpeed;
        private float evaporationTime;
        private Texture2D waterTexture;
        private Coroutine emissionCoroutine;
        private bool showParticles = false;

        // Lista para armazenar as gotas de água emitidas
        private List<WaterDroplet> emittedDroplets = new List<WaterDroplet>();

        public void SetEmissionRate(float rate)
        {
            emissionRate = rate;
            if (emissionCoroutine != null)
            {
                StopCoroutine(emissionCoroutine);
                emissionCoroutine = StartCoroutine(EmitWaterDroplets());
            }
        }

        public void Initialize(WaterDroplet dropletPrefab, float viscosity, float absorptionSpeed, float evaporationTime, Texture2D waterTexture)
        {
            this.WaterDropletPrefab = dropletPrefab;
            this.viscosity = viscosity;
            this.absorptionSpeed = absorptionSpeed;
            this.evaporationTime = evaporationTime;
            this.waterTexture = waterTexture;

            if (emissionCoroutine == null)
            {
                emissionCoroutine = StartCoroutine(EmitWaterDroplets());
            }
        }

        // Método para definir a visibilidade das gotas emitidas
        public void SetShowMesh(bool showParticles)
        {
            this.showParticles = showParticles;

            // Aplica a visibilidade para todas as gotas já emitidas
            foreach (var droplet in emittedDroplets)
            {
                if (droplet != null) // Verifica se a gota ainda existe
                {
                    droplet.SetShowMesh(showParticles);
                }
            }
        }
        private IEnumerator EmitWaterDroplets()
        {
            while (true)
            {
                //Debug.Log("EmitWaterDroplets");
                WaterDroplet droplet = Instantiate(WaterDropletPrefab, transform.position, Quaternion.identity);
                droplet.SetViscosity(viscosity);
                droplet.SetAbsorptionSpeed(absorptionSpeed);
                droplet.SetEvaporationTime(evaporationTime);
                //droplet.SetWaterTexture(waterTexture);
                droplet.SetShowMesh(showParticles);
                // Adiciona a gota à lista para controle de visibilidade
                emittedDroplets.Add(droplet);
                yield return new WaitForSeconds(1.0f / emissionRate);
            }
        }
    }
}
