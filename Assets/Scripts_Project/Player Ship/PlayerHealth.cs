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
    [SerializeField] private Image[] livesIcons;

    [Header("Visual Feedback")]
    [SerializeField] private float blinkRate = 0.1f;
    [SerializeField] private SpriteRenderer shipSpriteRenderer;

    [Header("Events")]
    public UnityEvent onPlayerHit;
    public UnityEvent onPlayerDeath;

    private int currentLives;
    private bool isInvincible = false;
    private float survivalTime = 0f;

    public int CurrentLives => currentLives;
    public float SurvivalTime => survivalTime;

    private void Start()
    {
        currentLives = maxLives;
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

        currentLives -= amount;

        // Ensure lives don't go below 0
        currentLives = Mathf.Max(0, currentLives);

        // Update UI
        UpdateUI();

        // Invoke hit event
        onPlayerHit?.Invoke();

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
        currentLives += amount;

        // Ensure lives don't exceed max
        currentLives = Mathf.Min(currentLives, maxLives);

        // Update UI
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update text if available
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }

        // Update icons if available
        if (livesIcons != null && livesIcons.Length > 0)
        {
            for (int i = 0; i < livesIcons.Length; i++)
            {
                if (livesIcons[i] != null)
                {
                    livesIcons[i].enabled = i < currentLives;
                }
            }
        }
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
    }
}