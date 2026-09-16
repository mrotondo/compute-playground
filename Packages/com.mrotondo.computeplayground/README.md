# Compute Playground

A minimal scriptable render pipeline that puts one `RenderTexture` on screen, plus the
scaffolding a compute-shader experiment needs. No package dependencies.

## What is in here

| | |
|---|---|
| `TextureBlitRenderPipeline` | Clear, then draw one texture to the camera target. That is the whole pipeline. |
| `TextureBlitRenderPipelineAsset` | Holds the blit shader, clear colour, aspect-fit and flip toggles. |
| `ComputeExperiment` | Base class: owns resources, the stepping loop, and the hand-off to the pipeline. |
| `PingPong<T>` | Read/write pair for double-buffered textures or buffers. |
| `[ExperimentButton]` | Inspector buttons without a third-party package. |
| `Tools > Minimal Project` | Bootstrap, package pruning, scaffolding, timed build. |

## Writing an experiment

`Tools > Minimal Project > New Experiment...` generates a script, a `.compute` file, and a
scene wired to both. A subclass only implements two methods:

```csharp
public class Life : ComputeExperiment
{
    PingPong<RenderTexture> _state;
    int _stepKernel;

    protected override void Initialize()
    {
        _stepKernel = compute.FindKernel("Step");
        _state = CreatePingPongTextures();
        Display = _state.Read;          // this is what lands on screen
    }

    protected override void Step(int stepIndex)
    {
        compute.SetTexture(_stepKernel, "_StateRead", _state.Read);
        compute.SetTexture(_stepKernel, "_StateWrite", _state.Write);
        Dispatch(_stepKernel);          // thread groups derived from [numthreads]
        _state.Swap();
        Display = _state.Read;
    }
}
```

Anything from `CreateTexture`, `CreatePingPongTextures`, `CreateBuffer` or
`CreatePingPongBuffers` is released for you. Override `Teardown()` only for resources you
allocated yourself.

`Dispatch` reads the kernel's own `[numthreads]` via `GetKernelThreadGroupSizes`, so changing
`[numthreads(8,8,1)]` to `[numthreads(16,16,1)]` in the `.compute` file cannot silently leave
part of the grid undispatched.

## The bootstrap

`Tools > Minimal Project > Apply Settings` is idempotent. It creates and assigns the pipeline
asset, empties `AlwaysIncludedShaders` / `PreloadedShaders` / `RenderPipelineGlobalSettingsMap`,
collapses quality to one level, sets Mono + high managed stripping + a single graphics API, and
turns off domain and scene reload on entering play mode.

This exists as C# rather than as checked-in `ProjectSettings/*.asset` YAML on purpose: Unity
migrates those files in place and reformats them every version, which makes a carried-forward
delta unreadable. A script that breaks on upgrade produces a compile error on a named line.

`Tools > Minimal Project > Prune Packages` rewrites `manifest.json` down to the allowlist in
`ManifestPruner.Keep`, showing you what it will remove first.

## Notes

- **The image is upside down.** Toggle `flipY` on the pipeline asset. Which way is correct
  depends on graphics API and whether the camera has a target texture.
- **Point sampling.** The blit shader uses `SamplerState sampler_SourceTex`, which inherits the
  `RenderTexture`'s own `filterMode`, so the `filterMode` field on the experiment controls it.
- **No `Blitter`, no RenderGraph.** The pipeline does a manual `DrawProcedural` of one oversized
  triangle. That is what keeps the dependency list empty. If you later want
  `Blitter.BlitCameraTexture` or RenderGraph, add `com.unity.render-pipelines.core` back.
- **`CommandBuffer.Blit` is deliberately not used.** It is legacy API that mutates render state
  and is not SRP-safe.
