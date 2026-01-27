using UnityEngine;
using System.Collections.Generic;

public class QuadStack : MonoBehaviour
{
    public int horizontalStackSize = 20;
    public float cloudHeight = 1f;
    public Mesh quadMesh; // Arraste o Mesh "Quad" aqui
    public Material cloudMaterial;
    
    // Lista para guardar as fatias reais
    private List<GameObject> _slices = new List<GameObject>();
    private bool _initialized = false;

    void Start()
    {
        BuildCloud();
    }

    void BuildCloud()
    {
        if (cloudMaterial == null || quadMesh == null) return;

        // Limpa fatias antigas se houver
        foreach (var slice in _slices) Destroy(slice);
        _slices.Clear();

        // Cria as fatias reais
        for (int i = 0; i < horizontalStackSize; i++)
        {
            // 1. Cria um objeto vazio filho
            GameObject slice = new GameObject($"CloudSlice_{i}");
            slice.transform.SetParent(this.transform);
            slice.transform.localRotation = Quaternion.identity;
            slice.transform.localScale = Vector3.one;
            
            // 2. Adiciona o visual (Mesh Filter e Renderer)
            MeshFilter mf = slice.AddComponent<MeshFilter>();
            mf.mesh = quadMesh;

            MeshRenderer mr = slice.AddComponent<MeshRenderer>();
            mr.material = cloudMaterial;
            
            // Define a layer igual a do pai
            slice.layer = gameObject.layer;

            // 3. Guarda na lista
            _slices.Add(slice);
        }
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        // Atualiza os valores do Shader (igual antes)
        cloudMaterial.SetFloat("_midYValue", transform.position.y);
        cloudMaterial.SetFloat("_cloudHeight", cloudHeight);
        cloudMaterial.SetVector("_Origin", transform.position);

        // Atualiza as posições das fatias
        float offset = cloudHeight / horizontalStackSize / 2f;
        Vector3 startLocalPos = Vector3.up * (offset * horizontalStackSize / 2f);

        for (int i = 0; i < _slices.Count; i++)
        {
            if (_slices[i] != null)
            {
                // Move cada fatia para o lugar certo
                _slices[i].transform.localPosition = startLocalPos - (Vector3.up * offset * i);
            }
        }
    }
}