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

using static System.Math;
using static BSP;

// Binary space partitioning
//
// Partitions space into convex cells using the given planes and organizes these cells in a binary
// tree. Each cell is a leaf in the tree and stores one representative point.
//
// This version builds two trees: one to find the cells and one for the structure of the tree. When
// building the second tree, the resulting cells are already known. This is used to build the second
// tree in a way that results in a lower average depth.
public class TwoPassBSP {
	public List<Node> nodes = new List<Node>();
	public List<Double3> leaves;

	FastList<Plane> planes = new FastList<Plane>(256);
	FastList<(int, Double3)> points = new FastList<(int, Double3)>(256);

	public int root;
	public float averageDepth;
	public int leafCount = 0, depth = 0;

	BSP firstPass;

	public TwoPassBSP(IEnumerable<Plane> planes, UnityEngine.Bounds bounds) {
		firstPass = new BSP(planes, bounds);
		leaves = new List<Double3>();

		// dummy to make sure no element 0 exists so that leaf indices are always negative and node
		// indices always positive
		leaves.Add(Double3.zero);

		foreach (var plane in planes) {
			this.planes.Add(plane);
		}

		for (int i = 1; i < firstPass.leaves.Count; i++) {
			points.Add((i, firstPass.leaves[i]));
		}

		root = MakeNode((0, this.planes.count), (0, points.count));

		averageDepth /= leafCount;
		firstPass = null;
	}

	int MakeNode((int, int) planeSpan, (int, int) pointSpan) {
		var (planeStart, planeEnd) = planeSpan;
		var (pointStart, pointEnd) = pointSpan;
		var bestRating = 0;
		var bestIndex = -1;

		for (int i = planeStart; i < planeEnd; i++) {
			var plane = planes[i];
			var rating = EvaluateCut(plane, pointSpan);

			if (rating > bestRating) {
				bestRating = rating;
				bestIndex = planes.count;
			}

			if (rating != 0) {
				planes.Add(plane);
			}
		}

		if (bestIndex < 0) {
			return MakeLeaf(pointStart, pointEnd);
		}

		var bestPlane = planes[bestIndex];
		planes.RemoveAt(bestIndex);

		return MakeInnerNode((planeEnd, planes.count), pointSpan, bestPlane);
	}

	int EvaluateCut(Plane plane, (int, int) pointSpan) {
		var (pointStart, pointEnd) = pointSpan;

		int count1 = 0, count2 = 0;

		for (int i = pointStart; i < pointEnd; i++) {
			var (index, point) = points[i];
			var distance = plane.GetDistanceToPoint(point);

			count1 += distance > 0 ? 1 : 0;
			count2 += distance < 0 ? 1 : 0;
		}

		return Min(count1, count2);
	}

	int MakeInnerNode((int, int) planeSpan, (int, int) pointSpan, Plane plane) {
		depth++;

		var (planeStart, planeEnd) = planeSpan;
		var (pointStart, pointEnd) = pointSpan;

		var node = new Node { plane = plane };
		var nodeIndex = nodes.Count;
		nodes.Add(node);

		node.left = MakeNode(planeSpan, Split(plane, pointSpan));
		node.right = MakeNode(planeSpan, Split(plane.flipped, pointSpan));

		points.count = pointStart;
		planes.count = planeStart;

		nodes[nodeIndex] = node;

		depth--;

		return nodeIndex;
	}

	int MakeLeaf(int pointStart, int pointEnd) {
		Double3 position = Double3.zero;

		for (int i = pointStart; i < pointEnd; i++) {
			position += points[i].Item2;
		}

		position /= pointEnd - pointStart;

		leaves.Add(position);

		leafCount++;
		averageDepth += depth;

		return -(leaves.Count - 1);
	}

	(int, int) Split(Plane plane, (int, int) pointSpan) {
		var (pointStart, pointEnd) = pointSpan;
		var newPointStart = points.count;

		for (int i = pointStart; i < pointEnd; i++) {
			var (index, point) = points[i];
			var distance = plane.GetDistanceToPoint(point);

			if (distance > 0) points.Add((index, point));
		}

		return (newPointStart, points.count);
	}
}