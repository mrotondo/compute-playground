# compute-playground

A minimal scriptable render pipeline that puts one `RenderTexture` on screen, plus the
scaffolding a compute-shader experiment needs. The package lives at
`Packages/com.mrotondo.computeplayground/`; this project is where it is developed.

Embedded rather than consumed from git, because a package installed by git URL lands read-only
in `Library/PackageCache` and cannot be edited in place. Experiments consume a released
version of it.

## Starting a new experiment project

You need two files **and an empty `Assets/` folder**:

```
my-experiment/
  Assets/                             <- must exist, even empty
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt
```

Unity Hub reports a folder with no `Assets/` as an invalid project, even when everything else
is correct. (Batch mode is more forgiving and will open it regardless, so this is easy to miss
when scripting.) Nothing else is checked in — Unity regenerates all of `ProjectSettings/` with
defaults on first open, and `Apply Settings` takes it from there.

**`Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.mrotondo.computeplayground": "https://github.com/mrotondo/compute-playground.git?path=/Packages/com.mrotondo.computeplayground#main",
    "com.unity.ide.visualstudio": "2.0.23",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0"
  }
}
```

**`ProjectSettings/ProjectVersion.txt`**

```
m_EditorVersion: 6000.6.0f1
m_EditorVersionWithRevision: 6000.6.0f1 (f7f8ed4d1e24)
```

Then:

1. Unity Hub → Add → Add project from disk → pick the folder, open with 6000.6.0f1. The first
   open takes a minute while `ProjectSettings/` is rebuilt.
2. `Tools > Minimal Project > Apply Settings`. Nothing draws until this has run: it creates and
   assigns the pipeline asset and applies the stripping.
3. `Tools > Minimal Project > New Experiment...`, name it, Create. Unity recompiles, then opens
   a scene with the component added and the compute shader assigned.
4. Press Play. If the image is upside down, turn on `flipY` on
   `Assets/Rendering/TextureBlitRenderPipelineAsset`.
5. `git init` and commit.

If you started from a Unity Hub template instead of a bare folder, install the package through
Package Manager (+ → Add package from git URL), then run **Apply Settings before Prune
Packages**. Pruning first strips a pipeline package while `GraphicsSettings` still references
it, leaving dangling keys in `m_RenderPipelineGlobalSettingsMap` — which is the failure that
breaks a project on the next editor upgrade.

### Versioning

Every commit meant to be picked up bumps the patch version in `package.json` and carries a
matching `vX.Y.Z` tag. Package Manager shows that number, so a project can tell you what it is
actually running.

The recipe above pins `#main`, which is what makes Package Manager's **Update** button useful:
it re-resolves the ref, and for a branch that means the newest commit. Pinning a tag instead —
`#v0.1.1` — freezes a project at that release; Update then re-resolves the same tag and changes
nothing, and moving up means editing `manifest.json` by hand. Pin a tag when a project needs to
stop moving, track `main` while it is still being built.

Either way, **Unity never re-fetches on its own.** The resolved commit is recorded in
`Packages/packages-lock.json` and cached in `Library/PackageCache`, and it is authoritative: a
project sitting on an old commit stays there through any number of pushes until you press
Update, or delete that package's entry from the lock.

Note that git dependencies get no semver resolution. Unity matches the ref literally, so
`#v0.1.1` means that tag and nothing else — there are no ranges and nothing for a resolver to
choose between. The version number is documentation and a name to pin to, not a constraint.

## Why these packages and no others

`ManifestPruner.Keep` in the package is the source of truth. `imgui`, `uielements`, `ui` and
`jsonserialize` are load-bearing for the editor's own windows. `imageconversion` and
`screencapture` are for saving frames out. `ide.visualstudio` generates the `.csproj` files your
editor reads — swap it for `com.unity.ide.rider` if you prefer.

