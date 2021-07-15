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

using System;
using static Double3;

// One vertex of the silhouette of a polyhedron, given in one of two different ways:
//
// Case 1: the vertex is a vertex of the polyhedron
// Case 2: the vertex is the apparent intersection of two edges as seen from a point
//
// Indices are encoded as bytes so the polyhedron can only have up to 256 vertices.
// This struct can be passed to a shader as an uint
[Serializable]
public struct SilhouetteVertex {
	public uint data;

	uint a1 => (data >> 0) & 0xff;
	uint a2 => (data >> 8) & 0xff;
	uint b1 => (data >> 16) & 0xff;
	uint b2 => (data >> 24) & 0xff;

	bool isPolyhedronVertex => a1 == a2;
	public bool isEdgeIntersection => a1 != b1;
	
	// Vertex is a vertex of the polyhedron
	public SilhouetteVertex(uint index) {
		data = index | index << 8 | index << 16 | index << 24;
	}

	// Vertex is the apparent intersection of two edges
	public SilhouetteVertex(uint a1, uint a2, uint b1, uint b2) {
		if (b1 < a1) (a1, a2, b1, b2) = (b1, b2, a1, a2);

		data = a1 | a2 << 8 | b1 << 16 | b2 << 24;
	}

	public override bool Equals(object other) {
		return other is SilhouetteVertex v && v.data == data;
	}

	public override int GetHashCode() => data.GetHashCode();
	public static bool operator ==(SilhouetteVertex a, SilhouetteVertex b) => a.data == b.data;
	public static bool operator !=(SilhouetteVertex a, SilhouetteVertex b) => a.data != b.data;

	// Returns the edge that both a and b lie on
	public static (uint, uint) GetSharedEdge(SilhouetteVertex a, SilhouetteVertex b) {
		if (a.isPolyhedronVertex && b.isPolyhedronVertex) {
			return (a.a1, b.a1);
		}

		if (a.isEdgeIntersection && b.isEdgeIntersection) {
			if (a.a1 == b.a1 || a.a1 == b.b1) return (a.a1, a.a2);
			if (a.b1 == b.a1 || a.b1 == b.b1) return (a.b1, a.b2);
		}

		if (a.isEdgeIntersection) {
			if (a.a1 == b.a1 || a.a2 == b.a1) {
				return (a.a1, a.a2);
			} else {
				return (a.b1, a.b2);
			}
		} else if (b.isEdgeIntersection) {
			if (b.a1 == a.a1 || b.a2 == a.a1) {
				return (b.a1, b.a2);
			} else {
				return (b.b1, b.b2);
			}
		}

		throw new Exception("Unexpected silhouette edge configuration. This exception should be unreachable");
	}

	public Double3 GetPosition(Polyhedron polyhedron, Double3 observer) {
		if (isEdgeIntersection) {
			var A1 = polyhedron.vertices[a1];
			var A2 = polyhedron.vertices[a2];
			var B1 = polyhedron.vertices[b1];
			var B2 = polyhedron.vertices[b2];

			Polyhedron.ApparentIntersection(A1, A2, B1, B2, observer, out var ta, out var tb);

			return Lerp(A1, A2, ta);
		} else {
			return polyhedron.vertices[data & 0xff];
		}
	}

	public Double3 GetDirection(Polyhedron polyhedron, Double3 observer) {
		return (GetPosition(polyhedron, observer) - observer).normalized;
	}
}