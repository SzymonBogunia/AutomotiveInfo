using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;

namespace AutomotiveInfo.Controllers;

[ApiController]
[Route("api/demo/news")]
public class DemoProxyController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeliveryApiSettings _deliveryApiSettings;

    public DemoProxyController(
        IHttpClientFactory httpClientFactory,
        IOptions<DeliveryApiSettings> deliveryApiSettings)
    {
        _httpClientFactory = httpClientFactory;
        _deliveryApiSettings = deliveryApiSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50); 

        var path = $"content?filter=contentType:newsPage&sort=createDate:desc&take={take}";
        return await ForwardToDeliveryApi(path);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var path = $"content/item/{id}?expand=properties[$all]";
        return await ForwardToDeliveryApi(path);
    }

    private async Task<IActionResult> ForwardToDeliveryApi(string relativePath)
    {
        var client = _httpClientFactory.CreateClient("DeliveryApiProxy");

        var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Add("Api-Key", _deliveryApiSettings.ApiKey);

        // przekazujemy język przeglądarki dalej, zamiast hardkodować pl-PL
        var acceptLanguage = Request.Headers.AcceptLanguage.ToString();
        request.Headers.Add("Accept-Language",
            string.IsNullOrWhiteSpace(acceptLanguage) ? "pl-PL" : acceptLanguage);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = body,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }
}