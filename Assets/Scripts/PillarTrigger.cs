using UnityEngine;
using Unity.Netcode;

public class PillarTrigger : NetworkBehaviour
{
    [SerializeField] private float cooldown = 0.3f;
    private float cd = 0f;

    private void Update()
    {
        if (cd > 0f) cd -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo el owner ejecuta la detección (para no duplicar llamadas)
        if (!IsOwner) return;

        if (cd > 0f) return;
        if (RoleManager.Instance == null) return;

        // Solo el seeker puede pillar por contacto
        if (RoleManager.Instance.seekerId.Value != OwnerClientId) return;

        NetworkObject otherNetObj = other.GetComponentInParent<NetworkObject>();
        if (otherNetObj == null) return;

        if (otherNetObj.OwnerClientId == OwnerClientId) return;

        // Si el otro está escondido, NO se puede pillar por contacto
        var otherPlayerNet = otherNetObj.GetComponent<PlayerNetcode>();
        if (otherPlayerNet != null && otherPlayerNet.isHidden.Value) return;

        cd = cooldown;

        Debug.Log($"[PillarTrigger] TOUCH CATCH -> seeker={OwnerClientId} caught={otherNetObj.OwnerClientId}");

        // Pedimos al servidor que procese la pillada
        RequestTouchCatchServerRpc(otherNetObj.OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestTouchCatchServerRpc(ulong caughtClientId)
    {
        if (!IsServer) return;
        if (RoleManager.Instance == null) return;

        RoleManager.Instance.ProcessCatchServer(OwnerClientId, caughtClientId);
    }
}
