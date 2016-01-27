﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;


namespace Ballz.GameSession.World
{
    /// <summary>
    /// Represents the Terrain.
    /// </summary>
    public class Terrain
    {

        public class TerrainShape
        {
            /// <summary>
            /// Triangles for rendering.
            /// </summary>
            /// <remarks>
            /// Positions are in Pixel units. Use <see cref="Terrain.Scale"/> to transform into world coordinates.
            /// </remarks>
            public List<Triangle> Triangles = new List<Triangle>();

            /// <summary>
            /// Outline line segments for use in physics.
            /// </summary>
            /// <remarks>
            /// Positions are in Pixel units. Use <see cref="Terrain.Scale"/> to transform into world coordinates.
            /// </remarks>
            public List<List<Vector2>> Outlines = new List<List<Vector2>>();

            /// <summary>
            /// A bitmap of the terrain shape. True means earth, False means air.
            /// </summary>
            public bool[,] TerrainBitmap = null;
        }

        /// <summary>
        /// The revision number of the <see cref="PublicShape"/> triangles and outline.
        /// </summary>
        public int Revision { get; private set; } = 0;

        /// <summary>
        /// The revision number of the <see cref="PublicShape"/> terrainBitmap.
        /// </summary>
        private int WorkingRevision = 1;

        /// <summary>
        /// The world scale of the terrain, in meters per pixel.
        /// </summary>
        public float Scale = 0.08f;

        /// <summary>
        /// The most recent terrain shape. Will be updated by the background worker once <see cref="WorkingShape"/> is finished.
        /// </summary>
        /// <remarks>
        /// Changes can be made to the terrainBitmap of this shape. They will be pulled by the background worker, which will process
        /// the changes and write the resulting triangles and outline back to this shape when finished.
        /// Use a lock on the publicShape object when accessing it.
        /// </remarks>
        public TerrainShape PublicShape = new TerrainShape();

        /// <summary>
        /// A "work-in-progress" terrain shape that is used by the background worker that updates the shape.
        /// </summary>
        private TerrainShape WorkingShape = null;

        // Internal terrain bitmaps
		private Texture2D terrainData = null;
        private float[,] terrainSmoothmap = null;
        private float[,] terrainSmoothmapCopy = null;

        // Terrain size
        private int width = -1;
        private int height = -1;

        // Internal members to avoid reallocation
        private Border[,] bordersH = null;
        private Border[,] bordersV = null;
        private List<List<IntVector2>> fullCells = null;
        private List<Edge> allEdges = null;


        
		public Terrain (Texture2D terrainTexture)
		{
			terrainData = terrainTexture;

			width = terrainData.Width;
			height = terrainData.Height;

            PublicShape.TerrainBitmap = new bool[width, height];
            terrainSmoothmap = new float[width, height];
            terrainSmoothmapCopy = new float[width, height];

            Color[] pixels = new Color[width * height];
			terrainData.GetData<Color> (pixels);


            for (int y = 0; y < height; ++y) {
                for (int x = 0; x < width; ++x) {

                    Color curPixel = pixels [y * width + x];

					// Note that we flip the y coord here
                    if (curPixel == Color.White)
                    {
                        PublicShape.TerrainBitmap[x, height - y - 1] = true;
                        terrainSmoothmap[x, height - y - 1] = 1.0f;
                    }
				}
			}

            // Pre-allocate internal members
            bordersH = new Border[width, height];
            bordersV = new Border[width, height];
            fullCells = new List<List<IntVector2>>(height);
            allEdges = new List<Edge>();

            update();
		}
            

