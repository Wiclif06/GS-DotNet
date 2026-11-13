using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AgroConecta.Api.Tests;
public class EndpointTests
{
    private static async Task Login(HttpClient client, string email = "admin@test.local")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Integration-test-password!" });
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("accessToken").GetString());
    }
    private static object Farm(string name = "Fazenda comunitária") => new { name, ownerName = "Cooperativa", city = "Itu", state = "sp", totalArea = 20 };
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString()); return client.SendAsync(request);
    }
    [Fact]
    public async Task Protected_routes_reject_missing_or_invalid_token()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/farms")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/farms")).StatusCode);
    }
    [Fact]
    public async Task Reader_can_read_but_cannot_write()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await app.AddReaderAsync();
        await Login(client, "reader@test.local");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/farms")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/farms", Farm())).StatusCode);
    }
    [Fact]
    public async Task Registration_login_and_duplicate_email_are_validated()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient();
        var payload = new { name = "Produtor", email = "producer@test.local", password = "Integration-test-password!" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
        await Login(client, payload.email);
        var me = await (await client.GetAsync("/api/auth/me")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PRODUCER", me.GetProperty("role").GetString());
        Assert.False(me.TryGetProperty("passwordHash", out _));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { payload.email, password = "wrong-password" })).StatusCode);
    }
    [Fact]
    public async Task Farm_crud_and_created_location_work()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var created = await Post(client, "/api/farms", Farm()); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(location, Farm("Atualizada"))).StatusCode);
        var item = await client.GetFromJsonAsync<JsonElement>(location);
        Assert.Equal("Atualizada", item.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(location)).StatusCode);
    }
    [Fact]
    public async Task Retry_replays_response_and_changed_body_conflicts()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var first = await Post(client, "/api/farms", Farm(), "same-key");
        var repeated = await Post(client, "/api/farms", Farm(), "same-key");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode); Assert.Equal(first.StatusCode, repeated.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await repeated.Content.ReadAsStringAsync());
        Assert.Equal(first.Headers.Location, repeated.Headers.Location); Assert.True(repeated.Headers.Contains("Idempotency-Replayed"));
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/farms")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await Post(client, "/api/farms", Farm("Outro corpo"), "same-key")).StatusCode);
    }
    [Fact]
    public async Task Missing_key_invalid_payload_and_foreign_key_are_rejected()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/farms", Farm())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/farms", new { name = "", ownerName = "", city = "", state = "xx", totalArea = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/farms", new { name = "Nome", ownerName = "Produtor", city = "Itu", state = "SP", totalArea = 10, userId = 999 })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/farms")).EnumerateArray());
    }
    [Fact]
    public async Task Satellite_flow_generates_one_alert_and_recommendation_and_preserves_history()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var farm = await (await Post(client, "/api/farms", Farm())).Content.ReadFromJsonAsync<JsonElement>();
        var cropResponse = await Post(client, "/api/crop-areas", new { name = "Talhão", cropType = "Milho", areaSize = 4, latitude = -23, longitude = -47, farmId = farm.GetProperty("id").GetInt32() });
        var crop = await cropResponse.Content.ReadFromJsonAsync<JsonElement>(); var cropId = crop.GetProperty("id").GetInt32();
        object Observation(string date) => new { cropAreaId = cropId, source = "SIMULATED", ndviAverage = .2, ndviMin = .1, ndviMax = .3, surfaceTemperature = 40, cloudCoverage = 10, fireFocusNearby = true, fireFocusDistanceKm = 2, captureDate = date };
        var first = await Post(client, "/api/satellite-data", Observation("2026-01-02"), "satellite-first");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(first.Headers.Location)).StatusCode);
        await Post(client, "/api/satellite-data", Observation("2026-01-02"), "satellite-first");
        await Post(client, "/api/satellite-data", Observation("2026-01-03"));
        var historical = await Post(client, "/api/satellite-data", new { cropAreaId = cropId, source = "SIMULATED", ndviAverage = .8, ndviMin = .7, ndviMax = .9, surfaceTemperature = 20, cloudCoverage = 10, fireFocusNearby = false, captureDate = "2026-01-01" });
        Assert.Equal(HttpStatusCode.Created, historical.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/climate-alerts")).EnumerateArray());
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/recommendations")).EnumerateArray());
        var area = await client.GetFromJsonAsync<JsonElement>($"/api/crop-areas/{cropId}"); Assert.Equal("CRITICAL", area.GetProperty("status").GetString());
        var dashboardResponse = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, dashboard.GetProperty("satelliteRecords").GetInt32());
        Assert.Equal(.4m, decimal.Round(dashboard.GetProperty("averageNdvi").GetDecimal(), 2));
        var alerts = await client.GetFromJsonAsync<JsonElement>("/api/climate-alerts");
        var alertId = alerts[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/climate-alerts/{alertId}/resolve", null)).StatusCode);
        var resolved = await client.GetFromJsonAsync<JsonElement>($"/api/climate-alerts/{alertId}");
        await client.PutAsync($"/api/climate-alerts/{alertId}/resolve", null);
        var resolvedAgain = await client.GetFromJsonAsync<JsonElement>($"/api/climate-alerts/{alertId}");
        Assert.Equal(resolved.GetProperty("resolvedAt").GetString(), resolvedAgain.GetProperty("resolvedAt").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(farmResponsePath(farm))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/crop-areas/{cropId}")).StatusCode);
        static string farmResponsePath(JsonElement f) => $"/api/farms/{f.GetProperty("id").GetInt32()}";
    }
    [Fact]
    public async Task Health_and_openapi_expose_real_contract()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient();
        var health = await client.GetFromJsonAsync<JsonElement>("/health/ready"); Assert.Equal("Healthy", health.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var spec = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.Equal("string", spec.GetProperty("components").GetProperty("schemas").GetProperty("CropStatus").GetProperty("type").GetString());
        Assert.True(spec.GetProperty("paths").TryGetProperty("/api/farms", out var farms));
        Assert.Contains(farms.GetProperty("post").GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "Idempotency-Key");
    }
    [Fact]
    public async Task Invalid_ndvi_order_is_rejected()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var response = await Post(client, "/api/satellite-data", new { cropAreaId = 1, source = "SIMULATED", ndviAverage = .2, ndviMin = .8, ndviMax = .9, surfaceTemperature = 20, cloudCoverage = 10, fireFocusNearby = false, captureDate = "2026-01-01" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.GetProperty("errors").TryGetProperty("NdviAverage", out _));
    }
    [Theory]
    [InlineData("expired")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-signature")]
    public async Task Jwt_validation_rejects_invalid_claims_or_signature(string scenario)
    {
        using var app = new ApiFactory(); using var client = app.CreateClient();
        var key = scenario == "wrong-signature" ? "Another-test-key-that-does-not-sign-valid-tokens!" : "Test-only-signing-key-with-more-than-32-bytes!";
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("AgroConecta", scenario == "wrong-audience" ? "OtherApp" : "AgroConecta.Mobile",
            expires: scenario == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/farms")).StatusCode);
    }
    [Fact]
    public async Task Replay_survives_application_restart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"agroconecta-restart-{Guid.NewGuid():N}.db");
        try
        {
            string original;
            using (var app = new ApiFactory { SharedDatabasePath = path })
            {
                using var client = app.CreateClient(); await Login(client);
                original = await (await Post(client, "/api/farms", Farm(), "persisted-key")).Content.ReadAsStringAsync();
            }
            using (var app = new ApiFactory { SharedDatabasePath = path })
            {
                using var client = app.CreateClient(); await Login(client);
                var response = await Post(client, "/api/farms", Farm(), "persisted-key");
                Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.True(response.Headers.Contains("Idempotency-Replayed"));
                Assert.Equal(original, await response.Content.ReadAsStringAsync());
                Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/farms")).EnumerateArray());
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }
    [Fact]
    public async Task Rejected_payload_does_not_consume_idempotency_key()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var invalid = await Post(client, "/api/farms", new { name = "", ownerName = "", city = "", state = "S", totalArea = -1 }, "correctable-key");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "/api/farms", Farm(), "correctable-key")).StatusCode);
    }
    [Fact]
    public async Task Malformed_json_is_400_without_internal_details()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/farms") { Content = new StringContent("{invalid-json", System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Add("Idempotency-Key", "malformed");
        var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("System.", await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Admin_created_reader_account_can_login()
    {
        using var app = new ApiFactory(); using var client = app.CreateClient(); await Login(client);
        var created = await Post(client, "/api/users", new { name = "Leitor", email = "newreader@test.local", role = "READER", password = "Integration-test-password!" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.DoesNotContain("passwordHash", await created.Content.ReadAsStringAsync());
        await Login(client, "newreader@test.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/farms", Farm())).StatusCode);
    }
    [Fact]
    public async Task Rate_limit_returns_429_and_retry_after()
    {
        using var app = new ApiFactory { Limit = 2 }; using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        var rejected = await client.GetAsync("/"); Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }
}
