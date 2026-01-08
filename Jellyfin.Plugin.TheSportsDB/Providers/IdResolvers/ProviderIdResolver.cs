using System.Text.RegularExpressions;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TheSportsDB.Providers.IdResolvers
{
    /// <summary>
    /// Provider ID resolver.
    /// </summary>
    /// <typeparam name="T">Resolver type.</typeparam>
    public class ProviderIdResolver<T>
        where T : ItemLookupInfo
    {
        private readonly ILogger<ProviderIdResolver<T>> _logger;

        private readonly Regex _tsdbidRegex = new(@"tsdbid[-=]?(?<tsdbid>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderIdResolver{T}"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public ProviderIdResolver(ILogger<ProviderIdResolver<T>> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resolves provider ID by file name.
        /// </summary>
        /// <param name="info">Metadata info.</param>
        /// <param name="value">Provider ID.</param>
        /// <returns>Provider ID is found.</returns>
        public bool TryResolve(T info, out int value)
        {
            value = 0;

            var idStr = info.GetProviderId(TheSportsDBPlugin.ProviderId);

            if (!string.IsNullOrEmpty(idStr) && int.TryParse(idStr, out var result))
            {
                _logger.LogDebug("Got provider ID {Result} from metadata", result);

                value = result;
            }

            return value > 0;
        }

        /// <summary>
        /// Resolves provider ID by file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="value">Provider ID.</param>
        /// <returns>Provider ID is found.</returns>
        public bool TryResolve(string fileName, out int value)
        {
            // Try to get from stored metadata
            var result = false;

            value = 0;

            if (!string.IsNullOrEmpty(fileName))
            {
                var match = _tsdbidRegex.Match(fileName);

                if (match.Success && int.TryParse(match.Groups["tsdbid"].Value, out value))
                {
                    _logger.LogDebug("Got provider ID {Result} from file name {FileName}", result, fileName);

                    result = value > 0;
                }
            }

            return result;
        }
    }
}
