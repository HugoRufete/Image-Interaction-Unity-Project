using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public bool gameStarted = false;
    public bool gamePaused = false;
    public bool gameOver = false;

    [Header("UI Panels")]
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject colorSelectionPanel;

    [Header("Game Control References")]
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [SerializeField] private PowerUpSpawner powerUpSpawner;
    [SerializeField] private SurvivalTimer survivalTimer;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Game Over Display")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private Button restartButton;

    [Header("Debug")]
    [SerializeField] private bool autoStartGame = false;
    [SerializeField] private bool logDebugMessages = true;

    private void Awake()
    {
        // Asegurar variables de estado iniciales
        gameStarted = false;
        gamePaused = false;
        gameOver = false;

        // Log de inicialización
        DebugLog("GameManager Awake - Setting initial game state variables");
    }

    private void Start()
    {
        // Find required components if not set in inspector
        FindAndAssignComponents();

        // Detener explícitamente cualquier actividad
        ForceStopAllGameSystems();

        // Activar UI inicial
        SetInitialUI();

        DebugLog("GameManager initialized - Game is NOT started. Waiting for player to press Start");

        // Auto-start game if in debug mode (disabled by default)
        if (autoStartGame)
        {
            DebugLog("Auto-start enabled - will start game in 0.5 seconds");
            Invoke(nameof(StartGame), 0.5f);
        }

        if (playerHealth != null && playerHealth.onPlayerDeath != null)
        {
            playerHealth.onPlayerDeath.AddListener(EndGame);
            DebugLog("Subscribed to player death event");
        }
    }

    private void FindAndAssignComponents()
    {
        if (asteroidSpawner == null)
        {
            asteroidSpawner = FindAnyObjectByType<AsteroidSpawner>();
            DebugLog("Found AsteroidSpawner: " + (asteroidSpawner != null));
        }

        if (powerUpSpawner == null)
        {
            powerUpSpawner = FindAnyObjectByType<PowerUpSpawner>();
            DebugLog("Found PowerUpSpawner: " + (powerUpSpawner != null));
        }

        if (survivalTimer == null)
        {
            survivalTimer = FindAnyObjectByType<SurvivalTimer>();
            DebugLog("Found SurvivalTimer: " + (survivalTimer != null));
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            DebugLog("Found PlayerHealth: " + (playerHealth != null));
        }
    }

    private void ForceStopAllGameSystems()
    {
        // Detener spawners
        if (asteroidSpawner != null)
        {
            asteroidSpawner.StopSpawning();
            asteroidSpawner.DestroyAllAsteroids();
            DebugLog("Forced stop of AsteroidSpawner");
        }

        if (powerUpSpawner != null)
        {
            powerUpSpawner.StopSpawning();
            powerUpSpawner.DestroyAllPowerUps();
            DebugLog("Forced stop of PowerUpSpawner");
        }

        // Detener timer
        if (survivalTimer != null)
        {
            survivalTimer.StopTimer();
            survivalTimer.ResetTimer();
            DebugLog("Forced stop and reset of SurvivalTimer");
        }
    }

    private void SetInitialUI()
    {
        // Activar panel de selección de color al inicio
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(true);
            DebugLog("Enabled Color Selection Panel");
        }

        // Activar UI inicial
        if (startUI != null)
        {
            startUI.SetActive(true);
            DebugLog("Enabled Start UI");
        }

        // Desactivar HUD y Game Over
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    public void StartGame()
    {
        DebugLog("StartGame() called - Starting game now");

        // Asegurar que no hay objetos activos de sesiones anteriores
        CleanupGameObjects();

        // Resetear jugador
        if (playerHealth != null)
        {
            playerHealth.ResetLives();
            DebugLog("Reset player lives");
        }

        // Actualizar estado del juego ANTES de iniciar cualquier sistema
        gameStarted = true;
        gamePaused = false;
        gameOver = false;
        DebugLog("Game state updated: gameStarted=true, gamePaused=false, gameOver=false");

        // Resetear y activar timer
        if (survivalTimer != null)
        {
            survivalTimer.ResetTimer();
            survivalTimer.StartTimer();
            DebugLog("Started survival timer");
        }

        // Ahora sí podemos iniciar los spawners
        // Los spawners verificarán el estado del juego antes de iniciar
        if (asteroidSpawner != null)
        {
            asteroidSpawner.ResetDifficulty();
            DebugLog("Asteroid spawner ready - will start automatically");
        }

        if (powerUpSpawner != null)
        {
            DebugLog("PowerUp spawner ready - will start automatically");
        }

        // Actualizar UI
        if (colorSelectionPanel != null) colorSelectionPanel.SetActive(false);
        if (startUI != null) startUI.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (gameOverUI != null) gameOverUI.SetActive(false);

        DebugLog("Game Started - UI updated and systems activated");
    }

    public void PauseGame()
    {
        if (!gameStarted || gameOver)
        {
            DebugLog("PauseGame ignored - game not active");
            return;
        }

        gamePaused = true;
        DebugLog("Game PAUSED - gamePaused=true");

        // Los spawners deberían detectar esta pausa automáticamente

        // Update UI
        if (hudPanel != null) hudPanel.SetActive(false);

        DebugLog("Game Paused - HUD hidden");
    }

    public void ResumeGame()
    {
        if (!gameStarted || gameOver)
        {
            DebugLog("ResumeGame ignored - game not active");
            return;
        }

        gamePaused = false;
        DebugLog("Game RESUMED - gamePaused=false");

        // Los spawners deberían detectar este cambio automáticamente

        // Update UI
        if (hudPanel != null) hudPanel.SetActive(true);
        if (colorSelectionPanel != null && colorSelectionPanel.activeSelf)
        {
            colorSelectionPanel.SetActive(false);
        }

        DebugLog("Game Resumed - HUD shown");
    }

    public void EndGame()
    {
        if (gameOver)
        {
            DebugLog("EndGame ignored - already in game over state");
            return;
        }

        DebugLog("EndGame called - Game is ending");

        // Actualizar estado
        gameOver = true;
        gamePaused = true;
        DebugLog("Game state updated: gameOver=true, gamePaused=true");

        // Los spawners deberían detectar este cambio automáticamente

        // Actualizar pantalla de fin de juego
        if (survivalTimer != null && finalScoreText != null)
        {
            finalScoreText.text = "Time Survived: " + FormatTime(survivalTimer.GetSurvivalTime());
        }

        // Update UI
        if (colorSelectionPanel != null) colorSelectionPanel.SetActive(false);
        if (startUI != null) startUI.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);

        DebugLog("Game Over - UI updated");
    }

    public void ToggleColorSelectionPanel()
    {
        if (colorSelectionPanel == null) return;

        if (colorSelectionPanel.activeSelf)
        {
            DebugLog("Closing color selection panel");
            colorSelectionPanel.SetActive(false);

            // Si el juego estaba en marcha, reanudarlo
            if (gameStarted && !gameOver)
            {
                ResumeGame();
            }
        }
        else
        {
            DebugLog("Opening color selection panel");

            // Pausar el juego si estaba activo
            if (gameStarted && !gameOver)
            {
                PauseGame();
            }

            colorSelectionPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        DebugLog("RestartGame called - Resetting all game systems");

        // Limpiar objetos existentes
        CleanupGameObjects();

        // Resetear estado del juego
        gameOver = false;
        gamePaused = false;
        gameStarted = false;
        DebugLog("Game state reset: gameStarted=false, gamePaused=false, gameOver=false");

        // Forzar detención de sistemas
        ForceStopAllGameSystems();

        // Resetear jugador
        if (playerHealth != null)
        {
            playerHealth.ResetLives();
        }

        // Update UI
        if (colorSelectionPanel != null) colorSelectionPanel.SetActive(true);
        if (startUI != null) startUI.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);

        DebugLog("Game Restarted - Back to initial state");
    }

    private void CleanupGameObjects()
    {
        // Usar los métodos específicos de cada spawner si es posible
        if (asteroidSpawner != null)
        {
            asteroidSpawner.DestroyAllAsteroids();
        }
        else
        {
            // Fallback si no hay spawner
            DestroyGameObjectsWithTag("Asteroid");
        }

        if (powerUpSpawner != null)
        {
            powerUpSpawner.DestroyAllPowerUps();
        }
        else
        {
            // Fallback si no hay spawner
            DestroyGameObjectsWithTag("PowerUp");
        }

        DebugLog("All game objects cleaned up");
    }

    private void DestroyGameObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
        DebugLog($"Destroyed {objects.Length} objects with tag '{tag}'");
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
    }

    private void DebugLog(string message)
    {
        if (logDebugMessages)
        {
            Debug.Log("[GameManager] " + message);
        }
    }
}