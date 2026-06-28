// FILE: TreeGenerator.cs
// AUTHOR: Otakar Kočí <xkocio00@stud.fit.vut.cz>
// DATE: 2025 - 2026
// NOTE: This file is part of my Bachelor's thesis: Procedural generator of 3D voxel maps
// DESC: Contains TreeGenerator class and its dependencies, that allows generation of configurable trees and bushes
//          with the use of Space Colonization algorithm: http://dx.doi.org/10.2312/NPH/NPH07/063-070

using System;
using System.Collections.Generic;
using System.Numerics;

namespace PCGVoxelLandscapes.Scripts;

/**
 * Tree / bush classification conifer / deciduous
 */
public enum TreeType : byte
{
    DECIDUOUS,
    CONIFER
};

/**
 * Tree classification size
 */
public enum TreeSize : byte
{
    SMALL,
    MEDIUM,
    LARGE
};

/**
 * Tree classification strength
 */
public enum TreeState : byte
{
    HEALTHY,
    WEAK,
    DEAD
};

/**
 * Structure holding parameters for generation and classification of tree / bush
 */
public struct TreeDefinition(
    TreeType type,
    TreeSize size,
    TreeState state,
    ushort nAttractionPoints,
    ushort maxIterations,
    float influenceRadius,
    float killDistance,
    float branchLength,
    Vector3 growthBias,
    float crownOffset,
    EnvelopeType envelopeType,
    float envelopeRadius,
    float envelopeHeight,
    float kTop = 1f,
    float kBottom = 1f,
    float radialBias = 2.8f,
    float verticalBias = 2.1f,
    float interiorMin = 0.13f,
    float coniferWedgeSpacing = 14f,
    float coniferWedgeHeight = 2.5f,
    VoxelType trunk = VoxelType.WOOD,
    VoxelType leaves = VoxelType.DECIDUOUS_LEAVES,
    float maxLeafRadius = 0.4f,
    float leafRadiusScale = 3.4f,
    float maxLeafDispersion = 2.4f,
    float padding = 5f,
    ushort totalLeafPatchesForNode = 70,
    short offsetY = 0,
    float basicRadius = 0.38f,
    float radiusExponent = 3f,
    int id = 0,
    int seed = 5000)
{
    public int seed = seed;
    public int id = id;
    public readonly TreeType type = type;
    public readonly TreeSize size = size;
    public readonly TreeState state = state;
    public readonly ushort nAttractionPoints = nAttractionPoints;
    public readonly ushort maxIterations = maxIterations;
    public readonly float influenceRadius = influenceRadius;
    public readonly float killDistance = killDistance;
    public readonly float branchLength = branchLength;
    public readonly Vector3 growthBias = growthBias;
    public readonly float crownOffset = crownOffset;
    public readonly EnvelopeType envelopeType = envelopeType;
    public readonly float envelopeRadius = envelopeRadius;
    public readonly float envelopeHeight = envelopeHeight;
    public readonly float kTop = kTop;
    public readonly float kBottom = kBottom;
    public readonly float radialBias = radialBias;
    public readonly float verticalBias = verticalBias;
    public readonly float interiorMin = interiorMin;
    public readonly float coniferWedgeSpacing = coniferWedgeSpacing;
    public readonly float coniferWedgeHeight = coniferWedgeHeight;
    public readonly VoxelType trunk = trunk;
    public readonly VoxelType leaves = leaves;
    public float maxLeafRadius = maxLeafRadius;
    public float leafRadiusScale = leafRadiusScale;
    public readonly float maxLeafDispersion = maxLeafDispersion;
    public readonly float padding = padding;
    public readonly ushort totalLeafPatchesForNode = totalLeafPatchesForNode;
    public readonly short offsetY = offsetY;
    public readonly float basicRadius = basicRadius;
    public readonly float radiusExponent = radiusExponent;
};

/**
 * Node in the branching structure of space colonization algorithm 
 * Handles addition of its own influence points and direction calculation of next growth
 */
