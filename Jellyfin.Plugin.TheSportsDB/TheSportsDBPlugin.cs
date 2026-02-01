using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.TheSportsDB.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.TheSportsDB;

/// <summary>
/// The main plugin.
/// </summary>
public class TheSportsDBPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public const string ProviderName = "TheSportsDB";

    /// <summary>
    /// Gets the provider id.
    /// </summary>
    public const string ProviderId = "thesportsdb";

    /// <summary>
    /// Initializes a new instance of the <see cref="TheSportsDBPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public TheSportsDBPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "TheSportsDB";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("df975f2f-25c9-4f1f-9d86-c01272bbd356");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static TheSportsDBPlugin Instance { get; private set; } = null!;

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
