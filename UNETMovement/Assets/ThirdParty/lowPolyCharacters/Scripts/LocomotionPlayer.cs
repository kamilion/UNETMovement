using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
  
[RequireComponent(typeof(Animator))]  

public class LocomotionPlayer : MonoBehaviour {

    protected Animator _animator;
	private CharacterController _charCtrl; 
	public AudioClip[] m_FootstepSoundsGround;
	public AudioClip[] m_FootstepSoundsWater;
	public GameObject myBody;
	private AudioSource m_AudioSource;
	public Transform ladder;//current ladder 
	private bool _jump;//jump button press detection
	private bool _jumpOverObstcle;
	public bool canClimb;
	public bool canClimbOff;
	public bool onLadder;//player is on the ladder
	public bool usingPistol;

	public bool _run;
	public bool _crouch;
	public bool canRotate = true;
	public bool alive = true;
	public bool waterWalk;

	public float _speed;
	public float _strafe;
	public float gravity ;//gravity force 
	public float health;

	private float _mouseX;
	private float tapTimeWindow;

	public Texture textureToChange;
	public Texture currTexture;
	
	// Use this for initialization
	void Start () 
	{
        _animator = GetComponent<Animator>();
		_charCtrl = GetComponent<CharacterController>();
		m_AudioSource = GetComponent<AudioSource>();
		currTexture = myBody.GetComponent<SkinnedMeshRenderer> ().material.mainTexture;
	}
    
	void Update () 
	{
		_mouseX = Input.GetAxis ("Mouse X");
		_speed = Input.GetAxis("Vertical");//reading vertical axis input
		_strafe = Input.GetAxis("Horizontal");//reading horizontal axis input
		_run = Input.GetKey(KeyCode.LeftShift) ? true : false;//check if run button was pressed
		_crouch = Input.GetKey(KeyCode.LeftControl) ? true : false;//check if run button was pressed
		if (Input.GetKeyDown (KeyCode.Alpha1)) {
			usingPistol = true;
		}
		if (Input.GetKeyDown (KeyCode.Alpha2)) {
			usingPistol = false;
		}
		if(Input.GetKeyDown(KeyCode.E)){
			ShootRay();
		}
		//PROCESSING ROTATION
		Vector3 aimPoint =  Camera.main.transform.forward*10f;
		if(canRotate){
			Quaternion targetRotation = Quaternion.LookRotation(aimPoint);
			this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 4* Time.deltaTime);
			this.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
		}
		//JUMP OVER OBSTACLE
		Vector3 ahead = transform.forward;
		Vector3 rayStart = new Vector3(this.transform.position.x, this.transform.position.y+1f, this.transform.position.z);
		Ray	ray = new Ray(rayStart, ahead);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 1f)) {
			if (hit.transform.gameObject.tag == ("wall")) {
				if ((Input.GetButtonDown ("Jump"))&&(!_jumpOverObstcle)) {
					_jumpOverObstcle = true;
				} else {
					_jumpOverObstcle = false;
				}
			}
		}
