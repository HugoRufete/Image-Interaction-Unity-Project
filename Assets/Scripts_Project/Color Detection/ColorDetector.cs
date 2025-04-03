using UnityEngine;
using System.Collections.Generic;

public class ColorDetector : MonoBehaviour
{
    [SerializeField] private WebcamDisplay webcamDisplay;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private Transform playerShip;

    [Header("Detection Settings")]
    [SerializeField] private int scanFrequency = 30; // Cada cuántos frames actualizar
    [SerializeField] private int samplePoints = 100; // Número de puntos de muestra en la imagen
    [SerializeField] private float minObjectSize = 10f; // Tamaño mínimo para considerar un objeto

    private Color targetColor;
    private float colorTolerance;
    private WebCamTexture webcamTexture;
    private int frameCounter = 0;
    private Vector2 lastDetectedPosition;
    private bool objectDetected = false;

    void Start()
    {
        // Suscribirse al evento de selección de color
        if (colorPicker != null)
        {
            colorPicker.OnColorSelected += OnColorSelected;
            targetColor = colorPicker.GetSelectedColor();
            colorTolerance = colorPicker.GetColorTolerance();
        }
        else
        {
            Debug.LogError("ColorPicker no asignado en ColorDetector");
        }

        // Obtener la referencia a la textura de la webcam
        if (webcamDisplay != null)
        {
            // Necesitamos modificar WebcamDisplay para exponer la textura
            webcamTexture = webcamDisplay.GetWebcamTexture();

            if (webcamTexture == null)
            {
                Debug.LogError("No se pudo obtener la textura de la webcam");
            }
        }
        else
        {
            Debug.LogError("WebcamDisplay no asignado en ColorDetector");
        }
    }

    void Update()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
            return;

        frameCounter++;

        // Actualizar la detección cada cierto número de frames para mejor rendimiento
        if (frameCounter >= scanFrequency)
        {
            DetectColorObject();
            frameCounter = 0;
        }

        // Si se detectó un objeto, mover la nave del jugador
        if (objectDetected)
        {
            // Convertir la posición detectada al espacio de la pantalla/mundo
            Vector2 worldPosition = ConvertToWorldPosition(lastDetectedPosition);
            playerShip.position = new Vector3(worldPosition.x, worldPosition.y, playerShip.position.z);
        }
    }

    private void OnColorSelected(Color newColor, float newTolerance)
    {
        targetColor = newColor;
        colorTolerance = newTolerance;
        Debug.Log($"Color detector actualizado: {newColor}, Tolerancia: {newTolerance}");
    }

    private void DetectColorObject()
    {
        if (webcamTexture.width <= 0 || webcamTexture.height <= 0)
            return;

        // Lista para almacenar posiciones de píxeles que coinciden con el color
        List<Vector2> matchingPixels = new List<Vector2>();

        // Recorrer puntos distribuidos en la imagen (no todos los píxeles por rendimiento)
        for (int x = 0; x < webcamTexture.width; x += (int)(webcamTexture.width / Mathf.Sqrt(samplePoints)))
        {
            for (int y = 0; y < webcamTexture.height; y += (int)(webcamTexture.height / Mathf.Sqrt(samplePoints)))
            {
                // Obtener el color del píxel actual
                Color pixelColor = webcamTexture.GetPixel(x, y);

                // Verificar si el color está dentro del rango de tolerancia
                if (IsColorMatch(pixelColor, targetColor, colorTolerance))
                {
                    matchingPixels.Add(new Vector2(x, y));
                }
            }
        }

        // Si encontramos suficientes píxeles coincidentes, calcular el centro del objeto
        if (matchingPixels.Count > minObjectSize)
        {
            Vector2 centerPoint = CalculateCenterPoint(matchingPixels);
            lastDetectedPosition = centerPoint;
            objectDetected = true;
        }
        else
        {
            objectDetected = false;
        }
    }

    private bool IsColorMatch(Color pixel, Color target, float tolerance)
    {
        // Calcular la diferencia entre los componentes RGB
        float rDiff = Mathf.Abs(pixel.r - target.r);
        float gDiff = Mathf.Abs(pixel.g - target.g);
        float bDiff = Mathf.Abs(pixel.b - target.b);

        // Verificar si está dentro del rango de tolerancia
        return rDiff < tolerance && gDiff < tolerance && bDiff < tolerance;
    }

    private Vector2 CalculateCenterPoint(List<Vector2> points)
    {
        if (points.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (Vector2 point in points)
        {
            sum += point;
        }

        return sum / points.Count;
    }

    private Vector2 ConvertToWorldPosition(Vector2 texturePosition)
    {
        // Convertir de coordenadas de textura a coordenadas normalizadas (0-1)
        float normalizedX = texturePosition.x / webcamTexture.width;
        float normalizedY = texturePosition.y / webcamTexture.height;

        // Convertir a coordenadas de pantalla
        // Nota: Puede que necesites ajustar esto según la configuración de tu cámara de Unity
        float screenX = normalizedX * Screen.width;
        float screenY = (1 - normalizedY) * Screen.height; // Invertir Y ya que en texturas el origen está arriba

        // Convertir a coordenadas de mundo
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenX, screenY, 10));

        return new Vector2(worldPosition.x, worldPosition.y);
    }
}