public class Node
{
    public Vector3 position { get; }
    private readonly List<Vector3> influencePoints; // attraction points influencing the node in current iteration
    public List<Node> children { get; }
    
    public Node(Vector3 position)
    {
        this.position = position;
        influencePoints = [];
        children = [];
    }
    
    public void AddInfluencePoint(Vector3 point)
    {
        influencePoints.Add(point);
    }
    
    public void AddChild(Node child)
    {
        children.Add(child);
    }
    
    /**
     * Calculates growth direction for a new node based on accumulated influence points
     */
    public Vector3 GetGrowthDirection(Vector3 growthBias)
    {
        Vector3 direction = Vector3.Zero;
        if (influencePoints.Count == 0)
            return Vector3.Zero;
        
        // calculate average direction towards influence points
        foreach (var point in influencePoints)
        {
           Vector3 dirToPoint = Vector3.Normalize(point - position);
           direction += dirToPoint;
        }

        // normalize and apply growth bias
        direction = Vector3.Normalize(direction / influencePoints.Count);
        direction = Vector3.Normalize(direction + growthBias);
        return direction;
    }
    
    public void ResetInfluencePoints()
    {
        influencePoints.Clear();
    }
}

/**
 * Branch for the resulting tree structure
 */
public class Branch
{
    public List<Vector3> branchPoints { get; }
    public  List<Branch> children { get; }
    public float radius { get; set; }
    
    public Branch(float radius)
    {
        branchPoints =  [];
        children = [];
        this.radius = radius;
    }
    
    public void AddChild(Branch child)
    {
        children.Add(child);
    }
    
    public void AddPoint(Vector3 point)
    {
        branchPoints.Add(point);
    }
}

// Types of envelopes supported by attraction points generators of ObjectBody
public enum EnvelopeType : byte
{
    SPHERE,
    OVAL,
    CONE,
    CYLINDER,
    PARABOLOID_CUP,
    PARABOLOID_CAP,
    PARABOLOID_LENS,
    // horizontal wedge for a single segment of complex conifer
    CONIFER_WEDGE
}

/**
 * Represents a body in space for generation of attraction points,
 * that are needed for the Space Colonization algorithm
 */
public class ObjectBody
{
    private readonly Random random;
    private readonly EnvelopeType envelopeType;
    private readonly float crownOffset;
    private readonly float envelopeRadius;
    private readonly float envelopeHeight;
    public List<Vector3> attractionPoints { get; }
    private readonly uint nAttractionPoints;
    private readonly float kTop; // line slope factor for the upper limit of horizontal conifer wedge
    private readonly float kBottom; // line slope factor for the lower limit of horizontal conifer wedge
    // biases 
    private readonly float radialBias; // higher value places more points towards the skin of the object body
    private readonly float verticalBias; // higher value places more points towards the top of the object body
    private readonly float interiorMin; // minimal probability ensuring that some points will be generated even in the center of the body
    

    public ObjectBody(Random random, EnvelopeType envelopeType, float crownOffset, float envelopeRadius, float envelopeHeight, uint nAttractionPoints, float kTop, float kBottom, float radialBias, float verticalBias, float interiorMin)
    {
        this.random = random;
        this.envelopeType = envelopeType;
        this.crownOffset = crownOffset;
        this.envelopeRadius = envelopeRadius;
        this.envelopeHeight = envelopeHeight;
        this.nAttractionPoints = nAttractionPoints;
        this.kTop = kTop;
        this.kBottom = kBottom;
        this.radialBias = radialBias;
        this.verticalBias = verticalBias;
        this.interiorMin = interiorMin;
        
        attractionPoints = new List<Vector3>();
    }

