using UnityEngine;
using UnityEngine.UI;

public class DetectionMarker : MonoBehaviour
{
    [SerializeField] private Image markerImage;
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private float initialSize = 30f;
    [SerializeField] private float growthSpeed = 15f;
    [SerializeField] private float maxSize = 60f;

    private RectTransform rectTransform;
    private Color originalColor;
    private float currentAlpha = 1.0f;
    private float currentSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (markerImage == null)
        {
            markerImage = GetComponent<Image>();
        }

        if (markerImage != null)
        {
            originalColor = markerImage.color;
            currentSize = initialSize;
            UpdateSize(currentSize);
        }
        else
        {
            Debug.LogError("No se encontró un componente Image en el marcador de detección");
        }
    }

    void Update()
    {
        // Efecto de desvanecimiento
        currentAlpha -= fadeSpeed * Time.deltaTime;

        if (currentAlpha <= 0)
        {
            Destroy(gameObject); // Autodestruir cuando sea completamente transparente
            return;
        }

        // Aplicar alpha y escala
        if (markerImage != null)
        {
            Color newColor = originalColor;
            newColor.a = currentAlpha;
            markerImage.color = newColor;

            // Efecto de crecimiento
            currentSize += growthSpeed * Time.deltaTime;
            if (currentSize > maxSize)
            {
                currentSize = maxSize;
            }

            UpdateSize(currentSize);
        }
    }

    private void UpdateSize(float size)
    {
        if (rectTransform != null)
        {
            Vector2 newSize = new Vector2(size, size);
            rectTransform.sizeDelta = newSize;
        }
    }

    // Método público para configurar la posición del marcador
    public void SetPosition(Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.position = new Vector3(position.x, position.y, 0);
        }
    }

    // Método público para configurar el color del marcador
    public void SetColor(Color color)
    {
        if (markerImage != null)
        {
            originalColor = color;
            // Preservar el alpha actual
            Color newColor = originalColor;
            newColor.a = currentAlpha;
            markerImage.color = newColor;
        }
    }
}