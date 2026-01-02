using UnityEngine;
using UnityEngine.AI;

public class SetupCollisionObjects : MonoBehaviour
{
    void Start()
    {
        foreach (Transform child in transform)
        {
            // Collider
            if (!child.GetComponent<Collider>())
            {
                BoxCollider col = child.gameObject.AddComponent<BoxCollider>();
                col.center = Vector3.zero;
            }

            // NavMesh Obstacle
            if (!child.GetComponent<NavMeshObstacle>())
            {
                NavMeshObstacle obs = child.gameObject.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.carveOnlyStationary = true;
            }
        }

        Debug.Log("Colisiones y obstáculos configurados");
    }
}