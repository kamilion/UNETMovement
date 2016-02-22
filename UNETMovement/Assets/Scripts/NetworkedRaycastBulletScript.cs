using UnityEngine;
using System.Collections;

public class NetworkedRaycastBulletScript : MonoBehaviour {
    public Transform hitEffect;

    public void Shoot (NetworkedObserverWeaponController weaponController, bool thisPlayerOwnsBullet, byte shotID) {
        RaycastHit hit;
        if ( Physics.Raycast (transform.position, transform.forward, out hit) ) {
            // Debugging effects
            Debug.DrawRay (transform.position, transform.forward, Color.green, 3);
            Debug.DrawLine (transform.position, hit.point, Color.red, 2);
            // Real effects
            Instantiate (hitEffect, hit.point, Quaternion.LookRotation (hit.normal, Vector3.up) );
            if (thisPlayerOwnsBullet) {
                weaponController.CheckShot (shotID, hit.point); // Tell the WeaponController to check this shot.
            }
        }
        GameObject.Destroy (gameObject); // Destroy this bullet.
    }

}
