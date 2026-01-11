using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TheSportsDB;
using Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB.Providers;

/// <summary>
/// Season image provider.
/// </summary>
public class SeasonImageProvider : IRemoteImageProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SeasonImageProvider> _logger;
    private readonly TheSportsDBClientV1 _tsdbClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{SeasonImageProvider}"/> interface.</param>
    /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
    public SeasonImageProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<SeasonImageProvider> logger,
        TheSportsDBClientV1 tsdbClient)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tsdbClient = tsdbClient;
    }

    /// <inheritdoc />
    public string Name => TheSportsDBPlugin.ProviderName;

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Season;
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var season = (Season)item;
        var series = season.Series;

        if (!series.IsSupported() || season.IndexNumber is null)
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        var seasonInfo = await _tsdbClient.GetSeason(series.GetTsdbId(), season.IndexNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty).ConfigureAwait(false);
        var seasonFirst = seasonInfo.Seasons[0];
        var remoteImages = new List<RemoteImageInfo>
        {
            new() { Url = seasonFirst.StrBadge, Type = ImageType.Primary },
        };

        return remoteImages;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, url);

        return _tsdbClient.HttpClient.SendAsync(message, cancellationToken);
    }
}
