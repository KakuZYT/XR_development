using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DescentAmbientAudioController : MonoBehaviour
{
    private const string SurfaceAudioPath = "Audio/SurfaceWaves";
    private const string UnderwaterAudioPath = "Audio/UnderwaterAmbience";

    [SerializeField, Range(0f, 1f)] private float surfaceVolume = 0.65f;
    [SerializeField, Range(0f, 1f)] private float underwaterVolume = 0.65f;

    private AudioSource surfaceSource;
    private AudioSource underwaterSource;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        AudioClip surfaceClip = Resources.Load<AudioClip>(SurfaceAudioPath);
        AudioClip underwaterClip = Resources.Load<AudioClip>(UnderwaterAudioPath);

        if (surfaceClip != null)
        {
            surfaceSource = CreateLoopingSource("Surface Waves", surfaceClip, surfaceVolume);
            surfaceSource.Play();
        }
        else
        {
            Debug.LogWarning("Surface wave audio could not be loaded from Resources/Audio/SurfaceWaves.");
        }

        if (underwaterClip != null)
        {
            underwaterSource = CreateLoopingSource("Underwater Ambience", underwaterClip, 0f);
            underwaterSource.Play();
        }
        else
        {
            Debug.LogWarning("Underwater audio could not be loaded from Resources/Audio/UnderwaterAmbience.");
        }
    }

    public void CrossfadeToUnderwater(float duration)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(CrossfadeRoutine(Mathf.Max(duration, 0.1f)));
    }

    private AudioSource CreateLoopingSource(string sourceName, AudioClip clip, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private IEnumerator CrossfadeRoutine(float duration)
    {
        float startingSurfaceVolume = surfaceSource == null ? 0f : surfaceSource.volume;
        float startingUnderwaterVolume = underwaterSource == null ? 0f : underwaterSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / duration));

            if (surfaceSource != null)
            {
                surfaceSource.volume = Mathf.Lerp(startingSurfaceVolume, 0f, t);
            }

            if (underwaterSource != null)
            {
                underwaterSource.volume = Mathf.Lerp(startingUnderwaterVolume, underwaterVolume, t);
            }

            yield return null;
        }

        if (surfaceSource != null)
        {
            surfaceSource.volume = 0f;
            surfaceSource.Stop();
        }

        if (underwaterSource != null)
        {
            underwaterSource.volume = underwaterVolume;
        }

        transitionCoroutine = null;
    }
}
