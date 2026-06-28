using UnityEngine;
using UnityEngine.UI;

namespace ARSandbox
{
    public class UI_RadarPing : MonoBehaviour
    {
        [Header("Configurações do Pulso")]
        public float speed = 1.5f;        // Velocidade da pulsação
        public float maxScale = 2.0f;     // Tamanho máximo que a mira vai atingir antes de sumir
        
        private RectTransform rect;
        private Image img;
        private Color originalColor;

        void Awake() 
        {
            rect = GetComponent<RectTransform>();
            img = GetComponent<Image>();
            if (img != null) originalColor = img.color;
        }

        void Update() 
        {
            // Cria um loop matemático infinito que vai de 0 a 1
            float progress = (Time.time * speed) % 1f; 
            
            // Faz a mira crescer do tamanho 1 até o limite máximo
            float currentScale = Mathf.Lerp(1f, maxScale, progress);
            rect.localScale = new Vector3(currentScale, currentScale, 1f);

            // Faz a mira ir sumindo (ficando transparente) conforme cresce
            if(img != null) 
            {
                Color c = originalColor;
                c.a = 1f - progress; 
                img.color = c;
            }
        }
    }
}