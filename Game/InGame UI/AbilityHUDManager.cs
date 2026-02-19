using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityHUDManager : MonoBehaviour
{
    public static AbilityHUDManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _abilityCooldownTimer;
    [SerializeField] private TextMeshProUGUI _abilityBindingName;
    [SerializeField] private GameObject _abilityCooldownParentToHide;
    [SerializeField] private Image _abilityCooldown;

    [Header("Cooldown Style")]
    [SerializeField] private Color _abilityCooldownColor = Color.gray;
    [SerializeField] private AudioClip _abilityReadySound;

    [Header("Ready Animation")]
    [SerializeField] private Image _abilityReadyAnimImage;
    [SerializeField] private float _abilityReadyAnimPeakHeight = 80f;
    [SerializeField] private float _abilityReadyAnimDuration = 0.4f;

    private bool _abilityOnCooldown;
    private int _animTweenId = -1;
    private float _animInitialHeight;
    private Color _abilityInitialColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_abilityCooldown != null)
            _abilityInitialColor = _abilityCooldown.color;

        if (_abilityReadyAnimImage != null)
            _abilityReadyAnimImage.enabled = false;
    }

    public void SetAbilityCooldown(float normalizedCooldown, float remainingSeconds)
    {
        bool onCooldown = remainingSeconds > 0f;

        if (_abilityCooldown != null)
        {
            _abilityCooldown.fillAmount = normalizedCooldown;
            _abilityCooldown.color = onCooldown ? _abilityCooldownColor : _abilityInitialColor;
        }

        if (_abilityCooldownTimer != null)
            _abilityCooldownTimer.text = onCooldown ? remainingSeconds.ToString("F1") : string.Empty;

        if (_abilityOnCooldown && !onCooldown)
        {
            if (_abilityReadySound != null)
                SoundManager.PlayNonDiegetic(_abilityReadySound);

            PlayReadyAnimation();
        }

        _abilityOnCooldown = onCooldown;
    }

    public void HideAbilityUI()
    {
        if (_abilityCooldownParentToHide != null)
            _abilityCooldownParentToHide.SetActive(false);
    }

    public void SetAbilityBindingName(string name)
    {
        if (_abilityBindingName != null)
            _abilityBindingName.text = name;
    }

    private void PlayReadyAnimation()
    {
        if (_abilityReadyAnimImage == null) return;

        if (_animTweenId != -1)
        {
            LeanTween.cancel(_animTweenId);
            _animTweenId = -1;
        }

        RectTransform rt = _abilityReadyAnimImage.rectTransform;
        _abilityReadyAnimImage.enabled = true;
        _animInitialHeight = rt.sizeDelta.y;

        _animTweenId = LeanTween.value(gameObject, _animInitialHeight, _abilityReadyAnimPeakHeight, _abilityReadyAnimDuration)
            .setEase(LeanTweenType.easeInQuad)
            .setOnUpdate((float val) =>
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, val);
            })
            .setOnComplete(() =>
            {
                _abilityReadyAnimImage.enabled = false;
                _animTweenId = -1;
            }).id;
    }
}
