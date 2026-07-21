using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResourceReservation.Api.Controllers;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceReservation.Tests.Controllers;
public class AuthControllerTests
{
    [Fact]
    public async Task Login_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<IAuthService>();
        var controller = new AuthController(svcMock.Object);

        var result = await controller.Login(null!);

        result.Should().BeOfType<BadRequestResult>();
        svcMock.Verify(s => s.AuthenticateAsync(It.IsAny<LoginDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        var svcMock = new Mock<IAuthService>();
        svcMock.Setup(s => s.AuthenticateAsync(It.IsAny<LoginDto>())).ReturnsAsync((TokenDto?)null);

        var controller = new AuthController(svcMock.Object);
        var dto = new LoginDto("noone@example.com", "bad");

        var result = await controller.Login(dto);

        result.Should().BeOfType<UnauthorizedResult>();
        svcMock.Verify(s => s.AuthenticateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithToken()
    {
        var token = new TokenDto("jwt-token-value");
        var svcMock = new Mock<IAuthService>();
        svcMock.Setup(s => s.AuthenticateAsync(It.IsAny<LoginDto>())).ReturnsAsync(token);

        var controller = new AuthController(svcMock.Object);
        var dto = new LoginDto("user@example.com", "pwd");

        var result = await controller.Login(dto);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(token);
        svcMock.Verify(s => s.AuthenticateAsync(dto), Times.Once);
    }
}

