using Microsoft.AspNetCore.Rewrite;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();


await app.BootUmbracoAsync();

app.UseHttpsRedirection();

// Culture routing uses host-agnostic prefix domains (/pl, /en) assigned to the home
// page, so NO content is reachable at the bare root — verified: without this rule
// "/" returns 404. Send the domain root to the default culture instead.
app.UseRewriter(new RewriteOptions().AddRedirect("^$", "pl", StatusCodes.Status301MovedPermanently));

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
