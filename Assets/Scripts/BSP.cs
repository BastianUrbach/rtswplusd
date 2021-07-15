// Copyright (C) 2021, Bastian Urbach
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using UnityEngine;

using static System.Array;
using static Utility;
using static Double3;

// Binary space partitioning
//
// Partitions space into convex cells using the given planes and organizes these cells in a binary
// tree. Each cell is a leaf in the tree and stores one representative point.
//
// This version builds two trees: one to find the cells and one for the structure of the tree. When
// building the second tree, the resulting cells are already known. This is used to build the second
// tree in a way that results in a lower average depth.
public class BSP {
	public List<Node> nodes = new List<Node>();
	public List<Double3> leaves = new List<Double3>();
	public int root;

	FastList<Edge>    edges             = new FastList<Edge>(256);
	FastList<Plane>   planes            = new FastList<Plane>(256);
	FastList<Double3> planeVertices     = new FastList<Double3>(256);
	FastList<double>  planeVertexAngles = new FastList<double>(256);

	// Builds a BSP from the given planes.
	//
	// The algorithm begins with the edges of the given bounding box. These define a convex cell
	// that is then subdivided recursively using one plane in each step.
	public BSP(IEnumerable<Plane> planes, Bounds bounds) {
		LoadCube(bounds);

		foreach (var plane in planes) {
			this.planes.Add(plane);
		}

		// dummy to make sure no element 0 exists
		leaves.Add(bounds.center);

		root = MakeNode((0, this.planes.count), (0, edges.count));
	}
	
	// Make a node and return its index in the node list. The index is positive for inner nodes and
	// negative for leaves. A leaf is created if no plane subdivides the cell further.
	int MakeNode((int, int) planeSpan, (int, int) edgeSpan) {
		var (planeStart, planeEnd) = planeSpan;
		
		var planeIndex = -1;

		for (int i = planeStart; i < planeEnd; i++) {
			var plane = planes[i];
			// var rating = EvaluateCut(plane, edgeSpan);
			var isValid = TestCut(plane, edgeSpan);

			if (isValid) {
				planeIndex = planes.count;
				planes.Add(plane);
			}
		}

		if (planeIndex == -1) {
			return -MakeLeaf(edgeSpan);
		} else {
			var bestPlane = planes[planeIndex];
			planes.RemoveAt(planeIndex);

			return +MakeInnerNode((planeEnd, planes.count), edgeSpan, bestPlane);
		}
	}

	// Creates an inner node that splits the current cell at the given plane and returns its index
	int MakeInnerNode((int, int) planeSpan, (int, int) edgeSpan, Plane plane) {
		var (planeStart, planeEnd) = planeSpan;
		var (edgeStart, edgeEnd) = edgeSpan;

		var node = new Node { plane = plane };
		var nodeIndex = nodes.Count;
		nodes.Add(node);

		node.left = MakeNode(planeSpan, Split(plane, edgeSpan));
		node.right = MakeNode(planeSpan, Split(plane.flipped, edgeSpan));

		edges.count = edgeStart;
		planes.count = planeStart;

		nodes[nodeIndex] = node;

		return nodeIndex;
	}

	// Creates a leaf and returns its index
	int MakeLeaf((int, int) edgeSpan) {
		var (edgeStart, edgeEnd) = edgeSpan;
		var point = zero;
		var totalWeight = 0.0;

		for (int i = edgeStart; i < edgeEnd; i++) {
			var edge = edges[i];

			var weightA = Random.Range(0.5f, 1f) / edge.a.magnitude;
			var weightB = Random.Range(0.5f, 1f) / edge.b.magnitude;

			point += edge.a * weightA;
			point += edge.b * weightB;

			totalWeight += weightA + weightB;
		}

		point /= totalWeight;

		leaves.Add(point);

		return leaves.Count - 1;
	}

	// Test if plane would result in a valid cut with parts of the cell on either side
	bool TestCut(Plane plane, (int, int) edgeSpan) {
		var (edgeStart, edgeEnd) = edgeSpan;

		int count1 = 0, count2 = 0;

		for (int i = edgeStart; i < edgeEnd; i++) {
			var edge = edges[i];
			var distanceA = plane.GetDistanceToPoint(edge.a);
			var distanceB = plane.GetDistanceToPoint(edge.b);

			count1 += distanceA > +epsilon ? 1 : 0;
			count2 += distanceA < -epsilon ? 1 : 0;
			count1 += distanceB > +epsilon ? 1 : 0;
			count2 += distanceB < -epsilon ? 1 : 0;
		}

		return count1 > 0 & count2 > 0;
	}

