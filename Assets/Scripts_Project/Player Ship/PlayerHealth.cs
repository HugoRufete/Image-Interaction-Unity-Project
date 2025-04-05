using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float invincibilityTime = 1.5f;

    [Header("UI References")]
    [SerializeField] private TMP_Text livesText;

    // Se eliminan los iconos de vida antiguos
    // [SerializeField] private Image[] livesIcons;

    [Header("Lives Display System")]
    [SerializeField] private GameObject lifePrefab; // Prefab que representa una vida
    [SerializeField] private Transform livesContainer; // El contenedor con HorizontalLayoutGroup
    [SerializeField] private bool useAnimationOnLifeChange = true;
    [SerializeField] private float lifeIconSpacing = 5f; // Espacio entre iconos de vida

    [Header("Visual Feedback")]
    [SerializeField] private float blinkRate = 0.1f;
    [SerializeField] private SpriteRenderer shipSpriteRenderer;

    [Header("Events")]
    public UnityEvent onPlayerHit;
    public UnityEvent onPlayerDeath;
    public UnityEvent onLifeAdded;
    public UnityEvent onLifeRemoved;

    private int currentLives;
    private bool isInvincible = false;
    private float survivalTime = 0f;
    private GameObject[] lifeObjects; // Array para almacenar las referencias a los objetos de vida

    public int CurrentLives => currentLives;
    public float SurvivalTime => survivalTime;

    private void Start()
    {
        currentLives = maxLives;

        // Inicializar array de objetos de vida
        lifeObjects = new GameObject[maxLives];

        // Comprobar que tenemos un contenedor y un prefab
        if (lifePrefab == null)
        {
            Debug.LogError("Life prefab not assigned to PlayerHealth. Cannot display life icons.");
        }

        if (livesContainer == null)
        {
            Debug.LogError("Lives container not assigned to PlayerHealth. Cannot display life icons.");
        }

        // Verificar que el contenedor tiene un HorizontalLayoutGroup
        if (livesContainer != null && livesContainer.GetComponent<HorizontalLayoutGroup>() == null)
        {
            Debug.LogWarning("Lives container does not have a HorizontalLayoutGroup component. Adding one...");
            HorizontalLayoutGroup layout = livesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = lifeIconSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
        }

        // Inicializar la visualización de vidas
        UpdateLivesDisplay();

        // Actualizar texto UI
        UpdateUI();

        // If sprite renderer not assigned, try to get it from this object
        if (shipSpriteRenderer == null)
        {
            shipSpriteRenderer = GetComponent<SpriteRenderer>();
            if (shipSpriteRenderer == null)
            {
                shipSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
    }

    private void Update()
    {
        // Update survival time while alive
        if (currentLives > 0)
        {
            survivalTime += Time.deltaTime;
        }
    }

    public void TakeDamage(int amount = 1)
    {
        // If already invincible, do not take damage
        if (isInvincible) return;

        int previousLives = currentLives;
        currentLives -= amount;

        // Ensure lives don't go below 0
        currentLives = Mathf.Max(0, currentLives);

        // Update UI
        UpdateUI();

        // Update lives display
        UpdateLivesDisplay();

        // Invoke hit event
        onPlayerHit?.Invoke();

        // Lanzar evento de vida perdida
        if (previousLives > currentLives)
        {
            onLifeRemoved?.Invoke();
        }

        // Play hit feedback (blink)
        StartCoroutine(InvincibilityCoroutine());

        // Check for death
        if (currentLives <= 0)
        {
            OnPlayerDeath();
        }
    }

    public void AddLife(int amount = 1)
    {
        int previousLives = currentLives;
        currentLives += amount;

        // Ensure lives don't exceed max
        currentLives = Mathf.Min(currentLives, maxLives);

        // Update UI
        UpdateUI();

        // Update lives display
        UpdateLivesDisplay();

        // Lanzar evento de vida añadida
        if (currentLives > previousLives)
        {
            onLifeAdded?.Invoke();
        }
    }

    private void UpdateUI()
    {
        // Update text if available
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
    }

    private void UpdateLivesDisplay()
    {
        // Verificar que tenemos el prefab y el contenedor
        if (lifePrefab == null || livesContainer == null)
            return;

        // Limpiar los iconos existentes
        foreach (Transform child in livesContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear nuevos iconos de vida basados en el número de vidas actuales
        for (int i = 0; i < currentLives; i++)
        {
            GameObject lifeIcon = Instantiate(lifePrefab, livesContainer);

            // Opcional: Añadir alguna animación para el cambio de vidas
            if (useAnimationOnLifeChange)
            {
                // Efecto de escala
                lifeIcon.transform.localScale = Vector3.zero;
                StartCoroutine(ScaleInAnimation(lifeIcon.transform));
            }

            // Guardar referencia
            lifeObjects[i] = lifeIcon;
        }
    }

    private IEnumerator ScaleInAnimation(Transform iconTransform)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // Curva de animación suavizada
            float scale = Mathf.SmoothStep(0, 1, normalizedTime);
            iconTransform.localScale = new Vector3(scale, scale, scale);

            yield return null;
        }

        // Asegurarse de que termina con escala 1
        iconTransform.localScale = Vector3.one;
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        // Visual feedback - blink ship
        if (shipSpriteRenderer != null)
        {
            float endTime = Time.time + invincibilityTime;

            while (Time.time < endTime)
            {
                shipSpriteRenderer.enabled = !shipSpriteRenderer.enabled;
                yield return new WaitForSeconds(blinkRate);
            }

            // Ensure sprite is visible at the end
            shipSpriteRenderer.enabled = true;
        }
        else
        {
            // Just wait for invincibility time
            yield return new WaitForSeconds(invincibilityTime);
        }

        isInvincible = false;
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player died. Survival time: " + survivalTime.ToString("F1") + " seconds");

        // Invoke death event
        onPlayerDeath?.Invoke();

        // Additional death logic can be added here
    }

    // For power-ups or testing
    public void ResetLives()
    {
        currentLives = maxLives;
        UpdateUI();
        UpdateLivesDisplay();
    }
}