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
using static Utility;
using static Polyhedron;
using static Double3;

public static class ConvexSilhouettes {
    // Enumerates the contour edges of a polyhedron as seen from the given point. For convex
	// polyhedra these are the silhouette edges.
	// This is significantly faster than GetSilhouette() (implemented in NonConvexSilhouettes)
	public static IEnumerable<Edge> GetConvexSilhouette(this Polyhedron polyhedron, Double3 point) {
		foreach (var edge in polyhedron.edges) {
			if (polyhedron.IsContourEdge(edge, point, out var orientedEdge)) {
				yield return orientedEdge;
			}
		}
	}

	// Enumerates the ordered silhouette vertices of a polyhedron as seen from the given point.
	// This is significantly faster than GetOrderedSilhouette() (implemented in NonConvexSilhouettes)
	public static IEnumerable<uint> GetOrderedConvexSilhouette(this Polyhedron polyhedron, Double3 point) {
		var silhouetteEdgeCount = 0;
		var edges = polyhedron.silhouetteCalculationBuffer;

		foreach (var edge in polyhedron.GetConvexSilhouette(point)) {
			edges[silhouetteEdgeCount++] = edge;
		}

		if (silhouetteEdgeCount == 0) yield break;

		var first = edges[0].start;
		var current = first;

		while (silhouetteEdgeCount > 0) {
			bool foundNextEdge = false;

			for (int i = 0; i < silhouetteEdgeCount; i++) {
				var edge = edges[i];

				if (edge.start == current) {
					yield return (uint)current;
					current = edge.end;
					Swap(ref edges[--silhouetteEdgeCount], ref edges[i]);
					foundNextEdge = true;
					break;
				}
			}

			if (!foundNextEdge) break;
		}
	}

	// Check if an edge is a contour edge when viewed from a given point. If it is then orientedEdge
	// is either the edge or a flipped version of it such that all silhouette edges go around the
	// polyhedron in the same direction
	public static bool IsContourEdge(this Polyhedron polyhedron, Edge edge, Double3 point, out Edge orientedEdge) {
		var normal1 = polyhedron.planes[edge.plane1].normal;
		var normal2 = polyhedron.planes[edge.plane2].normal;

		var relative = polyhedron.vertices[edge.start] - point;

		var visible1 = Dot(relative, normal1) > 0;
		var visible2 = Dot(relative, normal2) > 0;

		if (visible1) {
			orientedEdge = edge;
		} else {
			orientedEdge = new Edge { start = edge.end, end = edge.start, plane1 = edge.plane2, plane2 = edge.plane1 };
		}

		return visible1 != visible2;
	}
}