    /**
     * Calculates radius at vertical position of configured body type
     */
    private float RadiusAt(float yPosition)
    {
        switch (envelopeType)
        {
            case EnvelopeType.SPHERE:
                return float.Sqrt(float.Pow(envelopeRadius, 2) - float.Pow(yPosition - (crownOffset + envelopeRadius), 2));
            case EnvelopeType.OVAL:
                return envelopeRadius * float.Sqrt(1 - float.Pow( (yPosition - (crownOffset + envelopeHeight / 2)) / (envelopeHeight / 2), 2));
            case EnvelopeType.CONE:
                return envelopeRadius * (1 - ( yPosition - crownOffset ) / envelopeHeight);
            case EnvelopeType.CYLINDER:
                return envelopeRadius;
            case EnvelopeType.PARABOLOID_CUP:
                return envelopeRadius * float.Sqrt((yPosition - crownOffset) / envelopeHeight);
            case EnvelopeType.PARABOLOID_CAP:
                return envelopeRadius * float.Sqrt((crownOffset + envelopeHeight - yPosition) / envelopeHeight);
            case EnvelopeType.PARABOLOID_LENS:
                return envelopeRadius * (1 - float.Pow((yPosition - (crownOffset + envelopeHeight / 2)) / (envelopeHeight / 2), 2));
            default:
                return 0f; // should not reach here
        }
    }
    
    /**
     * Compute top boundary at radius for conifer wedge
     */
    private float YUpper(float radius)
    {
        return crownOffset + envelopeHeight - kTop * radius;
    }

    /**
     * Compute bottom boundary at radius for conifer wedge
     */
    private float YLower(float radius)
    {
        return crownOffset - kBottom * radius;
    }

    /**
     * Check if given point is in configured conifer wedge
     */
    private bool IsInEnvelopeWedge(Vector3 point)
    {
        float radius = float.Sqrt(point.X * point.X + point.Z * point.Z);

        // outside max radius
        if (radius > envelopeRadius)
            return false;

        // obtain vertical minima at this radius
        float yMin = YLower(radius);
        float yMax = YUpper(radius);

        // check placement within wedge
        return (point.Y >= yMin && point.Y <= yMax); 
    }
    
    /**
     * Returns envelope dimensions of configured body type and parameters
     */
    private (float maxX, float maxY, float maxZ, float minX, float minY, float minZ) GetEnvelopeDimensions()
    {
        float maxX = 0f, maxY = 0f, maxZ = 0f;
        float minX = 0f, minY = 0f, minZ = 0f;
        
        switch (envelopeType)
        {
            case EnvelopeType.SPHERE:
                maxX = envelopeRadius;
                maxY = envelopeRadius * 2 + crownOffset;
                maxZ = envelopeRadius;
                minX = -envelopeRadius;
                minY = crownOffset;
                minZ = -envelopeRadius;
                break;
            case EnvelopeType.OVAL:
            case EnvelopeType.CONE:
            case EnvelopeType.CYLINDER:
            case EnvelopeType.PARABOLOID_CUP:
            case EnvelopeType.PARABOLOID_CAP:
            case EnvelopeType.PARABOLOID_LENS:
                maxX = envelopeRadius;
                maxY = envelopeHeight + crownOffset;
                maxZ = envelopeRadius;
                minX = -envelopeRadius;
                minY = crownOffset;
                minZ = -envelopeRadius;
                break;
            case EnvelopeType.CONIFER_WEDGE:
                maxX = envelopeRadius;
                maxY = YUpper(0);
                maxZ = envelopeRadius;
                minX = -envelopeRadius;
                minY = YLower(envelopeRadius);
                minZ = -envelopeRadius;
                break;
        }
        
        return (maxX, maxY, maxZ, minX, minY, minZ);
    }
    
