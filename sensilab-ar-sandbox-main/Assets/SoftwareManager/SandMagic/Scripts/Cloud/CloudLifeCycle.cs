using UnityEngine;

public class CloudLifeCycle : MonoBehaviour
{
    public float LifeTime = 8.0f; // Dura 8 segundos
    public float FadeSpeed = 2.0f; 
    
    private float _timer;
    private QuadStack _quadStack;
    private bool _isDying = false;

    void Start()
    {
        _timer = LifeTime;
        _quadStack = GetComponent<QuadStack>();
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0)
        {
            _isDying = true;
        }

        if (_isDying)
        {
            // Efeito visual: Achata a nuvem até sumir
            if (_quadStack != null)
            {
                _quadStack.cloudHeight = Mathf.Lerp(_quadStack.cloudHeight, 0, Time.deltaTime * FadeSpeed);
                
                if (_quadStack.cloudHeight < 0.05f)
                {
                    Destroy(gameObject); // Tchau nuvem
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}