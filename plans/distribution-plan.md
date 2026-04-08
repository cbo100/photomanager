# Distribution Plan

## Phase 1: Native AOT / Standalone Binaries

### Goal

Produce self-contained single-file binaries for `win-x64`, `win-arm64`, `linux-x64`, `osx-x64`, and `osx-arm64` that run without the .NET runtime installed, and publish them to GitHub Releases automatically on version tags.

### AOT Compatibility

| Library | Status | Notes |
|---|---|---|
| Spectre.Console.Cli | ✅ | Full AOT support since v0.50 |
| System.IO.Abstractions | ✅ | Trimmer-annotated |
| MetadataExtractor | ⚠️ | Uses reflection internally — needs `ILLink.Descriptors.xml` |
| NSubstitute | ✅ | Test-only, not included in publish output |

### Steps

#### 1.1 — Configure CLI csproj for AOT and dotnet global tool

Add to `PhotoManager.Cli.csproj`:

```xml
<PropertyGroup>
  <!-- Package metadata -->
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>photomanager</ToolCommandName>
  <Version>0.1.0</Version>
  <Authors>cbo100</Authors>
  <Description>CLI tool for organising photos by EXIF metadata</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryUrl>https://github.com/cbo100/photomanager</RepositoryUrl>
  <PackageTags>photos;exif;cli;organise</PackageTags>

  <!-- AOT -->
  <PublishAot>true</PublishAot>
  <OptimizationPreference>Size</OptimizationPreference>
  <StripSymbols>true</StripSymbols>
  <TrimmerSingleWarn>false</TrimmerSingleWarn>
</PropertyGroup>
```

Keep `InvariantGlobalization=false` — required for culture-aware location formatting.

#### 1.2 — Add ILLink.Descriptors.xml for MetadataExtractor

Create `src/PhotoManager.Cli/ILLink.Descriptors.xml` to preserve the reflection targets that MetadataExtractor needs at runtime. Wire it up via:

```xml
<ItemGroup>
  <TrimmerRootDescriptor Include="ILLink.Descriptors.xml" />
</ItemGroup>
```

#### 1.3 — Test AOT build locally

```bash
dotnet publish src/PhotoManager.Cli -c Release -r osx-arm64 --self-contained -p:PublishAot=true -o ./publish
./publish/photomanager organise --help
```

Iterate until:
- No IL trim errors (warnings are acceptable if the feature still works)
- `scan`, `organise --dry-run`, and `organise --mode move` all function correctly

#### 1.4 — GitHub Actions release workflow

New file: `.github/workflows/release.yml`

- **Trigger:** push of a `v*` tag (e.g. `v0.1.0`)
- **Matrix:**

  | Runner | RID |
  |---|---|
  | `windows-latest` | `win-x64` |
  | `windows-latest` | `win-arm64` |
  | `ubuntu-latest` | `linux-x64` |
  | `macos-latest` | `osx-x64` |
  | `macos-latest` | `osx-arm64` |

- **Each job:**
  1. `dotnet publish` with AOT for the target RID
  2. Archive: `.zip` on Windows, `.tar.gz` on Unix
  3. Upload archive to the GitHub Release

- **NuGet publish job** (runs after matrix, framework-dependent):
  1. `dotnet pack -c Release`
  2. `dotnet nuget push` → NuGet.org
  3. Requires `NUGET_API_KEY` secret

---

## Phase 2: Package Manager Distribution

*(Deferred until Phase 1 binaries are validated)*

### dotnet global tool

Once published to NuGet.org:

```bash
dotnet tool install -g photomanager
```

### winget

- Fork `microsoft/winget-pkgs`
- Create manifest under `manifests/c/cbo100/photomanager/<version>/`
- Three YAML files: version, locale (en-US), installer
- Installer entries point to GitHub Release `.exe` for `win-x64` and `win-arm64`
- Submit PR; SHA256s are generated from release artifacts

### Homebrew

- Create `github.com/cbo100/homebrew-photomanager` repo
- Add `Formula/photomanager.rb` with `on_macos`/`on_linux`/`on_arm`/`on_intel` blocks
- Each block points to the relevant GitHub Release `.tar.gz`
- Add a step to the release workflow that auto-updates SHA256s and pushes to the tap

Users install via:
```bash
brew tap cbo100/photomanager
brew install photomanager
```

### Required secrets

| Secret | Purpose |
|---|---|
| `NUGET_API_KEY` | Push NuGet package |
| `HOMEBREW_PAT` | Write access to homebrew tap repo |
