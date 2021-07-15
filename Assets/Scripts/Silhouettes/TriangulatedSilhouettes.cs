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

public static class TriangulatedSilhouettes {
	const double epsilon = 0.000001;

    // Like GetSilhouette but triangulates the silhouette
	public static IEnumerable<SilhouetteTriangle> GetTriangulatedSilhouette(this Polyhedron polyhedron, Double3 point, bool random = true) {
		var vertices = new List<SilhouetteVertex>();
		var edges = new List<(int, int)>();

		foreach (var edge in polyhedron.GetSilhouette(point)) {
			var a1 = FindOrInsert(vertices, edge.start);
			var a2 = FindOrInsert(vertices, edge.end);
			edges.Add((a1, a2));
		}

		if (edges.Count == 0) yield break;

		var vertexDirections = vertices.Select(v => v.GetDirection(polyhedron, point)).ToList();
		var optionals = polyhedron.concaveVertices.Select(v => (v - point).normalized).ToList();
		var triangulation = new SphericalTriangulation(edges, vertexDirections, polyhedron.concaveVertices);

		triangulation.Triangulate(random);

		var triangles = triangulation.FindTriangles().ToArray();

		var rayDirections = triangles.Select(t => {
			var (a, b, c) = t;
			var A = triangulation.vertices[a];
			var B = triangulation.vertices[b];
			var C = triangulation.vertices[c];
			return (Double3)(A + B + C);
		}).ToArray();

		var r = polyhedron.Raycast(point, rayDirections);

		for (int i = 0; i < triangles.Length; i++) {
			var (a, b, c) = triangles[i];

			a = triangulation.TranslateIndex(a, out bool aIsOptional);
			b = triangulation.TranslateIndex(b, out bool bIsOptional);
			c = triangulation.TranslateIndex(c, out bool cIsOptional);

			var A = aIsOptional ? new SilhouetteVertex((uint)polyhedron.concaveVertexIndices[a]) : vertices[a];
			var B = bIsOptional ? new SilhouetteVertex((uint)polyhedron.concaveVertexIndices[b]) : vertices[b];
			var C = cIsOptional ? new SilhouetteVertex((uint)polyhedron.concaveVertexIndices[c]) : vertices[c];

			if (r[i]) yield return new SilhouetteTriangle(A, B, C);
		}
	}

	static int FindOrInsert(List<SilhouetteVertex> list, SilhouetteVertex element) {
		for (int i = 0; i < list.Count; i++) {
			if (list[i] == element) return i;
		}

		list.Add(element);
		return list.Count - 1;
	}

	public static bool[] Raycast(this Polyhedron polyhedron, Double3 point, Double3[] directions) {
		bool[] r = new bool[directions.Length];

		for (var i = 0; i < directions.Length; i++) {
			var direction = directions[i];

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

				if (Dot(normalAB, direction) < epsilon) continue;
				if (Dot(normalBC, direction) < epsilon) continue;
				if (Dot(normalCA, direction) < epsilon) continue;
				if (Dot(A + B + C, direction) < 0) continue;

				r[i] = true;
			}
		}

		return r;
	}
}
