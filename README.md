# Build Artifacts

This orphan branch contains generated `Mcl.Core.dll` builds for the public
`WPFLauncher_Hook` project.

- Do not merge this branch into `main`.
- Do not commit source code, private dependencies, or account data here.
- The only generated file is `build/Mcl.Core.dll`.
- Artifact commits reuse the triggering source commit message and include an
  `MCL-SOURCE-COMMIT` marker.
- `HookUpdateManager` reads this branch through the GitHub Contents API.
