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

namespace Jellyfin.Plugin.TheSportsDB.Providers
{
    /// <summary>
    /// Episode image provider.
    /// </summary>
    public class EpisodeImageProvider : IRemoteImageProvider
    {
        private readonly ILogger<EpisodeImageProvider> _logger;
        private readonly TheSportsDBClientV1 _tsdbClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{EpisodeImageProvider}"/> interface.</param>
        /// <param name="tsdbClient">Instance of <see cref="TheSportsDBClientV1"/>.</param>
        public EpisodeImageProvider(ILogger<EpisodeImageProvider> logger, TheSportsDBClientV1 tsdbClient)
        {
            _logger = logger;
            _tsdbClient = tsdbClient;
        }

        /// <inheritdoc />
        public string Name => TheSportsDBPlugin.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Episode;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Art;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;
            var series = episode.Series;
            var imageResult = new List<RemoteImageInfo>();
            var language = item.GetPreferredMetadataLanguage();
            if (!item.IsSupported())
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var tsdbId = item.GetTsdbId();
            var seriesImages = await _tsdbClient.GetEventByIdAsync(tsdbId, cancellationToken).ConfigureAwait(false);
            var seriesFirst = seriesImages.Events[0];
            var remoteImages = new List<RemoteImageInfo>
            {
                new() { Url = seriesFirst.StrThumb, Type = ImageType.Primary },
                new() { Url = seriesFirst.StrFanart, Type = ImageType.Art }
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
}
