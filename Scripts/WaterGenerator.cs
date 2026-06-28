// FILE: WaterGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains WaterGenerator class that generates systems of lakes and rivers including calculation
//          of their water availability effects in regions.

using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Represents lake of the water system.
 */
public class Lake
{
    public ushort waterHeight; // lake surface height
    public ushort bottomHeight; // lowest point in basin
    public readonly List<Vector2Int> surfaceCells = []; // lake surface cells
    public readonly List<Vector2Int> rimCells = []; // lake boundary cells
    
    public int area => surfaceCells.Count;
    public int depth => waterHeight - bottomHeight;
}

/**
 * Structure representing a river node at given position and with specified width.
 */
public struct RiverNode(Vector2Int position, float width)
{
    public Vector2Int position = position;
    public readonly float width = width;
}

/**
 * Represents river with a list of nodes defining its path.
 */
public class River()
{
    public readonly List<RiverNode> path = new();
}

/**
 * Represents spot in the simplified gradient field.
 */
public struct GradientSpot(float strength, Vector2 direction)
{
    public readonly float strength = strength;
    public Vector2 direction = direction;
}

/**
 * Encapsulates parameters of the WaterGenerator.
 */
public struct WaterGeneratorParams()
{
    public int prefilledLakeHeightThreshold { get; init; }
    public int unitsPerMeter { get; init; }
    public int regionSizeInUnits { get; init; }
    public int rimMetersPerRiverEntry { get; init; } = 10;
    public int maxLakeDepth { get; init; } = 20;
    public int maxLakeSize { get; init; } = 4000;
    public int maxRiverLength { get; init; } = 500;
    public int maxFlatSteps { get; init; } = 8;
    public int maxWaterInfluenceDistance { get; init; } = 48;
    public byte maxWaterInfluence { get; init; } = 128;
    public byte baseInfluence { get; init; } = 80;
}

/**
 * Generator of water network. Generates lakes and rivers. Calculates their influence of water availability in region.
 * Lake generation uses Priority Flood algorithm to extract depressions, where lakes are placed.
 * Rivers are generated backwards from lakes to sources based on precomputed values of gradient fields.
 */
public class WaterGenerator
{
    private readonly int prefilledLakeHeightThreshold;
    private readonly int maxLakeDepth;
    private readonly int maxLakeSize; // in surface area
    private readonly int unitsPerMeter;
    private readonly int rimMetersPerRiverEntry; // spacing around river entries to lakes
    private readonly int maxRiverLength; // maximum river length in meters
    private readonly int maxFlatSteps; // maximum steps on flat terrain before river ends
    private readonly int maxWaterInfluenceDistance; // maximum distance in meters for water influence on terrain
    private readonly byte maxWaterInfluence; // maximum water influence value for terrain modification
    private readonly byte baseInfluence; // base influence value for cells adjacent to lakes
    private readonly int maxWaterInfluenceDistanceSquared;
    // gradient field resolutions
    private readonly int gradientFieldResolution1;
    private readonly int gradientFieldResolution4;

    private readonly WorldRegion region;
    private readonly List<Lake> lakes;
    private readonly List<River> rivers;
    
    // uphill flow fields at different resolutions
    private readonly GradientSpot[,] gradientFieldRes1;  // 1 meter resolution
    private readonly GradientSpot[,] gradientFieldRes4;  // 4 meter resolution
    
    // epsilon
    private const float E = 0.0001f;
    
    public WaterGenerator(WorldRegion region, WaterGeneratorParams parameters)
    { 
        prefilledLakeHeightThreshold = parameters.prefilledLakeHeightThreshold;
        maxLakeDepth = parameters.maxLakeDepth;
        maxLakeSize = parameters.maxLakeSize;
        unitsPerMeter = parameters.unitsPerMeter;
        rimMetersPerRiverEntry = parameters.rimMetersPerRiverEntry;
        maxRiverLength = parameters.maxRiverLength;
        maxFlatSteps = parameters.maxFlatSteps;
        maxWaterInfluenceDistance = parameters.maxWaterInfluenceDistance;
        maxWaterInfluence = parameters.maxWaterInfluence;
        baseInfluence = parameters.baseInfluence;
        maxWaterInfluenceDistanceSquared = maxWaterInfluenceDistance * maxWaterInfluenceDistance;

        this.region = region;
        lakes = new List<Lake>();
        rivers = new List<River>();

        // initialize gradient fields
        // 1 meter resolution field
        gradientFieldResolution1 = parameters.regionSizeInUnits / unitsPerMeter;
        gradientFieldRes1 = new GradientSpot[gradientFieldResolution1, gradientFieldResolution1];
        // 4 meter resolution field
        gradientFieldResolution4 = gradientFieldResolution1 / 4;
        gradientFieldRes4 = new GradientSpot[gradientFieldResolution4, gradientFieldResolution4];
    }
    
