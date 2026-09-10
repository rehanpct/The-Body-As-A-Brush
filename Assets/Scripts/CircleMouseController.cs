using UnityEngine;
using UnityEngine.InputSystem;

public class CircleMouseController : MonoBehaviour
{
    public float smoothSpeed = 5f;

    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mousePosition.x,
            mousePosition.y,
            10f
        );

        Vector3 targetPosition =
            Camera.main.ScreenToWorldPoint(screenPosition);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}