    /**
     * Generates a random point for use in non-wedge object bodies.
     * Based on configured radial, vertical and interior biases, it favors placement of points towards
     * the skin of the object body and high areas
     */
    private Vector3 SamplePoint()
    {
        while (true)
        {
            // generate y position, influenced by configured vertical bias
            // increasing vertical bias places more points toward the top of the object body
            float yRandom = random.NextSingle();
            float y = crownOffset + envelopeHeight * float.Pow(yRandom, 1.0f / verticalBias);
            
            // obtain radius at y position
            float maxR = RadiusAt(y);
            if (maxR <= 0) continue;

            // generate actual radius at current y position, influenced by radial bias
            float radiusRandom = random.NextSingle();
            float r = maxR * float.Pow(radiusRandom, 1.0f / radialBias);

            // calculate probability for obtained radius, radius closer to max radius at current y position
            // favors higher probability, interiorMin ensures that at least some points generate in the center
            float probability = interiorMin + (1f - interiorMin) * (r / maxR);
            
            if (random.NextSingle() > probability)
                continue;

            // confirmed placement, generate random angle around vertical axis
            // and calculate x, z based on obtained radius
            float theta = 2f * (float)Math.PI * random.NextSingle();
            float x = r * float.Cos(theta);
            float z = r * float.Sin(theta);

            return new Vector3(x, y, z);
        }
    }

    /**
     * Generates configured number of attraction points in conifer wedge body
     */
    public void GenerateAttractionPointsConiferWedge()
    {
        var (maxX, maxY, maxZ, minX, minY, minZ) = GetEnvelopeDimensions();
        
        while (attractionPoints.Count < nAttractionPoints)
        {
            // random x, y, z position
            float x = random.NextSingle() * (maxX - minX) + minX;
            float y = random.NextSingle() * (maxY - minY) + minY;
            float z = random.NextSingle() * (maxZ - minZ) + minZ;
            Vector3 point = new Vector3(x, y, z);
            if (IsInEnvelopeWedge(point)) // check placement
            {
                attractionPoints.Add(point);
            }
        }
    }
    
    /**
     * Generates configured number of attraction points of non-wedge object bodies
     */
    public void GenerateAttractionPoints()
    {
        while (attractionPoints.Count < nAttractionPoints)
        {
            Vector3 point = SamplePoint();
            attractionPoints.Add(point);
        }
    }
}

/**
 * Tree generator class encapsulating generation of trees and bushes with the Space Colonization algorithm.
 * Instances are single run only.
 */
public class TreeGenerator
{
    private readonly Random random;
    private List<Vector3> attractionPoints;
    private readonly List<Node> treeNodes;
    private Node rootNode;
    private readonly List<Branch> branches;
    private Branch rootBranch;
    private readonly ushort nAttractionPoints;
    private readonly ushort maxIterations;
    private readonly float influenceRadius;
    private readonly float killDistance;
    private readonly float branchLength;
    private readonly Vector3 growthBias;
    private readonly float crownOffset; // must be smaller than influenceRadius
    private readonly EnvelopeType envelopeType;
    private readonly float envelopeRadius;
    private readonly float envelopeHeight; // also center of envelope in Y axis for sphere
    private readonly float basicRadius; // base radius for computation of branch radius
    private readonly float radiusExponent; // exponent for calculation of branch radius
    private readonly float coniferWedgeSpacing; // vertical spacing between conifer wedges
    private readonly float coniferWedgeHeight; // height of conifer wedges
    private readonly float kTop; // line slope factor for the upper limit of horizontal conifer wedge
    private readonly float kBottom; // line slope factor for the lower limit of horizontal conifer wedge
    private readonly float radialBias; // higher value places more points towards the skin of the object body
    private readonly float verticalBias; // higher value places more points towards the top of the object body
    private readonly float interiorMin; // minimal probability ensuring that some points will be generated even in the center of the body
    public short offsetY { get; }
    
    private const float trunkCreationStep = 1f; // vertical distance between trunk attraction points for complex conifers with wedges
    private const float trunkRandomizationScale = 1f; // spread of attraction points around the vertical axis of trunk (complex conifers)
    
