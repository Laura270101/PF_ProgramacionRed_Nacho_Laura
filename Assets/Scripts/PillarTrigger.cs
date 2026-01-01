using UnityEngine;
using Unity.Netcode;


public class PillarTrigger : NetworkBehaviour
{
    [SerializeField] private float cooldown = 0.3f;
    private float cd = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        if (cd > 0f)
        {
            cd -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner)
        {
            return;
        }

        if (cd > 0f)
        {
            return;
        }

        if (RoleManager.Instance == null)
        {
            return;
        }

        if (RoleManager.Instance.seekerId.Value != OwnerClientId)
        {
            return;
        }

        NetworkObject otherNetObj = other.GetComponentInParent<NetworkObject>();

        if (otherNetObj == null)
        {
            return;
        }

        if (otherNetObj.OwnerClientId == OwnerClientId)
        {
            return;
        }

        cd = cooldown;

        Debug.Log($"[CatchTrigger] TRY CATCH -> seeker={OwnerClientId} caught={otherNetObj.OwnerClientId}");

        RoleManager.Instance.CatchPlayerServerRpc(OwnerClientId, otherNetObj.OwnerClientId);
    }
}
