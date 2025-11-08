#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using Pinwheel.Vista.Graph;

namespace Pinwheel.Vista.RealWorldData.Graph
{
    [NodeMetadata(
        title = "Load Real World Data",
        path = "Data/Load Real World Data",
        description = "Load real world data from a container with a format that the graph can make use of. You need to create a data container (Create/Vista/Data Provider) to download data first.")]
    public class LoadRealWorldDataNode : ImageNodeBase
    {
        public readonly MaskSlot heightMapSlot = new MaskSlot("Height Map", SlotDirection.Output, 100);
        public readonly ColorTextureSlot colorMapSlot = new ColorTextureSlot("Color Map", SlotDirection.Output, 101);

        [SerializeAsset]
        private DataProviderAsset m_dataProviderAsset;
        public DataProviderAsset dataProviderAsset
        {
            get
            {
                return m_dataProviderAsset;
            }
            set
            {
                m_dataProviderAsset = value;
            }
        }

        public LoadRealWorldDataNode() : base()
        {
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            if (m_dataProviderAsset == null)
                return;

            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            int resolution = this.CalculateResolution(baseResolution, baseResolution);
            if (m_dataProviderAsset.heightMap != null)
            {
                SlotRef outputRef = new SlotRef(m_id, heightMapSlot.id);
                if (context.IsTargetNode(m_id) || context.GetReferenceCount(outputRef) > 0)
                {
                    DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution);
                    RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);
                    Drawing.Blit(m_dataProviderAsset.heightMap, targetRt);                    
                }
            }

            if (m_dataProviderAsset.colorMap != null)
            {
                SlotRef outputRef = new SlotRef(m_id, colorMapSlot.id);
                if (context.IsTargetNode(m_id) || context.GetReferenceCount(outputRef) > 0)
                {
                    DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution, RenderTextureFormat.ARGB32);
                    RenderTexture targetRt = context.CreateRenderTarget(desc, outputRef);
                    Drawing.Blit(m_dataProviderAsset.colorMap, targetRt);
                }
            }
        }
    }
}
#endif
