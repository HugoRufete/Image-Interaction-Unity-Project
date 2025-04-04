using UnityEngine;
using TMPro;
using System;

public class SurvivalTimer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Display Format")]
    [SerializeField] private bool showMinutesAndSeconds = true;
    [SerializeField] private string timePrefix = "Time: ";
    [SerializeField] private string highScorePrefix = "Best: ";

    [Header("High Score")]
    [SerializeField] private bool trackHighScore = true;
    [SerializeField] private string highScoreKey = "HighScoreSurvivalTime";

    private float survivalTime = 0f;
    private bool isTimerRunning = true;
    private float highScore = 0f;

    private void Start()
    {
        // Load high score if tracking is enabled
        if (trackHighScore)
        {
            highScore = PlayerPrefs.GetFloat(highScoreKey, 0f);
            UpdateHighScoreText();
        }
        else if (highScoreText != null)
        {
            highScoreText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            survivalTime += Time.deltaTime;
            UpdateTimerText();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = timePrefix + FormatTime(survivalTime);
        }
    }

    private void UpdateHighScoreText()
    {
        if (highScoreText != null)
        {
            highScoreText.text = highScorePrefix + FormatTime(highScore);
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        if (showMinutesAndSeconds)
        {
            // Format as MM:SS
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
            return string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
        }
        else
        {
            // Format as seconds with one decimal place
            return timeInSeconds.ToString("F1") + "s";
        }
    }

    // Public methods to control the timer

    public void StopTimer()
    {
        isTimerRunning = false;
        CheckForHighScore();
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void ResetTimer()
    {
        survivalTime = 0f;
        UpdateTimerText();
    }

    public float GetSurvivalTime()
    {
        return survivalTime;
    }

    private void CheckForHighScore()
    {
        if (trackHighScore && survivalTime > highScore)
        {
            highScore = survivalTime;
            PlayerPrefs.SetFloat(highScoreKey, highScore);
            PlayerPrefs.Save();
            UpdateHighScoreText();
        }
    }

    // Method to manually set survival time (useful for syncing with PlayerHealth)
    public void SetSurvivalTime(float time)
    {
        survivalTime = time;
        UpdateTimerText();
    }

    // Method to display final score (called when game ends)
    public void ShowFinalScore()
    {
        if (timerText != null)
        {
            timerText.text = "Final Time: " + FormatTime(survivalTime);

            // Change color or style to highlight the final score
            timerText.color = Color.yellow;
            timerText.fontSize *= 1.2f;
        }
    }
}