using UnityEngine;
using TMPro;

public class HideTimerUI : MonoBehaviour
{
    public static HideTimerUI Instance;

    [SerializeField] private TMP_Text timerText;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(float time)
    {
        timerText.text = $"Escondido: {Mathf.CeilToInt(time)}";
        timerText.gameObject.SetActive(true);
    }

    // NUEVO: mostrar texto directo
    public void ShowMessage(string msg)
    {
        timerText.text = msg;
        timerText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        timerText.text = "";
        timerText.gameObject.SetActive(false);
    }
}