    public TreeGenerator(
        ushort nAttractionPoints,
        ushort maxIterations,
        float influenceRadius,
        float killDistance,
        float branchLength,
        Vector3 growthBias,
        float crownOffset,
        EnvelopeType envelopeType,
        float envelopeRadius,
        float envelopeHeight,
        int seed = 5000,
        float kTop = 1f,
        float kBottom = 1f,
        float radialBias = 2.8f,
        float verticalBias = 2.1f,
        float interiorMin = 0.13f,
        float coniferWedgeSpacing = 14f,
        float coniferWedgeHeight = 2.5f,
        short offsetY = 0,
        float basicRadius = 0.38f,
        float radiusExponent = 3f
        )
    {
        random = new Random(seed);
        
        this.nAttractionPoints = nAttractionPoints;
        this.maxIterations = maxIterations;
        this.influenceRadius = influenceRadius;
        this.killDistance = killDistance;
        this.branchLength = branchLength;
        this.growthBias = growthBias;
        this.crownOffset = crownOffset;
        this.envelopeType = envelopeType;
        this.envelopeRadius = envelopeRadius;
        this.envelopeHeight = envelopeHeight;
        this.coniferWedgeSpacing = coniferWedgeSpacing;
        this.coniferWedgeHeight = coniferWedgeHeight;
        this.kTop = kTop;
        this.kBottom = kBottom;
        this.radialBias = radialBias;
        this.verticalBias = verticalBias;
        this.interiorMin = interiorMin;
        this.offsetY = offsetY;
        this.basicRadius = basicRadius;
        this.radiusExponent = radiusExponent;

        attractionPoints = [];
        treeNodes = [];
        rootNode = null;
        branches = [];
        rootBranch = null;
    }

    /**
     * Randomly generates a set of attraction points from 0,0,0 up to the crown
     * Used for generation of trunk attraction points for complex conifers with wedges
     */
    private void GenerateAttractionPointsTrunkForConiferWedge()
    {
        Vector3 currentPosition = new Vector3(0,0,0);
        while (currentPosition.Y < crownOffset + envelopeHeight)
        {
            attractionPoints.Add(currentPosition);
            currentPosition.Y += trunkCreationStep;
            currentPosition.X += (random.NextSingle() - 0.5f) * trunkRandomizationScale;
            currentPosition.Z += (random.NextSingle() - 0.5f) * trunkRandomizationScale;
        }
    }

    /**
     * Generates attraction points for complex conifer. Consisting of trunk and set of horizontal vertically stacked wedges.
     * Firstly trunk attraction points are generated, and then each wedge is generated one by one.
     */
    private void GenerateAttractionPointsConiferComplexWedge()
    {
        // generate attraction points for the trunk
        GenerateAttractionPointsTrunkForConiferWedge();

        // calculate number of wedges
        uint wedgeCount = 0;
        float totalHeight = 0;
        float step = coniferWedgeHeight +  coniferWedgeSpacing;
        while (totalHeight + step <= envelopeHeight)
        {
            totalHeight += step;
            wedgeCount++;
        }
        
        // calculate radius reduction for each new cylinder
        float radiusReduction = envelopeRadius / wedgeCount;
        
        float currentCylinderRadius = envelopeRadius;
        float currentHeightOffset = crownOffset;
        
        // calculate total sum of volumes for correct distribution of attraction points
        // among wedges (simplified to cylinders)
        float volumeSum = 0f;
        float volumeCalcCurrentRadius = envelopeRadius;
        for (uint i = 0; i < wedgeCount; i++)
        {
            volumeSum += (float)(Math.PI) * volumeCalcCurrentRadius * volumeCalcCurrentRadius * coniferWedgeHeight;
            volumeCalcCurrentRadius -= radiusReduction;
        }
        
        float attractionPointsPerVolumeUnit = nAttractionPoints / volumeSum;
        
        // generate attraction points for each wedge
        for (uint i = 0; i < wedgeCount; i++)
        {
            // generate attraction points for this wedge
            uint actualNAttractionPoints = (uint)Math.Round(attractionPointsPerVolumeUnit * (Math.PI * currentCylinderRadius * currentCylinderRadius * coniferWedgeHeight));
            ObjectBody objectBody = new ObjectBody(random, EnvelopeType.CONIFER_WEDGE, currentHeightOffset, currentCylinderRadius, coniferWedgeHeight, actualNAttractionPoints, kTop, kBottom, radialBias, verticalBias, interiorMin);
            objectBody.GenerateAttractionPointsConiferWedge();
            attractionPoints.AddRange(objectBody.attractionPoints);

            currentHeightOffset += coniferWedgeSpacing;
            currentCylinderRadius -= radiusReduction;
        }
    }

