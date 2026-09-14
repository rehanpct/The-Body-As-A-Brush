using UnityEngine;

public class CoralSway : MonoBehaviour
{
    [Header("Coral Parts")]
    public Transform leftPart;
    public Transform centerPart;
    public Transform rightPart;

    [Header("Sway Angle")]
    public float leftAngle = 3f;
    public float centerAngle = 2.5f;
    public float rightAngle = 3f;

    [Header("Sway Speed")]
    public float leftSpeed = 1.0f;
    public float centerSpeed = 0.9f;
    public float rightSpeed = 1.1f;

    private Quaternion leftStartRotation;
    private Quaternion centerStartRotation;
    private Quaternion rightStartRotation;

    void Start()
    {
        if (leftPart != null)
            leftStartRotation = leftPart.localRotation;

        if (centerPart != null)
            centerStartRotation = centerPart.localRotation;

        if (rightPart != null)
            rightStartRotation = rightPart.localRotation;
    }

    void Update()
    {
        float time = Time.time;

        if (leftPart != null)
        {
            float angle =
                Mathf.Sin(time * leftSpeed) * leftAngle;

            leftPart.localRotation =
                leftStartRotation *
                Quaternion.Euler(0f, 0f, angle);
        }

        if (centerPart != null)
        {
            float angle =
                Mathf.Sin(time * centerSpeed + 1f)
                * centerAngle;

            centerPart.localRotation =
                centerStartRotation *
                Quaternion.Euler(0f, 0f, angle);
        }

        if (rightPart != null)
        {
            float angle =
                Mathf.Sin(time * rightSpeed + 2f)
                * rightAngle;

            rightPart.localRotation =
                rightStartRotation *
                Quaternion.Euler(0f, 0f, angle);
        }
    }
}