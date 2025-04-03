using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugVisualizer : MonoBehaviour
{
    [SerializeField] private ColorDetector colorDetector;
    [SerializeField] private RawImage debugImage;
    [SerializeField] private Toggle debugToggle;
    [SerializeField] private GameObject debugPanel;

    [Header("Performance Info")]
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text detectionTimeText;

    private float deltaTime = 0.0f;
    private float detectionTime = 0.0f;
    private bool showDebug = false;

    void Start()
    {
        if (debugToggle != null)
        {
            debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
            showDebug = debugToggle.isOn;
        }

        // Inicializar panel de depuraci�n
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }
    }

    void Update()
    {
        // Actualizar c�lculo de FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        if (fpsText != null)
        {
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";

            // Cambiar color seg�n rendimiento
            if (fps < 30)
                fpsText.color = Color.red;
            else if (fps < 60)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.green;
        }

        // Mostrar tiempo de detecci�n
        if (detectionTimeText != null)
        {
            detectionTimeText.text = $"Tiempo de detecci�n: {detectionTime * 1000:F1} ms";
        }
    }

    private void OnDebugToggleChanged(bool isOn)
    {
        showDebug = isOn;

        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }

        // Informar al detector de color si debe mostrar informaci�n de depuraci�n
        if (colorDetector != null)
        {
            // Acceder directamente al campo p�blico
            colorDetector.showDebugInfo = showDebug;
        }
    }

    // M�todo p�blico para actualizar la imagen de depuraci�n
    public void UpdateDebugImage(Texture2D debugTexture)
    {
        if (debugImage != null && debugTexture != null)
        {
            debugImage.texture = debugTexture;
        }
    }

    // M�todo p�blico para actualizar el tiempo de detecci�n
    public void UpdateDetectionTime(float time)
    {
        detectionTime = time;
    }
}