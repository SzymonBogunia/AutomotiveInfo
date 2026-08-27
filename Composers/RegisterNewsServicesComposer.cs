using AutomotiveInfo.News;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace AutomotiveInfo.Composers;

public class RegisterNewsServicesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Scoped: depends on the request-scoped IPublishedContentQuery.
        builder.Services.AddScoped<INewsArticleService, NewsArticleService>();
    }
}
