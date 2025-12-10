using UnityEngine;
using Unity.Netcode;

public class PlayerMovement_Netcode : NetworkBehaviour
{
    public enum Estados { IDLE, RUN, PRESS };

    public float speed = 5f;
    public Estados mystate;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 1–5 según skin del jugador
    public NetworkVariable<int> skinID = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ARRAYS PARA TUS 5 JUGADORES
    public RuntimeAnimatorController[] walkAnimators;
    public Sprite[] idleSprites;
    public Sprite[] kickSprites;

    public override void OnNetworkSpawn()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (IsServer)
            skinID.Value = Random.Range(1, 6); // 1 a 5

        skinID.OnValueChanged += (oldVal, newVal) => ApplySkin(newVal);

        ApplySkin(skinID.Value);
    }

    private void ApplySkin(int id)
    {
        // WALK usa animator
        animator.runtimeAnimatorController = walkAnimators[id - 1];

        // IDLE inicial
        spriteRenderer.sprite = idleSprites[id - 1];
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            mystate = Estados.RUN;
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            mystate = Estados.PRESS;
        }
        else
        {
            mystate = Estados.IDLE;
        }

        switch (mystate)
        {
            case Estados.IDLE: Idle(); break;
            case Estados.RUN: Run(); break;
            case Estados.PRESS: Press(); break;
        }
    }

    void Idle()
    {
        animator.enabled = false;
        spriteRenderer.sprite = idleSprites[skinID.Value - 1];
        

    }

    void Run()
    {
        animator.enabled = true;
        animator.Play("Run"); 

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.A)) move += Vector3.left;
        if (Input.GetKey(KeyCode.D)) move += Vector3.right;
        if (Input.GetKey(KeyCode.W)) move += Vector3.up;
        if (Input.GetKey(KeyCode.S)) move += Vector3.down;

        if (move.sqrMagnitude > 0f)
        {
            move.Normalize();
            transform.Translate(move * speed * Time.deltaTime);
        }
    }

    void Press()
    {
        animator.enabled = false;
        spriteRenderer.sprite = kickSprites[skinID.Value - 1];
     
    }
}