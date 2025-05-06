using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(AudioSource))]
public class PerkShop : MonoBehaviour
{
    [Header("Shop Settings")]
    [Tooltip("Drag in one of the GameManager.availablePerks elements here")]
    public PerkData availablePerk;

    [Header("Interaction")]
    public float     interactionRange   = 3f;
    public KeyCode   interactionKey     = KeyCode.F;
    public GameObject interactionPrompt;  // “Press F to Buy” popup

    [Header("UI Elements")]
    public GameObject uiContainer;        
    public TMP_Text   perkNameText;       
    public TMP_Text   perkDescriptionText;
    public TMP_Text   perkCostText;       

    [Header("Visual Effects")]
    public Light    shopLight;
    public Material activeMaterial;
    public Material inactiveMaterial;

    private Renderer shopRenderer;
    private Transform playerTransform;
    private bool      inRange;

    private void Start()
    {
        shopRenderer = GetComponent<Renderer>();

        // Hide everything initially
        interactionPrompt?.SetActive(false);
        uiContainer    ?.SetActive(false);

        RefreshVisuals();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool now = dist <= interactionRange;

        if (now != inRange)
        {
            inRange = now;

            interactionPrompt?.SetActive(inRange);
            uiContainer     ?.SetActive(inRange);

            if (inRange)
                PopulateUI();
        }

        if (inRange && Input.GetKeyDown(interactionKey))
            TryPurchase();
    }

    private void PopulateUI()
    {
        if (availablePerk == null) return;

        // fill the text
        perkNameText       .text = availablePerk.name;
        perkDescriptionText.text = availablePerk.description;
        perkCostText       .text = $"${availablePerk.cost}";

        // ensure they are active
        perkNameText.gameObject       .SetActive(true);
        perkDescriptionText.gameObject.SetActive(true);
        perkCostText.gameObject       .SetActive(true);
    }

    private void TryPurchase()
    {
        if (GameManager.Instance == null) return;

        bool ok = GameManager.Instance.BuyPerk(availablePerk);
        if (ok) StartCoroutine(PurchaseEffect());

        RefreshVisuals();
    }

    private IEnumerator PurchaseEffect()
    {
        GetComponent<AudioSource>()?.Play();
        if (shopLight == null) yield break;

        float original = shopLight.intensity;
        float peak     = original * 3f;
        float t        = 0f;

        // ramp up
        while (t < 1f)
        {
            shopLight.intensity = Mathf.Lerp(original, peak, t);
            t += Time.deltaTime * 2f;
            yield return null;
        }
        t = 0f;
        // ramp down
        while (t < 1f)
        {
            shopLight.intensity = Mathf.Lerp(peak, original, t);
            t += Time.deltaTime;
            yield return null;
        }
        shopLight.intensity = original;
    }

    private void RefreshVisuals()
    {
        bool canAfford = GameManager.Instance != null
                      && GameManager.Instance.currentMoney >= availablePerk.cost;

        if (shopRenderer != null && activeMaterial != null && inactiveMaterial != null)
            shopRenderer.material = canAfford ? activeMaterial : inactiveMaterial;

        if (shopLight != null)
            shopLight.color = canAfford ? Color.green : Color.red;
    }

    public void OnMoneyChanged()
    {
        RefreshVisuals();
    }
}
