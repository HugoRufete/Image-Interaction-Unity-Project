// AsteroidSpawner.cs - Script optimizado
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDelay = 1f;
    [SerializeField] private float maxSpawnDelay = 3f;

    [Header("Asteroid Settings")]
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 1.5f;
    [SerializeField] private float destroyDistance = 20f;

    [Header("Difficulty Settings")]
    [SerializeField] private bool increaseDifficultyOverTime = true;
    [SerializeField] private float difficultyIncreaseRate = 0.1f;
    [SerializeField] private float maxDifficultyMultiplier = 2.5f;

    [Header("Optimization Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private bool useObjectPooling = true;

    private float difficultyMultiplier = 1f;
    private float timeSinceStart = 0f;
    private Coroutine spawnCoroutine;
    private GameManager gameManager;
    private bool spawningActive = false;

    // Pool de asteroides para optimización
    private List<GameObject> asteroidPool;

    private void Awake()
    {
        // No iniciar ninguna coroutine en Awake
        spawnCoroutine = null;
        spawningActive = false;
    }

    private void Start()
    {
        // Find GameManager
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found! AsteroidSpawner requires GameManager.");
            this.enabled = false;
            return;
        }

        // Validate settings
        if (asteroidPrefab == null)
        {
            Debug.LogError("Asteroid prefab not assigned to AsteroidSpawner!");
            this.enabled = false;
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned to AsteroidSpawner. Using this transform as default.");
            spawnPoints.Add(transform);
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not assigned to AsteroidSpawner. Attempting to find player object.");
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("Failed to find player. Please assign player transform manually.");
                this.enabled = false;
                return;
            }
        }

        // Inicializar el pool de asteroides
        if (useObjectPooling)
        {
            InitializeAsteroidPool();
        }

        Debug.Log("AsteroidSpawner initialized - NOT spawning until game starts");
    }

    // Inicializar el pool de asteroides
    private void InitializeAsteroidPool()
    {
        asteroidPool = new List<GameObject>(initialPoolSize);

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject asteroid = Instantiate(asteroidPrefab);
            asteroid.SetActive(false);
            asteroidPool.Add(asteroid);
        }

        Debug.Log($"Asteroid pool initialized with {initialPoolSize} asteroids");
    }

    // Obtener un asteroide del pool
    private GameObject GetAsteroidFromPool()
    {
        // Buscar asteroide inactivo en el pool
        for (int i = 0; i < asteroidPool.Count; i++)
        {
            if (asteroidPool[i] != null && !asteroidPool[i].activeInHierarchy)
            {
                return asteroidPool[i];
            }
        }

        // Si no hay asteroides inactivos disponibles y no hemos alcanzado el máximo,
        // crear uno nuevo y añadirlo al pool
        if (asteroidPool.Count < maxPoolSize)
        {
            GameObject newAsteroid = Instantiate(asteroidPrefab);
            newAsteroid.SetActive(false);
            asteroidPool.Add(newAsteroid);
            return newAsteroid;
        }

        // Si el pool está lleno, reutilizar el primer asteroide 
        // (probablemente el más antiguo)
        if (asteroidPool.Count > 0)
        {
            asteroidPool[0].SetActive(false);
            return asteroidPool[0];
        }

        // Fallback: crear un asteroide nuevo (no debería ocurrir normalmente)
        Debug.LogWarning("Asteroid pool is empty and at max capacity. Creating a non-pooled asteroid.");
        return Instantiate(asteroidPrefab);
    }

    private void Update()
    {
        // Verificar estado del juego
        if (gameManager == null)
            return;

        // Actualizar dificultad solo si el juego está activo
        if (gameManager.gameStarted && !gameManager.gamePaused && !gameManager.gameOver)
        {
            if (increaseDifficultyOverTime)
            {
                timeSinceStart += Time.deltaTime;
                difficultyMultiplier = Mathf.Min(1 + (timeSinceStart * difficultyIncreaseRate / 60f), maxDifficultyMultiplier);
            }

            // Iniciar spawning si no está activo
            if (!spawningActive)
            {
                Debug.Log("Game is now active - STARTING asteroid spawning");
                RestartSpawning();
            }
        }
        else
        {
            // Detener spawning si el juego no está activo
            if (spawningActive)
            {
                Debug.Log("Game is not active - STOPPING asteroid spawning");
                StopSpawning();
            }
        }
    }

    private IEnumerator SpawnAsteroidsRoutine()
    {
        spawningActive = true;
        Debug.Log("AsteroidSpawner routine STARTED");

        while (true)
        {
            // DOBLE VERIFICACIÓN crítica - si en algún momento el juego no está activo, detenemos la coroutine
            if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
            {
                Debug.Log("Game state changed - exiting asteroid spawn routine");
                spawningActive = false;
                yield break; // Salir completamente de la coroutine, no continuar
            }

            // Calcular retraso basado en dificultad
            float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay) / difficultyMultiplier;
            yield return new WaitForSeconds(spawnDelay);

            // TRIPLE VERIFICACIÓN - asegurarnos de que el juego sigue activo antes de crear el asteroide
            if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
            {
                Debug.Log("Game state changed before spawning - exiting routine");
                spawningActive = false;
                yield break;
            }

            // Ahora sí creamos el asteroide
            SpawnAsteroid();
        }
    }

    private void SpawnAsteroid()
    {
        // VERIFICACIÓN FINAL - no crear asteroides si el juego no está activo
        if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
        {
            Debug.LogWarning("Attempted to spawn asteroid while game is not active!");
            return;
        }

        // Select random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        GameObject asteroid;

        // Usar pool de objetos o crear nuevo según configuración
        if (useObjectPooling)
        {
            asteroid = GetAsteroidFromPool();
            if (asteroid == null)
            {
                Debug.LogError("Failed to get asteroid from pool");
                return;
            }

            // Posicionar el asteroide y activarlo
            asteroid.transform.position = spawnPoint.position;
            asteroid.SetActive(true);
        }
        else
        {
            // Método tradicional sin pooling
            asteroid = Instantiate(asteroidPrefab, spawnPoint.position, Quaternion.identity);
        }

        // Randomize asteroid properties
        float size = Random.Range(minSize, maxSize);
        asteroid.transform.localScale = new Vector3(size, size, size);

        // Calculate direction towards player and apply random variation
        Vector3 direction = (playerTransform.position - spawnPoint.position).normalized;

        // Add slight randomness to direction
        direction += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
        direction.Normalize();

        // Calculate speed based on difficulty
        float speed = Random.Range(minSpeed, maxSpeed) * difficultyMultiplier;

        // Add Asteroid component with parameters
        Asteroid asteroidComponent = asteroid.GetComponent<Asteroid>();
        if (asteroidComponent == null)
        {
            asteroidComponent = asteroid.AddComponent<Asteroid>();
        }

        asteroidComponent.Initialize(direction, speed, destroyDistance);
    }

    // Method to manually add spawn points at runtime
    public void AddSpawnPoint(Transform spawnTransform)
    {
        if (spawnTransform != null && !spawnPoints.Contains(spawnTransform))
        {
            spawnPoints.Add(spawnTransform);
            Debug.Log("Added new spawn point: " + spawnTransform.name);
        }
    }

    // Method to stop spawning
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        spawningActive = false;
        Debug.Log("Asteroid spawning STOPPED");
    }

    // Method to restart spawning
    public void RestartSpawning()
    {
        // Verificar que el juego esté activo antes de iniciar
        if (gameManager != null && !gameManager.gameStarted)
        {
            Debug.LogWarning("Attempted to start spawning while game is not active - PREVENTED");
            return;
        }

        StopSpawning(); // Detener cualquier rutina existente
        spawnCoroutine = StartCoroutine(SpawnAsteroidsRoutine());
        Debug.Log("Asteroid spawning STARTED");
    }

    // Reset difficulty for new game
    public void ResetDifficulty()
    {
        difficultyMultiplier = 1f;
        timeSinceStart = 0f;
    }

    // Destruir todos los asteroides existentes
    public void DestroyAllAsteroids()
    {
        if (useObjectPooling)
        {
            // Desactivar todos los asteroides en el pool
            foreach (GameObject asteroid in asteroidPool)
            {
                if (asteroid != null)
                {
                    asteroid.SetActive(false);
                }
            }
        }
        else
        {
            // Método tradicional: destruir todos los asteroides
            GameObject[] asteroids = GameObject.FindGameObjectsWithTag("Asteroid");
            foreach (GameObject asteroid in asteroids)
            {
                Destroy(asteroid);
            }
        }

        Debug.Log("All existing asteroids removed");
    }

#if UNITY_EDITOR
    // For visualization in the editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);

                if (playerTransform != null)
                {
                    Gizmos.DrawLine(spawnPoint.position, playerTransform.position);
                }
            }
        }
    }
#endif
}