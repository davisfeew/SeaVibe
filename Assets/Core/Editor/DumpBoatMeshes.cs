using UnityEngine;
using UnityEditor;

public class DumpBoatMeshes {
    [MenuItem("SeaVibe/Dump Boat Meshes")]
    public static void Dump() {
        GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Models/Boat/source/Boat A/Boat A.obj");
        if (boatPrefab == null) { Debug.Log("Boat prefab not found"); return; }
        GameObject boat = PrefabUtility.InstantiatePrefab(boatPrefab) as GameObject;
        foreach (Renderer r in boat.GetComponentsInChildren<Renderer>()) {
            Debug.Log($"Renderer: {r.gameObject.name}, Bounds: {r.bounds.size}, Type: {r.GetType().Name}");
        }
        GameObject.DestroyImmediate(boat);
    }
}
