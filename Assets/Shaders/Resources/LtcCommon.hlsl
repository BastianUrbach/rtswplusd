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

// Functions and data structures for dealing with Linearly Transformed Cosines as presented in
// https://doi.org/10.1145/2897824.2925895

struct LTC {
	float3x3 M_inv;
	float integral;
	
	// When a silhouette is clipped to the upper hemisphere, new edges may be created on the
	// clipping plane. These edges are all segments of the same circle and their integral is the sum
	// of their angles (see IntegrateEdge(a, b)). Instead of calculating the angles individually the
	// atan2 angle sum and difference identities are used (see Atan2Sum(a, b) and Atan2Diff(a, b))
	float2 clippingSum;

	// For ordered silhouette traversal we remember the last transformed point so that
	// we don't have to calculate it again for the next edge
	float3 previousPoint;

	// Determinant
	float det;
};

LTC NewLTC(float3x3 M_inv, float3x3 tSpace) {
	LTC r;

	r.M_inv = mul(M_inv, tSpace);
	r.integral = 0;
	r.clippingSum = float2(1, 0);
	r.previousPoint = 0;
	r.det = determinant(M_inv);

	return r;
}

// Calculate the contribution of an edge given the already transformed end points of the edge
// Slower version but easier to read:
float IntegrateEdge_Slow(float3 a, float3 b) {
	a = normalize(a);
	b = normalize(b);
	float AdotB = clamp(dot(a, b), -1, 1);
	return acos(AdotB) * normalize(cross(a, b)).z;
}

// Slightly faster version that's actually used:
float IntegrateEdge(float3 a, float3 b) {
	float3 c = cross(a, b);
	float2 n = rsqrt(float2(dot(a, a) * dot(b, b), dot(c, c)));
	float AdotB = clamp(dot(a, b) * n.x, -1, 1);
	return acos(AdotB) * c.z * n.y;
}

// Returns a vector c such that atan2(a.y, a.x) + atan2(b.y, b.x) = atan2(c.y, c.x)
float2 Atan2Sum(float2 a, float2 b) {
	float4 m = a.xxyy * b.xyxy;
	return m.xz + float2(-m.w, m.y);

	// Readable version:
	// return float2(a.x * b.x - a.y * b.y, a.y * b.x + a.x * b.y);
}

// Returns a vector c such that atan2(a.y, a.x) - atan2(b.y, b.x) = atan2(c.y, c.x)
float2 Atan2Diff(float2 a, float2 b) {
	float4 m = a.xxyy * b.xyxy;
	return m.xz + float2(m.w, -m.y);

	// Readable version:
	// return float2(a.x * b.x + a.y * b.y, a.y * b.x - a.x * b.y);
}

// Construct a matrix that transforms to a coordinate system where z is the normal and x is the
// tangent that's closest to the view direction
float3x3 BuildShadingSpace(float3 normal, float3 view) {
	float3 t2 = normal;
	float3 t1 = normalize(cross(t2, view));
	float3 t0 = cross(t1, t2);
	return float3x3(t0, t1, t2);
}

float3 EquatorIntersection(float3 a, float3 b) {
	return normalize(lerp(a, b, a.z / (a.z - b.z)));
}

// Unordered silhouette traversal, consecutive edges don't necessarily share a point
void SilhouetteEdge(inout LTC ltc, float3 a, float3 b) {
	// Transform
	a = mul(ltc.M_inv, a);
	b = mul(ltc.M_inv, b);

	// Clip to upper hemisphere
	bool aBelowEquator = a.z < 0;
	bool bBelowEquator = b.z < 0;
	bool bothBelowEquator = aBelowEquator & bBelowEquator;

	if (aBelowEquator != bBelowEquator) {
		float3 p = EquatorIntersection(a, b);

		if (aBelowEquator) {
			a = p;
			ltc.clippingSum = Atan2Sum(ltc.clippingSum, p.xy);
		} else {
			b = p;
			ltc.clippingSum = Atan2Diff(ltc.clippingSum, p.xy);
		}
	}

	// Integrate
	if (!bothBelowEquator) {
		ltc.integral += IntegrateEdge(a, b);
	}
}

// Ordered silhouette traversal, consecutive edges always share a point
void SilhouetteEdge(inout LTC ltc, float3 b) {
	// The first point of this edge is the second point of the previous edge
	float3 a = ltc.previousPoint;

	// Transform
	b = mul(ltc.M_inv, b);

	// Remember b for next edge
	ltc.previousPoint = b;

	// Clip to upper hemisphere
	bool aBelowEquator = a.z < 0;
	bool bBelowEquator = b.z < 0;
	bool bothBelowEquator = aBelowEquator & bBelowEquator;

	if (aBelowEquator != bBelowEquator) {
		float3 p = EquatorIntersection(a, b);

		if (aBelowEquator) {
			a = p;
			ltc.clippingSum = Atan2Sum(ltc.clippingSum, p.xy);
		} else {
			b = p;
			ltc.clippingSum = Atan2Diff(ltc.clippingSum, p.xy);
		}
	}

	// Integrate
	if (!bothBelowEquator) {
		ltc.integral += IntegrateEdge(a, b);
	}
}

// Set the vertex to be used as the first vertex of the first edge in ordered silhouette traversal
void SetFirstVertex(inout LTC ltc, float3 vertex) {
	ltc.previousPoint = mul(ltc.M_inv, vertex);
}

// Get integral (including edges created by clipping)
float GetIntegral(LTC ltc) {
	float angle = atan2(ltc.clippingSum.y, ltc.clippingSum.x);
	return (ltc.integral + (angle + 6.28318531) % 6.28318531) / 6.28318531;
}

float Evaluate(LTC ltc, float3 direction) {
	float3 d = mul(ltc.M_inv, direction);
	float l = dot(d, d);

	return max(0, d.z * ltc.det / (l * l));
}