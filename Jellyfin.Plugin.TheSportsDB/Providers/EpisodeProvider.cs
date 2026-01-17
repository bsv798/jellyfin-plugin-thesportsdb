using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TheSportsDB.Providers.IdResolvers;
using Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB.Providers
{
    /// <summary>
    /// Episode provider.
    /// </summary>
    public class EpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly ILogger<EpisodeProvider> _logger;
        private readonly TheSportsDBClientV1 _tsdbClient;
        private readonly ProviderIdResolver<EpisodeInfo> _providerIdResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{EpisodeProvider}"/> interface.</param>
        /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
        /// <param name="providerIdResolver">Instance of the <see cref="ProviderIdResolver{T}"/> interface.</param>
        public EpisodeProvider(ILogger<EpisodeProvider> logger, TheSportsDBClientV1 tsdbClient, ProviderIdResolver<EpisodeInfo> providerIdResolver)
        {
            _logger = logger;
            _tsdbClient = tsdbClient;
            _providerIdResolver = providerIdResolver;
        }

        /// <inheritdoc />
        public string Name => TheSportsDBPlugin.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            if ((searchInfo.IndexNumber == null && searchInfo.PremiereDate == null) || !searchInfo.SeriesProviderIds.IsSupported())
            {
                return list;
            }

            var metadataResult = await GetEpisode(searchInfo, cancellationToken).ConfigureAwait(false);

            if (!metadataResult.HasMetadata)
            {
                return list;
            }

            var item = metadataResult.Item;

            list.Add(new RemoteSearchResult
            {
                IndexNumber = item.IndexNumber,
                Name = item.Name,
                ParentIndexNumber = item.ParentIndexNumber,
                PremiereDate = item.PremiereDate,
                ProductionYear = item.ProductionYear,
                ProviderIds = item.ProviderIds,
                SearchProviderName = Name,
                IndexNumberEnd = item.IndexNumberEnd
            });

            return list;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            if ((info.IndexNumber == null && info.PremiereDate == null) || !info.SeriesProviderIds.IsSupported())
            {
                _logger.LogDebug("No series identity found for {EpisodeName}", info.Name);

                return new MetadataResult<Episode>
                {
                    QueriedById = true
                };
            }

            if (info.IndexNumberEnd.HasValue)
            {
                _logger.LogDebug("Multiple episodes found in {Path}", info.Path);

                return await GetCombinedEpisode(info, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await GetEpisode(info, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<MetadataResult<Episode>> GetCombinedEpisode(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var startIndex = info.IndexNumber;
            var endIndex = info.IndexNumberEnd;

            var results = new List<MetadataResult<Episode>>();

            for (var episode = startIndex; episode <= endIndex; episode++)
            {
                var tempEpisodeInfo = info;
                info.IndexNumber = episode;

                if (episode == startIndex)
                {
                    results.Add(await GetEpisode(tempEpisodeInfo, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    results.Add(await GetEpisode(tempEpisodeInfo, cancellationToken, true).ConfigureAwait(false));
                }
            }

            var result = CombineResults(results);

            return result;
        }

        private MetadataResult<Episode> CombineResults(List<MetadataResult<Episode>> results)
        {
            var result = results[0];

            var name = new StringBuilder(result.Item.Name);
            var originalTitle = new StringBuilder(result.Item.OriginalTitle);
            var overview = new StringBuilder(result.Item.Overview);

            for (var res = 1; res < results.Count; res++)
            {
                name.Append(" / ");
                name.Append(results[res].Item.Name);
                originalTitle.Append(" / ");
                originalTitle.Append(results[res].Item.OriginalTitle);
                overview.Append(" / ");
                overview.Append(results[res].Item.Overview);
            }

            result.Item.Name = name.ToString();
            result.Item.OriginalTitle = originalTitle.ToString();
            result.Item.Overview = overview.ToString();

            return result;
        }

        private async Task<MetadataResult<Episode>> GetEpisode(EpisodeInfo searchInfo, CancellationToken cancellationToken, bool ignoreTsdbIdField = false)
        {
            var result = new MetadataResult<Episode>
            {
                QueriedById = true
            };

            var episodeTsdbId = searchInfo.GetTsdbId();

            try
            {
                var fileName = Path.GetFileName(searchInfo.Path);

                if (episodeTsdbId == 0 || ignoreTsdbIdField || searchInfo.IsAutomated)
                {
                    if (_providerIdResolver.TryResolve(fileName, out episodeTsdbId))
                    {
                        var response = await _tsdbClient.GetEventByIdAsync(Convert.ToInt32(episodeTsdbId, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                        episodeTsdbId = Convert.ToInt32(response.Events[0].IdEvent, CultureInfo.InvariantCulture);
                    }
                    else if (_providerIdResolver.TryResolveEpisodeName(fileName, out var episodeName))
                    {
                        var response = await _tsdbClient.GetEventByTitleAsync(null, null, null, episodeName).ConfigureAwait(false);

                        episodeTsdbId = Convert.ToInt32(response.Event[0].IdEvent, CultureInfo.InvariantCulture);
                    }
                    else if (_providerIdResolver.TryResolveSeriesName(fileName, out var seriesName) && _providerIdResolver.TryResolveEpisodeDate(fileName, out var episodeDate))
                    {
                        var response = await _tsdbClient.GetEventByTitleAsync(seriesName, null, episodeDate, null).ConfigureAwait(false);

                        episodeTsdbId = Convert.ToInt32(response.Event[0].IdEvent, CultureInfo.InvariantCulture);
                    }

                    if (episodeTsdbId == 0)
                    {
                        _logger.LogError(
                            "Episode S{Season:00}E{Episode:00} not found for series {SeriesTsdbId}:{Name}",
                            searchInfo.ParentIndexNumber,
                            searchInfo.IndexNumber,
                            searchInfo.GetTsdbId(),
                            searchInfo.Name);

                        return result;
                    }
                }

                var episodeResult = await _tsdbClient.GetEventByIdAsync(episodeTsdbId).ConfigureAwait(false);

                result = MapEpisodeToResult(searchInfo, episodeResult, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Failed to retrieve episode with id {EpisodeTsdbId}, series id {SeriesTsdbId}:{Name}",
                    episodeTsdbId,
                    searchInfo.GetTsdbId(),
                    searchInfo.Name);
            }

            return result;
        }

        private MetadataResult<Episode> MapEpisodeToResult(EpisodeInfo id, EventsResponse episode1, CancellationToken cancellationToken)
        {
            var firstEpisode = episode1.Events[0];
            var result = new MetadataResult<Episode>
            {
                HasMetadata = true,
                Item = new Episode
                {
                    IndexNumber = id.IndexNumber,
                    ParentIndexNumber = id.ParentIndexNumber,
                    IndexNumberEnd = id.IndexNumberEnd,
                    Name = firstEpisode.StrEvent,
                    Overview = firstEpisode.StrDescriptionEN,
                }
            };
            result.ResetPeople();

            var item = result.Item;
            item.SetTsdbId(firstEpisode.IdEvent);

            if (firstEpisode.DateEvent != null)
            {
                item.PremiereDate = firstEpisode.DateEvent.Value.UtcDateTime;
                item.ProductionYear = firstEpisode.DateEvent.Value.UtcDateTime.Year;
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);

            return _tsdbClient.HttpClient.SendAsync(message, cancellationToken);
        }
    }
}
