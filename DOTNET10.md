# Upgrading to .NET 10

This project has been configured for **.NET 10** with all the latest features enabled.

## Current Status

✅ All projects updated to target `net10.0`  
✅ `global.json` configured to require .NET 10 SDK  
✅ `Directory.Build.props` added with .NET 10 optimizations  
✅ AOT-compatible configuration enabled  
✅ Latest C# language features enabled  
✅ Performance optimizations enabled  

## What's Enabled

### Performance Features
- **Tiered Compilation** - Faster startup and better steady-state performance
- **Quick JIT** - Reduced startup time
- **AOT Ready** - Projects marked as AOT-compatible for future native compilation

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

## Testing AOT Compatibility

Once .NET 10 is installed, test AOT compilation:

```bash
# Publish with AOT
dotnet publish src/PhotoManager.Cli/PhotoManager.Cli.csproj \
  -c Release \
  -r linux-x64 \
  -p:PublishAot=true

# Test the native binary
./src/PhotoManager.Cli/bin/Release/net10.0/linux-x64/publish/PhotoManager.Cli
```

## Benefits of .NET 10

- **Better Performance** - Up to 20% faster than .NET 9
- **Smaller Binaries** - With AOT enabled
- **Lower Memory** - Improved GC and memory management
- **New APIs** - Latest BCL improvements
- **Better Diagnostics** - Enhanced debugging and profiling

## Notes

The project will **require .NET 10 SDK** to build once you pull these changes. The `global.json` file enforces this requirement to ensure everyone uses the same SDK version.
