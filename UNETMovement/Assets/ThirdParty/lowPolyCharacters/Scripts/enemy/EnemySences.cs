using UnityEngine;
using System.Collections;

public class EnemySences : MonoBehaviour {
	public float fieldOfViewAngle = 110f;           // Number of degrees, centred on forward, for the enemy see.
	public bool playerInSight;                      // Whether or not the player is currently sighted.
	public Enemy enemyScript;
	private SphereCollider col;                         // Reference to the Animator.
	private GameObject player;                      // Reference to the player.
	// Use this for initialization
	void Start () {
		enemyScript = this.GetComponent<Enemy>();
		col = GetComponent<SphereCollider>();
		player = GameObject.FindGameObjectWithTag("Player");	
		//enemyScript = this.transform.root.gameObject.GetComponent<Enemy> ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnTriggerStay (Collider other)	{
		if(enemyScript.alive){
		// If the player has entered the trigger sphere...
		if(other.gameObject == player){
			// Create a vector from the enemy to the player and store the angle between it and forward.
			Vector3 direction = other.transform.position - transform.position;
			float angle = Vector3.Angle(direction, transform.forward);			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if(angle < fieldOfViewAngle * 0.5f){
				RaycastHit hit;				
				// ... and if a raycast towards the player hits something...
				if(Physics.Raycast(transform.position + transform.up, direction.normalized, out hit, col.radius)){
					Debug.DrawLine (transform.position + transform.up, hit.point, Color.blue);
					// ... and if the raycast hits the player...
					if(hit.collider.gameObject.tag == "Player"){
						playerInSight = true;
						Debug.Log("I SEE U!!!");
						}
					}
				}
			}
		}
		else{
			return;
		}
	}
	void OnTriggerExit(Collider other){
		// If the player leaves the trigger zone...
		if(other.gameObject == player)
			// ... the player is not in sight.
			playerInSight = false;
	}
}