Everything else Unity installs by default — terrain, particles, audio, video, XR, visual
scripting, timeline, uGUI, the five `unitywebrequest` modules — is absent, and absent packages
cost nothing to compile, import, or build.

One wrinkle: those seven requests resolve to **thirteen** packages.
`com.unity.ide.visualstudio` depends on `com.unity.test-framework`, which drags in
`com.unity.ext.nunit` plus the `animation` and `physics` modules; `uielements` pulls
`hierarchycore`. If you want those gone, drop the IDE package — the cost is that Unity stops
generating `.csproj` files, so your editor loses code completion. That is usually the wrong
trade, but it is the only thing still standing between this and a truly bare project.

## Working on the package

Open this folder in Unity 6000.6.0f1, then `Tools > Minimal Project > Apply Settings`.

`Assets/Experiments/Cyclic` is a smoke test: a cyclic cellular automaton generated by
`New Experiment...`, kept so there is always one known-good experiment to run.

### Smoke test after a change

Two batch-mode invocations, because the generated script has to compile between them:

```sh
UNITY=E:/Unity/6000.6.0f1/Editor/Unity.exe
"$UNITY" -batchmode -quit -nographics -projectPath . \
  -executeMethod Mrotondo.ComputePlayground.Editor.MinimalProjectBootstrap.Apply -logFile -
"$UNITY" -batchmode -quit -nographics -projectPath . \
  -executeMethod Mrotondo.ComputePlayground.Editor.HeadlessBuild.Run -logFile -
```

`HeadlessBuild` prints the ten slowest build steps, which is where to look when a build starts
feeling heavy. Baseline on 6000.6.0f1, Mono, D3D12 only, empty `AlwaysIncludedShaders`:

```
Timed build: Succeeded in 17.5s, 69MB, 0 errors
  17.1s  Build player
  12.3s  Postprocess built player
   2.7s  ProducePlayerScriptAssemblies
   2.2s  Compile scripts
```

Batch mode runs headless, so this proves the project compiles, imports and builds. It cannot
tell you whether the picture is right way up.

## Releasing

Unity generates `.meta` files for the package on first import. **Commit them.** They carry the
GUIDs that consuming projects reference; if they are not in the repo, every consumer generates
different GUIDs and asset references break.

Bump the patch version in `package.json`, commit, tag, and push both:

```sh
git add Packages/com.mrotondo.computeplayground
git commit -m "..."
git tag -a v0.1.2 -m "v0.1.2"
git push --follow-tags
```

Use `--follow-tags`, not `--tags`. `--tags` adds tags to the refspecs *explicitly listed on the
command line*, and does not add them to the default refspec — so a bare `git push --tags`
pushes the tags and silently skips the branch, leaving the remote with a tag pointing at a
commit nobody can see. `--follow-tags` keeps the normal branch push and brings along annotated
tags reachable from it, which is why the tag above is annotated (`-a`); `--follow-tags` ignores
lightweight ones.

Only changes inside `Packages/com.mrotondo.computeplayground/` need a version bump and tag.
Edits to this README or to the development project are not part of the package and can just be
pushed.

Bump the **minor** version rather than the patch when something changes shape — a renamed or
removed serialized field, a changed component layout — because Unity silently drops serialized
values that no longer match, and an experiment will come back with defaults where it had your
settings. Replacing `stepsPerFrame`/`frameInterval` with `speed` was one of those.

Experiments pick a release up as described under [Versioning](#versioning).

## Moving to a new editor version

1. Bump `ProjectSettings/ProjectVersion.txt`, open, let Unity migrate.
2. Fix whatever `MinimalProjectBootstrap` no longer compiles against. This is the point of the
   whole arrangement: the breakage arrives as a compile error on a named line, in one repo,
   once — not as silent misbehaviour in every experiment project you own.
3. Bump `unity` and the **minor** version in `package.json`, tag, and push. Then bump each
   experiment's `ProjectVersion.txt` to match as you move it up; experiments you leave alone
   stay on the commit their lock file names.