	// Cuts away the parts of the cell on the negative side of the plane. This is done in two steps:
	// 1. Cut each edge and discard it if it's entirely on the wrong side, keep track of
	//    intersection points
	// 2. Create new edges in the plane by adding the edges of the convex hull of the intersections.
	//    This closes the cut faces of the convex polyhedron
	// The resulting new cell is pushed onto the edge list and its index range is returned.
	(int, int) Split(Plane plane, (int, int) edgeSpan) {
		var (edgeStart, edgeEnd) = edgeSpan;
		var newEdgesStart = edges.count;
		var cutFaceCenter = zero;

		planeVertices.count = 0;
		planeVertexAngles.count = 0;

		// Split/discard existing edges
		for (int i = edgeStart; i < edgeEnd; i++) {
			var edge = edges[i];

			var distanceA = plane.GetDistanceToPoint(edge.a);
			var distanceB = plane.GetDistanceToPoint(edge.b);

			var t = DivideSafe(distanceA, distanceA - distanceB);
			var intersection = Lerp(edge.a, edge.b, Clamp(t, epsilon, 1 - epsilon));

			if (distanceA < 0) edge.a = intersection;
			if (distanceB < 0) edge.b = intersection;

			var length = Distance(edge.a, edge.b);

			if (length > epsilon) edges.Add(edge);

			if (distanceA < epsilon & distanceB > -epsilon || distanceA > -epsilon & distanceB < epsilon) {
				planeVertices.Add(intersection);
				cutFaceCenter += intersection;
			}
		}

		if (planeVertices.count < 3) {
			return (newEdgesStart, edges.count);
		}

		// Sort vertices in clip plane by angle around center
		cutFaceCenter /= planeVertices.count;

		var up = (planeVertices[0] - cutFaceCenter).normalized;
		var right = Cross(plane.normal, up);

		for (int i = 0; i < planeVertices.count; i++) {
			planeVertexAngles.Add(PseudoAngle(up, right, planeVertices[i] - cutFaceCenter));
		}

		Sort(planeVertexAngles.data, planeVertices.data, 0, planeVertices.count);

		// Create new edges
		var newEdge = new Edge();
		newEdge.a = planeVertices[planeVertices.count - 1];

		for (int i = 0; i < planeVertices.count; i++) {
			newEdge.b = planeVertices[i];
			edges.Add(newEdge);
			newEdge.a = newEdge.b;
		}

		return (newEdgesStart, edges.count);
	}
	
	// Sorting by this leads to the same order as sorting by angle
	public static double PseudoAngle(Double3 up, Double3 right, Double3 v) {
		var dx = Dot(right, v);
		var dy = Dot(up, v);

		if (System.Math.Abs(dx) > System.Math.Abs(dy)) {
			return (dx > 0 ? 0 : 4) + dy / dx;
		} else {
			return (dy > 0 ? 2 : 6) - dx / dy;
		}
	}

	// Pushes the edges of the given bounding box to the edge list
	void LoadCube(Bounds bounds) {
		var min = bounds.min;
		var max = bounds.max;
		var size = bounds.size;

		var v1 = new Double3(min.x, min.y, min.z);
		var v2 = new Double3(max.x, min.y, min.z);
		var v3 = new Double3(min.x, max.y, min.z);
		var v4 = new Double3(max.x, max.y, min.z);
		var v5 = new Double3(min.x, min.y, max.z);
		var v6 = new Double3(max.x, min.y, max.z);
		var v7 = new Double3(min.x, max.y, max.z);
		var v8 = new Double3(max.x, max.y, max.z);

		edges.Add(new Edge { a = v1, b = v2 });
		edges.Add(new Edge { a = v3, b = v4 });
		edges.Add(new Edge { a = v5, b = v6 });
		edges.Add(new Edge { a = v7, b = v8 });
		
		edges.Add(new Edge { a = v1, b = v3 });
		edges.Add(new Edge { a = v2, b = v4 });
		edges.Add(new Edge { a = v5, b = v7 });
		edges.Add(new Edge { a = v6, b = v8 });

		edges.Add(new Edge { a = v1, b = v5 });
		edges.Add(new Edge { a = v2, b = v6 });
		edges.Add(new Edge { a = v3, b = v7 });
		edges.Add(new Edge { a = v4, b = v8 });
	}

	public struct Node {
		public Plane plane;
		public int left;
		public int right;
	}

	public struct Edge {
		public Double3 a;
		public Double3 b;
	}
}