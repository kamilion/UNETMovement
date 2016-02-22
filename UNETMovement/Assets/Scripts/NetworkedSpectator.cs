using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkedSpectator : NetworkBehaviour {
    public GameObject playerPrefab;

    // // // UnityEngine magic methods
    // Use this for initialization when object is allocated in memory
    public void Awake() { Debug.Log(gameObject.name + "|NetworkedSpectator::Awake: NetworkedSpectator Has Awakened."); }

    // Use this for initialization when object is spawned into the worldspace
    public void Start() {
        Debug.Log(gameObject.name + "|NetworkedSpectator::Start: Renaming NetworkedSpectator to " + netId + ".");
        gameObject.name = "NetworkedSpectator for " + netId;
        Debug.Log(gameObject.name + "|NetworkedSpectator::Start: NetworkedSpectator has finished starting.");
    }

    // // // UnityEngine.Networking magic methods
    public override void OnStartServer() { Debug.Log (gameObject.name + "|NetworkedSpectator::OnServerStart: Server Has Started."); }

    public override void OnStartClient() { Debug.Log (gameObject.name + "|NetworkedSpectator::OnStartClient: Client Has Started."); }

    public override void OnStartAuthority() { Debug.Log (gameObject.name + "|NetworkedSpectator::OnStartAuthority: Authority status: " + hasAuthority + " and isServer: " + isServer + " and isClient: " + isClient + " and isLocalPlayer: " + isLocalPlayer ); }

    public override void OnStopAuthority() { Debug.Log (gameObject.name + "|NetworkedSpectator::OnStopAuthority: Authority status: " + hasAuthority + " and isServer: " + isServer + " and isClient: " + isClient + " and isLocalPlayer: " + isLocalPlayer ); }

    public override void OnStartLocalPlayer() { Debug.Log (gameObject.name + "|NetworkedSpectator::OnStartLocalPlayer: LocalPlayer Has Started."); }

    // // // User defined methods
    public void Spawn(){
        if (isLocalPlayer) {
            Debug.Log (gameObject.name + "|NetworkedSpectator::Spawn: Trying to spawn as PlayerPrefab Avatar.");
            Cmd_Spawn ();
        }
    }
    [Command]
    void Cmd_Spawn() {
        if (hasAuthority) {
            Transform spawn = NetworkManager.singleton.GetStartPosition ();
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Got Start Position from server.");
            GameObject player = (GameObject)Instantiate (playerPrefab, spawn.position, spawn.rotation);
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Instantiated PlayerPrefab, sending Position and Rotation.");
            player.SendMessage ("SetStartPosition", spawn.position, SendMessageOptions.DontRequireReceiver);
            player.SendMessage ("SetStartRotation", spawn.rotation, SendMessageOptions.DontRequireReceiver);
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Sent Start Position and Rotation, sending ObserverSpawned.");
            player.SendMessage ("ObserverSpawned", player, SendMessageOptions.DontRequireReceiver);
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Sent ObserverSpawned message to initialize SyncLists.");
            NetworkServer.Destroy (gameObject);
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Destroyed NetworkedSpectator.");
            NetworkServer.ReplacePlayerForConnection (connectionToClient, player, playerControllerId);
            Debug.Log (gameObject.name + "|NetworkedSpectator::Cmd_Spawn: Replaced NetworkedSpectator with PlayerPrefab Avatar.");
        }
    }

}
