using UnityEngine;

public class RandomVoicePlay : MonoBehaviour
{
    public AudioClip[] audioClips;
    public AudioSource audioSource;

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
        audioSource.Play();
    }


    
}
