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
using Xunit;

namespace ResourceReservation.Tests.Controllers;
public class ResourcesControllerTests
{
    [Fact]
    public async Task GetResources_ReturnsBadRequest_WhenAtTimeInvalid()
    {
        var svcMock = new Mock<IResourceService>();
        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetResources(q: null, categoryId: null, isAvailable: null, day: null, atTime: "not-a-time");

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetResources_ReturnsOk_WithResources()
    {
        var svcMock = new Mock<IResourceService>();
        var resources = new List<ResourceReadDto>
        {
            new() { Id = Guid.NewGuid(), Name = "R1", Description = "d", IsAvailable = true, SlotDurationMinutes = 30 },
            new() { Id = Guid.NewGuid(), Name = "R2", Description = "d2", IsAvailable = true, SlotDurationMinutes = 60 }
        };
        svcMock.Setup(s => s.SearchAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<DayOfWeek?>(), It.IsAny<TimeOnly?>()))
               .ReturnsAsync(resources);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetResources(q: null, categoryId: null, isAvailable: null, day: null, atTime: null);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task GetResource_ReturnsNotFound_WhenMissing()
    {
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ResourceReadDto?)null);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetResource(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetResource_ReturnsNotFound_WhenNotAvailableAndNotAdmin()
    {
        var dto = new ResourceReadDto { Id = Guid.NewGuid(), Name = "X", IsAvailable = false };
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.GetResource(dto.Id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetResource_ReturnsOk_WhenNotAvailableAndUserIsAdmin()
    {
        var dto = new ResourceReadDto { Id = Guid.NewGuid(), Name = "X", IsAvailable = false };
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var controller = new ResourcesController(svcMock.Object);
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = admin } };

        var result = await controller.GetResource(dto.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task CreateResource_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<IResourceService>();
        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.CreateResource(null!);

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task CreateResource_ReturnsCreated_WhenSuccess()
    {
        var created = new ResourceReadDto { Id = Guid.NewGuid(), Name = "New", IsAvailable = true };
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.CreateAsync(It.IsAny<ResourceCreateDto>())).ReturnsAsync(created);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        var dto = new ResourceCreateDto { Name = "New" };

        var result = await controller.CreateResource(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task UpdateResource_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<IResourceService>();
        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        var result = await controller.UpdateResource(Guid.NewGuid(), null!);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UpdateResource_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ResourceUpdateDto>())).ReturnsAsync(false);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        var result = await controller.UpdateResource(Guid.NewGuid(), new ResourceUpdateDto { Name = "A" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateResource_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ResourceUpdateDto>())).ReturnsAsync(true);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        var result = await controller.UpdateResource(Guid.NewGuid(), new ResourceUpdateDto { Name = "A" });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteResource_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        var result = await controller.DeleteResource(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteResource_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<IResourceService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var controller = new ResourcesController(svcMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        var result = await controller.DeleteResource(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }
}
    

