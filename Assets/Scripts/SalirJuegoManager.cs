using UnityEngine;

public class SalirJuegoManager : MonoBehaviour
{
    public void SalirJuego()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
