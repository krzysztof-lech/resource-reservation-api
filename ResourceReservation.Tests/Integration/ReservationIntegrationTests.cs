using FluentAssertions;
using ResourceReservation.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ResourceReservation.Tests.Integration;

[Trait("Category", "Integration")]
public class ReservationsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReservationsIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOk_WhenAuthenticated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.Scheme);

        var resp = await client.GetAsync("/api/reservations");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenResourceMissing()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.Scheme);

        var dto = new CreateReservationDto(Guid.NewGuid(), DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        var resp = await client.PostAsJsonAsync("/api/reservations", dto);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
