using AutomotiveInfo.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace AutomotiveInfo.Composers;

public class RegisterNotificationHandlersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishedNotification, NewsPublishedCacheInvalidationHandler>();
    }
}