    /**
     * Completely generates water network for the configured region.
     * Includes generation of lakes at depressions identified by Priority Flood algorithm and extraction of
     * explicit lake areas (where terrain height is lower than explicit lake threshold).
     * Generates rivers for all found lakes. Each river is generated backwards from lake to source with
     * use of precomputed gradient fields.
     * Calculates and applies water network influence on the region water availability.
     */
    public void GenerateWaterSystem()
    {
        // calculate gradient fields of the region heightmap
        CalculateGradientFields();
        
        // extract lakes from heightmap depressions
        ExtractLakesFromDepressions();
        
        // apply lakes to region
        ApplyLakesInRegion();
        
        // extract lakes that were created by explicit water placement
        ExtractPreFilledLakes();
        
        // generate rivers for all found lakes, each lake is generated backwards (lake to source)
        GenerateRiversForLakes();
        
        // apply rivers to region
        ApplyRiversToRegion();
        
        // calculate water availability influence on the region and its 8 neighbors from found lakes and rivers
        CalculateWaterAvailabilityInfluence();
    }
    
    /**
     * Priority Flood algorithm.
     * Fills depressions in heightmap.
     * Proceeds from the edges of the region inward.
     * Neighbors of currently processed cell are filled up to its height.
     * PAPER: https://arxiv.org/pdf/1511.04463
     */
    private ushort[,] ComputeFilledHeights()
    {
        PriorityQueue<Vector2Int, ushort> priorityQueue = new PriorityQueue<Vector2Int, ushort>();
        bool[,] visited = new bool[region.size, region.size];
        ushort[,] filled = new ushort[region.size, region.size];

        // init, push all boundary cells into priority queue, mark visited
        for (int x = 0; x < region.size; x++) // width
        {
            // add both edges
            EnqueueBoundaryCell(priorityQueue, visited, filled, x, 0);
            EnqueueBoundaryCell(priorityQueue, visited, filled, x, region.size - 1);
        }
        for (int z = 0; z < region.size; z++) // height
        {
            // add both edges
            EnqueueBoundaryCell(priorityQueue, visited, filled, 0, z);
            EnqueueBoundaryCell(priorityQueue, visited, filled, region.size - 1, z);
        }

        // process cells in order of increasing filled height
        while (priorityQueue.Count > 0)
        {
            // dequeue position
            priorityQueue.TryDequeue(out Vector2Int position, out ushort currentFilledHeight);
            // process its neighbors
            foreach (Vector2Int neighbor in FourNeighborhood(position, region.size, region.size))
            {
                if (visited[neighbor.x, neighbor.y])
                    continue;
                visited[neighbor.x, neighbor.y] = true;

                ushort neighborHeight = region.GetHeightAtLocalCoords((ushort)neighbor.x, (ushort)neighbor.y);
                // neighbor lower than current filled level will be filled up to it
                ushort neighborFilled = neighborHeight <= currentFilledHeight ? currentFilledHeight : neighborHeight;
                filled[neighbor.x, neighbor.y] = neighborFilled;
                priorityQueue.Enqueue(neighbor, neighborFilled);
            }
        }
        return filled;
    }

    /**
     * Enqueues specified cell if it was not yet visited. Sets its height.
     */
    private void EnqueueBoundaryCell(PriorityQueue<Vector2Int, ushort> priorityQueue, bool[,] visited, ushort[,] filled, int x, int z)
    {
        if (!visited[x, z])
        {
            visited[x, z] = true;
            ushort height = region.GetHeightAtLocalCoords((ushort)x, (ushort)z);
            filled[x, z] = height;
            priorityQueue.Enqueue(new Vector2Int(x, z), height);
        }
    }

    /**
     * Returns set of positions of 4 neighbors of the specified cell.
     */
    private IEnumerable<Vector2Int> FourNeighborhood(Vector2Int position, int width, int height)
    {
        // 4 neighborhood connectivity
        if (position.x + 1 < width) yield return new Vector2Int(position.x + 1, position.y);
        if (position.x - 1 >= 0) yield return new Vector2Int(position.x - 1, position.y);
        if (position.y + 1 < height) yield return new Vector2Int(position.x, position.y + 1);
        if (position.y - 1 >= 0) yield return new Vector2Int(position.x, position.y - 1);
    }
    
    /**
     * Extracts lakes from depressions in the terrain heightmap with Priority Flood algorithm.
     * Filled heightmap created by the Priority Flood algorithm is compared with original heightmap
     * and used to create mask of submerged cells from which are lakes extracted.
     */
    private void ExtractLakesFromDepressions()
    {
        // compute filled heightmap using priority flood algorithm
        ushort[,] filled = ComputeFilledHeights();

        // create mask of submerged cells
        bool[,] submerged = new bool[region.size, region.size];
        for (int x = 0; x < region.size; x++)
        {
            for (int y = 0; y < region.size; y++)
            {
                ushort orig = region.GetHeightAtLocalCoords((ushort)x, (ushort)y);
                // marked only when submerged and above prefilled threshold
                submerged[x, y] = filled[x, y] > orig && filled[x, y] > prefilledLakeHeightThreshold;
            }
        }

        // extract lakes formed by connected cells
        List<Lake> foundLakes = ExtractLakesFromMask(submerged);
        lakes.AddRange(foundLakes);
    }
    
