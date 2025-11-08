#if VISTA
using Pinwheel.Vista.Graph;
using UnityEngine;
using CoreNodeLibraryUtilities = Pinwheel.Vista.Graph.NodeLibraryUtilities;

namespace Pinwheel.Vista.Splines.Graph
{
    [NodeMetadata(
        title = "Thin Out Along",
        icon = "",
        path = "General/Thin Out Along",
        documentation = "",
        description = "Remove instances along a spline. Similar to the Thin Out node but with a spline mask.",
        keywords = "")]
    public class ThinOutAlongNode : ExecutableNodeBase, IHasDynamicSlotCount
    {
        public readonly MaskSlot inputFalloffDetailSlot = new MaskSlot("Falloff Detail", SlotDirection.Input, 1000);

        [SerializeField]
        private BufferSlot[] m_inputPositionSlots;
        public BufferSlot[] inputPositionSlots
        {
            get
            {
                return m_inputPositionSlots;
            }
        }

        [SerializeField]
        private BufferSlot[] m_outputPositionSlots;
        public BufferSlot[] outputPositionSlots
        {
            get
            {
                return m_outputPositionSlots;
            }
        }

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

        [SerializeField]
        private int m_inputCount;
        [NonExposable]
        public int inputCount
        {
            get
            {
                return m_inputCount;
            }
            set
            {
                int oldValue = m_inputCount;
                int newValue = Mathf.Clamp(value, MIN_INPUT_COUNT, MAX_INPUT_COUNT);
                m_inputCount = newValue;
                if (oldValue != newValue)
                {
                    UpdateSlotArrays();
                    if (slotsChanged != null)
                    {
                        slotsChanged.Invoke(this);
                    }
                }
            }
        }

        [SerializeField]
        private bool m_invertMask;
        public bool invertMask
        {
            get
            {
                return m_invertMask;
            }
            set
            {
                m_invertMask = value;
            }
        }

        public const int MIN_INPUT_COUNT = 1;
        public const int MAX_INPUT_COUNT = 100;

        public event IHasDynamicSlotCount.SlotsChangedHandler slotsChanged;

        private static readonly string TEMP_PATH_MASK_NAME = "~TempPathMask";

        public ThinOutAlongNode() : base()
        {
            m_splineId = "PICK_A_SPLINE_ID";
            m_falloff = CreateDefaultFalloff();
            m_invertMask = true;
            m_inputCount = 1;
            UpdateSlotArrays();
        }

        private AnimationCurve CreateDefaultFalloff()
        {
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe(0,0),
                new Keyframe(0.5f, 1),
                new Keyframe(1,1)
            };
            return new AnimationCurve(keys);
        }

        private void UpdateSlotArrays()
        {
            m_inputPositionSlots = new BufferSlot[m_inputCount];
            for (int i = 0; i < m_inputCount; ++i)
            {
                m_inputPositionSlots[i] = new BufferSlot($"Position {i}", SlotDirection.Input, i);
            }

            m_outputPositionSlots = new BufferSlot[m_inputCount];
            for (int i = 0; i < m_inputCount; ++i)
            {
                m_outputPositionSlots[i] = new BufferSlot($"Position {i}", SlotDirection.Output, MAX_INPUT_COUNT + i);
            }
        }

        public override ISlot[] GetInputSlots()
        {
            ISlot[] slots = new ISlot[m_inputPositionSlots.Length + 1];
            for (int i = 0; i < m_inputPositionSlots.Length; ++i)
            {
                slots[i] = m_inputPositionSlots[i];
            }
            slots[m_inputPositionSlots.Length + 0] = inputFalloffDetailSlot;
            return slots;
        }

        public override ISlot[] GetOutputSlots()
        {
            ISlot[] slots = new ISlot[m_outputPositionSlots.Length];
            for (int i = 0; i < m_outputPositionSlots.Length; ++i)
            {
                slots[i] = m_outputPositionSlots[i];
            }
            return slots;
        }

        public override ISlot GetSlot(int id)
        {
            if (id == inputFalloffDetailSlot.id)
                return inputFalloffDetailSlot;

            BufferSlot[] slots = id < MAX_INPUT_COUNT ? m_inputPositionSlots : m_outputPositionSlots;
            for (int i = 0; i < slots.Length; ++i)
            {
                if (slots[i].id == id)
                    return slots[i];
            }
            return null;
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            SlotRef inputFalloffDetailRefLink = context.GetInputLink(m_id, inputFalloffDetailSlot.id);
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

            int resolution = inputResolution;
            DataPool.RtDescriptor pathRtDesc = DataPool.RtDescriptor.Create(resolution, resolution);
            RenderTexture pathRt = context.CreateTemporaryRT(pathRtDesc, TEMP_PATH_MASK_NAME);

            NodeLibraryUtilities.PathNode.Execute(context, m_splineId, m_falloff, inputFalloffDetailTexture, pathRt);
            if (m_invertMask)
            {
                CoreNodeLibraryUtilities.MathNode.Invert(context, pathRt);
            }

            for (int i = 0; i < m_inputPositionSlots.Length; ++i)
            {
                SlotRef inputRefLink = context.GetInputLink(m_id, m_inputPositionSlots[i].id);
                if (context.GetReferenceCount(inputRefLink) > 0)
                {
                    ComputeBuffer inputBuffer = context.GetBuffer(inputRefLink);
                    if (inputBuffer == null)
                    {
                        //do nothing but don't return here
                    }
                    else if (inputBuffer.count % PositionSample.SIZE != 0)
                    {
                        Debug.LogError($"Cannot parse position buffer, node id {m_id}");
                    }
                    else
                    {
                        SlotRef outputRef = new SlotRef(m_id, m_outputPositionSlots[i].id);
                        DataPool.BufferDescriptor outputDesc = DataPool.BufferDescriptor.Create(inputBuffer.count);
                        ComputeBuffer outputBuffer = context.CreateBuffer(outputDesc, outputRef);

                        CoreNodeLibraryUtilities.ThinOutNode.Execute(context, inputBuffer, pathRt, 1, 0, outputBuffer);
                    }
                }

                context.ReleaseReference(inputRefLink);
            }

            context.ReleaseReference(inputFalloffDetailRefLink);
            context.ReleaseTemporary(TEMP_PATH_MASK_NAME);
        }

        public override void Bypass(GraphContext context)
        {
            for (int i = 0; i < m_inputPositionSlots.Length; ++i)
            {
                SlotRef inputRefLink = context.GetInputLink(m_id, m_inputPositionSlots[i].id);
                string varName = inputRefLink.ToString();

                SlotRef outputRef = new SlotRef(m_id, m_outputPositionSlots[i].id);
                if (!string.IsNullOrEmpty(varName))
                {
                    if (!context.HasVariable(varName))
                    {
                        context.SetVariable(varName, inputRefLink);
                    }
                    context.LinkToVariable(outputRef, varName);
                }
                else
                {
                    context.LinkToInvalid(outputRef);
                }
            }
        }
    }
}
#endif
