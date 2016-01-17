using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ItemScript : MonoBehaviour {
	public int slot;
	public bool selected = false;
    public int m_ammoInGun;
    public int m_ammoInPocket;
    public Transform _bullet;
	public int AnimationType = 0;
	public WeaponController _weaponController;
	public Animator animator;
	public Transform Aimpoint;
	public Transform ShootPoint;
    public List<MeshRenderer> m_meshRenderers;
	public float FireTime = 0.1f;
	private float _lastFireTime = 0;
	private Vector3 _startPos;
	private Quaternion _startRot;



	public void GiveAmmo(int amount){
        m_ammoInPocket += amount;
	}

	public void Select(){
		if (animator.GetBool ("Holstered")) {
			selected = true;
			animator.SetInteger ("AnimationType", AnimationType);
			animator.SetBool ("Holster", false);
            ItemExposed (true);
			//gameObject.SetActive (true);
		}
	}

	public void Deselect(){
		if (animator.GetBool ("Holstered")) {
			selected = false;
            ItemExposed (false);
            //gameObject.SetActive (false);
		} else {
			animator.SetInteger ("AnimationType", -1);
			animator.SetBool ("Holster", true);
		}
	}

	// Use this for initialization
	//void Start () {	}
	
	// Update is called once per frame
	void LateUpdate () {
		_startPos = ShootPoint.position;
		_startRot = ShootPoint.rotation;
	}

	public bool Fire1(){
        if (!(m_ammoInGun >= 1)) { // If you don't have ammo
            return false;
        } else {  // If you do have ammo
		    if (Time.time >= (_lastFireTime + FireTime)) {
			    _lastFireTime = Time.time;
                return true;
		    } else {
			    return false;
		    }
        } 
    }

    public bool Fire2()
    { // THIS IS ACTUALLY A CHECK TO SEE IF YOU *CAN* RELARD
        Debug.Log("Your weapon is now reloaded magically.");
        m_ammoInGun = 20;
        m_ammoInPocket = (m_ammoInPocket - m_ammoInGun);
        return true;
    }

    public void ItemExposed(bool exposed)
    {
        foreach (MeshRenderer meshRenderer in m_meshRenderers)
        {
            meshRenderer.GetComponent<Renderer>().enabled = exposed;
        }
    }


    public BulletScript PrepareBullet(){
		Transform bullet = (Transform)Instantiate (_bullet, _startPos, _startRot);
		return bullet.GetComponent<BulletScript> ();

	}

	public void Shoot(bool isOwner,byte shotID,BulletScript bullet ){
		ShootPoint.SendMessage ("Play", SendMessageOptions.DontRequireReceiver);
        m_ammoInGun--; // Decrement ammo in the gun
        bullet.Shoot (_weaponController, isOwner,shotID);
    }
}