    /**
     * Extracts lakes formed by connected submerged cells.
     */
    private List<Lake> ExtractLakesFromMask(bool[,] submerged)
    {
        bool[,] visited = new bool[region.size, region.size];
        List<Lake> foundLakes = [];
        // loop through region
        for (ushort sX = 0; sX < region.size; sX++)
        {
            for (ushort sY = 0; sY < region.size; sY++)
            {
                if (!submerged[sX, sY] || visited[sX, sY])
                    continue;

                // find connected components of submerged cells
                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                List<Vector2Int> component = [];
                stack.Push(new Vector2Int(sX, sY));
                visited[sX, sY] = true;

                ushort bottom = ushort.MaxValue;
                ushort minNeighborHeight = ushort.MaxValue;
                bool neighborMinOnBoundary = false;
                bool touchesRegionBoundary = false;

                // keep expanding component
                while (stack.Count > 0)
                {
                    // pop and process current cell
                    Vector2Int position = stack.Pop();
                    component.Add(position);
                    ushort heightAtPosition = region.GetHeightAtLocalCoords((ushort)position.x, (ushort)position.y);
                    bottom = Math.Min(bottom, heightAtPosition);

                    // reject if it touches region boundary
                    if (position.x == 0 || position.y == 0 || position.x == region.size - 1 || position.y == region.size - 1)
                        touchesRegionBoundary = true;

                    // check neighborhood, expand neighbors
                    foreach (Vector2Int neighbor in FourNeighborhood(position, region.size, region.size))
                    {
                        // submerged neighbor, part of component
                        if (submerged[neighbor.x, neighbor.y])
                        {
                            if (!visited[neighbor.x, neighbor.y])
                            {
                                visited[neighbor.x, neighbor.y] = true;
                                stack.Push(neighbor);
                            }
                        }
                        else
                        {
                            // neighbor outside component - candidate for spill point
                            ushort neighborHeight = region.GetHeightAtLocalCoords((ushort)neighbor.x, (ushort)neighbor.y);
                            bool neighborOnBoundary = (neighbor.x == 0 || neighbor.y == 0 || neighbor.x == region.size - 1 || neighbor.y == region.size - 1);

                            if (neighborHeight < minNeighborHeight)
                            {
                                minNeighborHeight = neighborHeight;
                                neighborMinOnBoundary = neighborOnBoundary;
                            }
                            else if (neighborHeight == minNeighborHeight)
                            {
                                neighborMinOnBoundary |= neighborOnBoundary;
                            }
                        }
                    }
                } // end of component flood fill

                // reject when touching region boundary
                if (touchesRegionBoundary)
                    continue;

                // nothing
                if (minNeighborHeight == ushort.MaxValue)
                    continue;

                // reject if spill point is on boundary
                if (neighborMinOnBoundary)
                    continue;

                ushort spillHeight = minNeighborHeight;

                // build lake
                Lake lake = new Lake
                {
                    waterHeight = spillHeight,
                    bottomHeight = bottom
                };

                // collect rim cells and add surface cells to lake
                HashSet<(int x, int z)> rimCells = [];
                foreach (Vector2Int position in component)
                {
                    lake.surfaceCells.Add(position);
                    
                    // check neighbors for rim candidates
                    foreach (Vector2Int neighbor in FourNeighborhood(position, region.size, region.size))
                    {
                        if (!submerged[neighbor.x, neighbor.y])
                        {
                            // neighbor outside component
                            ushort neighborHeight = region.GetHeightAtLocalCoords((ushort)neighbor.x, (ushort)neighbor.y);
                            if (neighborHeight >= spillHeight)
                            {
                                rimCells.Add((neighbor.x, neighbor.y));
                            }
                        }
                    }
                }

                // add unique rim cells to lake
                foreach (var cell in rimCells)
                {
                    lake.rimCells.Add(new Vector2Int(cell.x, cell.z));
                }
                
                int area = lake.area;
                int depth = lake.depth;

                // check constraints
                if (area == 0 || area > maxLakeSize || depth > maxLakeDepth || depth <= 0)
                    continue;

                foundLakes.Add(lake);
            }
        }

        return foundLakes;
    }
    
    /**
     * Extracts explicitly created prefilled lakes.
     * Creates mask of positions that are below explicit water thresholds
     * and then uses method ExtractPrefilledLakesFromMask to find lakes as
     * connected components.
     */
    private void ExtractPreFilledLakes()
    {
        // create mask of cells below prefilled threshold
        bool[,] belowThreshold = new bool[region.size, region.size];
        for (int x = 0; x < region.size; x++)
        {
            for (int y = 0; y < region.size; y++)
            {
                ushort cellHeight = region.GetHeightAtLocalCoords((ushort)x, (ushort)y);
                belowThreshold[x, y] = cellHeight < prefilledLakeHeightThreshold;
            }
        }

        // extract prefilled lakes as connected cells
        List<Lake> foundLakes = ExtractPrefilledLakesFromMask(belowThreshold);
        lakes.AddRange(foundLakes);
    }
    
