using UnityEngine;
using Photon.Pun;
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Point followed by the camera.")]
    public GameObject target;
    [Tooltip("Distance from target point to camera.")]
    public float distance = 10.0f;
    [Tooltip("Height offset from target point.")]
    public float targetHeightOffset = 0.0f;
    [Tooltip("The height of the camera from the target point.")]
    public float cameraHeightOffset = 0.0f;
    public float translateSpeed = 1f;
    public float yaw = 0.0f;

    private Camera m_cameraComponent = null;
    private CarController m_carComponent = null;
    private Vector3 m_currentPosition;

    [Tooltip("Dependence camera FOV from car speed")]
    public AnimationCurve fovCurve = AnimationCurve.Linear(0.0f, 60.0f, 120.0f, 40.0f);

    public PhotonView view;
    void Start()
    {
        m_cameraComponent = GetComponent<Camera>();
        if (target != null)
        {
            m_carComponent = target.GetComponent<CarController>();
        }

        m_currentPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (view.IsMine)
        {
        if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1))
        {
            yaw = 60.0f;
        }

        if (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2))
        {
            yaw = -60.0f;
        }

        if (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3))
        {
            yaw = 0f;
        }

        if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
        {
            yaw = 180f;
        }

        if (Input.GetKey(KeyCode.Alpha5) || Input.GetKey(KeyCode.Keypad5))
        {
            distance = 5f;
            targetHeightOffset = 0.3f;
            cameraHeightOffset = 1.36f;
        }

        if (Input.GetKey(KeyCode.Alpha6) || Input.GetKey(KeyCode.Keypad6))
        {
            distance = 3f;
            targetHeightOffset = 0f;
            cameraHeightOffset = 0.5f;
        }

        if (Input.GetKey(KeyCode.Alpha7) || Input.GetKey(KeyCode.Keypad7))
        {
            distance = 15f;
            targetHeightOffset = 1f;
            cameraHeightOffset = 8f;
        }

        if (Input.GetKey(KeyCode.Alpha8) || Input.GetKey(KeyCode.Keypad8))
        {
            distance = 12f;
            targetHeightOffset = 1f;
            cameraHeightOffset = 4f;
        }

        Vector3 curPosTmp = m_currentPosition;
        Vector3 tgtPos = target.transform.position;

        tgtPos.y = 0.0f;
        curPosTmp.y = 0.0f;

        Vector3 dir2D = curPosTmp - tgtPos;

        float len = dir2D.magnitude;
        dir2D.Normalize();

        Vector3 camPos = curPosTmp;
        if (len > distance)
        {
            camPos = tgtPos + dir2D * distance;
        }

        camPos.y = target.transform.position.y + cameraHeightOffset;
        transform.position = camPos;

        Vector3 targetPt = target.transform.position;
        targetPt.y += targetHeightOffset;

        Vector3 lookDir = targetPt - camPos;

        Quaternion rot = Quaternion.LookRotation(lookDir, Vector3.up);

        transform.rotation = rot;

        if (m_carComponent != null)
        {
            float speed = m_carComponent.GetSpeed();

            float speedKmH = speed * 3.6f;

            float fov = fovCurve.Evaluate(speedKmH);
            m_cameraComponent.fieldOfView = Mathf.Lerp(m_cameraComponent.fieldOfView, fov, 1f * Time.deltaTime);
        }

        m_currentPosition = transform.position;

        transform.RotateAround(targetPt, Vector3.up, yaw);
        }
    }
}