using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.TheSportsDB.Providers.ExternalId
{
    /// <inheritdoc />
    public class ProviderSeriesExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => TheSportsDBPlugin.ProviderName;

        /// <inheritdoc />
        public string Key => TheSportsDBPlugin.ProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Series;
    }
}
