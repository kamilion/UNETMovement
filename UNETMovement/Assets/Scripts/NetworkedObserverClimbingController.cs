using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkedObserverClimbingController : NetworkBehaviour
{
    // Discovered by the script automatically.
    [HideInInspector]
    public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    // Set these in the inspector.
    [Header("This observer's body mesh")]
    public GameObject m_BodyMesh;

    // These may be manipulated by other scripts.
    [Header("--Internal State--")]
    public Transform ladder;//current ladder 
    public bool _jump; //jump button press detection
    public bool _jumpOverObstacle;
    public bool canClimb;
    public bool canClimbOff;
    public bool onLadder; //player is on the ladder
    public bool canRotate = true;



    void Start()
    {
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
        // Grab the skinned mesh renderer attached to this object.
        //m_CurrentSkin = m_BodyMesh.GetComponent<SkinnedMeshRenderer>().material.mainTexture;
    }


    void TurnOffCollider()
    {
        router.characterController.enabled = false;
        canRotate = false;
    }
    void TurnOnCollider()
    {
        router.characterController.enabled = true;
        _jumpOverObstacle = false;
        canRotate = true;
    }

    //////////ON TRIGGER ENTER FUNCTION
    void OnTriggerEnter(Collider trigg)
    {
        if (trigg.gameObject.name == "ladder")
        {// if we've triggered a ladder
            canClimb = true; // tell player we can now climb
            ladder = trigg.transform.Find("ladderAligner"); // and triggered ladder is THE ladder we're going to climb on
        }
        if (trigg.gameObject.name == "ladderBottom")
        { // if we've triggered the bottom of the ladder
            canClimbOff = true; // tell player we can climb off
        }
        if (trigg.gameObject.name == "ladderTop")
        {
            if (onLadder)
            {
                router.animator.SetTrigger("AtLadderCrest");
            }
        }
    }
    //////////ON TRIGGER EXIT FUNCTION
    void OnTriggerExit(Collider trigg)
    {
        if (trigg.gameObject.name == "ladder")
        {   //if we've left ladder's trigger zone
            canClimb = false;
            onLadder = false;
            ladder = null; //zeroing ladder transform
            this.transform.parent = null; //deparenting player from ladder
        }
        if (trigg.gameObject.name == "ladderBottom")
        {
            canClimbOff = false;
        }
    }

}
