using UnityEngine;

public class GameplayLayout : MonoBehaviour
{
    [Header("Core References")]
    public Camera targetCamera;
    public Transform candidateWord;
    
    [Header("Left Side")]
    public Transform leftTarget;
    public Transform leftLabel;

    [Header("Right Side")]
    public Transform rightTarget;
    public Transform rightLabel;

    [Header("Positioning Settings")]
    [Tooltip("How far forward from the camera the targets should be")]
    public float targetDistance = 10f;
    [Tooltip("How far left/right from the center the targets sit")]
    public float targetSpread = 4f;
    [Tooltip("The world Y position of your ground mesh")]
    public float groundY = 0f;
    [Tooltip("How high above the target the category label floats")]
    public float labelHeightOffset = 1.5f;

    [Header("Candidate Word Settings")]
    [Tooltip("How close to the camera the word floats")]
    public float candidateDistance = 3f;
    [Tooltip("Slightly shift the word up or down relative to camera center")]
    public float candidateYOffset = -0.5f;

    [ContextMenu("Align Objects to Camera")]
    public void AlignToCamera()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning("No camera assigned or found!");
            return;
        }

        Transform camT = targetCamera.transform;

        // 1. Position Candidate Word (floating in front of camera)
        if (candidateWord != null)
        {
            candidateWord.position = camT.position + (camT.forward * candidateDistance) + (camT.up * candidateYOffset);
            // Reset rotation to (0,0,0) in the inspector
            candidateWord.localRotation = Quaternion.identity;
        }

        // 2. Calculate ground center position for targets
        Vector3 centerPoint = camT.position + (camT.forward * targetDistance);
        centerPoint.y = groundY; // Snap to the ground mesh height

        // Flatten the right vector so the spread is purely horizontal (not tilted by camera pitch)
        Vector3 flatRight = camT.right;
        flatRight.y = 0;
        flatRight.Normalize();

        // 3. Position Left Target & Label
        if (leftTarget != null)
        {
            leftTarget.position = centerPoint - (flatRight * targetSpread);
            leftTarget.localRotation = Quaternion.identity; 
        }
        if (leftLabel != null)
        {
            leftLabel.position = leftTarget.position + (Vector3.up * labelHeightOffset);
            leftLabel.localRotation = Quaternion.identity; 
        }

        // 4. Position Right Target & Label
        if (rightTarget != null)
        {
            rightTarget.position = centerPoint + (flatRight * targetSpread);
            rightTarget.localRotation = Quaternion.identity;
        }
        if (rightLabel != null)
        {
            rightLabel.position = rightTarget.position + (Vector3.up * labelHeightOffset);
            rightLabel.localRotation = Quaternion.identity;
        }

        Debug.Log("Gameplay layout successfully snapped positions and zeroed rotations!");
    }
}
