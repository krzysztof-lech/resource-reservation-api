using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResourceReservation.Api.Controllers;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ResourceReservation.Tests.Controllers;
public class ReservationsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk_AsNonAdmin()
    {
        var svcMock = new Mock<IReservationService>();
        var list = new List<IReservationReadDto>
        {
            new ReservationPublicReadDto(Guid.NewGuid(), Guid.NewGuid(), "R", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Pending")
        };
        svcMock.Setup(s => s.SearchAsync(false, It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>()))
               .ReturnsAsync(list);

        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetAll(userId: null, status: null, isPast: null);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_AsAdmin()
    {
        var svcMock = new Mock<IReservationService>();
        var list = new List<IReservationReadDto>
        {
            new ReservationReadDto(Guid.NewGuid(), Guid.NewGuid(), "R", Guid.NewGuid(), "u@e", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Pending")
        };
        svcMock.Setup(s => s.SearchAsync(true, It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool?>()))
               .ReturnsAsync(list);

        var controller = new ReservationsController(svcMock.Object);
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = admin } };

        var result = await controller.GetAll(userId: null, status: null, isPast: null);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync((IReservationReadDto?)null);

        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = new ReservationPublicReadDto(Guid.NewGuid(), Guid.NewGuid(), "R", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Pending");
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<bool>())).ReturnsAsync(dto);

        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetById(dto.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetMyReservations_ReturnsUnauthorized_WhenUserIdMissing()
    {
        var svcMock = new Mock<IReservationService>();
        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetMyReservations(status: null, isPast: null);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetMyReservations_ReturnsOk_WhenFound()
    {
        var userId = Guid.NewGuid();
        var svcMock = new Mock<IReservationService>();
        var list = new List<IReservationReadDto>
        {
            new ReservationPublicReadDto(Guid.NewGuid(), Guid.NewGuid(), "R", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Pending")
        };
        svcMock.Setup(s => s.SearchAsync(false, It.Is<Guid?>(g => g == userId), It.IsAny<string?>(), It.IsAny<bool?>()))
               .ReturnsAsync(list);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.GetMyReservations(status: null, isPast: null);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<IReservationService>();
        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.Create(null!);

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenUserIdInvalid()
    {
        var svcMock = new Mock<IReservationService>();
        var controller = new ReservationsController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var dto = new CreateReservationDto(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
        var result = await controller.Create(dto);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenSuccess()
    {
        var userId = Guid.NewGuid();
        var created = new ReservationReadDto(Guid.NewGuid(), Guid.NewGuid(), "R", userId, "u@e", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Pending");
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.CreateAsync(It.IsAny<CreateReservationDto>(), It.IsAny<Guid>())).ReturnsAsync(created);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new CreateReservationDto(created.ResourceId, created.StartTime, created.EndTime);
        var result = await controller.Create(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Cancel_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.CancelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
               .ReturnsAsync(CancelReservationResult.Success);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.Cancel(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenNotFound()
    {
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.CancelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
               .ReturnsAsync(CancelReservationResult.NotFound);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.Cancel(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_ReturnsForbid_WhenForbidden()
    {
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.CancelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
               .ReturnsAsync(CancelReservationResult.Forbidden);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.Cancel(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenCannotCancel()
    {
        var svcMock = new Mock<IReservationService>();
        svcMock.Setup(s => s.CancelAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()))
               .ReturnsAsync(CancelReservationResult.CannotCancel);

        var controller = new ReservationsController(svcMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.Cancel(Guid.NewGuid());

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

