using UnityEngine;

[RequireComponent(typeof(Animation))]
public class LegacyAnimationSpeedControl : MonoBehaviour
{
    [Header("Animation Speed Control")]
    [Tooltip("Set animation playback speed: 1 = normal, 0.5 = half speed, 0.2 = slow motion.")]
    [Range(0.1f, 3f)]
    public float playbackSpeed = 0.5f;

    private Animation _legacyAnimation;

    private void Awake()
    {
        // Get the legacy Animation component
        _legacyAnimation = GetComponent<Animation>();
    }

    private void Update()
    {
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        // Ensure the component and the default clip exist
        if (_legacyAnimation != null && _legacyAnimation.clip != null)
        {
            string clipName = _legacyAnimation.clip.name;
            
            // Check if the specific animation state exists
            if (_legacyAnimation[clipName] != null)
            {
                // Apply the playback speed to the animation state
                _legacyAnimation[clipName].speed = playbackSpeed;
            }
        }
    }
}