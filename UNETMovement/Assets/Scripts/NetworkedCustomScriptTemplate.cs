using UnityEngine;
using UnityEngine.Networking;

public class NetworkedCustomScriptTemplate : NetworkBehaviour
{
    [ServerCallback]
    public void ServerStuff(GameObject newObject, Vector3 newObjectPosition)
    {
        //Do server-side stuff. Initializing all game stuffs can be used here. Syncing objects can also be used here.

        //Let's pretend "newObject" has NetworkTransform attached.
        newObject.transform.position = newObjectPosition;

        //Now newObject's NetworkTransform will sync the new position across all connected clients. The values are updated on the server
        //side, all clients will have their old positions marked as Dirty() and will update to the server's values.
    }

    [ClientCallback]
    public void ClientStuff()
    {
        //Do client-side stuff.

        //Client may have this registered prefab from the Network Manager, but usually it's better to have it available in the Hierarchy before continuing. Usually, it's assigned directly through the Inspector, but let's go with this.
        GameObject prefabObject = GameObject.FindGameObjectWithTag("SamplePrefab");
        if (prefabObject != null)
        {
            //Always check for null. In UNET, there may be times where the object is not spawned through NetworkServer, thus the object will be missing.
            CmdDoAction(prefabObject);
        }
    }

    [Command]
    public void CmdDoAction(GameObject objectToUse)
    {
        //This is where you would do stuffs that the client wants the server to do.
        GameObject obj = Instantiate<GameObject>(objectToUse);
        NetworkServer.SpawnWithClientAuthority(obj, this.connectionToClient);

        //Or just have the server do something for the client.
        obj = Instantiate<GameObject>(objectToUse);
        NetworkServer.Spawn(obj);
        ServerStuff(obj, new Vector3(0f, 10f, 0f));

        //Once the server has done whatever it needs to do, you can choose if you want to have all clients do other actions.
        RpcDoAction();
    }

    [ClientRpc]
    public void RpcDoAction()
    {
        //Do broadcasting stuffs. Stuffs where all clients (local clients = LAN host, and remote clients = LAN clients) will do.

        //Because there exists a game object with client authority (meaning the game object's NetworkIdentity has "Local Player Authority" property
        //enabled), it means it's better to check for NetworkBehaviour.hasAuthority, rather than NetworkBehaviour.isLocalPlayer.
        if (!this.hasAuthority)
        {
            //Clients with no authority do stuffs here.

            //Or if you want, you can do something that all other clients without authority of this game object can do.
            Ping(new Vector3(0f, 10f, 0f), Color.red);
        }
        else {
            //Do something in which the client itself has control over the game object of local authority.

            //Then do whatever you need to do that tells the player the unit has spawned, and all other enemies have taken notice of it.
            Ping(new Vector3(0f, 10f, 0f), Color.green);
        }
        return;
    }

    public void Ping(Vector3 position, Color color)
    {
        //Pretend this is a function which pings a colored ! symbol on a minimap.
    }

    public void Update()
    {
        //Because this is a custom NetworkBehaviour script, this is attached to a game object that the client may use.
        if (Input.GetMouseButtonUp(0))
        {
            //When something occurred...
            ClientStuff();
        }
    }
}