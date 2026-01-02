using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    public enum Estado { IDLE, WALK, KICK }

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Skins (1–5)")]
    [Range(1, 5)]
    [SerializeField] private int skinID = 1;

    [Header("Skin Assets (size = 5)")]
    [SerializeField] private RuntimeAnimatorController[] walkAnimators;
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] kickSprites;

    [Header("Control")]
    public bool canControl = false;   // Control LOCAL (PlayerNetcode lo gestiona)

    [Header("Kick / Hide")]
    [SerializeField] private float kickDuration = 0.35f;
    [SerializeField] private float hiddenAlpha = 0.85f;

    private Animator animator;
    private SpriteRenderer sr;

    private Estado estadoActual = Estado.IDLE;
    private float kickTimer = 0f;

    // ===== ESCONDERSE =====
    private bool nearHideSpot = false;
    private bool isHidden = false;
    private bool hideTransition = false;
    private HideSpot currentHideSpot;

    private string baseSortingLayer;
    private int baseSortingOrder;
    private Color baseColor;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Guardamos estado base del sprite
        baseSortingLayer = sr.sortingLayerName;
        baseSortingOrder = sr.sortingOrder;
        baseColor = sr.color;
    }

    private void Start()
    {
        SetEstado(Estado.IDLE);
    }

    private void Update()
    {
        if (!canControl && !isHidden) return;

        // ===== SALIR DEL ESCONDITE =====
        if (isHidden)
        {
            if (Input.GetKeyDown(KeyCode.X))
                ExitHide();
            return;
        }

        // ===== BLOQUEO DURANTE TRANSICIÓN DE ESCONDERSE =====
        if (hideTransition) return;

        // ===== BLOQUEO DURANTE KICK =====
        if (kickTimer > 0f)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0f)
                SetEstado(Estado.IDLE);

            return;
        }

        // ===== ESCONDERSE (X CERCA DE ESCONDITE) =====
        if (nearHideSpot && Input.GetKeyDown(KeyCode.X))
        {
            // Si eres seeker, no puedes esconderte
            if (RoleManager.Instance != null &&
                RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId)
                return;

            StartCoroutine(HideAfterKick());
            return;
        }

        // ===== KICK NORMAL =====
        if (Input.GetKeyDown(KeyCode.X))
        {
            StartKick();
            return;
        }

        // ===== MOVIMIENTO =====
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(h, v);

        if (dir.sqrMagnitude > 0.01f)
            Move(dir);
        else
            SetEstado(Estado.IDLE);
    }

    private void Move(Vector2 dir)
    {
        dir.Normalize();

        if (Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0f;

        SetEstado(Estado.WALK);

        Vector3 delta = new Vector3(dir.x, dir.y, 0f) * speed * Time.deltaTime;
        transform.Translate(delta, Space.World);
    }

    private void StartKick()
    {
        SetEstado(Estado.KICK);
        kickTimer = kickDuration;
    }

    // ===== SECUENCIA ESCONDERSE =====
    private IEnumerator HideAfterKick()
    {
        hideTransition = true;

        // Forzamos el kick visual
        StartKick();

        // Bloqueamos control mientras dura la animación
        canControl = false;

        yield return new WaitForSeconds(kickDuration);

        EnterHide();
        hideTransition = false;
    }

    private void EnterHide()
    {
        isHidden = true;

        // Player detrás del objeto
        sr.sortingLayerName = currentHideSpot.SortingLayer;
        sr.sortingOrder = currentHideSpot.SortingOrder - 1;

        // Un poco transparente
        var c = sr.color;
        c.a = hiddenAlpha;
        sr.color = c;

        // El objeto se vuelve transparente
        currentHideSpot.SetAlpha(true);

      
    }

    private void ExitHide()
    {
        isHidden = false;

        // Restaurar visual
        sr.sortingLayerName = baseSortingLayer;
        sr.sortingOrder = baseSortingOrder;
        sr.color = baseColor;

        currentHideSpot?.SetAlpha(false);

        canControl = true;
       
    }

    // ===== LLAMADO DESDE HideSpot =====
    public void SetNearHideSpot(bool value, HideSpot spot)
    {
        nearHideSpot = value;
        currentHideSpot = spot;
    }

    // ===== VISUALES =====
    private void SetEstado(Estado nuevo)
    {
        if (estadoActual == nuevo) return;
        estadoActual = nuevo;

        int i = Mathf.Clamp(skinID - 1, 0, 4);

        switch (estadoActual)
        {
            case Estado.IDLE:
                animator.enabled = false;
                sr.sprite = idleSprites[i];
                break;

            case Estado.WALK:
                animator.enabled = true;
                animator.runtimeAnimatorController = walkAnimators[i];
                animator.Play("Walk", 0, 0f);
                break;

            case Estado.KICK:
                animator.enabled = false;
                sr.sprite = kickSprites[i];
                break;
        }
    }

    public void ApplySkin(int id)
    {
        skinID = Mathf.Clamp(id, 1, 5);
        SetEstado(estadoActual == Estado.WALK ? Estado.WALK : Estado.IDLE);
    }

    public void SetControllable(bool value)
    {
        canControl = value;

        if (!canControl)
        {
            kickTimer = 0f;
            SetEstado(Estado.IDLE);
        }
    }

    // ===== USADO POR PlayerNetcode =====
    public void ForceIdleVisual()
    {
        ApplySkin(skinID);
    }

    public void ForceKickVisual()
    {
        animator.enabled = false;
        int i = Mathf.Clamp(skinID - 1, 0, 4);
        sr.sprite = kickSprites[i];
    }

    public void ForceWalkVisual()
    {
        animator.enabled = true;
        int i = Mathf.Clamp(skinID - 1, 0, 4);
        animator.runtimeAnimatorController = walkAnimators[i];
        animator.Play("Walk", 0, 0f);
    }
}