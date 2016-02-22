using UnityEngine;
using UnityEngine.Serialization; // While we're changing around variables...
using System.Collections;

public class NetworkedLocalParticleEffectAutoDestroy : MonoBehaviour {
    [FormerlySerializedAs("_particleSystem")]
    private ParticleSystem m_particleSystem;

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    void Awake () {
        m_particleSystem = GetComponent<ParticleSystem> ();
    }
    
    // Update is called once per frame
    void Update () {
        if (!m_particleSystem.IsAlive ()) {
            GameObject.Destroy(gameObject);
        }
    }
}
