using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    public enum Estado { IDLE, WALK }

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Skins (1–5)")]
    [Range(1, 5)]
    [SerializeField] private int skinID = 1;

    [Header("Skin Assets (size = 5)")]
    [SerializeField] private RuntimeAnimatorController[] walkAnimators;
    [SerializeField] private Sprite[] idleSprites;

    [Header("Control")]
    public bool canControl = false;

    private Animator animator;
    private SpriteRenderer sr;

    private Estado estadoActual = Estado.IDLE;

    private PlayerNetcode pNet;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        pNet = GetComponent<PlayerNetcode>();
    }

    private void Start()
    {
        SetEstado(Estado.IDLE);
    }

    private void Update()
    {
        // CAMBIO: SOLO EL OWNER LEE INPUT
        if (pNet == null || !pNet.IsOwner) return;

        // CAMBIO: X = INTERACCIÓN CON HIDESPOT
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (RoleManager.Instance != null && Unity.Netcode.NetworkManager.Singleton != null)
            {
                bool iAmSeeker = RoleManager.Instance.seekerId.Value == Unity.Netcode.NetworkManager.Singleton.LocalClientId;

                // Seeker: revelar spot cercano
                if (iAmSeeker)
                {
                    pNet.TryRevealNearSpot();
                    return;
                }

                // Hider: si está escondido -> salir, si no -> entrar
                if (pNet.isHidden.Value) pNet.TryExitSpot();
                else pNet.TryEnterNearSpot();

                return;
            }
        }

        if (!canControl) return;

        // MOVIMIENTO
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
        }
    }

    public void ApplySkin(int id)
    {
        skinID = Mathf.Clamp(id, 1, 5);
        SetEstado(estadoActual == Estado.WALK ? Estado.WALK : Estado.IDLE);
    }

    // ===== USADO POR PlayerNetcode =====
    public void ForceIdleVisual()
    {
        ApplySkin(skinID);
    }

    public void ForceWalkVisual()
    {
        animator.enabled = true;
        int i = Mathf.Clamp(skinID - 1, 0, 4);
        animator.runtimeAnimatorController = walkAnimators[i];
        animator.Play("Walk", 0, 0f);
    }
}