    /**
     * Extracts actual lakes from the mask of cells below water threshold.
     * Finds connected cells that form the lakes and returns them.
     */
    private List<Lake> ExtractPrefilledLakesFromMask(bool[,] belowThreshold)
    {
        // track visited
        bool[,] visited = new bool[region.size, region.size];
        List<Lake> foundLakes = [];
        
        // loop through region
        for (int sX = 0; sX < region.size; sX++)
        {
            for (int sY = 0; sY < region.size; sY++)
            {
                if (!belowThreshold[sX, sY] || visited[sX, sY])
                    continue;

                // find connected cells below threshold
                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                List<Vector2Int> component = [];
                stack.Push(new Vector2Int(sX, sY));
                visited[sX, sY] = true;

                ushort bottom = ushort.MaxValue;
                
                // keep expanding cell area
                while (stack.Count > 0)
                {
                    // pop and process current cell
                    Vector2Int position = stack.Pop();
                    component.Add(position);
                    ushort heightAtPosition = region.GetHeightAtLocalCoords((ushort)position.x, (ushort)position.y);
                    bottom = Math.Min(bottom, heightAtPosition);

                    // expand to neighbors below threshold
                    foreach (Vector2Int neighbor in FourNeighborhood(position, region.size, region.size))
                    {
                        // another cell where the component continues
                        if (belowThreshold[neighbor.x, neighbor.y] && !visited[neighbor.x, neighbor.y])
                        {
                            visited[neighbor.x, neighbor.y] = true;
                            stack.Push(neighbor);
                        }
                    }
                }
                
                // water surface is at the prefilled threshold
                ushort waterSurface = (ushort)prefilledLakeHeightThreshold;

                // build lake
                Lake lake = new Lake
                {
                    waterHeight = waterSurface,
                    bottomHeight = bottom
                };

                // collect rim cells (cells at or just above threshold bordering the lake)
                HashSet<(int x, int z)> rimCells = [];
                foreach (Vector2Int position in component)
                {
                    lake.surfaceCells.Add(position);
                    
                    // check neighbors for rim candidates
                    foreach (Vector2Int neighbor in FourNeighborhood(position, region.size, region.size))
                    {
                        if (!belowThreshold[neighbor.x, neighbor.y])
                        {
                            // neighbor at or above threshold
                            ushort neighborHeight = region.GetHeightAtLocalCoords((ushort)neighbor.x, (ushort)neighbor.y);
                            if (neighborHeight >= waterSurface)
                            {
                                rimCells.Add((neighbor.x, neighbor.y));
                            }
                        }
                    }
                }

                // add unique rim cells to lake
                foreach (var rimCell in rimCells)
                {
                    lake.rimCells.Add(new Vector2Int(rimCell.x, rimCell.z));
                }

                // skip invalid lakes
                if (lake.area == 0 || lake.depth <= 0)
                    continue;

                foundLakes.Add(lake);
            }
        }

        return foundLakes;
    }
    
    /**
     * Applies generated lakes to the region water map.
     */
    private void ApplyLakesInRegion()
    {
        foreach (Lake lake in lakes)
        {
            // place lakes
            foreach (Vector2Int position in lake.surfaceCells)
            {
                ushort heightAtPoint = region.GetHeightAtLocalCoords((ushort)position.x, (ushort)position.y);
                int waterDepth = lake.waterHeight - heightAtPoint;
                region.SetWaterLevel((ushort)position.x, (ushort)position.y, (short)waterDepth);
            }
        }
    }
    
    /**
     * Generates rivers for all available lakes.
     * Rivers start generation from lake rim points and continue uphill.
     */
    private void GenerateRiversForLakes()
    {
        List<River> generatedRivers = [];

        foreach (Lake lake in lakes)
        {
            // plan number of rivers based on lake rim size
            int numRivers = Math.Max(1, lake.rimCells.Count / rimMetersPerRiverEntry);

            // select entry points for this lake
            List<Vector2Int> entryPoints = SelectRiverEntryPoints(lake, numRivers);
            
            // use them to generate rivers leading to this lake
            foreach (Vector2Int entryPoint in entryPoints)
            {
                // skip those on region boundary
                if (IsNearRegionBoundary(entryPoint, 2))
                    continue;
                
                // generate river
                River river = GenerateRiverFromEntryPoint(entryPoint);
                if (river != null && river.path.Count > 1)
                {
                    generatedRivers.Add(river);
                }
            }
        }
        
        rivers.AddRange(generatedRivers);
    }
    
