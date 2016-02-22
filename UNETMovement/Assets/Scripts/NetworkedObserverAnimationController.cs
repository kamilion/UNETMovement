
using UnityEngine;
using System.Collections;

public class NetworkedObserverAnimationController : MonoBehaviour {
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    //public Vector3 m_lastPosition; // The last position we knew this pawn was at.
    private Vector3 m_velocity; // The velocity this pawn is traveling at. (Public'd so inspectable)

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    public void Awake() {
        Debug.Log(gameObject.name + "|NetworkedObserverAnimationController::Awake: NetworkedObserverAnimationController has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    // Use this for initialization when object is spawned into the worldspace
    //public void Start () {
    //    m_lastPosition = router.networkPawn.m_observer.position;
    //}
    
    // Update is called once per frame
    public void Update () {
        m_velocity = Vector3.Lerp (m_velocity, router.networkPawn.m_observer.InverseTransformDirection(router.characterController.velocity), 5 * Time.deltaTime);

        router.animator.SetFloat ("Speed", m_velocity.z);
        router.animator.SetFloat ("Strafe", m_velocity.x);
    }
}
