using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
internal static class ApplyExactTeacherMapOnce
{
    private const string ReportDirectory = "C:/Users/qaz15/Documents/Codex/2026-10-19/using-unityengine-public-class-camararotate-x20/work/exact-teacher-map-5114ea4/";

    static ApplyExactTeacherMapOnce()
    {
        EditorApplication.update += Apply;
    }

    private static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(ReportDirectory + "result.txt"))
        {
            EditorApplication.update -= Apply;
            return;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Editor/TeacherMapSource.prefab");
        if (source == null) return;
        EditorApplication.update -= Apply;
        int undoGroup = -1;
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/01.Scenes/GameScenes.unity") throw new InvalidOperationException("Expected GameScenes.unity to remain open.");
            GameObject oldMap = scene.GetRootGameObjects().Single(root => root.name.Equals("map", StringComparison.OrdinalIgnoreCase));
            foreach (Transform item in source.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0) throw new InvalidOperationException("Missing source component: " + item.name);
            }

            string[] protectedRoots = scene.GetRootGameObjects().Where(root => root != oldMap).Select(root => root.GetInstanceID().ToString()).OrderBy(id => id).ToArray();
            PrefabUtility.SaveAsPrefabAsset(oldMap, "Assets/Editor/PreviousUserMap.prefab");
            File.Copy("Assets/Editor/PreviousUserMap.prefab", ReportDirectory + "PreviousUserMap.prefab", true);

            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace map with teacher map 5114ea4");
            GameObject map = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            Undo.RegisterCreatedObjectUndo(map, "Create exact teacher map");
            PrefabUtility.UnpackPrefabInstance(map, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            map.name = "Map";
            map.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            map.transform.localScale = Vector3.one;
            Undo.DestroyObjectImmediate(oldMap);

            GameObject zombieRoot = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Monster/Zombie.prefab");
            try
            {
                NavMeshAgent agent = zombieRoot.GetComponent<NavMeshAgent>();
                agent.agentTypeID = 0;
                PrefabUtility.SaveAsPrefabAsset(zombieRoot, "Assets/03.Prefabs/Monster/Zombie.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(zombieRoot);
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Zombie zombie in root.GetComponentsInChildren<Zombie>(true))
                {
                    NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
                    Undo.RecordObject(agent, "Match teacher navigation agent type");
                    agent.agentTypeID = 0;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
                }
            }

            NavMeshSurface surface = map.GetComponent<NavMeshSurface>();
            NavMesh.SetAreaCost(3, 4f);
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Map bake failed.");
            const string dataPath = "Assets/01.Scenes/GameScenes/NavMesh-map.asset";
            NavMeshData previousData = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);
            if (previousData == null)
            {
                surface.navMeshData.name = "NavMesh-map";
                AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
            }
            else
            {
                NavMeshData baked = surface.navMeshData;
                surface.RemoveData();
                EditorUtility.CopySerialized(baked, previousData);
                previousData.name = "NavMesh-map";
                surface.navMeshData = previousData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(baked);
                EditorUtility.SetDirty(previousData);
            }

            var report = new StringBuilder();
            int checks = 0;
            foreach (Transform expected in source.GetComponentsInChildren<Transform>(true))
            {
                Transform actual = expected == source.transform ? map.transform : map.transform.Find(expected.name);
                if (actual == null || Vector3.Distance(actual.localPosition, expected.localPosition) > 0.00001f || Vector3.Distance(actual.localScale, expected.localScale) > 0.00001f || Quaternion.Angle(actual.localRotation, expected.localRotation) > 0.001f) throw new InvalidOperationException("Teacher transform mismatch: " + expected.name);
                string expectedTypes = string.Join(",", expected.GetComponents<Component>().Select(component => component.GetType().FullName).OrderBy(name => name));
                string actualTypes = string.Join(",", actual.GetComponents<Component>().Select(component => component.GetType().FullName).OrderBy(name => name));
                if (expectedTypes != actualTypes) throw new InvalidOperationException("Teacher component mismatch: " + expected.name);
                report.AppendLine("PASS " + expected.name + ": Transform and component types match");
                checks++;
            }

            if (map.transform.childCount != 15) throw new InvalidOperationException("Expected 15 exact teacher map children.");
            string[] afterRoots = scene.GetRootGameObjects().Where(root => root != map).Select(root => root.GetInstanceID().ToString()).OrderBy(id => id).ToArray();
            if (!protectedRoots.SequenceEqual(afterRoots)) throw new InvalidOperationException("Unrelated scene root changed.");
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            int mudTriangles = triangulation.areas.Count(area => area == 3);
            if (mudTriangles == 0) throw new InvalidOperationException("Mud triangles missing after bake.");
            if (map.transform.Find("Bridge (1)").GetComponent<NavMeshModifier>().area != 3) throw new InvalidOperationException("Mud bridge setting mismatch.");
            if (NavMesh.GetAreaFromName("Mud") != 3) throw new InvalidOperationException("Mud area name was not loaded.");

            EditorUtility.SetDirty(surface);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
            AssetDatabase.SaveAssets();
            report.AppendLine($"APPLIED: {checks} objects; 15 map children; Mud triangles={mudTriangles}; agentType={surface.agentTypeID}; cost={NavMesh.GetAreaCost(3)}");
            report.AppendLine("Unrelated roots preserved; previous user map and scene backed up; scene saved without reopening.");
            File.WriteAllText(ReportDirectory + "result.txt", report.ToString());
        }
        catch (Exception exception)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            File.WriteAllText(ReportDirectory + "result.txt", "ERROR: " + exception);
        }
    }
}
