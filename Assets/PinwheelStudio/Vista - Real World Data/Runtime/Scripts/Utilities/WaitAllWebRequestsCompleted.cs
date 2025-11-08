#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using UnityEngine.Networking;

namespace Pinwheel.Vista.RealWorldData
{
    public class WaitAllWebRequestsCompleted : CustomYieldInstruction
    {
        private IEnumerable<UnityWebRequestAsyncOperation> m_tasks;

        public WaitAllWebRequestsCompleted(IEnumerable<UnityWebRequestAsyncOperation> tasks)
        {
            m_tasks = tasks;
        }

        public override bool keepWaiting
        {
            get
            {
                foreach (UnityWebRequestAsyncOperation t in m_tasks)
                {
                    if (!t.isDone)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
#endif
