using UnityEngine;

public class HealthPowerUp : MonoBehaviour
{
    [Header("PowerUp Settings")]
    [SerializeField] private int healthToRestore = 1;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float blinkStartTime = 7f;
    [SerializeField] private float blinkRate = 0.2f;

    [Header("Visual Effects")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float spawnTime;
    private bool isBlinking = false;
    private Vector3 moveDirection;
    private Camera mainCamera;

    private void Start()
    {
        spawnTime = Time.time;
        mainCamera = Camera.main;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("HealthPowerUp: SpriteRenderer component not found!");
            }
        }

        // Set random move direction
        moveDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;

        // Log spawn details
        Debug.Log($"HealthPowerUp spawned at position {transform.position} with direction {moveDirection}");
    }

    private void Update()
    {
        // Move the powerup
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Rotate for visual effect
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Check if powerup should start blinking
        float elapsedTime = Time.time - spawnTime;

        if (elapsedTime >= blinkStartTime && !isBlinking)
        {
            isBlinking = true;
            InvokeRepeating(nameof(ToggleVisibility), 0, blinkRate);
            Debug.Log("HealthPowerUp started blinking");
        }

        // Destroy if exceeded lifetime
        if (elapsedTime >= lifetime)
        {
            Debug.Log("HealthPowerUp lifetime expired - destroying");
            Destroy(gameObject);
        }
    }

    private void ToggleVisibility()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player collected the powerup
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player contacted HealthPowerUp");

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.AddLife(healthToRestore);
                Debug.Log($"Player health increased by {healthToRestore}. New health: {playerHealth.CurrentLives}");

                // Destroy the powerup
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("Player object missing PlayerHealth component!");
            }
        }
    }

    // This method keeps the powerup within screen boundaries
    private void KeepInBounds()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);

        bool wasOutOfBounds = false;

        // If powerup hits screen edge, bounce it
        if (viewportPosition.x <= 0.05f || viewportPosition.x >= 0.95f)
        {
            moveDirection.x = -moveDirection.x;
            wasOutOfBounds = true;
        }

        if (viewportPosition.y <= 0.05f || viewportPosition.y >= 0.95f)
        {
            moveDirection.y = -moveDirection.y;
            wasOutOfBounds = true;
        }

        if (wasOutOfBounds)
        {
            Debug.Log("HealthPowerUp hit screen boundary - direction changed to " + moveDirection);
        }

        // Ensure it stays on screen
        viewportPosition.x = Mathf.Clamp(viewportPosition.x, 0.05f, 0.95f);
        viewportPosition.y = Mathf.Clamp(viewportPosition.y, 0.05f, 0.95f);

        // Convertimos a coordenadas de mundo manteniendo Z siempre en 0
        Vector3 worldPos = mainCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 0));
        transform.position = new Vector3(worldPos.x, worldPos.y, 0);
    }

    private void LateUpdate()
    {
        // Keep powerup in bounds
        KeepInBounds();
    }
}