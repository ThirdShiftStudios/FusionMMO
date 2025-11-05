using Fusion.Addons.KCC;
using UnityEngine;

namespace TPSBR
{
    public sealed class BuffApplyZone : NetworkKCCProcessor
    {
        [SerializeField]
        private BuffDefinition _buffDefinition;

        public override void OnEnter(KCC kcc, KCCData data)
        {
            if (kcc == null || kcc.IsInFixedUpdate == false || HasStateAuthority == false)
            {
                return;
            }

            if (_buffDefinition == null)
            {
                return;
            }

            Agent agent = kcc.GetComponent<Agent>();
            BuffSystem buffSystem = agent != null ? agent.BuffSystem : null;

            buffSystem?.ApplyBuff(_buffDefinition);
        }
    }
}
