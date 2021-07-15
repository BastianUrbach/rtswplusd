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

struct Node {
	float4 plane;
	int left;
	int right;
};

float4x4 _LightToWorld;
float4x4 _WorldToLight;
uint _Root;
float3 _Center;

StructuredBuffer<Node> _Nodes;
StructuredBuffer<float3> _Vertices;
StructuredBuffer<uint> _SilhouetteVertices;
StructuredBuffer<uint2> _SilhouetteEdges;
StructuredBuffer<uint3> _SilhouetteTriangles;
StructuredBuffer<Node> _TriangulationNodes;

bool IsLeaf(Node node) {
	return node.left < 0;
}

bool IsInnerNode(Node node) {
	return node.left > 0;
}

bool GetSide(Node node, float3 position) {
	return dot(node.plane, float4(position, 1)) > 0;
}

float3 WorldToLightPos(float3 worldPosition) {
	float4 localPosition = mul(_WorldToLight, float4(worldPosition, 1));
	return localPosition.xyz / localPosition.w;
}

float3 LightToWorldPos(float3 localPosition) {
	float4 worldPosition = mul(_LightToWorld, float4(localPosition, 1));
	return worldPosition.xyz / worldPosition.w;
}

// Apparent intersection of edges a and b as seen from p
float3 GetApparentIntersection(float3 a1, float3 a2, float3 b1, float3 b2, float3 p) {
	float3 normal = cross(b1 - p, b2 - p);
	float t = dot(p - a1, normal) / dot(a2 - a1, normal);
	return lerp(a1, a2, t);
}

float3 ComputeVertexPositionConvex(uint encoded) {
	float4 vertex = float4(_Vertices[encoded], 1);

	vertex = mul(_LightToWorld, vertex);
	return vertex.xyz / vertex.w;
}

float3 ComputeVertexPositionNonConvex(uint encoded, float3 p) {
	uint4 indices = (encoded >> uint4(0, 8, 16, 24)) & 0xff;
	float4 vertex = 1;

	if (indices.x == indices.y) {
		// Vertex is a vertex of the light source
		vertex.xyz = _Vertices[indices.x];
	} else {
		// Vertex is an intersection of two polyhedron edges
		float3 a1 = _Vertices[indices.x];
		float3 a2 = _Vertices[indices.y];
		float3 b1 = _Vertices[indices.z];
		float3 b2 = _Vertices[indices.w];

		vertex.xyz = GetApparentIntersection(a1, a2, b1, b2, p);
	}

	vertex = mul(_LightToWorld, vertex);
	return vertex.xyz / vertex.w;
}

uint2 FindSilhouette(float3 localPosition) {
	Node node = _Nodes[_Root];

	while (IsInnerNode(node)) {
		bool side = GetSide(node, localPosition);
		node = _Nodes[side ? node.left : node.right];
	}

	return uint2(-node.left, -node.right);
}

uint2 FindTriangulation(float3 localPosition) {
	Node node = _TriangulationNodes[_Root];

	while (IsInnerNode(node)) {
		bool side = GetSide(node, localPosition);
		node = _TriangulationNodes[side ? node.left : node.right];
	}

	return uint2(-node.left, -node.right);
}

void GetTriangleConvex(uint index, uint2 range, out float3 A, out float3 B, out float3 C) {
	uint count = range.y - range.x;
	uint i1 = range.x + index;
	uint i2 = range.x + (index - 1 + count) % count;

	uint encodedB = _SilhouetteVertices[i1];
	uint encodedC = _SilhouetteVertices[i2];

	A = _Center;
	B = ComputeVertexPositionConvex(encodedB);
	C = ComputeVertexPositionConvex(encodedC);
}

void GetTriangleStarShaped(uint index, uint2 range, float3 localPosition, out float3 A, out float3 B, out float3 C) {
	uint2 edge = _SilhouetteEdges[index + range.x];

	A = _Center;
	B = ComputeVertexPositionNonConvex(edge.x, localPosition);
	C = ComputeVertexPositionNonConvex(edge.y, localPosition);
}

void GetTriangleNonStarShaped(uint index, uint2 range, float3 localPosition, out float3 A, out float3 B, out float3 C) {
	uint3 tri = _SilhouetteTriangles[index + range.x];

	A = ComputeVertexPositionNonConvex(tri.x, localPosition);
	B = ComputeVertexPositionNonConvex(tri.y, localPosition);
	C = ComputeVertexPositionNonConvex(tri.z, localPosition);
}