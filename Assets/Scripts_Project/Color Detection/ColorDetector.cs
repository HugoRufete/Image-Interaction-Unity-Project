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
    [SerializeField] private int scanFrequency = 30; // Cada cu�ntos frames actualizar
    [SerializeField] private int samplePoints = 100; // N�mero de puntos de muestra en la imagen
    [SerializeField] private float minObjectSize = 10f; // Tama�o m�nimo para considerar un objeto

    [Header("Detection Visualization")]
    [SerializeField] private GameObject detectionMarkerPrefab;
    [SerializeField] private bool showDetectionMarker = true;
    [SerializeField] private float markerSpawnInterval = 0.2f;
    [SerializeField] private bool moveShipToRedCircle = true; // Usar el centro del c�rculo rojo para la nave

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
    private RenderTexture debugRenderTexture; // Para capturar la textura de depuraci�n

    [Header("Direct Positioning")]
    [SerializeField] private bool createVisibleRedCircle = true; // Crea un objeto visible en la posici�n del c�rculo rojo
    [SerializeField] private GameObject redCirclePrefab; // Prefab para el c�rculo rojo visible
    private Vector3 redCircleWorldPosition;

    void Start()
    {
        // Inicializar el tiempo del �ltimo marcador
        lastMarkerTime = 0f;

        // Suscribirse al evento de selecci�n de color
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
                    Debug.LogError("No se pudo encontrar RawImage en webcamDisplay o sus hijos. Por favor, as�gnalo manualmente.");
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
            // Esperar a que la c�mara se inicialice
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
                Debug.Log("Canvas encontrado autom�ticamente.");
            }
            else
            {
                Debug.LogError("No se pudo encontrar un Canvas en la escena. Por favor, asigna manualmente la referencia al Canvas.");
            }
        }

        // Verificar si hay referencia a la c�mara
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No se pudo encontrar la c�mara principal. Por favor, asigna una referencia manualmente.");
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
               webcamDisplay.webcamTexture.width <= 16) // Verificar que la textura tenga un tama�o v�lido
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

        // Actualizar la detecci�n cada cierto n�mero de frames para mejor rendimiento
        if (frameCounter >= scanFrequency)
        {
            DetectColorObject();
            frameCounter = 0;

            // Actualizar el texto de estado
            UpdateDetectionStatus();
        }

        // No necesitamos mover la nave aqu� si estamos usando moveShipToRedCircle
        // ya que se mover� directamente en DetectColorObject
    }

    private void UpdateDetectionStatus()
    {
        if (detectionStatusText != null)
        {
            if (objectDetected)
            {
                detectionStatusText.text = "�Objeto detectado!";
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

        // Lista para almacenar posiciones de p�xeles que coinciden con el color
        List<Vector2> matchingPixels = new List<Vector2>();

        // Calcular el paso para distribuir los puntos de muestra uniformemente
        int stepX = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.width / Mathf.Sqrt(samplePoints)));
        int stepY = Mathf.Max(1, Mathf.FloorToInt(webcamTexture.height / Mathf.Sqrt(samplePoints)));

        // Crear una textura de depuraci�n si es necesario
        Texture2D debugTexture = null;
        if (showDebugInfo && createDebugTexture && debugVisualizer != null)
        {
            debugTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);

            // Copiar la textura de la webcam a la textura de depuraci�n
            Color[] pixels = webcamTexture.GetPixels();
            debugTexture.SetPixels(pixels);
        }

        // Recorrer puntos distribuidos en la imagen (no todos los p�xeles por rendimiento)
        for (int x = 0; x < webcamTexture.width; x += stepX)
        {
            for (int y = 0; y < webcamTexture.height; y += stepY)
            {
                // Obtener el color del p�xel actual
                Color pixelColor = webcamTexture.GetPixel(x, y);

                // Verificar si el color est� dentro del rango de tolerancia
                bool isMatch = IsColorMatch(pixelColor, targetColor, colorTolerance);

                if (isMatch)
                {
                    matchingPixels.Add(new Vector2(x, y));

                    // Marcar el p�xel en la textura de depuraci�n
                    if (debugTexture != null)
                    {
                        // Dibujar un peque�o cuadrado alrededor del p�xel coincidente
                        int size = 4; // Tama�o del marcador
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

        // Si encontramos suficientes p�xeles coincidentes, calcular el centro del objeto
        if (matchingPixels.Count > minObjectSize)
        {
            Vector2 centerPoint = CalculateCenterPoint(matchingPixels);
            lastDetectedPosition = centerPoint;
            objectDetected = true;

            // Mostrar marcador visual en la posici�n detectada
            if (showDetectionMarker && detectionMarkerPrefab != null && Time.time - lastMarkerTime > markerSpawnInterval)
            {
                // Verificar que canvasRectTransform no sea nulo
                if (canvasRectTransform != null)
                {
                    // Convertir la posici�n detectada al espacio del canvas
                    Vector2 canvasPosition = ConvertToCanvasPosition(centerPoint);
                    SpawnDetectionMarker(canvasPosition);
                    lastMarkerTime = Time.time;
                }
                else
                {
                    Debug.LogError("canvasRectTransform es nulo. No se puede convertir la posici�n para el marcador.");
                }
            }

            // Marcar el centro en la textura de depuraci�n
            if (debugTexture != null)
            {
                // Dibujar un c�rculo en el centro del objeto detectado
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

            // NUEVA IMPLEMENTACI�N: Mover siempre la nave al centro del objeto detectado
            MoveShipToCenterPoint(centerPoint);
        }
        else
        {
            objectDetected = false;
        }

        // Actualizar informaci�n de depuraci�n
        if (showDebugInfo)
        {
            if (pixelCountText != null && pixelThresholdText != null)
            {
                pixelCountText.text = "P�xeles detectados: " + matchingPixels.Count;
                pixelThresholdText.text = "Umbral m�nimo: " + minObjectSize;
            }

            // Actualizar la textura de depuraci�n
            if (debugTexture != null)
            {
                debugTexture.Apply();
                debugVisualizer.UpdateDebugImage(debugTexture);
            }

            // Registrar el tiempo de detecci�n
            float detectionTime = Time.realtimeSinceStartup - startTime;
            if (debugVisualizer != null)
            {
                debugVisualizer.UpdateDetectionTime(detectionTime);
            }
        }
    }

    private void CreateVisibleRedCircle(Vector2 centerPoint)
    {
        // Eliminar cualquier c�rculo anterior
        GameObject existingCircle = GameObject.Find("VisibleRedCircle");
        if (existingCircle != null)
        {
            Destroy(existingCircle);
        }

        // Crear un nuevo c�rculo rojo visible usando el prefab
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

            // Calcular la posici�n exacta en el mundo
            Vector3 worldPos = new Vector3(
                Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x),
                Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y),
                -1 // Delante del canvas
            );

            // Guardar la posici�n mundial para usar con la nave
            redCircleWorldPosition = worldPos;

            // Instanciar el c�rculo rojo visible
            GameObject circle = Instantiate(redCirclePrefab, worldPos, Quaternion.identity);
            circle.name = "VisibleRedCircle";
            circle.SetActive(true);

            // Hacer que el c�rculo sea peque�o
            circle.transform.localScale = Vector3.one * 0.1f;

            Debug.Log($"C�rculo rojo visible creado en posici�n mundial: {worldPos}");
        }
    }

    // M�todo nuevo para mover la nave a la posici�n del c�rculo rojo
    private void MoveShipToDebugPosition(Vector2 centerPoint)
    {
        // Obtener la posici�n normalizada (0-1) en la textura de la webcam
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

        // Calcular la posici�n exacta dentro del RawImage basado en la posici�n normalizada
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
            // Movimiento manual con interpolaci�n
            playerShip.position = Vector3.Lerp(
                playerShip.position,
                exactPosition,
                Time.deltaTime * 10f
            );
        }

        // Debug visual - dibujar una l�nea para ver d�nde se est� moviendo la nave
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
            // Convertir RGB a HSV para mejor detecci�n de color
            float pixelH, pixelS, pixelV;
            float targetH, targetS, targetV;

            Color.RGBToHSV(pixel, out pixelH, out pixelS, out pixelV);
            Color.RGBToHSV(target, out targetH, out targetS, out targetV);

            // Calcular la diferencia en matiz (H) teniendo en cuenta que es circular (0-1)
            float hDiff = Mathf.Min(Mathf.Abs(pixelH - targetH), 1 - Mathf.Abs(pixelH - targetH));

            // Calcular las diferencias en saturaci�n (S) y valor (V)
            float sDiff = Mathf.Abs(pixelS - targetS);
            float vDiff = Mathf.Abs(pixelV - targetV);

            // Ajustar las ponderaciones para dar m�s importancia al matiz y menor escala a la tolerancia
            float hWeight = 2.0f;
            float sWeight = 1.0f;
            float vWeight = 0.5f;

            // Normalizar el peso total
            float totalWeight = hWeight + sWeight + vWeight;

            // Aplicar una escala no lineal a la tolerancia para que tenga un comportamiento m�s intuitivo
            // Esto hace que la tolerancia sea m�s restrictiva incluso con valores altos
            float scaledTolerance = Mathf.Pow(tolerance, 1.5f) * 0.5f;

            // Calcular la diferencia ponderada
            float weightedDiff = (hDiff * hWeight + sDiff * sWeight + vDiff * vWeight) / totalWeight;

            // Usar un umbral m�s estricto para el matiz cuando la saturaci�n es alta
            // Esto evita que colores diferentes pero con la misma luminosidad se consideren iguales
            if (targetS > 0.3f && pixelS > 0.3f)
            {
                if (hDiff > tolerance * 0.7f)
                {
                    return false;
                }
            }

            // Aplicar umbrales m�ximos absolutos para cada componente
            // Esto impide que la tolerancia alta permita cambios demasiado dr�sticos
            float maxHDiff = Mathf.Min(0.25f, tolerance);
            float maxSDiff = Mathf.Min(0.5f, tolerance * 1.2f);
            float maxVDiff = Mathf.Min(0.5f, tolerance * 1.5f);

            if (hDiff > maxHDiff || sDiff > maxSDiff || vDiff > maxVDiff)
            {
                return false;
            }

            // Verificar si la diferencia ponderada est� dentro del rango de tolerancia
            return weightedDiff < scaledTolerance;
        }
        else
        {
            // M�todo RGB mejorado
            float rDiff = Mathf.Abs(pixel.r - target.r);
            float gDiff = Mathf.Abs(pixel.g - target.g);
            float bDiff = Mathf.Abs(pixel.b - target.b);

            // Calcular la diferencia promedio
            float avgDiff = (rDiff + gDiff + bDiff) / 3f;

            // Aplicar una escala no lineal a la tolerancia
            float scaledTolerance = Mathf.Pow(tolerance, 1.5f) * 0.5f;

            // Establecer l�mites m�ximos para cada componente
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
                Debug.LogError("No se encontr� RawImage en webcamDisplay");
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
            Debug.LogError("No se encontr� RectTransform en el RawImage");
            return Vector2.zero;
        }

        // Convertir de coordenadas de textura a coordenadas normalizadas (0-1)
        float normalizedX = texturePosition.x / webcamTexture.width;
        float normalizedY = texturePosition.y / webcamTexture.height;

        // En las texturas de webcam, el origen Y est� invertido respecto a la UI
        normalizedY = 1 - normalizedY;

        // Obtener la posici�n del RawImage en el canvas
        float imageWidth = imageRectTransform.rect.width;
        float imageHeight = imageRectTransform.rect.height;

        // Calcular la posici�n en el canvas
        Vector3 imagePosition = imageRectTransform.position;
        float imageLeft = imagePosition.x - (imageWidth / 2);
        float imageBottom = imagePosition.y - (imageHeight / 2);

        float canvasX = imageLeft + (normalizedX * imageWidth);
        float canvasY = imageBottom + (normalizedY * imageHeight);

        return new Vector2(canvasX, canvasY);
    }

    // M�todo para instanciar el marcador de detecci�n
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
                // Configurar la posici�n exacta
                marker.SetPosition(position);

                // Configurar el color (puede ser el color detectado o un color distintivo)
                marker.SetColor(new Color(targetColor.r, targetColor.g, targetColor.b, 0.7f));

                Debug.Log($"Marcador de detecci�n instanciado en posici�n: {position}");
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

    // Reemplaza el m�todo MoveShipToCenterPoint() en ColorDetector.cs con este:

    // Modificaci�n para el m�todo MoveShipToCenterPoint() en ColorDetector.cs

    private void MoveShipToCenterPoint(Vector2 centerPoint)
    {
        if (playerShip == null)
        {
            Debug.LogError("playerShip no est� asignado en ColorDetector");
            return;
        }

        if (webcamRawImage == null)
        {
            Debug.LogError("webcamRawImage no est� asignado en ColorDetector");
            return;
        }

        // IMPORTANTE: Problema identificado - el eje Y est� invertido
        // 1. Convertir de coordenadas de textura a normalizadas (0-1)
        // CORRECCI�N: No invertimos el eje Y aqu�, ya que eso causa la inversi�n
        Vector2 normalizedPos = new Vector2(
            centerPoint.x / webcamTexture.width,
            centerPoint.y / webcamTexture.height  // CAMBIO AQU�: Quitamos el 1.0f - ...
        );

        // Alternativamente, si lo anterior no funciona, invertir la l�gica aqu�:
        // Intentar segunda soluci�n si la primera no funciona
        // Vector2 normalizedPos = new Vector2(
        //     centerPoint.x / webcamTexture.width,
        //     1.0f - (centerPoint.y / webcamTexture.height)  // Mantener la inversi�n
        // );
        // // Pero luego invertir la interpolaci�n en Y:
        // worldPos.y = Mathf.Lerp(corners[2].y, corners[0].y, normalizedPos.y);  // Invertir orden de corners

        // 2. Obtener las dimensiones y posici�n del RawImage
        RectTransform rt = webcamRawImage.rectTransform;

        // Obtener posici�n y tama�o del RawImage en el espacio del mundo
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // corners[0] = Bottom-Left, corners[1] = Top-Left, 
        // corners[2] = Top-Right, corners[3] = Bottom-Right

        // 3. Calcular la posici�n exacta dentro del espacio del RawImage
        Vector3 worldPos = Vector3.zero;

        // Usar transformaci�n directa basada en las esquinas del RawImage
        worldPos.x = Mathf.Lerp(corners[0].x, corners[2].x, normalizedPos.x);

        // CORRECCI�N: Invertir el orden de interpolaci�n para el eje Y
        // Ya que no invertimos en la normalizaci�n, debemos interpolar
        // desde la esquina inferior a la superior (no al rev�s)
        worldPos.y = Mathf.Lerp(corners[0].y, corners[2].y, normalizedPos.y);

        // Si la soluci�n anterior no funciona, prueba esta alternativa:
        // worldPos.y = Mathf.Lerp(corners[2].y, corners[0].y, normalizedPos.y);

        worldPos.z = playerShip.position.z; // Mantener Z

        // Debug avanzado - mostrar todos los pasos de conversi�n
        Debug.Log($"Detecci�n: Pixel={centerPoint}, Normalizado={normalizedPos}, " +
                  $"Esquinas=[Bot-Left:{corners[0]}, Top-Left:{corners[1]}, Top-Right:{corners[2]}, Bot-Right:{corners[3]}], " +
                  $"Final={worldPos}");

        // Guardar posici�n para el c�rculo rojo
        redCircleWorldPosition = worldPos;

        // IMPORTANTE: Actualizar directamente la posici�n sin animaci�n
        playerShip.position = worldPos;

        // Crear visualizaci�n del c�rculo rojo para depuraci�n
        if (createVisibleRedCircle && redCirclePrefab != null)
        {
            // Eliminar c�rculo anterior
            GameObject existingCircle = GameObject.Find("VisibleRedCircle");
            if (existingCircle != null)
            {
                Destroy(existingCircle);
            }

            // Crear nuevo c�rculo en la posici�n calculada
            GameObject circle = Instantiate(redCirclePrefab, worldPos, Quaternion.identity);
            circle.name = "VisibleRedCircle";
            circle.SetActive(true);
            circle.transform.localScale = Vector3.one * 0.1f;
        }
    }

    private void CheckCameraSetup()
    {
        // Verificar la c�mara principal
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            Debug.Log("Se ha asignado autom�ticamente la c�mara principal: " +
                      (mainCamera != null ? mainCamera.name : "No encontrada"));
        }

        // Verificar si estamos usando una c�mara ortogr�fica como se recomienda para 2D
        if (mainCamera != null && !mainCamera.orthographic)
        {
            Debug.LogWarning("La c�mara principal no es ortogr�fica. Se recomienda usar una c�mara ortogr�fica para juegos 2D.");
        }

        // Verificar la configuraci�n del Canvas
        Canvas canvas = null;
        if (canvasRectTransform != null)
        {
            canvas = canvasRectTransform.GetComponent<Canvas>();
        }

        if (canvas != null)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("El Canvas no est� configurado como World Space. Modo actual: " + canvas.renderMode);
            }

            if (canvas.worldCamera != mainCamera)
            {
                Debug.LogWarning("El Canvas est� usando una c�mara diferente a mainCamera. " +
                                "Canvas.worldCamera: " + (canvas.worldCamera != null ? canvas.worldCamera.name : "null") +
                                ", mainCamera: " + (mainCamera != null ? mainCamera.name : "null"));
            }
        }

        // Imprimir informaci�n sobre capas y culling mask
        if (mainCamera != null && playerShip != null)
        {
            int shipLayer = playerShip.gameObject.layer;
            bool isLayerVisible = ((1 << shipLayer) & mainCamera.cullingMask) != 0;

            Debug.Log($"Nave en capa: {LayerMask.LayerToName(shipLayer)} (�ndice {shipLayer}). " +
                     $"Visible para mainCamera: {isLayerVisible}");
        }
    }
}