using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetcode : NetworkBehaviour
{
    public enum NetEstado : int { IDLE = 0, WALK = 1}

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hide")]
    public NetworkVariable<bool> isHidden = new NetworkVariable<bool>(
    false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> hideTimeLeft = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Server -> Everyone
    public NetworkVariable<int> skinID = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> estado = new NetworkVariable<int>(
        (int)NetEstado.IDLE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> flipX = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //Contador de veces atrapado:
    public NetworkVariable<int> vecesPillado = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //Bloqueo movimiento si te pillan
    public NetworkVariable<bool> movBloqueado = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetEstado lastSentEstado = NetEstado.IDLE;
    private bool lastSentFlip = false;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerNetcode] OnNetworkSpawn -> IsServer={IsServer} IsOwner={IsOwner} OwnerClientId={OwnerClientId}");
        // Control local solo al owner y si no esta bloqueado
        RefreshControl();
        movBloqueado.OnValueChanged += (_, __) => RefreshControl();

        // Aplicar skin cuando cambie (para todos)
        skinID.OnValueChanged += (_, newVal) => playerMovement.ApplySkin(newVal);
        playerMovement.ApplySkin(skinID.Value);

        // Aplicar visuales cuando cambie estado/flip (para todos)
        estado.OnValueChanged += (_, __) => ApplyRemoteVisual();
        flipX.OnValueChanged += (_, __) => ApplyRemoteVisual();
        ApplyRemoteVisual();
    }

    private void RefreshControl()
    {
        if (playerMovement == null)
        {
            return;
        }
        bool can = IsOwner && !movBloqueado.Value;
        playerMovement.canControl = true;

        Debug.Log($"[PlayerNetcode] RefreshControl -> owner={IsOwner} server={IsServer} clientId={OwnerClientId} locked={movBloqueado.Value} canControl={can}");
    }

    private void Update()
    {
        // Solo el owner reporta estado/flip al server
        if (!IsOwner) return;

        if (movBloqueado.Value)
        {
            SendEstado(NetEstado.IDLE, spriteRenderer.flipX);
            return;
        }

        // Sincronizar flip por red para que los demás vean hacia dónde miras
        // (tu PlayerMovement ya hace flip al moverse; aquí solo lo leemos)
        bool currentFlip = spriteRenderer.flipX;

        

        // Si se mueve, WALK; si no, IDLE
        float hh = Input.GetAxisRaw("Horizontal");
        float vv = Input.GetAxisRaw("Vertical");
        bool isMoving = (hh * hh + vv * vv) > 0.01f;

        SendEstado(isMoving ? NetEstado.WALK : NetEstado.IDLE, currentFlip);
    }

    private void SendEstado(NetEstado e, bool fx)
    {
        // Evitar spamear RPC si no cambia nada
        if (e == lastSentEstado && fx == lastSentFlip) return;
        lastSentEstado = e;
        lastSentFlip = fx;

        SubmitStateServerRpc((int)e, fx);
    }

    [ServerRpc]
    private void SubmitStateServerRpc(int newEstado, bool newFlipX)
    {
        estado.Value = newEstado;
        flipX.Value = newFlipX;
    }

    private void ApplyRemoteVisual()
    {
        // Flip siempre
        spriteRenderer.flipX = flipX.Value;

        // ===== ESTADO ESCONDIDO =====
        if (isHidden.Value)
        {
            // Parpadeo rojo
            float t = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, t);

            // Si YO soy el seeker, no veo a los escondidos
            if (RoleManager.Instance != null &&
                RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId)
            {
                spriteRenderer.enabled = false;
            }
            else
            {
                spriteRenderer.enabled = true;
            }

            return; // no aplicamos IDLE/WALK
        }

        // ===== NO ESCONDIDO =====
        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.white;

        // Estado normal
        if (IsOwner) return;

        switch ((NetEstado)estado.Value)
        {
            case NetEstado.IDLE:
                playerMovement.ForceIdleVisual();
                break;
            case NetEstado.WALK:
                playerMovement.ForceWalkVisual();
                break;
        }
    }

    private Coroutine hideRoutine;

    [ServerRpc]
    public void EnterHideServerRpc()
    {
        if (isHidden.Value) return;

        isHidden.Value = true;
        hideTimeLeft.Value = 5f;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideCountdown());
    }

    private IEnumerator HideCountdown()
    {
        while (hideTimeLeft.Value > 0f)
        {
            yield return new WaitForSeconds(1f);
            hideTimeLeft.Value -= 1f;
        }

        isHidden.Value = false;
        hideTimeLeft.Value = 0f;
    }
}
