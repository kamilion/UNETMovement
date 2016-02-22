using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

//[RequireComponent(typeof(Animator))]

public class NetworkedDude : NetworkBehaviour
{
    // Discovered by the script automatically.
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    public Transform ladder;//current ladder 
    public bool _jump; //jump button press detection
    public bool _jumpOverObstacle;
    public bool canClimb;
    public bool canClimbOff;
    public bool onLadder; //player is on the ladder
    public bool usingWeapon;

    public bool _run;
    public bool _crouch;
    public bool canRotate = true;
    public bool alive = true;
    public bool waterWalk;

    public float _speed;
    public float _strafe;
    public float gravity; //gravity force 
    public float health;

    private float _mouseX;
    private float tapTimeWindow;

    // Use this for initialization
    void Start()
    {
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
    }

    void Update()
    {
        _mouseX = Input.GetAxis("Mouse X");
        _speed = Input.GetAxis("Vertical"); //reading vertical axis input
        _strafe = Input.GetAxis("Horizontal"); //reading horizontal axis input
        _run = Input.GetKey(KeyCode.LeftShift) ? true : false; //check if run button was pressed
        _crouch = Input.GetKey(KeyCode.C) ? true : false; //check if crouch button was pressed
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            usingWeapon = true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            usingWeapon = false;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ShootRay();
        }
        //PROCESSING ROTATION
        Vector3 aimPoint = Camera.main.transform.forward * 10f;
        if (canRotate)
        {
            Quaternion targetRotation = Quaternion.LookRotation(aimPoint);
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 4 * Time.deltaTime);
            this.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
        //JUMP OVER OBSTACLE
        Vector3 ahead = transform.forward;
        Vector3 rayStart = new Vector3(this.transform.position.x, this.transform.position.y + 1f, this.transform.position.z);
        Ray ray = new Ray(rayStart, ahead);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1f))
        {
            if (hit.transform.gameObject.tag == ("wall"))
            {
                if ((Input.GetButtonDown("Jump")) && (!_jumpOverObstacle))
                {
                    _jumpOverObstacle = true;
                }
                else {
                    _jumpOverObstacle = false;
                }
            }
        }
        /////////////LADDER LOGIC
        if ((Input.GetKeyDown(KeyCode.E)) && (canClimb) && (!onLadder))
        { //if E button is pressed and we're near the ladder
            usingWeapon = false;
            onLadder = true;
            canRotate = false;
            this.transform.parent = ladder.transform;// parent player to ladder
                                                     //this.transform.localPosition = Vector3.zero;
            this.transform.rotation = ladder.transform.rotation; //face player to ladder
            this.transform.position = new Vector3(ladder.transform.position.x, this.transform.position.y, ladder.transform.position.z);//adding small offset to player on z axis
                                                                                                                                       //moveDirection = Vector3.zero;//zeroing players moveDirection
            router.animator.SetTrigger("Climbing");
        }
        if ((Input.GetAxis("Vertical") < 0) && (canClimbOff))
        {
            onLadder = false;
            canRotate = true;
            this.transform.parent = null;

        }
    }
    // FixedUpdate usually occurs after Update is processed.
    void FixedUpdate()
    {
        //router.animator.SetFloat("mouseX", _mouseX, 0.3f, Time.deltaTime);
        //router.animator.SetFloat("Speed", _speed);
        //router.animator.SetFloat("Strafe", _strafe);
        //router.animator.SetBool("Run", _run);
        //router.animator.SetBool("Crouch", _crouch);
        //router.animator.SetBool("JumpOverObstacle", _jumpOverObstacle);
        //router.animator.SetBool("OnLadder", onLadder);
        //router.animator.SetBool("UsingWeapon", usingWeapon);
    }

    //SHOOTING LOGIC
    void ShootRay()
    {
        float x = Screen.width / 2;
        float y = Screen.height / 2;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10))
        {
            if (hit.transform.gameObject.tag == ("cloth"))
            {
                float distance = Vector3.Distance(this.transform.position, hit.point);
                if (distance < 2 && canRotate)
                {
                    router.skin.m_NewBodySkin = hit.transform.gameObject.GetComponent<MeshRenderer>().material.mainTexture;
                    Debug.Log(hit.transform.gameObject.tag);
                    StartCoroutine(router.skin.ChangeClothes());
                    hit.transform.gameObject.GetComponent<MeshRenderer>().material.mainTexture = router.skin.m_CurrentBodySkin;
                }
            }
        }
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (hit.transform.GetComponent<CharacterController>())
            {
                Debug.Log("ENEMY");
            }
        }
    }

    void TurnOffCollider()
    {
        router.characterController.enabled = false;
        canRotate = false;
    }
    void TurnOnCollider()
    {
        router.characterController.enabled = true;
        _jumpOverObstacle = false;
        canRotate = true;
    }

    //////////ON TRIGGER ENTER FUNCTION
    void OnTriggerEnter(Collider trigg)
    {
        if (trigg.gameObject.name == "ladder")
        {// if we've triggered a ladder
            canClimb = true; // tell player we can now climb
            ladder = trigg.transform.Find("ladderAligner"); // and triggered ladder is THE ladder we're going to climb on
        }
        if (trigg.gameObject.name == "ladderBottom")
        { // if we've triggered the bottom of the ladder
            canClimbOff = true; // tell player we can climb off
        }
        if (trigg.gameObject.name == "ladderTop")
        {
            if (onLadder)
            {
                router.animator.SetTrigger("AtLadderCrest");
            }
        }
    }
    //////////ON TRIGGER EXIT FUNCTION
    void OnTriggerExit(Collider trigg)
    {
        if (trigg.gameObject.name == "ladder")
        {   //if we've left ladder's trigger zone
            canClimb = false;
            onLadder = false;
            ladder = null; //zeroing ladder transform
            this.transform.parent = null; //deparenting player from ladder
        }
        if (trigg.gameObject.name == "ladderBottom")
        {
            canClimbOff = false;
        }
    }

}
