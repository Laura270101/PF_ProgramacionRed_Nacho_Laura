using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(NetworkObject))]
public class HideSpot : NetworkBehaviour
{
    [Header("PULSO SOLO PARA HIDER (alpha)")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float minAlpha = 0.35f;

    [Range(0.1f, 1f)]
    [SerializeField] private float maxAlpha = 1.0f;

    [SerializeField] private float pulseSpeed = 2.0f;

    [Header("SI ESTA OCUPADO, PARPADEA MAS FUERTE (HIDER)")]
    [SerializeField] private float occupiedPulseBoost = 1.5f;

    [Header("POSICION DONDE SE METE EL JUGADOR (opcional)")]
    [SerializeField] private Vector3 hideOffset = Vector3.zero;

    private SpriteRenderer sr;
    private Color baseColor;

    // ulong.MaxValue = vacío
    public NetworkVariable<ulong> occupantId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ulong NetId => NetworkObjectId;

    public Vector3 HideWorldPos => transform.position + hideOffset;

    public int SortingOrder => sr != null ? sr.sortingOrder : 0;
    public string SortingLayer => sr != null ? sr.sortingLayerName : "Default";

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;

        // CAMBIO: Asegurar que el trigger está activo
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (RoleManager.Instance == null || NetworkManager.Singleton == null) return;

        bool iAmSeeker = RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId;
        bool occupied = occupantId.Value != ulong.MaxValue;

        if (iAmSeeker)
        {
            // CAMBIO: SEEKER ve normal SIEMPRE
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            return;
        }

        // CAMBIO: HIDER ve pulso de alpha (y más fuerte si está ocupado)
        float speed = pulseSpeed * (occupied ? occupiedPulseBoost : 1f);
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        float a = Mathf.Lerp(minAlpha, maxAlpha, t);

        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }

    // CAMBIO: SOLO PARA DETECTAR "ESTOY CERCA" EN LOCAL (NO RPC AQUÍ)
    private void OnTriggerEnter2D(Collider2D other)
    {
        var pNet = other.GetComponentInParent<PlayerNetcode>();
        if (pNet != null && pNet.IsOwner)
        {
            pNet.SetNearHideSpotClient(NetId, true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pNet = other.GetComponentInParent<PlayerNetcode>();
        if (pNet != null && pNet.IsOwner)
        {
            pNet.SetNearHideSpotClient(NetId, false);
        }
    }
}