    /**
     * Selects river entry points around the lake rim.
     * Evenly distributes the specified number of rivers.
     */
    private List<Vector2Int> SelectRiverEntryPoints(Lake lake, int riverCount)
    {
        List<Vector2Int> entryPoints = [];
        
        if (lake.rimCells.Count == 0)
            return entryPoints;
        
        if (riverCount >= lake.rimCells.Count)
        {
            // use all, should not happen
            entryPoints.AddRange(lake.rimCells);
        }
        else
        {
            // evenly distribute entry points around the rim
            int step = lake.rimCells.Count / riverCount;
            for (int i = 0; i < riverCount; i++)
            {
                int index = (i * step) % lake.rimCells.Count;
                entryPoints.Add(lake.rimCells[index]);
            }
        }
        
        return entryPoints;
    }
    
    /**
     * Returns true if specified point lies at region boundary within its vicinity
     * defined by specified buffer length.
     */
    private bool IsNearRegionBoundary(Vector2Int point, int buffer)
    {
        return point.x <= buffer || 
               point.y <= buffer || 
               point.x >= region.size - buffer - 1 || 
               point.y >= region.size - buffer - 1;
    }
    
    /**
     * Generates a single river from specified lake entry point.
     * Rivers are generated backwards from lakes uphill via the steepest path.
     * Along the path are generated control points.
     * River ends with a source when it gets too long, starts going downhill, loops, or stays at flat area for too long.
     * Control points are then used to generate actual path of the river for its river nodes
     * with evaluation of Bézier curves by De Casteljau algorithm.
     */
    private River GenerateRiverFromEntryPoint(Vector2Int entryPoint)
    {
        int boundaryBuffer = 3;
        int stepsPerControlPoint = 4;
        
        River river = new River();
        
        Vector2 currentPosition = new Vector2(entryPoint.x, entryPoint.y);
        ushort currentHeight = region.GetHeightAtLocalCoords((ushort)entryPoint.x, (ushort)entryPoint.y);
        
        // river control points for river path generation
        List<Vector2> controlPoints = [];
        controlPoints.Add(currentPosition);

        HashSet<(int, int)> visited = [];
        visited.Add((entryPoint.x, entryPoint.y));
        
        int flatStepCount = 0;
        int stepCount = 0;
        
        // generate control points, follow terrain uphill via the steepest path
        while (stepCount < maxRiverLength)
        {
            stepCount++;
            
            Vector2 flowDirection = GetGradientDirection(currentPosition);
            
            // move in the flow direction by 1 meter
            Vector2 newPosition = currentPosition + flowDirection * unitsPerMeter;
            
            int newX = (int)Math.Round(newPosition.X);
            int newY = (int)Math.Round(newPosition.Y);
            
            // check region boundary
            if (IsNearRegionBoundary(new Vector2Int(newX, newY), boundaryBuffer))
                break;
            
            // check already visited
            if (visited.Contains((newX, newY)))
                break;
            
            ushort nextHeight = region.GetHeightAtLocalCoords((ushort)newX, (ushort)newY);
            
            // check flat terrain
            if (nextHeight == currentHeight)
            {
                flatStepCount++;
                if (flatStepCount >= maxFlatSteps)
                    break;
            }
            else if (nextHeight < currentHeight)
            {
                // going downhill, terminate
                break;
            }
            else
            {
                flatStepCount = 0;
                currentHeight = nextHeight;
            }
            
            currentPosition = newPosition;
            visited.Add((newX, newY));
            // add every n-th position as control point
            if (stepCount % stepsPerControlPoint == 0)
                controlPoints.Add(currentPosition);
        }
        
        // add final position as control point if it differs from last one
        if (Vector2.Distance(controlPoints[controlPoints.Count - 1], currentPosition) > E)
        {
            controlPoints.Add(currentPosition);
        }
        
        // reject failed generation attempts
        if (controlPoints.Count < 2)
        {
            return river;
        }
        
        // generate smooth river path with Bézier curve
        // need to sample at every terrain unit, steps are for each meter
        int sampleCount = stepCount * 4;
        List<Vector2> curvePath = [];
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            Vector2 curvePoint = EvaluateBezierCurve(controlPoints, t);
            
            int px = (int)Math.Round(curvePoint.X);
            int py = (int)Math.Round(curvePoint.Y);
            
            // avoid duplicates
            if (curvePath.Count > 0)
            {
                Vector2 lastPoint = curvePath[curvePath.Count - 1];
                if (Vector2.Distance(lastPoint, curvePoint) <= E)
                    continue;
            }
            
            curvePath.Add(new Vector2(px, py));
        }
        
