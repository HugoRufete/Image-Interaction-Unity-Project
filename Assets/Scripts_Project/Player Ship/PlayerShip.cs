using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool enforceScreenBoundaries = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float boundaryPadding = 0.5f;

    // Parámetros de estabilidad adicionales
    [SerializeField] private bool applyMovementSmoothing = true;
    [SerializeField] private float positionSmoothingFactor = 0.3f;
    [SerializeField] private float minimumMovementThreshold = 0.1f; // Movimiento mínimo para actualizar

    // Configuración de suavizado
    [SerializeField] private bool useVelocityDamping = true;
    [SerializeField] private float velocityDampingFactor = 0.8f;

    // Nuevo parámetro para control directo
    [SerializeField] private bool useDirectPositioning = false;

    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private Vector3 velocity = Vector3.zero;
    private SpriteRenderer spriteRenderer;
    private Vector2 spriteHalfSize;
    private float stabilityTimer = 0f;
    private bool positionStable = false;

    void Start()
    {
        // Inicializar la posición objetivo
        targetPosition = transform.position;
        previousPosition = transform.position;

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
        if (useDirectPositioning)
            return;

        // Mover suavemente hacia la posición objetivo con filtrado adicional
        if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
        {
            Vector3 newPosition;

            if (applyMovementSmoothing)
            {
                if (useVelocityDamping)
                {
                    // Método de suavizado basado en velocidad (más natural)
                    Vector3 desiredVelocity = (targetPosition - transform.position) * smoothSpeed;
                    velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * 10f);

                    // Aplicar amortiguación para evitar oscilaciones
                    if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
                    {
                        velocity *= velocityDampingFactor;
                    }

                    newPosition = transform.position + velocity * Time.deltaTime;
                }
                else
                {
                    // Método de interpolación clásico
                    newPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
                }

                // Solo aplicar si el movimiento es significativo
                if (Vector3.Distance(newPosition, transform.position) > minimumMovementThreshold)
                {
                    // Comprueba estabilidad
                    if (Vector3.Distance(newPosition, previousPosition) < minimumMovementThreshold * 0.5f)
                    {
                        stabilityTimer += Time.deltaTime;
                        if (stabilityTimer > 0.1f)
                        {
                            positionStable = true;
                        }
                    }
                    else
                    {
                        stabilityTimer = 0f;
                        positionStable = false;
                    }

                    transform.position = newPosition;
                    previousPosition = transform.position;
                }
                else if (positionStable)
                {
                    // Si la posición es estable, forzar exactamente a la posición objetivo
                    // para evitar pequeñas vibraciones residuales
                    transform.position = targetPosition;
                    velocity = Vector3.zero;
                }
            }
            else
            {
                // Movimiento directo sin suavizado adicional
                transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Limpiar velocidad si hemos llegado al destino
            velocity = Vector3.zero;
        }
    }

    // Método público para establecer la posición objetivo con estabilización
    public void SetTargetPosition(Vector3 position)
    {
        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar límites de pantalla
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
        }

        // Si estamos en modo directo, actualizar la posición directamente
        if (useDirectPositioning)
        {
            transform.position = position;
            return;
        }

        // Filtrado adicional para evitar vibraciones
        if (applyMovementSmoothing)
        {
            // Si la nueva posición está muy cerca de la actual, ignorar el cambio
            // para evitar pequeñas vibraciones
            if (Vector3.Distance(position, targetPosition) < minimumMovementThreshold && positionStable)
            {
                return;
            }

            // Si la posición salta demasiado, resetear el sistema de suavizado
            if (Vector3.Distance(position, targetPosition) > 2.0f)
            {
                velocity = Vector3.zero;
                stabilityTimer = 0f;
                positionStable = false;
            }
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
        previousPosition = position;
        velocity = Vector3.zero;
        stabilityTimer = 0f;
        positionStable = true;
    }

    // Método para limitar la posición dentro de la pantalla
    private Vector3 ClampPositionToScreen(Vector3 position)
    {
        // Cálculo de los límites de la pantalla en unidades de mundo
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        float minX = mainCamera.transform.position.x - (camWidth / 2) + spriteHalfSize.x + boundaryPadding * 0.5f;
        float maxX = mainCamera.transform.position.x + (camWidth / 2) - spriteHalfSize.x - boundaryPadding * 0.5f;
        float minY = mainCamera.transform.position.y - (camHeight / 2) + spriteHalfSize.y + boundaryPadding * 0.5f;
        float maxY = mainCamera.transform.position.y + (camHeight / 2) - spriteHalfSize.y - boundaryPadding * 0.5f;

        // Restringir la posición dentro de los límites
        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }

    // Método para reiniciar el sistema de movimiento (útil después de cambios bruscos)
    public void ResetMovement()
    {
        velocity = Vector3.zero;
        previousPosition = transform.position;
        targetPosition = transform.position;
        stabilityTimer = 0f;
        positionStable = false;
    }

    // Método para obtener el estado de estabilidad actual
    public bool IsPositionStable()
    {
        return positionStable;
    }
}