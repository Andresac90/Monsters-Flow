using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public float CurrentHealth {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    [Header("UI References")]
    public Image healthBar;
    public Image damageOverlay;
    public GameObject deathUIPanel;         
    public Camera playerCamera;        
    public Camera deathCamera;         

    [Header("Death UI Buttons")]
    public Button restartButton;       
    public Button mainMenuButton;      
    public Button quitButton;          

    [Header("Recovery")]
    public float healthRegenDelay = 5f;
    public float healthRegenRate = 10f;
    public bool canRegenHealth = true;


    private float lastDamageTime;
    private bool isRegenerating = false;

    [SerializeField] private Image  crosshair;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (deathUIPanel != null)    deathUIPanel.SetActive(false);
        if (deathCamera != null) deathCamera.enabled = false;

        // hook up death‐screen buttons
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (canRegenHealth && currentHealth < maxHealth && Time.time > lastDamageTime + healthRegenDelay && !isRegenerating)
            StartCoroutine(RegenerateHealth());

        if (damageOverlay != null && damageOverlay.color.a > 0)
        {
            Color overlayColor = damageOverlay.color;
            overlayColor.a -= Time.deltaTime * 0.5f;
            damageOverlay.color = overlayColor;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        lastDamageTime = Time.time;

        if (damageOverlay != null)
        {
            Color overlayColor = damageOverlay.color;
            overlayColor.a = 0.6f;
            damageOverlay.color = overlayColor;
        }

        if (currentHealth <= 0)
            Die();

        UpdateHealthUI();
    }

    private IEnumerator RegenerateHealth()
    {
        isRegenerating = true;
        while (currentHealth < maxHealth)
        {
            if (Time.time < lastDamageTime + healthRegenDelay)
            {
                isRegenerating = false;
                yield break;
            }

            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            UpdateHealthUI();
            yield return null;
        }
        isRegenerating = false;
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    private void Die()
    {
        Debug.Log("Player died!");

        if (damageOverlay != null)
            damageOverlay.gameObject.SetActive(false);
        if (crosshair != null)
            crosshair.gameObject.SetActive(false);

        if (playerCamera != null) playerCamera.enabled = false;
        if (deathCamera   != null) deathCamera.enabled = true;
        if (deathUIPanel       != null) deathUIPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
