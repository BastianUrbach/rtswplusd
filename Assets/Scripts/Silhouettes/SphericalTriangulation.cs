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
using System.Linq;
using static Double3;

public class SphericalTriangulation {
	const double epsilon = 0.00001;

	public List<Double3> vertices;
	public List<Double3> optionals;
	public List<(int, int)> edges;

	List<int> optionalIndexMap = new List<int>();
	int originalVertexCount;

	List<List<int>> adjacency;
	List<(int, int)> vertexPairs;

	public SphericalTriangulation(IEnumerable<(int, int)> edges, IEnumerable<Double3> vertices, IEnumerable<Double3> optionals) {
		this.vertices = vertices.ToList();
		this.optionals = optionals.ToList();
		this.edges = new List<(int, int)>();
		
		adjacency = new List<List<int>>();
		originalVertexCount = this.vertices.Count;

		for (int i = 0; i < this.vertices.Count; i++) {
			adjacency.Add(new List<int>());

			this.optionals.RemoveAll(v => Distance(v.normalized, this.vertices[i].normalized) < 0.0001);
		}

		foreach (var (a1, a2) in edges) {
			AddEdge(a1, a2);
		}
	}

	public void Triangulate(bool random = true) {
		if (random) {
			Random.InitState(System.DateTime.Now.Millisecond);
		} else {
			Random.InitState(0);
		}

		vertexPairs = new List<(int, int)>();

		for (int a1 = 0; a1 < vertices.Count; a1++) {
			for (int a2 = a1 + 1; a2 < vertices.Count; a2++) {
				vertexPairs.Add((a1, a2));
			}
		}

		FindDiagonals();

		var isInTriangle = TriangleTest(FindTriangles(), optionals);

		for (int i = 0; i < optionals.Count; i++) {
			var o = optionals[i];

			if (!isInTriangle[i]) {
				AddVertex(optionals[i]);
				optionalIndexMap.Add(i);
			}
		}

		FindDiagonals();
	}

	void AddEdge(int a1, int a2) {
		edges.Add((a1, a2));
		adjacency[a1].Add(a2);
		adjacency[a2].Add(a1);
	}

	int AddVertex(Double3 vertex) {
		for (int i = 0; i < vertices.Count; i++) {
			vertexPairs.Add((i, vertices.Count));
		}

		vertices.Add(vertex);
		adjacency.Add(new List<int>());

		return vertices.Count - 1;
	}

	void FindDiagonals() {
		while (vertexPairs.Count > 0) {
			var i = Random.Range(0, vertexPairs.Count);
			var (a1, a2) = vertexPairs[i];

			vertexPairs[i] = vertexPairs[vertexPairs.Count - 1];
			vertexPairs.RemoveAt(vertexPairs.Count - 1);
			
			TryConnect(a1, a2);
		}
	}

	void TryConnect(int a1, int a2) {
		var A1 = vertices[a1];
		var A2 = vertices[a2];

		foreach (var (b1, b2) in edges) {
			if (a1 == b1 || a2 == b1 || a1 == b2 || a2 == b2) continue;

			var B1 = vertices[b1];
			var B2 = vertices[b2];

			if (Intersects(A1, A2, B1, B2)) return;
		}

		AddEdge(a1, a2);
	}

	bool Intersects(Double3 A1, Double3 A2, Double3 B1, Double3 B2) {
		var normalA = Cross(A1, A2);
		var normalB = Cross(B1, B2);
		var intersection = Cross(normalA, normalB);

		if (Sign(Dot(B1, normalA)) == Sign(Dot(B2, normalA))) return false;
		if (Sign(Dot(A1, normalB)) == Sign(Dot(A2, normalB))) return false;
		if (Sign(Dot(intersection, A1 + A2)) != Sign(Dot(intersection, B1 + B2))) return false;

		return true;
	}

	int Sign(double d) {
		return d > epsilon ? 1 : d < -epsilon ? -1 : 0;
	}

	(int, int, int) Sort(int a, int b, int c) {
		if (a > b) (a, b) = (b, a);
		if (a > c) (a, c) = (c, a);
		if (b > c) (b, c) = (c, b);

		return (a, b, c);
	}

	bool IsValidTriangle(int a, int b, int c) {
		var A = vertices[a];
		var B = vertices[b];
		var C = vertices[c];

		var normalAB = Cross(A, B);
		var normalBC = Cross(B, C);
		var normalCA = Cross(C, A);

		if (Dot(normalAB, C) < 0) normalAB = normalAB * (-1);
		if (Dot(normalBC, A) < 0) normalBC = normalBC * (-1);
		if (Dot(normalCA, B) < 0) normalCA = normalCA * (-1);

		for (int i = 0; i < vertices.Count; i++) {
			if (i == a | i == b | i == c) continue;

			var I = vertices[i];

			if (Dot(normalAB, I) < 0) continue;
			if (Dot(normalBC, I) < 0) continue;
			if (Dot(normalCA, I) < 0) continue;

			return false;
		}

		return true;
	}

	public IEnumerable<(int, int, int)> FindTriangles() {
		var triangles = new HashSet<(int, int, int)>();

		for (int a = 0; a < vertices.Count; a++) {
			var A = vertices[a];

			if (adjacency[a].Count < 2) continue;

			var zeroDirection = vertices[adjacency[a][0]] - A;
			var adjacent = adjacency[a].OrderBy(c => SignedAngle(zeroDirection, vertices[c] - A, A)).ToArray();

			for (int i = 0; i < adjacent.Length; i++) {
				var b = adjacent[i];
				var c = adjacent[(i + 1) % adjacent.Length];

				if (adjacency[b].Contains(c)) {
					triangles.Add(Sort(a, b, c));
				}
			}
		}

		foreach (var (a, b, c) in triangles) {
			if (!IsValidTriangle(a, b, c)) continue;

			var A = vertices[a];
			var B = vertices[b];
			var C = vertices[c];

			if (Dot(Cross(B - A, C - A), A) > 0) {
				yield return (a, b, c);
			} else {
				yield return (b, a, c);
			}
		}
	}

	// Determines the index that a vertex had in the original vertex or optional list
	public int TranslateIndex(int index, out bool isOptional) {
		if (index >= originalVertexCount) {
			isOptional = true;
			return optionalIndexMap[index - originalVertexCount];
		} else {
			isOptional = false;
			return index;
		}
	}

	bool[] TriangleTest(IEnumerable<(int, int, int)> triangles, List<Double3> directions) {
		bool[] r = new bool[directions.Count];

		for (var i = 0; i < directions.Count; i++) {
			var direction = directions[i];

			foreach (var (a, b, c) in triangles) {
				var A = vertices[a];
				var B = vertices[b];
				var C = vertices[c];

				var normalAB = Cross(A, B).normalized;
				var normalBC = Cross(B, C).normalized;
				var normalCA = Cross(C, A).normalized;

				normalAB *= (Dot(normalAB, C) > 0 ? 1 : -1);
				normalBC *= (Dot(normalBC, A) > 0 ? 1 : -1);
				normalCA *= (Dot(normalCA, B) > 0 ? 1 : -1);

				if (Dot(normalAB, direction) < -epsilon) continue;
				if (Dot(normalBC, direction) < -epsilon) continue;
				if (Dot(normalCA, direction) < -epsilon) continue;

				var normal = Cross(B - A, C - A);
				var d = Dot(A, normal) / Dot(direction, normal);
				
				if (d < 0) continue;

				r[i] = true;
			}
		}

		return r;
	}
}