using System.Globalization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Jellysweep.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellysweep;

/// <summary>
/// The main plugin.
/// </summary>
public class JellysweepPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JellysweepPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{T}"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public JellysweepPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<JellysweepPlugin> logger,
        IServerConfigurationManager configurationManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        if (!string.IsNullOrWhiteSpace(applicationPaths.WebPath))
        {
            var indexFile = Path.Combine(applicationPaths.WebPath, "index.html");
            if (File.Exists(indexFile))
            {
                string indexContents = File.ReadAllText(indexFile);
                string basePath = string.Empty;

                // Get base path from network config
                try
                {
                    var networkConfig = configurationManager.GetConfiguration("network");
                    var configType = networkConfig.GetType();
                    var basePathField = configType.GetProperty("BaseUrl");
                    var confBasePath = basePathField?.GetValue(networkConfig)?.ToString()?.Trim('/');

                    if (!string.IsNullOrEmpty(confBasePath))
                    {
                        basePath = "/" + confBasePath.ToString();
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to get base path from config, using '/': {0}", e);
                }

                string scriptReplace = "<script plugin=\"Jellysweep\".*?></script>";
                string scriptElement = string.Format(CultureInfo.InvariantCulture, "<script plugin=\"Jellysweep\" version=\"1.0.0.0\" src=\"{0}/Plugins/Jellysweep/Static/ClientScript\" defer></script>", basePath);

                if (!indexContents.Contains(scriptElement, StringComparison.Ordinal))
                {
                    logger.LogInformation("Attempting to inject jellysweep script code in {0}", indexFile);

                    // Replace old Jellysweep scripts
                    indexContents = Regex.Replace(indexContents, scriptReplace, string.Empty);

                    // Insert script last in body
                    int bodyClosing = indexContents.LastIndexOf("</body>", StringComparison.Ordinal);
                    if (bodyClosing != -1)
                    {
                        indexContents = indexContents.Insert(bodyClosing, scriptElement);

                        try
                        {
                            File.WriteAllText(indexFile, indexContents);
                            logger.LogInformation("Finished injecting jellysweep script code in {0}", indexFile);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Encountered exception while writing to {0}: {1}", indexFile, e);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Could not find closing body tag in {0}", indexFile);
                    }
                }
                else
                {
                    logger.LogInformation("Found client script injected in {0}", indexFile);
                }
            }
        }
    }

    /// <inheritdoc />
    public override string Name => "Jellysweep";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("bfd3327c-74c8-4664-8473-4fd2a4258be4");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static JellysweepPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
