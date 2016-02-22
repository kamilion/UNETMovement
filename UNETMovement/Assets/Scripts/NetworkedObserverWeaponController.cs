
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public struct ShotInfo {
    public byte shotID;  // 0-255 ID of the last shot
    public Vector3 startPosition; // Where the shot originated from
    public Vector3 startDirection; // Path of shot
}

public class NetworkedObserverWeaponController : NetworkBehaviour {
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    public int m_maxShotHistory = 30;
    private byte m_shotID = 0;
    private bool m_fire1 = false;
    private bool m_fire2 = false;
    private List<ShotInfo> m_shotInfoHistory = new List<ShotInfo> ();
    private NetworkedItemScript m_item; // Will contain the maximum number of charges in it.
    //[SyncVar] private int m_itemChargesInPocket = 0; // How many charges are in the pocket?
    //[SyncVar] private int m_itemChargesInItem = 0; // How many charges are in the item?

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    public void Awake () {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Awake: WeaponController has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    // Use this for initialization when object is spawned into the worldspace
    //void Start () { }

    // Update is called once per frame
    void Update () {
        if (isLocalPlayer) {
            if (!router.networkPawn.m_controlsLocked) {
                m_fire1 = Input.GetButton("Fire1");
                m_fire2 = Input.GetButton("Fire2");
            }
        }
    }

    void FixedUpdate ()
    {
        if (router.inventory.m_slots.Count == 0) { return; } // If there are no inventory slots at all, bail out early.
        m_item = router.inventory.m_availableItems[router.inventory.m_slots[router.inventory.m_currentSlot]]; // Get the item from the slots structure.
        //m_itemChargesInPocket = router.inventory.m_slotCharges[router.inventory.m_currentSlot]; // Get the itemCharges from the slotsCharges structure.
        if (m_item == null) { return; } // If the item in m_currentSlot is null, bail out early so we don't try to call functions on a null object.

        //Debug.Log("State of item: " + m_item.name);
        // This will only apply to client instances.
        if (isLocalPlayer)
        {
            // If the player presses their primary fire.
            if (m_fire1 && m_item.Input_Fire1())
            {
                NetworkedRaycastBulletScript bulletScript = m_item.PrepareBullet();

                // If we have authority, call the RPC function, otherwise call the player fire button command.
                if (hasAuthority)
                {
                    UpdateHistory(m_shotID, bulletScript);
                    Rpc_Shoot();
                    //router.inventory.m_slotCharges[router.inventory.m_currentSlot]--; // Decrement the charges here?
                }
                else
                {
                    Cmd_Fire1(m_shotID);
                }

                m_item.Shoot(true, m_shotID, bulletScript);

                m_shotID++;
                if (m_shotID == 255)
                {
                    m_shotID = 0;
                }
            }
            // If the player presses their secondary fire.
            if (m_fire2 && m_item.Input_Fire2())
            {
                // Quick and dirty reload, this needs to be replaced later.
                if (hasAuthority)
                {
                    Rpc_Reload();
                }
                else
                {
                    Cmd_Fire2();
                }

            }
        }
    }

    // Commands send information from a client to be invoked on the server.
    [Command]
    void Cmd_Fire1 (byte shotID) {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_Fire1:OBSERVATION: You actuated Fire1!");
        // This should be protected from m_item being null due to FixedUpdate bailing out early now.
        if (m_item.Input_Fire1 () ) {
            NetworkedRaycastBulletScript bulletScript = m_item.PrepareBullet();
            m_item.Shoot(false, m_shotID, bulletScript);
            UpdateHistory(shotID, bulletScript);
            Rpc_Shoot();
            router.inventory.m_slotCharges[router.inventory.m_currentSlot]--; // Decrement the charges here?
        }
    }

    // Commands send information from a client to be invoked on the server.
    [Command]
    void Cmd_Fire2 () {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_Fire2:OBSERVATION: You actuated Fire2!");
        // This should be protected from m_item being null due to FixedUpdate bailing out early now.
        if (m_item.Input_Fire2 ())
        {
            Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_Fire2:OBSERVATION: Calling Rpc_Reload");
            Rpc_Reload();
            Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_Fire2:OBSERVATION: Returned from Rpc_Reload");
        }
    }

    void UpdateHistory (byte shotID, NetworkedRaycastBulletScript bulletScript) {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::UpdateHistory:OBSERVATION: History was updated in " + gameObject.name + "!");
        ShotInfo shot;
        shot.shotID = shotID;
        shot.startPosition = bulletScript.transform.position;
        shot.startDirection = bulletScript.transform.forward;
        m_shotInfoHistory.Add(shot);
        if(m_shotInfoHistory.Count > m_maxShotHistory){
            m_shotInfoHistory.RemoveAt(0);
        }
    }

    [ClientRpc]
    void Rpc_Shoot () {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Rpc_Shoot:CLIENT_OBSERVATION: Weapon was discharged.");
        if (!isLocalPlayer) {
            m_item.Shoot(false, 0, m_item.PrepareBullet() );
        }
    }

    [ClientRpc]
    void Rpc_Reload()
    {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Rpc_Reload:CLIENT_OBSERVATION: Weapon was asked to reload.");
        if (!isLocalPlayer)
        {
            Debug.Log(GetObjectDebugInfo() + "|WeaponController::Rpc_Reload:CLIENT_OBSERVATION: Calling m_item.ReloadItem");
            m_item.ReloadItem();
            Debug.Log(GetObjectDebugInfo() + "|WeaponController::Rpc_Reload:CLIENT_OBSERVATION: Returned from m_item.ReloadItem");
        }
    }

    public void CheckShot (byte shotID, Vector3 position) {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::CheckShot:CLIENT_OBSERVATION: Checking Shot: " + shotID.ToString());
        if (isLocalPlayer) { // We are a local client player.
            if(hasAuthority){ // We are also the local host.
                Debug.Log(GetObjectDebugInfo() + "|WeaponController::CheckShot:LOCAL_HOST_AUTHORITY: Checking Shot: " + shotID.ToString());
                foreach (ShotInfo shot in m_shotInfoHistory) {
                    if(shot.shotID == shotID) {
                        var hitObjectToDamage = SimulateShot(shot.startPosition, shot.startDirection, position).GetComponent<NetworkedGeometryObservesDamage>();
                        // Tell the thing we hit to take damage.
                        if (hitObjectToDamage != null)
                        { // The object we hit may not have an ObjectDamage component.
                            Debug.Log(GetObjectDebugInfo() + "|WeaponController::CheckShot:LOCAL_HOST_OBSERVATION: Shot: " + shotID.ToString() + " caused damage to " + hitObjectToDamage.name);
                            hitObjectToDamage.Cmd_TakeDamage(10); // Apply 10 damage to the hit GameObject.
                        }
                    }
                }

            } else { // We do not have authority, so ask the server to do it for us.
                Cmd_CheckShot(shotID,position);
            }
        }
    }

    // Commands send information from a client to be invoked on the server.
    [Command]
    void Cmd_CheckShot (byte shotID, Vector3 position) {
        Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_CheckShot:SERVER_OBSERVATION: Checking Shot: " + shotID.ToString() );
        foreach (ShotInfo shot in m_shotInfoHistory){
            if(shot.shotID == shotID){
                var hitObjectToDamage = SimulateShot (shot.startPosition,shot.startDirection,position).GetComponent<NetworkedGeometryObservesDamage>();
                // Tell the thing we hit to take damage.
                if (hitObjectToDamage != null)
                { // The object we hit may not have an ObjectDamage component.
                    Debug.Log(GetObjectDebugInfo() + "|WeaponController::Cmd_CheckShot:SERVER_OBSERVATION: Shot: " + shotID.ToString() + " caused damage to " + hitObjectToDamage.name);
                    hitObjectToDamage.Rpc_TakeDamage(10); // Apply 10 damage to the hit GameObject.
                }
            }
        }
    }

    Collider SimulateShot (Vector3 startPosition, Vector3 startDirection, Vector3 hitPosition) {
        RaycastHit hit;
        if(Physics.Raycast(startPosition, startDirection, out hit)){
            Debug.Log(GetObjectDebugInfo() + "|WeaponController::SimulateShot:CLIENT_OBSERVATION: Shot Distance between hit.point and hitPosition = " + Vector3.Distance(hit.point, hitPosition));
            if(Vector3.Distance(hit.point, hitPosition) <= 10){
                Debug.Log(GetObjectDebugInfo() + "|WeaponController::SimulateShot:CLIENT_OBSERVATION: Shot Registered as impacting " + hit.collider.name + " after traveling " + Vector3.Distance(hit.point, startPosition) + " units.");
            }
        }
        return hit.collider;
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
