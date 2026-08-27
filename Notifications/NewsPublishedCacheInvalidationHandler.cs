using AutomotiveInfo.Caching;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace AutomotiveInfo.Notifications;

public class NewsPublishedCacheInvalidationHandler : INotificationHandler<ContentPublishedNotification>
{
    private readonly NewsCacheSignal _cacheSignal;
    private readonly ILogger<NewsPublishedCacheInvalidationHandler> _logger;

    public NewsPublishedCacheInvalidationHandler(
        NewsCacheSignal cacheSignal,
        ILogger<NewsPublishedCacheInvalidationHandler> logger)
    {
        _cacheSignal = cacheSignal;
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

        _cacheSignal.Invalidate();

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
