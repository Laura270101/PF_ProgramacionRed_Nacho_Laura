using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerHUD : NetworkBehaviour
{
    private TMP_Text txtRol;
    private TMP_Text txtVecesPillado;

    private PlayerNetcode pNet;

    private void Awake()
    {
        pNet = GetComponent<PlayerNetcode>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        
        txtRol = GameObject.Find("Txt_Rol")?.GetComponent<TMP_Text>();
        txtVecesPillado = GameObject.Find("Txt_VecesPillado")?.GetComponent<TMP_Text>();

        RefreshAll();

        
        if (pNet != null)
            pNet.vecesPillado.OnValueChanged += OnVecesPilladoChanged;

        if (RoleManager.Instance != null)
            RoleManager.Instance.seekerId.OnValueChanged += OnSeekerChanged;
    }

    private void OnDestroy()
    {
        if (pNet != null)
            pNet.vecesPillado.OnValueChanged -= OnVecesPilladoChanged;

        if (RoleManager.Instance != null)
            RoleManager.Instance.seekerId.OnValueChanged -= OnSeekerChanged;
    }

    private void OnVecesPilladoChanged(int oldVal, int newVal) => RefreshVecesPillado();
    private void OnSeekerChanged(ulong oldVal, ulong newVal) => RefreshRol();

    private void RefreshAll()
    {
        RefreshRol();
        RefreshVecesPillado();
    }

    private void RefreshRol()
    {
        if (txtRol == null)
        {
            
            txtRol = GameObject.Find("Txt_Rol")?.GetComponent<TMP_Text>();
            if (txtRol == null) return;
        }

        if (RoleManager.Instance == null || NetworkManager.Singleton == null)
        {
            txtRol.text = "ROL: ---";
            return;
        }

        bool soySeeker = RoleManager.Instance.seekerId.Value == NetworkManager.Singleton.LocalClientId;
        txtRol.text = soySeeker ? "ROL: TE TOCA PILLAR" : "ROL: TE TOCA ESCONDERTE";
    }

    private void RefreshVecesPillado()
    {
        if (txtVecesPillado == null)
        {
            txtVecesPillado = GameObject.Find("Txt_VecesPillado")?.GetComponent<TMP_Text>();
            if (txtVecesPillado == null) return;
        }

        if (pNet == null)
        {
            txtVecesPillado.text = "PILLADO: ---";
            return;
        }

        txtVecesPillado.text = $"PILLADO: {pNet.vecesPillado.Value}";
    }
}
