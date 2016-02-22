using UnityEngine;
using System.Collections;

public class HitDetector : MonoBehaviour {
	public bool thisIsHead;
	private Enemy enemyScript;
	private GameObject EnemyObj;
	private float maxSpeed = 50f;
	// Use this for initialization
	void Start () {
		enemyScript = this.transform.root.gameObject.GetComponent<Enemy> ();
		EnemyObj = this.transform.root.gameObject;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnCollisionEnter(Collision col){
		if(enemyScript.alive){
		if (col.gameObject.tag == "bullet") {
			if(thisIsHead){
				enemyScript.health -=100;
				//this.rigidbody.AddForce(col.gameObject.transform.up * 50, ForceMode.Impulse);
			}
			else{
				enemyScript.health -=20;
				EnemyObj.GetComponent<Animator>().SetTrigger("Hited");
				enemyScript.EnemySounds();
				if(enemyScript.state == Enemy.State.Aggro){
					enemyScript.SwitchToAttack();
				}
				if(enemyScript.state == Enemy.State.Idle || enemyScript.state == Enemy.State.Patrol){
					enemyScript.state = Enemy.State.Attack;
					}
				}
			}
			if(col.gameObject.GetComponent<Rigidbody>() != null){
				if(GetComponent<Rigidbody>().velocity.magnitude > maxSpeed){
					enemyScript.Die();
				}
			}
		}
		else{
			return;
		}
	}
}
