using AutomotiveInfo.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace AutomotiveInfo.Composers;

public class ArticleSearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<IArticleSearchService, ArticleSearchService>();
    }
}