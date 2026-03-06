using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonVFXHandler : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip hoverSound; 
    [SerializeField] private float cooldownTime = 0.5f;
    private float _nextPlayTime;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Time.time >= _nextPlayTime)
        {
            OnHoverSound();
            _nextPlayTime = Time.time + cooldownTime;
        }
    }

    void OnHoverSound()
    {
        if (hoverSound != null)
        {
            SoundManager.PlayNonDiegetic(hoverSound);
        }
    }
}