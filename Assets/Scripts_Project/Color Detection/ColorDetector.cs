using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private int maxMatchingPixels = 1000; // Tamaño máximo del buffer
    [SerializeField] private float blobProximityThreshold = 20f; // Umbral para considerar píxeles como parte del mismo blob

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

    // Buffers optimizados
    private Color[] pixelBuffer;
    private Vector2[] matchingPixelsBuffer;
    private int matchingPixelCount;
    private bool[] processedPixelBuffer;

    [Header("Direct Positioning")]
    [SerializeField] private bool createVisibleRedCircle = true; // Crea un objeto visible en la posición del círculo rojo
    [SerializeField] private GameObject redCirclePrefab; // Prefab para el círculo rojo visible
    private Vector3 redCircleWorldPosition;

    // Filtrado y suavizado
    private Vector2 filteredPosition;
    [SerializeField] private float positionSmoothFactor = 0.3f;
    [SerializeField] private bool enablePositionSmoothing = true;

    // Control de estabilidad
    [SerializeField] private float minMovementThreshold = 2.5f;
    private Vector2 lastStablePosition;
    private float stabilityTimer = 0f;

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
            }
        }

        // Inicializar buffers
        matchingPixelsBuffer = new Vector2[maxMatchingPixels];
        processedPixelBuffer = new bool[maxMatchingPixels];
        matchingPixelCount = 0;
        filteredPosition = Vector2.zero;
        lastStablePosition = Vector2.zero;

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

        // Configuración del círculo rojo
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

        // Inicializar buffer de píxeles
        pixelBuffer = new Color[webcamTexture.width * webcamTexture.height];

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
            OptimizedDetectColorObject();
            frameCounter = 0;

            // Actualizar el texto de estado
            UpdateDetectionStatus();
        }
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

    private void OptimizedDetectColorObject()
    {
        float startTime = Time.realtimeSinceStartup;

        // Verificar dimensiones de textura
        if (webcamTexture.width <= 0 || webcamTexture.height <= 0)
            return;

        // Verificar y redimensionar buffers si es necesario
        if (pixelBuffer == null || pixelBuffer.Length != webcamTexture.width * webcamTexture.height)
        {
            pixelBuffer = new Color[webcamTexture.width * webcamTexture.height];
        }

        // Calcular el paso para distribuir los puntos de muestra uniformemente
        int stepX = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.width / Mathf.Sqrt(samplePoints)));
        int stepY = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.height / Mathf.Sqrt(samplePoints)));

        Color[] pixels = webcamTexture.GetPixels(0, 0, webcamTexture.width, webcamTexture.height);
        System.Array.Copy(pixels, pixelBuffer, pixels.Length);

        // Resetear contador de píxeles coincidentes
        matchingPixelCount = 0;

        // Crear una textura de depuración si es necesario
        Texture2D debugTexture = null;
        if (showDebugInfo && createDebugTexture && debugVisualizer != null)
        {
            debugTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
            debugTexture.SetPixels(pixelBuffer);
        }

        // Muestrear píxeles con el paso calculado
        for (int x = 0; x < webcamTexture.width; x += stepX)
        {
            for (int y = 0; y < webcamTexture.height; y += stepY)
            {
                // Calcular índice en el buffer lineal
                int index = y * webcamTexture.width + x;

                // Comprobar límites
                if (index < pixelBuffer.Length)
                {
                    Color pixelColor = pixelBuffer[index];

                    // Verificar si el color está dentro del rango de tolerancia
                    bool isMatch = OptimizedIsColorMatch(pixelColor, targetColor, colorTolerance);

                    if (isMatch && matchingPixelCount < matchingPixelsBuffer.Length)
                    {
                        matchingPixelsBuffer[matchingPixelCount] = new Vector2(x, y);
                        matchingPixelCount++;

                        // Marcar el píxel en la textura de depuración
                        if (debugTexture != null)
                        {
                            // Dibujar un pequeño cuadrado alrededor del píxel coincidente
                            int size = 4;
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
        }

        // Si encontramos suficientes píxeles coincidentes, calcular el centro
        if (matchingPixelCount > minObjectSize)
        {
            // Encontrar el blob dominante y calcular su centro
            Vector2 centerPoint = CalculateDominantBlobCenter();

            // Aplicar filtrado para estabilidad
            if (enablePositionSmoothing && filteredPosition != Vector2.zero)
            {
                filteredPosition = Vector2.Lerp(filteredPosition, centerPoint, positionSmoothFactor);

                // Comprobar si el movimiento es significativo
                float movement = Vector2.Distance(filteredPosition, lastStablePosition);
                if (movement < minMovementThreshold)
                {
                    stabilityTimer += Time.deltaTime;
                    // Actualizar posición estable si ha estado estable por un tiempo
                    if (stabilityTimer > 0.2f)
                    {
                        lastStablePosition = filteredPosition;
                    }
                }
                else
                {
                    stabilityTimer = 0f;
                }

                centerPoint = filteredPosition;
            }
            else
            {
                filteredPosition = centerPoint;
                lastStablePosition = centerPoint;
            }

            lastDetectedPosition = centerPoint;
            objectDetected = true;

            // Mostrar marcador visual en la posición detectada
            if (showDetectionMarker && detectionMarkerPrefab != null && Time.time - lastMarkerTime > markerSpawnInterval)
            {
                if (canvasRectTransform != null)
                {
                    Vector2 canvasPosition = ConvertToCanvasPosition(centerPoint);
                    SpawnDetectionMarker(canvasPosition);
                    lastMarkerTime = Time.time;
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

            // Mover la nave al centro del objeto detectado
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
                pixelCountText.text = "Píxeles detectados: " + matchingPixelCount;
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

    // Algoritmo para encontrar el blob más grande y calcular su centro
    private Vector2 CalculateDominantBlobCenter()
    {
        // Si hay pocos píxeles, simplemente promediar
        if (matchingPixelCount < 20)
        {
            return CalculateAverageCenter();
        }

        // Implementar algoritmo de clustering simple para encontrar blobs
        List<List<int>> blobs = FindConnectedBlobs();

        // Encontrar el blob más grande
        int largestBlobIndex = 0;
        int maxSize = 0;

        for (int i = 0; i < blobs.Count; i++)
        {
            if (blobs[i].Count > maxSize)
            {
                maxSize = blobs[i].Count;
                largestBlobIndex = i;
            }
        }

        // Calcular centro del blob más grande
        Vector2 center = Vector2.zero;
        foreach (int pixelIndex in blobs[largestBlobIndex])
        {
            center += matchingPixelsBuffer[pixelIndex];
        }

        center /= blobs[largestBlobIndex].Count;
        return center;
    }

    // Método simple para calcular el centro promedio de todos los píxeles
    private Vector2 CalculateAverageCenter()
    {
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < matchingPixelCount; i++)
        {
            sum += matchingPixelsBuffer[i];
        }

        return sum / matchingPixelCount;
    }

    // Algoritmo para agrupar píxeles en blobs conectados
    private List<List<int>> FindConnectedBlobs()
    {
        List<List<int>> blobs = new List<List<int>>();

        // Resetear buffer de procesados
        for (int i = 0; i < matchingPixelCount; i++)
        {
            processedPixelBuffer[i] = false;
        }

        for (int i = 0; i < matchingPixelCount; i++)
        {
            if (processedPixelBuffer[i])
                continue;

            // Iniciar nuevo blob
            List<int> currentBlob = new List<int>();
            Queue<int> pixelsToProcess = new Queue<int>();

            pixelsToProcess.Enqueue(i);
            processedPixelBuffer[i] = true;

            while (pixelsToProcess.Count > 0)
            {
                int pixelIndex = pixelsToProcess.Dequeue();
                currentBlob.Add(pixelIndex);

                // Buscar píxeles cercanos (proximidad)
                for (int j = 0; j < matchingPixelCount; j++)
                {
                    if (!processedPixelBuffer[j] &&
                        Vector2.Distance(matchingPixelsBuffer[pixelIndex], matchingPixelsBuffer[j]) < blobProximityThreshold)
                    {
                        pixelsToProcess.Enqueue(j);
                        processedPixelBuffer[j] = true;
                    }
                }
            }

            blobs.Add(currentBlob);
        }

        return blobs;
    }

    // Versión optimizada de IsColorMatch
    private bool OptimizedIsColorMatch(Color pixel, Color target, float tolerance)
    {
        if (useHSVDetection)
        {
            // Comprobación rápida RGB para descarte temprano
            float rDiff = Mathf.Abs(pixel.r - target.r);
            float gDiff = Mathf.Abs(pixel.g - target.g);
            float bDiff = Mathf.Abs(pixel.b - target.b);

            // Si alguna diferencia es grande, rechazar inmediatamente
            float quickThreshold = tolerance * 0.7f;
            if (rDiff > quickThreshold || gDiff > quickThreshold || bDiff > quickThreshold)
                return false;

            // Convertir a HSV para comparación más precisa
            float pixelH, pixelS, pixelV;
            float targetH, targetS, targetV;

            Color.RGBToHSV(pixel, out pixelH, out pixelS, out pixelV);
            Color.RGBToHSV(target, out targetH, out targetS, out targetV);

            // Calcular diferencia en matiz (teniendo en cuenta naturaleza circular)
            float hDiff = Mathf.Min(Mathf.Abs(pixelH - targetH), 1 - Mathf.Abs(pixelH - targetH));
            float sDiff = Mathf.Abs(pixelS - targetS);
            float vDiff = Mathf.Abs(pixelV - targetV);

            // Aplicar ponderaciones para dar más importancia al matiz
            float hWeight = 2.0f;
            float sWeight = 1.0f;
            float vWeight = 0.5f;

            // Normalizar el peso total
            float totalWeight = hWeight + sWeight + vWeight;

            // Calcular diferencia ponderada
            float weightedDiff = (hDiff * hWeight + sDiff * sWeight + vDiff * vWeight) / totalWeight;

            // Umbrales específicos para cada componente
            float maxHDiff = Mathf.Min(0.25f, tolerance);
            float maxSDiff = Mathf.Min(0.5f, tolerance * 1.2f);
            float maxVDiff = Mathf.Min(0.5f, tolerance * 1.5f);

            if (hDiff > maxHDiff || sDiff > maxSDiff || vDiff > maxVDiff)
            {
                return false;
            }

            // Aplicar tolerancia escalada no lineal
            return weightedDiff < Mathf.Pow(tolerance, 1.5f) * 0.5f;
        }
        else
        {
            // Método RGB optimizado
            float rDiff = Mathf.Abs(pixel.r - target.r);
            float gDiff = Mathf.Abs(pixel.g - target.g);
            float bDiff = Mathf.Abs(pixel.b - target.b);

            // Calcular diferencia promedio
            float avgDiff = (rDiff + gDiff + bDiff) / 3f;

            // Umbral máximo por componente
            float maxComponentDiff = Mathf.Min(0.4f, tolerance * 1.2f);

            // Verificación de componentes y promedio
            return (rDiff < maxComponentDiff &&
                    gDiff < maxComponentDiff &&
                    bDiff < maxComponentDiff &&
                    avgDiff < Mathf.Pow(tolerance, 1.5f) * 0.5f);
        }
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

    private void MoveShipToCenterPoint(Vector2 centerPoint)
    {
        if (playerShip == null || webcamRawImage == null)
            return;

        // Convertir de coordenadas de textura a normalizadas (0-1)
        Vector2 normalizedPos = new Vector2(
            centerPoint.x / webcamTexture.width,
            centerPoint.y / webcamTexture.height
        );

        // Obtener las dimensiones del RawImage en el mundo
        RectTransform rt = webcamRawImage.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // Calcular la posición exacta en el espacio del mundo
        Vector3 worldPos = Vector3.zero;
        worldPos.x = Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x);
        worldPos.y = Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y);
        worldPos.z = playerShip.position.z;  // Mantener Z

        // Guardar posición para el círculo rojo
        redCircleWorldPosition = worldPos;

        // Actualizar directamente la posición de la nave
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
                Debug.LogWarning("El Canvas está usando una cámara diferente a mainCamera.");
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
        }
    }

    // Método para depuración - dibuja rayos de detección en la vista de escena
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !objectDetected || lastDetectedPosition == Vector2.zero)
            return;

        // Dibujar el centro detectado
        Gizmos.color = Color.red;
        Vector3 worldPos = new Vector3(redCircleWorldPosition.x, redCircleWorldPosition.y, redCircleWorldPosition.z);
        Gizmos.DrawSphere(worldPos, 0.2f);

        // Dibujar línea de la nave al objeto detectado
        if (playerShip != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerShip.position, worldPos);
        }
    }
}