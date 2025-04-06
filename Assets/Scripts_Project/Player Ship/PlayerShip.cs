using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool enforceScreenBoundaries = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float boundaryPadding = 0.5f;

    // Par�metros de estabilidad adicionales
    [SerializeField] private bool applyMovementSmoothing = true;
    [SerializeField] private float positionSmoothingFactor = 0.3f;
    [SerializeField] private float minimumMovementThreshold = 0.1f; // Movimiento m�nimo para actualizar

    // Configuraci�n de suavizado
    [SerializeField] private bool useVelocityDamping = true;
    [SerializeField] private float velocityDampingFactor = 0.8f;

    // Nuevo par�metro para control directo
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
        // Inicializar la posici�n objetivo
        targetPosition = transform.position;
        previousPosition = transform.position;

        // Obtener la c�mara si no est� asignada
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Obtener el SpriteRenderer para los l�mites de pantalla
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // Guardar el tama�o del sprite para los l�mites
            spriteHalfSize = new Vector2(
                spriteRenderer.bounds.extents.x,
                spriteRenderer.bounds.extents.y
            );
        }
        else
        {
            Debug.LogWarning("No se encontr� SpriteRenderer para PlayerShip. No se aplicar�n l�mites de pantalla.");
            enforceScreenBoundaries = false;
        }
    }

    void Update()
    {
        // Si est� en modo directo, no hacemos nada aqu�
        if (useDirectPositioning)
            return;

        // Mover suavemente hacia la posici�n objetivo con filtrado adicional
        if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
        {
            Vector3 newPosition;

            if (applyMovementSmoothing)
            {
                if (useVelocityDamping)
                {
                    // M�todo de suavizado basado en velocidad (m�s natural)
                    Vector3 desiredVelocity = (targetPosition - transform.position) * smoothSpeed;
                    velocity = Vector3.Lerp(velocity, desiredVelocity, Time.deltaTime * 10f);

                    // Aplicar amortiguaci�n para evitar oscilaciones
                    if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
                    {
                        velocity *= velocityDampingFactor;
                    }

                    newPosition = transform.position + velocity * Time.deltaTime;
                }
                else
                {
                    // M�todo de interpolaci�n cl�sico
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
                    // Si la posici�n es estable, forzar exactamente a la posici�n objetivo
                    // para evitar peque�as vibraciones residuales
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

    // M�todo p�blico para establecer la posici�n objetivo con estabilizaci�n
    public void SetTargetPosition(Vector3 position)
    {
        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar l�mites de pantalla
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
        }

        // Si estamos en modo directo, actualizar la posici�n directamente
        if (useDirectPositioning)
        {
            transform.position = position;
            return;
        }

        // Filtrado adicional para evitar vibraciones
        if (applyMovementSmoothing)
        {
            // Si la nueva posici�n est� muy cerca de la actual, ignorar el cambio
            // para evitar peque�as vibraciones
            if (Vector3.Distance(position, targetPosition) < minimumMovementThreshold && positionStable)
            {
                return;
            }

            // Si la posici�n salta demasiado, resetear el sistema de suavizado
            if (Vector3.Distance(position, targetPosition) > 2.0f)
            {
                velocity = Vector3.zero;
                stabilityTimer = 0f;
                positionStable = false;
            }
        }

        // Establecer la nueva posici�n objetivo para interpolaci�n
        targetPosition = position;
    }

    // M�todo p�blico para posicionar la nave directamente sin interpolaci�n
    public void SetDirectPosition(Vector3 position)
    {
        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar l�mites de pantalla si es necesario
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
        }

        // Establecer la posici�n directamente sin interpolaci�n
        transform.position = position;
        targetPosition = position; // Actualizar target para consistencia
        previousPosition = position;
        velocity = Vector3.zero;
        stabilityTimer = 0f;
        positionStable = true;
    }

    // M�todo para limitar la posici�n dentro de la pantalla
    private Vector3 ClampPositionToScreen(Vector3 position)
    {
        // C�lculo de los l�mites de la pantalla en unidades de mundo
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        float minX = mainCamera.transform.position.x - (camWidth / 2) + spriteHalfSize.x + boundaryPadding * 0.5f;
        float maxX = mainCamera.transform.position.x + (camWidth / 2) - spriteHalfSize.x - boundaryPadding * 0.5f;
        float minY = mainCamera.transform.position.y - (camHeight / 2) + spriteHalfSize.y + boundaryPadding * 0.5f;
        float maxY = mainCamera.transform.position.y + (camHeight / 2) - spriteHalfSize.y - boundaryPadding * 0.5f;

        // Restringir la posici�n dentro de los l�mites
        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }

    // M�todo para reiniciar el sistema de movimiento (�til despu�s de cambios bruscos)
    public void ResetMovement()
    {
        velocity = Vector3.zero;
        previousPosition = transform.position;
        targetPosition = transform.position;
        stabilityTimer = 0f;
        positionStable = false;
    }

    // M�todo para obtener el estado de estabilidad actual
    public bool IsPositionStable()
    {
        return positionStable;
    }
}