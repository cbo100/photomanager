# Upgrading to .NET 10

This project has been configured for **.NET 10** with all the latest features enabled.

## Current Status

✅ All projects updated to target `net10.0`  
✅ `global.json` configured to require .NET 10 SDK  
✅ `Directory.Build.props` added with .NET 10 optimizations  
❌ AOT disabled - Spectre.Console.Cli is not AOT-compatible  
✅ Latest C# language features enabled  
✅ Performance optimizations enabled  

## What's Enabled

### Performance Features
- **Tiered Compilation** - Faster startup and better steady-state performance
- **Quick JIT** - Reduced startup time

### Code Quality
- **Latest C#** - Using the newest C# language features
- **Nullable Reference Types** - Enabled across all projects
- **.NET Analyzers** - Latest analysis level for code quality
- **Code Style Enforcement** - Enforced during build

### Build Improvements
- **Deterministic Builds** - Reproducible builds
- **CI Optimizations** - Special handling for CI environments

## Installing .NET 10

Once .NET 10 is released or you have preview access:

### Ubuntu/Debian
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

### Windows
Download from: https://dotnet.microsoft.com/download/dotnet/10.0

### macOS
```bash
brew install --cask dotnet-sdk
```

### Verify Installation
```bash
dotnet --version
# Should show 10.0.x

dotnet --list-sdks
# Should include 10.0.x
```

## Current .NET 9 Compatibility

If you need to build with .NET 9 temporarily:

1. Remove or modify `global.json`:
```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

2. Change all `net10.0` to `net9.0` in .csproj files

## Benefits of .NET 10

- **Better Performance** - Up to 20% faster than .NET 9
- **Lower Memory** - Improved GC and memory management
- **New APIs** - Latest BCL improvements
- **Better Diagnostics** - Enhanced debugging and profiling
## AOT Compatibility Note

**Native AOT is currently disabled** because Spectre.Console.Cli uses reflection and is not compatible with AOT compilation. If AOT is a requirement, consider switching to a different CLI framework like System.CommandLine or implementing a custom command parser.

`IsAotCompatible` and `PublishAot` are set to `false` in the project configuration.
## Notes

The project will **require .NET 10 SDK** to build once you pull these changes. The `global.json` file enforces this requirement to ensure everyone uses the same SDK version.
