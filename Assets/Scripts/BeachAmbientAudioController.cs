using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BeachAmbientAudioController : MonoBehaviour
{
    private const string BeachSceneName = "01_Beach_StartMenu";
    private const string AudioResourcePath = "Audio/BeachAmbience";
    private const float TargetVolume = 0.65f;
    private const float FadeInDuration = 1.5f;

    private AudioSource ambientSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterForBeachScene()
    {
        SceneManager.sceneLoaded -= CreateForBeachScene;
        SceneManager.sceneLoaded += CreateForBeachScene;
    }

    private static void CreateForBeachScene(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name != BeachSceneName || FindFirstObjectByType<BeachAmbientAudioController>() != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("Beach Ambient Audio");
        SceneManager.MoveGameObjectToScene(audioObject, scene);
        audioObject.AddComponent<BeachAmbientAudioController>();
    }

    private void Awake()
    {
        AudioClip beachAmbience = Resources.Load<AudioClip>(AudioResourcePath);
        if (beachAmbience == null)
        {
            Debug.LogWarning("Beach ambience audio could not be loaded from Resources/Audio/BeachAmbience.");
            return;
        }

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = beachAmbience;
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 0f;
        ambientSource.Play();

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < FadeInDuration && ambientSource != null)
        {
            timer += Time.unscaledDeltaTime;
            ambientSource.volume = Mathf.Lerp(0f, TargetVolume, timer / FadeInDuration);
            yield return null;
        }

        if (ambientSource != null)
        {
            ambientSource.volume = TargetVolume;
        }
    }
}
