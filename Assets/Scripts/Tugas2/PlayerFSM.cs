using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerFSM : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    public float moveSpeed = 3f;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (h == 0f && v == 0f)
        {
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }
#endif

        bool isWalking = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
        }

        Vector3 move = new Vector3(h, 0, v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (controller != null)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
    }
}