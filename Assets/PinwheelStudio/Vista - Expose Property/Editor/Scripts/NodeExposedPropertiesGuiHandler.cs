#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using Pinwheel.VistaEditor.Graph;
using Pinwheel.Vista.Graph;
using System;
using System.Reflection;
using Pinwheel.Vista.ExposeProperty;

namespace Pinwheel.VistaEditor.ExposeProperty
{
    [InitializeOnLoad]
    public static class NodeExposedPropertiesGuiHandler
    {
        private static readonly GUIContent EXPOSED_PROPERTIES = new GUIContent("EXPOSED PROPERTIES");
        private static readonly GUIContent ADD_PROPERTY = new GUIContent("Add Property...");
        private static readonly GUIContent REMOVE_PROPERTY = new GUIContent("X");

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            NodeEditor.exposedPropertiesGuiCallback += OnNodeExposedPropertiesGUI;
        }

        private static void OnNodeExposedPropertiesGUI(GraphAsset parentGraph, INode node)
        {
            List<PropertyInfo> exposableProperties = GetExposableProperties(node);
            if (exposableProperties.Count == 0)
                return;

            const string EXPOSED_PROPERTIES_FOLDOUT_KEY = "vista-graph-editor-exposed-properties";
            bool exposedPropertiesExpanded = SessionState.GetBool(EXPOSED_PROPERTIES_FOLDOUT_KEY, false);
            exposedPropertiesExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(exposedPropertiesExpanded, "Exposed Properties");
            if (exposedPropertiesExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                GenericMenu menu = new GenericMenu();
                foreach (PropertyInfo p in exposableProperties)
                {
                    GUIContent label = new GUIContent(p.Name);
                    bool isExposed = parentGraph.HasPropertyExposed(node.id, p.Name);
                    if (isExposed)
                    {
                        menu.AddDisabledItem(new GUIContent(label));
                        PropertyDescriptor exposedProperty = parentGraph.GetExposedProperty(node.id, p.Name);
                        DrawExposedPropertyGUI(parentGraph, node, exposedProperty);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(label), false, () => { parentGraph.ExposeProperty(node.id, p.Name); GUI.changed = true; });
                    }
                }

                if (EditorCommon.Button(ADD_PROPERTY))
                {
                    menu.ShowAsContext();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            SessionState.SetBool(EXPOSED_PROPERTIES_FOLDOUT_KEY, exposedPropertiesExpanded);
        }

        private static List<PropertyInfo> GetExposableProperties(INode node)
        {
            List<PropertyInfo> exposableProperties = new List<PropertyInfo>();
            PropertyInfo[] properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            foreach (PropertyInfo p in properties)
            {
                if (!PropertyDescriptor.IsExposable(p.PropertyType))
                    continue;
                if (p.GetSetMethod() == null)
                    continue;
                NonExposableAttribute att = p.GetCustomAttribute<NonExposableAttribute>();
                if (att == null)
                {
                    exposableProperties.Add(p);
                }
            }

            return exposableProperties;
        }

        private static readonly GUIContent PROPERTY_LABEL = new GUIContent("Label", "Nice display name for the property in the graph inspector.");
        private static readonly GUIContent PROPERTY_DESCRIPTION = new GUIContent("Description", "Explain how the property will affect the terrain generation.");
        private static readonly GUIContent PROPERTY_GROUP_NAME = new GUIContent("Group", "Display this property in a group in the graph inspector GUI.");
        private static readonly GUIContent MIN = new GUIContent("Min", "Minimum value of this property");
        private static readonly GUIContent MAX = new GUIContent("Max", "Maximum value of this property");

        private static void DrawExposedPropertyGUI(GraphAsset parentGraph, INode node, PropertyDescriptor property)
        {
            EditorGUILayout.BeginHorizontal();
            bool expanded = SessionState.GetBool(property.id.ToString(), false);
            expanded = EditorGUILayout.Foldout(expanded, $"{property.id.propertyName} ({ObjectNames.NicifyVariableName(property.propertyType.ToString())})", true);
            SessionState.SetBool(property.id.ToString(), expanded);

            if (EditorCommon.Button(REMOVE_PROPERTY, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight - 2)))
            {
                parentGraph.UnexposeProperty(node.id, property.id.propertyName);
            }
            EditorGUILayout.EndHorizontal();

            if (expanded)
            {
                EditorGUI.indentLevel += 1;
                EditorGUI.BeginChangeCheck();
                string label = EditorGUILayout.TextField(PROPERTY_LABEL, property.label);
                string description = EditorGUILayout.TextField(PROPERTY_DESCRIPTION, property.description);
                string groupName = EditorGUILayout.TextField(PROPERTY_GROUP_NAME, property.groupName);
                MinMaxInt intRange = property.intValueRange;
                MinMaxFloat floatRange = property.floatValueRange;
                if (property.propertyType == PropertyType.IntegerNumber)
                {
                    intRange.min = EditorGUILayout.IntField(MIN, intRange.min);
                    intRange.max = EditorGUILayout.IntField(MAX, intRange.max);
                }
                else if (property.propertyType == PropertyType.RealNumber)
                {
                    floatRange.min = EditorGUILayout.FloatField(MIN, floatRange.min);
                    floatRange.max = EditorGUILayout.FloatField(MAX, floatRange.max);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(parentGraph, $"Modify {parentGraph.name}");
                    EditorUtility.SetDirty(parentGraph);
                    property.label = label;
                    property.description = description;
                    property.groupName = groupName;
                    property.intValueRange = intRange;
                    property.floatValueRange = floatRange;
                }
                EditorGUI.indentLevel -= 1;
            }
        }
    }
}
#endif
