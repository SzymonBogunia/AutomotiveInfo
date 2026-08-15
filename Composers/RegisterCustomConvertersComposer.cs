using AutomotiveInfo.PropertyValueConverters;
using Umbraco.Cms.Core.Composing;

namespace AutomotiveInfo.Composers;

public class RegisterCustomConvertersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.PropertyValueConverters().Append<ComponentTextReadingTimeValueConverter>();
    }
}