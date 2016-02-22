using UnityEngine;
using UnityEngine.Serialization; // While we're changing around variables...
using System.Collections;

public class NetworkedObserverSMMouseLook : StateMachineBehaviour {
    [FormerlySerializedAs("_aim")]
    public bool m_aim = false; // Should we be considered aiming for this state?
    public float m_minPitchThreshold = -30; // Minimum allowed pitch for mouselook
    public float m_maxPitchThreshold = 30; // Maximum allowed pitch for mouselook
    public float m_progress = 0; // Tracks progress through an animation track
    private int m_slot = -1; // Which inventory slot is selected?
    private NetworkedRouterOfComponentsForObserver m_components;  // This object is our router to find the other components we need to interact with.
    private Transform m_head; // Populated through the m_components router.
    private Transform m_leftHand; // Populated through the m_components router.
    private Transform m_rightHand; // Populated through the m_components router.
    private Transform m_chest; // Populated through the m_components router.
    private Transform m_spine; // Populated through the m_components router.
    // Rotations of our components
    private Quaternion m_chestDeltaRotation; // The delta rotation of the chest component.
    private Quaternion m_spineDeltaRotation; // The delta rotation of the spine component.
    // private Quaternion m_targetRotation;  // The target rotation of the head?


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state.
    override public void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        m_components = animator.GetComponent<NetworkedRouterOfComponentsForObserver> (); // Query the ComponentList in the gameObject containing the Animator.
        m_head = animator.GetBoneTransform (HumanBodyBones.Head); // Populate the Head component.
        m_spine = animator.GetBoneTransform(HumanBodyBones.Spine); // Populate the Spine component.
        m_chest = animator.GetBoneTransform(HumanBodyBones.Chest); // Populate the Chest component.
        m_leftHand = animator.GetBoneTransform (HumanBodyBones.LeftHand); // Populate the Left Hand (off hand) component.
        m_rightHand = animator.GetBoneTransform (HumanBodyBones.RightHand); // Populate the Right Hand (gun hand) component.
        m_slot = m_components.inventory.m_currentSlot; // Populate from the current inventory slot of PlayerPrefab.
        UpdateStateProgress(animator, stateInfo, layerIndex);
    }

    // OnStateExit is called when a outgoing transition starts and the state machine will run this before the new OnStateEntry.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        m_progress = 0; // Set animation progress counter to zero.
        UpdateBodyRotations(animator); // Force the body rotations to update.
    }

    // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
    override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        UpdateStateProgress(animator, stateInfo, layerIndex);
        UpdateBodyRotations(animator);
        if (m_aim)
        { // Are we aiming? If so, we should update the aim targets...
            UpdateAim(animator);
        }
    }

    // Updates the progress counter of this animation state.
    void UpdateStateProgress (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        AnimatorTransitionInfo info = animator.GetAnimatorTransitionInfo (layerIndex);

        if (animator.GetNextAnimatorStateInfo(layerIndex).GetHashCode() == stateInfo.GetHashCode()) { //Entering the state
            m_progress = info.normalizedTime; // Set progress of the animation to the time taken so far.
        } else if(animator.GetCurrentAnimatorStateInfo(layerIndex).GetHashCode() == stateInfo.GetHashCode()){ //Exiting the state
            m_progress = (1 - info.normalizedTime); // Set the final progress of the animation.
        }

    }

    // Update the body rotations.
    void UpdateBodyRotations (Animator animator) {
        Quaternion originalSpineRot = m_spine.rotation;
        Quaternion originalChestRot = m_chest.rotation;
        Quaternion originalHeadRot = m_head.rotation;
        float pitch = m_components.networkPawn.m_observerRotation.eulerAngles.x;
        if (pitch > 180)
            pitch -= 360;
        pitch *= -1;

        // Calculate new target rotation
        Quaternion l_targetRotation = Quaternion.Euler (animator.transform.eulerAngles.x - pitch, animator.transform.eulerAngles.y, animator.transform.eulerAngles.z);
        //Quaternion l_targetRotation = Quaternion.Euler(animator.transform.eulerAngles.x - pitch, animator.transform.eulerAngles.y, animator.transform.eulerAngles.z);

        // Calculate new target body rotation
        Quaternion l_targetBodyRotation = Quaternion.Euler(animator.transform.eulerAngles.x - (pitch - Mathf.Clamp(pitch, m_minPitchThreshold, m_maxPitchThreshold)), animator.transform.eulerAngles.y, animator.transform.eulerAngles.z);
        //Quaternion l_targetBodyRotation = Quaternion.Euler(animator.transform.eulerAngles.x - pitch, animator.transform.eulerAngles.y, animator.transform.eulerAngles.z); // - (pitch - Mathf.Clamp(pitch, m_minPitchThreshold, m_maxPitchThreshold)));

        // Calculate new deltas
        Quaternion delta = Quaternion.Inverse (m_head.rotation) * l_targetBodyRotation;
        //Quaternion delta = m_head.rotation * l_targetBodyRotation;
        m_chestDeltaRotation = Quaternion.Slerp (Quaternion.identity, delta, 0.3f);
        m_spineDeltaRotation = Quaternion.Slerp (Quaternion.identity, delta, 0.3f);

        // Calculate new local rotations from the deltas
        m_spine.localRotation *= m_spineDeltaRotation;
        m_chest.localRotation *= m_chestDeltaRotation;

        // Set new rotations
        m_spine.rotation = Quaternion.Slerp (originalSpineRot, m_spine.rotation, m_progress);
        m_chest.rotation =  Quaternion.Slerp (originalChestRot, m_chest.rotation, m_progress);
        m_head.rotation = Quaternion.Slerp (originalHeadRot, l_targetRotation, m_progress);

        // Assign new rotations to the bodycontroller.
        m_components.bodyController.SetTargetRotations (m_head.rotation, m_chest.rotation, m_spine.rotation);
    }

    // Update the aim target.
    void UpdateAim (Animator animator) {
        // This is only called if we're actually supposed to be aiming.

        // Need to discard previously made rotation in order to get the correct final position of the hands around a weapon.
        m_spine.localRotation *= Quaternion.Inverse(m_spineDeltaRotation);
        m_chest.localRotation *= Quaternion.Inverse(m_chestDeltaRotation);

        // Aiming.
        Vector3 originalCamPos = m_components.fpcamera.transform.position;
        Quaternion originalCamRot = m_components.fpcamera.transform.rotation;
        m_components.fpcamera.transform.position = m_components.inventory.m_availableItems [m_components.inventory.m_slots [m_slot] ].m_Aimpoint.position;
        m_components.fpcamera.transform.rotation = m_components.inventory.m_availableItems [m_components.inventory.m_slots [m_slot] ].m_Aimpoint.rotation;

        // Translating chest position and rotation to camera space.
        Vector3 localChestPosition = m_components.fpcamera.transform.InverseTransformPoint (m_chest.position);
        Vector3 localChestUp = m_components.fpcamera.transform.InverseTransformDirection (m_chest.up);
        Vector3 localChestForward = m_components.fpcamera.transform.InverseTransformDirection (m_chest.forward);

        // Reverting to original camera position and rotation.
        m_components.fpcamera.transform.position = originalCamPos;
        m_components.fpcamera.transform.rotation = originalCamRot;

        // Setting chest to target position.
        Vector3 targetChestPos = m_components.fpcamera.transform.TransformPoint (localChestPosition);
        Vector3 targetChestUp = m_components.fpcamera.transform.TransformDirection (localChestUp);
        Vector3 targetChestForward = m_components.fpcamera.transform.TransformDirection (localChestForward);

        Vector3 originalChestPos = m_chest.position;
        Quaternion originalChestRot = m_chest.rotation;
        m_chest.position = Vector3.Lerp (m_chest.position, targetChestPos, m_progress);
        m_chest.rotation = Quaternion.Slerp (m_chest.rotation, Quaternion.LookRotation (targetChestForward, targetChestUp), m_progress);
        
        // Setting IK targets.
        Vector3 leftHandPos = m_leftHand.position;
        Vector3 rightHandPos = m_rightHand.position;

        Quaternion leftHandRot = m_leftHand.rotation;
        Quaternion rightHandRot = m_rightHand.rotation;
        
        // Reset of chest position and rotation.
        m_chest.position = originalChestPos;
        m_chest.rotation = originalChestRot;
        
        // Do Inverse Kinematics.
        animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, m_progress);
        animator.SetIKPositionWeight (AvatarIKGoal.RightHand, m_progress);

        animator.SetIKRotationWeight (AvatarIKGoal.LeftHand, m_progress);
        animator.SetIKRotationWeight (AvatarIKGoal.RightHand, m_progress);
        
        animator.SetIKPosition (AvatarIKGoal.LeftHand, leftHandPos);
        animator.SetIKPosition (AvatarIKGoal.RightHand, rightHandPos);

        animator.SetIKRotation (AvatarIKGoal.LeftHand, leftHandRot * Quaternion.Euler(0, -90, 0) );
        animator.SetIKRotation (AvatarIKGoal.RightHand, rightHandRot * Quaternion.Euler(0, 90, 0) );
    }
}
