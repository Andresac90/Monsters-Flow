using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TMP_Text moneyText;
    public TMP_Text roundText;
    public TMP_Text healthText;
    public Image    healthBar;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    public Button     resumeButton;
    public Button     mainMenuButton;
    public Button     quitButton;

    [Header("Stats Panel")]
    public GameObject statsPanel;
    public TMP_Text   killsText;
    public TMP_Text   headshotsText;
    public TMP_Text   roundsText;
    public TMP_Text   timePlayedText;
    public TMP_Text   highestRoundText;

    [Header("Perk UI")]
    public GameObject perkIconsPanel;
    public GameObject perkIconPrefab;

    [Header("Interaction Prompts")]
    public GameObject interactionPrompt;
    public TMP_Text   interactionText;

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (statsPanel != null)
        {
            bool show = Input.GetKey(KeyCode.Tab);
            statsPanel.SetActive(show);
            if (show) UpdateStatsPanel();
        }

        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (moneyText != null)
            moneyText.text = $"${GameManager.Instance.currentMoney}";

        if (roundText != null)
        {
            var spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
                roundText.text = "Round: " + spawner.CurrentRound;
        }

        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
        {
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(ph.CurrentHealth)}/{Mathf.CeilToInt(ph.maxHealth)}";
            if (healthBar != null)
                healthBar.fillAmount = ph.CurrentHealth / ph.maxHealth;
        }
    }

    public void UpdateStatsPanel()
    {
        if (GameManager.Instance == null) return;

        if (killsText      != null) killsText.text      = "Kills: " + GameManager.Instance.totalKills;
        if (headshotsText  != null) headshotsText.text  = "Headshots: " + GameManager.Instance.totalHeadshots;
        if (roundsText     != null) roundsText.text     = "Rounds Completed: " + GameManager.Instance.totalRoundsCompleted;
        if (highestRoundText != null) highestRoundText.text = "Highest Round: " + GameManager.Instance.highestRoundReached;

        int minutes = Mathf.FloorToInt(GameManager.Instance.timeAlive / 60);
        int seconds = Mathf.FloorToInt(GameManager.Instance.timeAlive % 60);
        string t   = $"{minutes:00}:{seconds:00}";

        if (timePlayedText != null) timePlayedText.text = "Time Alive: " + t;
    }

    public void ShowInteractionPrompt(string message)
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(true);
        if (interactionText   != null) interactionText.text = message;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    public void UpdatePerkIcons()
    {
        if (GameManager.Instance == null || perkIconsPanel == null || perkIconPrefab == null)
            return;

        foreach (Transform c in perkIconsPanel.transform)
            Destroy(c.gameObject);

        foreach (var perk in GameManager.Instance.activePerks)
        {
            var icon = Instantiate(perkIconPrefab, perkIconsPanel.transform);
            var txt  = icon.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = perk.name;
        }
    }

    private void OnResumeClicked()
    {
        GameManager.Instance?.TogglePause();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnQuitClicked()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
