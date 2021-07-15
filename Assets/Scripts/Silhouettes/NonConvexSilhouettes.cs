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
using static Double3;
using static Utility;

public static class NonConvexSilhouettes {
    // Enumerates the silhouette edges of a polyhedron as seen from the given point.
	// Algorithm outline:
	// 1. Find contour edges
	// 2. Find apparent intersections of contour edges
	// 3. Split contour edges at intersections so that only non-intersecting segments remain
	// 4. Eliminate all segments where the ray through the center hits a triangle
	public static IEnumerable<SilhouetteEdge> GetSilhouette(this Polyhedron polyhedron, Double3 point) {
		// if (polyhedron.IsInside(point)) return Enumerable.Empty<SilhouetteEdge>();

		// Find contour edges. Every silhouette edge is a segment of a contour edge
		var contourEdges = polyhedron.GetConvexSilhouette(point).ToList();

		// Holds intersections of the current contour edge with other contour edges.
		// Despite being declared outside the loop for performance reasons, the content of this
		// array only has a meaning within one iteration
		// The first value is the index of the intersecting edge, the second one the position of the
		// intersection on the current edge as an interpolation parameter in range [0, 1]
		var intersections = new (int, double)[contourEdges.Count];

		// Holds silhouette edges and their midpoints (for performance reasons)
		var silhouetteEdges = new List<(SilhouetteVertex, SilhouetteVertex, Double3)>();

		// A comparer to sort intersections by their position on the edge
		var comparer = Comparer<(int, double)>.Create((a, b) => a.Item2.CompareTo(b.Item2));

		// Split contour edges at intersections
		for (int i = 0; i < contourEdges.Count; i++) {
			var count = 0;

			var a = contourEdges[i];
			var a1 = polyhedron.vertices[a.start];
			var a2 = polyhedron.vertices[a.end];

			// Find all intersections with other edges
			for (int j = 0; j < contourEdges.Count; j++) {
				if (i == j) continue;

				var b = contourEdges[j];
				var b1 = polyhedron.vertices[b.start];
				var b2 = polyhedron.vertices[b.end];

				if (Polyhedron.ApparentIntersection(a1, a2, b1, b2, point, out var ta, out var tb)) {
					intersections[count++] = (j, ta);
				}
			}

			// Sort intersections by position along the edge
			// This often does nothing because the vast majority of contour edges have just one
			// or no intersections. But if there are more intersections, they need to be sorted.
			System.Array.Sort(intersections, 0, count, comparer);

			// Generate edge segments
			var previousVertex = new SilhouetteVertex(a.start);
			var previousT = 0.0;

			for (int j = 0; j < count; j++) {
				var (edgeIndex, t) = intersections[j];
				var b = contourEdges[edgeIndex];
				var currentVertex = new SilhouetteVertex(a.start, a.end, b.start, b.end);
				var midPoint = Lerp(a1, a2, (previousT + t) / 2) - point;

				silhouetteEdges.Add((previousVertex, currentVertex, midPoint));
				previousVertex = currentVertex;
				previousT = t;
			}

			silhouetteEdges.Add((previousVertex, new SilhouetteVertex(a.end), Lerp(a1, a2, (previousT + 1) / 2) - point));
		}

		// Cast a ray through the midpoint of each segment. If the ray hits a triangle of the
		// light source, the segment is not a silhouette edge
		foreach (var (a, b, c) in polyhedron.triangles) {
			var A = polyhedron.vertices[a] - point;
			var B = polyhedron.vertices[b] - point;
			var C = polyhedron.vertices[c] - point;

			var normalAB = Cross(A, B).normalized;
			var normalBC = Cross(B, C).normalized;
			var normalCA = Cross(C, A).normalized;

			normalAB = Dot(normalAB, C) > 0 ? normalAB : -normalAB;
			normalBC = Dot(normalBC, A) > 0 ? normalBC : -normalBC;
			normalCA = Dot(normalCA, B) > 0 ? normalCA : -normalCA;

			var plane = new Plane(Cross(B - A, C - A), zero);

			for (int j = 0; j < silhouetteEdges.Count; j++) {
				var (v1, v2, m) = silhouetteEdges[j];

				if (Dot(normalAB, m) < epsilon) continue;
				if (Dot(normalBC, m) < epsilon) continue;
				if (Dot(normalCA, m) < epsilon) continue;
				if (Dot(A + B + C, m) < 0) continue;

				// Remove segment as it's not a silhouette edge
				silhouetteEdges[j] = silhouetteEdges[silhouetteEdges.Count - 1];
				silhouetteEdges.RemoveAt(silhouetteEdges.Count - 1);
				j--;
			}
		}

		return silhouetteEdges.Select(e => new SilhouetteEdge(e.Item1, e.Item2));
	}

	// Like GetSilhouette but sorts closed polygons (outer boundary and holes) together
	public static IEnumerable<SilhouetteEdge> GetOrderedSilhouette(this Polyhedron polyhedron, Double3 point) {
		var edges = polyhedron.GetSilhouette(point).ToList();

		for (int i = 0; i < edges.Count; i++) {
			var end = edges[i].end;

			for (int j = i + 1; j < edges.Count; j++) {
				var start = edges[j].start;

				if (end == start) {
					var temp = edges[j];
					edges[j] = edges[i + 1];
					edges[i + 1] = temp;
					break;
				}
			}
		}

		return edges;
	}
}