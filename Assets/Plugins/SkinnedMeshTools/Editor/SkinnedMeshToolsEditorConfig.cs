/*
 *	Created by:  Peter @sHTiF Stefcek
 */

#if UNITY_EDITOR
using System;
using UnityEngine;

[Serializable]
public class SkinnedMeshToolsEditorConfig : ScriptableObject
{
    public bool enabled = true;
    
    public int boneIndex = 1;

    public bool showBoneWeights = false;

    public bool showBones = true;

    public bool useAlphaForWeightColor = true;
    
    public Color boneWeightColor;

    public bool enableEditing = false;
}
#endif