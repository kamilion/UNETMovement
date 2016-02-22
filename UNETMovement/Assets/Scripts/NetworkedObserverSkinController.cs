using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class NetworkedObserverSkinController : NetworkBehaviour
{
    // Discovered by the script automatically.
    [HideInInspector] public NetworkedRouterOfComponentsForObserver router; // The router to all of the other components we need.
    // Set these in the inspector.
    [Header("This observer's meshes")]
    public GameObject m_BodyMesh;
    public GameObject m_HeadMesh;

    // These may be manipulated by other scripts.
    [Header("--Internal State--")]
    public Texture m_NewBodySkin;
    public Texture m_CurrentBodySkin;
    public Texture m_NewHeadSkin;
    public Texture m_CurrentHeadSkin;


    void Start()
    {
        router = GetComponentInChildren<NetworkedRouterOfComponentsForObserver>(); // Grab a reference to the ComponentsList.
        // Grab the skinned mesh renderer attached to this object.
        m_CurrentBodySkin = m_BodyMesh.GetComponent<SkinnedMeshRenderer>().material.mainTexture;
        m_CurrentHeadSkin = m_HeadMesh.GetComponent<SkinnedMeshRenderer>().material.mainTexture;
    }


    public IEnumerator ChangeClothes()
    {
        // usingWeapon = false; // holster weapon here.
        router.animator.SetTrigger("ChangeClothes");
        yield return new WaitForSeconds(2.0f);
        m_BodyMesh.GetComponent<SkinnedMeshRenderer>().material.mainTexture = m_NewBodySkin;
        m_CurrentBodySkin = m_NewBodySkin;
    }

    public IEnumerator ChangeHead()
    {
        // usingWeapon = false; // holster weapon here.
        // router.animator.SetTrigger("ChangeClothes");
        yield return new WaitForSeconds(0.1f);
        m_HeadMesh.GetComponent<SkinnedMeshRenderer>().material.mainTexture = m_NewHeadSkin;
        m_CurrentHeadSkin = m_NewHeadSkin;
    }
}
