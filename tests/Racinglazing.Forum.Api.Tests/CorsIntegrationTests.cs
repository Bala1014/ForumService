using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Forums;
using Xunit;

namespace Racinglazing.Forum.Api.Tests;

public sealed class CorsIntegrationTests : IClassFixture<ForumApiFactory>
{
    private const string AllowedOrigin =
        "https://raceservice-frontend-dev-dot-racingglazing.de.r.appspot.com";
    private readonly HttpClient _client;

    public CorsIntegrationTests(ForumApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Forums_list_from_allowed_origin_returns_cors_header_and_existing_response()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/forums");
        request.Headers.Add("Origin", AllowedOrigin);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains(response.Headers.Vary,
            value => string.Equals(value.Value, "Origin", StringComparison.OrdinalIgnoreCase));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("data").ValueKind);
        Assert.Empty(document.RootElement.GetProperty("data").EnumerateArray());
        Assert.False(document.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
    }

    [Fact]
    public async Task Forums_preflight_from_allowed_origin_returns_configured_allow_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/forums");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");

        using var response = await _client.SendAsync(request);

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("GET", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        var allowedHeaders = response.Headers.GetValues("Access-Control-Allow-Headers").Single();
        Assert.Contains("Content-Type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Authorization", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Forums_list_from_unapproved_origin_does_not_return_cors_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/forums");
        request.Headers.Add("Origin", "https://untrusted.example.com");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Error_response_from_allowed_origin_returns_cors_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/forums/not-a-guid");
        request.Headers.Add("Origin", AllowedOrigin);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}

public sealed class ForumApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ForumDatabase:AutoMigrate", "false");
        builder.UseSetting("CORS_ALLOWED_ORIGINS",
            "https://raceservice-frontend-dev-dot-racingglazing.de.r.appspot.com");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IForumService>();
            services.AddScoped<IForumService, EmptyForumService>();
        });
    }
}

public sealed class EmptyForumService : IForumService
{
    public Task<PagedResult<ForumDto>> ListAsync(Guid? categoryId, PageRequest page, CancellationToken ct = default)
        => Task.FromResult(PagedResult<ForumDto>.Empty);

    public Task<ForumDto> GetByIdAsync(Guid forumId, CancellationToken ct = default)
        => throw new NotImplementedException();
}
