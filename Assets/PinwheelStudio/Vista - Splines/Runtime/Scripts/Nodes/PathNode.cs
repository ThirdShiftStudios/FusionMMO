#if VISTA
using Pinwheel.Vista.Graph;
using UnityEngine;

namespace Pinwheel.Vista.Splines.Graph
{
    [NodeMetadata(
        title = "Path",
        path = "General/Path",
        icon = "",
        documentation = "",
        description = "Paint a path follow a spline as a mask. The mask can be later used in texturing with Weight Blend or Color Blend node",
        keywords = "road")]
    public class PathNode : ImageNodeBase
    {
        public readonly MaskSlot falloffDetailSlot = new MaskSlot("Falloff Detail", SlotDirection.Input, 0);
        public readonly MaskSlot outputSlot = new MaskSlot("Output", SlotDirection.Output, 101);

        [SerializeField]
        private string m_splineId;
        public string splineId
        {
            get
            {
                return m_splineId;
            }
            set
            {
                m_splineId = value;
            }
        }

        [SerializeField]
        private AnimationCurve m_falloff;
        public AnimationCurve falloff
        {
            get
            {
                return m_falloff;
            }
            set
            {
                m_falloff = value;
            }
        }

        public PathNode() : base()
        {
            m_splineId = "PICK_A_SPLINE_ID";
            m_falloff = CreateDefaultFalloff();
        }

        private AnimationCurve CreateDefaultFalloff()
        {
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe(0,0),
                new Keyframe(0.4f, 1),
                new Keyframe(0.8f, 0),
                new Keyframe(1,0)
            };
            return new AnimationCurve(keys);
        }


        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            SlotRef inputFalloffDetailRefLink = context.GetInputLink(m_id, falloffDetailSlot.id);
            Texture inputFalloffDetailTexture = context.GetTexture(inputFalloffDetailRefLink);
            int inputResolution;
            if (inputFalloffDetailTexture == null)
            {
                inputFalloffDetailTexture = Texture2D.whiteTexture;
                inputResolution = baseResolution;
            }
            else
            {
                inputResolution = inputFalloffDetailTexture.width;
            }

            int resolution = this.CalculateResolution(baseResolution, inputResolution);
            DataPool.RtDescriptor outputDesc = DataPool.RtDescriptor.Create(resolution, resolution);
            SlotRef outputRef = new SlotRef(m_id, outputSlot.id);
            RenderTexture outputRt = context.CreateRenderTarget(outputDesc, outputRef);

            NodeLibraryUtilities.PathNode.Execute(context, m_splineId, m_falloff, inputFalloffDetailTexture, outputRt);

            context.ReleaseReference(inputFalloffDetailRefLink);
        }
    }
}
#endif
