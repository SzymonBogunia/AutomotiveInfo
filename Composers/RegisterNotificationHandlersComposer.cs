using AutomotiveInfo.Caching;
using AutomotiveInfo.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace AutomotiveInfo.Composers;

public class RegisterNotificationHandlersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Shared invalidation signal for the per-culture news caches
        // (used by the controller to tag entries and by the handler to expire them all at once).
        builder.Services.AddSingleton<NewsCacheSignal>();

        builder.AddNotificationHandler<ContentPublishedNotification, NewsPublishedCacheInvalidationHandler>();
    }
}
