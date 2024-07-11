using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System;

public class AirDistortionFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
        public Material AirDistortionMaterial = null;
        
        /// <summary>
        /// 时间系数
        /// </summary>
        [Tooltip("时间系数")] public float DistortTimeFactor;

        /// <summary>
        /// 扭曲强度
        /// </summary>
        [Tooltip("扭曲强度")] public float DistortStrength;

        /// <summary>
        /// 噪声图
        /// </summary>
        [Tooltip("噪声图")] public Texture2D NoiseTex;
    }
    
    class AirDistortionRenderPass : ScriptableRenderPass, IDisposable
    {
        public Material blitMaterial = null;
        private RTHandle m_source;

        RTHandle m_TemporaryColorTexture;
        readonly string m_ProfilerTag;

        public void Dispose()
        {
            m_TemporaryColorTexture?.Release();
        }

    
        public AirDistortionRenderPass(RenderPassEvent renderPassEvent, Material blitMaterial)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            m_ProfilerTag = "AirDistortion01";
        }
    
        public void Setup(RTHandle source)
        {
            this.m_source = source;
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTexture, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_TemporaryColorTexture");
            ConfigureTarget(this.m_TemporaryColorTexture);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            cmd.Blit(m_source, m_TemporaryColorTexture, blitMaterial);
            cmd.Blit(m_TemporaryColorTexture, m_source);
        
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name));
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    public Settings settings = new();
    AirDistortionRenderPass _airDistortionRenderPass;
    
    private static readonly int DistortTimeFactor = Shader.PropertyToID("_DistortTimeFactor");
    private static readonly int DistortStrength = Shader.PropertyToID("_DistortStrength");
    private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

    public override void Create()
    {
        this.settings.AirDistortionMaterial.SetFloat(DistortTimeFactor, this.settings.DistortTimeFactor);
        this.settings.AirDistortionMaterial.SetFloat(DistortStrength, this.settings.DistortStrength);
        this.settings.AirDistortionMaterial.SetTexture(NoiseTex, this.settings.NoiseTex);
        _airDistortionRenderPass = new AirDistortionRenderPass(settings.Event, settings.AirDistortionMaterial);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var src = renderer.cameraColorTargetHandle;
        _airDistortionRenderPass.Setup(src);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_airDistortionRenderPass);
    }
}