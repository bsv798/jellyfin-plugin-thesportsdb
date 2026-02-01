using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TheSportsDB.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Default TSDB REST API key.
    /// </summary>
    public static readonly string DefaultApiKey = "123";

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ApiKey = DefaultApiKey;
    }

    /// <summary>
    /// Gets or sets the TSDB REST API key.
    /// </summary>
    public string ApiKey { get; set; }
}
