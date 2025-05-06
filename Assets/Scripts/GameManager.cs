using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    #region Singleton Setup
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    [Header("Player References")]
    public PlayerController      playerController;
    public PlayerHealth          playerHealth;
    public WaterMagicController  waterMagicController;

    [Header("Economy Settings")]
    public int startingMoney            = 500;
    [HideInInspector] public int currentMoney;
    public int moneyPerKill             = 50;
    public int moneyPerFireExtinguished = 10;
    public int moneyPerHeadshot         = 100;

    [Header("Stats Tracking")]
    public int   totalKills;
    public int   totalHeadshots;
    public int   totalRoundsCompleted;
    public int   highestRoundReached     = 0;
    public int   totalFiresExtinguished;
    public float timeAlive; // seconds
    public int   totalPerksActivated;

    [Header("UI References")]
    public TMP_Text moneyText;
    public GameObject pauseMenu;
    public GameObject statsPanel;
    public GameObject perkShopPanel;
    public TMP_Text statsKillsText;
    public TMP_Text statsRoundText;
    public TMP_Text statsTimeText;
    public TMP_Text statsFiresText;
    public TMP_Text statsPerksText;

    [Header("Pause Settings")]
    private bool isPaused = false;

    [Header("Perk System")]
    public List<PerkData> availablePerks = new List<PerkData>();
    public List<PerkData> activePerks    = new List<PerkData>();
    public int           maxPerks        = 5;

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        HandlePauseInput();
        UpdateTimerIfPlaying();
        UpdateUI();
    }

    private void InitializeGame()
    {
        if (playerController == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController     = player.GetComponent<PlayerController>();
                playerHealth         = player.GetComponent<PlayerHealth>();
                waterMagicController = player.GetComponent<WaterMagicController>();
            }
        }

        currentMoney           = startingMoney;
        totalKills             = 0;
        totalHeadshots         = 0;
        totalRoundsCompleted   = 0;
        totalFiresExtinguished = 0;
        timeAlive              = 0f;
        totalPerksActivated    = 0;

        InitializePerks();

        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    private void InitializePerks()
    {
        if (availablePerks.Count == 0)
        {
            availablePerks.Add(new PerkData("Health Up",   "Increases max health by 50%",    1000, PerkType.HealthBoost));
            availablePerks.Add(new PerkData("Speed Boost", "Increases movement speed by 20%",1500, PerkType.SpeedBoost));
            availablePerks.Add(new PerkData("Super Jump",  "Increases jump height by 30%",   1200, PerkType.JumpBoost));
            availablePerks.Add(new PerkData("Water Bend",  "Unlocks Water Bend ability",     2000, PerkType.WaterAbility1));
            availablePerks.Add(new PerkData("Water Tube",  "Unlocks Water Tube ability",     2500, PerkType.WaterAbility2));
            availablePerks.Add(new PerkData("Quick Draw",  "Increases water‑ball fire rate", 1200, PerkType.FireRateBoost));
        }
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void RegisterKill(bool isHeadshot = false)
    {
        totalKills++;
        AddMoney(moneyPerKill);
        if (isHeadshot)
        {
            totalHeadshots++;
            AddMoney(moneyPerHeadshot - moneyPerKill);
        }
    }

    public void RegisterFireExtinguished()
    {
        totalFiresExtinguished++;
        AddMoney(moneyPerFireExtinguished);
    }

    public void RegisterRoundCompleted(int roundNumber)
    {
        totalRoundsCompleted++;
        if (roundNumber > highestRoundReached)
            highestRoundReached = roundNumber;
    }

    private void UpdateTimerIfPlaying()
    {
        if (!isPaused)
            timeAlive += Time.deltaTime;
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
            if (isPaused) UpdateStatsPanel();
        }

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = isPaused;
    }

    public void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"${currentMoney}";
    }

    private void UpdateStatsPanel()
    {
        if (statsPanel == null) return;

        int minutes = Mathf.FloorToInt(timeAlive / 60);
        int seconds = Mathf.FloorToInt(timeAlive % 60);
        string timeString = $"{minutes:00}:{seconds:00}";

        if (statsKillsText != null) statsKillsText.text = $"Kills: {totalKills} (Headshots: {totalHeadshots})";
        if (statsRoundText != null) statsRoundText.text = $"Round: {highestRoundReached}";
        if (statsTimeText  != null) statsTimeText.text  = $"Time Alive: {timeString}";
        if (statsFiresText != null) statsFiresText.text = $"Fires Extinguished: {totalFiresExtinguished}";
        if (statsPerksText != null) statsPerksText.text = $"Perks Active: {activePerks.Count}/{maxPerks}";
    }

    #region Perk System
    public bool BuyPerk(PerkData perk)
    {
        if (activePerks.Contains(perk) || activePerks.Count >= maxPerks)
            return false;

        if (SpendMoney(perk.cost))
        {
            activePerks.Add(perk);
            ApplyPerkEffect(perk);
            totalPerksActivated++;
            return true;
        }
        return false;
    }

    public void ApplyPerkEffect(PerkData perk)
    {
        switch (perk.type)
        {
            case PerkType.HealthBoost:
                playerHealth.maxHealth     *= 1.5f;
                playerHealth.CurrentHealth  = playerHealth.maxHealth;
                break;
            case PerkType.SpeedBoost:
                playerController.MoveSpeed   *= 1.2f;
                playerController.SprintSpeed *= 1.2f;
                break;
            case PerkType.JumpBoost:
                playerController.JumpHeight *= 1.3f;
                break;
            case PerkType.WaterAbility1:
                waterMagicController.unlockedWaterBend = true;
                break;
            case PerkType.WaterAbility2:
                waterMagicController.unlockedWaterTube = true;
                break;
            case PerkType.FireRateBoost:
                waterMagicController.WaterBallFireRate *= 0.8f;
                break;
        }
    }
    #endregion

    public bool HasPerk(PerkType type)
    {
        return activePerks.Any(p => p.type == type);
    }
}

[System.Serializable]
public class PerkData
{
    public string   name;
    public string   description;
    public int      cost;
    public PerkType type;

    public PerkData(string _name, string _description, int _cost, PerkType _type)
    {
        name        = _name;
        description = _description;
        cost        = _cost;
        type        = _type;
    }
}

public enum PerkType
{
    HealthBoost,
    SpeedBoost,
    JumpBoost,
    WaterAbility1,
    WaterAbility2,
    FireRateBoost
}
