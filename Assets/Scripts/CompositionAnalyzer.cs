using System;
using System.Collections.Generic;
using UnityEngine;

public class CompositionAnalyzer : MonoBehaviour
{
    [Serializable]
    public class ElementInfo
    {
        public string type;

        public Vector2 normalizedPosition;
        public Vector2 normalizedSize;

        public float width;
        public float height;

        public float relativeSize;
    }

    [Serializable]
    public class EmptySpaceInfo
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public string largestEmptyRegion;
        public float largestEmptyPercentage;
    }

    [Serializable]
    public class CompositionSnapshot
    {
        public int totalElements;

        public float leftDensity;
        public float rightDensity;
        public float topDensity;
        public float bottomDensity;

        public EmptySpaceInfo emptySpace =
            new EmptySpaceInfo();

        public bool hasLargeObject;
        public string largeObjectType;
        public float largestObjectPercentage;

        public bool hasCluster;
        public float averageDistance;

        public List<ElementInfo> elements =
            new List<ElementInfo>();
    }

    [Header("Artwork Area")]
    [SerializeField]
    private float minX = -10f;

    [SerializeField]
    private float maxX = 10f;

    [SerializeField]
    private float minY = -5.6f;

    [SerializeField]
    private float maxY = 5.6f;

    [Header("Analysis")]
    [SerializeField]
    private float clusterDistance = 2.0f;

    [SerializeField]
    private float largeObjectPercentage = 0.12f;

    [SerializeField]
    private int gridColumns = 4;

    [SerializeField]
    private int gridRows = 3;

    // =========================================================
    // MAIN ANALYSIS
    // =========================================================

    public CompositionSnapshot Analyze()
    {
        CompositionSnapshot snapshot =
            new CompositionSnapshot();

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag(
                "ArtworkElement"
            );

        List<GameObject> validObjects =
            new List<GameObject>();

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                validObjects.Add(obj);
        }

        snapshot.totalElements =
            validObjects.Count;

        if (validObjects.Count == 0)
        {
            return snapshot;
        }

        // -----------------------------------------------------
        // CALCULATE TOTAL ARTWORK AREA
        // -----------------------------------------------------

        float artworkWidth =
            Mathf.Abs(maxX - minX);

        float artworkHeight =
            Mathf.Abs(maxY - minY);

        float artworkArea =
            artworkWidth * artworkHeight;

        // -----------------------------------------------------
        // DENSITY
        // -----------------------------------------------------

        int leftCount = 0;
        int rightCount = 0;
        int topCount = 0;
        int bottomCount = 0;

        // -----------------------------------------------------
        // LARGE OBJECT
        // -----------------------------------------------------

        float largestArea = 0f;
        string largestType = "";

        // -----------------------------------------------------
        // OBJECT INFORMATION
        // -----------------------------------------------------

        foreach (GameObject obj in validObjects)
        {
            Bounds bounds =
                CalculateWorldBounds(obj);

            Vector3 center =
                bounds.center;

            Vector3 size =
                bounds.size;

            float normalizedX =
                Mathf.InverseLerp(
                    minX,
                    maxX,
                    center.x
                );

            float normalizedY =
                Mathf.InverseLerp(
                    minY,
                    maxY,
                    center.y
                );

            float normalizedWidth =
                Mathf.Clamp01(
                    size.x / artworkWidth
                );

            float normalizedHeight =
                Mathf.Clamp01(
                    size.y / artworkHeight
                );

            float objectArea =
                size.x * size.y;

            float relativeArea =
                artworkArea > 0f
                    ? objectArea / artworkArea
                    : 0f;

            ElementInfo info =
                new ElementInfo();

            info.type =
                GetElementType(obj);

            info.normalizedPosition =
                new Vector2(
                    normalizedX,
                    normalizedY
                );

            info.normalizedSize =
                new Vector2(
                    normalizedWidth,
                    normalizedHeight
                );

            info.width =
                size.x;

            info.height =
                size.y;

            info.relativeSize =
                relativeArea;

            snapshot.elements.Add(info);

            // -------------------------------------------------
            // LEFT / RIGHT
            // -------------------------------------------------

            if (normalizedX < 0.5f)
                leftCount++;
            else
                rightCount++;

            // -------------------------------------------------
            // TOP / BOTTOM
            // -------------------------------------------------

            if (normalizedY >= 0.5f)
                topCount++;
            else
                bottomCount++;

            // -------------------------------------------------
            // LARGEST OBJECT
            // -------------------------------------------------

            if (objectArea > largestArea)
            {
                largestArea =
                    objectArea;

                largestType =
                    info.type;
            }
        }

        // -----------------------------------------------------
        // DENSITY VALUES
        // -----------------------------------------------------

        snapshot.leftDensity =
            (float)leftCount /
            validObjects.Count;

        snapshot.rightDensity =
            (float)rightCount /
            validObjects.Count;

        snapshot.topDensity =
            (float)topCount /
            validObjects.Count;

        snapshot.bottomDensity =
            (float)bottomCount /
            validObjects.Count;

        // -----------------------------------------------------
        // LARGE OBJECT
        // -----------------------------------------------------

        snapshot.largestObjectPercentage =
            largestArea /
            Mathf.Max(artworkArea, 0.0001f);

        if (snapshot.largestObjectPercentage >=
            largeObjectPercentage)
        {
            snapshot.hasLargeObject = true;

            snapshot.largeObjectType =
                largestType;
        }

        // -----------------------------------------------------
        // EMPTY SPACE
        // -----------------------------------------------------

        snapshot.emptySpace =
            CalculateEmptySpace(
                validObjects
            );

        // -----------------------------------------------------
        // CLUSTERING
        // -----------------------------------------------------

        CalculateClustering(
            validObjects,
            snapshot
        );

        return snapshot;
    }

    // =========================================================
    // WORLD BOUNDS
    // =========================================================

    private Bounds CalculateWorldBounds(
        GameObject obj
    )
    {
        Renderer[] renderers =
            obj.GetComponentsInChildren<
                Renderer
            >(true);

        if (renderers.Length == 0)
        {
            return new Bounds(
                obj.transform.position,
                Vector3.zero
            );
        }

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(
                    renderers[i].bounds
                );
            }
        }

        return bounds;
    }

    // =========================================================
    // EMPTY SPACE
    // =========================================================

    private EmptySpaceInfo CalculateEmptySpace(
        List<GameObject> objects
    )
    {
        EmptySpaceInfo result =
            new EmptySpaceInfo();

        int[,] occupied =
            new int[
                gridColumns,
                gridRows
            ];

        foreach (GameObject obj in objects)
        {
            Bounds bounds =
                CalculateWorldBounds(obj);

            Vector3 center =
                bounds.center;

            float normalizedX =
                Mathf.InverseLerp(
                    minX,
                    maxX,
                    center.x
                );

            float normalizedY =
                Mathf.InverseLerp(
                    minY,
                    maxY,
                    center.y
                );

            int column =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        normalizedX *
                        gridColumns
                    ),
                    0,
                    gridColumns - 1
                );

            int row =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        normalizedY *
                        gridRows
                    ),
                    0,
                    gridRows - 1
                );

            occupied[
                column,
                row
            ]++;
        }

        // -----------------------------------------------------
        // COUNT EMPTY CELLS
        // -----------------------------------------------------

        int totalCells =
            gridColumns *
            gridRows;

        int emptyCells = 0;

        int leftEmpty = 0;
        int rightEmpty = 0;
        int topEmpty = 0;
        int bottomEmpty = 0;

        int largestEmptyRun = 0;
        string largestRegion = "";

        for (int x = 0;
             x < gridColumns;
             x++)
        {
            for (int y = 0;
                 y < gridRows;
                 y++)
            {
                if (occupied[x, y] == 0)
                {
                    emptyCells++;

                    if (x < gridColumns / 2)
                        leftEmpty++;

                    else
                        rightEmpty++;

                    if (y >= gridRows / 2)
                        topEmpty++;

                    else
                        bottomEmpty++;

                    int emptyNeighbours =
                        CountEmptyNeighbours(
                            occupied,
                            x,
                            y
                        );

                    if (emptyNeighbours >
                        largestEmptyRun)
                    {
                        largestEmptyRun =
                            emptyNeighbours;

                        largestRegion =
                            GetRegionName(
                                x,
                                y
                            );
                    }
                }
            }
        }

        result.left =
            (float)leftEmpty /
            Mathf.Max(
                1,
                gridColumns / 2 *
                gridRows
            );

        result.right =
            (float)rightEmpty /
            Mathf.Max(
                1,
                gridColumns / 2 *
                gridRows
            );

        result.top =
            (float)topEmpty /
            Mathf.Max(
                1,
                gridColumns *
                (gridRows / 2)
            );

        result.bottom =
            (float)bottomEmpty /
            Mathf.Max(
                1,
                gridColumns *
                (gridRows / 2)
            );

        result.largestEmptyPercentage =
            (float)emptyCells /
            Mathf.Max(
                1,
                totalCells
            );

        result.largestEmptyRegion =
            largestRegion;

        return result;
    }

    // =========================================================
    // EMPTY NEIGHBOURS
    // =========================================================

    private int CountEmptyNeighbours(
        int[,] occupied,
        int x,
        int y
    )
    {
        int count = 0;

        int width =
            occupied.GetLength(0);

        int height =
            occupied.GetLength(1);

        if (x > 0 &&
            occupied[x - 1, y] == 0)
            count++;

        if (x < width - 1 &&
            occupied[x + 1, y] == 0)
            count++;

        if (y > 0 &&
            occupied[x, y - 1] == 0)
            count++;

        if (y < height - 1 &&
            occupied[x, y + 1] == 0)
            count++;

        return count;
    }

    // =========================================================
    // REGION NAME
    // =========================================================

    private string GetRegionName(
        int x,
        int y
    )
    {
        string horizontal =
            x < gridColumns / 2
                ? "left"
                : "right";

        string vertical =
            y >= gridRows / 2
                ? "top"
                : "bottom";

        return vertical +
               "-" +
               horizontal;
    }

    // =========================================================
    // CLUSTERING
    // =========================================================

    private void CalculateClustering(
        List<GameObject> objects,
        CompositionSnapshot snapshot
    )
    {
        if (objects.Count < 2)
            return;

        float totalDistance = 0f;
        int pairCount = 0;

        foreach (GameObject first in objects)
        {
            foreach (GameObject second in objects)
            {
                if (first == second)
                    continue;

                float distance =
                    Vector3.Distance(
                        first.transform.position,
                        second.transform.position
                    );

                totalDistance +=
                    distance;

                pairCount++;

                if (distance <=
                    clusterDistance)
                {
                    snapshot.hasCluster =
                        true;
                }
            }
        }

        if (pairCount > 0)
        {
            snapshot.averageDistance =
                totalDistance /
                pairCount;
        }
    }

    // =========================================================
    // OBJECT TYPE
    // =========================================================

    private string GetElementType(
        GameObject obj
    )
    {
        string objectName =
            obj.name.ToLower();

        if (objectName.Contains("fish"))
            return "fish";

        if (objectName.Contains("coral"))
            return "coral";

        if (objectName.Contains("bubble"))
            return "bubble";

        if (objectName.Contains("water"))
            return "water";

        if (objectName.Contains("lantern"))
            return "lantern";

        return "unknown";
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public void DebugAnalyze()
    {
        CompositionSnapshot snapshot =
            Analyze();

        Debug.Log(
            "===== CAPTRACK COMPOSITION ====="
        );

        Debug.Log(
            "Total Elements: " +
            snapshot.totalElements
        );

        Debug.Log(
            "Left Density: " +
            snapshot.leftDensity
        );

        Debug.Log(
            "Right Density: " +
            snapshot.rightDensity
        );

        Debug.Log(
            "Top Density: " +
            snapshot.topDensity
        );

        Debug.Log(
            "Bottom Density: " +
            snapshot.bottomDensity
        );

        Debug.Log(
            "Largest Object: " +
            snapshot.hasLargeObject +
            " (" +
            snapshot.largeObjectType +
            ")"
        );

        Debug.Log(
            "Largest Object %: " +
            snapshot.largestObjectPercentage
        );

        Debug.Log(
            "Empty Left: " +
            snapshot.emptySpace.left
        );

        Debug.Log(
            "Empty Right: " +
            snapshot.emptySpace.right
        );

        Debug.Log(
            "Empty Top: " +
            snapshot.emptySpace.top
        );

        Debug.Log(
            "Empty Bottom: " +
            snapshot.emptySpace.bottom
        );

        Debug.Log(
            "Largest Empty Region: " +
            snapshot.emptySpace.largestEmptyRegion
        );

        Debug.Log(
            "Empty Space %: " +
            snapshot.emptySpace.largestEmptyPercentage
        );

        Debug.Log(
            "Cluster: " +
            snapshot.hasCluster
        );

        Debug.Log(
            "Average Distance: " +
            snapshot.averageDistance
        );

        Debug.Log(
            "===== ELEMENTS ====="
        );

        foreach (
            ElementInfo element
            in snapshot.elements
        )
        {
            Debug.Log(
                element.type +
                " | Position: " +
                element.normalizedPosition +
                " | Size: " +
                element.normalizedSize +
                " | Relative Area: " +
                element.relativeSize
            );
        }
    }
}