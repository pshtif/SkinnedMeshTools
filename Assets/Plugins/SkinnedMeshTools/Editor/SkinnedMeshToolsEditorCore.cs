#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

[InitializeOnLoad]
public class SkinnedMeshToolsEditorCore
{
    static private Texture circleTexture => Resources.Load<Texture>("Circle_Icon");

    static private Material _boneWeightMaterial;
    static private Material boneWeightMaterial
    {
        get
        {
            if (_boneWeightMaterial == null)
                _boneWeightMaterial = new Material(Shader.Find("Hidden/SkinnedMeshTools/VertexColorShader"));
            return _boneWeightMaterial;
        }
    } 
    static private GameObject _previousSelected;

    static private SkinnedMeshRenderer _currentSkinnedMesh;
    
    static public SkinnedMeshToolsEditorConfig Config { get; private set; }

    static void CreateConfig()
    {
        Config = (SkinnedMeshToolsEditorConfig) AssetDatabase.LoadAssetAtPath("Assets/Resources/SkinnedMeshToolsEditorConfig.asset",
            typeof(SkinnedMeshToolsEditorConfig));
            
        if (Config == null)
        {
            Config = ScriptableObject.CreateInstance<SkinnedMeshToolsEditorConfig>();
            Config.boneWeightColor = new Color(1, 0, 0, 0);
            if (Config != null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets","Resources");
                }
                AssetDatabase.CreateAsset(Config, "Assets/Resources/SkinnedMeshToolsEditorConfig.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
    
    static SkinnedMeshToolsEditorCore()
    {
        CreateConfig();
        
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView p_view)
    {
        Tools.hidden = false;
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.activeInHierarchy || !Config.enabled)
            return;

        _currentSkinnedMesh = selected.GetComponent<SkinnedMeshRenderer>();
        if (_currentSkinnedMesh == null)
        {
            _currentSkinnedMesh = selected.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_currentSkinnedMesh == null)
                return;
        }

        if (Config.showBones && Config.showBoneWeights)
            DrawBoneWeights(Config.boneIndex);

        if (Config.showBones)
            DrawBones();
        
        //DrawBindPose(smr);
        
        DrawGUI();
    }

    private static void DrawGUI()
    {
        Handles.BeginGUI();
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = Texture2D.whiteTexture; // must be white to tint properly
        GUI.color = new Color(0,0,0,.7f);
        GUI.Box(new Rect(0,0,Screen.width,30),"", style);
        
        GUILayout.BeginArea(new Rect(5,5,600,20));
        GUILayout.BeginHorizontal();

        GUI.color = Color.white;
        Config.showBones = GUILayout.Toggle(Config.showBones, "Show Bones", GUILayout.Width(100));

        if (Config.showBones)
        {
            Config.enableEditing = GUILayout.Toggle(Config.enableEditing, "Enable editing", GUILayout.Width(100));
            Config.showBoneWeights = GUILayout.Toggle(Config.showBoneWeights, "Show Bone Weights");
        }

        if (Config.showBones && Config.showBoneWeights)
        {
            string[] boneNames = GetBoneNames(_currentSkinnedMesh);
            Config.boneIndex = EditorGUILayout.Popup(Config.boneIndex, boneNames);

            Config.useAlphaForWeightColor = GUILayout.Toggle(Config.useAlphaForWeightColor, "Use Alpha");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUI.color = new Color(.75f, .75f, .75f);
        GUI.Label(new Rect(Screen.width-180,5, 180, 20), "SkinnedMeshTools v0.1b");
        GUI.color = Color.white;

        Handles.EndGUI();
    }
    
    private static void DrawBindPose()
    {
        Handles.BeginGUI();
        GUI.color = Color.green;
        
        List<Matrix4x4> bindPoses = new List<Matrix4x4>();
        _currentSkinnedMesh.sharedMesh.GetBindposes(bindPoses);
        foreach (Matrix4x4 pose in bindPoses)
        {
            Vector3 position = pose.inverse.MultiplyPoint(Vector3.zero);
            DrawPoint(position, Color.white, 1);
        }

        GUI.color = Color.white;
        Handles.EndGUI();
    }
    
    private static void DrawBones()
    {
        List<Transform> bones = _currentSkinnedMesh.bones.ToList();
        if (bones == null || bones.Count == 0)
            return;
        
        bones.FindAll(b => bones.Contains(b.parent)).ForEach(b => Handles.DrawLine(b.parent.position, b.position));
        Handles.BeginGUI();
        bones.ForEach(b => DrawBone(bones.IndexOf(b), b.position));
        Handles.EndGUI();

        if (Config.enableEditing)
        {
            Tools.hidden = true;
            EditorGUI.BeginChangeCheck();
            Transform currentBone = _currentSkinnedMesh.bones[Config.boneIndex];
            Quaternion rot = Handles.RotationHandle(currentBone.rotation, currentBone.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_currentSkinnedMesh.gameObject, "Bone rotation");
                currentBone.rotation = rot;
            }
        }
    }

    private static void DrawBone(int p_index, Vector3 p_position)
    {
        GUI.color = Color.white;
        Vector2 pos2D = HandleUtility.WorldToGUIPoint(p_position);
        int scale = 10;
        Rect mouseRect = new Rect(pos2D.x - scale / 2, pos2D.y - scale / 2, scale, scale);

        if (Config.showBoneWeights)
        {
            if (p_index == Config.boneIndex)
            {
                GUI.color = Color.green;
                scale = 16;
            }
            else if (mouseRect.Contains(Event.current.mousePosition))
            {
                GUI.color = Color.yellow;
                scale = 16;
            }
        }

        if (GUI.Button(new Rect(pos2D.x - scale / 2, pos2D.y - scale / 2, scale, scale), circleTexture, GUIStyle.none))
        {
            Config.boneIndex = p_index;
        }

        GUI.color = Color.white;
    }

    private static void DrawPoint(Vector3 p_position, Color p_color, float p_scale)
    {
        GUI.color = p_color;
        Vector2 pos2D = HandleUtility.WorldToGUIPoint(p_position);
        GUI.DrawTexture(new Rect(pos2D.x - 4*p_scale, pos2D.y - 4*p_scale, 8*p_scale, 8*p_scale), circleTexture);
        GUI.color = Color.white;
    }
    
    private static void DrawBoneWeights(int p_boneIndex)
    {
        GL.Clear(true, false, Color.black);
        Mesh mesh = GenerateBoneWeightMesh(_currentSkinnedMesh, p_boneIndex);
        if (boneWeightMaterial != null)
        {
            boneWeightMaterial.SetPass(0);
            Graphics.DrawMeshNow(mesh, _currentSkinnedMesh.transform.localToWorldMatrix);
        }
    }

    private static Mesh GenerateBoneWeightMesh(SkinnedMeshRenderer p_skinnedMesh, int p_boneIndex)
    {
        Mesh mesh = new Mesh();
        p_skinnedMesh.BakeMesh(mesh);
        Color[] colors = new Color[mesh.vertexCount];
        BoneWeight[] boneWeights = p_skinnedMesh.sharedMesh.boneWeights;
        for (int i =0; i< mesh.vertexCount; i++)
        {
            colors[i] = GetBoneWeightColor(boneWeights[i], p_boneIndex);
        }

        mesh.colors = colors;
        return mesh;
    }

    private static Color GetBoneWeightColor(BoneWeight p_boneWeight, int p_boneIndex)
    {
        if (Config.useAlphaForWeightColor)
        {
            if (p_boneWeight.boneIndex0 == p_boneIndex)
                return new Color(Config.boneWeightColor.r, Config.boneWeightColor.g, Config.boneWeightColor.b,
                    p_boneWeight.weight0);
            if (p_boneWeight.boneIndex1 == p_boneIndex)
                return new Color(Config.boneWeightColor.r, Config.boneWeightColor.g, Config.boneWeightColor.b,
                    p_boneWeight.weight1);
            if (p_boneWeight.boneIndex2 == p_boneIndex)
                return new Color(Config.boneWeightColor.r, Config.boneWeightColor.g, Config.boneWeightColor.b,
                    p_boneWeight.weight2);
            if (p_boneWeight.boneIndex3 == p_boneIndex)
                return new Color(Config.boneWeightColor.r, Config.boneWeightColor.g, Config.boneWeightColor.b,
                    p_boneWeight.weight3);
            
            return new Color(0, 0, 0, 0);
        }
        
        if (p_boneWeight.boneIndex0 == p_boneIndex)
            return new Color(Config.boneWeightColor.r * p_boneWeight.weight0,
                Config.boneWeightColor.g * p_boneWeight.weight0, Config.boneWeightColor.b * p_boneWeight.weight0, 1);
        if (p_boneWeight.boneIndex1 == p_boneIndex)
            return new Color(Config.boneWeightColor.r * p_boneWeight.weight1,
                Config.boneWeightColor.g * p_boneWeight.weight1, Config.boneWeightColor.b * p_boneWeight.weight1, 1);
        if (p_boneWeight.boneIndex2 == p_boneIndex)
            return new Color(Config.boneWeightColor.r * p_boneWeight.weight2,
                Config.boneWeightColor.g * p_boneWeight.weight2, Config.boneWeightColor.b * p_boneWeight.weight2, 1);
        if (p_boneWeight.boneIndex3 == p_boneIndex)
            return new Color(Config.boneWeightColor.r * p_boneWeight.weight3,
                Config.boneWeightColor.g * p_boneWeight.weight3, Config.boneWeightColor.b * p_boneWeight.weight3, 1);
        
        return new Color(0, 0, 0, 1);
    }

    private static string[] GetBoneNames(SkinnedMeshRenderer p_skinnedMesh)
    {
        return p_skinnedMesh.bones.ToList().Select(b => b.name).ToArray();
    }
}
#endif