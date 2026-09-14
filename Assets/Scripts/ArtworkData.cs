using System;

[Serializable]
public class ArtworkData
{
    // -----------------------------------------
    // SESSION
    // -----------------------------------------

    public float creationTime;

    // -----------------------------------------
    // GENERATED ELEMENTS
    // -----------------------------------------

    public int waterCount;
    public int coralCount;
    public int fishSchoolCount;
    public int bubbleBurstCount;

    // -----------------------------------------
    // GESTURES
    // -----------------------------------------

    public int indexGestureCount;
    public int openPalmGestureCount;
    public int vSignGestureCount;
    public int thumbUpGestureCount;

    // -----------------------------------------
    // MOVEMENT
    // -----------------------------------------

    public float totalHandDistance;

    public float averageHandSpeed;

    public float maximumHandSpeed;

    public float pauseTime;

    // -----------------------------------------
    // HAND USAGE
    // -----------------------------------------

    public int handTrackingSamples;

    // -----------------------------------------
    // RESET
    // -----------------------------------------

    public void Reset()
    {
        creationTime = 0f;

        waterCount = 0;
        coralCount = 0;
        fishSchoolCount = 0;
        bubbleBurstCount = 0;

        indexGestureCount = 0;
        openPalmGestureCount = 0;
        vSignGestureCount = 0;
        thumbUpGestureCount = 0;

        totalHandDistance = 0f;

        averageHandSpeed = 0f;

        maximumHandSpeed = 0f;

        pauseTime = 0f;

        handTrackingSamples = 0;
    }
}