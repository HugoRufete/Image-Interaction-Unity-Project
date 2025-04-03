using UnityEngine;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool enforceScreenBoundaries = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float boundaryPadding = 0.5f;

    // Nuevo par�metro para control directo
    [SerializeField] private bool useDirectPositioning = false;

    private Vector3 targetPosition;
    private SpriteRenderer spriteRenderer;
    private Vector2 spriteHalfSize;

    void Start()
    {
        // Inicializar la posici�n objetivo
        targetPosition = transform.position;

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
        // La posici�n se controlar� completamente desde ColorDetector
        if (useDirectPositioning)
            return;

        // Mover suavemente hacia la posici�n objetivo
        if (targetPosition != transform.position)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }

    // M�todo p�blico para establecer la posici�n objetivo
    public void SetTargetPosition(Vector3 position)
    {
        // Si estamos en modo directo, actualizar la posici�n directamente
        if (useDirectPositioning)
        {
            // Asegurarnos que se mantiene la profundidad Z
            position.z = transform.position.z;

            // Aplicar l�mites de pantalla si es necesario
            if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
            {
                position = ClampPositionToScreen(position);
            }

            // Establecer la posici�n directamente
            transform.position = position;
            return;
        }

        // Asegurarnos que se mantiene la profundidad Z
        position.z = transform.position.z;

        // Aplicar l�mites de pantalla si es necesario
        if (enforceScreenBoundaries && mainCamera != null && spriteRenderer != null)
        {
            position = ClampPositionToScreen(position);
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
    }

    // M�todo para limitar la posici�n dentro de la pantalla
    private Vector3 ClampPositionToScreen(Vector3 position)
    {
        // C�lculo de los l�mites de la pantalla en unidades de mundo
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        // L�mites considerando la posici�n de la c�mara y el tama�o del sprite
        float minX = mainCamera.transform.position.x - (camWidth / 2) + spriteHalfSize.x + boundaryPadding;
        float maxX = mainCamera.transform.position.x + (camWidth / 2) - spriteHalfSize.x - boundaryPadding;
        float minY = mainCamera.transform.position.y - (camHeight / 2) + spriteHalfSize.y + boundaryPadding;
        float maxY = mainCamera.transform.position.y + (camHeight / 2) - spriteHalfSize.y - boundaryPadding;

        // Restringir la posici�n dentro de los l�mites
        float x = Mathf.Clamp(position.x, minX, maxX);
        float y = Mathf.Clamp(position.y, minY, maxY);

        return new Vector3(x, y, position.z);
    }
}