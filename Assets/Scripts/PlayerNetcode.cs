using System.Globalization;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetcode : NetworkBehaviour
{
    public enum NetEstado : int { IDLE = 0, WALK = 1 }

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ===== NET VARS =====

    public NetworkVariable<int> skinID = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> estado = new NetworkVariable<int>(
        (int)NetEstado.IDLE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> flipX = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> vecesPillado = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> movBloqueado = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // CAMBIO: ESTADO DE ESCONDIDO POR RED
    public NetworkVariable<bool> isHidden = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // CAMBIO: EN QUÉ HIDESPOT ESTÁ (NetObjectId). ulong.MaxValue = ninguno
    public NetworkVariable<ulong> currentHideSpotId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ===== LOCAL STATE (no net) =====
    private NetEstado lastSentEstado = NetEstado.IDLE;
    private bool lastSentFlip = false;

    // CAMBIO: GUARDAMOS EL HIDESPOT CERCANO EN LOCAL
    private ulong nearHideSpotId = ulong.MaxValue;

    // CAMBIO: GUARDAMOS VISUAL BASE PARA RESTAURAR
    private string baseSortingLayer;
    private int baseSortingOrder;
    private Color baseColor;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        baseSortingLayer = spriteRenderer.sortingLayerName;
        baseSortingOrder = spriteRenderer.sortingOrder;
        baseColor = spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        // CAMBIO: REFRESH CONTROL CORRECTO
        RefreshControl();
        movBloqueado.OnValueChanged += (_, __) => RefreshControl();
        isHidden.OnValueChanged += (_, __) => RefreshControl();

        // Aplicar skin para todos
        skinID.OnValueChanged += (_, newVal) => playerMovement.ApplySkin(newVal);
        playerMovement.ApplySkin(skinID.Value);

        // Visuales remotos
        estado.OnValueChanged += (_, __) => ApplyRemoteVisual();
        flipX.OnValueChanged += (_, __) => ApplyRemoteVisual();

        // CAMBIO: CUANDO CAMBIA currentHideSpotId / isHidden, actualizamos visual de esconder
        currentHideSpotId.OnValueChanged += (_, __) => ApplyHideVisual();
        isHidden.OnValueChanged += (_, __) => ApplyHideVisual();

        ApplyRemoteVisual();
        ApplyHideVisual();
    }

    private void RefreshControl()
    {
        if (playerMovement == null) return;

        bool can = IsOwner && !movBloqueado.Value && !isHidden.Value;

        // CAMBIO: ANTES ESTABA EN TRUE SIEMPRE. AHORA USA "can"
        playerMovement.canControl = can;

        Debug.Log($"[PlayerNetcode] RefreshControl -> owner={IsOwner} locked={movBloqueado.Value} hidden={isHidden.Value} canControl={can}");
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Si estoy escondido o bloqueado, fuerzo IDLE por red
        if (movBloqueado.Value || isHidden.Value)
        {
            SendEstado(NetEstado.IDLE, spriteRenderer.flipX);
            return;
        }

        bool currentFlip = spriteRenderer.flipX;

        float hh = Input.GetAxisRaw("Horizontal");
        float vv = Input.GetAxisRaw("Vertical");
        bool isMoving = (hh * hh + vv * vv) > 0.01f;

        SendEstado(isMoving ? NetEstado.WALK : NetEstado.IDLE, currentFlip);
    }

    private void SendEstado(NetEstado e, bool fx)
    {
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
        spriteRenderer.flipX = flipX.Value;

        // CAMBIO: SI ESTOY ESCONDIDO, NO FORZAMOS WALK/IDLE AQUÍ
        if (isHidden.Value) return;

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

    // ====== HIDESPOT (LOCAL) ======

    // CAMBIO: HideSpot llama a esto SOLO en local para decir "estás cerca"
    public void SetNearHideSpotClient(ulong hideSpotId, bool near)
    {
        if (!IsOwner) return;

        if (near) nearHideSpotId = hideSpotId;
        else if (nearHideSpotId == hideSpotId) nearHideSpotId = ulong.MaxValue;
    }

    public bool HasNearHideSpot => nearHideSpotId != ulong.MaxValue;

    // ====== HIDESPOT (SERVER REQUESTS) ======

    // CAMBIO: ENTRAR SIENDO HIDER
    [ServerRpc]
    public void RequestEnterHideServerRpc(ulong hideSpotId)
    {
        if (!IsServer) return;

        // seeker no puede esconderse
        if (RoleManager.Instance != null && RoleManager.Instance.seekerId.Value == OwnerClientId) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(hideSpotId, out var netObj)) return;

        var spot = netObj.GetComponent<HideSpot>();
        if (spot == null) return;

        // Si está ocupado, nada
        if (spot.occupantId.Value != ulong.MaxValue) return;

        // ocupar
        spot.occupantId.Value = OwnerClientId;

        // marcar player
        isHidden.Value = true;
        currentHideSpotId.Value = hideSpotId;
    }

    // CAMBIO: SALIR SIENDO HIDER
    [ServerRpc]
    public void RequestExitHideServerRpc()
    {
        if (!IsServer) return;

        if (!isHidden.Value) return;
        if (currentHideSpotId.Value == ulong.MaxValue) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(currentHideSpotId.Value, out var netObj))
        {
            var spot = netObj.GetComponent<HideSpot>();
            if (spot != null && spot.occupantId.Value == OwnerClientId)
            {
                spot.occupantId.Value = ulong.MaxValue;
            }
        }

        isHidden.Value = false;
        currentHideSpotId.Value = ulong.MaxValue;
    }

    // CAMBIO: SEEKER REVELA HIDESPOT (SACA AL QUE ESTE DENTRO)
    [ServerRpc]
    public void RequestRevealHideSpotServerRpc(ulong hideSpotId)
    {
        if (!IsServer) return;
        if (RoleManager.Instance == null) return;

        // Solo seeker
        if (RoleManager.Instance.seekerId.Value != OwnerClientId) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(hideSpotId, out var netObj)) return;

        var spot = netObj.GetComponent<HideSpot>();
        if (spot == null) return;

        // si está vacío nada
        if (spot.occupantId.Value == ulong.MaxValue) return;

        ulong caughtId = spot.occupantId.Value;

        // liberar spot
        spot.occupantId.Value = ulong.MaxValue;

        // expulsar al pillado
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(caughtId, out var client) && client.PlayerObject != null)
        {
            var pNet = client.PlayerObject.GetComponent<PlayerNetcode>();
            if (pNet != null)
            {
                pNet.isHidden.Value = false;
                pNet.currentHideSpotId.Value = ulong.MaxValue;
            }
        }

        // procesar catch (cambia seeker, suma contador, stun, etc.)
        RoleManager.Instance.ProcessCatchServer(OwnerClientId, caughtId);
    }

    // ===== VISUAL / POSICION AL ESCONDERSE =====
    private void ApplyHideVisual()
    {
        if (!isHidden.Value)
        {
            // restaurar visual normal
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseColor;
            spriteRenderer.sortingLayerName = baseSortingLayer;
            spriteRenderer.sortingOrder = baseSortingOrder;
            return;
        }

        // Si estoy escondido:
        // - el seeker NO debe ver al escondido (renderer off en su cliente)
        // - los hiders sí lo ven (renderer on)
        if (RoleManager.Instance != null && NetworkManager.Singleton != null)
        {
            bool iAmSeekerLocal = RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId;
            spriteRenderer.enabled = !iAmSeekerLocal;
        }

        // CAMBIO: SNAP A POSICION DEL HIDESPOT (EN TODOS, ASÍ QUEDA DENTRO)
        if (currentHideSpotId.Value != ulong.MaxValue &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(currentHideSpotId.Value, out var netObj))
        {
            var spot = netObj.GetComponent<HideSpot>();
            if (spot != null)
            {
                transform.position = spot.HideWorldPos;

                // poner al jugador "detrás" del spot
                spriteRenderer.sortingLayerName = spot.SortingLayer;
                spriteRenderer.sortingOrder = spot.SortingOrder - 1;
            }
        }
    }

    // ===== HELPERS PARA INPUT (LOS LLAMA PlayerMovement) =====
    public void TryEnterNearSpot()
    {
        if (!IsOwner) return;
        if (nearHideSpotId == ulong.MaxValue) return;
        if (isHidden.Value) return;

        RequestEnterHideServerRpc(nearHideSpotId);
    }

    public void TryExitSpot()
    {
        if (!IsOwner) return;
        if (!isHidden.Value) return;

        RequestExitHideServerRpc();
    }

    public void TryRevealNearSpot()
    {
        if (!IsOwner) return;
        if (nearHideSpotId == ulong.MaxValue) return;

        RequestRevealHideSpotServerRpc(nearHideSpotId);
    }
}
