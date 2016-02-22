using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Networking;

public class UnetDebug : MonoBehaviour {

	/*
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	*/
	void OnGUI()
	{
		Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
	
		int posY = -50;
		int xDiff = 20;
		int yDiff = 15;
		
		GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 100, 20), "netId:" + GetComponent<NetworkIdentity>().netId);	
		posY += yDiff;
		GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "assetId:" + GetComponent<NetworkIdentity>().assetId);	
		posY += yDiff;
		GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "pos: (" + transform.position.x + "," + transform.position.y + ")");
		posY += yDiff;
				
		if (GetComponent<NetworkIdentity>().isLocalPlayer) {
			GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "IsLocalPlayer");
			posY += yDiff;
		}
		if (GetComponent<NetworkIdentity>().isServer) {
			GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "IsServer");
			posY += yDiff;
		}
		
		if (GetComponent<NetworkIdentity>().isClient) {
			GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "IsClient");
			posY += yDiff;
		}
		
		if (GetComponents<NetworkBehaviour>().Length > 0) {
		
			if (!m_ShowBehaviours) {
				if (GUI.Button(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 80, 18), "behaviours")) {
					m_ShowBehaviours = true;
				}
			}
			else
			{
				foreach (NetworkBehaviour beh in GetComponents<NetworkBehaviour>())
				{
					GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "beh: " + beh.GetType().Name);
					posY += yDiff;
					foreach (FieldInfo field in beh.GetType ().GetFields())
					{
						System.Attribute[] markers = (System.Attribute[])field.GetCustomAttributes(typeof(SyncVarAttribute), true);
						if (markers.Length > 0)
						{
							GUI.Label(new Rect(pos.x+xDiff, Screen.height - pos.y + posY, 200, 20), "  Var " + field.Name + "=" + field.GetValue(beh));
							posY += yDiff;
						}
					}
				}
			}
		}
	}
	
	bool m_ShowBehaviours = false;
}

	
	