    /**
     * Generates attraction points for non-conifer object types
     */
    private void GenerateAttractionPointsNormal()
    {
        ObjectBody objectBody = new ObjectBody(random, envelopeType, crownOffset, envelopeRadius, envelopeHeight, nAttractionPoints, kTop,  kBottom, radialBias, verticalBias, interiorMin);
        objectBody.GenerateAttractionPoints();
        attractionPoints = objectBody.attractionPoints;
    }
    
    /**
     * Generate a tree structure with Space Colonization algorithm
     * Encapsulates preparation of attraction points, generation of a tree structure, conversion to branches and branch radius calculation
     */
    public void Generate()
    {
        // generate attraction points
        if (envelopeType == EnvelopeType.CONIFER_WEDGE)
        {
            GenerateAttractionPointsConiferComplexWedge();
        }
        else
        {
            GenerateAttractionPointsNormal();
        }
        
        // create and add root node of the tree structure
        rootNode = new Node(Vector3.Zero);
        treeNodes.Add(rootNode);
        // tracking of used positions
        HashSet<Vector3> usedNodePositions = [rootNode.position];
        
        // run the algorithm
        uint iteration = 0;
        bool changeOccurred = true;
        // set of working active nodes, future iterations work only with these nodes and not all nodes present
        List<Node> activeNodes = [rootNode];
        
        while (attractionPoints.Count > 0 && iteration < maxIterations && changeOccurred)
        {
            changeOccurred = false;

            // obtain influence points for each node
            foreach (Vector3 point in attractionPoints)
            {
                Node closestNode = new Node(new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
                float minDist = float.PositiveInfinity;
                // find the closest node to this point
                foreach (Node node in activeNodes)
                {
                    float dist = Vector3.Distance(node.position, point);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestNode = node;
                    }
                }
                
                if (minDist <= influenceRadius)
                {
                    closestNode.AddInfluencePoint(point);
                }
            }
            
            // grow tree nodes
            List<Node> newNodes = new List<Node>(); // newly added nodes
            List<Node> nodesToRemove = new List<Node>(); // nodes that did not produce any new children, to remove from active
            foreach (Node node in activeNodes)
            {
                Vector3 growthDir = node.GetGrowthDirection(growthBias);
                if (growthDir == Vector3.Zero)
                {
                    // will not produce new nodes, remove from active
                    nodesToRemove.Add(node);
                    continue;
                }
                
                Vector3 newPosition = node.position + growthDir * branchLength;
                // ensure newPos is unique
                if (usedNodePositions.Contains(newPosition))
                    continue;
                usedNodePositions.Add(newPosition);
                
                Node childNode = new Node(newPosition);
                node.AddChild(childNode);
                newNodes.Add(childNode);
                changeOccurred = true;
                
                node.ResetInfluencePoints();
            }
            // add new nodes, remove inactive
            treeNodes.AddRange(newNodes);
            activeNodes.AddRange(newNodes);
            foreach (Node node in nodesToRemove)
            {
                activeNodes.Remove(node);
            }
            
            // remove attraction points that are too close to any node
            List<Vector3> pointsToRemove = new List<Vector3>();
            foreach (Vector3 point in attractionPoints)
            {
                foreach (Node node in treeNodes)
                {
                    if (Vector3.Distance(node.position, point) < killDistance)
                    {
                        // point has a node closer than kill distance
                        pointsToRemove.Add(point);
                        break;
                    }
                }
            }
            foreach (Vector3 point in pointsToRemove)
            {
                attractionPoints.Remove(point);
            }
            
            iteration++;
        }

        // traverse tree of nodes to create branches and compute radii
        GenerateBranches(rootNode, null, null);
        ComputeBranchRadius(rootBranch);
    }

