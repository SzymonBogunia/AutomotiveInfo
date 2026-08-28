using Umbraco.Cms.Core.Composing;

namespace AutomotiveInfo.Composers;

public class DeliveryApiProxyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddHttpClient("DeliveryApiProxy", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["App:SelfUrl"] ?? "https://localhost:44328";
            client.BaseAddress = new Uri($"{baseUrl}/umbraco/delivery/api/v2/");
        });
    }
}