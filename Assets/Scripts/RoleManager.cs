using UnityEngine;
using System.Collections;
using Unity.Netcode;


public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance;

    [Header("Ajustes")]
    [SerializeField] private float bloqSegundos = 3f;

    //Guardamos que es el que busca (clientId)
    public NetworkVariable<ulong> seekerId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private new void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[RoleManager][SERVER] Client connected: {clientId}. seekerId={seekerId.Value}");

        if (seekerId.Value == ulong.MaxValue)
        {
            seekerId.Value = clientId;
            Debug.Log($"[RoleManager][SERVER] Seeker assigned -> {seekerId.Value}");
        }
        
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CatchPlayerServerRpc(ulong catcherClientId, ulong caughtClientId)
    {
        Debug.Log($"[RoleManager][SERVER] CatchPlayerServerRpc catcher={catcherClientId} caught={caughtClientId} seeker={seekerId.Value}");
        if (catcherClientId != seekerId.Value)
        {
            return;
        }

        if (caughtClientId == seekerId.Value)
        {
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(caughtClientId))
        {
            return;
        }

        var caughtObj = NetworkManager.Singleton.ConnectedClients[caughtClientId].PlayerObject;
        if (caughtObj != null)
        {
            var caughtNet = caughtObj.GetComponent<PlayerNetcode>();
            if (caughtNet != null)
            {
                caughtNet.vecesPillado.Value += 1;
                Debug.Log($"[RoleManager][SERVER] vecesPillado++ for clientId={caughtClientId} -> {caughtNet.vecesPillado.Value}");
                StopAllCoroutines();
                seekerId.Value = caughtClientId;
                Debug.Log($"[RoleManager][SERVER] Seeker cambiado -> {seekerId.Value}");
                StartCoroutine(StunCorountine(caughtNet));
            }
        }
    }

    private IEnumerator StunCorountine(PlayerNetcode target)
    {
        if (target == null)
        {
            yield break;
        }
        Debug.Log($"[RoleManager][SERVER] STUN ON clientId={target.OwnerClientId} for {bloqSegundos}s");
        target.movBloqueado.Value = true;

        yield return new WaitForSeconds(bloqSegundos);

        target.movBloqueado.Value = false;
        Debug.Log($"[RoleManager][SERVER] STUN OFF clientId={target.OwnerClientId}");



    }
}
