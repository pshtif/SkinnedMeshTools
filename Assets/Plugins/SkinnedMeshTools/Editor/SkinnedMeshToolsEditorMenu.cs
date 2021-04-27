/*
 *	Created by:  Peter @sHTiF Stefcek
 */


using UnityEditor;

public class SkinnedMeshToolsEditorMenu
{
    [MenuItem("Tools/SkinnedMeshTools/Enable")]
    static void EnableSkinnedMeshTool()
    {
        SkinnedMeshToolsEditorCore.Config.enabled = true;
        SceneView.RepaintAll();
    }
   
    [MenuItem("Tools/SkinnedMeshTools/Enable", true)]
    static bool ValidateEnableSkinnedMeshTool()
    {
        return !SkinnedMeshToolsEditorCore.Config.enabled;
    }
    
    [MenuItem("Tools/SkinnedMeshTools/Disable")]
    static void DisableSkinnedMeshTool()
    {
        SkinnedMeshToolsEditorCore.Config.enabled = false;
        SceneView.RepaintAll();
    }
   
    [MenuItem("Tools/SkinnedMeshTools/Disable", true)]
    static bool ValidateDisableSkinnedMeshTool()
    {
        return SkinnedMeshToolsEditorCore.Config.enabled;
    }
}