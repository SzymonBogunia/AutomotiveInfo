using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AutomotiveInfo.Swagger;

public class NewsApiSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("news-api", new OpenApiInfo
        {
            Title = "AutomotiveInfo News API",
            Version = "v1",
            Description = "Publiczny endpoint zwracający najnowsze artykuły motoryzacyjne."
        });

        var previousPredicate = options.SwaggerGeneratorOptions.DocInclusionPredicate;

        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            var isNewsController = apiDesc.ActionDescriptor.RouteValues
                .TryGetValue("controller", out var controllerName)
                && controllerName == "NewsApi";

            if (docName == "news-api")
            {
                return isNewsController;
            }

            // Nie pokazuj naszego kontrolera w domyślnych dokumentach Umbraco
            return !isNewsController && (previousPredicate?.Invoke(docName, apiDesc) ?? true);
        });
    }
}