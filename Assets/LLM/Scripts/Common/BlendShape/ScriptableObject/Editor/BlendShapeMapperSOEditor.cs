using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(BlendShapeMapperSO))]
public class BlendShapeMapperSOEditor : Editor
{
    private BlendShapeMapperSO _mapperSO;
    private SkinnedMeshRenderer _skinnedMeshRenderer; // Added field
    private BlendShapeMapperSO.FacialAnimationMapping _selectedMapping;
    private string _currentEditingCategory = null; // Current category being edited

    private void OnEnable()
    {
        _mapperSO = (BlendShapeMapperSO)target;
    }

    public override void OnInspectorGUI()
    {
        // Step 1: Select SkinnedMeshRenderer
        _skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
            "Skinned Mesh Renderer", 
            _skinnedMeshRenderer, 
            typeof(SkinnedMeshRenderer), 
            true
        );

        if (_skinnedMeshRenderer == null)
        {
            EditorGUILayout.HelpBox("Please assign a SkinnedMeshRenderer to proceed.", MessageType.Info);
            return;
        }

        // Step 2: Display configuration options (Blink, LipSync)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select Target Configuration", EditorStyles.boldLabel);

        if (GUILayout.Button("Blink"))
        {
            ConfigureBlink();
        }

        if (GUILayout.Button("LipSync"))
        {
            ConfigureLipSync();
        }

