#if VISTA
using Pinwheel.Vista.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Type = System.Type;

namespace Pinwheel.VistaEditor.Graph
{
    [NodeEditor(typeof(DefaultValueNode))]
    public class DefaultValueNodeEditor : ExecutableNodeEditorBase
    {
        private static readonly GUIContent SLOT_TYPE = new GUIContent("Slot Type", "Data type of the slot");

        public override void OnGUI(INode node)
        {
            DefaultValueNode n = node as DefaultValueNode;
            EditorGUI.BeginChangeCheck();
            List<Type> slotTypes = SlotProvider.GetAllSlotTypes();
            int selectedTypeIndex = slotTypes.IndexOf(n.slotType);
            string[] slotTypeLabels = new string[slotTypes.Count];
            for (int i = 0; i < slotTypes.Count; ++i)
            {
                slotTypeLabels[i] = ObjectNames.NicifyVariableName(slotTypes[i].Name);
            }
            selectedTypeIndex = EditorGUILayout.Popup(SLOT_TYPE, selectedTypeIndex, slotTypeLabels);
            if (EditorGUI.EndChangeCheck())
            {
                m_graphEditor.RegisterUndo(n);
                if (selectedTypeIndex >= 0 && selectedTypeIndex < slotTypes.Count)
                {
                    n.SetSlotType(slotTypes[selectedTypeIndex]);
                }
                else
                {
                    n.SetSlotType(slotTypes[0]);
                }
            }
        }
    }
}
#endif
