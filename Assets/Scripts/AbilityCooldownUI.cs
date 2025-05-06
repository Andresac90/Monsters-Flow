using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class AbilityCooldownUI : MonoBehaviour
{
    public enum Ability { WaterBend, WaterTube }
    public Ability ability;

    public Image cooldownMask;

    private WaterMagicController _wmc;
    private CanvasGroup         _cg;

    void Awake()
    {
        _wmc = FindObjectOfType<WaterMagicController>();
        _cg  = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (_wmc == null) return;

        // only show if the perk is unlocked
        bool shouldShow = (ability == Ability.WaterBend && _wmc.unlockedWaterBend)
                       || (ability == Ability.WaterTube && _wmc.unlockedWaterTube);

        _cg.alpha          = shouldShow ? 1f : 0f;
        _cg.blocksRaycasts = shouldShow;

        if (!shouldShow || cooldownMask == null) return;

        float rem = 0, max = 1;
        switch (ability)
        {
            case Ability.WaterBend:
                rem = _wmc.WaterBendCooldownRemaining;
                max = _wmc.WaterBendCooldownMax;
                break;
            case Ability.WaterTube:
                rem = _wmc.WaterTubeCooldownRemaining;
                max = _wmc.WaterTubeCooldownMax;
                break;
        }

        cooldownMask.fillAmount = Mathf.Clamp01(rem / max);
    }
}
