using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif

        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            mouseX = delta.x * (mouseSensitivity * 0.015f);
            mouseY = delta.y * (mouseSensitivity * 0.015f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (mouseX == 0f && mouseY == 0f)
        {
            mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivity;
        }
#endif

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}