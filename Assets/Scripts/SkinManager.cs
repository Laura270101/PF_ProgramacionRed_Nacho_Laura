using UnityEngine;
using Unity.Netcode;

public class SkinManager : MonoBehaviour
{
    private int siguienteSkin = 1;


    private void Awake()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        int skin = Mathf.Clamp(siguienteSkin, 1, 5);
        siguienteSkin++;

        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        if (client == null || client.PlayerObject == null)
        {
            return;
        }

        var pNet = client.PlayerObject.GetComponent<PlayerNetcode>();
        if (pNet != null)
        {
            pNet.skinID.Value = skin;
        }
    }
}
