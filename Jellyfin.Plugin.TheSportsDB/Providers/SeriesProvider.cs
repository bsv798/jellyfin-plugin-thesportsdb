#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Jellyfin.Plugin.TheSportsDB.Providers.IdResolvers;
using Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB.Providers
{
    /// <summary>
    /// Series provider.
    /// </summary>
    public class SeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SeriesProvider> _logger;
        private readonly TheSportsDBClientV1 _tsdbClient;
        private readonly ProviderIdResolver<SeriesInfo> _providerIdResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SeriesProvider}"/> interface.</param>
        /// <param name="providerIdResolver">Instance of the <see cref="ProviderIdResolver{T}"/> interface.</param>
        /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
        public SeriesProvider(IHttpClientFactory httpClientFactory, ILogger<SeriesProvider> logger, TheSportsDBClientV1 tsdbClient, ProviderIdResolver<SeriesInfo> providerIdResolver)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _tsdbClient = tsdbClient;
            _providerIdResolver = providerIdResolver;
        }

        /// <inheritdoc />
        public string Name => TheSportsDBPlugin.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            return await FindSeriesInternal(Path.GetFileName(searchInfo.Path), searchInfo.Year, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series> { QueriedById = true, };

            if (!info.IsSupported())
            {
                result.QueriedById = false;
                await Identify(info).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (info.IsSupported())
            {
                result.Item = new Series();
                result.HasMetadata = true;

                await FetchSeriesMetadata(result, info, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }

        private async Task FetchSeriesMetadata(MetadataResult<Series> result, SeriesInfo seriesInfo, CancellationToken cancellationToken)
        {
            var seriesMetadata = result.Item;

            if (_providerIdResolver.TryResolve(seriesInfo, out var tsdbId))
            {
                seriesInfo.SetTsdbId(tsdbId.ToString(CultureInfo.InvariantCulture));
            }

            if (seriesInfo.HasTsdbId(out var tsdbIdTxt))
            {
                seriesMetadata.SetTsdbId(tsdbIdTxt);
            }

            if (string.IsNullOrWhiteSpace(tsdbIdTxt))
            {
                _logger.LogWarning("No valid tsdb id found for series {TsdbId}:{SeriesName}", tsdbIdTxt, seriesInfo.Name);

                return;
            }

            tsdbId = Convert.ToInt32(tsdbIdTxt, CultureInfo.InvariantCulture);

            try
            {
                var seriesResult = await _tsdbClient.GetLeagueByIdAsync(tsdbId, cancellationToken).ConfigureAwait(false);

                MapSeriesToResult(result, seriesResult, seriesInfo);

                result.ResetPeople();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve series with id {TsdbId}:{SeriesName}", tsdbId, seriesInfo.Name);

                return;
            }
        }

        private async Task<List<RemoteSearchResult>> FindSeriesInternal(string name, int? year, string language, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Trying to find an ID for item: {Name} ({Year})", name, year);

            var list = new List<(List<string> Titles, RemoteSearchResult SearchResult)>();
            LeaguesResponse result;

            try
            {
                if (_providerIdResolver.TryResolve(name, out var tsdbId))
                {
                    result = await _tsdbClient.GetLeagueByIdAsync(tsdbId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new NotImplementedException("Search league by name is not yet implemented.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "No series results found for item: {Name} ({Year})", name, year);

                return new List<RemoteSearchResult>();
            }

            foreach (var seriesSearchResult in result.Leagues)
            {
                var tsdbTitles = new List<string> { seriesSearchResult.StrLeague };

                if (seriesSearchResult.StrLeagueAlternate is not null)
                {
                    tsdbTitles.Add(seriesSearchResult.StrLeagueAlternate);
                }

                DateTime? firstAired = null;

                if (seriesSearchResult.DateFirstEvent != null)
                {
                    firstAired = seriesSearchResult.DateFirstEvent.UtcDateTime;
                }

                var remoteSearchResult = new RemoteSearchResult
                {
                    Name = tsdbTitles.FirstOrDefault(),
                    ProductionYear = firstAired?.Year,
                    SearchProviderName = Name
                };

                if (!string.IsNullOrEmpty(seriesSearchResult.StrBadge))
                {
                    remoteSearchResult.ImageUrl = seriesSearchResult.StrBadge;
                }

                remoteSearchResult.SetTsdbId(seriesSearchResult.IdLeague);
                list.Add((tsdbTitles, remoteSearchResult));
            }

            return list
                .OrderBy(i => i.Titles.Contains(name, StringComparer.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(i => list.IndexOf(i))
                .Select(i => i.SearchResult)
                .ToList();
        }

        private async Task Identify(SeriesInfo info)
        {
            if (info.HasTsdbId())
            {
                return;
            }

            var remoteSearchResults = await FindSeriesInternal(Path.GetFileName(info.Path), info.Year, info.MetadataLanguage, CancellationToken.None).ConfigureAwait(false);
            var entry = remoteSearchResults.FirstOrDefault();

            if (entry.HasTsdbId(out var tsdbId))
            {
                info.SetTsdbId(tsdbId);
            }
        }

        private void MapSeriesToResult(MetadataResult<Series> result, LeaguesResponse tsdbSeries, SeriesInfo info)
        {
            Series series = result.Item;
            var seriesFirst = tsdbSeries.Leagues[0];
            series.SetTsdbId(seriesFirst.IdLeague);
            series.Name = seriesFirst.StrLeague;
            series.Overview = seriesFirst.StrDescriptionEN;
            series.OriginalTitle = seriesFirst.StrLeagueAlternate;
            series.HomePageUrl = seriesFirst.StrWebsite;
            result.ResultLanguage = info.MetadataLanguage;

            series.Status = "yes".Equals(seriesFirst.StrComplete, StringComparison.OrdinalIgnoreCase) ? SeriesStatus.Ended : SeriesStatus.Continuing;

            if (seriesFirst.DateFirstEvent != null)
            {
                series.PremiereDate = seriesFirst.DateFirstEvent.UtcDateTime;
                series.ProductionYear = seriesFirst.DateFirstEvent.UtcDateTime.Year;
            }

            if (!string.IsNullOrEmpty(seriesFirst.StrCountry))
            {
                series.ProductionLocations = [seriesFirst.StrCountry];
            }

            if (!string.IsNullOrEmpty(seriesFirst.StrSport))
            {
                series.AddGenre(seriesFirst.StrSport);
            }
        }
    }
}

#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
