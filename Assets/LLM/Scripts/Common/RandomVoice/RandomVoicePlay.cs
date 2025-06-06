using System.Collections;
using UnityEngine;

public class RandomVoicePlay : MonoBehaviour
{
    public AudioClip[] audioClips;
    public AudioSource audioSource;
    public float playDelay = 1f; // Delay before playing the audio clip

    private void Awake()
    {
        audioSource = GetComponentInParent<AudioSource>();
    }

    public void PlayRandomAudio()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("The audio source varible is not assigned.");
            return;
        }



        int randomID = Random.Range(0, audioClips.Length);
        var audioClip = audioClips[randomID];
        if (audioClip == null)
        {
            Debug.LogWarning("The audio clip is invalid.");
            return;
        }

        audioSource.clip = audioClip;
        //audioSource.Play();

        StartCoroutine(DelayPlayClip(playDelay));
    }

    IEnumerator DelayPlayClip(float duration)
    {
        yield return new WaitForSeconds(duration);
        audioSource.Play();
    }
    
}