		private float distance(float x1, float y1, float x2, float y2)
		{
			return (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
		}

		public void SubtractCircle(float x, float y, float radius)
		{
            x /= Scale;
            y /= Scale;
            radius /= Scale;

            // Compute bounding box
            int tlx = (int)Math.Floor(x - radius);
			int tly = (int)Math.Floor(y - radius);
			int brx = (int)Math.Ceiling(x + radius);
			int bry = (int)Math.Ceiling(y + radius);

			// Iterate over bounding box part of bitmap
			for (int j = Math.Max(0, tly); j < Math.Min(height, bry); ++j) {
				for (int i = Math.Max(0, tlx); i < Math.Min(width, brx); ++i) {

					if (distance(i, j, x, y) > radius)
						continue;

					// Subtract dirt (if any)
                    PublicShape.TerrainBitmap [i, j] = false;

				}
			}

            WorkingRevision++;

        }

        public void AddCircle(float x, float y, float radius)
        {
            x /= Scale;
            y /= Scale;
            radius /= Scale;

            // Compute bounding box
            int tlx = (int)Math.Floor(x - radius);
            int tly = (int)Math.Floor(y - radius);
            int brx = (int)Math.Ceiling(x + radius);
            int bry = (int)Math.Ceiling(y + radius);


            // Iterate over bounding box part of bitmap
            for (int j = Math.Max(0, tly); j < Math.Min(height, bry); ++j) {
                for (int i = Math.Max(0, tlx); i < Math.Min(width, brx); ++i) {

                    if (distance(i, j, x, y) > radius)
                        continue;

                    // Add dirt
                    PublicShape.TerrainBitmap [i, j] = true;

                }
            }

            WorkingRevision++;

        }

       


        public class Border
        {
            public Edge e0 = null, e1 = null;

            public Border(Edge edge0)
            {
                if(edge0 == null)
                    throw new InvalidOperationException("Trying to add null-edge to border!");
                
                e0 = edge0;
            }

            public bool valid()
            {
                return e0 != null || e1 != null;
            }
        }

        public class Edge
        {
            public Border b0, b1;

            public int ax, ay;
            public int bx, by;

            public Vector2 a;
            public Vector2 b;

            public Edge(int ax, int ay, int bx, int by)
            {
                this.ax = ax; this.ay = ay;
                this.bx = bx; this.by = by;

                a = 0.5f * new Vector2(ax, ay);
                b = 0.5f * new Vector2(bx, by);
            }

            public Edge(int ax, int ay, int bx, int by, Vector2 a, Vector2 b)
            {
                this.a = a; this.b = b;
                this.ax = ax; this.ay = ay;
                this.bx = bx; this.by = by;
            }
        }
            

        private void extractOutline(Edge edge)
        {
            var o1 = extractOutlineInternal(edge);
            if (o1 == null)
                return; // no outline at all

            // check non-closed case
            if (edge.b0 != null || edge.b1 != null)
            {
                var o2 = extractOutlineInternal(edge);
                o1.Reverse();
                o1.AddRange(o2);
            }

            WorkingShape.Outlines.Add(o1);
        }
        private List<Vector2> extractOutlineInternal(Edge edge)
        {
            if (edge == null)
                return null;

            var firstEdge = edge;
            
            var result = new List<Vector2>(100);

            while(edge != null)
            {
                // follow first border
                Border b = edge.b0 ?? edge.b1;
                if (b == null)
                    break;

                // null border in edge (and add result)
                if (edge.b0 == b)
                {
                    edge.b0 = null;
                    result.Add(edge.a);
                }
                else if (edge.b1 == b)
                {
                    edge.b1 = null;
                    result.Add(edge.b);
                }
                else
                    throw new InvalidOperationException("lolnope1");

                // null edge in border
                if(b.e0 == edge)
                {
                    b.e0 = null;
                }
                else if(b.e1 == edge)
                {
                    b.e1 = null;
                }
                else 
                    throw new InvalidOperationException("lolnope2");

                // follow next edge
                edge = b.e0 ?? b.e1;
                if (edge == null)
                    break;

                // null border in edge
                if (edge.b0 == b)
                {
                    edge.b0 = null;
                    if (edge == firstEdge) // closed loop
                        result.Add(edge.b);
                }
                else if (edge.b1 == b)
                {
                    edge.b1 = null;
                    if (edge == firstEdge) // closed loop
                        result.Add(edge.a);
                }
                else
                    throw new InvalidOperationException("lolnope3");

                // null edge in border
                if(b.e0 == edge)
                {
                    b.e0 = null;
                }
                else if(b.e1 == edge)
                {
                    b.e1 = null;
                }
                else 
                    throw new InvalidOperationException("lolnope4");
            }

            return result;
        }

        public Vector2 Mix(Vector2 a, Vector2 b, float wa, float wb)
        {
            float weight = (0.5f - wa) / (wb - wa);

            return a + weight * (b - a);
        }

        public struct Triangle
        {
            public Vector2 a,b,c;
            public Triangle(Vector2 a, Vector2 b, Vector2 c)
            {
                this.a = a; this.b = b; this.c = c;
            }
        }


        private void smoothTerrain(float[] gauss)
        {
            
            int halfGauss = gauss.GetLength(0) / 2;

            // Iterate over all bitmap pixels
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float sum = 0.0f;
                    float weight = 0.0f;
                    for(int i = -halfGauss; i <= halfGauss; ++i)
                    {
                        int index = x + i;
                        if(index >= 0 && index < width)
                        {
                            float val = gauss[i + halfGauss];

                            sum    += (WorkingShape.TerrainBitmap[index, y] ? 1.0f : 0.0f) * val;
                            weight += val;
                        }
                    }

                    float newVal = sum / weight;
                    terrainSmoothmap[x, y]     = newVal;
                    terrainSmoothmapCopy[x, y] = newVal;

                }
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float sum = 0.0f;
                    float weight = 0.0f;
                    for(int i = -halfGauss; i <= halfGauss; ++i)
                    {
                        int index = y + i;
                        if(index >= 0 && index < height)
                        {
                            float val = gauss[i + halfGauss];

                            sum    += terrainSmoothmapCopy[x, index] * val;
                            weight += val;
                        }
                    }

                    terrainSmoothmap[x, y] = sum / weight;
                }
            }
        }

        private void performMarchingSquares()
        {
            // Clear internal members
            WorkingShape.Triangles.Clear();
            allEdges.Clear();
            fullCells.Clear();

            // Iterate over all bitmap pixels
            for (int y = 1; y < height; ++y)
            {
                fullCells.Add(new List<IntVector2>(width));
                for (int x = 1; x < width; ++x)
                {

                    /*
                     *      0 -- 3
                     *      |    |
                     *      1 -- 2
                     * 
                     */

                    // TODO: upper-left row/column

                    float w0 = terrainSmoothmap[x - 1, y - 1];
                    float w1 = terrainSmoothmap[x-1, y];
                    float w2 = terrainSmoothmap[x, y];
                    float w3 = terrainSmoothmap[x, y-1];

                    bool _0 = w0 > 0.5f;
                    bool _1 = w1 > 0.5f;
                    bool _2 = w2 > 0.5f;
                    bool _3 = w3 > 0.5f;

                    int mscase = (_0?1:0) + (_1?2:0) + (_2?4:0) + (_3?8:0);

                    // Early-out?
                    if (mscase == 0)
                        continue;

                    // Early-in?
                    if (mscase == 15)
                    {
                        fullCells[fullCells.Count - 1].Add(new IntVector2(x, y));
                    }
                    else
                    {
                        Vector2 v0 = new Vector2(x-1, y-1);
                        Vector2 v1 = new Vector2(x-1, y);
                        Vector2 v2 = new Vector2(x, y);
                        Vector2 v3 = new Vector2(x, y-1);



                        switch(mscase)
                        {
                            case 0:
                                // No triangles at all
                                break;
                            case 1:
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v0, v1, w0, w1), Mix(v0, v3, w0, w3)));
                                //allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*(x-1), 2*y-1));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*(x-1), 2*y-1, Mix(v0, v1, w0, w1), Mix(v0, v3, w0, w3)));
                                break;
                            case 2:
                                WorkingShape.Triangles.Add(new Triangle(v1, Mix(v1, v2, w1, w2), Mix(v0, v1, w0, w1)));
                                allEdges.Add(new Edge(2*(x-1), 2*y-1, 2*x-1, 2*y, Mix(v1, v2, w1, w2), Mix(v0, v1, w0, w1)));
                                break;
                            case 3:
                                WorkingShape.Triangles.Add(new Triangle(v0, v1, Mix(v1, v2, w1, w2)));
                                WorkingShape.Triangles.Add(new Triangle(Mix(v1, v2, w1, w2), Mix(v0, v3, w0, w3), v0));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*x-1, 2*y, Mix(v1, v2, w1, w2), Mix(v0, v3, w0, w3)));
                                break;
                            case 4:
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v2, v3, w2, w3), Mix(v1, v2, w1, w2)));
                                allEdges.Add(new Edge(2*x-1, 2*y, 2*x, 2*y-1, Mix(v2, v3, w2, w3), Mix(v1, v2, w1, w2)));
                                break;
                            case 5:
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v0, v1, w0, w1), Mix(v0, v3, w0, w3)));
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v2, v3, w2, w3), Mix(v1, v2, w1, w2)));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*(x-1), 2*y-1, Mix(v0, v1, w0, w1), Mix(v0, v3, w0, w3)));
                                allEdges.Add(new Edge(2*x-1, 2*y, 2*x, 2*y-1, Mix(v2, v3, w2, w3), Mix(v1, v2, w1, w2)));
                                break;
                            case 6:
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v0, v1, w0, w1), v1));
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v2, v3, w2, w3), Mix(v0, v1, w0, w1)));
                                allEdges.Add(new Edge(2*(x-1), 2*y-1, 2*x, 2*y-1, Mix(v2, v3, w2, w3), Mix(v0, v1, w0, w1)));
                                break;
                            case 7:
                                WorkingShape.Triangles.Add(new Triangle(v1, Mix(v0, v3, w0, w3), v0));
                                WorkingShape.Triangles.Add(new Triangle(v1, Mix(v2, v3, w2, w3), Mix(v0, v3, w0, w3)));
                                WorkingShape.Triangles.Add(new Triangle(v1, v2, Mix(v2, v3, w2, w3)));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*x, 2*y-1, Mix(v2, v3, w2, w3), Mix(v0, v3, w0, w3)));
                                break;
                            case 8:
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v0, v3, w0, w3), Mix(v2, v3, w2, w3)));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*x, 2*y-1, Mix(v0, v3, w0, w3), Mix(v2, v3, w2, w3)));
                                break;
                            case 9:
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v2, v3, w2, w3), v3));
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v0, v1, w0, w1), Mix(v2, v3, w2, w3)));
                                allEdges.Add(new Edge(2*(x-1), 2*y-1, 2*x, 2*y-1, Mix(v0, v1, w0, w1), Mix(v2, v3, w2, w3)));
                                break;
                            case 10: 
                                WorkingShape.Triangles.Add(new Triangle(v1, Mix(v1, v2, w1, w2), Mix(v0, v1, w0, w1)));
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v0, v3, w0, w3), Mix(v2, v3, w2, w3)));
                                allEdges.Add(new Edge(2*(x-1), 2*y-1, 2*x-1, 2*y, Mix(v1, v2, w1, w2), Mix(v0, v1, w0, w1)));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*x, 2*y-1, Mix(v0, v3, w0, w3), Mix(v2, v3, w2, w3)));
                                break;
                            case 11: 
                                WorkingShape.Triangles.Add(new Triangle(v0, v1, Mix(v1, v2, w1, w2)));
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v1, v2, w1, w2), Mix(v2, v3, w2, w3)));
                                WorkingShape.Triangles.Add(new Triangle(v0, Mix(v2, v3, w2, w3), v3));
                                allEdges.Add(new Edge(2*x-1, 2*y, 2*x, 2*y-1, Mix(v1, v2, w1, w2), Mix(v2, v3, w2, w3)));
                                break;
                            case 12: 
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v0, v3, w0, w3), Mix(v1, v2, w1, w2)));
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v1, v2, w1, w2), v2));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*x-1, 2*y, Mix(v0, v3, w0, w3), Mix(v1, v2, w1, w2)));
                                break;
                            case 13: 
                                WorkingShape.Triangles.Add(new Triangle(v3, v0, Mix(v0, v1, w0, w1)));
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v0, v1, w0, w1), Mix(v1, v2, w1, w2)));
                                WorkingShape.Triangles.Add(new Triangle(v3, Mix(v1, v2, w1, w2), v2));
                                allEdges.Add(new Edge(2*(x-1), 2*y-1, 2*x-1, 2*y, Mix(v0, v1, w0, w1), Mix(v1, v2, w1, w2)));
                                break;
                            case 14: 
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v0, v1, w0, w1), v1));
                                WorkingShape.Triangles.Add(new Triangle(v2, Mix(v0, v3, w0, w3), Mix(v0, v1, w0, w1)));
                                WorkingShape.Triangles.Add(new Triangle(v2, v3, Mix(v0, v3, w0, w3)));
                                allEdges.Add(new Edge(2*x-1, 2*(y-1), 2*(x-1), 2*y-1, Mix(v0, v3, w0, w3), Mix(v0, v1, w0, w1)));
                                break;
                            case 15:
                                fullCells[fullCells.Count - 1].Add(new IntVector2(x, y));
                                break;
                            default:
                                break;
                        }


                    }
                }
            }
        }

        private void extractOutlineFromEdges()
        {
            // Clear old borders and outline
            Array.Clear(bordersH, 0, width * height);
            Array.Clear(bordersV, 0, width * height);
            WorkingShape.Outlines.Clear();

            foreach(var edge in allEdges)
            {
                IntVector2 edgeCoordsA = new IntVector2(edge.ax / 2, edge.ay / 2);
                IntVector2 edgeCoordsB = new IntVector2(edge.bx / 2, edge.by / 2);
                if(edge.ax % 2 == 0)
                {
                    if (bordersV[edgeCoordsA.x, edgeCoordsA.y] == null)
                        bordersV[edgeCoordsA.x, edgeCoordsA.y] = new Border(edge);
                    else
                        bordersV[edgeCoordsA.x, edgeCoordsA.y].e1 = edge;

                    edge.b0 = bordersV[edgeCoordsA.x, edgeCoordsA.y];
                }
                else
                {
                    if (bordersH[edgeCoordsA.x, edgeCoordsA.y] == null)
                        bordersH[edgeCoordsA.x, edgeCoordsA.y] = new Border(edge);
                    else
                        bordersH[edgeCoordsA.x, edgeCoordsA.y].e1 = edge;

                    edge.b0 = bordersH[edgeCoordsA.x, edgeCoordsA.y];
                }

                if(edge.bx % 2 == 0)
                {
                    if (bordersV[edgeCoordsB.x, edgeCoordsB.y] == null)
                        bordersV[edgeCoordsB.x, edgeCoordsB.y] = new Border(edge);
                    else
                        bordersV[edgeCoordsB.x, edgeCoordsB.y].e1 = edge;

                    edge.b1 = bordersV[edgeCoordsB.x, edgeCoordsB.y];
                }
                else
                {
                    if (bordersH[edgeCoordsB.x, edgeCoordsB.y] == null)
                        bordersH[edgeCoordsB.x, edgeCoordsB.y] = new Border(edge);
                    else
                        bordersH[edgeCoordsB.x, edgeCoordsB.y].e1 = edge;

                    edge.b1 = bordersH[edgeCoordsB.x, edgeCoordsB.y];
                }
            }

            foreach(Border b in bordersH)
            {
                if (b == null)
                    continue;

                if(b.e0 != null)
                {
                    extractOutline(b.e0);
                }
                if(b.e1 != null)
                {
                    extractOutline(b.e1);
                }
            }
            foreach(Border b in bordersV)
            {
                extractOutline(b?.e0);
                extractOutline(b?.e1);
            }
        }

        private void extractTrianglesFromCells()
        {
            int triStartX = -1;
            bool startNew = true;
            foreach (var line in fullCells)
            {
                for (int i = 0; i < line.Count; ++i)
                {
                    var cell = line[i];

                    bool createTriangle = false;
                    if (i == 0 || startNew)
                    {
                        // new start
                        triStartX = cell.x - 1;
                        startNew = false;
                    }

                    if (i == line.Count - 1)
                    {
                        // need to close segment here
                        createTriangle = true;
                    }
                    else
                    {
                        if(cell.x+1 < line[i+1].x)
                        {
                            // gap
                            createTriangle = true;
                            startNew = true;
                        }
                    }

                    if(createTriangle)
                    {
                        WorkingShape.Triangles.Add(new Triangle(new Vector2(triStartX, cell.y-1), new Vector2(triStartX, cell.y), new Vector2(cell.x, cell.y-1)));
                        WorkingShape.Triangles.Add(new Triangle(new Vector2(triStartX, cell.y), new Vector2(cell.x, cell.y), new Vector2(cell.x, cell.y-1)));
                    }
                }
            }
        }

        private void extractTrianglesAndOutline()
        {
            // Smooth the terrain bitmap
            smoothTerrain(new float[]{1,1,1,1,1});
            //smoothTerrain(new float[]{1,1,1}); // Use this one if performance is an issue

            // Extract dirt cells and outline triangles/edges from the bitmap
            performMarchingSquares();

            // Extract the outline line segments from the aforementioned edges
            extractOutlineFromEdges();

            // Combine full dirt cells and create a couple of triangles from them
            extractTrianglesFromCells();
        }


        public void update()
        {
            if (WorkingRevision == Revision || WorkingShape != null)
                return;
            
            int currentRevision = WorkingRevision;

            lock (PublicShape)
            {
                WorkingShape = new TerrainShape
                {
                    TerrainBitmap = PublicShape.TerrainBitmap.Clone() as bool[,]
                };
            }
            
            // Enqueue update-task
            var Task = new Task(()=>{
                
                extractTrianglesAndOutline();

                lock(PublicShape)
                {
                    // Copy new triangles/outline to TerrainShape
                    PublicShape = WorkingShape;
                    WorkingShape = null;
                    Revision = currentRevision;
                }
            });
                
            Task.Start();

            if (currentRevision == 1)
                Task.Wait();
        }

        public List<Triangle> getTriangles(bool enforceUpdate = false)
        {
            update();

            lock (PublicShape)
            {
                return PublicShape.Triangles;
            }
        }
            

        public List<List<Vector2>> getOutline(bool enforceUpdate = false)
        {
            update();

            lock (PublicShape)
            {
                return PublicShape.Outlines;
            }
        }



		public struct IntVector2  {
			public int x, y;

			public IntVector2 (int x, int y) {
				this.x = x;
				this.y = y;
			}

            public static Vector2 operator*(IntVector2 v, float f) => new Vector2(v.x * f, v.y * f);
		}

        /*
		private void sdfOneIter(bool[] dirtPixels, float[] sdfPixels, int width, int height, IntVector2 dir)
		{
			// TODO(ks): allow other sdf-vectors besides -1/0/1 combinations

			var from = new IntVector2 (dir.x > 0 ? 0 : width-1, dir.y > 0 ? 0 : height-1);
			var to = new IntVector2 (dir.x <= 0 ? 0 : width-1, dir.y <= 0 ? 0 : height-1);

			var offset = new IntVector2 (0, 0);
			var size = new IntVector2 (0, 0);
			if (from.x < to.x) {
				size.x = to.x + 1;
				offset.x = 1;
			} else {
				size.x = from.x + 1;
				offset.x = -1;
			}
			if (from.y < to.y) {
				size.y = to.y + 1;
				offset.y = 1;
			} else {
				size.y = from.y + 1;
				offset.y = -1;
			}


			int borderX = dir.x == 0 ? -1 : dir.x > 0 ? Math.Min(from.x, to.x) : Math.Max(from.x, to.x);
			int borderY = dir.y == 0 ? -1 : dir.y > 0 ? Math.Min(from.y, to.y) : Math.Max(from.y, to.y);
			for (int y = from.y; offset.y > 0 ? y <= to.y : y >= to.y; y += offset.y) {
				for (int x = from.x; offset.x > 0 ? x <= to.x : x >= to.x; x += offset.x) {

					int curIndex = y * size.x + x;
					bool dirt = dirtPixels [curIndex];
					float value = 0;

					float increment = Math.Abs (dir.x) + Math.Abs (dir.y) > 1 ? (float)Math.Sqrt(2.0) : 1.0f;

					if (x == borderX || y == borderY) { // border case
						if (dirt)
							value = 127;
						else
							value = -128;
					} else {
						float valueOnTheLeft = sdfPixels [(y - dir.y) * size.x + x - dir.x];
						if (dirt)
							value = Math.Max(0.0f, Math.Min (valueOnTheLeft + increment, 127.0f));
						else
							value = Math.Min(-1.0f, Math.Max (valueOnTheLeft - increment, -128.0f));
					}

					float lastValue = sdfPixels [curIndex];
					float newValue = dirt ? Math.Min (lastValue, value) : Math.Max (lastValue, value);
					sdfPixels [curIndex] = newValue;
				}
			}

		}





		public Texture2D ExtractSignedDistanceField(Texture2D terrainTex)
		{
			int Width = terrainTex.Width;
			int Height = terrainTex.Height;

			Color[] pixels = new Color[Width * Height];
			terrainTex.GetData<Color> (pixels);


			bool[] dirtPixels = new bool[Width * Height];
			float[] sdfPixels = new float[Width * Height];

			// Initialize with "dirt"
			for (int y = 0; y < Height; ++y) {
				for (int x = 0; x < Width; ++x) {

					Color curPixel = pixels [y * Width + x];
					bool dirt = curPixel.R == curPixel.G && curPixel.G == curPixel.B && curPixel.R == 255;
					dirtPixels [y * Width + x] = dirt;

					sdfPixels [y * Width + x] = dirt ? 127 : -128;
				}
			}
				
			for (int i = -1; i <= 1; ++i)
				for (int j = -1; j <= 1; ++j) {

					// omit no-op
					if (i == 0 && j == 0)
						continue;

					sdfOneIter (dirtPixels, sdfPixels, Width, Height, new IntVector2 (i, j));
				}
					




			Texture2D sdf = new Texture2D (Ballz.The().Graphics.GraphicsDevice, Width, Height);

			Color[] sdfColorPixels = new Color[Width * Height];
			for (int y = 0; y < Height; ++y) {
				for (int x = 0; x < Width; ++x) {
					int colVal = (int)(sdfPixels [y * Width + x] + 128.0f);
					sdfColorPixels [y * Width + x] = new Color (colVal, colVal, colVal);
				}
			}
			sdf.SetData (sdfColorPixels);

			return sdf;
		}
        */

    }
}