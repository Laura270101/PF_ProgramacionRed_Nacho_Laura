using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;

public class GestionConectar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtJoinCode;
    [SerializeField] private TMP_InputField inputJoinCode;

    [Header("Netcode")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;

    [Header("Relay")]
    [SerializeField] private int maxJugadores = 4;  //El host y 3 clientes.

    private async void Awake()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
        
        if (unityTransport == null && networkManager != null)
        {
            unityTransport = networkManager.GetComponent<UnityTransport>();
        }

        await InitUnityServices();
    }

    private async Task InitUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    
    public async void CrearJuego_Host()
    {
        await InitUnityServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        if (txtJoinCode != null)
        {
            txtJoinCode.text = $"Código: {joinCode}";
        }

        var relayData = new RelayServerData(allocation, "dtls");
        unityTransport.SetRelayServerData(relayData);

        networkManager.StartHost();
    }

    public async void UnirseJuego_Client()
    {
        await InitUnityServices();

        string code = inputJoinCode != null ? inputJoinCode.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            if (txtJoinCode != null)
            {
                txtJoinCode.text = "Código: (vacío)";
            }
            return;
        }

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

        var relayData = new RelayServerData(joinAllocation, "dtls");
        unityTransport.SetRelayServerData(relayData);

        networkManager.StartClient();
    }

    public void Salir()
    {
        if (networkManager != null)
        {
            networkManager.Shutdown();
        }

        if (txtJoinCode != null)
        {
            txtJoinCode.text = "Código: ---";
        }

        if (inputJoinCode != null)
        {
            inputJoinCode.text = "";
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
