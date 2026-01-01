using UnityEngine;

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
    public bool canControl = false;   // LOCAL ahora / IsOwner luego

    [Header("Kick")]
    [SerializeField] private float kickDuration = 0.35f;

    private Animator animator;
    private SpriteRenderer sr;

    private Estado estadoActual = Estado.IDLE;
    private float kickTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        //ApplySkin(skinID);
        SetEstado(Estado.IDLE);
    }

    private void Update()
    {
        if (!canControl) return;

        // Bloqueo durante kick
        if (kickTimer > 0f)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0f)
                SetEstado(Estado.IDLE);

            return;
        }

        // KICK
        if (Input.GetKeyDown(KeyCode.X))
        {
            StartKick();
            return;
        }

        // Movimiento 
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
        // Normalizamos para que diagonal no sea más rápido
        dir.Normalize();

        // Flip SOLO según horizontal
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

    public void ForceIdleVisual()
    {
        // Mismo efecto que IDLE
        // (Reutiliza tu SetEstado internamente: hay que hacerlo accesible o replicar)
        // Solución simple: reutilizamos ApplySkin que ya setea IDLE visual.
        ApplySkin(skinID);
    }

    public void ForceKickVisual()
    {
        // Simula KICK sin input
        // Ojo: aquí necesitamos acceso a tus arrays y skinID ya lo tienes.
        // Copiamos el comportamiento de SetEstado(KICK) sin tocar el estado interno:
        animator.enabled = false;
        int i = Mathf.Clamp(skinID - 1, 0, 4);
        sr.sprite = kickSprites[i];
    }

    public void ForceWalkVisual()
    {
        // Pone el animator en Walk sin mover
        animator.enabled = true;
        int i = Mathf.Clamp(skinID - 1, 0, 4);
        animator.runtimeAnimatorController = walkAnimators[i];
        animator.Play("Walk", 0, 0f);
    }





}