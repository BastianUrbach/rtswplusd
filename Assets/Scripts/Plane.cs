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

using static Double3;
using static System.Math;
using static Utility;

public struct Plane {
	public double x, y, z, d;
	public Double3 normal => new Double3(x, y, z);

	public Plane(double x, double y, double z, double d) {
		this.x = x;
		this.y = y;
		this.z = z;
		this.d = d;
	}

	public Plane(Double3 normal, double distance) {
		normal = Normalize(normal);
		x = normal.x;
		y = normal.y;
		z = normal.z;
		d = distance;
	}

	public Plane(Double3 a, Double3 b, Double3 c) {
		var normal = Normalize(Cross(b - a, c - a));
		x = normal.x;
		y = normal.y;
		z = normal.z;
		d = -Dot(normal, a);
	}

	public Plane(Double3 normal, Double3 point) {
		x = normal.x;
		y = normal.y;
		z = normal.z;
		d = -Dot(normal, point);
	}

	public Plane flipped => new Plane(-x, -y, -z, -d);
	public double GetDistanceToPoint(Double3 p) => x * p.x + y * p.y + z * p.z + d;
	public bool GetSide(Double3 p) => GetDistanceToPoint(p) > 0;
	public Double3 ClosestPointOnPlane(Double3 p) => p - normal * GetDistanceToPoint(p);

	public static bool operator ==(Plane a, Plane b) => a.Equals(b);
	public static bool operator !=(Plane a, Plane b) => !a.Equals(b);

	public static explicit operator UnityEngine.Vector4(Plane a) => new UnityEngine.Vector4((float)a.x, (float)a.y, (float)a.z, (float)a.d);

	public override bool Equals(object obj) {
		if (obj is Plane other) {
			if (Abs(other.x - this.x) > epsilon) return false;
			if (Abs(other.y - this.y) > epsilon) return false;
			if (Abs(other.z - this.z) > epsilon) return false;
			if (Abs(other.d - this.d) > epsilon) return false;

			return true;
		} else {
			return false;
		}
	}

	public override int GetHashCode() {
		int hashCode = -1371227472;
		hashCode = hashCode * -1521134295 + x.GetHashCode();
		hashCode = hashCode * -1521134295 + y.GetHashCode();
		hashCode = hashCode * -1521134295 + z.GetHashCode();
		hashCode = hashCode * -1521134295 + d.GetHashCode();
		return hashCode;
	}
}