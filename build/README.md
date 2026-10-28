# Build Assets

Generated `Mcl.Core.dll` builds are not tracked on `main`. The `Build Mcl.Core`
workflow publishes them to the orphan `build-artifacts` branch at
`build/Mcl.Core.dll`.

`HookUpdateManager` reads that branch and pins downloads to the exact artifact
commit returned by GitHub. Do not add a generated `Mcl.Core.dll` to `main`.

The following approved native runtime dependencies remain tracked on `main`:

- `ocr.dll`
- `OcrProxy.dll`
- `wintun.dll`
- `WmCode.dll`

## Workflow Setup

- Private dependency repository:
  `daijunhaoMinecraft/WPFLauncher_Hook_Build`.
- Repository secret: `WPFLAUNCHER_HOOK_BUILD_DEPLOY_KEY`.
- The secret is a read-only SSH deploy key scoped to the private repository.
- The workflow runs on `main` pushes that change production source, tests,
  scripts, or the workflow itself. It can also be started manually.
- The workflow publishes only `build/Mcl.Core.dll` to `build-artifacts`.
- Artifact commits reuse the triggering source commit message and append an
  `MCL-SOURCE-COMMIT` marker so the updater can show useful release notes.
- Rapid pushes cancel outdated runs. A run also verifies that its source commit
  is still current on `main` before publishing, so an obsolete build cannot
  overwrite a newer artifact.
