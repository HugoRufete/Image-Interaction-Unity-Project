using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool enforceScreenBoundaries = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float boundaryPadding = 0.5f;

    // Nuevo parámetro para control directo
    [SerializeField] private bool useDirectPositioning = false;

    private Vector3 targetPosition;
    private SpriteRenderer spriteRenderer;
    private Vector2 spriteHalfSize;

    void Start()
    {
        // Inicializar la posición objetivo
        targetPosition = transform.position;

        // Obtener la cámara si no está asignada
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Obtener el SpriteRenderer para los límites de pantalla
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // Guardar el tamaño del sprite para los límites
            spriteHalfSize = new Vector2(
                spriteRenderer.bounds.extents.x,
                spriteRenderer.bounds.extents.y
            );
        }
        else
        {
            Debug.LogWarning("No se encontró SpriteRenderer para PlayerShip. No se aplicarán límites de pantalla.");
            enforceScreenBoundaries = false;
        }
    }

    void Update()
    {
        // Si está en modo directo, no hacemos nada aquí
        // La posición se controlará completamente desde ColorDetector
        if (useDirectPositioning)
            return;

        // Mover suavemente hacia la posición objetivo
        if (targetPosition != transform.position)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }

    // Método público para establecer la posición objetivo
    public void SetTargetPosition(Vector3 position)
    {
        // Si estamos en modo directo, actualizar la posición directamente
        if (useDirectPositioning)
        {
            // Asegurarnos que se mantiene la profundidad Z
            position.z = transform.position.z;

            // Aplicar límites de pantalla si es necesario
            if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
            {
                position = ClampPositionToScreen(position);
            }

            // Establecer la posición directamente
            transform.position = position;
            return;
        }

        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar límites de pantalla si es necesario
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
        }

        // Establecer la nueva posición objetivo para interpolación
        targetPosition = position;
    }

    // Método público para posicionar la nave directamente sin interpolación
    public void SetDirectPosition(Vector3 position)
    {
        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar límites de pantalla si es necesario
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
        }

        // Establecer la posición directamente sin interpolación
        transform.position = position;
        targetPosition = position; // Actualizar target para consistencia
    }

    // Método para limitar la posición dentro de la pantalla
    private Vector3 ClampPositionToScreen(Vector3 position)
    {
        // Cálculo de los límites de la pantalla en unidades de mundo
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        // Límites considerando la posición de la cámara y el tamaño del sprite
        float minX = mainCamera.transform.position.x - (camWidth / 2) + spriteHalfSize.x + boundaryPadding;
        float maxX = mainCamera.transform.position.x + (camWidth / 2) - spriteHalfSize.x - boundaryPadding;
        float minY = mainCamera.transform.position.y - (camHeight / 2) + spriteHalfSize.y + boundaryPadding;
        float maxY = mainCamera.transform.position.y + (camHeight / 2) - spriteHalfSize.y - boundaryPadding;

        // Restringir la posición dentro de los límites
        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }
}