using UnityEngine;
//using UnityEngine.Networking;
using System.Collections;

public class NetworkedObserverSoundController : MonoBehaviour {
    // Discovered by the script automatically.
    private AudioSource m_AudioSource;
    // Set these in the inspector.
    [Header("Footstep sounds on hard surfaces")]
    public AudioClip[] m_FootstepSoundsGround;
    [Header("Footstep sounds on liquid surfaces")]
    public AudioClip[] m_FootstepSoundsWater;
    // These may be manipulated by other scripts.
    [Header("--Internal State--")]
    // Is the observer in liquid like water?
    public bool isInWater;

    void Start() {
        // Grab the audio source attached to this object.
        m_AudioSource = GetComponentInChildren<AudioSource>();
    }

    // Compatibility with LowPolyCharacters default events
    void footStep() { audioPlaySingleFootstep(); }

    // SOUND FUNCTIONS ARE CALLED FROM ANIMATION EVENTS, FOUND IN THE INSPECTOR.
    // Not in the Animator or Animation panel. Select an ANIMATION, then inspect.
    // See: http://docs.unity3d.com/Manual/AnimationEventsOnImportedClips.html
    void audioPlaySingleFootstep()
    {
        if (isInWater)
        {
            int n = UnityEngine.Random.Range(0, m_FootstepSoundsWater.Length);
            m_AudioSource.clip = m_FootstepSoundsWater[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSoundsWater[n] = m_FootstepSoundsWater[0];
            m_FootstepSoundsWater[0] = m_AudioSource.clip;
        }
        else {
            int n = UnityEngine.Random.Range(0, m_FootstepSoundsGround.Length);
            m_AudioSource.clip = m_FootstepSoundsGround[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSoundsGround[n] = m_FootstepSoundsGround[0];
            m_FootstepSoundsGround[0] = m_AudioSource.clip;
        }
    }
}
