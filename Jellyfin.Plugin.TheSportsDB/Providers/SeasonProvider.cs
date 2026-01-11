using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB.Providers
{
    /// <summary>
    /// Season Provider.
    /// </summary>
    public class SeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SeasonProvider> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly TheSportsDBClientV1 _tsdbClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SeasonProvider}"/> interface.</param>
        /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
        /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
        public SeasonProvider(IHttpClientFactory httpClientFactory, ILogger<SeasonProvider> logger, ILibraryManager libraryManager, TheSportsDBClientV1 tsdbClient)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _tsdbClient = tsdbClient;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc/>
        public string Name => TheSportsDBPlugin.ProviderName;

        /// <inheritdoc/>
        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo season, CancellationToken cancellationToken)
        {
            if (season.IndexNumber == null || !season.SeriesProviderIds.IsSupported())
            {
                _logger.LogDebug("No series identity found for {EpisodeName}", season.Name);

                return new MetadataResult<Season>
                {
                    QueriedById = true
                };
            }

            season.SeriesProviderIds.TryGetValue(TheSportsDBPlugin.ProviderId, out var tsdbid);

            var response = await _tsdbClient.GetSeason(int.Parse(tsdbid ?? "0", CultureInfo.InvariantCulture), season.IndexNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).ConfigureAwait(false);

            return MapSeasonToResult(season, response);
        }

        private MetadataResult<Season> MapSeasonToResult(SeasonInfo season, SeasonsResponse response)
        {
            var seasonFirst = response.Seasons[0];
            var result = new MetadataResult<Season>
            {
                HasMetadata = true,
                Item = new Season
                {
                    IndexNumber = season.IndexNumber,
                    Overview = seasonFirst.StrPoster,
                }
            };

            return result;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);

            return _tsdbClient.HttpClient.SendAsync(message, cancellationToken);
        }
    }
}
