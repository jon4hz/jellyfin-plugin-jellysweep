using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Jellyfin.Plugin.Jellysweep.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.Jellysweep;

/// <summary>
/// The main plugin.
/// </summary>
public class JellysweepPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<JellysweepPlugin> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellysweepPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{T}"/> interface.</param>
    public JellysweepPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<JellysweepPlugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string Name => "Jellysweep";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("bfd3327c-74c8-4664-8473-4fd2a4258be4");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static JellysweepPlugin? Instance { get; private set; }

    /// <summary>
    /// Registers the JavaScript with the JavaScript Injector plugin.
    /// </summary>
    public void RegisterJavascript()
    {
        try
        {
            // Find the JavaScript Injector assembly
            Assembly? jsInjectorAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Plugin.JavaScriptInjector", StringComparison.Ordinal) ?? false);

            if (jsInjectorAssembly != null)
            {
                var customScriptPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.Web.jellysweep.js";
                var scriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(customScriptPath);
                if (scriptStream == null)
                {
                    _logger.LogError("Could not find embedded Jellysweep script at path: {Path}", customScriptPath);
                    return;
                }

                string scriptContent;
                using (var reader = new StreamReader(scriptStream))
                {
                    scriptContent = reader.ReadToEnd();
                }

                // Get the PluginInterface type
                Type? pluginInterfaceType = jsInjectorAssembly.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");
                if (pluginInterfaceType == null)
                {
                    _logger.LogError("Could not find PluginInterface type in JavaScript Injector assembly.");
                    return;
                }

                // Create the registration payload
                var scriptRegistration = new JObject
                {
                            { "id", $"{Id}-script" },
                            { "name", "Jellysweep Client Script" },
                            { "script", scriptContent },
                            { "enabled", true },
                            { "requiresAuthentication", true },
                            { "pluginId", Id.ToString() },
                            { "pluginName", Name },
                            { "pluginVersion", Version.ToString() }
                };

                // Register the script
                var registerResult = pluginInterfaceType.GetMethod("RegisterScript")?.Invoke(null, new object[] { scriptRegistration });

                // Validate the return value
                if (registerResult is bool success && success)
                {
                    _logger.LogInformation("Successfully registered JavaScript with JavaScript Injector plugin.");
                }
                else
                {
                    _logger.LogWarning("Failed to register JavaScript with JavaScript Injector plugin. RegisterScript returned false.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register JavaScript with JavaScript Injector plugin.");
        }
    }

    /// <inheritdoc />
    public override void OnUninstalling()
    {
        try
        {
            // Find the JavaScript Injector assembly
            Assembly? jsInjectorAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Plugin.JavaScriptInjector", StringComparison.Ordinal) ?? false);

            if (jsInjectorAssembly != null)
            {
                Type? pluginInterfaceType = jsInjectorAssembly.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");

                if (pluginInterfaceType != null)
                {
                    // Unregister all scripts from your plugin
                    var unregisterResult = pluginInterfaceType.GetMethod("UnregisterAllScriptsFromPlugin")?.Invoke(null, new object[] { Id.ToString() });

                    // Validate the return value
                    if (unregisterResult is int removedCount)
                    {
                        _logger?.LogInformation("Successfully unregistered {Count} script(s) from JavaScript Injector plugin.", removedCount);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to unregister scripts from JavaScript Injector plugin. Method returned unexpected value.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unregister JavaScript scripts.");
        }

        base.OnUninstalling();
    }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                DisplayName = "Jellysweep",
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
