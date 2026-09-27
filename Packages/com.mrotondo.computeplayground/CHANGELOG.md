# Changelog

## [0.1.2] - 2026-09-26

- `TextureBlitRenderPipeline` keeps one `CommandBuffer` for its lifetime and clears it each
  frame, rather than allocating and releasing one every `Render`. `CommandBufferPool` would do
  the same, but it lives in the SRP Core package and this package has no dependencies.

## [0.1.1] - 2026-09-26

**Breaking:** `stepsPerFrame` and `frameInterval` are replaced by a single float `speed`. Unity
silently drops serialized values whose fields no longer exist, so existing experiments come back
at the default of 1. By the rule in the README this should have been a minor bump, not a patch.

- `speed`: at or above 1, `floor(speed)` steps run per frame; below 1, one step runs every
  `floor(1/speed)` frames; 0 pauses and `Step Once` advances by hand.
- Experiments no longer run in edit mode. `[ExecuteAlways]` and the editor-side ticker that
  propped it up are both gone, matching how the original controllers worked.
- The cyclic CA template stores state as a raw index instead of a fraction of `_StateCount`.
  Normalising made every stored value depend on a live inspector parameter, so dragging State
  Count mid-run reinterpreted the whole field as noise with no recovery short of Reset.
- The cyclic CA template defaults `threshold` to 1. At 2, an isolated wavefront cannot recruit
  its neighbour, which suppresses spirals and parks the automaton in the debris phase.
- `showTestPattern` draws an F-shaped orientation card. Noise is symmetric in both axes, so
  nothing the simulation produces can reveal whether `flipY` is set correctly.
- `TextureBlitRenderPipelineAsset` derives from `RenderPipelineAsset<T>` and overrides
  `renderPipelineShaderTag`, silencing two warnings logged on every `Apply Settings`.
- `TextureBlitRenderPipeline.Source` widened from `RenderTexture` to `Texture`.
- The custom inspector resolves `[ExperimentButton]` methods once per type through `TypeCache`
  instead of reflecting over the target on every IMGUI event, which had been costing hundreds
  of allocating reflection calls per mouse-move.

## [0.1.0] - 2026-09-14

First cut.

- `TextureBlitRenderPipeline`: clear and draw one texture, via `DrawProcedural` rather than
  `CommandBuffer.Blit`. Aspect-fit letterboxing, flip toggle, no package dependencies.
- `ComputeExperiment` base class with resource ownership, ping-pong helpers, and a `Dispatch`
  that derives thread groups from the kernel's `[numthreads]`.
- `[ExperimentButton]` inspector buttons, replacing the EasyButtons dependency.
- `Tools > Minimal Project`: idempotent settings bootstrap, manifest pruner, experiment
  scaffolder, timed build.
