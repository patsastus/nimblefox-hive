using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    private AudioSource audioSource;
    private AudioSource sfxSource;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    // Just use whatever is in the Audio Source
    public void Play()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void SwitchBGM(AudioClip newBGM)
    {
        if (audioSource != null && newBGM != null)
        {
            audioSource.Stop();
            audioSource.clip = newBGM;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}