using System;
using System.Globalization;
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
        private readonly Regex _episodeRegex = new(@"^(?<series>.+?)[_ .-]*(?:\((?<year>\d+)\))?[_ .-]*(?<season>[Ss]\d+)?(?<episode>[Ee]\d+(?:-[Ee]\d+)*)[_ .-]*(?<title>.*?)?[_ .-]*(?:\[(?<id>[\w=-]+)\])?[_ .-]*(?: - (?<resolution>(?:\d+[Pp])|\d+[Kk]))?\.(?<extention>[\w\d]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _dateRegex = new(@"\d{4}-\d{2}-\d{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        /// <summary>
        /// Resolves series name by file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="value">Series name.</param>
        /// <returns>>Series name is resolved.</returns>
        public bool TryResolveSeriesName(string fileName, out string value)
        {
            return TryResolveField(fileName, "series", out value);
        }

        /// <summary>
        /// Resolves episode name by file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="value">Episode name.</param>
        /// <returns>>Episode name is resolved.</returns>
        public bool TryResolveEpisodeName(string fileName, out string value)
        {
            return TryResolveField(fileName, "title", out value);
        }

        /// <summary>
        /// Resolves episode date by file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="value">Episode date.</param>
        /// <returns>Episode date is resolved.</returns>
        public bool TryResolveEpisodeDate(string fileName, out DateTimeOffset value)
        {
            var result = false;
            var match = _dateRegex.Match(fileName);

            value = default;

            if (match.Success && DateTimeOffset.TryParseExact(match.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Resolves provider ID by file name.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="fieldName">Field name.</param>
        /// <param name="value">Provider ID.</param>
        /// <returns>Provider ID is found.</returns>
        internal bool TryResolveField(string fileName, string fieldName, out string value)
        {
            var result = false;

            value = string.Empty;

            if (!string.IsNullOrEmpty(fileName))
            {
                var match = _episodeRegex.Match(fileName);

                if (match.Success && match.Groups[fieldName].Success)
                {
                    value = match.Groups[fieldName].Value;

                    _logger.LogDebug("Got field {FieldName}={FieldValue} from file name {FileName}", fieldName, value, fileName);

                    result = true;
                }
            }

            return result;
        }
    }
}
