// Asteroid.cs - Script optimizado
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float destroyDistance;
    private Vector3 startPosition;

    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool randomizeRotation = true;

    [Header("Performance Optimization")]
    [SerializeField] private bool useFixedUpdate = true; // Mejora rendimiento en sistemas con muchos asteroides

    private bool isInitialized = false;

    private void OnEnable()
    {
        // Almacenar posición inicial cuando se activa desde el pool
        startPosition = transform.position;

        // Randomizar rotación inicial cuando se activa
        if (randomizeRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            rotationSpeed = 30f * Random.Range(0.5f, 1.5f) * (Random.value > 0.5f ? 1 : -1); // Random speed and direction
        }
    }

    public void Initialize(Vector3 direction, float speed, float destroyDistance)
    {
        this.direction = direction;
        this.speed = speed;
        this.destroyDistance = destroyDistance;
        isInitialized = true;
    }

    private void Update()
    {
        // Si estamos usando FixedUpdate para el movimiento, solo aplicamos
        // la rotación aquí para mantener la suavidad visual
        if (useFixedUpdate)
        {
            // Solo aplicar rotación
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Si no usamos FixedUpdate, hacemos todo el movimiento aquí
            UpdateAsteroidMovement(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate && isInitialized)
        {
            // Actualizar movimiento en FixedUpdate para mejor rendimiento físico
            UpdateAsteroidMovement(Time.fixedDeltaTime);
        }
    }

    private void UpdateAsteroidMovement(float deltaTime)
    {
        if (!isInitialized)
            return;

        // Mover el asteroide
        transform.position += direction * speed * deltaTime;

        // Aplicar rotación (solo si no estamos usando FixedUpdate)
        if (!useFixedUpdate)
        {
            transform.Rotate(0, 0, rotationSpeed * deltaTime);
        }

        // Comprobar si el asteroide se ha alejado demasiado
        if (Vector3.Distance(startPosition, transform.position) > destroyDistance)
        {
            // Devolver al pool en lugar de destruir
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Comprobar si el asteroide golpeó al jugador
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            // Devolver al pool en lugar de destruir
            ReturnToPool();
        }
    }

    // Método para devolver el asteroide al pool
    private void ReturnToPool()
    {
        // Desactivar el objeto en lugar de destruirlo
        gameObject.SetActive(false);
    }

    // OnDisable se llama cuando el objeto se desactiva
    private void OnDisable()
    {
        // Resetear variables cuando se desactiva el asteroide
        isInitialized = false;
    }
}