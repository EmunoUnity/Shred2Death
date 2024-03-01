using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RotateWithMouse : MonoBehaviour
{
    public float rotationSpeed = 1f;
    private Vector2 rotation;
    private bool resetting;

    public float xRotationOffset = 0;
    public float yRotationOffset = 0;

    [SerializeField]
    float resetRotationSpeed;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (resetting) return;
        
        // Get the input delta
        Vector2 lookDelta = InputRouting.Instance.GetLookInput();

        rotation.y += lookDelta.x * rotationSpeed * Time.deltaTime;
        rotation.x -= lookDelta.y * rotationSpeed * Time.deltaTime;
        rotation.x = Mathf.Clamp(rotation.x, -90f, 90f); // Limit the vertical rotation
        
        // Apply the rotation to the origin transform
        transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
    }

    public void SetRotation(Vector3 eulers)
    {
        rotation = eulers;
    }
    
    public IEnumerator ResetLocalRotationToEulers(Vector3 eulers)
    {
        float t = 0;
        resetting = true;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * resetRotationSpeed;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(eulers), t);
            yield return null;
        }

        rotation =  eulers;
        resetting = false;
    }
}
