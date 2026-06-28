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

        public void SetShowMesh(bool showParticles)
        {
            this.showParticles = showParticles;
            foreach (var droplet in emittedDroplets)
            {
                if (droplet != null) droplet.SetShowMesh(showParticles);
            }
        }
        
        private IEnumerator EmitWaterDroplets()
        {
            while (true)
            {
                WaterDroplet droplet = Instantiate(WaterDropletPrefab, transform.position, Quaternion.identity);
                droplet.SetViscosity(viscosity);
                droplet.SetAbsorptionSpeed(absorptionSpeed);
                droplet.SetEvaporationTime(evaporationTime);
                //droplet.SetWaterTexture(waterTexture);
                droplet.SetShowMesh(showParticles);
                emittedDroplets.Add(droplet);
                yield return new WaitForSeconds(1.0f / emissionRate);
            }
        }
    }
}