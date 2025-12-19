using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMenuPrincipal;
    public GameObject panelConexion;
    public GameObject panelCrearPartida;
    public GameObject panelUnirsePartida;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MostrarMenuPrincipal();
    }

    void DescativarTodos()
    {
        panelMenuPrincipal.SetActive(false);
        panelConexion.SetActive(false);
        panelCrearPartida.SetActive(false);
        panelUnirsePartida.SetActive(false);
    }

    public void MostrarMenuPrincipal()
    {
        DescativarTodos();
        panelMenuPrincipal.SetActive(true);
    }

    public void MostrarConexion()
    {
        DescativarTodos();
        panelConexion.SetActive(true);
    }

    public void MostrarCrearPartida()
    {
        DescativarTodos();
        panelCrearPartida.SetActive(true);
    }

    public void MostrarUnirsePartida()
    {
        DescativarTodos();
        panelUnirsePartida.SetActive(true);
    }
}
