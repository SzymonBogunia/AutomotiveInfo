using AutomotiveInfo.Caching;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace AutomotiveInfo.Notifications;

public class NewsPublishedCacheInvalidationHandler : INotificationHandler<ContentPublishedNotification>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<NewsPublishedCacheInvalidationHandler> _logger;

    public NewsPublishedCacheInvalidationHandler(
        IMemoryCache cache,
        ILogger<NewsPublishedCacheInvalidationHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void Handle(ContentPublishedNotification notification)
    {
        var publishedNewsArticles = notification.PublishedEntities
            .Where(x => x.ContentType.Alias == "newsPage")
            .ToList();

        if (publishedNewsArticles.Count == 0)
        {
            return;
        }

        _cache.Remove(NewsCacheKeys.AllArticles);

        foreach (var article in publishedNewsArticles)
        {
            _logger.LogInformation(
                "AUDIT: Artykuł '{ArticleName}' (id: {ArticleId}) został opublikowany o {PublishedAt:u}. Cache listy aktualności unieważniony.",
                article.Name,
                article.Id,
                DateTime.UtcNow);
        }
    }
}