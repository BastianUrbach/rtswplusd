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
using static Utility;
using static Double3;
using System.Linq;

[CreateAssetMenu]
public class NonConvexSilhouetteBSP : ScriptableObject {
	public Mesh mesh;

	public Node[] nodes;
	public SilhouetteEdge[] silhouettes;
	public Vector3[] vertices;
	public int root;
	public bool isInitialized = false;

	Polyhedron _polyhedron;
	public Polyhedron polyhedron => _polyhedron ??= new Polyhedron(mesh);

	[ContextMenu("Bake")]
	public void Initialize() {
		var bounds = new Bounds(UnityEngine.Vector3.zero, UnityEngine.Vector3.one * 100);
		var planes = FindPlanes();
		
		var bsp = new BSP(planes, bounds);

		root = bsp.root;
		
		var nodes = new List<Node>();

		foreach (var node in bsp.nodes) {
			nodes.Add(new Node {
				plane = (Vector4)node.plane,
				left = node.left,
				right = node.right
			});
		}

		// Create silhouette lists and add dummy element to prevent index 0 from being used because
		// leaves are recognized by the sign of the index
		var silhouettes = new List<SilhouetteEdge>();
		silhouettes.Add(new SilhouetteEdge(new SilhouetteVertex(0), new SilhouetteVertex(0)));

		for (int i = 0; i < bsp.nodes.Count; i++) {
			var node = nodes[i];

			if (node.left < 0) {
				node.left = MakeLeaf(-node.left, bsp, silhouettes, nodes);
			}

			if (node.right < 0) {
				node.right = MakeLeaf(-node.right, bsp, silhouettes, nodes);
			}

			nodes[i] = node;
		}

		this.nodes = nodes.ToArray();
		this.silhouettes = silhouettes.ToArray();
		this.vertices = polyhedron.vertices.Select(v => (Vector3)v).ToArray();
		
		isInitialized = true;
	}

	Plane[] FindPlanes() {
		var planes = new List<Plane>();

		for (int i = 0; i < polyhedron.edges.Length; i++) {
			var edge1 = polyhedron.edges[i];

			var pointA1 = polyhedron.vertices[edge1.start];
			var pointA2 = polyhedron.vertices[edge1.end];

			var normalA1 = polyhedron.planes[edge1.plane1].normal;
			var normalA2 = polyhedron.planes[edge1.plane2].normal;

			for (int j = i + 1; j < polyhedron.edges.Length; j++) {
				var edge2 = polyhedron.edges[j];

				var pointB1 = polyhedron.vertices[edge2.start];
				var pointB2 = polyhedron.vertices[edge2.end];

				var normalB1 = polyhedron.planes[edge2.plane1].normal;
				var normalB2 = polyhedron.planes[edge2.plane2].normal;

				if (CanVisiblyOverlap(pointA1, pointA2, pointB1, pointB2, normalA1, normalA2, normalB1, normalB2)) {
					InsertPlane(planes, pointA2, pointB1, pointB2);
					InsertPlane(planes, pointA1, pointB1, pointB2);
					InsertPlane(planes, pointA1, pointA2, pointB2);
					InsertPlane(planes, pointA1, pointA2, pointB1);
				}
			}
		}

		planes.AddRange(polyhedron.planes);

		return planes.ToArray();
	}

	void InsertPlane(List<Plane> list, Double3 a, Double3 b, Double3 c) {
		if (a == b || b == c || c == a) return;

		var plane = new Plane(a, b, c);

		for (int i = 0; i < list.Count; i++) {
			var other = list[i];

			if (Dot(plane.normal, other.normal) < 0) {
				other = other.flipped;
			}

			if (other == plane) return;
		}

		list.Add(plane);
	}

	int MakeLeaf(int index, BSP bsp, List<SilhouetteEdge> silhouettes, List<Node> nodes) {
		var position = bsp.leaves[index];
		
		MakeLeaf(position, silhouettes, nodes);

		return nodes.Count - 1;
	}

	void MakeLeaf(Double3 position, List<SilhouetteEdge> silhouettes, List<Node> nodes) {
		var silhouette = polyhedron.GetSilhouette(position);

		var left = silhouettes.Count;
		silhouettes.AddRange(silhouette);
		var right = silhouettes.Count;

		nodes.Add(new Node {
			left = -left,
			right = -right,
			plane = new Vector4((float)position.x, (float)position.y, (float)position.z, 0)
		});
	}

	// We only care about pairs of edges that can overlap when seen from a point where both are contour edges
	bool CanVisiblyOverlap(Double3 a1, Double3 a2, Double3 b1, Double3 b2, Double3 normalA1, Double3 normalA2, Double3 normalB1, Double3 normalB2) {
		if (IsContour(a1 - b1, normalA1, normalA2) && IsContour(a1 - b1, normalB1, normalB2)) return true;
		if (IsContour(a2 - b1, normalA1, normalA2) && IsContour(a2 - b1, normalB1, normalB2)) return true;
		if (IsContour(a1 - b2, normalA1, normalA2) && IsContour(a1 - b2, normalB1, normalB2)) return true;
		if (IsContour(a2 - b2, normalA1, normalA2) && IsContour(a2 - b2, normalB1, normalB2)) return true;

		return false;
	}

	bool IsContour(Double3 viewDirection, Double3 normal1, Double3 normal2) {
		var d1 = Dot(viewDirection, normal1);
		var d2 = Dot(viewDirection, normal2);

		return d1 > epsilon & d2 < -epsilon || d1 < -epsilon & d2 > epsilon;
	}

	public ComputeBuffer GetSilhouetteBuffer() {
		if (!isInitialized) Initialize();
		return Buffer(silhouettes);
	}

	public ComputeBuffer GetNodeBuffer() {
		if (!isInitialized) Initialize();
		return Buffer(nodes);
	}

	public ComputeBuffer GetVertexBuffer() {
		if (!isInitialized) Initialize();
		return Buffer(vertices);
	}

	[System.Serializable]
	public struct Node {
		public Vector4 plane;
		public int left;
		public int right;
	}
}