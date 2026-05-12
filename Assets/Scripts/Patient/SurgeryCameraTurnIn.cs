using UnityEngine;

public class SurgeryCameraTurnIn : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private Vector3 rightArmFocusOffset = new Vector3(2.2f, 0.45f, 0f);
    [SerializeField] private Vector2 rightArmCameraPosition = new Vector2(6.3f, 1.38f);
    [SerializeField] private float rightArmCameraZRotation = -41.146f;
    [SerializeField] private Vector2 lowerLeftLegCameraPosition = new Vector2(-2.4f, -8.4f);
    [SerializeField] private float lowerLeftLegCameraZRotation = 80f;
    [SerializeField] private float startZoomOutMultiplier = 1.55f;
    [SerializeField] private float duration = 2.4f;
    [SerializeField] private float focusMoveDuration = 1.2f;
    [SerializeField] private float focusOrthographicSize = 2.4f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Camera surgeryCamera;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 endPosition;
    private Quaternion endRotation;
    private float startOrthographicSize;
    private float endOrthographicSize;
    private Vector3 moveStartPosition;
    private Vector3 moveEndPosition;
    private Quaternion moveStartRotation;
    private Quaternion moveEndRotation;
    private float moveStartOrthographicSize;
    private float moveEndOrthographicSize;
    private float elapsed;
    private bool isFocusMoveActive;

    void Start()
    {
        surgeryCamera = GetComponent<Camera>();
        ResolveTarget();

        endPosition = transform.position;
        endRotation = transform.rotation;

        endOrthographicSize = surgeryCamera != null && surgeryCamera.orthographic
            ? surgeryCamera.orthographicSize
            : 0f;
        startOrthographicSize = endOrthographicSize * startZoomOutMultiplier;

        if (target == null || surgeryCamera == null || !surgeryCamera.orthographic)
        {
            enabled = false;
            return;
        }

        startPosition = endPosition;
        startRotation = endRotation;

        transform.SetPositionAndRotation(startPosition, startRotation);
        surgeryCamera.orthographicSize = startOrthographicSize;
    }

    void Update()
    {
        if (isFocusMoveActive)
        {
            UpdateFocusMove();
            return;
        }

        if (target == null || duration <= 0f)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float easedT = ease != null ? ease.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(startPosition, endPosition, easedT);
        transform.rotation = Quaternion.Slerp(startRotation, endRotation, easedT);
        if (surgeryCamera != null && surgeryCamera.orthographic)
        {
            surgeryCamera.orthographicSize = Mathf.Lerp(startOrthographicSize, endOrthographicSize, easedT);
        }

        if (t >= 1f)
        {
            if (surgeryCamera != null && surgeryCamera.orthographic)
            {
                surgeryCamera.orthographicSize = endOrthographicSize;
            }

            enabled = false;
        }
    }

    public void FocusForWoundLocation(CutWound.WoundLocation woundLocation, Transform patientRoot)
    {
        if (patientRoot != null)
        {
            target = patientRoot;
        }
        else
        {
            ResolveTarget();
        }

        Vector3 focusOffset = woundLocation == CutWound.WoundLocation.Part
            ? Vector3.zero
            : rightArmFocusOffset;

        if (woundLocation == CutWound.WoundLocation.Part)
        {
            MoveToCameraPosition(lowerLeftLegCameraPosition, lowerLeftLegCameraZRotation);
            return;
        }

        MoveToFocus(target, focusOffset);
    }

    public void FocusForSpawnArea(string spawnAreaId, Transform patientRoot)
    {
        if (string.Equals(spawnAreaId, "LeftLeg", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spawnAreaId, "Leg", System.StringComparison.OrdinalIgnoreCase))
        {
            if (patientRoot != null)
            {
                target = patientRoot;
            }

            MoveToCameraPosition(lowerLeftLegCameraPosition, lowerLeftLegCameraZRotation);
            return;
        }

        if (string.Equals(spawnAreaId, "RightArm", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spawnAreaId, "Arm", System.StringComparison.OrdinalIgnoreCase))
        {
            if (patientRoot != null)
            {
                target = patientRoot;
            }

            MoveToCameraPosition(rightArmCameraPosition, rightArmCameraZRotation);
            return;
        }

        if (patientRoot != null)
        {
            target = patientRoot;
        }
        else
        {
            ResolveTarget();
        }

        MoveToFocus(target, targetOffset);
    }

    void MoveToCameraPosition(Vector2 cameraPosition, float zRotation)
    {
        surgeryCamera ??= GetComponent<Camera>();

        moveStartPosition = transform.position;
        moveEndPosition = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
        moveStartRotation = transform.rotation;
        moveEndRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, zRotation);
        moveStartOrthographicSize = surgeryCamera != null ? surgeryCamera.orthographicSize : 0f;
        moveEndOrthographicSize = focusOrthographicSize;

        elapsed = 0f;
        isFocusMoveActive = true;
        enabled = true;
    }

    void MoveToFocus(Transform focusRoot, Vector3 focusOffset)
    {
        if (focusRoot == null)
        {
            return;
        }

        surgeryCamera ??= GetComponent<Camera>();

        Vector3 focusPoint = focusRoot.position + focusOffset;
        moveStartPosition = transform.position;
        moveEndPosition = new Vector3(focusPoint.x, focusPoint.y, transform.position.z);
        moveStartRotation = transform.rotation;
        moveEndRotation = Quaternion.LookRotation(focusPoint - moveEndPosition, Vector3.up);
        moveStartOrthographicSize = surgeryCamera != null ? surgeryCamera.orthographicSize : 0f;
        moveEndOrthographicSize = focusOrthographicSize;

        elapsed = 0f;
        isFocusMoveActive = true;
        enabled = true;
    }

    void UpdateFocusMove()
    {
        if (focusMoveDuration <= 0f)
        {
            CompleteFocusMove();
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / focusMoveDuration);
        float easedT = ease != null ? ease.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(moveStartPosition, moveEndPosition, easedT);
        transform.rotation = Quaternion.Slerp(moveStartRotation, moveEndRotation, easedT);

        if (surgeryCamera != null && surgeryCamera.orthographic)
        {
            surgeryCamera.orthographicSize = Mathf.Lerp(moveStartOrthographicSize, moveEndOrthographicSize, easedT);
        }

        if (t >= 1f)
        {
            CompleteFocusMove();
        }
    }

    void CompleteFocusMove()
    {
        transform.position = moveEndPosition;
        transform.rotation = moveEndRotation;

        if (surgeryCamera != null && surgeryCamera.orthographic)
        {
            surgeryCamera.orthographicSize = moveEndOrthographicSize;
        }

        isFocusMoveActive = false;
        enabled = false;
    }

    void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        Patient patient = FindAnyObjectByType<Patient>();
        if (patient != null)
        {
            target = patient.transform;
        }
    }
}
