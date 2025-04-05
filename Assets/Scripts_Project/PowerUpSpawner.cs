using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject healthPowerUpPrefab;
    [SerializeField] private float initialSpawnDelay = 20f;
    [SerializeField] private float minSpawnInterval = 15f;
    [SerializeField] private float maxSpawnInterval = 30f;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Spawn Area")]
    [SerializeField] private bool useRandomSpawnArea = true;
    [SerializeField] private Transform[] specificSpawnPoints;
    [SerializeField][Range(0f, 0.4f)] private float edgePadding = 0.1f;

    [Header("Dynamic Spawning")]
    [SerializeField] private bool increaseFrequencyWhenLowHealth = true;
    [SerializeField] private float lowHealthMultiplier = 0.7f;

    private Camera mainCamera;
    private Coroutine spawnCoroutine;
    private GameManager gameManager;
    private bool spawningActive = false;

    private void Awake()
    {
        // No iniciar ninguna coroutine en Awake
        spawnCoroutine = null;
        spawningActive = false;
    }

    private void Start()
    {
        // Obtener la cámara principal
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found! PowerUpSpawner requires a main camera.");
            this.enabled = false;
            return;
        }

        // Encontrar GameManager
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found! PowerUpSpawner requires GameManager.");
            this.enabled = false; // Desactivar este componente si no hay GameManager
            return;
        }

        // Buscar playerHealth si no está asignado
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                Debug.Log("PowerUpSpawner: PlayerHealth component found automatically");
            }
            else
            {
                Debug.LogWarning("Player not found - PowerUp frequency won't adjust based on health");
            }
        }

        // Verificación del prefab
        if (healthPowerUpPrefab == null)
        {
            Debug.LogError("Health PowerUp prefab not assigned! PowerUpSpawner won't function.");
            this.enabled = false;
            return;
        }

        // Verificar spawnPoints
        if (!useRandomSpawnArea && (specificSpawnPoints == null || specificSpawnPoints.Length == 0))
        {
            Debug.LogWarning("No specific spawn points assigned but useRandomSpawnArea is false. Defaulting to random spawning.");
            useRandomSpawnArea = true;
        }

        Debug.Log("PowerUpSpawner initialized - NOT spawning until game starts");
    }

    private void Update()
    {
        // Verificar estado del juego
        if (gameManager == null)
            return;

        // Iniciar o detener spawning basado en el estado del juego
        if (gameManager.gameStarted && !gameManager.gamePaused && !gameManager.gameOver)
        {
            // Iniciar spawning si no está activo
            if (!spawningActive)
            {
                Debug.Log("Game is now active - STARTING powerup spawning");
                RestartSpawning();
            }
        }
        else
        {
            // Detener spawning si el juego no está activo
            if (spawningActive)
            {
                Debug.Log("Game is not active - STOPPING powerup spawning");
                StopSpawning();
            }
        }
    }

    private IEnumerator SpawnPowerUpsRoutine()
    {
        spawningActive = true;
        Debug.Log("PowerUpSpawner routine STARTED");

        // Initial delay before first spawn
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            // DOBLE VERIFICACIÓN crítica - si en algún momento el juego no está activo, detenemos la coroutine
            if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
            {
                Debug.Log("Game state changed - exiting powerup spawn routine");
                spawningActive = false;
                yield break; // Salir completamente de la coroutine
            }

            // Calculate next spawn interval
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);

            // Adjust interval based on player health if enabled
            if (increaseFrequencyWhenLowHealth && playerHealth != null)
            {
                // If player is at 1 life, spawn more frequently
                if (playerHealth.CurrentLives == 1)
                {
                    interval *= lowHealthMultiplier;
                    Debug.Log($"Low health detected - reduced spawn interval to {interval}s");
                }
            }

            Debug.Log($"Next powerup in {interval} seconds");
            yield return new WaitForSeconds(interval);

            // TRIPLE VERIFICACIÓN - asegurarnos de que el juego sigue activo antes de crear el powerup
            if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
            {
                Debug.Log("Game state changed before spawning powerup - exiting routine");
                spawningActive = false;
                yield break;
            }

            // Ahora sí creamos el powerup
            SpawnHealthPowerUp();
        }
    }

    private void SpawnHealthPowerUp()
    {
        // VERIFICACIÓN FINAL - no crear powerups si el juego no está activo
        if (gameManager == null || !gameManager.gameStarted || gameManager.gamePaused || gameManager.gameOver)
        {
            Debug.LogWarning("Attempted to spawn powerup while game is not active!");
            return;
        }

        Vector3 spawnPosition;

        if (useRandomSpawnArea)
        {
            // Get random position in viewport with padding
            spawnPosition = new Vector3(
                Random.Range(edgePadding, 1f - edgePadding),
                Random.Range(edgePadding, 1f - edgePadding),
                0f  // Mantenemos Z en 0 ya que estamos en 2D
            );

            // Convert to world position
            spawnPosition = mainCamera.ViewportToWorldPoint(spawnPosition);
            spawnPosition.z = 0;  // Asegurar que Z es siempre 0

            Debug.Log($"Spawning powerup at random position: {spawnPosition}");
        }
        else if (specificSpawnPoints != null && specificSpawnPoints.Length > 0)
        {
            // Pick a random spawn point from the array
            Transform spawnPoint = specificSpawnPoints[Random.Range(0, specificSpawnPoints.Length)];
            if (spawnPoint == null)
            {
                Debug.LogError("PowerUpSpawner: Selected spawn point is null!");
                return;
            }

            spawnPosition = spawnPoint.position;
            Debug.Log($"Spawning powerup at specific spawn point: {spawnPosition} (from {spawnPoint.name})");
        }
        else
        {
            // Fallback to center of screen
            spawnPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            spawnPosition.z = 0;
            Debug.Log($"Spawning powerup at screen center fallback: {spawnPosition}");
        }

        // Instantiate the power-up
        GameObject powerup = Instantiate(healthPowerUpPrefab, spawnPosition, Quaternion.identity);

        // Asegurarse de que tiene tag correcto
        if (!powerup.CompareTag("PowerUp"))
        {
            powerup.tag = "PowerUp";
            Debug.Log("PowerUp tag was missing - applied automatically");
        }

        // Log de confirmación
        Debug.Log($"PowerUp successfully spawned at {spawnPosition}");
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        spawningActive = false;
        Debug.Log("PowerUp spawning STOPPED");
    }

    public void RestartSpawning()
    {
        // Verificar que el juego esté activo antes de iniciar
        if (gameManager != null && !gameManager.gameStarted)
        {
            Debug.LogWarning("Attempted to start powerup spawning while game is not active - PREVENTED");
            return;
        }

        StopSpawning(); // Detener cualquier rutina existente
        spawnCoroutine = StartCoroutine(SpawnPowerUpsRoutine());
        Debug.Log("PowerUp spawning STARTED");
    }

    // Force spawn a power-up immediately (useful for testing)
    public void ForceSpawnPowerUp()
    {
        Debug.Log("Force spawning a powerup");
        SpawnHealthPowerUp();
    }

    // Destruir todos los powerups existentes
    public void DestroyAllPowerUps()
    {
        GameObject[] powerups = GameObject.FindGameObjectsWithTag("PowerUp");
        foreach (GameObject powerup in powerups)
        {
            Destroy(powerup);
        }
        Debug.Log("Destroyed all existing powerups: " + powerups.Length);
    }
}