# Changelog

## [0.1.0] - 2026-09-14

First cut.

- `TextureBlitRenderPipeline`: clear and draw one texture, via `DrawProcedural` rather than
  `CommandBuffer.Blit`. Aspect-fit letterboxing, flip toggle, no package dependencies.
- `ComputeExperiment` base class with resource ownership, ping-pong helpers, and a `Dispatch`
  that derives thread groups from the kernel's `[numthreads]`.
- `[ExperimentButton]` inspector buttons, replacing the EasyButtons dependency.
- `Tools > Minimal Project`: idempotent settings bootstrap, manifest pruner, experiment
  scaffolder, timed build.
