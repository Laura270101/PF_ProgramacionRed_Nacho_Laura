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
        if (!IsOwner) return;
        if (cd > 0f) return;
        if (RoleManager.Instance == null) return;

        // Solo seeker
        if (RoleManager.Instance.seekerId.Value != OwnerClientId) return;

        NetworkObject otherNetObj = other.GetComponentInParent<NetworkObject>();
        if (otherNetObj == null) return;
        if (otherNetObj.OwnerClientId == OwnerClientId) return;

        cd = cooldown;

        Debug.Log($"[PillarTrigger] TRY CATCH -> seeker={OwnerClientId} caught={otherNetObj.OwnerClientId}");

        // CAMBIO: USAMOS LA FUNCIÓN CENTRAL
        if (NetworkManager.Singleton.IsServer)
        {
            RoleManager.Instance.ProcessCatchServer(OwnerClientId, otherNetObj.OwnerClientId);
        }
        else
        {
            // Si quieres mantener esto en cliente, habría que hacer un RPC al server,
            // pero como ya estás avanzando con HideSpots, puedes dejar este trigger sin usar.
        }
    }
}
