using UnityEngine;
using System.Collections;

// This component is placed next to the animator component
// so that the other scripts can be discovered through this central route.
[RequireComponent ( typeof (Animator) ) ]
public class NetworkedRouterOfComponentsForObserver : MonoBehaviour {
    [Header("NetworkBehaviors")]
    public NetworkedObserver networkPawn; // This pawn's NetworkPawn
    public NetworkedObserverInventory inventory; // This pawn's Inventory
    public NetworkedObserverSkinController skin; // This pawn's Skin
    public NetworkedObserverSoundController sounds; // This pawn's Sounds
    [Header("MonoBehaviors")]
    public Camera fpcamera; // This pawn's fpcamera
    public AudioListener audioListener; // The audioListener attached to the fpcamera
    public NetworkedObserverBodyController bodyController; // This pawn's BodyController
    public CharacterController characterController; // This pawn's CharacterController
    public Animator animator; // This pawn's Animator (Which should be this gameObject)

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    void Awake() {
        Debug.Log(gameObject.name + "|ComponentsList::Awake: ComponentsList has Awakened.");
        // Grab references to NetworkBehaviors, which MUST be in the root GameObject next to the NetworkIdentity.
        networkPawn = GetComponentInParent<NetworkedObserver>(); // Grab a reference to the NetworkPawn.
        inventory = GetComponentInParent<NetworkedObserverInventory>(); // Grab a reference to the Inventory.
        skin = GetComponent<NetworkedObserverSkinController>(); // Grab a reference to the SkinController.
        sounds = GetComponent<NetworkedObserverSoundController>(); // Grab a reference to the SoundController.
        // Grab references to MonoBehaviors and useful GameObjects in this object's hierarchy.
        fpcamera = GetComponentInChildren<Camera>(); // We expect the First Person camera to be attached to the Head.
        audioListener = GetComponentInChildren<AudioListener>(); // Grab a reference to the AudioListener.
        bodyController = GetComponentInParent<NetworkedObserverBodyController>(); // Grab a reference to the BodyController.
        characterController = GetComponentInParent<CharacterController>(); // Grab a reference to the CharacterController.
        animator = GetComponent<Animator>(); // Grab a reference to the Animator.
        Debug.Log(gameObject.name + "|ComponentsList::Awake: ComponentsList has finished gathering references.");
    }

    // Use this for initialization when object is spawned into the worldspace
    void Start() {
        if (networkPawn.isLocalPlayer && !fpcamera.enabled) {
            // Things we need to enable when the Pawn begins Existance.
            fpcamera.enabled = true;
            audioListener.enabled = true;
        }
    }
}
