using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class HideSpot : MonoBehaviour
{
    [Header("Hide (alpha cuando te escondes)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float alphaWhenHiding = 0.55f;

    [Header("Visual (parpadeo rojo)")]
    [SerializeField] private Color glowColor = new Color(1f, 0.15f, 0.15f, 1f); // rojo
    [SerializeField] private float pulseSpeed = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.7f;

    private SpriteRenderer sr;

    // Guardamos el color base (sin tocar alpha) y el alpha base
    private Color baseRGB;
    private float baseAlpha;

    // Para que el mensaje salga solo la 1ª vez por jugador y por escondite
    private static HashSet<string> shown = new HashSet<string>();

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseAlpha = sr.color.a;
        baseRGB = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }

    private void Update()
    {
        // Si no hay RoleManager aún, no hacemos nada
        if (RoleManager.Instance == null)
            return;

        // Si YO soy el seeker
        if (NetworkManager.Singleton != null &&
            RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId)
        {
        
            sr.color = new Color(baseRGB.r, baseRGB.g, baseRGB.b, sr.color.a);
            return;
        }

        // Si NO soy seeker
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float k = t * intensity;

        Color rgb = Color.Lerp(baseRGB, glowColor, k);
        sr.color = new Color(rgb.r, rgb.g, rgb.b, sr.color.a);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        var otherNet = other.GetComponentInParent<PlayerNetcode>();
        if (otherNet != null && otherNet.isHidden.Value)
        {
            // No se puede pillar a alguien escondido
            return;
        }

        var pNet = other.GetComponentInParent<PlayerNetcode>();
        if (pNet == null) return;

        // Si es seeker, no puede esconderse
        if (RoleManager.Instance.seekerId.Value == pNet.OwnerClientId)
            return;

        // Pedimos al SERVER escondernos
        if (pNet.IsOwner)
            pNet.EnterHideServerRpc();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var pNet = other.GetComponentInParent<PlayerNetcode>();
        if (pNet == null || !pNet.IsOwner) return;

        // Hablamos con PlayerMovement (que ya tiene la lógica integrada)
        var pm = pNet.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.SetNearHideSpot(true, this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pNet = other.GetComponentInParent<PlayerNetcode>();
        if (pNet == null || !pNet.IsOwner) return;

        var pm = pNet.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.SetNearHideSpot(false, null);
        }

    }

    // ===== VISUAL: alpha del objeto cuando estás escondido =====
    public void SetAlpha(bool hiding)
    {
        if (sr == null) return;

        var c = sr.color;
        c.a = hiding ? alphaWhenHiding : baseAlpha;
        sr.color = c;
    }

    public int SortingOrder => sr != null ? sr.sortingOrder : 0;
    public string SortingLayer => sr != null ? sr.sortingLayerName : "Default";
}