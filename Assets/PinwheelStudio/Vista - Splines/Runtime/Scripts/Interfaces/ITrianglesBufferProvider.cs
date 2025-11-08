#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Vista.Splines
{
    public interface ITrianglesBufferProvider
    {
        ComputeBuffer trianglesBuffer { get; }
    }
}
#endif
