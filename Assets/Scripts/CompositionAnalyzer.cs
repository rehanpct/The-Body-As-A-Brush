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
        if (Instance != null &&
            Instance != this)
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
            if (obj != null &&
                obj.activeInHierarchy)
            {
                validObjects.Add(obj);
            }
        }

        snapshot.totalElements =
            validObjects.Count;

        if (validObjects.Count == 0)
        {
            snapshot.zones = BuildZoneGrid(snapshot.elements);
            return snapshot;
        }

        // =====================================================
        // ARTWORK AREA
        // =====================================================

        float artworkWidth =
            Mathf.Abs(
                maxX - minX
            );

        float artworkHeight =
            Mathf.Abs(
                maxY - minY
            );

        float artworkArea =
            artworkWidth *
            artworkHeight;

        // =====================================================
        // DENSITY COUNTERS
        // =====================================================

        int leftCount = 0;

        int rightCount = 0;

        int topCount = 0;

        int bottomCount = 0;

        // =====================================================
        // LARGEST OBJECT
        // =====================================================

        float largestArea = 0f;

        string largestType = "";

        // =====================================================
        // ANALYZE EVERY OBJECT
        // =====================================================

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

            // -------------------------------------------------
            // NORMALIZED POSITION
            // -------------------------------------------------

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

            // -------------------------------------------------
            // NORMALIZED SIZE
            // -------------------------------------------------

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

            // -------------------------------------------------
            // OBJECT AREA
            // -------------------------------------------------

            float objectArea =
                size.x *
                size.y;

            float relativeArea =
                artworkArea > 0f
                    ? objectArea /
                      artworkArea
                    : 0f;

            // -------------------------------------------------
            // ELEMENT INFO
            // -------------------------------------------------

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

            snapshot.elements.Add(
                info
            );

            // -------------------------------------------------
            // LEFT / RIGHT
            // -------------------------------------------------

            if (normalizedX < 0.5f)
            {
                leftCount++;
            }
            else
            {
                rightCount++;
            }

            // -------------------------------------------------
            // TOP / BOTTOM
            // -------------------------------------------------

            if (normalizedY >= 0.5f)
            {
                topCount++;
            }
            else
            {
                bottomCount++;
            }

            // -------------------------------------------------
            // LARGEST OBJECT
            // -------------------------------------------------

            if (objectArea >
                largestArea)
            {
                largestArea =
                    objectArea;

                largestType =
                    info.type;
            }
        }

        // =====================================================
        // 3 x 3 SPATIAL GRID
        // =====================================================

        snapshot.zones =
            BuildZoneGrid(snapshot.elements);

        // =====================================================
        // DENSITY
        // =====================================================

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

        // =====================================================
        // LARGE OBJECT
        // =====================================================

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
            snapshot.hasLargeObject =
                true;

            snapshot.largeObjectType =
                largestType;
        }

        // =====================================================
        // EMPTY SPACE
        // =====================================================

        snapshot.emptySpace =
            CalculateEmptySpace(
                validObjects
            );

        // =====================================================
        // CLUSTERING
        // =====================================================

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

            occupied[
                column,
                row
            ]++;
        }

        // =====================================================
        // COUNT EMPTY CELLS
        // =====================================================

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
                if (
                    occupied[x, y] ==
                    0
                )
                {
                    emptyCells++;

                    // -----------------------------------------
                    // LEFT / RIGHT EMPTY
                    // -----------------------------------------

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

                    // -----------------------------------------
                    // TOP / BOTTOM EMPTY
                    // -----------------------------------------

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

                    // -----------------------------------------
                    // LARGEST EMPTY REGION
                    // -----------------------------------------

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
        }

        // =====================================================
        // EMPTY RATIOS
        // =====================================================

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
        {
            return;
        }

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
                if (
                    first ==
                    second
                )
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        first.transform.position,
                        second.transform.position
                    );

                totalDistance +=
                    distance;

                pairCount++;

                if (
                    distance <=
                    clusterDistance
                )
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

    public static string GetElementType(GameObject obj)
    {
        if (obj == null)
            return "unknown";

        string objectName = obj.name.ToLowerInvariant();

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
        if (objectName.Contains("lighttrail") ||
            objectName.Contains("goldenlight") ||
            objectName.Contains("trail"))
            return "light_trail";

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
        List<ZoneInfo> zones = new List<ZoneInfo>();
        Dictionary<string, int>[] typeCounts =
            new Dictionary<string, int>[gridSize * gridSize];
        string[] rows = { "TOP", "CENTER", "BOTTOM" };
        string[] columns = { "LEFT", "CENTER", "RIGHT" };

        for (int row = 0; row < gridSize; row++)
        {
            for (int column = 0; column < gridSize; column++)
            {
                int index = row * gridSize + column;
                zones.Add(new ZoneInfo
                {
                    name = rows[row] + "_" + columns[column],
                    dominantElementType = "",
                    isEmpty = true
                });
                typeCounts[index] = new Dictionary<string, int>();
            }
        }

        int total = elements == null ? 0 : elements.Count;
        if (elements != null)
        {
            foreach (ElementInfo element in elements)
            {
                if (element == null)
                    continue;

                int column = Mathf.Clamp(
                    Mathf.FloorToInt(element.normalizedPosition.x * gridSize),
                    0, gridSize - 1);
                int bottomRow = Mathf.Clamp(
                    Mathf.FloorToInt(element.normalizedPosition.y * gridSize),
                    0, gridSize - 1);
                int displayRow = gridSize - 1 - bottomRow;
                int index = displayRow * gridSize + column;
                zones[index].elementCount++;

                string type = string.IsNullOrEmpty(element.type)
                    ? "unknown"
                    : element.type;
                if (!typeCounts[index].ContainsKey(type))
                    typeCounts[index][type] = 0;
                typeCounts[index][type]++;
            }
        }

        int crowdedThreshold = Mathf.Max(2, Mathf.CeilToInt(total / 3f));
        for (int index = 0; index < zones.Count; index++)
        {
            ZoneInfo zone = zones[index];
            zone.density = total > 0
                ? (float)zone.elementCount / total
                : 0f;
            zone.isEmpty = zone.elementCount == 0;
            zone.isCrowded = zone.elementCount >= crowdedThreshold;

            int largest = 0;
            List<string> types = new List<string>(typeCounts[index].Keys);
            types.Sort(StringComparer.Ordinal);
            foreach (string type in types)
            {
                int count = typeCounts[index][type];
                if (count > largest)
                {
                    largest = count;
                    zone.dominantElementType = type;
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
    }
}
[Serializable]
public class CompositionAdvisorRecommendation
{
    public string action = "NONE";
    public string element = "";
    public string zone = "";
    public string reason = "";
    public string fallbackMessage = "";
}

public static class CompositionAdvisor
{
    public static bool IsElementAllowed(string theme, string type)
    {
        if (string.Equals(theme, "Taiwan", StringComparison.OrdinalIgnoreCase))
            return type == "light_trail" || type == "lantern" ||
                   type == "petals" || type == "fireworks";

        if (string.Equals(theme, "Underwater", StringComparison.OrdinalIgnoreCase))
            return type == "water_current" || type == "coral" ||
                   type == "fish_school" || type == "bubble_burst";

        return false;
    }

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
            foreach (CompositionAnalyzer.ElementInfo item in elements)
                if (item != null && IsElementAllowed(theme, item.type))
                    valid.Add(item);
        }

        if (valid.Count == 0)
        {
            result.reason = "Unity found no active elements allowed by the current theme.";
            result.fallbackMessage =
                "There are no active " + theme + " elements to review yet.";
            return result;
        }

        List<CompositionAnalyzer.ZoneInfo> zones =
            CompositionAnalyzer.BuildZoneGrid(valid);
        int sourceIndex = 0;
        for (int i = 1; i < zones.Count; i++)
            if (zones[i].elementCount > zones[sourceIndex].elementCount)
                sourceIndex = i;

        CompositionAnalyzer.ZoneInfo source = zones[sourceIndex];
        string element = source.dominantElementType;
        if (string.IsNullOrEmpty(element))
            element = MostCommonType(valid);

        result.element = element;
        result.zone = source.name;

        if (theme == "Underwater" &&
            ContainsType(valid, "coral") &&
            !ContainsType(valid, "bubble_burst") &&
            HasIsolatedCoral(valid))
        {
            CompositionAnalyzer.ElementInfo coral = FirstOfType(valid, "coral");
            string coralZone = ZoneFor(coral.normalizedPosition);
            int coralZoneIndex = -1;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].name == coralZone)
                {
                    coralZoneIndex = i;
                    break;
                }
            }

            if (coralZoneIndex >= 0 && zones[coralZoneIndex].isCrowded)
            {
                int openZoneIndex = TargetZone(zones, -1, -1, coralZoneIndex);
                if (openZoneIndex < 0)
                {
                    result.action = "NONE";
                    result.zone = "";
                    result.reason = "No non-crowded zone is available for a placement recommendation.";
                    result.fallbackMessage = "Your composition is full of energy. Keep some space open while you continue.";
                    return result;
                }

                coralZone = zones[openZoneIndex].name;
            }

            result.action = "ADD";
            result.element = "bubble_burst";
            result.zone = coralZone;
            result.reason =
                "A coral element is isolated in the " + Place(coralZone) +
                " with no nearby artwork element.";
            result.fallbackMessage =
                "Your coral is isolated in the " + Place(coralZone) +
                ". A small bubble burst nearby could connect it to the surrounding water.";
            return result;
        }

        int left = 0, right = 0, top = 0, bottom = 0;
        foreach (CompositionAnalyzer.ElementInfo item in valid)
        {
            if (item.normalizedPosition.x < (1f / 3f)) left++;
            else if (item.normalizedPosition.x >= (2f / 3f)) right++;

            if (item.normalizedPosition.y >= (2f / 3f)) top++;
            else if (item.normalizedPosition.y < (1f / 3f)) bottom++;
        }

        bool horizontal = Mathf.Abs(left - right) / (float)valid.Count >= 0.30f;
        bool vertical = Mathf.Abs(top - bottom) / (float)valid.Count >= 0.30f;
        int preferredColumn = horizontal ? (left < right ? 0 : 2) : -1;
        int preferredRow = vertical ? (top > bottom ? 2 : 0) : -1;
        int targetIndex = TargetZone(
            zones, preferredColumn, preferredRow, sourceIndex);
        if (targetIndex < 0)
        {
            result.action = "NONE";
            result.zone = "";
            result.reason = "No non-crowded zone is available for a placement recommendation.";
            result.fallbackMessage = "Your composition is full of energy. Keep some space open while you continue.";
            return result;
        }

        CompositionAnalyzer.ZoneInfo target = zones[targetIndex];

        if (source.isCrowded)
        {
            result.action = "SPREAD";
            result.zone = target.name;
            result.reason =
                "The " + Place(source.name) + " zone contains " +
                source.elementCount + " elements and meets Unity's crowded-zone threshold; " +
                "the " + Place(target.name) + " contains " +
                target.elementCount + ".";
            result.fallbackMessage =
                "Your " + Plural(element) + " are clustered in the " +
                Place(source.name) + ". Try placing the next " +
                Singular(element) + " near the " + Place(target.name) +
                " to spread the visual weight.";
            return result;
        }

        if (theme == "Taiwan" && ContainsType(valid, "fireworks"))
        {
            result.action = "LEAVE_OPEN";
            result.element = "fireworks";
            result.reason =
                "Fireworks are present in the " + Place(source.name) +
                " and provide a focal point.";
            result.fallbackMessage =
                "Your fireworks already create a focal point in the " +
                Place(source.name) +
                ". Leaving nearby space open will help them stand out.";
            return result;
        }

        if (horizontal || vertical)
        {
            result.action = "BALANCE";
            result.zone = target.name;
            result.reason = horizontal
                ? "Unity counted " + left + " elements on the left and " +
                  right + " on the right; the " + Place(target.name) +
                  " is the less populated target area."
                : "Unity counted " + top + " elements in the upper half and " +
                  bottom + " in the lower half; the " + Place(target.name) +
                  " is the less populated target area.";
            result.fallbackMessage =
                "Your " + Plural(element) + " carry more visual weight on the " +
                (horizontal ? (left > right ? "left" : "right")
                            : (top > bottom ? "top" : "bottom")) +
                ". Placing the next " + Singular(element) + " near the " +
                Place(target.name) + " would balance the scene.";
            return result;
        }

        if (valid.Count == 1)
        {
            result.action = "ADD";
            result.zone = target.name;
            result.reason =
                "The artwork has one active element in the " + Place(source.name) +
                ", while the " + Place(target.name) + " is empty.";
            result.fallbackMessage =
                "Your " + Singular(element) + " sits in the " +
                Place(source.name) + ". Adding another near the " +
                Place(target.name) + " would give the composition more reach.";
            return result;
        }

        float smallest = float.MaxValue, largest = 0f;
        foreach (CompositionAnalyzer.ElementInfo item in valid)
        {
            if (item.relativeSize <= 0f) continue;
            smallest = Mathf.Min(smallest, item.relativeSize);
            largest = Mathf.Max(largest, item.relativeSize);
        }

        if (smallest < float.MaxValue && largest > 0f &&
            (largest - smallest) / largest < 0.20f)
        {
            result.action = "VARY_SIZE";
            result.reason =
                "The measured elements span multiple grid zones and their relative sizes vary by less than 20 percent.";
            result.fallbackMessage =
                "Your elements are spread across the scene. Varying the size of your next " +
                Singular(element) +
                " could add rhythm without crowding another area.";
            return result;
        }

        result.action = "NONE";
        result.reason =
            "No 3 x 3 zone meets the crowded threshold and neither axis is strongly imbalanced.";
        result.fallbackMessage =
            "Your elements are distributed across the scene without a clearly crowded area. Keeping some open space will preserve breathing room.";
        return result;
    }

    private static int TargetZone(
        List<CompositionAnalyzer.ZoneInfo> zones,
        int preferredColumn,
        int preferredRow,
        int sourceIndex
    )
    {
        int best = -1, bestScore = int.MaxValue;
        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i].isCrowded)
                continue;

            int row = i / 3, column = i % 3;
            int score = zones[i].elementCount * 100;
            if (preferredColumn >= 0 && column != preferredColumn) score += 30;
            if (preferredRow >= 0 && row != preferredRow) score += 15;
            if (i == sourceIndex) score += 10;
            if (score < bestScore) { best = i; bestScore = score; }
        }
        return best;
    }

    private static bool HasIsolatedCoral(
        List<CompositionAnalyzer.ElementInfo> elements
    )
    {
        foreach (CompositionAnalyzer.ElementInfo coral in elements)
        {
            if (coral.type != "coral") continue;
            bool nearby = false;
            foreach (CompositionAnalyzer.ElementInfo other in elements)
            {
                if (coral != other &&
                    Vector2.Distance(coral.normalizedPosition,
                                     other.normalizedPosition) <= 0.15f)
                {
                    nearby = true;
                    break;
                }
            }
            if (!nearby) return true;
        }
        return false;
    }

    private static bool ContainsType(
        List<CompositionAnalyzer.ElementInfo> elements,
        string type
    )
    {
        foreach (CompositionAnalyzer.ElementInfo item in elements)
            if (item.type == type) return true;
        return false;
    }

    private static CompositionAnalyzer.ElementInfo FirstOfType(
        List<CompositionAnalyzer.ElementInfo> elements,
        string type
    )
    {
        foreach (CompositionAnalyzer.ElementInfo item in elements)
            if (item.type == type) return item;
        return elements[0];
    }

    private static string MostCommonType(
        List<CompositionAnalyzer.ElementInfo> elements
    )
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (CompositionAnalyzer.ElementInfo item in elements)
        {
            if (!counts.ContainsKey(item.type)) counts[item.type] = 0;
            counts[item.type]++;
        }

        string best = "";
        int maximum = 0;
        List<string> types = new List<string>(counts.Keys);
        types.Sort(StringComparer.Ordinal);
        foreach (string type in types)
            if (counts[type] > maximum) { best = type; maximum = counts[type]; }
        return best;
    }

    private static string ZoneFor(Vector2 position)
    {
        int column = Mathf.Clamp(Mathf.FloorToInt(position.x * 3f), 0, 2);
        int row = Mathf.Clamp(Mathf.FloorToInt(position.y * 3f), 0, 2);
        string vertical = row == 2 ? "TOP" : row == 1 ? "CENTER" : "BOTTOM";
        string horizontal = column == 0 ? "LEFT" : column == 1 ? "CENTER" : "RIGHT";
        return vertical + "_" + horizontal;
    }

    private static string Place(string zone)
    {
        return zone.ToLowerInvariant().Replace("_", "-");
    }

    private static string Singular(string type)
    {
        switch (type)
        {
            case "light_trail": return "light trail";
            case "lantern": return "lantern";
            case "petals": return "petal group";
            case "fireworks": return "firework";
            case "water_current": return "water current";
            case "coral": return "coral";
            case "fish_school": return "fish school";
            case "bubble_burst": return "bubble burst";
            default: return "element";
        }
    }

    private static string Plural(string type)
    {
        switch (type)
        {
            case "light_trail": return "light trails";
            case "lantern": return "lanterns";
            case "petals": return "petal groups";
            case "fireworks": return "fireworks";
            case "water_current": return "water currents";
            case "coral": return "coral";
            case "fish_school": return "fish schools";
            case "bubble_burst": return "bubble bursts";
            default: return "elements";
        }
    }
}
