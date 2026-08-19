using AutomotiveInfo.Swagger;
using Umbraco.Cms.Core.Composing;

namespace AutomotiveInfo.Composers;

public class RegisterNewsApiSwaggerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<NewsApiSwaggerGenOptions>();
    }
}