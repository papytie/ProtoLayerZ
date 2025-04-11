using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public InputAction Move => controls.MainCharacterMap.Walking;
    public InputAction Jump => controls.MainCharacterMap.Jumping;
    public InputAction Slide => controls.MainCharacterMap.Sliding;

    Controls controls;

    private void Awake()
    {
        controls = new Controls();
        controls.MainCharacterMap.Enable();
    }
}
