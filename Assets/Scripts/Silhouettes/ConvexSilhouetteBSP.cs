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
using static Utility;

[CreateAssetMenu]
public class ConvexSilhouetteBSP : ScriptableObject {
	public Mesh mesh;

	public Node[] nodes;
	public uint[] silhouettes;
	public Vector3[] vertices;
	public int root;
	public bool isInitialized = false;

	Polyhedron _polyhedron;
	public Polyhedron polyhedron => _polyhedron ??= new Polyhedron(mesh);

	[ContextMenu("Bake")]
	public void Initialize() {
		var bounds = new Bounds(Vector3.zero, Vector3.one * 100);
		var planes = polyhedron.planes;
		var bsp = new TwoPassBSP(planes, bounds);

		root = bsp.root;
		
		var nodes = new List<Node>();

		foreach (var node in bsp.nodes) {
			nodes.Add(new Node {
				plane = (Vector4)node.plane,
				left = node.left,
				right = node.right
			});
		}

		// Create silhouette list and add dummy element to prevent index 0 from being used because
		// leaves are recognized by the sign of the index
		var silhouettes = new List<uint>();
		silhouettes.Add(0);

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

		this.silhouettes = silhouettes.ToArray();
		this.nodes = nodes.ToArray();
		this.vertices = polyhedron.vertices.Select(v => (Vector3)v).ToArray();

		isInitialized = true;
	}

	int MakeLeaf(int index, TwoPassBSP bsp, List<uint> silhouettes, List<Node> nodes) {
		var silhouette = polyhedron.GetOrderedConvexSilhouette(bsp.leaves[index]);

		var left = silhouettes.Count;
		silhouettes.AddRange(silhouette);
		var right = silhouettes.Count;
		
		nodes.Add(new Node { left = -left, right = -right });

		return nodes.Count - 1;
	}

	public ComputeBuffer GetNodeBuffer() {
		if (!isInitialized) Initialize();
		return Buffer(nodes);
	}

	public ComputeBuffer GetSilhouetteBuffer() {
		if (!isInitialized) Initialize();
		return Buffer(silhouettes);
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