        // calculate widths and build river nodes
        AssembleRiverNodes(river, curvePath);
        return river;
    }
    
    /**
     * Evaluates Bézier curve on a path at given distance from start with De Casteljau algorithm.
     */
    private Vector2 EvaluateBezierCurve(List<Vector2> path, float distance) {
        if (path.Count == 1)
            return path[0]; 
        
        List<Vector2> tempPoints = new List<Vector2>(path);

        while (tempPoints.Count > 1)
        {
            List<Vector2> newPoints = [];
    
            for (int i = 0; i < tempPoints.Count - 1; i++)
            {
                Vector2 interpolated = Vector2.Lerp(tempPoints[i], tempPoints[i + 1], distance);
                newPoints.Add(interpolated);
            }
    
            tempPoints = newPoints;
        }

        return tempPoints[0];
    }
    
    /**
     * Calculates widths of river nodes from supplied river path and saves them to the river structure.
     */
    private void AssembleRiverNodes(River river, List<Vector2> riverPath)
    {
        if (riverPath.Count < 2)
            return;
        
        // starting from river source
        Vector2 startPoint = riverPath[riverPath.Count - 1];
        // looping from river source to lake entry
        for (int i = riverPath.Count - 1; i >= 0; i--)
        {
            // calculate distance to source, obtain width
            Vector2 currentPoint = riverPath[i];
            float distanceToSource = Vector2.Distance(currentPoint, startPoint);
            float width = RiverWidthFromSourceDistance(distanceToSource);
            // create and save river node
            RiverNode node = new RiverNode(new Vector2Int((int)Math.Round(currentPoint.X), (int)Math.Round(currentPoint.Y)), width);
            river.path.Add(node);
        }
    }
    
    /**
     * Returns river width for given distance to the river source.
     */
    private float RiverWidthFromSourceDistance(float distanceFromSource)
    {
        switch (distanceFromSource)
        {
            case < 25:
                return 1;
            case < 60:
                return 2;
            case < 125:
                return 3;
            case < 250:
                return 4;
            default:
                return 5;
        }
    }
    
    /**
     * Applies all generated rivers to this region.
     * Uses ApplyRiverNode method that sets correct water levels.
     */
    private void ApplyRiversToRegion()
    {
        foreach (River river in rivers)
        {
            if (river.path.Count < 2)
                return;
        
            // apply river nodes
            foreach (RiverNode node in river.path)
            {
                // apply water at this river node
                ApplyRiverNode(node);
            }
        }
    }
    
    /**
     * Applies river node to the world region.
     * Node is applied as a circle of water placed at node position.
     */
    private void ApplyRiverNode(RiverNode node)
    {
        int radius = (int)Math.Ceiling(node.width / 2.0f);
        
        // circle bounding box
        for (int dX = -radius; dX <= radius; dX++)
        {
            for (int dY = -radius; dY <= radius; dY++)
            {
                // actual position
                int x = node.position.x + dX;
                int y = node.position.y + dY;
                
                // check bounds
                if (x < 0 || x >= region.size || y < 0 || y >= region.size)
                    continue;
                
                // check circle
                float dist = (float)Math.Sqrt(dX * dX + dY * dY);
                if (dist > node.width / 2.0f)
                    continue;
                
                // do not overwrite existing water
                short currentWater = region.GetWaterLevelAtLocalCoords((ushort)x, (ushort)y);
                if (currentWater != 0)
                    continue;
                
                // carve deeper in center, shallower at edges
                // depth -3 center, -2 edges
                short riverCarveDepth = (short)-Math.Max(2, 3 - (int)dist);
                region.SetWaterLevel((ushort)x, (ushort)y, riverCarveDepth);
            }
        }
    }
    
    /**
     * Calculates and applies influence of water system (lakes, rivers) at water availability.
     * Calculation is done in extended region map, that covers spills to neighbor regions as well.
     * Resulting water effects are applied to this region and exported for use by neighbor regions.
     */
    private void CalculateWaterAvailabilityInfluence()
    {
        // extended influence map, includes space for effects that spill to neighbor regions
        int extendedMapSize = region.size + 2 * maxWaterInfluenceDistance;
        byte[,] extendedInfluenceMap = new byte[extendedMapSize, extendedMapSize];
        
        // offset for converting region coords to extended map coords
        int offset = maxWaterInfluenceDistance;
        
        // process lakes, add influence from rim cells
        foreach (Lake lake in lakes)
        {
            foreach (Vector2Int rimCell in lake.rimCells)
            {
                // skip rim cells near region boundaries of prefilled lakes
                if (lake.waterHeight == prefilledLakeHeightThreshold)
                {
                    // skip rim cells within 2 cells of region boundary
                    if (IsNearRegionBoundary(rimCell, 2))
                        continue;
                }
                
                AddWaterInfluenceToExtendedMap(
                    extendedInfluenceMap,
                    extendedMapSize,
                    rimCell.x + offset,
                    rimCell.y + offset,
                    baseInfluence // base influence used
                );
            }
        }
        
        // process rivers, add influence from river path
        foreach (River river in rivers)
        {
            foreach (RiverNode node in river.path)
            {
                // influence depends on river width
                int influence = Math.Min(baseInfluence, (int)(baseInfluence * 0.5f * node.width));
                AddWaterInfluenceToExtendedMap(
                    extendedInfluenceMap,
                    extendedMapSize,
                    node.position.x + offset,
                    node.position.y + offset,
                    influence
                );
            }
        }
        
        // apply influence to region and cap at maxWaterInfluence
        ApplyInfluenceToRegion(extendedInfluenceMap, offset);
        
        // export neighbor effects
        ExportNeighborWaterEffects(extendedInfluenceMap, offset);
    }
    
    /**
     * Applies circular influence of water availability around specified position to the influence map.
     * Uses circular falloff with maximum value being baseWaterInfluence.
     */
    private void AddWaterInfluenceToExtendedMap(byte[,] influenceMap, int mapSize, int centerX, int centerY, int baseWaterInfluence)
    {
        // obtain rectangle bounds of the circle
        int minX = Math.Max(0, centerX - maxWaterInfluenceDistance);
        int maxX = Math.Min(mapSize - 1, centerX + maxWaterInfluenceDistance);
        int minY = Math.Max(0, centerY - maxWaterInfluenceDistance);
        int maxY = Math.Min(mapSize - 1, centerY + maxWaterInfluenceDistance);
        // loop over rectangle points
        for (int x = minX; x <= maxX; x++)
        {
            int dX = x - centerX;
            int dXSquared = dX * dX;
            
            for (int y = minY; y <= maxY; y++)
            {
                int dY = y - centerY;
                int distanceSquared = dXSquared + dY * dY;
                
                // outside influence radius
                if (distanceSquared > maxWaterInfluenceDistanceSquared)
                    continue;
                
                // calculate actual distance
                int dist = (int)Math.Sqrt(distanceSquared);
                
                // linear falloff based on distance
                byte influence = (byte)(baseWaterInfluence * (maxWaterInfluenceDistance - dist) / maxWaterInfluenceDistance);
                
                if (influence > 0)
                {
                    // combine with existing value
                    influenceMap[x, y] = Math.Max(influenceMap[x, y], influence);
                }
            }
        }
    }
    
    /**
     * Applies water availability influences to processed region.
     */
    private void ApplyInfluenceToRegion(byte[,] extendedInfluenceMap, int offset)
    {
        // apply for all region points   
        for (ushort x = 0; x < region.size; x++)
        {
            for (ushort z = 0; z < region.size; z++)
            {
                byte influence = extendedInfluenceMap[x + offset, z + offset];
                
                if (influence > 0)
                {
                    // cap value and set
                    byte cappedInfluence = Math.Min(influence, maxWaterInfluence);
                    region.SetWaterAvailability(x, z, cappedInfluence);
                }
            }
        }
    }
    
    /**
     * Exports influence values of water availability that spilled outside this region
     * to the neighbor regions where these belong to.
     */
    private void ExportNeighborWaterEffects(byte[,] extendedInfluenceMap, int offset)
    {
        // collect effects for each of the 8 neighbor regions
        for (short nX = -1; nX <= 1; nX++)
        {
            for (short nZ = -1; nZ <= 1; nZ++)
            {
                // skip self
                if (nX == 0 && nZ == 0)
                    continue;

                List<WaterEffect> effects = [];
                
                // determine which area of the extended map corresponds to this neighbor
                int startX, endX, startZ, endZ;
                
                if (nX == -1)
                {
                    startX = 0;
                    endX = offset - 1;
                }
                else if (nX == 0)
                {
                    startX = offset;
                    endX = offset + region.size - 1;
                }
                else // nx == 1
                {
                    startX = offset + region.size;
                    endX = offset + region.size + maxWaterInfluenceDistance - 1;
                }
                
                if (nZ == -1)
                {
                    startZ = 0;
                    endZ = offset - 1;
                }
                else if (nZ == 0)
                {
                    startZ = offset;
                    endZ = offset + region.size - 1;
                }
                else // nz == 1
                {
                    startZ = offset + region.size;
                    endZ = offset + region.size + maxWaterInfluenceDistance - 1;
                }
                
                // collect non-zero influences in this neighbor region
                for (int x = startX; x <= endX; x++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        byte influence = extendedInfluenceMap[x, z];
                        
                        if (influence > 0)
                        {
                            // convert to neighbor local coordinates and save
                            ushort localX = (ushort)(x - offset - nX * region.size);
                            ushort localZ = (ushort)(z - offset - nZ * region.size);
                            effects.Add(new WaterEffect(localX, localZ, Math.Min(influence, (byte)255)));
                        }
                    }
                }
                
                // store effects for this neighbor if any exist
                if (effects.Count > 0)
                {
                    region.SetNeighborWaterEffects(nX, nZ, effects.ToArray());
                }
            }
        }
    }
    
    /**
     * Calculates gradient field from the heightmap data.
     * Processes the heightmap in 1 meter sized cells.
     */
    private void CalculateGradientFieldRes1()
    {
        // working at 1 meter scale
        for (ushort x = 0; x < gradientFieldResolution1; x++)
        {
            for (ushort z = 0; z < gradientFieldResolution1; z++)
            {
                // calculate region coordinates for current cell
                ushort regionX = (ushort)(x * unitsPerMeter);
                ushort regionZ = (ushort)(z * unitsPerMeter);
                
                // 4 terrain units per meter
                // cell of size 4 by 4

                // obtain average height of this cell center - 2 by 2 area
                ushort centerStartX = (ushort)(regionX + 1);
                ushort centerEndX = (ushort)(regionX + 2);
                ushort centerStartZ = (ushort)(regionZ + 1);
                ushort centerEndZ = (ushort)(regionZ + 2);
                
                // calculate average height of the center area
                float centerHeight = 0;
                for (ushort cX = centerStartX; cX <= centerEndX; cX++)
                {
                    for (ushort cZ = centerStartZ; cZ <= centerEndZ; cZ++)
                    {
                        centerHeight += region.GetHeightAtLocalCoords(cX, cZ);
                    }
                }
                // obtain the average
                centerHeight /= 4;
                
                // calculate precise center position in region terrain units
                float centerPosX = (centerStartX + centerEndX) / 2.0f;
                float centerPosZ = (centerStartZ + centerEndZ) / 2.0f;
                
                // calculate gradient vector
                Vector2 gradientVector = Vector2.Zero;

                // 4th position from start
                int endX = regionX + 4;
                int endZ = regionZ + 4;
                
                // iterate through all points in the cell and calculate gradient vector based on height difference to center
                for (ushort pX = regionX; pX < endX; pX++)
                {
                    for (ushort pZ = regionZ; pZ < endZ; pZ++)
                    {
                        float pointHeight = region.GetHeightAtLocalCoords(pX, pZ);
                        float heightDiff = pointHeight - centerHeight; // difference from the center point

                        // vector from center to point
                        Vector2 directionVector = new Vector2(pX - centerPosX, pZ - centerPosZ);

                        // weight by height difference and normalize
                        if (directionVector.LengthSquared() > E)
                        {
                            directionVector = Vector2.Normalize(directionVector);
                            gradientVector += directionVector * heightDiff;
                        }
                    }
                }
                
                // obtain strength and normalized direction
                float strength = gradientVector.Length();
                Vector2 direction = Vector2.Zero;
                if (strength > E)
                {
                    direction = Vector2.Normalize(gradientVector);
                }
                
                gradientFieldRes1[x, z] = new GradientSpot(strength, direction);
            }
        }
    }

    /**
     * Calculates low resolution large scale gradient field from high resolution gradient field.
     * Cell size is 4 meters. Calculates average gradient from spots of the high resolution field.
     */
    private void CalculateGradientFieldRes4()
    {
        // each output cell represents a 4 by 4 area of input cells
        for (int x = 0; x < gradientFieldResolution4; x++)
        {
            for (int z = 0; z < gradientFieldResolution4; z++)
            {
                int startX = x * 4;
                int startZ = z * 4;
                int endX = startX + 4;
                int endZ = startZ + 4;
                
                // accumulate gradient
                Vector2 accumulatedGradient = Vector2.Zero;
                
                for (int pX = startX; pX < endX; pX++)
                {
                    for (int pZ = startZ; pZ < endZ; pZ++)
                    {
                        GradientSpot spot = gradientFieldRes1[pX, pZ];
                        Vector2 gradientVector = spot.direction * spot.strength;
                        accumulatedGradient += gradientVector;
                    }
                }
                
                // obtain strength and normalized direction
                float strength = accumulatedGradient.Length();
                Vector2 direction = Vector2.Zero;
                if (strength > E)
                {
                    direction = Vector2.Normalize(accumulatedGradient);
                }
                
                gradientFieldRes4[x, z] = new GradientSpot(strength, direction);
            }
        }
    }

    /**
     * Calculates both 1 and 4 meter resolution gradient fields.
     */
    private void CalculateGradientFields()
    {
        // calculate base resolution gradient field from heightmap
        CalculateGradientFieldRes1();
        
        // calculate low resolution gradient field from high resolution one
        CalculateGradientFieldRes4();
    }

    /**
     * Returns gradient direction for given position. Firstly tries high resolution field.
     * If that one indicates flat area, tries lower resolution field. If that fails too, zero vector is returned.
     */
    private Vector2 GetGradientDirection(Vector2 position)
    {
        // try 1 meter resolution first
        Vector2 direction = GetGradientDirectionFromGradientField(position, gradientFieldRes1, 1);
        if (direction.LengthSquared() > E) // flat area
        {
            return direction;
        }
        
        // try 4 meter resolution
        direction = GetGradientDirectionFromGradientField(position, gradientFieldRes4, 4);
        if (direction.LengthSquared() > E) // flat area
        {
            return direction;
        }
        
        // flat area
        return Vector2.Zero;
    }

    /**
     * Returns gradient direction for given position.
     */
    private Vector2 GetGradientDirectionFromGradientField(Vector2 position, GradientSpot[,] gradientField, int resolutionInMeters)
    {
        // convert position to gradient field coordinates
        ushort gradientX = (ushort)(position.X / unitsPerMeter / resolutionInMeters);
        ushort gradientZ = (ushort)(position.Y / unitsPerMeter / resolutionInMeters);
        
        GradientSpot spot = gradientField[gradientX, gradientZ];
        
        // check if gradient has meaningful strength
        if (spot.strength < E)
        {
            return Vector2.Zero;
        }
        
        return spot.direction;
    }
}
