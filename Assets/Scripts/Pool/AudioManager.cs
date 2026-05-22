using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip ballCollisionSound;
    [SerializeField] private AudioClip shotSound;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource interruptSource;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Collision Sound Control")]
    [SerializeField] private float ballCollisionCooldown = 0.15f;
    [SerializeField] private float minimumCollisionVelocity = 0.2f;

    private readonly Dictionary<string, float> recentBallCollisions = new Dictionary<string, float>();

    private Coroutine currentInterruptRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (interruptSource == null)
        {
            interruptSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        interruptSource.loop = false;
        interruptSource.playOnAwake = false;
        interruptSource.volume = sfxVolume;
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Tried to play music, but no clip was assigned.");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.time = 0f;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayShotSound()
    {
        PlayInterruptSound(shotSound, true);
    }

    public void PlayBallCollisionSound(GameObject ballA, GameObject ballB, float relativeVelocity)
    {
        if (ballCollisionSound == null)
        {
            return;
        }

        if (relativeVelocity < minimumCollisionVelocity)
        {
            return;
        }

        if (ballA == null || ballB == null)
        {
            return;
        }

        // Prevent both balls from playing the same collision.
        int idA = ballA.GetInstanceID();
        int idB = ballB.GetInstanceID();

        int smallerId = Mathf.Min(idA, idB);
        int largerId = Mathf.Max(idA, idB);

        string collisionKey = smallerId + "_" + largerId;

        if (recentBallCollisions.TryGetValue(collisionKey, out float lastPlayTime))
        {
            if (Time.time - lastPlayTime < ballCollisionCooldown)
            {
                return;
            }
        }

        recentBallCollisions[collisionKey] = Time.time;

        // Collision sounds are low priority, so do not restart a shot sound.
        PlayInterruptSound(ballCollisionSound, false);
    }

    private void PlayInterruptSound(AudioClip clip, bool forceRestart)
    {
        if (clip == null)
        {
            Debug.LogWarning("Tried to play interrupt sound, but no clip was assigned.");
            return;
        }

        if (interruptSource.isPlaying)
        {
            if (!forceRestart)
            {
                return;
            }

            interruptSource.Stop();

            if (currentInterruptRoutine != null)
            {
                StopCoroutine(currentInterruptRoutine);
            }
        }

        currentInterruptRoutine = StartCoroutine(PlayInterruptRoutine(clip));
    }

    private IEnumerator PlayInterruptRoutine(AudioClip clip)
    {
        bool musicWasPlaying = musicSource.isPlaying;
        float savedMusicTime = musicSource.time;

        if (musicWasPlaying)
        {
            musicSource.Pause();
        }

        interruptSource.clip = clip;
        interruptSource.volume = sfxVolume;
        interruptSource.Play();

        yield return new WaitWhile(() => interruptSource.isPlaying);

        if (musicWasPlaying && musicSource.clip != null)
        {
            musicSource.time = savedMusicTime;
            musicSource.Play();
        }

        currentInterruptRoutine = null;
    }
}