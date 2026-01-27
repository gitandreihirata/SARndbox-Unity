using UnityEngine;

public class SimpleCloudBehavior : MonoBehaviour
{
    [Header("Configuração")]
    public float LifeTime = 10.0f; // A nuvem vive por 10 segundos
    public ParticleSystem CloudParts; // Arraste a fumaça aqui
    public ParticleSystem RainParts;  // Arraste a chuva aqui

    private float _timer;
    private bool _isDying = false;

    void Start()
    {
        _timer = LifeTime;
    }

    void Update()
    {
        // Contagem regressiva
        _timer -= Time.deltaTime;

        if (_timer <= 0 && !_isDying)
        {
            StartDying();
        }
    }

    void StartDying()
    {
        _isDying = true;

        // 1. Para de criar novas gotas e fumaça
        if (CloudParts != null)
        {
            var emission = CloudParts.emission;
            emission.enabled = false;
        }
        if (RainParts != null)
        {
            var emission = RainParts.emission;
            emission.enabled = false;
        }

        // 2. Destroi o objeto depois de 3 segundos (para as ultimas gotas caírem)
        Destroy(gameObject, 3.0f);
    }
}