using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public struct PickupInfo {
    public int itemIndex;
    public int itemAmmo;
}

[NetworkSettings(channel=0,sendInterval=1f)]
public class NetworkedObserverInventory : NetworkBehaviour {
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    public GameObject m_itemRenderers; // An object containing all holdable items as children

    public int maxSlotsCount = 6; // How many slots can we have total?
    public int m_currentSlot = 0; // Which slot is currently selected?
    public NetworkedItemScript[] m_availableItems; //Array of available items specific to this entity

    public SyncListInt m_slots = new SyncListInt (); //List of item indexs to sync over network
    public SyncListInt m_slotCharges = new SyncListInt(); //List of item Charges to sync over network

    [SyncVar]
    private int m_syncSlot; //Selected slot
    
    private int m_input;

    private int m_lastSlot = -1;

    private bool m_holster = false;

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    public void Awake()
    {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::Awake: NetworkedObserverInventory has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    // Use this for initialization when object is spawned into the worldspace
    //void Start () { }

    // Use this for initialization
    public void ObserverSpawned()
    {
        if (m_slots.Count < maxSlotsCount)
        {
            for (int i = 0; i < maxSlotsCount; i++)
            {
                m_slots.Add(0); // Add an empty slot with itemid 0.
                m_slotCharges.Add(0); // Add a slotCharges entry with 0 charges.
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::PawnSpawned:OBSERVATION: Adding slot " + i);
            }
        }
    }

    public void GiveItem(PickupInfo itemInfo){
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Pickup " + itemInfo.itemIndex + " triggered giveitem.");
        if (hasAuthority) {
            //Create all slots if not created yet
            if(m_slots.Count < maxSlotsCount){
                for(int i = 0; i < maxSlotsCount; i++){
                    m_slots.Add(0); // Add an empty slot with itemid 0.
                    m_slotCharges.Add(0); // Add a slotCharges entry with 0 charges.
                    Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Adding slot " + i);
                }
                //if it is a first item switch to it
                m_currentSlot = m_availableItems[itemInfo.itemIndex].m_slot;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Changing current slot " + m_currentSlot);
                m_syncSlot = m_currentSlot;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Changing sync'd slot " + m_syncSlot);
                Rpc_FirstItem(m_currentSlot);
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Calling Rpc_FirstItem on " + m_currentSlot);
            }

            if(m_slots[m_availableItems[itemInfo.itemIndex].m_slot] == itemInfo.itemIndex){
                //Add ammo only if item already is in inventory
                m_slotCharges[m_availableItems[itemInfo.itemIndex].m_slot] += itemInfo.itemAmmo;
                //m_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Gave Ammo " + itemInfo.itemAmmo);
            }
            else{
                //Add item and ammo
                m_slots[m_availableItems[itemInfo.itemIndex].m_slot] = itemInfo.itemIndex;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Gave ItemID " + itemInfo.itemIndex);
                m_slotCharges[m_availableItems[itemInfo.itemIndex].m_slot] += itemInfo.itemAmmo;
                //m_availableItems[itemInfo.itemIndex].GiveAmmo(itemInfo.itemAmmo);
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GiveItem:OBSERVATION: Gave Ammo " + itemInfo.itemAmmo);
            }
        }
    }
    //Needed to switch client to first item
    [ClientRpc]
    void Rpc_FirstItem(int slot) {
        m_currentSlot = slot;
        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::Rpc_FirstItem:OBSERVATION: Switched Slots to " + slot);
    }

    void LateUpdate() {
        if (isLocalPlayer) {
            if (!router.networkPawn.m_controlsLocked) { GetInputs(); } // Get the player's inputs if the controls aren't locked.
            // Checking if it's possible to switch to new slot
            if (m_slots.Count > 0 && m_input < m_slots.Count && m_slots [m_input] >= 0 && m_availableItems [m_slots [m_input]]) {
                m_currentSlot = m_input; // Assign a new currentSlot from the current m_input.
                //Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:LOCALPLAYER: Can Switch Slots? " + m_slots.Count + " slots and " + m_availableItems.Length + " available items.");
            }

            if (hasAuthority) { // We are the authority, so we set the syncSlot's contents from the local currentSlot.
                m_syncSlot = m_currentSlot; // Set this object's syncSlot equal to the current slot content.
                //Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:HASAUTHORITY: Synced syncSlot " + m_syncSlot + " with currentSlot " + m_currentSlot + ".");
            }
            else {
                Cmd_SetSlot (m_currentSlot); // Call the SetSlot command, since we do not have authority.
                //Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:NOAUTHORITY: Execute Cmd_SetSlot to " + m_currentSlot);
            }

        } else { // Isn't the local player... Does it have authority?
            if(!hasAuthority){ // It does not have authority.
                m_currentSlot = m_syncSlot; // Set this object's currentSlot equal to the networked synced slot content.
                //Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:NOAUTHORITY: Synced currentSlot " + m_currentSlot + " with syncSlot " + m_syncSlot + ".");

            }
        }
        //Actual switching
        if (m_holster) {
            if (m_availableItems[m_slots[m_currentSlot]] != null)
            {
                if (m_availableItems[m_slots[m_currentSlot]].m_selected)
                {
                    m_availableItems[m_slots[m_currentSlot]].Deselect();
                    Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Item Deselected.");
                }
            } //else { Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Item was null, target wasn't a valid ItemScript?"); }
        } else {
            if ( m_slots.Count > 0 && m_currentSlot < m_slots.Count && m_availableItems [m_slots [m_currentSlot]]) {
                //Disabling last item
                if (m_lastSlot >= 0 && m_lastSlot != m_currentSlot) {
                    Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Disabling last item?");
                    if (m_availableItems [m_slots [m_lastSlot]].m_selected) {
                        m_availableItems [m_slots [m_lastSlot]].Deselect ();
                        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Different item.");
                    } else {
                        m_lastSlot = m_currentSlot;
                        Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Same item.");
                    }
                } else {
                    m_lastSlot = m_currentSlot;
                }
                //Enabling new item
                if (!m_availableItems [m_slots [m_currentSlot]].m_selected) {
                    m_availableItems [m_slots [m_currentSlot]].Select ();
                    Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::LateUpdate:OBSERVATION: Selected new item.");
                }
            }
        }
    }

    void GetInputs(){
        if (!router.networkPawn.m_controlsLocked)
        {
            if (Input.GetButtonDown("Slot1"))
            {
                m_input = 0;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 1 selected.");
            }
            else if (Input.GetButtonDown("Slot2"))
            {
                m_input = 1;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 2 selected.");
            }
            else if (Input.GetButtonDown("Slot3"))
            {
                m_input = 2;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 3 selected.");
            }
            else if (Input.GetButtonDown("Slot4"))
            {
                m_input = 3;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 4 selected.");
            }
            else if (Input.GetButtonDown("Slot5"))
            {
                m_input = 4;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 5 selected.");
            }
            else if (Input.GetButtonDown("Slot6"))
            {
                m_input = 5;
                m_holster = false;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 6 selected.");
            }

            if (Input.GetButtonDown("Holster"))
            {
                m_holster = !m_holster;
                Debug.Log(GetObjectDebugInfo() + "|NetworkedObserverInventory::GetInputs:OBSERVATION: Slot 0 selected, holstering weapon.");
            }

        }
    }

    // Commands send information from a client to be invoked on the server.
    [Command(channel=0)]
    void Cmd_SetSlot(int input){
        if (!isLocalPlayer && hasAuthority) {
            m_currentSlot = input;
            //Debug.Log(gameObject.name + "|NetworkedObserverInventory::Cmd_SetSlot: Slot selected: " + m_currentSlot + ", was: " + m_syncSlot);
            m_syncSlot = m_currentSlot;
        }
    }

    // A short helper for Debug.Log and uNET status.
    public string GetObjectDebugInfo()
    {
        var thisObjectDebugInfo = gameObject.name + "|S:" + GetShorterAnswerToBool(isServer) + "|A:" + GetShorterAnswerToBool(hasAuthority) + "|C:" + GetShorterAnswerToBool(isClient) + "|L:" + GetShorterAnswerToBool(isLocalPlayer) + "|";
        return thisObjectDebugInfo; 
    }

    // Stupid helper for GetObjectDebugInfo
    public string GetShorterAnswerToBool(bool input) {
        if (input) {return "Y";}
        else {return "N";}
    }
}
