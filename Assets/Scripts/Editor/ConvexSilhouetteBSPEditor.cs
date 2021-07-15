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

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConvexSilhouetteBSP))]
public class ConvexSilhouetteBSPEditor : Editor {
	static string infoText = $"A {nameof(ConvexSilhouetteBSP)} stores precomputed data that is required for {nameof(ConvexLTCLight)} and {nameof(ConvexRaytracedLight)}. Note that this method only works well for relatively simple convex polyhedra. Precomputation time and size of precomputed data grow rapidly with the complexity of the polyhedron.";

    public override void OnInspectorGUI() {
		GUILayout.Label(infoText, EditorStyles.wordWrappedLabel);
		GUILayout.Space(EditorGUIUtility.singleLineHeight);

		serializedObject.Update();

		var meshProperty = serializedObject.FindProperty("mesh");
		EditorGUILayout.PropertyField(meshProperty);

		serializedObject.ApplyModifiedProperties();

		if (GUILayout.Button("Bake")) {
			var target = this.target as ConvexSilhouetteBSP;
			target.isInitialized = false;
			target.Initialize();
			serializedObject.Update();
		}

		var isInitializedProperty = serializedObject.FindProperty("isInitialized");

		if (isInitializedProperty.boolValue) {
			EditorGUILayout.HelpBox("Precomputation has been completed", MessageType.Info);
		} else {
			EditorGUILayout.HelpBox("Precomputation is required. Press \"Bake\" to start the precomputation phase. This operation may take a while.", MessageType.Warning);
		}
	}
}