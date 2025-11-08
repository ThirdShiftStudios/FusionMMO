#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEditor;
using System;
using Pinwheel.Vista.Graph;
using System.Linq;
using Pinwheel.Vista.ExposeProperty;

namespace Pinwheel.VistaEditor.ExposeProperty
{
    public static class LPBInspectorHandler
    {
        private static readonly GUIContent SYNC_WITH_GRAPH = new GUIContent("Sync With Graph");
        private static readonly string UNNAMED_PROPERTY = "Unnamed Property";

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            LocalProceduralBiomeInspector.drawExposedPropertiesCallback += OnDrawExposedProperties;
        }

        private static void OnDrawExposedProperties(LocalProceduralBiomeInspector inspector, LocalProceduralBiome biome)
        {
            DrawExposedProperties(biome, biome.terrainGraph);
        }

        private static void DrawExposedProperties(LocalProceduralBiome biome, TerrainGraph graph)
        {
            if (!graph.HasExposedProperties)
                return;

            IEnumerable<PropertyDescriptor> properties = graph.GetExposedProperties();
            IEnumerable<string> groupNames = properties.Select(p => p.groupName).Distinct().OrderBy(name => name);

            foreach (string gn in groupNames)
            {
                IEnumerable<PropertyDescriptor> propertiesInGroup = properties.Where(p => string.Equals(p.groupName, gn)).OrderBy(p => p.label);
                if (!string.IsNullOrEmpty(gn))
                {
                    string k = $"graph-properties-group-{gn}";
                    bool expanded = SessionState.GetBool(k, true);
                    expanded = EditorGUILayout.Foldout(expanded, gn, true, EditorCommon.Styles.foldoutBold);
                    SessionState.SetBool(k, expanded);
                    if (expanded)
                    {
                        foreach (PropertyDescriptor p in propertiesInGroup)
                        {
                            DrawProperty(biome, graph, p);
                        }
                    }
                }
                else
                {
                    foreach (PropertyDescriptor p in propertiesInGroup)
                    {
                        DrawProperty(biome, graph, p);
                    }
                }
            }

            if (EditorCommon.Button(SYNC_WITH_GRAPH))
            {
                Undo.RecordObject(biome, $"Modify {biome.name}");
                Undo.RecordObject(graph, $"Modify {graph.name}");
                EditorUtility.SetDirty(biome);
                EditorUtility.SetDirty(graph);

                foreach (PropertyDescriptor p in properties)
                {
                    p.SyncWithGraph(graph);
                    PropertyOverride po = biome.GetPropertyOverride(p.id);
                    if (po != null)
                    {
                        po.SyncWithGraph(graph);
                    }
                }
            }
        }

        private static void DrawProperty(LocalProceduralBiome biome, TerrainGraph graph, PropertyDescriptor p)
        {
            PropertyOverride po = biome.GetPropertyOverride(p.id);

            EditorGUI.BeginChangeCheck();
            GUIContent guiContent = new GUIContent(!string.IsNullOrEmpty(p.label) ? p.label : UNNAMED_PROPERTY, p.description);
            int intValue = po.intValue;
            float floatValue = po.floatValue;
            bool boolValue = po.boolValue;
            string stringValue = po.stringValue;
            Vector4 vectorValue = po.vectorValue;
            int enumValue = po.enumValue;
            Color colorValue = po.colorValue;
            Gradient gradientValue = po.gradientValue;
            AnimationCurve curveValue = po.curveValue;
            UnityEngine.Object objectValue = po.objectValue;

            PropertyType t = p.propertyType;
            if (t == PropertyType.IntegerNumber)
            {
                MinMaxInt intRange = p.intValueRange;
                if (intRange == MinMaxInt.FULL_RANGE)
                {
                    intValue = EditorGUILayout.IntField(guiContent, po.intValue);
                }
                else
                {
                    intValue = EditorGUILayout.IntSlider(guiContent, po.intValue, intRange.min, intRange.max);
                }
            }
            else if (t == PropertyType.RealNumber)
            {
                MinMaxFloat floatRange = p.floatValueRange;
                if (floatRange == MinMaxFloat.FULL_RANGE)
                {
                    floatValue = EditorGUILayout.FloatField(guiContent, po.floatValue);
                }
                else
                {
                    floatValue = EditorGUILayout.Slider(guiContent, po.floatValue, floatRange.min, floatRange.max);
                }
            }
            else if (t == PropertyType.TrueFalse)
            {
                boolValue = EditorGUILayout.Toggle(guiContent, po.boolValue);
            }
            else if (t == PropertyType.Text)
            {
                stringValue = EditorGUILayout.TextField(guiContent, po.stringValue);
            }
            else if (t == PropertyType.Vector)
            {
                vectorValue = EditorGUILayout.Vector4Field(guiContent, po.vectorValue);
            }
            else if (t == PropertyType.Options)
            {
                enumValue = EnumPopupInt(guiContent, po.enumValue, p.enumType);
            }
            else if (t == PropertyType.Color)
            {
                colorValue = EditorGUILayout.ColorField(guiContent, po.colorValue);
            }
            else if (t == PropertyType.Gradient)
            {
                gradientValue = EditorGUILayout.GradientField(guiContent, po.gradientValue);
            }
            else if (t == PropertyType.Curve)
            {
                curveValue = EditorGUILayout.CurveField(guiContent, po.curveValue);
            }
            else if (t == PropertyType.UnityObject)
            {
                objectValue = EditorGUILayout.ObjectField(guiContent, po.objectValue, p.objectType, false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(graph, $"Modify {graph.name}");
                EditorUtility.SetDirty(graph);
                Undo.RecordObject(biome, $"Modify {biome.name}");
                EditorUtility.SetDirty(biome);
                po.intValue = intValue;
                po.floatValue = floatValue;
                po.boolValue = boolValue;
                po.stringValue = stringValue;
                po.vectorValue = vectorValue;
                po.enumValue = enumValue;
                po.colorValue = colorValue;
                po.gradientValue = gradientValue;
                po.curveValue = curveValue;
                po.objectValue = objectValue;
            }
        }

        private static int EnumPopupInt(GUIContent label, int value, Type enumType)
        {
            string[] optionTexts = Enum.GetNames(enumType);
            GUIContent[] options = new GUIContent[optionTexts.Length];
            for (int i = 0; i < optionTexts.Length; ++i)
            {
                options[i] = new GUIContent(optionTexts[i]);
            }

            Array values = Enum.GetValues(enumType);
            int[] valuesInt = new int[values.Length];
            values.CopyTo(valuesInt, 0);

            value = EditorGUILayout.IntPopup(label, value, options, valuesInt);
            return value;
        }
    }
}
#endif
