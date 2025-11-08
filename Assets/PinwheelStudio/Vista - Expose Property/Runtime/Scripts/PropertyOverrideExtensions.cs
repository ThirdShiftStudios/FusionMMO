#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using System.Reflection;
using System;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.ExposeProperty
{
    public static class PropertyOverrideExtensions
    {
        internal static void SyncWithGraph(this PropertyOverride po, GraphAsset graph)
        {
            INode node = graph.GetNode(po.id.nodeId);
            if (node == null)
                return;

            PropertyInfo propertyInfo = node.GetType().GetProperty(po.id.propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            if (propertyInfo == null)
                return;

            Type t = propertyInfo.PropertyType;
            if (t.IsEnum)
            {
                po.enumValue = (int)propertyInfo.GetValue(node);
            }
            else if (t.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                po.objectValue = (UnityEngine.Object)propertyInfo.GetValue(node);
            }
            else if (t == typeof(int))
            {
                po.intValue = (int)propertyInfo.GetValue(node);
            }
            else if (t == typeof(float))
            {
                po.floatValue = (float)propertyInfo.GetValue(node);
            }
            else if (t == typeof(bool))
            {
                po.boolValue = (bool)propertyInfo.GetValue(node);
            }
            else if (t == typeof(string))
            {
                po.stringValue = (string)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Vector2))
            {
                po.vectorValue = (Vector2)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Vector3))
            {
                po.vectorValue = (Vector3)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Vector4))
            {
                po.vectorValue = (Vector4)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Color))
            {
                po.colorValue = (Color)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Color32))
            {
                po.colorValue = (Color32)propertyInfo.GetValue(node);
            }
            else if (t == typeof(Gradient))
            {
                po.gradientValue = (Gradient)propertyInfo.GetValue(node);
            }
            else if (t == typeof(AnimationCurve))
            {
                po.curveValue = (AnimationCurve)propertyInfo.GetValue(node);
            }
        }

        internal static bool OverrideValue(this PropertyOverride po, GraphAsset graph)
        {
            INode node = graph.GetNode(po.id.nodeId);
            if (node == null)
                return false;

            PropertyInfo propertyInfo = node.GetType().GetProperty(po.id.propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            if (propertyInfo == null)
                return false;

            PropertyDescriptor desc = graph.GetExposedProperty(po.id.nodeId, po.id.propertyName);
            if (desc == null)
                return false;

            PropertyType propertyType = desc.propertyType;
            try
            {
                if (propertyType == PropertyType.IntegerNumber)
                {
                    propertyInfo.SetValue(node, po.intValue);
                }
                else if (propertyType == PropertyType.RealNumber)
                {
                    propertyInfo.SetValue(node, po.floatValue);
                }
                else if (propertyType == PropertyType.TrueFalse)
                {
                    propertyInfo.SetValue(node, po.boolValue);
                }
                else if (propertyType == PropertyType.Text)
                {
                    propertyInfo.SetValue(node, po.stringValue);
                }
                else if (propertyType == PropertyType.Vector)
                {
                    propertyInfo.SetValue(node, po.vectorValue);
                }
                else if (propertyType == PropertyType.Options)
                {
                    propertyInfo.SetValue(node, po.enumValue);
                }
                else if (propertyType == PropertyType.Color)
                {
                    propertyInfo.SetValue(node, po.colorValue);
                }
                else if (propertyType == PropertyType.Gradient)
                {
                    propertyInfo.SetValue(node, po.gradientValue);
                }
                else if (propertyType == PropertyType.Curve)
                {
                    propertyInfo.SetValue(node, po.curveValue);
                }
                else if (propertyType == PropertyType.UnityObject)
                {
                    propertyInfo.SetValue(node, po.objectValue != null ? po.objectValue : null);
                }
                return true;
            }
            catch (System.Exception e)
            {
                Debug.Log(propertyType);
                Debug.Log(e);
            }

            return false;
        }
    }
}
#endif
