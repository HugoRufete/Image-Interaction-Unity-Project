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

    private void Start()
    {
        spawnTime = Time.time;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Set random move direction
        moveDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
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
        }

        // Destroy if exceeded lifetime
        if (elapsedTime >= lifetime)
        {
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
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.AddLife(healthToRestore);

                // Play pickup effect here if needed
                // PlayPickupEffect();

                // Destroy the powerup
                Destroy(gameObject);
            }
        }
    }

    // This method keeps the powerup within screen boundaries
    private void KeepInBounds()
    {
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);

        // If powerup hits screen edge, bounce it
        if (viewportPosition.x <= 0.05f || viewportPosition.x >= 0.95f)
        {
            moveDirection.x = -moveDirection.x;
        }

        if (viewportPosition.y <= 0.05f || viewportPosition.y >= 0.95f)
        {
            moveDirection.y = -moveDirection.y;
        }

        // Ensure it stays on screen
        viewportPosition.x = Mathf.Clamp(viewportPosition.x, 0.05f, 0.95f);
        viewportPosition.y = Mathf.Clamp(viewportPosition.y, 0.05f, 0.95f);

        transform.position = Camera.main.ViewportToWorldPoint(viewportPosition);
    }

    private void LateUpdate()
    {
        // Keep powerup in bounds
        KeepInBounds();
    }
}