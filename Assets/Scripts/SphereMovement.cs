using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SphereMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private float movementAcceleration = 20f;
    [SerializeField] private float maximumHorizontalSpeed = 8f;
    [SerializeField] private float airControlMultiplier = 0.65f;

    private Vector3 movementInput;
    private HashSet<Collider> groundContacts = new HashSet<Collider>();

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            movementInput = Vector3.zero;
            return;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            verticalInput -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            verticalInput += 1f;
        }

        movementInput = new Vector3(horizontalInput, 0f, verticalInput);

        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void FixedUpdate()
    {
        if (targetRigidbody == null)
        {
            return;
        }

        if (movementInput.sqrMagnitude > 0f)
        {
            float controlMultiplier = groundContacts.Count > 0 ? 1f : airControlMultiplier;

            targetRigidbody.AddForce(
                movementInput * movementAcceleration * controlMultiplier,
                ForceMode.Acceleration);
        }

        LimitHorizontalSpeed();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HasUpwardContact(collision))
        {
            groundContacts.Add(collision.collider);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (HasUpwardContact(collision))
        {
            groundContacts.Add(collision.collider);
        }
        else
        {
            groundContacts.Remove(collision.collider);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        groundContacts.Remove(collision.collider);
    }

    private void OnDisable()
    {
        movementInput = Vector3.zero;
        groundContacts.Clear();
    }

    private bool HasUpwardContact(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private void LimitHorizontalSpeed()
    {
        Vector3 velocity = targetRigidbody.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.sqrMagnitude <= maximumHorizontalSpeed * maximumHorizontalSpeed)
        {
            return;
        }

        Vector3 limitedHorizontalVelocity = horizontalVelocity.normalized * maximumHorizontalSpeed;

        targetRigidbody.linearVelocity = new Vector3(
            limitedHorizontalVelocity.x,
            velocity.y,
            limitedHorizontalVelocity.z);
    }
}