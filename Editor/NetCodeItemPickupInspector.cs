using GreedyVox.NetCode.Data;
using GreedyVox.NetCode.Objects;
using GreedyVox.NetCode.Traits;
using GreedyVox.NetCode.Utilities;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Opsive.UltimateCharacterController.Traits;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace GreedyVox.NetCode.Editors
{
    /// <summary>
    /// Custom Unity Editor window for configuring multiple Pickup Item network items.
    /// Allows drag-and-drop of multiple GameObject prefabs or scene objects into a single drop area,
    /// then applies network-related components and settings to all selected items at once.
    /// Features a scrollable list to display dropped items and a button to process them.
    /// </summary>
    public class NetCodeItemPickupInspector : EditorWindow
    {
        // Opens the editor window centered on the screen with a fixed size
        [MenuItem("Tools/GreedyVox/NetCode/Items/Pickup Item Inspector")]
        private static NetCodeItemPickupInspector Init() =>
        EditorWindow.GetWindowWithRect<NetCodeItemPickupInspector>(
            new Rect(Screen.width - 400 / 2, Screen.height - 200 / 2, 400, 400), true, "Network Pickup Item");
        private Object[] m_NetworkItem; // Array to store multiple dragged GameObjects
        private LayerMask m_LayerMask; // Default layer mask (will be set to VisualEffect in OnEnable)
        private Vector2 m_ScrollPosition; // Scroll position for the list of dropped items
        private const string IconErrorPath = "d_console.erroricon.sml"; // Path to error icon for notifications
        private const string IconIfoPath = "d_console.infoicon.sml"; // Path to info icon for notifications
        // Ensure m_NetworkItem is initialized as an empty array to avoid null reference issues
        private void OnEnable()
        {
            m_NetworkItem ??= new Object[0];
            // Set default layer mask to "VisualEffect" if it exists
            int visualEffectLayer = LayerMask.NameToLayer("VisualEffect");
            if (visualEffectLayer != -1)
                m_LayerMask = visualEffectLayer;
        }
        private void OnGUI()
        {
            // Display a header box for the Pickup Item prefabs section
            GUILayout.Box("Pickup Item PREFABS", GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical();
            // Allow selection of a layer mask for all processed objects
            m_LayerMask = EditorGUILayout.LayerField("Layer Mask", m_LayerMask);
            // Define a rectangular drop area for dragging multiple objects
            Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag and Drop Items Here");
            HandleDragAndDrop(dropArea);
            // Scrollable list to display all dropped items (read-only)
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(100));
            if (m_NetworkItem.Length > 0)
            {
                foreach (var item in m_NetworkItem)
                    if (item != null)
                        EditorGUILayout.ObjectField(item, typeof(GameObject), true, GUILayout.ExpandWidth(true));
            }
            else EditorGUILayout.LabelField("No items dropped yet.");
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            // Button to process all dropped items
            if (GUILayout.Button("Update Pickup Item"))
            {
                // Check if array is empty or all items are null
                if (m_NetworkItem == null || m_NetworkItem.Length == 0 || System.Array.TrueForAll(m_NetworkItem, item => item == null))
                {
                    ShowNotification(new GUIContent("No objects selected for updating",
                        EditorGUIUtility.IconContent(IconErrorPath).image), 15);
                }
                else
                {
                    // Process each non-null item in the array
                    foreach (var item in m_NetworkItem)
                        if (item != null)
                            SetupItem((GameObject)item);
                    ShowNotification(new GUIContent("Finished updating building items",
                        EditorGUIUtility.IconContent(IconIfoPath).image), 15);
                }
            }
            EditorGUILayout.Space();
        }
        /// <summary>
        /// Handles drag-and-drop events to accept multiple objects dropped into the drop area.
        /// Updates the m_NetworkItem array with all dragged objects.
        /// </summary>
        /// <param name="dropArea">The rectangular area where objects can be dropped.</param>
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    // Only process if the mouse is within the drop area
                    if (!dropArea.Contains(currentEvent.mousePosition))
                        break;
                    // Show a copy cursor to indicate objects will be added
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (currentEvent.type == EventType.DragPerform)
                    {
                        // Accept the drag and update the array with all dragged objects
                        DragAndDrop.AcceptDrag();
                        m_NetworkItem = DragAndDrop.objectReferences;
                        currentEvent.Use(); // Consume the event to prevent further processing
                    }
                    break;
            }
        }
        /// <summary>
        /// Configures a GameObject for network functionality by adding required components and settings.
        /// Applies to Pickup Item prefabs, setting layers and network properties.
        /// </summary>
        /// <param name="go">The GameObject to configure.</param>
        private void SetupItem(GameObject go)
        {
            if (go == null) return;
            // Recursively set the layer for the object and its children
            SetLayerRecursively(go, m_LayerMask.value);
            // Add NetworkObject component with specific settings
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
            // Add custom NetCode components
            ComponentUtility.TryAddComponent<NetCodeInfo>(go);
            ComponentUtility.TryAddComponent<NetCodeEvent>(go);
            ComponentUtility.TryAddComponent<NetworkRigidbody>(go);
            ComponentUtility.TryAddComponent<NetworkTransform>(go);
            // Add network monitors if AttributeManager or Health components are present
            if (ComponentUtility.HasComponent<AttributeManager>(go))
                ComponentUtility.TryAddComponent<NetCodeAttributeMonitor>(go);
            ComponentUtility.TryAddComponent<PayloadItemPickupData>(go);
            if (!ComponentUtility.TryReplaceCopy<ItemPickup, NetCodeItemPickup>(go))
                Debug.LogWarning($"Failed to replace ItemPickup with NetCodeItemPickup on {go.name}");
            if (ComponentUtility.TryAddGetComponent(go, out Health from)
            && ComponentUtility.TryAddComponent(go, out NetCodeHealthMonitor to)
            && !ComponentUtility.TryCopyNetworkedSpawnedObjects(from, to))
                Debug.LogError($"Error copying networked spawned objects from {from} to {to}. " +
                "Ensure that the Health component is properly set up with the NetCodeHealthMonitor component.");
        }
        /// <summary>
        /// Recursively sets the layer for a GameObject and all its children.
        /// </summary>
        /// <param name="go">The GameObject to set the layer for.</param>
        /// <param name="layer">The layer index to apply.</param>
        private void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
                if (child != null)
                    SetLayerRecursively(child.gameObject, layer);
        }
    }
}