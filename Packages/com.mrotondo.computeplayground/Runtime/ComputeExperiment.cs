using System.Collections.Generic;
using UnityEngine;

namespace Mrotondo.ComputePlayground
{
    /// <summary>
    /// Base class for a compute-shader experiment. Owns the resources, the stepping loop,
    /// and the hand-off to the render pipeline, so a subclass only writes Initialize() and Step().
    /// </summary>
    public abstract class ComputeExperiment : MonoBehaviour
    {
        [Header("Simulation")]
        [SerializeField] protected ComputeShader compute;
        [SerializeField, Range(16, 8192)] protected int resolution = 512;

        [Header("Stepping")]
        [Tooltip("Simulation steps per second, independent of framerate: 60 runs sixty steps a " +
                 "second whether you render at 60 or 500, and 0.2 runs one every five seconds. " +
                 "Ask for more than the GPU can manage and it simply falls behind. 0 pauses; " +
                 "Step Once then advances by hand.")]
        [SerializeField, Min(0f)] protected float stepsPerSecond = 60f;

        [Header("Display")]
        [SerializeField] protected FilterMode filterMode = FilterMode.Point;
        [SerializeField] protected RenderTextureFormat format = RenderTextureFormat.ARGBFloat;

        [Tooltip("Show an orientation test card instead of the simulation. The F should read " +
                 "the right way round; if it does not, toggle flipY on the pipeline asset.")]
        [SerializeField] protected bool showTestPattern;

        readonly List<RenderTexture> _textures = new List<RenderTexture>();
        readonly List<GraphicsBuffer> _buffers = new List<GraphicsBuffer>();

        /// <summary>
        /// Ceiling on the time a single frame may contribute. Without it a slow frame requests a
        /// proportionally larger batch on the next one, and that feedback loop is geometric: ask
        /// for ten times what the GPU can manage and the editor hangs within a few frames. Unity
        /// bounds FixedUpdate the same way, via Time.maximumDeltaTime.
        /// </summary>
        const float MaxDelta = 0.1f;

        RenderTexture _display;
        Texture2D _testPattern;
        float _stepAccumulator;
        ComputeShader _builtWithCompute;
        int _builtAtResolution;
        bool _builtWithTestPattern;
        bool _rebuildQueued;

        /// <summary>
        /// False wherever there is no usable graphics device, such as a headless batch-mode run.
        /// </summary>
        static bool CanRunCompute =>
            SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null &&
            SystemInfo.supportsComputeShaders;

        public ComputeShader Compute => compute;
        public int Resolution => resolution;
        public int StepCount { get; private set; }

        /// <summary>The texture the pipeline puts on screen. Assign this in Initialize().</summary>
        protected RenderTexture Display
        {
            get => _display;
            set
            {
                _display = value;
                TextureBlitRenderPipeline.Source = value;
            }
        }

        /// <summary>Allocate textures and buffers, seed initial state, and set Display.</summary>
        protected abstract void Initialize();

        /// <summary>Advance the simulation by one step.</summary>
        protected abstract void Step(int stepIndex);

        /// <summary>Optional. Anything created via the Create* helpers is released for you.</summary>
        protected virtual void Teardown() { }

        void OnEnable() => Rebuild();

        void OnDisable() => ReleaseAll();

        void OnValidate()
        {
            // Don't touch GPU resources from inside OnValidate; defer to the next Update.
            if (_builtAtResolution != resolution ||
                _builtWithCompute != compute ||
                _builtWithTestPattern != showTestPattern)
                _rebuildQueued = true;
        }

        void Update()
        {
            if (_rebuildQueued)
            {
                _rebuildQueued = false;
                Rebuild();
            }

            if (showTestPattern || compute == null || stepsPerSecond <= 0f)
                return;

            _stepAccumulator += Mathf.Min(Time.unscaledDeltaTime, MaxDelta);

            int steps = Mathf.FloorToInt(_stepAccumulator * stepsPerSecond);
            if (steps <= 0)
                return;

            // Subtracting exactly what ran leaves less than one step's worth behind, so the
            // accumulator cannot bank time and dump a burst later.
            _stepAccumulator -= steps / stepsPerSecond;

            for (int i = 0; i < steps; i++)
                StepOnce();
        }

