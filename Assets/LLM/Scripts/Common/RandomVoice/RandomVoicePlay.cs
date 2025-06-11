using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public class RandomVoicePlay : MonoBehaviour
{
    public AudioClip[] audioClips;
    public AudioSource audioSource;
    public float playDelay = 1f; // Delay before playing the audio clip

    private void Awake()
    {
        audioSource = GetComponentInParent<AudioSource>();
    }

    public async void PlayRandomAudio()
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

        while (audioSource.isPlaying)
        {
            Debug.Log("AudioSource is currently playing. Waiting...");
            await Task.Delay(2000); // 不阻塞主线程
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
