using System;
using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Objects;
using GreedyVox.NetCode.Traits;
using GreedyVox.NetCode.Utilities;
using Opsive.UltimateCharacterController.Objects;
using Opsive.UltimateCharacterController.Traits;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GreedyVox.NetCode.Editors
{
    public class NetCodeItemGrenadeInspector : EditorWindow
    {
        [MenuItem("Tools/GreedyVox/NetCode/Items/Grenade Inspector")]
        private static NetCodeItemGrenadeInspector Init() =>
        GetWindowWithRect<NetCodeItemGrenadeInspector>(
        new Rect(Screen.width - 400 / 2, Screen.height - 100 / 2, 300, 600), true, "Network Grenade");
        private GameObject m_NetworkItem;
        private const string IconErrorPath = "d_console.erroricon.sml";
        private const string IconIfoPath = "d_console.infoicon.sml";
        private void OnGUI()
        {
            GUILayout.Box("GRENADE PREFAB", GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            m_NetworkItem = (GameObject)EditorGUILayout.ObjectField(m_NetworkItem, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Update Grenade"))
            {
                if (m_NetworkItem == null)
                {
                    ShowNotification(new GUIContent("No object selected for updating",
                                         EditorGUIUtility.IconContent(IconErrorPath).image), 15);
                }
                else if (SetupItem(m_NetworkItem))
                {
                    ShowNotification(new GUIContent("Finished updating grenade item",
                                         EditorGUIUtility.IconContent(IconIfoPath).image), 15);
                }
                else
                {
                    ShowNotification(new GUIContent("Could not update the grenade. Select a GameObject with a single grenade to convert.",
                                         EditorGUIUtility.IconContent(IconErrorPath).image), 15);
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("The upgrade turns off Initialize On Enable so the network spawn can initialize the grenade.", MessageType.Info);
        }
        /// <summary>
        /// Converts or repairs a grenade and saves the network setup on prefab assets.
        /// </summary>
        private bool SetupItem(GameObject go)
        {
            if (go == null) return false;

            var sources = Array.FindAll(go.GetComponents<Grenade>(), grenade => grenade is not NetCodeGrenade);
            var networkGrenade = go.GetComponent<NetCodeGrenade>();
            if (sources.Length > 1 || (sources.Length == 0 && networkGrenade == null)) return false;

            if (sources.Length == 1)
            {
                var source = sources[0];
                if (!ComponentUtility.TryAddGetComponent(go, out networkGrenade) ||
                    !ComponentUtility.TryCopyValues<Grenade>(source, networkGrenade))
                    return false;

                ComponentUtility.TryRemoveComponent(source);
                if (source != null) return false;
            }

            using (var settings = new SerializedObject(networkGrenade))
            {
                settings.FindProperty("m_InitializeOnEnable").boolValue = false;
                settings.ApplyModifiedProperties();
            }

            if (ComponentUtility.TryAddComponent<NetworkObject>(go, out var net))
            {
                net.SpawnWithObservers = true;
                net.SynchronizeTransform = true;
                net.AlwaysReplicateAsRoot = false;
                net.ActiveSceneSynchronization = false;
                net.SceneMigrationSynchronization = false;
                net.DontDestroyWithOwner = false;
                net.AutoObjectParentSync = false;
            }
            ComponentUtility.TryAddComponent<NetworkRigidbody>(go);
            ComponentUtility.TryAddComponent<NetworkTransform>(go);
            ComponentUtility.TryAddComponent<NetCodeInfo>(go);
            ComponentUtility.TryAddComponent<NetCodeDestructibleMonitor>(go);
            // The payload requires NetCodeGrenade, so add it after converting the original.
            if (!ComponentUtility.TryAddGetComponent<PayloadGrenadoData>(go, out _)) return false;
            if (ComponentUtility.HasComponent<AttributeManager>(go))
            {
                ComponentUtility.TryAddComponent<NetCodeAttributeMonitor>(go);
                if (ComponentUtility.TryAddGetComponent(go, out Health from)
                && ComponentUtility.TryAddComponent(go, out NetCodeHealthMonitor to)
                && !ComponentUtility.TryCopyNetworkedSpawnedObjects(from, to))
                    Debug.LogError($"Error copying networked spawned objects from {from} to {to}. " +
                    "Ensure that the Health component is properly set up with the NetCodeHealthMonitor component.");
            }

            if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                PrefabUtility.SavePrefabAsset(go, out var saved);
                return saved;
            }

            return EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
