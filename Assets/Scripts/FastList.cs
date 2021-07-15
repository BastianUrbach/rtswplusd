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

// A simple array list that can start large and never shrinks to avoid unnecessary allocations
public struct FastList<T> {
	public T[] data;
	public int count;
	int length;

	public FastList(int startLength) {
		data = new T[startLength];
		length = startLength;
		count = 0;
	}

	public void Add(T item) {
		if (count == length) {
			var newData = new T[length * 2];
			System.Array.Copy(data, newData, length);
			data = newData;
			length *= 2;
		}

		data[count++] = item;
	}

	public void RemoveAt(int i) {
		data[i] = data[--count];
	}

	public T this[int i] {
		get => data[i];
		set => data[i] = value;
	}
}