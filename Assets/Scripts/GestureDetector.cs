using UnityEngine;
using UnityEngine.InputSystem;

public class GestureDetector : MonoBehaviour
{
    public bool coralGesture;
    public bool fishGesture;
    public bool bubbleGesture;

    void Update()
    {
        coralGesture = Keyboard.current.digit1Key.isPressed;
        fishGesture = Keyboard.current.digit2Key.isPressed;
        bubbleGesture = Keyboard.current.digit3Key.isPressed;
    }
}