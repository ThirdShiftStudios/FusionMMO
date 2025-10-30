using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AzureFogScatteringFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent { get => m_renderPassEvent; set => m_renderPassEvent = value; }
    [SerializeField] private RenderPassEvent m_renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;

    /// <summary>The material that will render the fog scattering RenderTexture into the screen.</summary>
    public Material fogRendererMaterial { get => m_fogRendererMaterial; set => m_fogRendererMaterial = value; }
    [SerializeField] private Material m_fogRendererMaterial = null;

    /// <summary>The material that will compute the fog scattering effect and render it into a RenderTexture.</summary>
    public Material fogComputationMaterial { get => m_fogComputationMaterial; set => m_fogComputationMaterial = value; }
    [SerializeField] private Material m_fogComputationMaterial = null;

    /// <summary>The render texture that stores the fog scattering data. (RGB: Scattering Data), (Alpha: Fog Data).</summary>
    public RenderTexture fogScatteringRT { get => m_fogScatteringRT; set => m_fogScatteringRT = value; }
    private RenderTexture m_fogScatteringRT = null;

    /// <summary>The start width resolution of the fog scattering render texture.</summary>
    public int fogRenderTextureWidth => m_fogRenderTextureWidth;
    [SerializeField] private int m_fogRenderTextureWidth = 1920;

    /// <summary>The start height resolution of the fog scattering render texture.</summary>
    public int fogRenderTextureHeight => m_fogRenderTextureHeight;
    [SerializeField] private int m_fogRenderTextureHeight = 1080;

    /// <summary>The instance of the AzureFogScatteringPass class.</summary>
    private AzureFogScatteringPass m_azureFogScatteringPass;

    /// <summary>Called when the renderer feature is created or modified.</summary>
    public override void Create()
    {
        m_azureFogScatteringPass = new AzureFogScatteringPass();
        m_azureFogScatteringPass.fogRendererMaterial = m_fogRendererMaterial;
        m_azureFogScatteringPass.fogComputationMaterial = m_fogComputationMaterial;

        SetFogScatteringResolution(m_fogRenderTextureWidth, m_fogRenderTextureHeight);
        m_azureFogScatteringPass.fogScatteringRT = m_fogScatteringRT;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (fogComputationMaterial == null)
        {
            Debug.LogWarningFormat("Missing the Fog Computation Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }

        if (m_fogRendererMaterial == null)
        {
            Debug.LogWarningFormat("Missing the Fog Renderer Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }

        m_azureFogScatteringPass.renderPassEvent = m_renderPassEvent;
        renderer.EnqueuePass(m_azureFogScatteringPass);
    }

    private class AzureFogScatteringPass : ScriptableRenderPass
    {
        /// <summary>The reference to the camera that the fog scattering should be rendered.</summary>
        public Camera camera { get => m_camera; set => m_camera = value; }
        private Camera m_camera = null;

        /// <summary>The material that will render the fog scattering RenderTexture into the screen.</summary>
        public Material fogRendererMaterial { get => m_fogRendererMaterial; set => m_fogRendererMaterial = value; }
        [SerializeField] private Material m_fogRendererMaterial = null;

        /// <summary>The material that will compute the fog scattering effect and render it into a RenderTexture.</summary>
        public Material fogComputationMaterial { get => m_fogComputationMaterial; set => m_fogComputationMaterial = value; }
        [SerializeField] private Material m_fogComputationMaterial = null;

        /// <summary>The render texture that stores the fog scattering data. (RGB: Scattering Data), (Alpha: Fog Data).</summary>
        public RenderTexture fogScatteringRT { get => m_fogScatteringRT; set => m_fogScatteringRT = value; }
        [SerializeField] private RenderTexture m_fogScatteringRT = null;

        /// <summary>Stores the camera frunstum corners position.</summary>
        private Vector3[] m_frustumCorners = new Vector3[4];

        /// <summary>The view port rect.</summary>
        private Rect m_viewRect = new Rect(0, 0, 1, 1);

        /// <summary>The camera frustum corners matrix.</summary>
        private Matrix4x4 m_frustumCornersArray = Matrix4x4.identity;

        private RTHandle m_sourceColor;
        private RTHandle m_DestinationColor;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_sourceColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_DestinationColor, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_TemporaryDestinationHandle");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_fogRendererMaterial == null || m_fogComputationMaterial == null) return;
            m_camera = renderingData.cameraData.camera;
            if (m_camera == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();

            m_camera.CalculateFrustumCorners(m_viewRect, m_camera.farClipPlane, m_camera.stereoActiveEye, m_frustumCorners);
            m_frustumCornersArray = Matrix4x4.identity;
            m_frustumCornersArray.SetRow(0, m_camera.transform.TransformVector(m_frustumCorners[0]));  // bottom left
            m_frustumCornersArray.SetRow(2, m_camera.transform.TransformVector(m_frustumCorners[1]));  // top left
            m_frustumCornersArray.SetRow(3, m_camera.transform.TransformVector(m_frustumCorners[2]));  // top right
            m_frustumCornersArray.SetRow(1, m_camera.transform.TransformVector(m_frustumCorners[3]));  // bottom right
            m_fogComputationMaterial.SetMatrix(AzureShaderUniforms.FrustumCornersMatrix, m_frustumCornersArray);
            m_fogRendererMaterial.SetMatrix(AzureShaderUniforms.FrustumCornersMatrix, m_frustumCornersArray);

            // Compute the fog scattering effect and render it into a RenderTexture
            Graphics.Blit(null, m_fogScatteringRT, m_fogComputationMaterial, 0);

            //Blitter.BlitCameraTexture(cmd, sourceColor, m_DestinationColor, blitMaterial, -1);
            //Blitter.BlitCameraTexture(cmd, m_DestinationColor, sourceColor);

            //cmd.Blit(m_sourceColor, m_DestinationColor, blitMaterial, -1);
            //cmd.Blit(m_DestinationColor, m_sourceColor);

            // Render the fog scattering RenderTexture into the screen.
            m_fogRendererMaterial.SetTexture(AzureShaderUniforms.FogScatteringDataTexture, m_fogScatteringRT);
            cmd.Blit(m_sourceColor.rt, m_DestinationColor.rt, m_fogRendererMaterial, -1);
            cmd.Blit(m_DestinationColor.rt, m_sourceColor.rt);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    /// <summary>Sets the resolution of the render texture that stores fog scattering data.</summary>
    public void SetFogScatteringResolution(int width, int height)
    {
        m_fogRenderTextureWidth = width;
        m_fogRenderTextureHeight = height;

        m_fogScatteringRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        {
            name = "Fog Scattering RT",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        m_fogScatteringRT.Create();
    }
}