using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ColorDetector : MonoBehaviour
{
    [SerializeField] private WebcamDisplay webcamDisplay;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private Transform playerShip;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private RawImage webcamRawImage;
    [SerializeField] private Camera mainCamera;

    [Header("Detection Settings")]
    [SerializeField] private int scanFrequency = 30; // Cada cuántos frames actualizar
    [SerializeField] private int samplePoints = 100; // Número de puntos de muestra en la imagen
    [SerializeField] private float minObjectSize = 10f; // Tamaño mínimo para considerar un objeto

    [Header("Detection Visualization")]
    [SerializeField] private GameObject detectionMarkerPrefab;
    [SerializeField] private bool showDetectionMarker = true;
    [SerializeField] private float markerSpawnInterval = 0.2f;
    [SerializeField] private bool moveShipToRedCircle = true; // Usar el centro del círculo rojo para la nave

    [Header("Debug")]
    public bool showDebugInfo = true;
    [SerializeField] private TMP_Text detectionStatusText;
    [SerializeField] private TMP_Text pixelCountText;
    [SerializeField] private TMP_Text pixelThresholdText;
    [SerializeField] private DebugVisualizer debugVisualizer;
    [SerializeField] private bool createDebugTexture = false;
    [SerializeField] private bool useHSVDetection = true;

    private Color targetColor;
    private float colorTolerance;
    private WebCamTexture webcamTexture;
    private int frameCounter = 0;
    private Vector2 lastDetectedPosition;
    private bool objectDetected = false;
    private float lastMarkerTime = 0f;
    private RenderTexture debugRenderTexture; // Para capturar la textura de depuración

    [Header("Direct Positioning")]
    [SerializeField] private bool createVisibleRedCircle = true; // Crea un objeto visible en la posición del círculo rojo
    [SerializeField] private GameObject redCirclePrefab; // Prefab para el círculo rojo visible
    private Vector3 redCircleWorldPosition;

    void Start()
    {
        // Inicializar el tiempo del último marcador
        lastMarkerTime = 0f;

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

        // Verificar la referencia al RawImage
        if (webcamRawImage == null && webcamDisplay != null)
        {
            webcamRawImage = webcamDisplay.GetComponent<RawImage>();
            if (webcamRawImage == null)
            {
                webcamRawImage = webcamDisplay.GetComponentInChildren<RawImage>();
                if (webcamRawImage == null)
                {
                    Debug.LogError("No se pudo encontrar RawImage en webcamDisplay o sus hijos. Por favor, asígnalo manualmente.");
                }
                else
                {
                    Debug.Log("RawImage encontrado en los hijos de webcamDisplay");
                }
            }
            else
            {
                Debug.Log("RawImage encontrado en webcamDisplay");
            }
        }

        // Obtener la referencia a la textura de la webcam
        if (webcamDisplay != null)
        {
            // Esperar a que la cámara se inicialice
            StartCoroutine(WaitForWebcamInitialization());
        }
        else
        {
            Debug.LogError("WebcamDisplay no asignado en ColorDetector");
        }

        // Inicializar texto de estado
        if (detectionStatusText != null)
        {
            detectionStatusText.text = "No se detecta objeto";
            detectionStatusText.color = Color.red;
        }

        // Verificar si hay referencia del Canvas
        if (canvasRectTransform == null)
        {
            Debug.LogWarning("Canvas RectTransform no asignado en ColorDetector. Intentando encontrar Canvas en padres...");
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
                Debug.Log("Canvas encontrado automáticamente.");
            }
            else
            {
                Debug.LogError("No se pudo encontrar un Canvas en la escena. Por favor, asigna manualmente la referencia al Canvas.");
            }
        }

        // Verificar si hay referencia a la cámara
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No se pudo encontrar la cámara principal. Por favor, asigna una referencia manualmente.");
            }
        }

        if (createVisibleRedCircle && redCirclePrefab == null)
        {
            // Crear un prefab simple si no se proporciona uno
            GameObject circleObj = new GameObject("RedCirclePrefab");
            SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();

            // Crear una textura circular roja
            Texture2D tex = new Texture2D(32, 32);
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(tex.width / 2, tex.height / 2));
                    if (distFromCenter <= tex.width / 2)
                    {
                        tex.SetPixel(x, y, Color.red);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
            tex.Apply();

            // Crear sprite y asignarlo
            Sprite circleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            sr.sprite = circleSprite;

            // Guardar como prefab
            redCirclePrefab = circleObj;
            redCirclePrefab.SetActive(false); // Ocultar el original
        }

        CheckCameraSetup();
    }

    private System.Collections.IEnumerator WaitForWebcamInitialization()
    {
        // Esperar hasta que la webcam se inicialice completamente
        while (webcamDisplay.webcamTexture == null || !webcamDisplay.webcamTexture.isPlaying ||
               webcamDisplay.webcamTexture.width <= 16) // Verificar que la textura tenga un tamaño válido
        {
            yield return new WaitForSeconds(0.1f);
        }

        webcamTexture = webcamDisplay.webcamTexture;
        Debug.Log("Webcam inicializada correctamente: " + webcamTexture.width + "x" + webcamTexture.height);
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

            // Actualizar el texto de estado
            UpdateDetectionStatus();
        }

        // No necesitamos mover la nave aquí si estamos usando moveShipToRedCircle
        // ya que se moverá directamente en DetectColorObject
    }

    private void UpdateDetectionStatus()
    {
        if (detectionStatusText != null)
        {
            if (objectDetected)
            {
                detectionStatusText.text = "¡Objeto detectado!";
                detectionStatusText.color = Color.green;
            }
            else
            {
                detectionStatusText.text = "No se detecta objeto";
                detectionStatusText.color = Color.red;
            }
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

        float startTime = Time.realtimeSinceStartup;

        // Lista para almacenar posiciones de píxeles que coinciden con el color
        List<Vector2> matchingPixels = new List<Vector2>();

        // Calcular el paso para distribuir los puntos de muestra uniformemente
        int stepX = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.width / Mathf.Sqrt(samplePoints)));
        int stepY = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.height / Mathf.Sqrt(samplePoints)));

        // Crear una textura de depuración si es necesario
        Texture2D debugTexture = null;
        if (showDebugInfo && createDebugTexture && debugVisualizer != null)
        {
            debugTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);

            // Copiar la textura de la webcam a la textura de depuración
            Color[] pixels = webcamTexture.GetPixels();
            debugTexture.SetPixels(pixels);
        }

        // Recorrer puntos distribuidos en la imagen (no todos los píxeles por rendimiento)
        for (int x = 0; x < webcamTexture.width; x += stepX)
        {
            for (int y = 0; y < webcamTexture.height; y += stepY)
            {
                // Obtener el color del píxel actual
                Color pixelColor = webcamTexture.GetPixel(x, y);

                // Verificar si el color está dentro del rango de tolerancia
                bool isMatch = IsColorMatch(pixelColor, targetColor, colorTolerance);

                if (isMatch)
                {
                    matchingPixels.Add(new Vector2(x, y));

                    // Marcar el píxel en la textura de depuración
                    if (debugTexture != null)
                    {
                        // Dibujar un pequeño cuadrado alrededor del píxel coincidente
                        int size = 4; // Tamaño del marcador
                        for (int dx = -size; dx <= size; dx++)
                        {
                            for (int dy = -size; dy <= size; dy++)
                            {
                                int px = Mathf.Clamp(x + dx, 0, webcamTexture.width - 1);
                                int py = Mathf.Clamp(y + dy, 0, webcamTexture.height - 1);
                                debugTexture.SetPixel(px, py, Color.green);
                            }
                        }
                    }
                }
            }
        }

        // Si encontramos suficientes píxeles coincidentes, calcular el centro del objeto
        if (matchingPixels.Count > minObjectSize)
        {
            Vector2 centerPoint = CalculateCenterPoint(matchingPixels);
            lastDetectedPosition = centerPoint;
            objectDetected = true;

            // Mostrar marcador visual en la posición detectada
            if (showDetectionMarker && detectionMarkerPrefab != null && Time.time - lastMarkerTime > markerSpawnInterval)
            {
                // Verificar que canvasRectTransform no sea nulo
                if (canvasRectTransform != null)
                {
                    // Convertir la posición detectada al espacio del canvas
                    Vector2 canvasPosition = ConvertToCanvasPosition(centerPoint);
                    SpawnDetectionMarker(canvasPosition);
                    lastMarkerTime = Time.time;
                }
                else
                {
                    Debug.LogError("canvasRectTransform es nulo. No se puede convertir la posición para el marcador.");
                }
            }

            // Marcar el centro en la textura de depuración
            if (debugTexture != null)
            {
                // Dibujar un círculo en el centro del objeto detectado
                int radius = 10;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int px = Mathf.Clamp((int)centerPoint.x + dx, 0, webcamTexture.width - 1);
                            int py = Mathf.Clamp((int)centerPoint.y + dy, 0, webcamTexture.height - 1);
                            debugTexture.SetPixel(px, py, Color.red);
                        }
                    }
                }
            }

            // NUEVA IMPLEMENTACIÓN: Mover siempre la nave al centro del objeto detectado
            MoveShipToCenterPoint(centerPoint);
        }
        else
        {
            objectDetected = false;
        }

        // Actualizar información de depuración
        if (showDebugInfo)
        {
            if (pixelCountText != null && pixelThresholdText != null)
            {
                pixelCountText.text = "Píxeles detectados: " + matchingPixels.Count;
                pixelThresholdText.text = "Umbral mínimo: " + minObjectSize;
            }

            // Actualizar la textura de depuración
            if (debugTexture != null)
            {
                debugTexture.Apply();
                debugVisualizer.UpdateDebugImage(debugTexture);
            }

            // Registrar el tiempo de detección
            float detectionTime = Time.realtimeSinceStartup - startTime;
            if (debugVisualizer != null)
            {
                debugVisualizer.UpdateDetectionTime(detectionTime);
            }
        }
    }

    private void CreateVisibleRedCircle(Vector2 centerPoint)
    {
        // Eliminar cualquier círculo anterior
        GameObject existingCircle = GameObject.Find("VisibleRedCircle");
        if (existingCircle != null)
        {
            Destroy(existingCircle);
        }

        // Crear un nuevo círculo rojo visible usando el prefab
        if (redCirclePrefab != null)
        {
            // Convertir coordenadas de textura a coordenadas normalizadas
            Vector2 normalizedPos = new Vector2(
                centerPoint.x / webcamTexture.width,
                1 - (centerPoint.y / webcamTexture.height) // Invertir Y
            );

            // Obtener las esquinas del RawImage en coordenadas de mundo
            RectTransform rt = webcamRawImage.rectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            // Calcular la posición exacta en el mundo
            Vector3 worldPos = new Vector3(
                Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x),
                Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y),
                -1 // Delante del canvas
            );

            // Guardar la posición mundial para usar con la nave
            redCircleWorldPosition = worldPos;

            // Instanciar el círculo rojo visible
            GameObject circle = Instantiate(redCirclePrefab, worldPos, Quaternion.identity);
            circle.name = "VisibleRedCircle";
            circle.SetActive(true);

            // Hacer que el círculo sea pequeño
            circle.transform.localScale = Vector3.one * 0.1f;

            Debug.Log($"Círculo rojo visible creado en posición mundial: {worldPos}");
        }
    }

    // Método nuevo para mover la nave a la posición del círculo rojo
    private void MoveShipToDebugPosition(Vector2 centerPoint)
    {
        // Obtener la posición normalizada (0-1) en la textura de la webcam
        Vector2 normalizedPos = new Vector2(
            centerPoint.x / webcamTexture.width,
            1f - (centerPoint.y / webcamTexture.height) // Invertir Y para UI
        );

        // Obtener las esquinas del RawImage en coordenadas de mundo
        RectTransform rt = webcamRawImage.rectTransform;
        if (rt == null)
        {
            Debug.LogError("No se pudo obtener el RectTransform del RawImage");
            return;
        }

        // Obtener las esquinas del RawImage en el mundo
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // corners[0] = esquina inferior izquierda
        // corners[2] = esquina superior derecha

        // Calcular la posición exacta dentro del RawImage basado en la posición normalizada
        Vector3 exactPosition = new Vector3(
            Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x),
            Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y),
            playerShip.position.z // Mantener Z
        );

        // Mover la nave
        PlayerShip shipController = playerShip.GetComponent<PlayerShip>();
        if (shipController != null)
        {
            shipController.SetTargetPosition(exactPosition);
        }
        else
        {
            // Movimiento manual con interpolación
            playerShip.position = Vector3.Lerp(
                playerShip.position,
                exactPosition,
                Time.deltaTime * 10f
            );
        }

        // Debug visual - dibujar una línea para ver dónde se está moviendo la nave
        Debug.DrawLine(
            playerShip.position,
            exactPosition,
            Color.yellow,
            0.1f
        );

        Debug.Log($"Moviendo nave a: {exactPosition} (desde centro en textura: {centerPoint})");
    }

    private bool IsColorMatch(Color pixel, Color target, float tolerance)
    {
        if (useHSVDetection)
        {
            // Convertir RGB a HSV para mejor detección de color
            float pixelH, pixelS, pixelV;
            float targetH, targetS, targetV;

            Color.RGBToHSV(pixel, out pixelH, out pixelS, out pixelV);
            Color.RGBToHSV(target, out targetH, out targetS, out targetV);

            // Calcular la diferencia en matiz (H) teniendo en cuenta que es circular (0-1)
            float hDiff = Mathf.Min(Mathf.Abs(pixelH - targetH), 1 - Mathf.Abs(pixelH - targetH));

            // Calcular las diferencias en saturación (S) y valor (V)
            float sDiff = Mathf.Abs(pixelS - targetS);
            float vDiff = Mathf.Abs(pixelV - targetV);

            // Ajustar las ponderaciones para dar más importancia al matiz y menor escala a la tolerancia
            float hWeight = 2.0f;
            float sWeight = 1.0f;
            float vWeight = 0.5f;

            // Normalizar el peso total
            float totalWeight = hWeight + sWeight + vWeight;

            // Aplicar una escala no lineal a la tolerancia para que tenga un comportamiento más intuitivo
            // Esto hace que la tolerancia sea más restrictiva incluso con valores altos
            float scaledTolerance = Mathf.Pow(tolerance, 1.5f) * 0.5f;

            // Calcular la diferencia ponderada
            float weightedDiff = (hDiff * hWeight + sDiff * sWeight + vDiff * vWeight) / totalWeight;

            // Usar un umbral más estricto para el matiz cuando la saturación es alta
            // Esto evita que colores diferentes pero con la misma luminosidad se consideren iguales
            if (targetS > 0.3f && pixelS > 0.3f)
            {
                if (hDiff > tolerance * 0.7f)
                {
                    return false;
                }
            }

            // Aplicar umbrales máximos absolutos para cada componente
            // Esto impide que la tolerancia alta permita cambios demasiado drásticos
            float maxHDiff = Mathf.Min(0.25f, tolerance);
            float maxSDiff = Mathf.Min(0.5f, tolerance * 1.2f);
            float maxVDiff = Mathf.Min(0.5f, tolerance * 1.5f);

            if (hDiff > maxHDiff || sDiff > maxSDiff || vDiff > maxVDiff)
            {
                return false;
            }

            // Verificar si la diferencia ponderada está dentro del rango de tolerancia
            return weightedDiff < scaledTolerance;
        }
        else
        {
            // Método RGB mejorado
            float rDiff = Mathf.Abs(pixel.r - target.r);
            float gDiff = Mathf.Abs(pixel.g - target.g);
            float bDiff = Mathf.Abs(pixel.b - target.b);

            // Calcular la diferencia promedio
            float avgDiff = (rDiff + gDiff + bDiff) / 3f;

            // Aplicar una escala no lineal a la tolerancia
            float scaledTolerance = Mathf.Pow(tolerance, 1.5f) * 0.5f;

            // Establecer límites máximos para cada componente
            float maxComponentDiff = Mathf.Min(0.4f, tolerance * 1.2f);

            // Verificar diferencias por componente y la diferencia promedio
            return (rDiff < maxComponentDiff &&
                    gDiff < maxComponentDiff &&
                    bDiff < maxComponentDiff &&
                    avgDiff < scaledTolerance);
        }
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

    private Vector2 ConvertToCanvasPosition(Vector2 texturePosition)
    {
        // Verificar que las referencias necesarias no sean nulas
        if (canvasRectTransform == null)
        {
            Debug.LogError("canvasRectTransform es nulo en ConvertToCanvasPosition");
            return Vector2.zero;
        }

        // Obtener el RectTransform del RawImage
        RectTransform imageRectTransform = null;

        if (webcamRawImage != null)
        {
            imageRectTransform = webcamRawImage.rectTransform;
        }
        else if (webcamDisplay != null)
        {
            // Intentar obtener el RawImage nuevamente
            RawImage rawImage = webcamDisplay.GetComponent<RawImage>();
            if (rawImage != null)
            {
                webcamRawImage = rawImage; // Guardar para futuros usos
                imageRectTransform = rawImage.rectTransform;
            }
            else
            {
                Debug.LogError("No se encontró RawImage en webcamDisplay");
                return Vector2.zero;
            }
        }
        else
        {
            Debug.LogError("webcamDisplay es nulo en ConvertToCanvasPosition");
            return Vector2.zero;
        }

        if (imageRectTransform == null)
        {
            Debug.LogError("No se encontró RectTransform en el RawImage");
            return Vector2.zero;
        }

        // Convertir de coordenadas de textura a coordenadas normalizadas (0-1)
        float normalizedX = texturePosition.x / webcamTexture.width;
        float normalizedY = texturePosition.y / webcamTexture.height;

        // En las texturas de webcam, el origen Y está invertido respecto a la UI
        normalizedY = 1 - normalizedY;

        // Obtener la posición del RawImage en el canvas
        float imageWidth = imageRectTransform.rect.width;
        float imageHeight = imageRectTransform.rect.height;

        // Calcular la posición en el canvas
        Vector3 imagePosition = imageRectTransform.position;
        float imageLeft = imagePosition.x - (imageWidth / 2);
        float imageBottom = imagePosition.y - (imageHeight / 2);

        float canvasX = imageLeft + (normalizedX * imageWidth);
        float canvasY = imageBottom + (normalizedY * imageHeight);

        return new Vector2(canvasX, canvasY);
    }

    // Método para instanciar el marcador de detección
    private void SpawnDetectionMarker(Vector2 position)
    {
        if (detectionMarkerPrefab == null)
        {
            Debug.LogError("detectionMarkerPrefab es nulo. No se puede instanciar el marcador.");
            return;
        }

        if (canvasRectTransform == null)
        {
            Debug.LogError("canvasRectTransform es nulo. No se puede instanciar el marcador como hijo del canvas.");
            return;
        }

        try
        {
            // Instanciar el marcador como hijo del canvas
            GameObject markerObj = Instantiate(detectionMarkerPrefab, position, Quaternion.identity, canvasRectTransform);

            // Configurar el marcador
            DetectionMarker marker = markerObj.GetComponent<DetectionMarker>();
            if (marker != null)
            {
                // Configurar la posición exacta
                marker.SetPosition(position);

                // Configurar el color (puede ser el color detectado o un color distintivo)
                marker.SetColor(new Color(targetColor.r, targetColor.g, targetColor.b, 0.7f));

                Debug.Log($"Marcador de detección instanciado en posición: {position}");
            }
            else
            {
                Debug.LogWarning("El prefab no tiene el componente DetectionMarker");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al instanciar el marcador: " + e.Message);
        }
    }

    // Reemplaza el método MoveShipToCenterPoint() en ColorDetector.cs con este:

    // Modificación para el método MoveShipToCenterPoint() en ColorDetector.cs

    private void MoveShipToCenterPoint(Vector2 centerPoint)
    {
        if (playerShip == null)
        {
            Debug.LogError("playerShip no está asignado en ColorDetector");
            return;
        }

        if (webcamRawImage == null)
        {
            Debug.LogError("webcamRawImage no está asignado en ColorDetector");
            return;
        }

        // IMPORTANTE: Problema identificado - el eje Y está invertido
        // 1. Convertir de coordenadas de textura a normalizadas (0-1)
        // CORRECCIÓN: No invertimos el eje Y aquí, ya que eso causa la inversión
        Vector2 normalizedPos = new Vector2(
            centerPoint.x / webcamTexture.width,
            centerPoint.y / webcamTexture.height  // CAMBIO AQUÍ: Quitamos el 1.0f - ...
        );

        // Alternativamente, si lo anterior no funciona, invertir la lógica aquí:
        // Intentar segunda solución si la primera no funciona
        // Vector2 normalizedPos = new Vector2(
        //     centerPoint.x / webcamTexture.width,
        //     1.0f - (centerPoint.y / webcamTexture.height)  // Mantener la inversión
        // );
        // // Pero luego invertir la interpolación en Y:
        // worldPos.y = Mathf.Lerp(corners[2].y, corners[0].y, normalizedPos.y);  // Invertir orden de corners

        // 2. Obtener las dimensiones y posición del RawImage
        RectTransform rt = webcamRawImage.rectTransform;

        // Obtener posición y tamaño del RawImage en el espacio del mundo
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // corners[0] = Bottom-Left, corners[1] = Top-Left, 
        // corners[2] = Top-Right, corners[3] = Bottom-Right

        // 3. Calcular la posición exacta dentro del espacio del RawImage
        Vector3 worldPos = Vector3.zero;

        // Usar transformación directa basada en las esquinas del RawImage
        worldPos.x = Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x);

        // CORRECCIÓN: Invertir el orden de interpolación para el eje Y
        // Ya que no invertimos en la normalización, debemos interpolar
        // desde la esquina inferior a la superior (no al revés)
        worldPos.y = Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y);

        // Si la solución anterior no funciona, prueba esta alternativa:
        // worldPos.y = Mathf.Lerp(corners[2].y, corners[0].y, normalizedPos.y);

        worldPos.z = playerShip.position.z; // Mantener Z

        // Debug avanzado - mostrar todos los pasos de conversión
        Debug.Log($"Detección: Pixel={centerPoint}, Normalizado={normalizedPos}, " +
                  $"Esquinas=[Bot-Left:{corners[0]}, Top-Left:{corners[1]}, Top-Right:{corners[2]}, Bot-Right:{corners[3]}], " +
                  $"Final={worldPos}");

        // Guardar posición para el círculo rojo
        redCircleWorldPosition = worldPos;

        // IMPORTANTE: Actualizar directamente la posición sin animación
        playerShip.position = worldPos;

        // Crear visualización del círculo rojo para depuración
        if (createVisibleRedCircle && redCirclePrefab != null)
        {
            // Eliminar círculo anterior
            GameObject existingCircle = GameObject.Find("VisibleRedCircle");
            if (existingCircle != null)
            {
                Destroy(existingCircle);
            }

            // Crear nuevo círculo en la posición calculada
            GameObject circle = Instantiate(redCirclePrefab, worldPos, Quaternion.identity);
            circle.name = "VisibleRedCircle";
            circle.SetActive(true);
            circle.transform.localScale = Vector3.one * 0.1f;
        }
    }

    private void CheckCameraSetup()
    {
        // Verificar la cámara principal
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            Debug.Log("Se ha asignado automáticamente la cámara principal: " +
                      (mainCamera != null ? mainCamera.name : "No encontrada"));
        }

        // Verificar si estamos usando una cámara ortográfica como se recomienda para 2D
        if (mainCamera != null && !mainCamera.orthographic)
        {
            Debug.LogWarning("La cámara principal no es ortográfica. Se recomienda usar una cámara ortográfica para juegos 2D.");
        }

        // Verificar la configuración del Canvas
        Canvas canvas = null;
        if (canvasRectTransform != null)
        {
            canvas = canvasRectTransform.GetComponent<Canvas>();
        }

        if (canvas != null)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("El Canvas no está configurado como World Space. Modo actual: " + canvas.renderMode);
            }

            if (canvas.worldCamera != mainCamera)
            {
                Debug.LogWarning("El Canvas está usando una cámara diferente a mainCamera. " +
                                "Canvas.worldCamera: " + (canvas.worldCamera != null ? canvas.worldCamera.name : "null") +
                                ", mainCamera: " + (mainCamera != null ? mainCamera.name : "null"));
            }
        }

        // Imprimir información sobre capas y culling mask
        if (mainCamera != null && playerShip != null)
        {
            int shipLayer = playerShip.gameObject.layer;
            bool isLayerVisible = ((1 << shipLayer) & mainCamera.cullingMask) != 0;

            Debug.Log($"Nave en capa: {LayerMask.LayerToName(shipLayer)} (índice {shipLayer}). " +
                     $"Visible para mainCamera: {isLayerVisible}");
        }
    }
}