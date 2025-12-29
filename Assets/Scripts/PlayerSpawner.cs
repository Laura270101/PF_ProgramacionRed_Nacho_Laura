using UnityEngine;

public class PlayerSelectorLocal : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private PlayerMovement playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Skin (1–5)")]
    [Range(1, 5)]
    [SerializeField] private int skinID = 1;

    private void Start()
    {
        PlayerMovement player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        player.ApplySkin(skinID);
        player.SetControllable(true);
    }
}