    /**
     * Recursively generates resulting branching structure. Segments that do not divide
     * are collapsed into a single encapsulating branch
     */
    private void GenerateBranches(Node currentNode, Node prevNode, Branch parent)
    {
        Branch currentBranch = new Branch(basicRadius);
        if (prevNode != null)
        {
            currentBranch.AddPoint(prevNode.position);
        }
        if (parent != null)
        {
            parent.AddChild(currentBranch);
        }
        
        // process current node
        currentBranch.AddPoint(currentNode.position);
        while (true)
        {
            switch (currentNode.children.Count)
            {
                case 0: // no children, branch end here
                {
                    if (currentBranch.branchPoints.Count <= 0)
                    {
                        return;
                    }
                    
                    branches.Add(currentBranch);
                    if (rootBranch == null)
                    {
                        rootBranch = currentBranch;
                    }
                    return;
                }
                case 1: // single continuing child
                    currentNode = currentNode.children[0];
                    currentBranch.AddPoint(currentNode.position);
                    continue;
            }
            
            if (currentBranch.branchPoints.Count > 0)
            {
                branches.Add(currentBranch);
                if (rootBranch == null)
                {
                    rootBranch = currentBranch;
                }
            }
            
            // multiple children, branching
            foreach (Node child in currentNode.children)
            {
                GenerateBranches(child, currentNode, currentBranch);
            }

            return;
        }
    }

    /**
     * Recursively computes branch radii of current and deeper branches.
     *
     * Proceeds from tips to the root, calculating radius of each branch.
     * Formula taken from Space Colonization article:
     * Multiple branches with radii r1, r2, ..., rn meeting at a single point will mean their parent branch
     * will have radius calculated by r = (r1^radiusExponent + r2^radiusExponent + ... + rn^radiusExponent)^(1/radiusExponent)
     */
    private float ComputeBranchRadius(Branch branch)
    {
        float compSum = 0f;
        if (branch.children.Count == 0)
            return basicRadius;
        // traverse to children
        foreach (Branch child in branch.children)
        {
            float childRadius = ComputeBranchRadius(child);
            compSum += float.Pow(childRadius, radiusExponent);
        }
        // calculate own radius
        float ownRadius = float.Pow(compSum, 1f / radiusExponent);
        branch.radius = ownRadius;
        return ownRadius;
    }

    /**
     * Finishes generation of a voxel model of a tree by adding leaves.
     */
    public VoxelModel GetVoxelModel(VoxelType woodType, VoxelType leafType, float padding=5f, float maxLeafRadius=0.4f, float leafRadiusScale=3.4f, float maxLeafDispersion=2.4f, int totalLeafPatchesForNode=70)
    {
        // calculate size of the model based on envelope dimensions, add some padding
        float sizeX = 2 * (envelopeRadius + padding);
        float sizeZ = sizeX;
        float sizeY = envelopeHeight + crownOffset + 2 * padding;
        
        VoxelModel model = new VoxelModel((ushort)sizeX, (ushort)sizeY, (ushort)sizeZ);
        
        foreach (Branch branch in branches)
        {
            // fill voxel space with branches by bezier curve
            model.FillByBezierCurve(branch.branchPoints, (short)Math.Ceiling(branch.radius), woodType);
			
            if (branch.radius <= maxLeafRadius)
            {
                // generate randomly distributed leaves patches around branch points, fill by spheres
                foreach (Vector3 point in branch.branchPoints)
                {
                    for (int i = 0; i < totalLeafPatchesForNode; i++)
                    {
                        float x = random.NextSingle() * 2 * maxLeafDispersion - maxLeafDispersion + point.X;
                        float y = random.NextSingle() * 2 * maxLeafDispersion - maxLeafDispersion + point.Y;
                        float z = random.NextSingle() * 2 * maxLeafDispersion - maxLeafDispersion + point.Z;
                        Vector3 leafPatch = new Vector3(x, y, z);
                        float radius = maxLeafRadius * leafRadiusScale;
                        model.FillSphere(leafPatch, (short)radius, leafType);
                    }
                }
            }
        }
        
        return model;
    }
}
