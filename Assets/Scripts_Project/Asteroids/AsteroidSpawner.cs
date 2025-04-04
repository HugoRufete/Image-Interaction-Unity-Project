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

    private float difficultyMultiplier = 1f;
    private float timeSinceStart = 0f;
    private Coroutine spawnCoroutine;

    private void Start()
    {
        // Validate settings
        if (asteroidPrefab == null)
        {
            Debug.LogError("Asteroid prefab not assigned to AsteroidSpawner!");
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
                return;
            }
        }

        // Start spawning asteroids
        spawnCoroutine = StartCoroutine(SpawnAsteroidsRoutine());
    }

    private void Update()
    {
        if (increaseDifficultyOverTime)
        {
            timeSinceStart += Time.deltaTime;
            difficultyMultiplier = Mathf.Min(1 + (timeSinceStart * difficultyIncreaseRate / 60f), maxDifficultyMultiplier);
        }
    }

    private IEnumerator SpawnAsteroidsRoutine()
    {
        while (true)
        {
            // Calculate spawn delay based on difficulty
            float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay) / difficultyMultiplier;
            yield return new WaitForSeconds(spawnDelay);

            SpawnAsteroid();
        }
    }

    private void SpawnAsteroid()
    {
        // Select random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // Create asteroid
        GameObject asteroid = Instantiate(asteroidPrefab, spawnPoint.position, Quaternion.identity);

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
    }

    // Method to restart spawning
    public void RestartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnAsteroidsRoutine());
        }
    }

    // Reset difficulty for new game
    public void ResetDifficulty()
    {
        difficultyMultiplier = 1f;
        timeSinceStart = 0f;
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