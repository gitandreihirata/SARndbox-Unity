//  
//  WaterDroplet.cs
//
//	Copyright 2021 SensiLab, Monash University <sensilab@monash.edu>
//
//  This file is part of sensilab-ar-sandbox.
//
//  sensilab-ar-sandbox is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  sensilab-ar-sandbox is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with sensilab-ar-sandbox.  If not, see <https://www.gnu.org/licenses/>.
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARSandbox.WaterSimulation
{
    public class WaterDroplet : MonoBehaviour
    {
        public GameObject WaterDropletPhysicsPrefab;
        public const float DROPLET_RADIUS = 2.5f;

        private GameObject waterDropletPhysics;
        private bool showMesh = false;
        private float viscosity = 1.0f; // Define a viscosidade padrão

        // Parâmetros para absorção e evaporação
        public float absorptionSpeed = 0.5f; // Velocidade de absorção do solo (quanto maior, mais rápida a absorção)
        public float evaporationTime = 10.0f; // Tempo para a evaporação completa
        private float evaporationTimer = 0.0f;

        private bool isAbsorbing = false;
        private bool isEvaporating = false;

        void Start()
        {
            // Verifica se o prefab foi atribuído corretamente
            if (WaterDropletPhysicsPrefab != null)
            {
                //Debug.Log("Prefab WaterDropletPhysicsPrefab foi atribuído corretamente.");

                // Instancia o WaterDropletPhysicsPrefab
                waterDropletPhysics = Instantiate(WaterDropletPhysicsPrefab, transform.position, Quaternion.identity);
                if (waterDropletPhysics != null)
                {
                    //Debug.Log("WaterDropletPhysics foi instanciado com sucesso.");
                    waterDropletPhysics.GetComponent<MeshRenderer>().enabled = showMesh;

                    // Aplica o material físico ao SphereCollider baseado na viscosidade inicial
                    ApplyPhysicMaterial(viscosity);
                }
                else
                {
                    //Debug.LogError("WaterDropletPhysics não foi instanciado corretamente.");
                }
            }
            else
            {
                Debug.LogError("WaterDropletPhysicsPrefab não está atribuído no inspector.");
            }

        }

        void Update()
        {
            transform.position = waterDropletPhysics.transform.position;

            // Absorção e evaporação
            HandleAbsorption();
            HandleEvaporation();
        }

        void OnDestroy()
        {
            Destroy(waterDropletPhysics);
            Destroy(gameObject);
            Destroy(this);
        }

        public void SetShowMesh(bool showMesh)
        {
            this.showMesh = showMesh;
            if (waterDropletPhysics != null)
            {
                MeshRenderer renderer = waterDropletPhysics.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = showMesh;
                }
            }
        }

        public void SetZPosition(float z)
        {
            Vector3 newPosition = transform.position;
            newPosition.z = z;
            transform.position = newPosition;
            waterDropletPhysics.transform.position = newPosition;
        }

        public void SetViscosity(float viscosity)
        {
            this.viscosity = viscosity;
            ApplyPhysicMaterial(viscosity); // Aplica o material físico baseado na nova viscosidade
        }

        public void SetAbsorptionSpeed(float absorptionSpeed)
        {
            this.absorptionSpeed = absorptionSpeed;
        }

        public void SetEvaporationTime(float evaporationTime)
        {
            this.evaporationTime = evaporationTime;
        }

        private void ApplyPhysicMaterial(float viscosity)
        {
            // Certifique-se de que waterDropletPhysics foi instanciado
            if (waterDropletPhysics != null)
            {
                SphereCollider collider = waterDropletPhysics.GetComponent<SphereCollider>();

                // Verifica se o SphereCollider existe
                if (collider != null)
                {
                    //Debug.Log("Aplicando PhysicMaterial de acordo com a viscosidade: " + viscosity);

                    // Cria um novo PhysicMaterial e ajusta suas propriedades com base na viscosidade
                    PhysicMaterial physicMaterial = new PhysicMaterial();

                    if (viscosity >= 0f && viscosity <= 0.25f)
                    {
                        physicMaterial.frictionCombine = PhysicMaterialCombine.Average;
                        physicMaterial.bounceCombine = PhysicMaterialCombine.Average;
                        physicMaterial.bounciness = 0f; // Baixa elasticidade

                        // Ajusta a fricção de acordo com a viscosidade
                        physicMaterial.dynamicFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f); // Fricção dinâmica ajustada pela viscosidade
                        physicMaterial.staticFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f);  // Fricção estática ajustada pela viscosidade

                    }
                    else if (viscosity > 0.25f && viscosity <= 0.5f)
                    {
                        physicMaterial.frictionCombine = PhysicMaterialCombine.Average;
                        physicMaterial.bounceCombine = PhysicMaterialCombine.Average;
                        physicMaterial.bounciness = 0.5f; // Elasticidade média

                        // Ajusta a fricção de acordo com a viscosidade
                        physicMaterial.dynamicFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f); // Fricção dinâmica ajustada pela viscosidade
                        physicMaterial.staticFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f);  // Fricção estática ajustada pela viscosidade

                    }
                    else if (viscosity > 0.5f && viscosity <= 1f)
                    {
                        physicMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
                        physicMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
                        physicMaterial.bounciness = 1f; // Alta elasticidade

                        // Ajusta a fricção de acordo com a viscosidade
                        physicMaterial.dynamicFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f); // Fricção dinâmica ajustada pela viscosidade
                        physicMaterial.staticFriction = Mathf.Clamp(viscosity * 0.1f, 0f, 1f);  // Fricção estática ajustada pela viscosidade

                    }

                    // Aplica o PhysicMaterial ao collider
                    collider.material = physicMaterial;

                    // Ajusta o arrasto (drag) no Rigidbody para simular resistência no movimento
                    Rigidbody rb = waterDropletPhysics.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Quanto maior a viscosidade, maior o drag, o que diminui a velocidade de movimento
                        rb.drag = viscosity * 5f; // O multiplicador de 5f pode ser ajustado para um efeito mais forte ou mais fraco
                        rb.angularDrag = viscosity * 2f; // Ajusta a resistência à rotação também
                    }
                }
                else
                {
                    //Debug.Log("O prefab WaterDropletPhysics não contém um componente SphereCollider.");
                }
            }
            else
            {
                //Debug.Log("WaterDropletPhysics não foi instanciado corretamente.");
            }
        }

        private void HandleAbsorption()
        {
            if (!isAbsorbing)
            {
                isAbsorbing = true;
                StartCoroutine(AbsorbIntoGround());
            }
        }

        private void HandleEvaporation()
        {
            evaporationTimer += Time.deltaTime;

            if (evaporationTimer >= evaporationTime)
            {
                isEvaporating = true;
                StartCoroutine(Evaporate());
            }
        }

        private IEnumerator AbsorbIntoGround()
        {
            float elapsedTime = 0.0f;
            Vector3 originalScale = transform.localScale;

            while (elapsedTime < absorptionSpeed)
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsedTime / absorptionSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator Evaporate()
        {
            float elapsedTime = 0.0f;
            Vector3 originalScale = transform.localScale;

            while (elapsedTime < evaporationTime)
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsedTime / evaporationTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        // Recebe o vetor de força do gesto de Swipe (Vento)
        public void ApplyWindForce(Vector2 forceVector)
        {
            if (waterDropletPhysics != null)
            {
                Rigidbody rb = waterDropletPhysics.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Aplica a força de impulso na horizontal (ignorando o Z vertical para não afundar a água na areia)
                    rb.AddForce(new Vector3(forceVector.x, forceVector.y, 0f), ForceMode.Impulse);
                }
            }
        }

    }
}
