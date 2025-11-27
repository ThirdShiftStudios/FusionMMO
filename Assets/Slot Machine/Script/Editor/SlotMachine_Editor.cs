using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SlotMachine))]
public class SlotMachine_Editor : Editor
{
    GUIContent _tooltip;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SlotMachine action = (SlotMachine)target;

        for (int i = 0; i < action.slotList.Length; i++)
        {
            action.slotList[i].sharedMaterial.SetTextureScale("_MainTex", new Vector2(1, 1.0f / (float)action.numberOfObject));
        }
    }
}