/////////////LADDER LOGIC
		if((Input.GetKeyDown(KeyCode.E))&&(canClimb)&&(!onLadder)){ //if E button is pressed and we're near the ladder
			usingPistol = false;
			onLadder=true;
			canRotate = false;
			this.transform.parent = ladder.transform;// parent player to ladder
			//this.transform.localPosition = Vector3.zero;
			this.transform.rotation = ladder.transform.rotation;//face player to ladder
			this.transform.position = new Vector3(ladder.transform.position.x, this.transform.position.y, ladder.transform.position.z);//adding small offset to player on z axis
			//moveDirection = Vector3.zero;//zeroing players moveDirection
			_animator.SetTrigger("climb");
		}
		if ((Input.GetAxis ("Vertical") < 0) && (canClimbOff)) {
			onLadder = false;
			canRotate = true;
			this.transform.parent = null;

		}
	}
	void FixedUpdate(){
		_animator.SetFloat("mouseX",_mouseX, 0.3f, Time.deltaTime);
		_animator.SetFloat ("Speed", _speed);
		_animator.SetFloat("Strafe", _strafe);
		_animator.SetBool("Run", _run);
		_animator.SetBool("crouch", _crouch);
		_animator.SetBool("JumpOverObstcle", _jumpOverObstcle);
		_animator.SetBool("onLadder", onLadder);
		_animator.SetBool("usingPistol", usingPistol);
	}
	//SHOOTING LOGIC
	void ShootRay(){
		float x = Screen.width / 2;
		float y = Screen.height / 2;
		Ray ray = Camera.main.ScreenPointToRay (new Vector3 (x, y, 0));
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 10)) {
			if (hit.transform.gameObject.tag == ("cloth")) {
				float distance = Vector3.Distance(this.transform.position, hit.point);
				if(distance<2 && canRotate){
				textureToChange = hit.transform.gameObject.GetComponent<MeshRenderer>().material.mainTexture;
				Debug.Log (hit.transform.gameObject.tag);
				StartCoroutine(ChangeCloth());
					hit.transform.gameObject.GetComponent<MeshRenderer>().material.mainTexture = currTexture;
				}
			}
		}
		if (Physics.Raycast (ray, out hit, Mathf.Infinity)) {
			if(hit.transform.GetComponent<CharacterController>()){
				Debug.Log ("ENEMY");
			}
		}
	}
	void TurnOffCollider (){
		_charCtrl.enabled = false;
		canRotate = false;
	}
	void TurnOnCollider (){
		_charCtrl.enabled = true;
		_jumpOverObstcle = false;
		canRotate = true;
	}
	//////////ON TRIGGER ENTER FUNCTION
	void OnTriggerEnter(Collider trigg){
		if(trigg.gameObject.name == "ladder"){//if we've triggered a ladder
			canClimb = true;//tell player we can now climb
			ladder = trigg.transform.Find("ladderAligner");//and triggered ladder is THE ladder we're going to climb on
		}
		if(trigg.gameObject.name == "ladderBottom"){//if we've triggered the bottom of the ladder
			canClimbOff = true;//tall player we can climb off
		}
		if (trigg.gameObject.name == "ladderTop") {
			if(onLadder){
				_animator.SetTrigger("ClimbOff");
			}
		}
	}
	//////////ON TREGGER EXIT FUNCTION
	void OnTriggerExit(Collider trigg){
		if (trigg.gameObject.name == "ladder") {//if we've left ladder's trigger zone
			canClimb = false; 
			onLadder = false;
			ladder = null;//zeroing ladder transform
			this.transform.parent = null;//deparenting player from ladder
		}
		if (trigg.gameObject.name == "ladderBottom") {
			canClimbOff = false;
		}
	}

	public IEnumerator ChangeCloth(){
		usingPistol = false;
		_animator.SetTrigger("changeClth");
		yield return new WaitForSeconds(2.0f);
		myBody.GetComponent<SkinnedMeshRenderer>().material.mainTexture = textureToChange;
		currTexture = textureToChange;
	}
	//FOOTSTEP SOUND FUNCTION IS CALLED FROM ANIMATION EVENT
	void footStep(){
		if(waterWalk){
			int n = UnityEngine.Random.Range (0, m_FootstepSoundsWater.Length);
			m_AudioSource.clip = m_FootstepSoundsWater [n];
			m_AudioSource.PlayOneShot (m_AudioSource.clip);
			// move picked sound to index 0 so it's not picked next time
			m_FootstepSoundsWater [n] = m_FootstepSoundsWater [0];
			m_FootstepSoundsWater [0] = m_AudioSource.clip;
		}
		else{
			int n = UnityEngine.Random.Range (0, m_FootstepSoundsGround.Length);
			m_AudioSource.clip = m_FootstepSoundsGround [n];
			m_AudioSource.PlayOneShot (m_AudioSource.clip);
			// move picked sound to index 0 so it's not picked next time
			m_FootstepSoundsGround [n] = m_FootstepSoundsGround [0];
			m_FootstepSoundsGround [0] = m_AudioSource.clip;
		}
	}
}
