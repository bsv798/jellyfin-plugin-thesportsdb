using Jellyfin.Plugin.TheSportsDB.Providers.IdResolvers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;
using TheSportsDBClient;

namespace Jellyfin.Plugin.TheSportsDB
{
    /// <summary>
    /// Register plugin services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton(new TheSportsDBClientV1());
            serviceCollection.AddSingleton<ProviderIdResolver<SeriesInfo>>();
            serviceCollection.AddSingleton<ProviderIdResolver<EpisodeInfo>>();
        }
    }
}
