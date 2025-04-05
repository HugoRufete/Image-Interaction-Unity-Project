using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugVisualizer : MonoBehaviour
{
    [SerializeField] private ColorDetector colorDetector;
    [SerializeField] private RawImage debugImage;
    [SerializeField] private GameObject debugPanel;

    [Header("Performance Info")]
    [SerializeField] private TMP_Text fpsText;

    private float deltaTime = 0.0f;
    private float detectionTime = 0.0f;
    private bool showDebug = false;

    void Start()
    {
        // Inicializar panel de depuración
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }
    }

    void Update()
    {
        // Actualizar cálculo de FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        if (fpsText != null)
        {
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";

            // Cambiar color según rendimiento
            if (fps < 30)
                fpsText.color = Color.red;
            else if (fps < 60)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.green;
        }

    }

    private void OnDebugToggleChanged(bool isOn)
    {
        showDebug = isOn;

        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }

        // Informar al detector de color si debe mostrar información de depuración
        if (colorDetector != null)
        {
            // Acceder directamente al campo público
            colorDetector.showDebugInfo = showDebug;
        }
    }

    // Método público para actualizar la imagen de depuración
    public void UpdateDebugImage(Texture2D debugTexture)
    {
        if (debugImage != null && debugTexture != null)
        {
            debugImage.texture = debugTexture;
        }
    }

    // Método público para actualizar el tiempo de detección
    public void UpdateDetectionTime(float time)
    {
        detectionTime = time;
    }
}