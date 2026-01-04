using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance;

    [Header("Ajustes")]
    [SerializeField] private float bloqSegundos = 3f;

    [Header("Anti doble-pillada (cuando se cambia seeker)")]
    [SerializeField] private float catchGraceSeconds = 0.6f; // <- AJUSTA (0.5-1 va bien)

    // Guardamos quién es el seeker (clientId)
    public NetworkVariable<ulong> seekerId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Tiempo (ServerTime) hasta el que el seeker NO puede pillar (evita el rebote instantáneo)
    private NetworkVariable<double> nextCatchAllowedTime = new NetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            // También aplicamos grace al primer seeker por seguridad
            nextCatchAllowedTime.Value = NetworkManager.ServerTime.Time + catchGraceSeconds;

            Debug.Log($"[RoleManager][SERVER] Seeker assigned -> {seekerId.Value}");
        }
    }

    // FUNCIÓN CENTRAL PARA PROCESAR UNA "PILLADA"
    public void ProcessCatchServer(ulong catcherClientId, ulong caughtClientId)
    {
        if (!IsServer) return;

        double now = NetworkManager.ServerTime.Time;

        Debug.Log($"[RoleManager][SERVER] ProcessCatchServer catcher={catcherClientId} caught={caughtClientId} seeker={seekerId.Value} now={now:0.00} allowAt={nextCatchAllowedTime.Value:0.00}");

        // Solo el seeker puede pillar
        if (catcherClientId != seekerId.Value) return;

        // Anti rebote: si aún estamos dentro de la ventana de gracia, ignorar
        if (now < nextCatchAllowedTime.Value) return;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(caughtClientId)) return;

        var caughtObj = NetworkManager.Singleton.ConnectedClients[caughtClientId].PlayerObject;
        if (caughtObj == null) return;

        var caughtNet = caughtObj.GetComponent<PlayerNetcode>();
        if (caughtNet == null) return;

        // (Opcional) si el pillado está escondido, aquí podrías ignorarlo.
        // En tu diseño: al escondido se le saca por HideSpot, así que normalmente esto no debería ocurrir.

        caughtNet.vecesPillado.Value += 1;
        Debug.Log($"[RoleManager][SERVER] vecesPillado++ for clientId={caughtClientId} -> {caughtNet.vecesPillado.Value}");

        // Cambiamos seeker al pillado
        seekerId.Value = caughtClientId;
        Debug.Log($"[RoleManager][SERVER] Seeker cambiado -> {seekerId.Value}");

        // Bloqueamos temporalmente “poder pillar” para evitar recatch instantáneo por contacto
        nextCatchAllowedTime.Value = now + catchGraceSeconds;

        // Stun SOLO al nuevo seeker (el pillado)
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
