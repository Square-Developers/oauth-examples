using Square;
using Square.Models;
using Square.Exceptions;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/authorize", async context =>
{
    var configuration = context.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    var applicationId = configuration["Square:ApplicationId"];
    var redirectUri = configuration["Square:RedirectUri"];
    var state = Guid.NewGuid().ToString(); // CSRF protection

    var baseUrl = configuration["Square:Environment"] == "Production" ? "https://connect.squareup.com" : "https://connect.squareupsandbox.com";
    var authorizationUrl = $"{baseUrl}/oauth2/authorize?client_id={applicationId}&response_type=code&scope=MERCHANT_PROFILE_READ&state={state}&redirect_uri={Uri.EscapeDataString(redirectUri)}";


    context.Response.Redirect(authorizationUrl);
});

app.MapGet("/callback", async context =>
{
    var request = context.Request;
    var code = request.Query["code"];
    var state = request.Query["state"];

    if (!string.IsNullOrEmpty(code))
    {
        // Exchange code for access token
        var configuration = context.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var client = new SquareClient.Builder()
            .Environment(Square.Environment.Sandbox)
            .Build();

        var body = new ObtainTokenRequest.Builder(clientId: configuration["Square:ApplicationId"], grantType: "authorization_code")
        .ClientSecret(configuration["Square:ApplicationSecret"])
        .Code(code)
        .RedirectUri(configuration["Square:RedirectUri"])
        .Build();

        try
        {
            var result = await client.OAuthApi.ObtainTokenAsync(body: body);
            if (result.AccessToken != null)
            {
                var accessToken = result.AccessToken;
                var refreshToken = result.RefreshToken;
                var expiresAt = result.ExpiresAt;
                var merchantId = result.MerchantId;

                var redirectUrl = $"/Callback?accessToken={accessToken}&refreshToken={refreshToken}&expiresAt={expiresAt}&merchantId={merchantId}";
                context.Response.Redirect(redirectUrl);
            }
            else
            {
                Console.WriteLine("Failed to obtain access token: ", result);
                context.Response.Redirect("/");
            }
        }
        catch (ApiException e)
        {
            Console.WriteLine("Failed to obtain access token: ", e);
            context.Response.Redirect("/");
        }
    }
    else
    {
        context.Response.Redirect("/");
    }
});

app.Run();
