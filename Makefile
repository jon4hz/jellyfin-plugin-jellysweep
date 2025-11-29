.PHONY: help build clean package install test restore lint

# Default target
help:
	@echo "Jellyfin Plugin Jellysweep - Build System"
	@echo ""
	@echo "Available targets:"
	@echo "  build      - Build the plugin (Release configuration)"
	@echo "  debug      - Build the plugin (Debug configuration)"
	@echo "  clean      - Clean build artifacts"
	@echo "  package    - Build and create plugin package"
	@echo "  test       - Run tests and validation"
	@echo "  restore    - Restore NuGet packages"
	@echo "  lint       - Run code analysis"
	@echo "  install    - Install to local Jellyfin (requires sudo)"
	@echo "  version    - Update version (requires VERSION=x.y.z)"
	@echo ""
	@echo "Examples:"
	@echo "  make build"
	@echo "  make package"
	@echo "  make version VERSION=1.2.3"
	@echo "  make install"

# Build targets
build:
	cd build && ./build.sh --configuration Release

debug:
	cd build && ./build.sh --configuration Debug

package:
	cd build && ./build.sh --configuration Release --package

clean:
	cd build && ./build.sh --clean --configuration Release

# Development targets
restore:
	dotnet restore Jellyfin.Plugin.Jellysweep.sln

test: build
	@echo "Testing build output..."
	@if [ ! -f "Jellyfin.Plugin.Jellysweep/bin/Release/net9.0/Jellyfin.Plugin.Jellysweep.dll" ]; then \
		echo "Build test failed - DLL not found"; \
		exit 1; \
	fi
	@echo "Build test passed"

lint:
	dotnet build Jellyfin.Plugin.Jellysweep.sln \
		--configuration Release \
		--verbosity normal \
		/p:TreatWarningsAsErrors=true

# Version management
version:
ifndef VERSION
	@echo "Error: VERSION not specified"
	@echo "Usage: make version VERSION=1.2.3"
	@exit 1
endif
	cd build && ./update-version.sh $(VERSION)

# Installation
install: package
	@echo "Installing plugin to local Jellyfin..."
	sudo mkdir -p /var/lib/jellyfin/plugins/Jellysweep
	sudo cp bin/plugin/*.dll /var/lib/jellyfin/plugins/Jellysweep/
	@echo "Plugin installed. Restart Jellyfin to load the updated plugin."
	@echo "To restart Jellyfin: sudo systemctl restart jellyfin"

# Development helpers
dev-install: debug
	@echo "Installing debug build to local Jellyfin..."
	sudo mkdir -p /var/lib/jellyfin/plugins/Jellysweep
	sudo cp Jellyfin.Plugin.Jellysweep/bin/Debug/net9.0/Jellyfin.Plugin.Jellysweep.dll /var/lib/jellyfin/plugins/Jellysweep/
	# Copy Humanizer dependency for debug builds
	@if [ -f "Jellyfin.Plugin.Jellysweep/bin/Debug/net9.0/Humanizer.dll" ]; then \
		sudo cp Jellyfin.Plugin.Jellysweep/bin/Debug/net9.0/Humanizer.dll /var/lib/jellyfin/plugins/Jellysweep/; \
	fi
	@echo "Debug plugin installed. Restart Jellyfin to load the updated plugin."

uninstall:
	@echo "Removing plugin from local Jellyfin..."
	sudo rm -rf /var/lib/jellyfin/plugins/Jellysweep
	@echo "Plugin removed. Restart Jellyfin to complete removal."

# CI/CD helpers
ci-build: restore lint build test

ci-package: ci-build package
