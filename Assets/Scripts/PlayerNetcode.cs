using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerNetcode : NetworkBehaviour
{
    public enum NetEstado : int { IDLE = 0, WALK = 1, KICK = 2 }

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Kick")]
    [SerializeField] private float kickDuration = 0.35f;

    // Server -> Everyone
    public NetworkVariable<int> skinID = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> estado = new NetworkVariable<int>(
        (int)NetEstado.IDLE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> flipX = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float kickTimerLocal = 0f;
    private NetEstado lastSentEstado = NetEstado.IDLE;
    private bool lastSentFlip = false;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // Control local solo al owner
        playerMovement.canControl = IsOwner;

        // Aplicar skin cuando cambie (para todos)
        skinID.OnValueChanged += (_, newVal) => playerMovement.ApplySkin(newVal);
        playerMovement.ApplySkin(skinID.Value);

        // Aplicar visuales cuando cambie estado/flip (para todos)
        estado.OnValueChanged += (_, __) => ApplyRemoteVisual();
        flipX.OnValueChanged += (_, __) => ApplyRemoteVisual();
        ApplyRemoteVisual();
    }

    private void Update()
    {
        // Solo el owner reporta estado/flip al server
        if (!IsOwner) return;

        // Sincronizar flip por red para que los demás vean hacia dónde miras
        // (tu PlayerMovement ya hace flip al moverse; aquí solo lo leemos)
        bool currentFlip = spriteRenderer.flipX;

        // Detectar si está en kick según tu input (X) y temporizador local.
        // Nota: el input lo gestiona PlayerMovement, pero aquí replicamos la intención visual.
        if (Input.GetKeyDown(KeyCode.X))
        {
            kickTimerLocal = kickDuration;
            SendEstado(NetEstado.KICK, currentFlip);
            return;
        }

        if (kickTimerLocal > 0f)
        {
            kickTimerLocal -= Time.deltaTime;
            if (kickTimerLocal <= 0f)
            {
                // al terminar el kick, volvemos a IDLE/WALK según input
                // (si te estás moviendo, manda WALK)
                var h = Input.GetAxisRaw("Horizontal");
                var v = Input.GetAxisRaw("Vertical");
                var moving = (h * h + v * v) > 0.01f;
                SendEstado(moving ? NetEstado.WALK : NetEstado.IDLE, currentFlip);
            }
            return;
        }

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
        // Aplicamos flip a TODOS (incluido owner, no molesta)
        spriteRenderer.flipX = flipX.Value;

        // Aplicamos estado visual SOLO si NO soy el owner.
        // Porque el owner ya lo ve “en vivo” por su PlayerMovement.
        if (IsOwner) return;

        switch ((NetEstado)estado.Value)
        {
            case NetEstado.IDLE:
                playerMovement.ForceIdleVisual();
                break;
            case NetEstado.WALK:
                playerMovement.ForceWalkVisual();
                break;
            case NetEstado.KICK:
                playerMovement.ForceKickVisual();
                break;
        }
    }
}
