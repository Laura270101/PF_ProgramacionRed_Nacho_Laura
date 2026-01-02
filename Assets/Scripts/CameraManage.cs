using UnityEngine;
using Unity.Netcode;

public class CameraManage : NetworkBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private float smoothSpeed = 10f;

    private Camera cam;

    private void Start()
    {
        if (!IsOwner)
        {
            
            Destroy(this);
            return;
        }

        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        Vector3 targetPos = transform.position + offset;
        cam.transform.position = Vector3.Lerp(
            cam.transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}