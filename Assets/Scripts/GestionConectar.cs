using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GestionConectar : MonoBehaviour
{
    public Button Btn_Unirse;
    public Button Btn_Salir;
    //public Button btnDesconectar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ConectarComoHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("Conectado como Host");
    }

    public void ConectarComoCliente()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Conectado como Client");
    }

   // public void Desconectar()
   // {
   //     NetworkManager.Singleton.Shutdown();
   //     Debug.Log("Desconectado");
   // }
}
