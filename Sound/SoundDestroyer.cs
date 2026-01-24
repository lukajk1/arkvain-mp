using System.Collections;
using UnityEngine;

public class SoundDestroyer : MonoBehaviour
{
    private AudioSource audioSource;
    private Coroutine returnCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayAndReturn()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
        returnCoroutine = StartCoroutine(ReturnToPoolAfterClip());
    }

    private IEnumerator ReturnToPoolAfterClip()
    {
        float clipLength = audioSource.clip.length;
        yield return new WaitForSeconds(clipLength + 1f);

        SoundManager.ReturnToPool(gameObject);
        returnCoroutine = null;
    }

    public void OnReturnedToPool()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
}