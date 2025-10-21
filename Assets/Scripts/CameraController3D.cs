using UnityEngine;

public class CameraController3D : MonoBehaviour {
    [Header("Настройки управления камерой")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 100f;
    public float zoomSpeed = 5f;
    public float minZoom = 10f;
    public float maxZoom = 50f;
    
    [Header("Настройки сетки")]
    public Vector3 gridCenter = Vector3.zero;
    public float gridWidth = 20f;
    public float gridHeight = 20f;
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start() {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        if (gridCenter == Vector3.zero) {
            gridCenter = new Vector3(gridWidth / 2, 0, gridHeight / 2);
        }
    }

    void Update() {
        HandleCameraRotation();
        HandleCameraZoom();
    }

    void HandleCameraRotation() {
        if (Input.GetMouseButton(1)) {
            float rotateX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float rotateY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            
            transform.RotateAround(gridCenter, Vector3.up, rotateX);
            transform.RotateAround(gridCenter, transform.right, -rotateY);
            
            Vector3 angles = transform.eulerAngles;
            angles.z = 0;
            transform.eulerAngles = angles;
        }
    }

    void HandleCameraZoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        Vector3 directionToCenter = (gridCenter - transform.position).normalized;
        transform.Translate(directionToCenter * scroll, Space.World);
        float currentDistance = Vector3.Distance(transform.position, gridCenter);
        if (currentDistance < minZoom) {
            transform.position = gridCenter - directionToCenter * minZoom;
        } else if (currentDistance > maxZoom) {
            transform.position = gridCenter - directionToCenter * maxZoom;
        }
    }

    public void SetGridParameters(Vector3 center, float width, float height) {
        gridCenter = center;
        gridWidth = width;
        gridHeight = height;
    }
}