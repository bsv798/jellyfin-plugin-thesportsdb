using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.TheSportsDB.Providers.IdsExtensions;

internal static class ProviderIdsExtensions
{
    /// <summary>
    /// Check whether an item includes an entry for any supported provider IDs.
    /// </summary>
    /// <param name="item">The <see cref="IHasProviderIds"/>.</param>
    /// <returns>True, if <paramref name="item"/> contains any supported provider IDs.</returns>
    internal static bool IsSupported(this IHasProviderIds? item)
    {
        return item.HasProviderId(TheSportsDBPlugin.ProviderId);
    }

    internal static bool IsSupported(this Dictionary<string, string> item)
    {
        return item.TryGetValue(TheSportsDBPlugin.ProviderId.ToString(), out var tsdbId) && !string.IsNullOrEmpty(tsdbId);
    }

    /// <summary>
    /// Get the id stored within the item.
    /// </summary>
    /// <param name="item">The <see cref="IHasProviderIds"/> item to get the id from.</param>
    /// <returns>The Id, or 0.</returns>
    public static int GetTsdbId(this IHasProviderIds item)
        => Convert.ToInt32(item.GetProviderId(TheSportsDBPlugin.ProviderId), CultureInfo.InvariantCulture);

    /// <inheritdoc cref="SetTsdbId(IHasProviderIds, string?)" />
    public static void SetTsdbId(this IHasProviderIds item, long? value)
        => item.SetTsdbId(value.HasValue && value > 0 ? value.Value.ToString(CultureInfo.InvariantCulture) : null);

    /// <summary>
    /// Set the id in the item, if provided <paramref name="value"/> is not <see langword="null"/> or white space.
    /// </summary>
    /// <param name="item">>The <see cref="IHasProviderIds"/> to set the id.</param>
    /// <param name="value">id to set.</param>
    /// <returns><see langword="true"/> if value was set.</returns>
    public static bool SetTsdbId(this IHasProviderIds item, string? value)
        => item.SetProviderIdIfHasValue(TheSportsDBPlugin.ProviderId, value);

    /// <inheritdoc cref="SetProviderIdIfHasValue(IHasProviderIds, string, string?)"/>
    public static bool SetProviderIdIfHasValue(this IHasProviderIds item, MetadataProvider provider, string? value)
        => item.SetProviderIdIfHasValue(provider.ToString(), value);

    /// <summary>
    /// Set the provider id in the item, if provided <paramref name="value"/> is not <see langword="null"/> or white space.
    /// </summary>
    /// <param name="item">>The <see cref="IHasProviderIds"/> to set the id.</param>
    /// <param name="name">Provider name.</param>
    /// <param name="value">Provider id to set.</param>
    /// <returns><see langword="true"/> if value was set.</returns>
    public static bool SetProviderIdIfHasValue(this IHasProviderIds item, string name, string? value)
    {
        if (!HasValue(value))
        {
            return false;
        }

        item.SetProviderId(name, value);
        return true;
    }

    /// <summary>
    /// Checks whether the item has Id stored.
    /// </summary>
    /// <param name="item">The <see cref="IHasProviderIds"/> item.</param>
    /// <returns>True, if item has Id stored.</returns>
    public static bool HasTsdbId(this IHasProviderIds? item)
        => item.HasTsdbId(out var value);

    /// <inheritdoc cref="HasProviderId(IHasProviderIds?, string, out string?)"/>
    public static bool HasTsdbId(this IHasProviderIds? item, out string? value)
        => item.HasProviderId(TheSportsDBPlugin.ProviderId, out value);

    /// <inheritdoc cref="HasProviderId(IHasProviderIds?, string)"/>
    public static bool HasProviderId(this IHasProviderIds? item, MetadataProvider provider)
        => item.HasProviderId(provider, out var value);

    /// <inheritdoc cref="HasProviderId(IHasProviderIds?, string, out string?)"/>
    public static bool HasProviderId(this IHasProviderIds? item, MetadataProvider provider, out string? value)
        => item.HasProviderId(provider.ToString(), out value);

    /// <summary>
    /// Checks whether the item has provider id stored.
    /// </summary>
    /// <param name="item">The <see cref="IHasProviderIds"/> item.</param>
    /// <param name="name">Provider.</param>
    /// <returns>True, if item has provider id  stored.</returns>
    public static bool HasProviderId(this IHasProviderIds? item, string name)
        => item.HasProviderId(name, out var value);

    /// <summary>
    /// Checks whether the item has provider id stored.
    /// </summary>
    /// <param name="item">The <see cref="IHasProviderIds"/> item.</param>
    /// <param name="name">Provider.</param>
    /// <param name="value">The current provider id value.</param>
    /// <returns>True, if item has provider id stored.</returns>
    public static bool HasProviderId(this IHasProviderIds? item, string name, out string? value)
    {
        value = null;
        var result = item is { }
            && item.TryGetProviderId(name, out value)
            && HasValue(value);

        value = result ? value : null;
        return result;
    }

    private static bool HasValue([NotNullWhen(true)] string? value)
        => !string.IsNullOrWhiteSpace(value);
}
