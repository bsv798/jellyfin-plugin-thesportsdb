using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

using Microsoft.Extensions.Logging;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB.Providers;

/// <summary>
/// Series image provider.
/// </summary>
public class SeriesImageProvider : IRemoteImageProvider
{
    private readonly ILogger<SeriesImageProvider> _logger;
    private readonly TheSportsDBClientV1 _tsdbClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesImageProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{SeriesImageProvider}"/> interface.</param>
    /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
    public SeriesImageProvider(ILogger<SeriesImageProvider> logger, TheSportsDBClientV1 tsdbClient)
    {
        _logger = logger;
        _tsdbClient = tsdbClient;
    }

    /// <inheritdoc />
    public string Name => TheSportsDBPlugin.ProviderName;

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Series;
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
        yield return ImageType.Logo;
        yield return ImageType.Art;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!item.IsSupported())
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        var tsdbId = item.GetTsdbId();
        var seriesImages = await _tsdbClient.GetLeagueByIdAsync(tsdbId, cancellationToken).ConfigureAwait(false);
        var seriesFirst = seriesImages.Leagues[0];
        var remoteImages = new List<RemoteImageInfo>
        {
            new() { Url = seriesFirst.StrBadge, Type = ImageType.Primary },
            new() { Url = seriesFirst.StrLogo, Type = ImageType.Logo },
            new() { Url = seriesFirst.StrFanart1, Type = ImageType.Art },
            new() { Url = seriesFirst.StrFanart2, Type = ImageType.Art },
            new() { Url = seriesFirst.StrFanart3, Type = ImageType.Art },
            new() { Url = seriesFirst.StrFanart4, Type = ImageType.Art }
        };

        remoteImages.RemoveAll(x => string.IsNullOrEmpty(x.Url));

        return remoteImages;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, url);

        return _tsdbClient.HttpClient.SendAsync(message, cancellationToken);
    }
}
