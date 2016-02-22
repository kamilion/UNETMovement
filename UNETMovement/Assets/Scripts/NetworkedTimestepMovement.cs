
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

//Server-authoritative movement with Client-side prediction and reconciliation
//Author:gennadiy.shvetsov@gmail.com

//QoS channels used:
//channel #0: Reliable Sequenced
//channel #1: Unreliable Sequenced
[NetworkSettings(channel=1,sendInterval=0.05f)]
public class NetworkedTimestepMovement : NetworkBehaviour {
    //This struct would be used to collect player inputs locally
    public struct Inputs {
        public float forward;
        public float sides;
        public float yaw;
        public float vertical;
        public float pitch;
        public bool sprint;
        public bool crouch;

        public float timeStamp;
    }

    //This struct would be used to collect player inputs from across the network
    public struct SyncInputs {
        public sbyte forward;
        public sbyte sides;
        public float yaw;
        public sbyte vertical;
        public float pitch;
        public bool sprint;
        public bool crouch;
        
        public float timeStamp;
    }

    //This struct would be used to collect results of Move and Rotate functions locally
    public struct Results {
        public Quaternion rotation;
        public Vector3 position;
        public bool sprinting;
        public bool crouching;

        public float timeStamp;
    }

    //This struct would be used to collect results of Move and Rotate functions locally
    public struct SyncResults {
        public ushort yaw;
        public ushort pitch;
        public Vector3 position;
        public bool sprinting;
        public bool crouching;

        public float timeStamp;
    }

    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.

    private Inputs m_inputs;

    //Synced from server to all clients
    [SyncVar(hook="RecieveResults")]
    private SyncResults syncResults;

    private Results m_results;

    // Which cursor lock mode?
    public CursorLockMode m_CursorLockMode;
    //Mouse sensativity
    public float m_mouseSense = 100;
    //Object should ignore control inputs?
    public bool m_controlsLocked = false;
    //Owner client and server would store it's inputs in this list
    private List<Inputs> m_inputsList = new List<Inputs>();
    //This list stores results of movement and rotation. Needed for non-owner client interpolation
    private List<Results> m_resultsList = new List<Results>();

    
    //Interpolation related variables
    private bool m_playData = false;

    private float m_dataStep = 0; // holds Time.fixedDeltaTime passed between network events?
    private float m_lastTimeStamp = 0; // Used for discarding out of sequence results

    private bool m_jumping = false; // Used to hold the input state of jump button
    private Vector3 m_startPosition; // The position we started in
    private Quaternion m_startRotation; // The rotation we started in

    private float m_step = 0; // holds Time.fixedDeltaTime passed between some event processing?

