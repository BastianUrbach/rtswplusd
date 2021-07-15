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
using System.Linq;
using UnityEngine;

using static Double3;
using static Utility;

// Represents a polyhedron. Compared to Unity's builtin Mesh class, this class provides easy access
// to relevant features of the polyhedron without unwanted artifacts like duplicate vertices and
// edges. Algorithms for calculating the silhouette are provided elsewhere as extension methods for
// this class.
public class Polyhedron {
	public Double3[] vertices;
	public Plane[] planes;
	public Edge[] edges;
	public (int, int, int)[] triangles;
	public bool[] vertexConcavity;
	public Double3[] concaveVertices;
	public int[] concaveVertexIndices;
	public Bounds bounds;

	public Edge[] silhouetteCalculationBuffer;

	public Polyhedron(Mesh mesh) {
		var vertices = mesh.vertices;
		var indices = mesh.triangles;

		var vertexRemap = new int[vertices.Length];
		var distinctVertices = new List<Double3>();
		var distinctPlanes = new List<Plane>();
		var distinctEdges = new List<Edge>();
		var preliminaryEdges = new List<Edge>();

		triangles = new (int, int, int)[indices.Length / 3];

		// Find distinct vertices and fill vertexRemap such that vertexRemap[oldIndex] == newIndex
		for (int i = 0; i < vertices.Length; i++) {
			var vertex = vertices[i];
			var isNew = true;

			for (int j = 0; j < distinctVertices.Count; j++) {
				if (distinctVertices[j] == (Double3)vertex) {
					vertexRemap[i] = j;
					isNew = false;
					break;
				}
			}

			if (isNew) {
				vertexRemap[i] = distinctVertices.Count;
				distinctVertices.Add(vertex);
			}
		}

		this.vertices = distinctVertices.ToArray();

		vertexConcavity = new bool[distinctVertices.Count];

		// Find distinct edges and planes
		for (int i = 0; i < indices.Length; i += 3) {
			var a = vertexRemap[indices[i + 0]];
			var b = vertexRemap[indices[i + 1]];
			var c = vertexRemap[indices[i + 2]];

			var A = distinctVertices[a];
			var B = distinctVertices[b];
			var C = distinctVertices[c];
			
			var plane = new Plane(A, B, C);
			var planeIndex = -1;

			for (int j = 0; j < distinctPlanes.Count; j++) {
				if (distinctPlanes[j] == plane) {
					planeIndex = j;
					break;
				}
			}

			if (planeIndex < 0) {
				planeIndex = distinctPlanes.Count;
				distinctPlanes.Add(plane);
			}

			InsertEdge(distinctPlanes, distinctEdges, preliminaryEdges, a, b, c, planeIndex);
			InsertEdge(distinctPlanes, distinctEdges, preliminaryEdges, b, c, a, planeIndex);
			InsertEdge(distinctPlanes, distinctEdges, preliminaryEdges, c, a, b, planeIndex);

			triangles[i / 3] = (a, b, c);
		}

		this.edges = distinctEdges.ToArray();
		this.planes = distinctPlanes.ToArray();

		concaveVertices = this.vertices.Where((v, i) => vertexConcavity[i]).ToArray();
		concaveVertexIndices = Enumerable.Range(0, vertexConcavity.Length).Where(i => vertexConcavity[i]).ToArray();

		FixEdgeOrientations();

		// Avoid allocations for calcuating convex silhouettes
		silhouetteCalculationBuffer = new Edge[edges.Length];

		// Bounds
		mesh.RecalculateBounds();
		bounds = mesh.bounds;
	}

	public bool IsInside(Double3 point) {
		int counter = 0;

		foreach (var (a, b, c) in triangles) {
			var A = vertices[a];
			var B = vertices[b];
			var C = vertices[c];

			var sideAB = new Plane(A, B, A + up).GetSide(point);
			var sideBC = new Plane(A, B, A + up).GetSide(point);
			var sideCA = new Plane(A, B, A + up).GetSide(point);
			var planeABC = new UnityEngine.Plane((Vector3)A, (Vector3)B, (Vector3)C);

			if (planeABC.Raycast(new Ray((Vector3)point, Vector3.up), out var enter)) {
				if (sideAB == sideBC && sideBC == sideCA) {
					counter++;
				}
			}
		}

		return (counter % 2) == 1;
	}

	// Used in constructor for finding distinct, real edges (as opposed to duplicates and edges that
	// only exist due to face triangulation)
	void InsertEdge(List<Plane> planes, List<Edge> edges, List<Edge> preliminary, int start, int end, int third, int plane) {
		bool wasCompleted = false;

		var edge = new Edge { start = (uint)start, end = (uint)end, plane1 = (uint)plane };
		var tangent = vertices[third] - vertices[start];

		for (int i = 0; i < preliminary.Count; i++) {
			var p = preliminary[i];
			
			if (p.start == start && p.end == end || p.start == end && p.end == start) {
				// If both edges have the same face then the edge is not a real edge
				if (p.plane1 != plane) {
					edge.plane2 = p.plane1;
					edges.Add(edge);

					bool isConcave = Dot(tangent, planes[(int)edge.plane2].normal) > 0;
					vertexConcavity[start] |= isConcave;
					vertexConcavity[end] |= isConcave;
				}

				preliminary.RemoveAt(i);
				wasCompleted = true;
				break;
			}
		}

		if (!wasCompleted) {
			preliminary.Add(edge);
		}
	}

	// A silhouette edge has polyhedron on one side and nothing/background on the other. When an
	// edge is a silhouette edge we want to determine on which side of it the polyhedron is by
	// checking which one of the two faces is visible. To make this possible some edges need to be
	// flipped
	void FixEdgeOrientations() {
		for (int i = 0; i < edges.Length; i++) {
			var edge = edges[i];
			var normal1 = planes[edge.plane1].normal;
			var normal2 = planes[edge.plane2].normal;
			var start = vertices[edge.start];
			var end = vertices[edge.end];

			if (Dot(Cross(normal1, normal2), end - start) < 0) {
				Swap(ref edge.plane1, ref edge.plane2);
			}

			edges[i] = edge;
		}
	}

	static int Sign(double a) {
		if (a < 0) return -1;
		if (a > 0) return 1;

		return 0;
	}

	// Test if edges a and b appear to intersect as seen from p and return interpolation parameters
	// ta and tb for the intersection point along a and b (note that these generally don't result
	// in the same 3D point since a and b don't actually intersect)
	public static bool ApparentIntersection(Double3 a1, Double3 a2, Double3 b1, Double3 b2, Double3 p, out double ta, out double tb) {
		var an = Cross(a1 - p, a2 - p);
		var bn = Cross(b1 - p, b2 - p);

		ta = Dot(p - a1, bn) / Dot(a2 - a1, bn);
		tb = Dot(p - b1, an) / Dot(b2 - b1, an);

		var d = Cross(an, bn);

		if (Sign(Dot(Lerp(a1, a2, ta) - p, d)) != Sign(Dot(Lerp(b1, b2, tb) - p, d))) {
			return false;
		}

		return ta > epsilon & ta < 1 - epsilon & tb > epsilon & tb < 1 - epsilon;
	}

	[System.Serializable]
	public struct Edge {
		public uint start;
		public uint end;
		public uint plane1;
		public uint plane2;
	}
}