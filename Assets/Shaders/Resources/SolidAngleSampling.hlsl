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

struct SphericalTriangle {
	float3 A, B, C;
	float3 angles;
	float solidAngle;
};

SphericalTriangle CreateSphericalTriangle(float3 A, float3 B, float3 C) {
	SphericalTriangle t;

	t.A = normalize(A);
	t.B = normalize(B);
	t.C = normalize(C);

	float dotAB = dot(t.A, t.B);
	float dotBC = dot(t.B, t.C);
	float dotCA = dot(t.C, t.A);

	float3 AonB = t.A - t.B * dotAB;
	float3 AonC = t.A - t.C * dotCA;
	float3 BonA = t.B - t.A * dotAB;
	float3 BonC = t.B - t.C * dotBC;
	float3 ConA = t.C - t.A * dotCA;
	float3 ConB = t.C - t.B * dotBC;

	float3 l = rsqrt(float3(
		dot(ConA, ConA) * dot(BonA, BonA),
		dot(AonB, AonB) * dot(ConB, ConB),
		dot(BonC, BonC) * dot(AonC, AonC)
	));

	t.angles = acos(float3(
		dot(ConA, BonA),
		dot(AonB, ConB),
		dot(BonC, AonC)
	) * l);

	t.solidAngle = t.angles.x + t.angles.y + t.angles.z - 3.14159265;

	return t;
}

SphericalTriangle CreateSphericalTriangle(float3 A, float3 B, float3 C, float solidAngle) {
	SphericalTriangle t;

	t.A = normalize(A);
	t.B = normalize(B);
	t.C = normalize(C);
	
	float3 BonA = t.B - t.A * dot(t.A, t.B);
	float3 ConA = t.C - t.A * dot(t.C, t.A);

	t.angles.x = acos(dot(ConA, BonA) * rsqrt(dot(ConA, ConA) * dot(BonA, BonA)));
	t.solidAngle = solidAngle;

	return t;
}

float SolidAngle(float3 A, float3 B, float3 C) {
	float a = length(A);
	float b = length(B);
	float c = length(C);

	float ABC = dot(A, cross(B, C));
	float abc = a * b * c;

	return 2 * atan(ABC / (abc + a * dot(B, C) + b * dot(C, A) + c * dot(A, B)));
}

float3 OrthonormalComponent(float3 x, float3 y) {
	return normalize(x - dot(x, y) * y);
}

// https://dl.acm.org/doi/10.1145/218380.218500
float3 SampleSphericalTriangle(float2 random, SphericalTriangle t) {
	// Use one random variable to select the new solidAngle
	float partialSolidAngle = t.solidAngle * random.x;

	// Save the sine and cosine of the angles Phi and Alpha
	float sinP, cosP; sincos(partialSolidAngle - t.angles.x, sinP, cosP);
	float sinA, cosA; sincos(t.angles.x, sinA, cosA);

	// Compute the pair (u, v) that determines beta2
	float u = cosP - cosA;
	float v = sinP + sinA * dot(t.A, t.B);

	// Let q be the cosine of the new edge length b2
	float q = ((v * cosP - u * sinP) * cosA - v) / ((v * sinP + u * cosP) * sinA);

	// Compute the third vertex of the sub-triangle
	float3 C2 = q * t.A + sqrt(1 - q * q) * OrthonormalComponent(t.C, t.A);

	// Use the other random variable to select cos(Theta)
	float z = 1 - random.y * (1 - dot(C2, t.B));

	// Construct the corresponding point on the sphere
	return z * t.B + sqrt(1 - z * z) * OrthonormalComponent(C2, t.B);
}