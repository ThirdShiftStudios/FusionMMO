#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using System;
using System.Reflection;

namespace Pinwheel.Vista.ExposeProperty
{
    public static class PropertyDescriptorExtensions
    {
        public static PropertyDescriptor ExposeProperty(this GraphAsset graph, string nodeId, string propertyName)
        {
            if (graph.HasPropertyExposed(nodeId, propertyName))
            {
                throw new ArgumentException($"Property {propertyName} for node {nodeId} is already exposed.");
            }

            PropertyDescriptor exposedProperty = new PropertyDescriptor(graph, nodeId, propertyName);
            exposedProperty.SyncWithGraph(graph);
            graph.m_exposedProperties.Add(exposedProperty);

            return exposedProperty;
        }

        public static PropertyDescriptor UnexposeProperty(this GraphAsset graph, string nodeId, string propertyName)
        {
            PropertyDescriptor exposedProperty = graph.m_exposedProperties.Find(p => string.Equals(p.id.nodeId, nodeId) && string.Equals(p.id.propertyName, propertyName));
            if (graph.m_exposedProperties.Remove(exposedProperty))
            {
                return exposedProperty;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<PropertyDescriptor> GetExposedProperties(this GraphAsset graph)
        {
            if (graph.HasExposedProperties)
            {
                return graph.m_exposedProperties.ToArray();
            }
            else
            {
                return new PropertyDescriptor[0];
            }
        }

        public static IEnumerable<PropertyDescriptor> GetExposedProperties(this GraphAsset graph, string nodeId)
        {
            if (graph.HasExposedProperties)
            {
                return graph.m_exposedProperties.FindAll(p => string.Equals(p.id.nodeId, nodeId));
            }
            else
            {
                return new PropertyDescriptor[0];
            }
        }

        public static PropertyDescriptor GetExposedProperty(this GraphAsset graph, string nodeId, string propertyName)
        {
            PropertyDescriptor p = graph.m_exposedProperties.Find(p => string.Equals(p.id.nodeId, nodeId) && string.Equals(p.id.propertyName, propertyName));
            return p;
        }

        public static bool HasPropertyExposed(this GraphAsset graph, string nodeId, string propertyName)
        {
            return graph.m_exposedProperties.Exists(p => string.Equals(p.id.nodeId, nodeId) && string.Equals(p.id.propertyName, propertyName));
        }

        public static bool HasPropertyExposed(this GraphAsset graph, string nodeId)
        {
            return graph.m_exposedProperties.Exists(p => string.Equals(p.id.nodeId, nodeId));
        }

        public static void SyncExposedPropertyValue(this GraphAsset graph, string nodeId, string propertyName)
        {
            PropertyDescriptor prop = graph.GetExposedProperty(nodeId, propertyName);
            prop.SyncWithGraph(graph);
        }

        internal static void SyncWithGraph(this PropertyDescriptor p, GraphAsset graph)
        {
            INode node = graph.GetNode(p.id.nodeId);
            if (node == null)
                return;

            PropertyInfo propertyInfo = node.GetType().GetProperty(p.id.propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            if (propertyInfo == null)
                return;

            Type t = propertyInfo.PropertyType;
            if (t.IsEnum)
            {
                p.m_propertyType = PropertyType.Options;
                p.m_enumTypeName = t.AssemblyQualifiedName;
            }
            else if (t.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                p.m_propertyType = PropertyType.UnityObject;
                p.m_objectTypeName = t.AssemblyQualifiedName;
            }
            else if (t == typeof(int))
            {
                p.m_propertyType = PropertyType.IntegerNumber;
            }
            else if (t == typeof(float))
            {
                p.m_propertyType = PropertyType.RealNumber;
            }
            else if (t == typeof(bool))
            {
                p.m_propertyType = PropertyType.TrueFalse;
            }
            else if (t == typeof(string))
            {
                p.m_propertyType = PropertyType.Text;
            }
            else if (t == typeof(Vector2))
            {
                p.m_propertyType = PropertyType.Vector;
            }
            else if (t == typeof(Vector3))
            {
                p.m_propertyType = PropertyType.Vector;
            }
            else if (t == typeof(Vector4))
            {
                p.m_propertyType = PropertyType.Vector;
            }
            else if (t == typeof(Color))
            {
                p.m_propertyType = PropertyType.Color;
            }
            else if (t == typeof(Color32))
            {
                p.m_propertyType = PropertyType.Color;
            }
            else if (t == typeof(Gradient))
            {
                p.m_propertyType = PropertyType.Gradient;
            }
            else if (t == typeof(AnimationCurve))
            {
                p.m_propertyType = PropertyType.Curve;
            }
        }
    }
}
#endif
