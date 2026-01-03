using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance;

    [Header("Ajustes")]
    [SerializeField] private float bloqSegundos = 3f;

    public NetworkVariable<ulong> seekerId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private new void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
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

    // CAMBIO: FUNCIÓN CENTRAL PARA PROCESAR UNA "PILLADA"
    public void ProcessCatchServer(ulong catcherClientId, ulong caughtClientId)
    {
        if (!IsServer) return;

        Debug.Log($"[RoleManager][SERVER] ProcessCatchServer catcher={catcherClientId} caught={caughtClientId} seeker={seekerId.Value}");

        // Solo el seeker puede pillar
        if (catcherClientId != seekerId.Value) return;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(caughtClientId)) return;

        var caughtObj = NetworkManager.Singleton.ConnectedClients[caughtClientId].PlayerObject;
        if (caughtObj == null) return;

        var caughtNet = caughtObj.GetComponent<PlayerNetcode>();
        if (caughtNet == null) return;

        caughtNet.vecesPillado.Value += 1;
        Debug.Log($"[RoleManager][SERVER] vecesPillado++ for clientId={caughtClientId} -> {caughtNet.vecesPillado.Value}");

        // CAMBIO: CAMBIAR SEEKER AL PILLADO
        seekerId.Value = caughtClientId;
        Debug.Log($"[RoleManager][SERVER] Seeker cambiado -> {seekerId.Value}");

        // CAMBIO: STUN AL NUEVO SEEKER (o al pillado, según tu diseño)
        StopAllCoroutines();
        StartCoroutine(StunCoroutine(caughtNet));
    }

    private IEnumerator StunCoroutine(PlayerNetcode target)
    {
        if (target == null) yield break;

        Debug.Log($"[RoleManager][SERVER] STUN ON clientId={target.OwnerClientId} for {bloqSegundos}s");
        target.movBloqueado.Value = true;

        yield return new WaitForSeconds(bloqSegundos);

        target.movBloqueado.Value = false;
        Debug.Log($"[RoleManager][SERVER] STUN OFF clientId={target.OwnerClientId}");
    }
}
