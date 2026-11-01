# Jellyfin Plugin: Jellysweep

A Jellyfin plugin for sweeping and cleaning media libraries.

## Features

- Media library cleanup and organization
- Web-based configuration interface
- Integration with Jellyfin's admin dashboard

## Building

### Prerequisites

- .NET 9.0 SDK
- Linux/Unix environment with bash
- zip utility (for packaging)

### Build Commands

The project includes build scripts for easy compilation:

```bash
# Navigate to build directory
cd build

# Make scripts executable (first time only)
chmod +x *.sh

# Build the plugin
./build.sh --configuration Release --package

# Or build with version update
./build.sh --configuration Release --package --version 1.0.0
```

### Build Options

- `--configuration Release|Debug`: Build configuration (default: Release)
- `--clean`: Clean before build
- `--package`: Create plugin package (zip file)
- `--version <version>`: Update version before build

### Manual Build

If you prefer to build manually:

```bash
# Restore packages
dotnet restore Jellyfin.Plugin.Jellysweep.sln

# Build
dotnet build Jellyfin.Plugin.Jellysweep.sln --configuration Release

# Publish
dotnet publish Jellyfin.Plugin.Jellysweep.sln --configuration Release
```

## Installation

### From Release

1. Download the latest `jellyfin-plugin-jellysweep-*.zip` from the [releases page](https://github.com/jon4hz/jellyfin-plugin-jellysweep/releases)
2. Extract the contents to your Jellyfin plugins directory:
   - Linux: `/var/lib/jellyfin/plugins/Jellysweep/`
   - Docker: `/config/plugins/Jellysweep/`
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\Jellysweep\`
3. Restart Jellyfin
4. Enable the plugin in the Jellyfin admin dashboard under **Dashboard > Plugins**

**Note:** The plugin package contains only the necessary files:

- `Jellyfin.Plugin.Jellysweep.dll` - The main plugin
- Third-party dependencies (e.g., `Humanizer.dll`) - Automatically detected

The build system automatically excludes all Jellyfin built-in assemblies (Jellyfin._, MediaBrowser._, Microsoft.Extensions._, System._, etc.) and includes only the plugin DLL and legitimate third-party dependencies.

### From Source

After building:

```bash
# Copy the plugin files to Jellyfin (using the package contents)
sudo mkdir -p /var/lib/jellyfin/plugins/Jellysweep
sudo cp bin/plugin/*.dll /var/lib/jellyfin/plugins/Jellysweep/

# Or use the convenience command
make install

# Restart Jellyfin
sudo systemctl restart jellyfin
```

## Development

### Project Structure

```
Jellyfin.Plugin.Jellysweep/
├── Api/                    # API controllers
│   ├── JellysweepController.cs
│   ├── StaticController.cs
│   └── Responses/          # API response models
├── Configuration/          # Plugin configuration
│   ├── configPage.html     # Admin UI
│   └── PluginConfiguration.cs
├── Services/               # Business logic
│   ├── JellysweepApiService.cs
│   └── Requests/           # Service request models
│   └── Responses/          # Service response models
├── Web/                    # Static web assets
│   ├── jellysweep.js      # Client-side JavaScript
│   └── jellysweep.png     # Plugin icon
└── JellysweepPlugin.cs    # Main plugin class
```

### Versioning

The project uses semantic versioning (SemVer). Version numbers are automatically updated during the build process when using the build scripts.

To manually update the version:

```bash
cd build
./update-version.sh 1.2.3
```

This updates:

- Assembly version in the project file
- Plugin version in the project file
- Version in the injected client script

### Packaging Logic

The plugin packaging is entirely handled by MSBuild targets in the `.csproj` file. The build system uses an **inclusion approach**:

1. **Publishes** all dependencies to the publish directory
2. **Includes** only specifically listed files:
   - The main plugin DLL (`Jellyfin.Plugin.Jellysweep.dll`)
   - Explicitly listed third-party dependencies (e.g., `Humanizer.dll`)
3. **Ignores** everything else (all Jellyfin built-ins are automatically excluded)
4. **Creates** a ZIP package with only the required files

To add new third-party dependencies to the package, simply add them to the `ThirdPartyDependencies` list in the `.csproj` file:

```xml
<ItemGroup>
  <ThirdPartyDependencies Include="Humanizer" />
  <ThirdPartyDependencies Include="YourNewDependency" />
</ItemGroup>
```

This approach is much simpler than maintaining exclusion lists and ensures only intentionally bundled dependencies are included.

### Testing

Build and test the plugin:

```bash
# Build in debug mode
./build.sh --configuration Debug

# Test the build output
ls -la ../Jellyfin.Plugin.Jellysweep/bin/Debug/net10.0/
```

## Continuous Integration

The workflows in `.github/workflows/` are thin callers of the shared reusable workflows in
[jon4hz/jellyfin-plugin-ci](https://github.com/jon4hz/jellyfin-plugin-ci):

- **CI** (`ci.yml`): build with warnings as errors plus pre-commit on every push/PR
- **Release** (`release.yml`): triggered on tag creation, builds the package, creates the GitHub release and updates `manifest.json`
- **Update pre-commit hooks** (`update-pre-commit.yml`): weekly `pre-commit autoupdate` PR

### Creating a Release

1. Commit your changes with conventional commit messages:

   ```bash
   git commit -m "feat: add new cleanup feature"
   git commit -m "fix: resolve library scanning issue"
   ```

2. Create and push a tag:

   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. The GitHub Action will automatically:
   - Build the plugin
   - Generate release notes from conventional commits
   - Create a GitHub release with the plugin package
   - Calculate and include checksums

### Conventional Commits

The release notes are generated from conventional commit messages:

- `feat:` - New features (🚀 Features section)
- `fix:` - Bug fixes (🐛 Bug Fixes section)
- `feat!:` or `fix!:` - Breaking changes (💥 Breaking Changes section)
- Other types go in 📝 Other Changes section

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes with proper commit messages
4. Test your changes
5. Submit a pull request

## Support

For issues and questions:

- Create an issue on GitHub
- Check existing issues for solutions
- Provide logs and system information when reporting bugs
