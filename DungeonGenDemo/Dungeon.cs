using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGenDemo
{
    /// <summary>
    /// A single 'room' inside a DungeonGraph
    /// </summary>
    public class Node
    {
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public int PositionZ { get; private set; }
        public int Width { get; private set; }
        public int Length { get; private set; }

        public Node(int x, int y, int z, int w, int l)
        {
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            Width = w;
            Length = l;
        }
    }

    public enum CellEdgeType
    {
        Unused,
        Door,
        Wall,
    }

    public enum CellEdgeAlignment
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// Defines an edge between two cells and how they are connected.
    /// </summary>
    public class CellEdge
    {
        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public int PositionZ { get; private set; }
        public CellEdgeType ConnectionType { get; set; }
        public CellEdgeAlignment Alignment { get; private set; }

        public CellEdge(int x, int y, int z, CellEdgeAlignment a)
        {
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            ConnectionType = CellEdgeType.Unused;
            Alignment = a;
        }
    }
    
    /// <summary>
    /// A model of a dungeon of varying size and multiple floors
    /// </summary>
    public class DungeonGraph
    {
        /// <summary>
        /// Describes a node which may be used in generation of the dungeon.
        /// </summary>
        private struct NodeConfiguration
        {
            public int Width;
            public int Length;
            
            /// <summary>
            /// The probability of this NodeSize being used in node generation
            /// </summary>
            public int Probability;

            public NodeConfiguration(int w, int l, int prob)
            {
                Width = w;
                Length = l;
                Probability = prob;
            }
        }

        private enum Cardinal
        {
            North,
            South,
            East,
            West,
            Up,
            Down
        }

        /// <summary>
        /// Used to describe a cell which neighbors another cell.
        /// </summary>
        private struct NeighborCell
        {
            public int X;
            public int Y;
            public int Z;
            public CellEdge SharedEdge;

            public NeighborCell(int x, int y, int z, CellEdge edge)
            {
                X = x;
                Y = y;
                Z = z;
                SharedEdge = edge;
            }
        }

        /// <summary>
        /// The various Node configurations that can be placed in the dungeon
        /// </summary>
        private static readonly NodeConfiguration[] _nodeConfigs =
        {
            new NodeConfiguration(1, 1, 2),
            new NodeConfiguration(2, 2, 1),
            new NodeConfiguration(1, 2, 2),
            new NodeConfiguration(2, 1, 2),
            new NodeConfiguration(3, 1, 1),
            new NodeConfiguration(1, 3, 1),
            new NodeConfiguration(3, 2, 1),
            new NodeConfiguration(2, 3, 1)
        };

        public IEnumerable<Node> Nodes { get { return mNodes.AsEnumerable(); } }
        public IEnumerable<CellEdge> Edges { get { return mEdges.AsEnumerable(); } }
        public int Width { get; private set; }
        public int Length { get; private set; }
        public int Floors { get; private set; }

        private List<Node> mNodes;
        private List<CellEdge> mEdges;
        private CellEdge[,,] mEdgesX;
        private CellEdge[,,] mEdgesY;
        private CellEdge[,,] mEdgesZ;

        public DungeonGraph(int w, int l, int floors)
        {
            Width = w;
            Length = l;
            Floors = floors;
            mEdgesX = new CellEdge[w - 1, l, floors];
            mEdgesY = new CellEdge[w, l - 1, floors];
            mEdgesZ = new CellEdge[w, l, floors - 1];
        }

        /// <summary>
        /// Generates a seeded randomized dungeon
        /// </summary>
        /// <param name="seed"></param>
        public void Generate(int seed)
        {
            mNodes = new List<Node>(Width * Length * Floors);
            mEdges = new List<CellEdge>(Width * Length * Floors * 3 
                - Width - Length - Floors);
            InitializeEdges();

            Random rnd = new Random(seed);

            //Use this list for fetching walls along nodes and picking node walls at random
            List<CellEdge> nodeWalls = new List<CellEdge>();
            List<NeighborCell> neighborCells = new List<NeighborCell>();

            //Backtracking stack
            List<Node> nodeStack = new List<Node>();

            //Also, remember which 'cells' are occupied, and which node
            //Since a node can vary in size, it may occupy multiple cells
            Node[,,] nodeGrid = new Node[Width, Length, Floors];

            //Create start node
            Node startNode = new Node(rnd.Next(Width), rnd.Next(Length),
                rnd.Next(Floors), 1, 1);
            GetCellEdges(startNode, nodeWalls);
            SetEdges(nodeWalls, CellEdgeType.Unused, CellEdgeType.Wall);
            PlaceNodeOnGrid(nodeGrid, startNode);
            mNodes.Add(startNode);

            Node currentNode = startNode;
            bool mazeFilled = false;

            while (!mazeFilled)
            {
                neighborCells.Clear();
                nodeWalls.Clear();
                FindVacantNeighborCells(nodeGrid, currentNode, neighborCells);

                if (neighborCells.Count > 0)
                {
                    //Pick an empty space
                    NeighborCell target = neighborCells[rnd.Next(neighborCells.Count)];

                    //...create an opening to this new space
                    CellEdge openEdge = target.SharedEdge;
                    openEdge.ConnectionType = CellEdgeType.Door;

                    //...and create another node on the other side
                    Node newNode = null;
                    while (newNode == null)
                    {
                        NodeConfiguration size = PickRandomNodeConfiguration(rnd);
                        newNode = TryNodePlacement(nodeGrid, rnd, target.X,
                            target.Y, target.Z, size.Width, size.Length);
                    }

                    GetCellEdges(newNode, nodeWalls);
                    SetEdges(nodeWalls, CellEdgeType.Unused, CellEdgeType.Wall);

                    //Move into the new room, push old room onto stack
                    nodeStack.Add(currentNode);
                    currentNode = newNode;
                }
                else
                {
                    //Else, pop back to previous room, and find new walls
                    if (nodeStack.Count > 0)
                    {
                        currentNode = nodeStack[nodeStack.Count - 1];
                        nodeStack.RemoveAt(nodeStack.Count - 1);
                    }
                    else
                    {
                        //If there are no new places to go to, the maze is filled
                        mazeFilled = true;
                    }
                }

            }
            Console.WriteLine("Created " + mNodes.Count + " node dungeon.");
        }

        /// <summary>
        /// Retrieves all cell edges around a node
        /// </summary>
        /// <param name="node">Node to search around</param>
        /// <param name="results">All cell edges around a node</param>
        public void GetCellEdges(Node node, List<CellEdge> results)
        {
            //North edges
            if (node.PositionY > 0)
            {
                for (int i = node.PositionX; i < node.PositionX + node.Width; i++)
                    results.Add(mEdgesY[i, node.PositionY - 1, node.PositionZ]);
            }

            //South edges
            if (node.PositionY + node.Length - 1 < mEdgesY.GetLength(1))
            {
                for (int i = node.PositionX; i < node.PositionX + node.Width; i++)
                    results.Add(mEdgesY[i, node.PositionY - 1 + node.Length, node.PositionZ]);
            }

            //West edges
            if (node.PositionX > 0)
            {
                for (int i = node.PositionY; i < node.PositionY + node.Length; i++)
                    results.Add(mEdgesX[node.PositionX - 1, i, node.PositionZ]);
            }

            //East edges
            if (node.PositionX + node.Width - 1 < mEdgesX.GetLength(0))
            {
                for (int i = node.PositionY; i < node.PositionY + node.Length; i++)
                    results.Add(mEdgesX[node.PositionX - 1 + node.Width, i, node.PositionZ]);
            }

            //Down edges
            if (node.PositionZ > 0)
            {
                for (int ix = node.PositionX; ix < node.PositionX + node.Width; ix++)
                {
                    for (int iy = node.PositionY; iy < node.PositionY + node.Length; iy++)
                    {
                        results.Add(mEdgesZ[ix, iy, node.PositionZ - 1]);
                    }
                }
            }

            //Up edges
            if (node.PositionZ < mEdgesZ.GetLength(2))
            {
                for (int ix = node.PositionX; ix < node.PositionX + node.Width; ix++)
                {
                    for (int iy = node.PositionY; iy < node.PositionY + node.Length; iy++)
                    {
                        results.Add(mEdgesZ[ix, iy, node.PositionZ]);
                    }
                }
            }
        }

        /// <summary>
        /// Selects a random node configuration
        /// </summary>
        /// <param name="rnd">Randomizer being used in generation</param>
        /// <returns></returns>
        private NodeConfiguration PickRandomNodeConfiguration(Random rnd)
        {
            int roomRnd = 0;
            foreach (NodeConfiguration r in _nodeConfigs) roomRnd += r.Probability;

            int rndNum = rnd.Next(roomRnd);
            int i = 0;
            int adder = _nodeConfigs[0].Probability;

            while (i < _nodeConfigs.Length - 1 && rndNum >= adder)
                adder += _nodeConfigs[i++].Probability;

            return _nodeConfigs[i];
        }

        /// <summary>
        /// Attempts to randomly place a node to occupy a targeted cell
        /// without intersecting any other nodes.
        /// </summary>
        /// <param name="grid">DungeonGraph cell grid</param>
        /// <param name="rnd">Random number generator used in generation</param>
        /// <param name="searchDir"></param>
        /// <param name="x">Targeted X coordinate of cell to occupy</param>
        /// <param name="y">Targeted Y coordinate of cell to occupy</param>
        /// <param name="z">Targeted Z coordinate of cell to occupy</param>
        /// <param name="w">Width of node to place</param>
        /// <param name="l">Length of node to place</param>
        /// <returns>New Node on success, null on failure</returns>
        private Node TryNodePlacement(Node[,,] grid, Random rnd,
            int x, int y, int z, int w, int l)
        {
            // Given a grid where some cells are already occupied,
            // and the desired size of the node to be placed,
            // determine all locations where this node can be placed
            // which will occupy the targeted cell at (x,y,z)
            List<int> xCoords = new List<int>(4);
            List<int> yCoords = new List<int>(4);

            for (int ix = Math.Max(0, x - w + 1); ix <= Math.Min(Width - 1, x); ix++)
            {
                for (int iy = Math.Max(0, y - l + 1); iy <= Math.Min(Length - 1, y); iy++)
                {
                    if (IsAreaClear(grid, ix, iy, z, w, l))
                    {
                        //Add
                        xCoords.Add(ix);
                        yCoords.Add(iy);
                    }
                }
            }

            if (xCoords.Count == 0) return null;

            //Decide where to place node
            Node n = null;
            int idx = rnd.Next(xCoords.Count);
            n = new Node(xCoords[idx], yCoords[idx], z, w, l);
            PlaceNodeOnGrid(grid, n);
            mNodes.Add(n);
            return n;
        }

        /// <summary>
        /// Finds neighboring cells around a node which are not occupied by other nodes.
        /// </summary>
        /// <param name="grid">Cell grid</param>
        /// <param name="node">Node to search around</param>
        /// <param name="results">Vacant cells found</param>
        private void FindVacantNeighborCells(Node[,,] grid, Node node, List<NeighborCell> results)
        {
            //North edges
            if (node.PositionY > 0)
            {
                for (int i = node.PositionX; i < node.PositionX + node.Width; i++)
                {
                    CellEdge e = mEdgesY[i, node.PositionY - 1, node.PositionZ];
                    results.Add(new NeighborCell(i, node.PositionY - 1,
                        node.PositionZ, e));
                }
            }

            //South edges
            if (node.PositionY + node.Length - 1 < mEdgesY.GetLength(1))
            {
                for (int i = node.PositionX; i < node.PositionX + node.Width; i++)
                {
                    CellEdge e = mEdgesY[i, node.PositionY - 1 + node.Length, node.PositionZ];
                    results.Add(new NeighborCell(i, node.PositionY + node.Length,
                        node.PositionZ, e));
                }
            }

            //West edges
            if (node.PositionX > 0)
            {
                for (int i = node.PositionY; i < node.PositionY + node.Length; i++)
                {
                    CellEdge e = mEdgesX[node.PositionX - 1, i, node.PositionZ];
                    results.Add(new NeighborCell(node.PositionX - 1, i,
                        node.PositionZ, e));
                }
            }

            //East edges
            if (node.PositionX + node.Width - 1 < mEdgesX.GetLength(0))
            {
                for (int i = node.PositionY; i < node.PositionY + node.Length; i++)
                {
                    CellEdge e = mEdgesX[node.PositionX - 1 + node.Width, i, node.PositionZ];
                    results.Add(new NeighborCell(node.PositionX + node.Width, i,
                        node.PositionZ, e));
                }
            }

            //Down edges
            if (node.PositionZ > 0)
            {
                for (int ix = node.PositionX; ix < node.PositionX + node.Width; ix++)
                {
                    for (int iy = node.PositionY; iy < node.PositionY + node.Length; iy++)
                    {
                        CellEdge e = mEdgesZ[ix, iy, node.PositionZ - 1];
                        results.Add(new NeighborCell(ix, iy, node.PositionZ - 1, e));
                    }
                }
            }

            //Up edges
            if (node.PositionZ < mEdgesZ.GetLength(2))
            {
                for (int ix = node.PositionX; ix < node.PositionX + node.Width; ix++)
                {
                    for (int iy = node.PositionY; iy < node.PositionY + node.Length; iy++)
                    {
                        CellEdge e = mEdgesZ[ix, iy, node.PositionZ];
                        results.Add(new NeighborCell(ix, iy, node.PositionZ + 1, e));
                    }
                }
            }

            // Removed occupied cells
            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (grid[results[i].X, results[i].Y, results[i].Z] != null)
                {
                    results.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Place a node to occupy cells in a grid
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="node"></param>
        private void PlaceNodeOnGrid(Node[,,] grid, Node node)
        {
            for (int x = node.PositionX; x < node.PositionX + node.Width; x++)
            {
                for (int y = node.PositionY; y <node.PositionY + node.Length; y++)
                {
                    grid[x, y, node.PositionZ] = node;
                }
            }
        }

        /// <summary>
        /// Set edges of a certain type to another type.
        /// </summary>
        /// <param name="edges">Edge list to evaluate</param>
        /// <param name="oldType"></param>
        /// <param name="newType"></param>
        private void SetEdges(List<CellEdge> edges, CellEdgeType oldType, CellEdgeType newType)
        {
            foreach (CellEdge e in edges)
            {
                if (e.ConnectionType == oldType)
                {
                    e.ConnectionType = newType;
                }
            }
        }

        /// <summary>
        /// Checks if a certain area of the Cell grid is clear of any Nodes.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w">Width of area</param>
        /// <param name="l">Length of area</param>
        /// <returns></returns>
        private bool IsAreaClear(Node[,,] grid, int x, int y, int z, int w, int l)
        {
            if (Width - x < w) return false;
            if (Length - y < l) return false;

            for (int ix = x; ix < x + w; ix++)
            {
                for (int iy = y; iy < y + l; iy++)
                {
                    if (grid[ix, iy, z] != null)
                        return false;
                }
            }
            return true;
        }

        private void InitializeEdges()
        {
            //Initialize all edges
            for (int x = 0; x < mEdgesX.GetLength(0); x++)
            {
                for (int y = 0; y < mEdgesX.GetLength(1); y++)
                {
                    for (int z = 0; z < mEdgesX.GetLength(2); z++)
                    {
                        CellEdge e = new CellEdge(x, y, z, CellEdgeAlignment.X);
                        mEdges.Add(e);
                        mEdgesX[x, y, z] = e;
                    }
                }
            }

            for (int x = 0; x < mEdgesY.GetLength(0); x++)
            {
                for (int y = 0; y < mEdgesY.GetLength(1); y++)
                {
                    for (int z = 0; z < mEdgesY.GetLength(2); z++)
                    {
                        CellEdge e = new CellEdge(x, y, z, CellEdgeAlignment.Y);
                        mEdges.Add(e);
                        mEdgesY[x, y, z] = e;
                    }
                }
            }

            for (int x = 0; x < mEdgesZ.GetLength(0); x++)
            {
                for (int y = 0; y < mEdgesZ.GetLength(1); y++)
                {
                    for (int z = 0; z < mEdgesZ.GetLength(2); z++)
                    {
                        CellEdge e = new CellEdge(x, y, z, CellEdgeAlignment.Z);
                        mEdges.Add(e);
                        mEdgesZ[x, y, z] = e;
                    }
                }
            }
        }
    }
}
