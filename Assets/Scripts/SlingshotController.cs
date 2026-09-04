using UnityEngine;
using UnityEngine.InputSystem;

public class SlingshotController : MonoBehaviour
{
    [SerializeField] private Rigidbody projectile;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float launchForce = 14f;
    [SerializeField] private float maxDragDistance = 2.5f;
    [SerializeField] private int trajectoryStepCount = 25;
    [SerializeField] private float timeStep = 0.05f;

    private readonly Vector3 anchor = new Vector3(0f, -2f, 0f);
    private Vector3 dragVector;
    private bool isDragging;
    private bool hasLaunched;

    private void Start()
    {
        ResolveReferences();
        ValidateSettings();

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            HideTrajectory();
        }

        if (projectile != null)
        {
            projectile.isKinematic = true;
            projectile.useGravity = false;
            projectile.linearVelocity = Vector3.zero;
            projectile.angularVelocity = Vector3.zero;
            projectile.position = anchor;
        }
    }

    private void Update()
    {
        if (!HasRequiredReferences() || hasLaunched)
        {
            return;
        }

        if (!isDragging)
        {
            projectile.isKinematic = true;
            projectile.useGravity = false;
            projectile.linearVelocity = Vector3.zero;
            projectile.angularVelocity = Vector3.zero;
            projectile.position = anchor;
        }

        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            return;
        }

        if (isDragging)
        {
            if (pointer.press.wasReleasedThisFrame || !pointer.press.isPressed)
            {
                ReleaseProjectile();
                return;
            }

            if (TryGetPointerPlanePosition(pointer, out Vector3 pointerPlanePosition))
            {
                UpdateDrag(pointerPlanePosition);
            }

            return;
        }

        if (pointer.press.wasPressedThisFrame &&
            PointerHitsProjectile(pointer) &&
            TryGetPointerPlanePosition(pointer, out Vector3 dragStartPosition))
        {
            BeginDrag(dragStartPosition);
        }
    }

    private void ResolveReferences()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (projectile == null)
        {
            Rigidbody[] childRigidbodies = GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody childRigidbody in childRigidbodies)
            {
                if (childRigidbody.transform != transform)
                {
                    projectile = childRigidbody;
                    break;
                }
            }
        }
    }

    private void ValidateSettings()
    {
        maxDragDistance = Mathf.Max(0f, maxDragDistance);
        trajectoryStepCount = Mathf.Max(2, trajectoryStepCount);
        timeStep = Mathf.Max(float.Epsilon, timeStep);
    }

    private bool HasRequiredReferences()
    {
        return projectile != null &&
               lineRenderer != null &&
               Camera.main != null;
    }

    private bool PointerHitsProjectile(Pointer pointer)
    {
        Camera aimingCamera = Camera.main;

        if (pointer == null || aimingCamera == null || projectile == null)
        {
            return false;
        }

        Ray pointerRay = aimingCamera.ScreenPointToRay(pointer.position.ReadValue());

        return Physics.Raycast(
            pointerRay,
            out RaycastHit hit,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide) &&
            hit.collider.attachedRigidbody == projectile;
    }

    private bool TryGetPointerPlanePosition(Pointer pointer, out Vector3 planePosition)
    {
        planePosition = anchor;

        Camera aimingCamera = Camera.main;
        if (pointer == null || aimingCamera == null)
        {
            return false;
        }

        Ray pointerRay = aimingCamera.ScreenPointToRay(pointer.position.ReadValue());
        Plane aimingPlane = new Plane(Vector3.forward, Vector3.zero);

        if (!aimingPlane.Raycast(pointerRay, out float rayDistance))
        {
            return false;
        }

        planePosition = pointerRay.GetPoint(rayDistance);
        planePosition.z = 0f;
        return true;
    }

    private void BeginDrag(Vector3 pointerPlanePosition)
    {
        isDragging = true;

        projectile.isKinematic = true;
        projectile.useGravity = false;
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;

        UpdateDrag(pointerPlanePosition);
    }

    private void UpdateDrag(Vector3 pointerPlanePosition)
    {
        projectile.isKinematic = true;
        projectile.useGravity = false;
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;

        dragVector = pointerPlanePosition - anchor;
        dragVector.z = 0f;

        if (dragVector.sqrMagnitude > maxDragDistance * maxDragDistance)
        {
            dragVector = dragVector.normalized * maxDragDistance;
        }

        Vector3 heldPosition = anchor + dragVector;
        heldPosition.z = 0f;

        projectile.position = heldPosition;
        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        if (lineRenderer == null || projectile == null)
        {
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = trajectoryStepCount;

        Vector3 heldPosition = projectile.position;
        heldPosition.z = 0f;

        Vector3 impulse = -dragVector * launchForce;
        Vector3 initialVelocity = impulse / Mathf.Max(projectile.mass, float.Epsilon);

        for (int i = 0; i < trajectoryStepCount; i++)
        {
            float t = i * timeStep;
            Vector3 trajectoryPoint =
                heldPosition +
                initialVelocity * t +
                0.5f * Physics.gravity * t * t;

            trajectoryPoint.z = 0f;
            lineRenderer.SetPosition(i, trajectoryPoint);
        }
    }

    private void ReleaseProjectile()
    {
        isDragging = false;
        hasLaunched = true;

        projectile.isKinematic = false;
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;
        projectile.useGravity = true;
        projectile.AddForce(-dragVector * launchForce, ForceMode.Impulse);

        HideTrajectory();
    }

    private void HideTrajectory()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = trajectoryStepCount;

        for (int i = 0; i < trajectoryStepCount; i++)
        {
            lineRenderer.SetPosition(i, anchor);
        }

        lineRenderer.enabled = false;
    }
}