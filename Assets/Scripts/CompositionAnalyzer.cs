using System;
using System.Collections.Generic;
using UnityEngine;

public class CompositionAnalyzer : MonoBehaviour
{
    public static CompositionAnalyzer Instance;

    // =========================================================
    // ELEMENT INFORMATION
    // =========================================================

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

    // =========================================================
    // EMPTY SPACE INFORMATION
    // =========================================================

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

    // =========================================================
    // ZONE INFORMATION
    // =========================================================

    [Serializable]
    public class ZoneInfo
    {
        public string name;
        public int elementCount;
        public float density;
        public string dominantElementType;
        public bool isEmpty;
        public bool isCrowded;
    }

    // =========================================================
    // COMPOSITION SNAPSHOT
    // =========================================================

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

        public List<ZoneInfo> zones =
            new List<ZoneInfo>();
    }

    // =========================================================
    // ARTWORK AREA
    // =========================================================

    [Header("Artwork Area")]

    [SerializeField]
    private float minX = -10f;

    [SerializeField]
    private float maxX = 10f;

    [SerializeField]
    private float minY = -5.6f;

    [SerializeField]
    private float maxY = 5.6f;

    // =========================================================
    // ANALYSIS SETTINGS
    // =========================================================

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
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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
            if (
                obj != null &&
                obj.activeInHierarchy
            )
            {
                validObjects.Add(obj);
            }
        }

        snapshot.totalElements =
            validObjects.Count;

        if (validObjects.Count == 0)
        {
            snapshot.zones =
                BuildZoneGrid(snapshot.elements);

            return snapshot;
        }

        float artworkWidth =
            Mathf.Abs(maxX - minX);

        float artworkHeight =
            Mathf.Abs(maxY - minY);

        float artworkArea =
            artworkWidth * artworkHeight;

        int leftCount = 0;
        int rightCount = 0;
        int topCount = 0;
        int bottomCount = 0;

        float largestArea = 0f;
        string largestType = "";

        foreach (
            GameObject obj
            in validObjects
        )
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
                    size.x /
                    Mathf.Max(
                        artworkWidth,
                        0.0001f
                    )
                );

            float normalizedHeight =
                Mathf.Clamp01(
                    size.y /
                    Mathf.Max(
                        artworkHeight,
                        0.0001f
                    )
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

            if (normalizedX < 0.5f)
                leftCount++;
            else
                rightCount++;

            if (normalizedY >= 0.5f)
                topCount++;
            else
                bottomCount++;

            if (objectArea > largestArea)
            {
                largestArea = objectArea;
                largestType = info.type;
            }
        }

        snapshot.zones =
            BuildZoneGrid(
                snapshot.elements
            );

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

        snapshot.largestObjectPercentage =
            largestArea /
            Mathf.Max(
                artworkArea,
                0.0001f
            );

        if (
            snapshot.largestObjectPercentage >=
            largeObjectPercentage
        )
        {
            snapshot.hasLargeObject = true;
            snapshot.largeObjectType = largestType;
        }

        snapshot.emptySpace =
            CalculateEmptySpace(
                validObjects
            );

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
            obj.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers.Length == 0)
        {
            return new Bounds(
                obj.transform.position,
                Vector3.zero
            );
        }

        Bounds bounds =
            renderers[0].bounds;

        for (
            int i = 1;
            i < renderers.Length;
            i++
        )
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

        foreach (
            GameObject obj
            in objects
        )
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

            occupied[column, row]++;
        }

        int totalCells =
            gridColumns * gridRows;

        int emptyCells = 0;
        int leftEmpty = 0;
        int rightEmpty = 0;
        int topEmpty = 0;
        int bottomEmpty = 0;

        int largestEmptyRun = 0;
        string largestRegion = "";

        for (
            int x = 0;
            x < gridColumns;
            x++
        )
        {
            for (
                int y = 0;
                y < gridRows;
                y++
            )
            {
                if (occupied[x, y] != 0)
                    continue;

                emptyCells++;

                if (
                    x <
                    gridColumns / 2
                )
                {
                    leftEmpty++;
                }
                else
                {
                    rightEmpty++;
                }

                if (
                    y >=
                    gridRows / 2
                )
                {
                    topEmpty++;
                }
                else
                {
                    bottomEmpty++;
                }

                int emptyNeighbours =
                    CountEmptyNeighbours(
                        occupied,
                        x,
                        y
                    );

                if (
                    emptyNeighbours >
                    largestEmptyRun
                )
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

        result.left =
            (float)leftEmpty /
            Mathf.Max(
                1,
                (gridColumns / 2) *
                gridRows
            );

        result.right =
            (float)rightEmpty /
            Mathf.Max(
                1,
                (gridColumns / 2) *
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

        if (
            x > 0 &&
            occupied[x - 1, y] == 0
        )
        {
            count++;
        }

        if (
            x < width - 1 &&
            occupied[x + 1, y] == 0
        )
        {
            count++;
        }

        if (
            y > 0 &&
            occupied[x, y - 1] == 0
        )
        {
            count++;
        }

        if (
            y < height - 1 &&
            occupied[x, y + 1] == 0
        )
        {
            count++;
        }

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

        foreach (
            GameObject first
            in objects
        )
        {
            foreach (
                GameObject second
                in objects
            )
            {
                if (first == second)
                    continue;

                float distance =
                    Vector3.Distance(
                        first.transform.position,
                        second.transform.position
                    );

                totalDistance += distance;
                pairCount++;

                if (
                    distance <=
                    clusterDistance
                )
                {
                    snapshot.hasCluster = true;
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

    public static string GetElementType(
        GameObject obj
    )
    {
        if (obj == null)
            return "unknown";

        string objectName =
            obj.name.ToLowerInvariant();

        if (objectName.Contains("fish"))
            return "fish_school";

        if (objectName.Contains("coral"))
            return "coral";

        if (objectName.Contains("bubble"))
            return "bubble_burst";

        if (objectName.Contains("water"))
            return "water_current";

        if (objectName.Contains("lantern"))
            return "lantern";

        if (objectName.Contains("petal"))
            return "petals";

        if (objectName.Contains("firework"))
            return "fireworks";

        if (
            objectName.Contains("lighttrail") ||
            objectName.Contains("goldenlight") ||
            objectName.Contains("trail")
        )
        {
            return "light_trail";
        }

        return "unknown";
    }

    // =========================================================
    // 3 x 3 GRID
    // =========================================================

    public static List<ZoneInfo> BuildZoneGrid(
        List<ElementInfo> elements
    )
    {
        const int gridSize = 3;

        List<ZoneInfo> zones =
            new List<ZoneInfo>();

        Dictionary<string, int>[] typeCounts =
            new Dictionary<string, int>[
                gridSize * gridSize
            ];

        string[] rows =
        {
            "TOP",
            "CENTER",
            "BOTTOM"
        };

        string[] columns =
        {
            "LEFT",
            "CENTER",
            "RIGHT"
        };

        for (
            int row = 0;
            row < gridSize;
            row++
        )
        {
            for (
                int column = 0;
                column < gridSize;
                column++
            )
            {
                int index =
                    row * gridSize +
                    column;

                zones.Add(
                    new ZoneInfo
                    {
                        name =
                            rows[row] +
                            "_" +
                            columns[column],

                        dominantElementType = "",

                        isEmpty = true
                    }
                );

                typeCounts[index] =
                    new Dictionary<string, int>();
            }
        }

        int total =
            elements == null
                ? 0
                : elements.Count;

        if (elements != null)
        {
            foreach (
                ElementInfo element
                in elements
            )
            {
                if (element == null)
                    continue;

                int column =
                    Mathf.Clamp(
                        Mathf.FloorToInt(
                            element.normalizedPosition.x *
                            gridSize
                        ),
                        0,
                        gridSize - 1
                    );

                int bottomRow =
                    Mathf.Clamp(
                        Mathf.FloorToInt(
                            element.normalizedPosition.y *
                            gridSize
                        ),
                        0,
                        gridSize - 1
                    );

                int displayRow =
                    gridSize -
                    1 -
                    bottomRow;

                int index =
                    displayRow *
                    gridSize +
                    column;

                zones[index].elementCount++;

                string type =
                    string.IsNullOrEmpty(
                        element.type
                    )
                        ? "unknown"
                        : element.type;

                if (
                    !typeCounts[index]
                        .ContainsKey(type)
                )
                {
                    typeCounts[index][type] = 0;
                }

                typeCounts[index][type]++;
            }
        }

        int crowdedThreshold =
            Mathf.Max(
                2,
                Mathf.CeilToInt(
                    total / 3f
                )
            );

        for (
            int index = 0;
            index < zones.Count;
            index++
        )
        {
            ZoneInfo zone =
                zones[index];

            zone.density =
                total > 0
                    ? (float)zone.elementCount /
                      total
                    : 0f;

            zone.isEmpty =
                zone.elementCount == 0;

            zone.isCrowded =
                zone.elementCount >=
                crowdedThreshold;

            int largest = 0;

            List<string> types =
                new List<string>(
                    typeCounts[index].Keys
                );

            types.Sort(
                StringComparer.Ordinal
            );

            foreach (
                string type
                in types
            )
            {
                int count =
                    typeCounts[index][type];

                if (count > largest)
                {
                    largest = count;

                    zone.dominantElementType =
                        type;
                }
            }
        }

        return zones;
    }

    // =========================================================
    // DEBUG ANALYSIS
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
            snapshot.emptySpace
                .largestEmptyRegion
        );

        Debug.Log(
            "Empty Space %: " +
            snapshot.emptySpace
                .largestEmptyPercentage
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

        Debug.Log(
            "===== ZONES ====="
        );

        foreach (
            ZoneInfo zone
            in snapshot.zones
        )
        {
            Debug.Log(
                zone.name +
                " | Count: " +
                zone.elementCount +
                " | Dominant: " +
                zone.dominantElementType +
                " | Crowded: " +
                zone.isCrowded
            );
        }
    }
}

// =============================================================
// COMPOSITION ADVISOR RECOMMENDATION
// =============================================================

[Serializable]
public class CompositionAdvisorRecommendation
{
    public string action = "NONE";
    public string element = "";
    public string zone = "";
    public string reason = "";
    public string fallbackMessage = "";
}

// =============================================================
// COMPOSITION ADVISOR
// =============================================================

public static class CompositionAdvisor
{
    // =========================================================
    // THEME ALLOWLIST
    // =========================================================

    public static bool IsElementAllowed(
        string theme,
        string type
    )
    {
        if (
            string.Equals(
                theme,
                "Taiwan",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return
                type == "light_trail" ||
                type == "lantern" ||
                type == "petals" ||
                type == "fireworks";
        }

        if (
            string.Equals(
                theme,
                "Underwater",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return
                type == "water_current" ||
                type == "coral" ||
                type == "fish_school" ||
                type == "bubble_burst";
        }

        return false;
    }

    // =========================================================
    // MAIN ADVISOR
    // =========================================================

    public static CompositionAdvisorRecommendation Analyze(
        List<CompositionAnalyzer.ElementInfo> elements,
        string theme
    )
    {
        CompositionAdvisorRecommendation result =
            new CompositionAdvisorRecommendation();

        List<CompositionAnalyzer.ElementInfo> valid =
            new List<CompositionAnalyzer.ElementInfo>();

        if (elements != null)
        {
            foreach (
                CompositionAnalyzer.ElementInfo item
                in elements
            )
            {
                if (
                    item != null &&
                    IsElementAllowed(
                        theme,
                        item.type
                    )
                )
                {
                    valid.Add(item);
                }
            }
        }

        // ---------------------------------------------------------
        // NO ARTWORK
        // ---------------------------------------------------------

        if (valid.Count == 0)
        {
            result.action = "NONE";
            result.element = "";
            result.zone = "";

            result.reason =
                "Unity found no active elements allowed by the current theme.";

            result.fallbackMessage =
                "There are no active " +
                theme +
                " elements to review yet.";

            return result;
        }

        // ---------------------------------------------------------
        // BUILD ZONES
        // ---------------------------------------------------------

        List<CompositionAnalyzer.ZoneInfo> zones =
            CompositionAnalyzer.BuildZoneGrid(
                valid
            );

        // ---------------------------------------------------------
        // FIND MOST OCCUPIED ZONE
        // ---------------------------------------------------------

        int sourceIndex =
            FindMostOccupiedZone(
                zones
            );

        CompositionAnalyzer.ZoneInfo source =
            zones[sourceIndex];

        string sourceElement =
            source.dominantElementType;

        if (
            string.IsNullOrEmpty(
                sourceElement
            )
        )
        {
            sourceElement =
                MostCommonType(valid);
        }

        // ---------------------------------------------------------
        // FIND ELEMENT COUNTS
        // ---------------------------------------------------------

        Dictionary<string, int> typeCounts =
            CountTypes(valid);

        // ---------------------------------------------------------
        // FIND EMPTY / NON-CROWDED TARGET
        // ---------------------------------------------------------

        int emptyZoneIndex =
            FindBestOpenZone(
                zones,
                sourceIndex
            );

        // ---------------------------------------------------------
        // 1. CROWDED COMPOSITION
        // ---------------------------------------------------------

        // This takes priority over all theme-specific behavior.

        if (source.isCrowded)
        {
            if (emptyZoneIndex >= 0)
            {
                CompositionAnalyzer.ZoneInfo target =
                    zones[emptyZoneIndex];

                result.action = "SPREAD";

                result.element =
                    sourceElement;

                result.zone =
                    target.name;

                result.reason =
                    "The " +
                    Place(source.name) +
                    " zone contains " +
                    source.elementCount +
                    " elements and is the strongest crowded area, " +
                    "while " +
                    Place(target.name) +
                    " has more available space.";

                result.fallbackMessage =
                    "Your " +
                    Plural(sourceElement) +
                    " are concentrated in the " +
                    Place(source.name) +
                    ". Try placing the next " +
                    Singular(sourceElement) +
                    " near the " +
                    Place(target.name) +
                    " to spread the visual weight.";

                return result;
            }

            result.action = "LEAVE_OPEN";

            result.element =
                sourceElement;

            result.zone =
                source.name;

            result.reason =
                "The strongest area is already crowded and there is no useful non-crowded target.";

            result.fallbackMessage =
                "Your " +
                Plural(sourceElement) +
                " already create a strong area of focus. Leaving nearby space open will help the composition breathe.";

            return result;
        }

        // ---------------------------------------------------------
        // 2. STRONG HORIZONTAL / VERTICAL IMBALANCE
        // ---------------------------------------------------------

        int left = 0;
        int right = 0;
        int top = 0;
        int bottom = 0;

        foreach (
            CompositionAnalyzer.ElementInfo item
            in valid
        )
        {
            if (
                item.normalizedPosition.x <
                1f / 3f
            )
            {
                left++;
            }
            else if (
                item.normalizedPosition.x >=
                2f / 3f
            )
            {
                right++;
            }

            if (
                item.normalizedPosition.y >=
                2f / 3f
            )
            {
                top++;
            }
            else if (
                item.normalizedPosition.y <
                1f / 3f
            )
            {
                bottom++;
            }
        }

        bool horizontal =
            Mathf.Abs(left - right) /
            (float)valid.Count >=
            0.30f;

        bool vertical =
            Mathf.Abs(top - bottom) /
            (float)valid.Count >=
            0.30f;

        int preferredColumn =
            horizontal
                ? left < right
                    ? 0
                    : 2
                : -1;

        int preferredRow =
            vertical
                ? top > bottom
                    ? 2
                    : 0
                : -1;

        int balanceTarget =
            FindTargetZone(
                zones,
                preferredColumn,
                preferredRow,
                sourceIndex
            );

        if (
            (horizontal || vertical) &&
            balanceTarget >= 0
        )
        {
            CompositionAnalyzer.ZoneInfo target =
                zones[balanceTarget];

            result.action = "BALANCE";

            result.element =
                sourceElement;

            result.zone =
                target.name;

            if (horizontal)
            {
                result.reason =
                    "Unity counted " +
                    left +
                    " elements on the left and " +
                    right +
                    " on the right; " +
                    Place(target.name) +
                    " is the less populated target area.";

                result.fallbackMessage =
                    "Your " +
                    Plural(sourceElement) +
                    " carry more visual weight on the " +
                    (left > right
                        ? "left"
                        : "right") +
                    ". Placing the next " +
                    Singular(sourceElement) +
                    " near the " +
                    Place(target.name) +
                    " could balance the scene.";
            }
            else
            {
                result.reason =
                    "Unity counted " +
                    top +
                    " elements in the upper half and " +
                    bottom +
                    " in the lower half; " +
                    Place(target.name) +
                    " is the less populated target area.";

                result.fallbackMessage =
                    "Your " +
                    Plural(sourceElement) +
                    " carry more visual weight toward the " +
                    (top > bottom
                        ? "top"
                        : "bottom") +
                    ". Placing the next " +
                    Singular(sourceElement) +
                    " near the " +
                    Place(target.name) +
                    " could balance the scene.";
            }

            return result;
        }

        // ---------------------------------------------------------
        // 3. LARGE OBJECT / FOCAL POINT
        // ---------------------------------------------------------

        CompositionAnalyzer.ElementInfo largeElement =
            FindLargestElement(valid);

        if (
            largeElement != null &&
            largeElement.relativeSize >=
            0.12f
        )
        {
            string largeZone =
                ZoneFor(
                    largeElement.normalizedPosition
                );

            result.action =
                "LEAVE_OPEN";

            result.element =
                largeElement.type;

            result.zone =
                largeZone;

            result.reason =
                "A relatively large " +
                Singular(largeElement.type) +
                " is creating a clear focal point in the " +
                Place(largeZone) +
                ".";

            result.fallbackMessage =
                "Your " +
                Singular(largeElement.type) +
                " creates a strong focal point in the " +
                Place(largeZone) +
                ". Leaving nearby space open will help it stand out.";

            return result;
        }

        // ---------------------------------------------------------
        // 4. ISOLATED ELEMENT
        // ---------------------------------------------------------

        CompositionAnalyzer.ElementInfo isolated =
            FindIsolatedElement(
                valid
            );

        if (
            isolated != null &&
            emptyZoneIndex >= 0
        )
        {
            CompositionAnalyzer.ZoneInfo target =
                zones[emptyZoneIndex];

            // Do NOT force bubble burst for coral.
            // Do NOT force fireworks for Taiwan.
            //
            // Instead, keep the actual isolated element as
            // the subject of the feedback.

            result.action =
                "BALANCE";

            result.element =
                isolated.type;

            result.zone =
                target.name;

            result.reason =
                "A " +
                Singular(isolated.type) +
                " is isolated from the surrounding artwork, while " +
                Place(target.name) +
                " has available space.";

            result.fallbackMessage =
                "Your " +
                Singular(isolated.type) +
                " has a lot of space around it. You can use the nearby open area to build a stronger visual connection.";

            return result;
        }

        // ---------------------------------------------------------
        // 5. SIZE VARIATION
        // ---------------------------------------------------------

        float smallest =
            float.MaxValue;

        float largest =
            0f;

        foreach (
            CompositionAnalyzer.ElementInfo item
            in valid
        )
        {
            if (item.relativeSize <= 0f)
                continue;

            smallest =
                Mathf.Min(
                    smallest,
                    item.relativeSize
                );

            largest =
                Mathf.Max(
                    largest,
                    item.relativeSize
                );
        }

        if (
            smallest < float.MaxValue &&
            largest > 0f &&
            (largest - smallest) /
            largest < 0.20f &&
            valid.Count >= 3
        )
        {
            string sizeElement =
                SelectRepresentativeElement(
                    valid
                );

            result.action =
                "VARY_SIZE";

            result.element =
                sizeElement;

            result.zone = "";

            result.reason =
                "The measured elements are similar in size, so more size variation could create visual rhythm.";

            result.fallbackMessage =
                "Your elements are similar in scale. Varying the size of your next " +
                Singular(sizeElement) +
                " could add more visual rhythm.";

            return result;
        }

        // ---------------------------------------------------------
        // 6. MODERATE EMPTY SPACE
        // ---------------------------------------------------------

        int meaningfulEmptyZone =
            FindBestOpenZone(
                zones,
                sourceIndex
            );

        if (
            meaningfulEmptyZone >= 0 &&
            valid.Count <= 3
        )
        {
            CompositionAnalyzer.ZoneInfo target =
                zones[meaningfulEmptyZone];

            // Choose an element intelligently rather than
            // defaulting to fireworks or bubbles.

            string addition =
                SelectAdditionElement(
                    theme,
                    typeCounts,
                    valid,
                    target.name
                );

            result.action =
                "ADD";

            result.element =
                addition;

            result.zone =
                target.name;

            result.reason =
                "The artwork is still developing and " +
                Place(target.name) +
                " has available space for another element.";

            result.fallbackMessage =
                "There is open space in the " +
                Place(target.name) +
                ". Adding a " +
                Singular(addition) +
                " there could extend the composition.";

            return result;
        }

        // ---------------------------------------------------------
        // 7. GOOD COMPOSITION
        // ---------------------------------------------------------

        result.action =
            "NONE";

        result.element =
            sourceElement;

        result.zone = "";

        result.reason =
            "No strong crowding or directional imbalance was detected.";

        result.fallbackMessage =
            "Your elements are distributed across the scene without a clearly dominant problem. Keep building the composition at your own pace.";

        return result;
    }

    // =========================================================
    // FIND MOST OCCUPIED ZONE
    // =========================================================

    private static int FindMostOccupiedZone(
        List<CompositionAnalyzer.ZoneInfo> zones
    )
    {
        int best =
            0;

        for (
            int i = 1;
            i < zones.Count;
            i++
        )
        {
            if (
                zones[i].elementCount >
                zones[best].elementCount
            )
            {
                best = i;
            }
        }

        return best;
    }

    // =========================================================
    // FIND OPEN ZONE
    // =========================================================

    private static int FindBestOpenZone(
        List<CompositionAnalyzer.ZoneInfo> zones,
        int sourceIndex
    )
    {
        int best =
            -1;

        int bestScore =
            int.MaxValue;

        for (
            int i = 0;
            i < zones.Count;
            i++
        )
        {
            CompositionAnalyzer.ZoneInfo zone =
                zones[i];

            if (zone.isCrowded)
                continue;

            int score =
                zone.elementCount * 100;

            if (i == sourceIndex)
                score += 20;

            if (zone.isEmpty)
                score -= 20;

            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        return best;
    }

    // =========================================================
    // FIND BALANCE TARGET
    // =========================================================

    private static int FindTargetZone(
        List<CompositionAnalyzer.ZoneInfo> zones,
        int preferredColumn,
        int preferredRow,
        int sourceIndex
    )
    {
        int best =
            -1;

        int bestScore =
            int.MaxValue;

        for (
            int i = 0;
            i < zones.Count;
            i++
        )
        {
            if (zones[i].isCrowded)
                continue;

            int row =
                i / 3;

            int column =
                i % 3;

            int score =
                zones[i].elementCount *
                100;

            if (
                preferredColumn >= 0 &&
                column != preferredColumn
            )
            {
                score += 30;
            }

            if (
                preferredRow >= 0 &&
                row != preferredRow
            )
            {
                score += 15;
            }

            if (i == sourceIndex)
                score += 20;

            if (zones[i].isEmpty)
                score -= 15;

            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        return best;
    }

    // =========================================================
    // FIND LARGEST ELEMENT
    // =========================================================

    private static CompositionAnalyzer.ElementInfo
        FindLargestElement(
            List<CompositionAnalyzer.ElementInfo> elements
        )
    {
        CompositionAnalyzer.ElementInfo best =
            null;

        float largest =
            0f;

        foreach (
            CompositionAnalyzer.ElementInfo element
            in elements
        )
        {
            if (
                element == null ||
                element.relativeSize <= 0f
            )
            {
                continue;
            }

            if (
                element.relativeSize >
                largest
            )
            {
                largest =
                    element.relativeSize;

                best =
                    element;
            }
        }

        return best;
    }

    // =========================================================
    // FIND ISOLATED ELEMENT
    // =========================================================

    private static CompositionAnalyzer.ElementInfo
        FindIsolatedElement(
            List<CompositionAnalyzer.ElementInfo> elements
        )
    {
        CompositionAnalyzer.ElementInfo best =
            null;

        float bestDistance =
            0f;

        foreach (
            CompositionAnalyzer.ElementInfo element
            in elements
        )
        {
            if (element == null)
                continue;

            float nearest =
                float.MaxValue;

            foreach (
                CompositionAnalyzer.ElementInfo other
                in elements
            )
            {
                if (
                    other == null ||
                    other == element
                )
                {
                    continue;
                }

                float distance =
                    Vector2.Distance(
                        element.normalizedPosition,
                        other.normalizedPosition
                    );

                nearest =
                    Mathf.Min(
                        nearest,
                        distance
                    );
            }

            if (
                nearest != float.MaxValue &&
                nearest > 0.20f &&
                nearest > bestDistance
            )
            {
                bestDistance =
                    nearest;

                best =
                    element;
            }
        }

        return best;
    }

    // =========================================================
    // COUNT TYPES
    // =========================================================

    private static Dictionary<string, int>
        CountTypes(
            List<CompositionAnalyzer.ElementInfo> elements
        )
    {
        Dictionary<string, int> counts =
            new Dictionary<string, int>();

        foreach (
            CompositionAnalyzer.ElementInfo element
            in elements
        )
        {
            if (element == null)
                continue;

            if (
                !counts.ContainsKey(
                    element.type
                )
            )
            {
                counts[element.type] = 0;
            }

            counts[element.type]++;
        }

        return counts;
    }

    // =========================================================
    // SELECT ADDITION ELEMENT
    // =========================================================

    private static string SelectAdditionElement(
        string theme,
        Dictionary<string, int> counts,
        List<CompositionAnalyzer.ElementInfo> elements,
        string targetZone
    )
    {
        List<string> candidates =
            new List<string>();

        if (
            string.Equals(
                theme,
                "Taiwan",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            candidates.Add("light_trail");
            candidates.Add("lantern");
            candidates.Add("petals");
            candidates.Add("fireworks");
        }
        else if (
            string.Equals(
                theme,
                "Underwater",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            candidates.Add("water_current");
            candidates.Add("coral");
            candidates.Add("fish_school");
            candidates.Add("bubble_burst");
        }

        if (candidates.Count == 0)
            return "";

        // Find the least-used valid element.
        //
        // This prevents the system from always selecting fireworks
        // or bubbles simply because the theme supports them.

        string best =
            candidates[0];

        int bestCount =
            GetCount(
                counts,
                best
            );

        for (
            int i = 1;
            i < candidates.Count;
            i++
        )
        {
            string candidate =
                candidates[i];

            int count =
                GetCount(
                    counts,
                    candidate
                );

            if (
                count <
                bestCount
            )
            {
                best =
                    candidate;

                bestCount =
                    count;
            }
        }

        // If all four are equally unused, use the first candidate.
        // This is only used for an ADD recommendation.
        return best;
    }

    // =========================================================
    // GET COUNT
    // =========================================================

    private static int GetCount(
        Dictionary<string, int> counts,
        string type
    )
    {
        if (
            counts != null &&
            counts.ContainsKey(type)
        )
        {
            return counts[type];
        }

        return 0;
    }

    // =========================================================
    // REPRESENTATIVE ELEMENT
    // =========================================================

    private static string SelectRepresentativeElement(
        List<CompositionAnalyzer.ElementInfo> elements
    )
    {
        if (
            elements == null ||
            elements.Count == 0
        )
        {
            return "";
        }

        Dictionary<string, int> counts =
            CountTypes(elements);

        string best =
            "";

        int maximum =
            -1;

        List<string> types =
            new List<string>(
                counts.Keys
            );

        types.Sort(
            StringComparer.Ordinal
        );

        foreach (
            string type
            in types
        )
        {
            if (
                counts[type] >
                maximum
            )
            {
                maximum =
                    counts[type];

                best =
                    type;
            }
        }

        return best;
    }

    // =========================================================
    // MOST COMMON TYPE
    // =========================================================

    private static string MostCommonType(
        List<CompositionAnalyzer.ElementInfo> elements
    )
    {
        return SelectRepresentativeElement(
            elements
        );
    }

    // =========================================================
    // CONTAINS TYPE
    // =========================================================

    private static bool ContainsType(
        List<CompositionAnalyzer.ElementInfo> elements,
        string type
    )
    {
        foreach (
            CompositionAnalyzer.ElementInfo item
            in elements
        )
        {
            if (
                item != null &&
                item.type == type
            )
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // FIRST OF TYPE
    // =========================================================

    private static CompositionAnalyzer.ElementInfo
        FirstOfType(
            List<CompositionAnalyzer.ElementInfo> elements,
            string type
        )
    {
        foreach (
            CompositionAnalyzer.ElementInfo item
            in elements
        )
        {
            if (
                item != null &&
                item.type == type
            )
            {
                return item;
            }
        }

        return null;
    }

    // =========================================================
    // ZONE FOR POSITION
    // =========================================================

    private static string ZoneFor(
        Vector2 position
    )
    {
        int column =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    position.x * 3f
                ),
                0,
                2
            );

        int row =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    position.y * 3f
                ),
                0,
                2
            );

        string vertical =
            row == 2
                ? "TOP"
                : row == 1
                    ? "CENTER"
                    : "BOTTOM";

        string horizontal =
            column == 0
                ? "LEFT"
                : column == 1
                    ? "CENTER"
                    : "RIGHT";

        return vertical +
               "_" +
               horizontal;
    }

    // =========================================================
    // PLACE
    // =========================================================

    private static string Place(
        string zone
    )
    {
        if (
            string.IsNullOrEmpty(zone)
        )
        {
            return "the scene";
        }

        return zone
            .ToLowerInvariant()
            .Replace(
                "_",
                "-"
            );
    }

    // =========================================================
    // SINGULAR
    // =========================================================

    private static string Singular(
        string type
    )
    {
        switch (type)
        {
            case "light_trail":
                return "light trail";

            case "lantern":
                return "lantern";

            case "petals":
                return "petal group";

            case "fireworks":
                return "firework";

            case "water_current":
                return "water current";

            case "coral":
                return "coral";

            case "fish_school":
                return "fish school";

            case "bubble_burst":
                return "bubble burst";

            default:
                return "element";
        }
    }

    // =========================================================
    // PLURAL
    // =========================================================

    private static string Plural(
        string type
    )
    {
        switch (type)
        {
            case "light_trail":
                return "light trails";

            case "lantern":
                return "lanterns";

            case "petals":
                return "petal groups";

            case "fireworks":
                return "fireworks";

            case "water_current":
                return "water currents";

            case "coral":
                return "coral";

            case "fish_school":
                return "fish schools";

            case "bubble_burst":
                return "bubble bursts";

            default:
                return "elements";
        }
    }
}