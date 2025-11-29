# Jellyfin Plugin Jellysweep - Development Guide

## Build/Test Commands

```bash
# Build the solution
dotnet build Jellyfin.Plugin.Jellysweep.sln

# Build in release mode
dotnet build Jellyfin.Plugin.Jellysweep.sln -c Release

# Clean build artifacts
dotnet clean Jellyfin.Plugin.Jellysweep.sln

# Restore packages
dotnet restore Jellyfin.Plugin.Jellysweep.sln
```

## Code Style Guidelines

### Project Structure

- Target Framework: .NET 9.0
- Nullable reference types enabled
- Implicit usings enabled
- Documentation generation enabled
- Treat warnings as errors

### Naming Conventions

- PascalCase for classes, methods, properties, namespaces
- camelCase for parameters, local variables
- Private fields prefixed with underscore (\_fieldName)
- Static readonly fields in PascalCase

### Code Analysis

- Uses jellyfin.ruleset with StyleCop.Analyzers
- CA1305 (IFormatProvider) enforced as error
- CA2016 (CancellationToken forwarding) enforced as error
- Documentation comments required for public APIs
- Primary constructors preferred for dependency injection

### Error Handling

- Use structured logging with ILogger
- ConfigureAwait(false) for async calls
- Proper CancellationToken propagation
- Try-catch with specific exception handling
