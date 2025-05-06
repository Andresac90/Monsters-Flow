using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Image healthBar;               
    public GameObject healthBarCanvas;    
    public TextMeshProUGUI damageText; // Floating damage text

    [Header("Element Weakness")]
    public float waterDamageMultiplier = 1.5f;

    [Header("Physics")]
    public float knockbackResistance = 0.5f;
    public bool applyKnockback = true;

    [Tooltip("Time in seconds to disable the NavMeshAgent during knockback.")]
    [SerializeField] private float knockbackDisableTime = 0.5f;

    [Header("Rewards")]
    public int moneyValue = 50;
    public bool wasHeadshot = false;

    private Rigidbody rb;
    private EnemyAI enemyAI;
    private Coroutine damageTextFade;
    private bool isDead = false; // Flag to prevent multiple deaths

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        enemyAI = GetComponent<EnemyAI>();

        UpdateHealthUI();
    }

    public void TakeDamage(float damage, bool isHeadshot = false)
    {
        if (isDead) return;

        float calculatedDamage = damage * waterDamageMultiplier;

        if (isHeadshot)
        {
            calculatedDamage *= 2f;
            wasHeadshot = true;
        }

        currentHealth -= calculatedDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();
        ShowDamageText(calculatedDamage);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }
    }

    private void ShowDamageText(float amount)
    {
        if (damageText != null)
        {
            damageText.text = "-" + amount.ToString("F0");
            damageText.color = new Color(1f, 1f, 1f, 1f); // Reset color to opaque
            damageText.gameObject.SetActive(true);

            // Reset text position and apply a random x offset
            RectTransform rt = damageText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Random.Range(20f, 50f), 0f);

            if (damageTextFade != null)
                StopCoroutine(damageTextFade);

            damageTextFade = StartCoroutine(FadeDamageText(rt));
        }
    }

    private IEnumerator FadeDamageText(RectTransform rt)
    {
        float duration = 1.5f;
        float t = 0f;
        Color startColor = damageText.color;
        Vector2 startPos = rt.anchoredPosition;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duration);
            float verticalOffset = Mathf.Lerp(0f, 40f, t / duration);

            damageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            rt.anchoredPosition = startPos + new Vector2(0f, verticalOffset);

            yield return null;
        }

        damageText.gameObject.SetActive(false);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Hide the enemy's health UI
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(false);
        }

        // Register kill with the GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(wasHeadshot);
        }

        // Call the enemy AI's death method if available
        if (enemyAI != null)
        {
            enemyAI.Die();
        }
        else
        {
            Destroy(gameObject, 5f);
        }
    }

    public void ApplyKnockback(Vector3 force)
    {
        if (!applyKnockback || rb == null || isDead) return;
        StartCoroutine(DisableAgentAndKnockbackRoutine(force));
    }

    private IEnumerator DisableAgentAndKnockbackRoutine(Vector3 force)
    {
        // Disable the NavMeshAgent if it exists
        if (enemyAI != null && enemyAI.Agent != null)
        {
            enemyAI.Agent.enabled = false;
        }

        // Adjust for knockbackResistance
        Vector3 adjustedForce = force * (1f - knockbackResistance);
        rb.AddForce(adjustedForce, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDisableTime);

        // If still alive, re-enable the agent
        if (enemyAI != null && !enemyAI.IsDead && enemyAI.Agent != null)
        {
            enemyAI.Agent.enabled = true;
        }
    }

    public void SetStats(float health, float damageMultiplier = 1.5f)
    {
        maxHealth = health;
        currentHealth = health;
        waterDamageMultiplier = damageMultiplier;
        UpdateHealthUI();
    }
}
