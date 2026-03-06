using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonFeedbackHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject glowEffect;

    [SerializeField] private AudioClip hoverSound; 
    [SerializeField] private AudioClip clickSound; 
    [SerializeField] private float cooldownTime = 0.4f;
    private float _nextPlayTime;
    void Awake()
    {
        if (glowEffect != null)
            glowEffect.SetActive(false);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
        {
            SoundManager.PlayNonDiegetic(clickSound);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (glowEffect != null) glowEffect.SetActive(true);

        if (Time.time >= _nextPlayTime)
        {
            OnHoverSound();
            _nextPlayTime = Time.time + cooldownTime;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glowEffect != null) glowEffect.SetActive(false);
    }

    void OnHoverSound()
    {
        if (hoverSound != null)
        {
            SoundManager.PlayNonDiegetic(hoverSound);
        }
    }
}