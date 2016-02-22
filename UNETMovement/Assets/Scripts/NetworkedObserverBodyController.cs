using UnityEngine;
using System.Collections;

public class NetworkedObserverBodyController : MonoBehaviour {
    public float m_aimPower;
    private NetworkedRouterOfComponentsForObserver router;
    private Quaternion m_targetHeadRotation;
    private Quaternion m_targetChestRotation;
    private Quaternion m_targetSpineRotation;
    private Transform m_head;
    private Transform m_spine;
    private Transform m_chest;
    private bool m_updateRotations = false;

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    void Awake () {
        Debug.Log(gameObject.name + "|NetworkedObserverBodyController::Awake: NetworkedObserverBodyController has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
        m_head = router.animator.GetBoneTransform (HumanBodyBones.Head); // Pull the reference to the head out of the router
        m_spine = router.animator.GetBoneTransform (HumanBodyBones.Spine); // Pull the reference to the spine out of the router
        m_chest = router.animator.GetBoneTransform (HumanBodyBones.Chest); // Pull the reference to the chest out of the router
    }

    void LateUpdate() {
        if (m_updateRotations) {
            m_spine.rotation = m_targetSpineRotation;
            m_chest.rotation = m_targetChestRotation;
            m_head.rotation = m_targetHeadRotation;
            m_updateRotations = false;
        }
    }

    public void SetTargetRotations(Quaternion targetHeadRotation, Quaternion targetChestRotation,Quaternion targetSpineRotation){
        m_targetHeadRotation = targetHeadRotation;
        m_targetChestRotation = targetChestRotation;
        m_targetSpineRotation = targetSpineRotation;
        m_updateRotations = true;
    }
}