        [ExperimentButton("Reset")]
        public void Rebuild()
        {
            ReleaseAll();

            _builtWithTestPattern = showTestPattern;
            if (showTestPattern)
            {
                _testPattern = TestPattern.Create();
                TextureBlitRenderPipeline.Source = _testPattern;
                return;
            }

            // Silent: a freshly added component has no shader yet, and the inspector shows that.
            if (compute == null || !CanRunCompute)
                return;

            _builtWithCompute = compute;
            _builtAtResolution = resolution;
            _stepAccumulator = 0f;
            StepCount = 0;
            Initialize();

            // Initialize() may not have set Display; make sure the pipeline agrees either way.
            TextureBlitRenderPipeline.Source = _display;
        }

        [ExperimentButton("Step Once")]
        public void StepOnce()
        {
            if (showTestPattern || compute == null || !CanRunCompute)
                return;

            Step(StepCount);
            StepCount++;
        }

        /// <summary>
        /// Looks up a kernel by name, reporting a missing one as an inspector-clickable error
        /// rather than the bare ArgumentException ComputeShader.FindKernel throws.
        /// Returns -1 if absent; Dispatch ignores a negative kernel.
        /// </summary>
        protected int FindKernel(string kernelName)
        {
            if (compute == null)
                return -1;

            if (!compute.HasKernel(kernelName))
            {
                Debug.LogError($"{name}: kernel '{kernelName}' not found in {compute.name}.", this);
                return -1;
            }

            return compute.FindKernel(kernelName);
        }

        /// <summary>A square R/W RenderTexture at the experiment's resolution.</summary>
        protected RenderTexture CreateTexture(int size = 0, RenderTextureFormat? formatOverride = null)
        {
            int side = size > 0 ? size : resolution;

            var texture = new RenderTexture(side, side, 0, formatOverride ?? format)
            {
                enableRandomWrite = true,
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Repeat,
                useMipMap = false,
                autoGenerateMips = false,
            };
            texture.Create();

            _textures.Add(texture);
            return texture;
        }

        /// <summary>Two matching textures to swap between steps.</summary>
        protected PingPong<RenderTexture> CreatePingPongTextures(int size = 0, RenderTextureFormat? formatOverride = null) =>
            new PingPong<RenderTexture>(CreateTexture(size, formatOverride), CreateTexture(size, formatOverride));

        protected GraphicsBuffer CreateBuffer(int count, int stride,
            GraphicsBuffer.Target target = GraphicsBuffer.Target.Structured)
        {
            var buffer = new GraphicsBuffer(target, Mathf.Max(1, count), stride);
            _buffers.Add(buffer);
            return buffer;
        }

        protected PingPong<GraphicsBuffer> CreatePingPongBuffers(int count, int stride,
            GraphicsBuffer.Target target = GraphicsBuffer.Target.Structured) =>
            new PingPong<GraphicsBuffer>(CreateBuffer(count, stride, target), CreateBuffer(count, stride, target));

        /// <summary>
        /// Dispatch, deriving thread-group counts from the kernel's own [numthreads] so that
        /// changing numthreads in the .compute file does not silently under- or over-dispatch.
        /// Defaults to covering a resolution x resolution grid.
        /// </summary>
        protected void Dispatch(int kernel, int width = 0, int height = 0, int depth = 1)
        {
            if (kernel < 0 || compute == null || !CanRunCompute)
                return;

            if (width <= 0) width = resolution;
            if (height <= 0) height = resolution;

            compute.GetKernelThreadGroupSizes(kernel, out uint sizeX, out uint sizeY, out uint sizeZ);

            compute.Dispatch(kernel,
                Mathf.CeilToInt(width / (float)sizeX),
                Mathf.CeilToInt(height / (float)sizeY),
                Mathf.CeilToInt(depth / (float)sizeZ));
        }

        void ReleaseAll()
        {
            Teardown();

            foreach (RenderTexture texture in _textures)
            {
                if (texture == null) continue;
                texture.Release();
                if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture);
            }
            _textures.Clear();

            foreach (GraphicsBuffer buffer in _buffers)
                buffer?.Release();
            _buffers.Clear();

            if (TextureBlitRenderPipeline.Source == _display ||
                TextureBlitRenderPipeline.Source == _testPattern)
                TextureBlitRenderPipeline.Source = null;

            if (_testPattern != null)
            {
                if (Application.isPlaying) Destroy(_testPattern); else DestroyImmediate(_testPattern);
                _testPattern = null;
            }

            _display = null;
        }
    }
}
