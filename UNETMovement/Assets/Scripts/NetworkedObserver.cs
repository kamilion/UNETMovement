
using UnityEngine;
using UnityEngine.UI;

// Define a player pawn as a subclass of NetworkedTimestepMovement

public class NetworkedObserver : NetworkedTimestepMovement {
    public GameObject m_pausedPanel; // We assign this reference to the paused panel in the canvas through the inspector. 
    public Transform m_observer; // We assign this reference through the inspector.
    public Quaternion m_observerRotation;
    public Vector3 m_observerRotationEuler;
    public float m_viewPitch;
    public float m_viewYaw;

    public float m_verticalMouseLookLimit = 170;
    public float m_snapDistance = 1;
    private float m_verticalSpeed = 0;
    public float m_jumpHeight = 10;
    private bool m_jump = false;

    // // // UnityEngine magic methods
    // Do not define Awake, Start, Update or FixedUpdate in subclasses of NetworkedTimestepMovement.

    // // // Parent class override methods
    public override void GetInputs (ref Inputs inputs) {
        inputs.sides = RoundToLargest( Input.GetAxis("Horizontal" ) );
        inputs.forward = RoundToLargest( Input.GetAxis("Vertical" ) );
        inputs.yaw = -Input.GetAxis( "Mouse Y" ) * m_mouseSense * Time.fixedDeltaTime / Time.deltaTime;
        inputs.pitch = Input.GetAxis( "Mouse X" ) * m_mouseSense * Time.fixedDeltaTime / Time.deltaTime;
        inputs.sprint = Input.GetButton( "Sprint" );
        inputs.crouch = Input.GetButton( "Crouch" );

        float verticalTarget = -1;
        if ( router.characterController.isGrounded ) {
            if( Input.GetButton( "Jump" ) ) {
                m_jump = true;
            }
            inputs.vertical = 0;
            verticalTarget = 0;
        }
        if ( m_jump ) {
            verticalTarget = 1;
            if( inputs.vertical >= 0.9f ) {
                m_jump = false;
            }
        }
        inputs.vertical = Mathf.Lerp( inputs.vertical, verticalTarget, 20 * Time.deltaTime );

        if( Input.GetKeyDown (KeyCode.Escape) )
        {
            if (m_pausedPanel != null) { m_pausedPanel.SetActive(true); } // Enable the paused panel
            // lock the controls
            InputsSetLock(true);
        }
    }

    // Move, Rotate, UpdatePosition, and UpdateRotation.
    public override Vector3 Move (Inputs inputs, Results current) {
        m_observer.position = current.position;
        float speed = 2;
        if (current.crouching) {
            speed = 1.5f;
        }
        if (current.sprinting) {
            speed = 3;
        }
        if (inputs.vertical > 0) {
            m_verticalSpeed = inputs.vertical * m_jumpHeight;
        } else {
            m_verticalSpeed = inputs.vertical * Physics.gravity.magnitude;
        }
        router.characterController.Move (m_observer.TransformDirection((Vector3.ClampMagnitude(new Vector3(inputs.sides,0,inputs.forward),1) * speed) + new Vector3(0,m_verticalSpeed,0) ) * Time.fixedDeltaTime);
        return m_observer.position;

    }

    public override Quaternion Rotate (Inputs inputs, Results current) {
        m_observer.rotation = current.rotation;
        float mHor = current.rotation.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
        float mVert = current.rotation.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;
        
        if (mVert > 180) mVert -= 360;
        mVert = Mathf.Clamp (mVert, -m_verticalMouseLookLimit * 0.5f, m_verticalMouseLookLimit * 0.5f);
        m_observer.rotation = Quaternion.Euler (0, mHor, 0);
        m_observerRotation = Quaternion.Euler (mVert, mHor, 0);
        // These three are to show the values in the inspector.
        m_observerRotationEuler = m_observerRotation.eulerAngles;
        m_viewPitch = mHor;
        m_viewYaw = mVert;
        return m_observerRotation;
    }

    public override void UpdatePosition (Vector3 newPosition) {
        if (Vector3.Distance (newPosition, m_observer.position) > m_snapDistance) {
            m_observer.position = newPosition;
        } else {
            router.characterController.Move (newPosition - m_observer.position);
        }

    }

    public override void UpdateRotation (Quaternion newRotation) {
        m_observer.rotation = Quaternion.Euler (0, newRotation.eulerAngles.y, 0);
        m_observerRotation = newRotation;
    }
}
