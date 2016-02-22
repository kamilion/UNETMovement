
// Imports from core CLR libraries
using System.Collections;

// Imports from Unity libraries
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Use the reliable channel, send updates every tenth of a second.
[NetworkSettings(channel = 0, sendInterval = 0.1f)]
public class NetworkedGeometryObservesDamage : NetworkBehaviour
{
    public const int m_maxHealth = 100; // This is a constant and will have special optimizations applied to it when compiled.
    public Vector3 m_initialLocation; // Remember our initial position when we woke up.
    public bool m_destroyOnDeath;     // Do we destroy ourselves or respawn?
    public Text m_canvasText;        // A UI text object to display some data
    private const string c_canvasTextPrefix = "Health: ";

    //[SyncVar]
    public int m_currentHealth = m_maxHealth; // Setting a variable to a constant is a fast process.

    // Use this for initialization
    void Awake() { m_initialLocation = transform.position; } // Store our initial position so we can respawn there.

    // Update is called once per frame
    //void Update () {	}

    // Commands send information from a client to be invoked on the server.
    [Command]
    public void Cmd_TakeDamage(int amount) {
        // The server has sole dominion here.
        if (!hasAuthority)
            return;

        Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Cmd_TakeDamage: Tell clients we took " + amount + " damage!");
        // Tell the clients this object should take some damage.
        Rpc_TakeDamage(amount);

    }

    // Server calls this on clients
    [ClientRpc]
    public void Rpc_TakeDamage(int amount) { ApplyDamage(amount); // Decrement some health through ApplyDamage
        Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Rpc_TakeDamage: Server says we took " + amount + " damage!");
    }

    [Server]
    public void Srv_ApplyDamage(int amount)
    {

    }
    
    // Server calls this on itself, All clients will decrement the amount now.
    public void ApplyDamage(int amount) {
        m_currentHealth -= amount; // Decrement some health
        Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Rpc_TakeDamage: Server applied " + amount + " damage!");
        if (m_canvasText != null) { m_canvasText.text = c_canvasTextPrefix + m_currentHealth; } // Write the new values to our canvas text.
        if (m_currentHealth <= 0) { // Blank the canvas and do the dying thing if we need to
            if (m_canvasText != null) { m_canvasText.text = ""; }
            ApplyDeath();
        } 
    }

    // Apply damage can eventually Apply death. Avoid damage to live. Shoot opponents for score.
    public void ApplyDeath()
    {
        Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Rpc_TakeDamage: Server says we took enough damage to be killed!");
        if (m_destroyOnDeath) { Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Rpc_TakeDamage: Goodbye, Cruel World! (Destroying Self)");
            // We are no longer needed, go to the bitbucket for garbage collection. Eventually this should return the object to a pool instead.
            Destroy(gameObject);
        } else { Debug.Log(GetObjectDebugInfo() + "|NetworkedGeometryObservesDamage::Rpc_TakeDamage: Beware, I live! (Fake-Respawned)");
            // Restore the object to full health
            m_currentHealth = m_maxHealth;
            if (m_canvasText != null) { m_canvasText.text = c_canvasTextPrefix + m_currentHealth; } // Write the new values to our canvas text.
            // move back to initial position (for now)
            transform.position = m_initialLocation;
        }
    }

    // A short helper for Debug.Log and uNET status.
    public string GetObjectDebugInfo()
    {
        var thisObjectDebugInfo = gameObject.name + "|S:" + GetShorterAnswerToBool(isServer) + "|A:" + GetShorterAnswerToBool(hasAuthority) + "|C:" + GetShorterAnswerToBool(isClient) + "|L:" + GetShorterAnswerToBool(isLocalPlayer) + "|";
        return thisObjectDebugInfo;
    }

    // Stupid helper for GetObjectDebugInfo
    public string GetShorterAnswerToBool(bool input)
    {
        if (input) { return "Y"; }
        else { return "N"; }
    }

}
