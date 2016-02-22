using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NetworkedItemScript : MonoBehaviour {
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    public int m_slot;
    public bool m_selected = false;
    public Transform m_bullet;
    public int m_AnimationType = 0;
    public NetworkedObserverWeaponController m_weaponController;
    public Animator m_animator;
    public Transform m_Aimpoint;
    public Transform m_ShootPoint;
    public List<MeshRenderer> m_meshRenderers;
    public float m_FireTime = 0.1f;
    private float m_lastFireTime = 0;
    private Vector3 m_startPos;
    private Quaternion m_startRot;

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    public void Awake() {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Awake: NetworkedItemScript has Awakened.");
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    // Use this for initialization when object is spawned into the worldspace
    //void Start () { }

    public void ItemExposed(bool exposed) {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::ItemExposed:LOCAL_OBSERVATION: Exposing object: " + gameObject.name);
        foreach (MeshRenderer meshRenderer in m_meshRenderers) { meshRenderer.GetComponent<Renderer>().enabled = exposed; }
    }

    public void ReloadItem()
    {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::ReloadItem:LOCAL_OBSERVATION: This object was asked to reload!");
        m_animator.SetInteger("AnimationType", m_AnimationType);
        // Put away the gun
        m_animator.SetBool("Holster", true);
        if (m_animator.GetBool("Holstered"))
        {
            Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::ReloadItem:LOCAL_OBSERVATION: Weapon holstered, adding ammo!");
            // FIXME -- THIS IS TEMPORARY, DOING NOTHING.
            // Take ammo out of the pocket
            m_weaponController.router.inventory.m_slotCharges[m_weaponController.router.inventory.m_currentSlot] -= 10;
            // Add ammo to the gun
            m_weaponController.router.inventory.m_slotCharges[m_weaponController.router.inventory.m_currentSlot] += 10;
            // Pull the gun back out
            m_animator.SetBool("Holster", false);
        }
        // Wait for the animation to finish
        if (!m_animator.GetBool("Holstered"))
        {
            Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::ReloadItem:LOCAL_OBSERVATION: This object was reloaded and unholstered!");
        }

    }

    public void Select() {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Select:LOCAL_OBSERVATION: This object was selected!");
        if (m_animator.GetBool ("Holstered")) {
            m_selected = true;
            m_animator.SetInteger ("AnimationType", m_AnimationType);
            m_animator.SetBool ("Holster", false);
            ItemExposed (m_selected); //gameObject.SetActive (true);
        }
        // And now the unholster animation begins to play.
    }

    public void Deselect() {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Deselect:LOCAL_OBSERVATION: This object was deselected!");
        if (m_animator.GetBool ("Holstered")) {
            m_selected = false;
            ItemExposed (m_selected); //gameObject.SetActive (false);
        } else {
            m_animator.SetInteger ("AnimationType", -1);
            m_animator.SetBool ("Holster", true);
        }
        // And now the holster animation begins to play.
    }
    
    // Update is called once per frame
    void LateUpdate () {
        m_startPos = m_ShootPoint.position;
        m_startRot = m_ShootPoint.rotation;
    }

    public bool Input_Fire1()
    { // THIS IS ACTUALLY A CHECK TO SEE IF YOU *CAN* FIRE1, overload it to false if this object can't Fire1.
        //Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Input_Fire1:LOCAL_OBSERVATION: You actuated Fire1!");
        // Check to see if we have any Charges to fire.
        if ( !(m_weaponController.router.inventory.m_slotCharges[m_weaponController.router.inventory.m_currentSlot] >= 1) ) { // If you don't have Charges
            Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Input_Fire1:LOCAL_OBSERVATION: You have no Charges in this item to successfully actuate Fire1!");
            return false; // return CannotFire
        } else {  // If you do have ammo
            if (Time.time >= (m_lastFireTime + m_FireTime)) {
                m_lastFireTime = Time.time;
                return true; // return CanFire
            } else {
                return false; // return CannotFire
            }
        } 
    }

    public bool Input_Fire2()
    { // THIS IS ACTUALLY A CHECK TO SEE IF YOU *CAN* FIRE2, overload it to false if this object can't Fire2.
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Input_Fire2:LOCAL_OBSERVATION: You actuated Fire2!");
        return true; // return CanFire
    }

    public bool Input_Fire3()
    { // THIS IS ACTUALLY A CHECK TO SEE IF YOU *CAN* FIRE3, overload it to false if this object can't Fire3.
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Input_Fire3:LOCAL_OBSERVATION: You actuated Fire3!");
        return true; // return CanFire
    }

    public bool Input_Reload()
    { // THIS IS ACTUALLY A CHECK TO SEE IF YOU *CAN* RELOAD, overload it to false if this object can't Reload.
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Input_Reload:LOCAL_OBSERVATION: Your weapon can be reloaded at this time.");
        return true; // return CanReload
    }

    // Create a new bullet object and hand it's script instance back to our caller.
    public NetworkedRaycastBulletScript PrepareBullet(){
        Transform bullet = (Transform)Instantiate (m_bullet, m_startPos, m_startRot);
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::PrepareBullet:LOCAL_OBSERVATION: A bullet was instantiated at: " + m_startPos.ToString());
        return bullet.GetComponent<NetworkedRaycastBulletScript> ();

    }

    // Tell the bullet object we created to begin it's work making particles and sounds and such for client enjoyment.
    public void Shoot(bool isOwner, byte shotID, NetworkedRaycastBulletScript bullet ){
        m_ShootPoint.SendMessage ( "Play", SendMessageOptions.DontRequireReceiver ); // Tell the muzzle particle system to play.
        Debug.Log(GetObjectDebugInfo() + "|NetworkedItemScript::Shoot:LOCAL_OBSERVATION: Your weapon was discharged with ShotID: " + shotID.ToString() );
        bullet.Shoot (m_weaponController, isOwner, shotID);
    }


    // A short helper for Debug.Log and uNET status.
    public string GetObjectDebugInfo()
    {
        var thisObjectDebugInfo = gameObject.name + "|";
        return thisObjectDebugInfo;
    }

    // Stupid helper for GetObjectDebugInfo
    public string GetShorterAnswerToBool(bool input)
    {
        if (input) { return "Y"; }
        else { return "N"; }
    }
}
