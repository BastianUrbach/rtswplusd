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

using static System.Math;
using static Utility;

public struct Double3 {
	public double x, y, z;

	public Double3(double x, double y, double z) {
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public double sqrMagnitude => x * x + y * y + z * z;
	public double magnitude => Sqrt(sqrMagnitude);
	public Double3 normalized => Normalize(this);

	public override bool Equals(object obj) {
		if (obj is Double3 other) {
			if (Abs(other.x - this.x) > epsilon) return false;
			if (Abs(other.y - this.y) > epsilon) return false;
			if (Abs(other.z - this.z) > epsilon) return false;

			return true;
		} else {
			return false;
		}
	}

	public static Double3 operator +(Double3 a, Double3 b) {
		return new Double3(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static Double3 operator -(Double3 a, Double3 b) {
		return new Double3(a.x - b.x, a.y - b.y, a.z - b.z);
	}

	public static Double3 operator *(Double3 a, Double3 b) {
		return new Double3(a.x * b.x, a.y * b.y, a.z * b.z);
	}

	public static Double3 operator /(Double3 a, Double3 b) {
		return new Double3(a.x / b.x, a.y / b.y, a.z / b.z);
	}

	public static bool operator ==(Double3 a, Double3 b) => a.Equals(b);
	public static bool operator !=(Double3 a, Double3 b) => !a.Equals(b);
	public static Double3 operator -(Double3 a) => new Double3(-a.x, -a.y, -a.z);

	public static implicit operator Double3(double d) {
		return new Double3(d, d, d);
	}

	public static implicit operator Double3(UnityEngine.Vector3 v) {
		return new Double3(v.x, v.y, v.z);
	}

	public static explicit operator UnityEngine.Vector3(Double3 v) {
		return new UnityEngine.Vector3((float)v.x, (float)v.y, (float)v.z);
	}

	public static implicit operator Double3((float x, float y, float z) v) {
		return new Double3(v.x, v.y, v.z);
	}

	public static double Dot(Double3 a, Double3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	public static Double3 Cross(Double3 a, Double3 b) {
		return new Double3(
			a.y * b.z - a.z * b.y,
			a.z * b.x - a.x * b.z,
			a.x * b.y - a.y * b.x
		);
	}

	public static double SqrDistance(Double3 a, Double3 b) {
		return (b - a).sqrMagnitude;
	}

	public static double Distance(Double3 a, Double3 b) {
		return (b - a).magnitude;
	}

	public static double Angle(Double3 a, Double3 b) {
		var denominator = Sqrt(Dot(a, a) * Dot(b, b));
		if (denominator < 0.000001) return 0;

		var dot = Dot(a, b) / denominator;
		dot = dot < -1 ? -1 : dot > 1 ? 1 : dot; // clamp

		return Acos(dot);
	}

	public static double SignedAngle(Double3 from, Double3 to, Double3 axis) {
		var sign = Dot(Cross(from, to), axis) > 0 ? 1 : -1;
		return sign * Angle(Cross(axis, from), Cross(axis, to));
	}

	public static Double3 Normalize(Double3 a) => a / a.magnitude;

	public static Double3 Lerp(Double3 a, Double3 b, double t) {
		t = t < 0 ? 0 : t > 1 ? 1 : t;
		return a + (b - a) * t;
	}

	public override int GetHashCode() {
		int hashCode = -1255691586;
		hashCode = hashCode * -1521134295 + x.GetHashCode();
		hashCode = hashCode * -1521134295 + y.GetHashCode();
		hashCode = hashCode * -1521134295 + z.GetHashCode();
		return hashCode;
	}

	public static readonly Double3 zero = new Double3(0, 0, 0);
	public static readonly Double3 left = new Double3(-1, 0, 0);
	public static readonly Double3 right = new Double3(+1, 0, 0);
	public static readonly Double3 down = new Double3(0, -1, 0);
	public static readonly Double3 up = new Double3(0, +1, 0);
	public static readonly Double3 back = new Double3(0, 0, -1);
	public static readonly Double3 forward = new Double3(0, 0, +1);
}