        // Step 3: Display selected mapping details or editing mode
        if (_currentEditingCategory != null)
        {
            DisplayBlendShapeSelection();
        }
        else if (_selectedMapping != null)
        {
            DisplaySelectedMapping();
        }
    }

    private void DisplayBlendShapeSelection()
    {
        EditorGUILayout.LabelField($"Editing: {_currentEditingCategory}", EditorStyles.boldLabel);

        if (_skinnedMeshRenderer == null || _skinnedMeshRenderer.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("SkinnedMeshRenderer is missing or does not have a mesh.", MessageType.Warning);
            return;
        }

        var mesh = _skinnedMeshRenderer.sharedMesh;

        // Iterate through all blend shapes in the SkinnedMeshRenderer
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string blendShapeName = mesh.GetBlendShapeName(i);

            // Check if the blend shape is already added to the current category
            var group = _selectedMapping.Groups.Find(g => g.Name == _currentEditingCategory);
            bool isAlreadyAdded = group != null && group.Targets.Exists(t => t.Index == i);

            // Display blend shape name and button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Index {i}: {blendShapeName}");

            if (isAlreadyAdded)
            {
                if (GUILayout.Button("Remove this blendshape"))
                {
                    RemoveBlendShapeFromCategory(_currentEditingCategory, i);
                }
            }
            else
            {
                if (GUILayout.Button("Add this blendshape"))
                {
                    AddBlendShapeToCategory(_currentEditingCategory, i);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Add a separator line between blend shapes
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        // End Edit button
        if (GUILayout.Button("End Edit", CreateColoredButtonStyle(Color.gray)))
        {
            _currentEditingCategory = null; // Exit editing mode
        }
    }

    private void AddBlendShapeToCategory(string category, int index)
    {
        var group = _selectedMapping.Groups.Find(g => g.Name == category);
        if (group != null && !group.Targets.Exists(t => t.Index == index))
        {
            group.Targets.Add(new BlendShapeGroup.BlendShapeDetail { Index = index });
            EditorUtility.SetDirty(_mapperSO);
        }
    }

    private void RemoveBlendShapeFromCategory(string category, int index)
    {
        var group = _selectedMapping.Groups.Find(g => g.Name == category);
        if (group != null)
        {
            var target = group.Targets.Find(t => t.Index == index);
            if (target != null)
            {
                group.Targets.Remove(target);
                EditorUtility.SetDirty(_mapperSO);
            }
        }
    }

    private void ConfigureBlink()
    {
        _selectedMapping = GetOrCreateMapping(FaceAnimationType.Blink);

        // Add Blink groups if they don't exist
        AddGroupIfNotExists(_selectedMapping, "Left Eye Blink");
        AddGroupIfNotExists(_selectedMapping, "Right Eye Blink");

        EditorUtility.SetDirty(_mapperSO);
    }

    private void ConfigureLipSync()
    {
        _selectedMapping = GetOrCreateMapping(FaceAnimationType.LipSync);

        // Add LipSync groups if they don't exist
        string[] lipSyncNames = {
            "sil", "PP", "FF", "TH", "DD", "kk", "CH", "SS",
            "nn", "RR", "aa", "E", "ih", "oh", "ou"
        };

        foreach (var name in lipSyncNames)
        {
            AddGroupIfNotExists(_selectedMapping, name);
        }

        EditorUtility.SetDirty(_mapperSO);
    }

    private GUIStyle CreateColoredButtonStyle(Color baseColor)
    {
        // Adjust the base color to be softer
        Color softenedColor = Color.Lerp(baseColor, Color.white, 0.6f); // Base (normal) color
        Color activeColor = Color.Lerp(baseColor, Color.black, 0.3f); // Active color

        // Create a texture for each state
        Texture2D normalTexture = CreateTexture(softenedColor);
        Texture2D activeTexture = CreateTexture(activeColor);

        // Create the GUIStyle
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // Assign textures to the GUIStyle
        buttonStyle.normal.background = normalTexture;
        buttonStyle.active.background = activeTexture;
        buttonStyle.normal.textColor = Color.black;  // Text color for normal state
        buttonStyle.active.textColor = Color.white;  // Text color for active state

        return buttonStyle;
    }

    /// <summary>
    /// Creates a 1x1 Texture2D of the given color.
    /// </summary>
    /// <param name="color">The color to fill the texture with.</param>
    /// <returns>A Texture2D filled with the specified color.</returns>
    private Texture2D CreateTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Dictionary<string, bool> groupFoldoutStates = new Dictionary<string, bool>(); // Store foldout states for each group

    private void DisplaySelectedMapping()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Editing: {_selectedMapping.Type}", EditorStyles.boldLabel);

        foreach (var group in _selectedMapping.Groups)
        {
            EditorGUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            
            // Ensure the foldout state exists for the group
            if (!groupFoldoutStates.ContainsKey(group.Name))
            {
                groupFoldoutStates[group.Name] = false; // Default to collapsed
            }

            // Group Header with foldout
            groupFoldoutStates[group.Name] = EditorGUILayout.Foldout(groupFoldoutStates[group.Name], group.Name, true);

            if (groupFoldoutStates[group.Name])
            {
                EditorGUILayout.BeginVertical("box");

                // Display existing blend shapes in the group
                for (int i = 0; i < group.Targets.Count; i++)
                {
                    var target = group.Targets[i];
                    EditorGUILayout.Space(5);

                    // Blend shape details
                    if (_skinnedMeshRenderer != null &&
                        _skinnedMeshRenderer.sharedMesh != null &&
                        target.Index >= 0 &&
                        target.Index < _skinnedMeshRenderer.sharedMesh.blendShapeCount)
                    {
                        string blendShapeName = _skinnedMeshRenderer.sharedMesh.GetBlendShapeName(target.Index);

                        EditorGUILayout.BeginHorizontal();

                        // Display blend shape name
                        EditorGUILayout.LabelField($"Blend Shape Name: {blendShapeName}", GUILayout.Width(200));

                        // Display non-editable index
                        EditorGUILayout.LabelField($"Index: {target.Index}", GUILayout.Width(100));

                        EditorGUILayout.EndHorizontal();
                    }

                    // Max Value slider
                    EditorGUILayout.LabelField("Target Value");
                    target.TargetValue = EditorGUILayout.Slider(target.TargetValue, 0f, 100f);

                    // Remove button
                    if (GUILayout.Button("Remove BlendShape", CreateColoredButtonStyle(Color.red)))
                    {
                        group.Targets.RemoveAt(i);
                        EditorUtility.SetDirty(_mapperSO); // Mark the object as dirty for saving
                        break;
                    }
                }

                // Add some spacing and explanation for "Start Edit"
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Start Edit allows you to add new blend shapes to this group from the Skinned Mesh Renderer.", MessageType.Info);

                // Add Target button
                if (GUILayout.Button("Start Edit", CreateColoredButtonStyle(Color.green)))
                {
                    _currentEditingCategory = group.Name; // Enter editing mode
                }

                // Add some spacing and explanation for "Simulate Blend Shape"
                EditorGUILayout.HelpBox("Simulate Blend Shape allows you to preview the blend shape animation by cycling its value.", MessageType.Info);

                // Simulate Blend Shape button
                if (GUILayout.Button("Simulate Blend Shape"))
                {
                    SimulateBlendShape(group);
                }
                // Simulate Blend Shape button
                if (GUILayout.Button("Reset Simulate Blend Shape") )
                {
                    ResetSimulatingBlendShape();
                }

                EditorGUILayout.EndVertical();
            }
        }
    }

    private void SimulateBlendShape(BlendShapeGroup group)
    {
        if (_skinnedMeshRenderer == null || _skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogWarning("SkinnedMeshRenderer or Mesh is missing.");
            return;
        }

        // Apply TargetValue to the blend shape weights
        foreach (var target in group.Targets)
        {
            if (target.Index >= 0 && target.Index < _skinnedMeshRenderer.sharedMesh.blendShapeCount)
            {
                _skinnedMeshRenderer.SetBlendShapeWeight(target.Index, target.TargetValue);
            }
        }

        // Repaint the editor to reflect changes
        SceneView.RepaintAll();
    }

    private void ResetSimulatingBlendShape()
    {
        if (_skinnedMeshRenderer == null || _skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogWarning("SkinnedMeshRenderer or Mesh is missing.");
            return;
        }

        // Reset all blend shape weights to 0
        for (int i = 0; i < _skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
        }

        // Repaint the editor to reflect changes
        SceneView.RepaintAll();
    }

    private BlendShapeMapperSO.FacialAnimationMapping GetOrCreateMapping(FaceAnimationType type)
    {
        var mapping = _mapperSO.Mappings.Find(m => m.Type == type);
        if (mapping == null)
        {
            mapping = new BlendShapeMapperSO.FacialAnimationMapping { Type = type };
            _mapperSO.Mappings.Add(mapping);
        }
        return mapping;
    }

    private void AddGroupIfNotExists(BlendShapeMapperSO.FacialAnimationMapping mapping, string groupName)
    {
        if (mapping.Groups.Exists(g => g.Name == groupName)) return;

        mapping.Groups.Add(new BlendShapeGroup
        {
            Name = groupName,
            Targets = new List<BlendShapeGroup.BlendShapeDetail>()
        });
    }
}