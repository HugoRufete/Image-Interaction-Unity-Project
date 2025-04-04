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

    private void Start()
    {
        // Store initial position
        startPosition = transform.position;

        // Randomize initial rotation
        if (randomizeRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            rotationSpeed *= Random.Range(0.5f, 1.5f) * (Random.value > 0.5f ? 1 : -1); // Random speed and direction
        }
    }

    public void Initialize(Vector3 direction, float speed, float destroyDistance)
    {
        this.direction = direction;
        this.speed = speed;
        this.destroyDistance = destroyDistance;
    }

    private void Update()
    {
        // Move the asteroid
        transform.position += direction * speed * Time.deltaTime;

        // Apply rotation
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Check if asteroid has moved too far from start position
        if (Vector3.Distance(startPosition, transform.position) > destroyDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if asteroid hit the player
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }

            // Optional - destroy asteroid on hit
            Destroy(gameObject);
        }
    }
}