    // // // UnityEngine magic methods
    // Do not define Awake, Start, Update or FixedUpdate in subclasses.
    // Use this for initialization when object is allocated in memory
    public void Awake()
    {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserver::Awake: Observer has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    // Use this for initialization when object is spawned into the worldspace
    public void Start()
    {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserver::Start: Renaming Observer to " + netId + ".");
        gameObject.name = "Pawn!" + netId + "!";
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserver::Start: Observer has finished starting.");
    }

    // Get inputs from the local player
    void Update() {
        if (isLocalPlayer) {
            //Getting clients inputs
            if (!m_controlsLocked)
            {
                GetInputs(ref m_inputs);
            }
        }
    }

    // Process updates from the server and mobilize observers
    void FixedUpdate() {
        if ( isLocalPlayer ) {

            m_inputs.timeStamp = Time.time;
            //Client side prediction for non-authoritative client or plane movement and rotation for listen server/host
            Vector3 lastPosition = m_results.position;
            Quaternion lastRotation = m_results.rotation;
            bool lastCrouch = m_results.crouching;
            m_results.rotation = Rotate( m_inputs, m_results );
            m_results.crouching = Crouch( m_inputs, m_results );
            m_results.sprinting = Sprint( m_inputs, m_results );
            m_results.position = Move( m_inputs, m_results );
            if( hasAuthority ) {
                //Listen server/host part
                //Sending results to other clients(state sync)
                if( m_dataStep >= GetNetworkSendInterval() ) {
                    if( Vector3.Distance( m_results.position, lastPosition ) > 0 || Quaternion.Angle( m_results.rotation, lastRotation ) > 0 || m_results.crouching != lastCrouch ) {
                        m_results.timeStamp = m_inputs.timeStamp;
                        //Struct need to be fully new to count as dirty 
                        //Convering some of the values to get less traffic
                        SyncResults tempResults;
                        tempResults.yaw = (ushort)( m_results.rotation.eulerAngles.y * 182 );
                        tempResults.pitch = (ushort)( m_results.rotation.eulerAngles.x * 182 );
                        tempResults.position = m_results.position;
                        tempResults.sprinting = m_results.sprinting;
                        tempResults.crouching = m_results.crouching;
                        tempResults.timeStamp = m_results.timeStamp;
                        syncResults = tempResults;
                    }
                    m_dataStep = 0;
                }
                m_dataStep += Time.fixedDeltaTime;
            } else {
                //Owner client. Non-authoritative part
                //Add inputs to the inputs list so they could be used during reconciliation process
                if( Vector3.Distance( m_results.position, lastPosition ) > 0 || Quaternion.Angle( m_results.rotation, lastRotation ) > 0 || m_results.crouching != lastCrouch ) {
                    m_inputsList.Add( m_inputs );
                }
                //Sending inputs to the server
                //Unfortunately there is now method overload for [Command] so I need to write several almost similar functions
                //This one is needed to save on network traffic
                SyncInputs syncInputs;
                syncInputs.forward = (sbyte)( m_inputs.forward * 127 );
                syncInputs.sides = (sbyte)( m_inputs.sides * 127 );
                syncInputs.vertical = (sbyte)( m_inputs.vertical * 127 );
                if( Vector3.Distance( m_results.position, lastPosition ) > 0 ){
                    if( Quaternion.Angle( m_results.rotation,lastRotation ) > 0){
                        Cmd_MovementRotationInputs( syncInputs.forward, syncInputs.sides, syncInputs.vertical, m_inputs.pitch, m_inputs.yaw, m_inputs.sprint, m_inputs.crouch, m_inputs.timeStamp );
                    }else{
                        Cmd_MovementInputs( syncInputs.forward, syncInputs.sides, syncInputs.vertical, m_inputs.sprint, m_inputs.crouch, m_inputs.timeStamp );
                    }
                } else {
                    if( Quaternion.Angle( m_results.rotation, lastRotation ) > 0 ) {
                        Cmd_RotationInputs( m_inputs.pitch, m_inputs.yaw, m_inputs.crouch, m_inputs.timeStamp );
                    } else {
                        Cmd_OnlyStances( m_inputs.crouch, m_inputs.timeStamp );
                    }
                }
            }
        } else {
            if( hasAuthority ) {
                //Server

                //Check if there is atleast one record in inputs list
                if( m_inputsList.Count == 0 ) {
                    return;
                }
                //Move and rotate part. Nothing interesting here
                Inputs inputs = m_inputsList[ 0 ];
                m_inputsList.RemoveAt( 0 );
                Vector3 lastPosition = m_results.position;
                Quaternion lastRotation = m_results.rotation;
                bool lastCrouch = m_results.crouching;
                m_results.rotation = Rotate( inputs, m_results );
                m_results.crouching = Crouch( inputs, m_results );
                m_results.sprinting = Sprint( inputs, m_results );
                m_results.position = Move( inputs, m_results );
                //Sending results to other clients(state sync)

                if( m_dataStep >= GetNetworkSendInterval() ) {
                    if( Vector3.Distance( m_results.position, lastPosition ) > 0 || Quaternion.Angle( m_results.rotation, lastRotation ) > 0 || m_results.crouching != lastCrouch ) {
                        //Struct need to be fully new to count as dirty 
                        //Convering some of the values to get less traffic
                        m_results.timeStamp = inputs.timeStamp;
                        SyncResults tempResults;
                        tempResults.yaw = (ushort)( m_results.rotation.eulerAngles.y * 182 );
                        tempResults.pitch = (ushort)( m_results.rotation.eulerAngles.x * 182 );
                        tempResults.position = m_results.position;
                        tempResults.sprinting = m_results.sprinting;
                        tempResults.crouching = m_results.crouching;
                        tempResults.timeStamp = m_results.timeStamp;
                        syncResults = tempResults;
                    }
                    m_dataStep = 0;
                }
                m_dataStep += Time.fixedDeltaTime;
            }else{
                //Non-owner client a.k.a. dummy client
                //there should be at least two records in the results list so it would be possible to interpolate between them in case if there would be some dropped packed or latency spike
                //And yes this stupid structure should be here because it should start playing data when there are at least two records and continue playing even if there is only one record left 
                if( m_resultsList.Count == 0 ) {
                    m_playData = false;
                }
                if( m_resultsList.Count >=2 ) {
                    m_playData = true;
                }
                if( m_playData ) {
                    if( m_dataStep==0 ) {
                        m_startPosition = m_results.position;
                        m_startRotation = m_results.rotation;
                    }
                    m_step = 1 / ( GetNetworkSendInterval() ) ;
                    m_results.rotation = Quaternion.Slerp(m_startRotation, m_resultsList[0].rotation, m_dataStep);
                    m_results.position = Vector3.Lerp(m_startPosition, m_resultsList[0].position, m_dataStep);
                    m_results.crouching = m_resultsList[0].crouching;
                    m_results.sprinting = m_resultsList[0].sprinting;
                    m_dataStep += m_step * Time.fixedDeltaTime;
                    if( m_dataStep>= 1 ) {
                        m_dataStep = 0;
                        m_resultsList.RemoveAt( 0 );
                    }
                }
                UpdateRotation( m_results.rotation );
                UpdatePosition( m_results.position );
                UpdateCrouch( m_results.crouching );
                UpdateSprinting( m_results.sprinting );
            }
        }
    }
    // // // End of Unity3D Magic Methods

    // // // Commands sent over the network to the server
    // Commands send information from a client to be invoked on the server.

    //Standing on spot
    [Command(channel = 0)]
    void Cmd_OnlyStances(bool crouch, float timeStamp) {
        if( hasAuthority && !isLocalPlayer ) {
            Inputs inputs;
            inputs.forward = 0;
            inputs.sides = 0;
            inputs.pitch = 0;
            inputs.vertical = 0;
            inputs.yaw = 0;
            inputs.sprint = false;
            inputs.crouch = crouch;
            inputs.timeStamp = timeStamp;
            m_inputsList.Add( inputs );
        }
    }
    //Only rotation inputs sent 
    [Command(channel = 0)]
    void Cmd_RotationInputs( float pitch, float yaw, bool crouch, float timeStamp ) {
        if( hasAuthority && !isLocalPlayer ) {
            Inputs inputs;
            inputs.forward = 0;
            inputs.sides = 0;
            inputs.vertical = 0;
            inputs.pitch = pitch;
            inputs.yaw = yaw;
            inputs.sprint = false;
            inputs.crouch = crouch;
            inputs.timeStamp = timeStamp;
            m_inputsList.Add( inputs );
        }
    }
    //Rotation and movement inputs sent 
    [Command(channel = 0)]
    void Cmd_MovementRotationInputs( sbyte forward, sbyte sides, sbyte vertical, float pitch, float yaw, bool sprint, bool crouch, float timeStamp ) {
        if( hasAuthority && !isLocalPlayer ) {
            Inputs inputs;
            inputs.forward = Mathf.Clamp( (float)forward/127, -1, 1 );
            inputs.sides = Mathf.Clamp( (float)sides/127, -1, 1 );
            inputs.vertical = Mathf.Clamp( (float)vertical/127, -1, 1 );
            inputs.pitch = pitch;
            inputs.yaw = yaw;
            inputs.sprint = sprint;
            inputs.crouch = crouch;
            inputs.timeStamp = timeStamp;
            m_inputsList.Add( inputs );
        }
    }

    //Only movements inputs sent
    [Command(channel = 0)]
    void Cmd_MovementInputs( sbyte forward, sbyte sides, sbyte vertical, bool sprint, bool crouch, float timeStamp ) {
        if( hasAuthority && !isLocalPlayer ) {
            Inputs inputs;
            inputs.forward = Mathf.Clamp( (float)forward/127, -1, 1 );
            inputs.sides = Mathf.Clamp( (float)sides/127, -1, 1 );
            inputs.vertical = Mathf.Clamp( (float)vertical/127, -1, 1 );
            inputs.pitch = 0;
            inputs.yaw = 0;
            inputs.sprint = sprint;
            inputs.crouch = crouch;
            inputs.timeStamp = timeStamp;
            m_inputsList.Add( inputs );
        }
    }

    // Things that should be subclassed

    //Self explanatory
    //Can be changed in inherited class
    public virtual void GetInputs(ref Inputs inputs)
    {
        //Don't use one frame events in this part
        //It would be processed incorrectly 
        inputs.sides = RoundToLargest(Input.GetAxis("Horizontal"));
        inputs.forward = RoundToLargest(Input.GetAxis("Vertical"));
        inputs.yaw = -Input.GetAxis("Mouse Y") * m_mouseSense * Time.fixedDeltaTime / Time.deltaTime;
        inputs.pitch = Input.GetAxis("Mouse X") * m_mouseSense * Time.fixedDeltaTime / Time.deltaTime;
        inputs.sprint = Input.GetButton("Sprint");
        inputs.crouch = Input.GetButton("Crouch");

        if (Input.GetButtonDown("Jump") && inputs.vertical <= -0.9f)
        {
            m_jumping = true;
        }
        float verticalTarget = -1;
        if (m_jumping)
        {
            verticalTarget = 1;
            if (inputs.vertical >= 0.9f)
            {
                m_jumping = false;
            }
        }
        inputs.vertical = Mathf.Lerp(inputs.vertical, verticalTarget, 20 * Time.deltaTime);
    }

    //Next virtual functions can be changed in inherited class for custom movement and rotation mechanics
    //So it would be possible to control for example humanoid or vehicle from one script just by changing controlled pawn
    public virtual void UpdatePosition( Vector3 newPosition ) {
        transform.position = newPosition;
    }

    public virtual void UpdateRotation( Quaternion newRotation ) {
        transform.rotation = newRotation;
    }

    public virtual void UpdateCrouch( bool crouch ) {

    }

    public virtual void UpdateSprinting( bool sprinting ) {
        
    }

    public virtual Vector3 Move( Inputs inputs, Results current ) {
        transform.position = current.position;
        float speed = 2;
        if( current.crouching ) {
            speed = 1.5f;
        }
        if( current.sprinting ) {
            speed = 3;
        }
        transform.Translate ( Vector3.ClampMagnitude( new Vector3( inputs.sides, inputs.vertical, inputs.forward), 1 ) * speed * Time.fixedDeltaTime );
        return transform.position;
    }

    public virtual Quaternion Rotate( Inputs inputs, Results current ) {
        transform.rotation = current.rotation;
        float mHor = transform.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
        float mVert = transform.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;
        
        if( mVert > 180 )
            mVert -= 360;
        transform.rotation = Quaternion.Euler ( mVert, mHor, 0 );
        return transform.rotation;
    }

    public virtual bool Sprint(Inputs inputs, Results current)
    {
        return inputs.sprint;
    }

    public virtual bool Crouch(Inputs inputs, Results current)
    {
        return inputs.crouch;
    }

    // Client Callbacks

    // Updating Clients with server states
    [ClientCallback]
    void RecieveResults( SyncResults syncResults ) { 
        // Convering values back
        Results results;
        results.rotation = Quaternion.Euler ( (float)syncResults.pitch/182, (float)syncResults.yaw/182, 0 );
        results.position = syncResults.position;
        results.sprinting = syncResults.sprinting;
        results.crouching = syncResults.crouching;
        results.timeStamp = syncResults.timeStamp;

        // Discard out of order results
        if( results.timeStamp <= m_lastTimeStamp ) {
            return;
        }
        m_lastTimeStamp = results.timeStamp;
        // Non-owner client
        if( !isLocalPlayer && !hasAuthority ) {
            // Adding results to the results list so they can be used in interpolation process
            results.timeStamp = Time.time;
            m_resultsList.Add(results);
        }

        // Owner client
        // Server client reconciliation process should be executed in order to client's rotation and position with server values but do it without jittering
        if( isLocalPlayer && !hasAuthority ) {
            // Update client's position and rotation with ones from server 
            m_results.rotation = results.rotation;
            m_results.position = results.position;
            int foundIndex = -1;
            // Search recieved time stamp in client's inputs list
            for( int index = 0; index < m_inputsList.Count; index++ ) {
                // If time stamp found run through all inputs starting from needed time stamp 
                if( m_inputsList[index].timeStamp > results.timeStamp ) {
                    foundIndex = index;
                    break;
                }
            }
            if( foundIndex ==-1 ) {
                // Clear Inputs list if no needed records found 
                while( m_inputsList.Count != 0 ) {
                    m_inputsList.RemoveAt(0);
                }
                return;
            }
            // Replay all of the recorded inputs from the user
            for( int subIndex = foundIndex; subIndex < m_inputsList.Count; subIndex++ ) {
                m_results.rotation = Rotate( m_inputsList[subIndex], m_results );
                m_results.crouching = Crouch( m_inputsList[subIndex], m_results );
                m_results.sprinting = Sprint( m_inputsList[subIndex], m_results );

                m_results.position = Move( m_inputsList[subIndex], m_results );
            }
            // Remove all inputs recorded before the current target time stamp
            int targetCount = m_inputsList.Count - foundIndex;
            while( m_inputsList.Count > targetCount ) {
                m_inputsList.RemoveAt(0);
            }
        }
    }


    // // // Helper Methods
    // Public method to set our start position
    public void SetStartPosition(Vector3 position) { m_results.position = position; }

    // Public method to set our start rotation
    public void SetStartRotation(Quaternion rotation) { m_results.rotation = rotation; }

    // Round a float to the largest sbyte
    public sbyte RoundToLargest(float theInput)
    {
        if (theInput > 0) { return 1; }
        else if (theInput < 0) { return -1; }
        return 0;
    }

    // Callable by UI buttons and such...
    public void InputsLocked() { m_controlsLocked = true; m_CursorLockMode = CursorLockMode.None; }
    public void InputsUnlock() { m_controlsLocked = false; m_CursorLockMode = CursorLockMode.Locked;  }
    public void InputsSetLock(bool lockedstate) { if (lockedstate) { InputsLocked(); } else { InputsUnlock(); } SetCursorState(); }

    // Apply requested cursor state
    public void SetCursorState()
    {
        Cursor.lockState = m_CursorLockMode;
        // Hide cursor when locking
        Cursor.visible = (CursorLockMode.Locked != m_CursorLockMode);
    }

    // A short helper for Debug.Log and uNET status.
    public string GetObjectDebugInfo()
    {
        var thisObjectDebugInfo = gameObject.name + "|S:" + GetShorterAnswerToBool(isServer) + "|A:" + GetShorterAnswerToBool(hasAuthority) + "|C:" + GetShorterAnswerToBool(isClient) + "|L:" + GetShorterAnswerToBool(isLocalPlayer) + "|";
        return thisObjectDebugInfo;
    }

    // Stupid helper for GetObjectDebugInfo
    public string GetShorterAnswerToBool(bool input)
    {
        if (input) { return "Y"; }
        else { return "N"; }
    }

}
