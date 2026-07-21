using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResourceReservation.Api.Controllers;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ResourceReservation.Tests.Controllers;
public class UsersControllerTests
{
    [Fact]
    public async Task GetUsers_ReturnsBadRequest_WhenPageInvalid()
    {
        var svcMock = new Mock<IUserService>();
        var controller = new UsersController(svcMock.Object);

        var result = await controller.GetUsers(q: null, role: null, createdAfter: null, createdBefore: null, page: 0, pageSize: 10);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithUsers()
    {
        var svcMock = new Mock<IUserService>();
        var users = new List<UserReadDto>
        {
            new() { Id = Guid.NewGuid(), FirstName = "A", LastName = "A", Email = "a@example.com", Role = "User", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), FirstName = "B", LastName = "B", Email = "b@example.com", Role = "User", CreatedAt = DateTime.UtcNow }
        };
        svcMock.Setup(s => s.SearchAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<int?>()))
               .ReturnsAsync(users);

        var controller = new UsersController(svcMock.Object);

        var result = await controller.GetUsers(q: null, role: null, createdAfter: null, createdBefore: null, page: 1, pageSize: 10);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenMissing()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserReadDto?)null);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.GetUser(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUser_ReturnsOk_WhenFound()
    {
        var dto = new UserReadDto { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "x@y.com", Role = "User", CreatedAt = DateTime.UtcNow };
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.GetUser(dto.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenPasswordMissing()
    {
        var svcMock = new Mock<IUserService>();
        var controller = new UsersController(svcMock.Object);

        var dto = new UserCreateDto { FirstName = "N", LastName = "L", Email = "n@l.com", Password = null! };

        var result = await controller.CreateUser(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenServiceReturnsNull()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>())).ReturnsAsync((UserReadDto?)null);

        var controller = new UsersController(svcMock.Object);
        var dto = new UserCreateDto { FirstName = "N", LastName = "L", Email = "dup@example.com", Password = "pwd" };

        var result = await controller.CreateUser(dto);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenSuccess()
    {
        var created = new UserReadDto { Id = Guid.NewGuid(), FirstName = "C", LastName = "U", Email = "c@u.com", Role = "User", CreatedAt = DateTime.UtcNow };
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>())).ReturnsAsync(created);

        var controller = new UsersController(svcMock.Object);
        var dto = new UserCreateDto { FirstName = "C", LastName = "U", Email = "c@u.com", Password = "pwd" };

        var result = await controller.CreateUser(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task UpdateUser_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<IUserService>();
        var controller = new UsersController(svcMock.Object);

        var result = await controller.UpdateUser(Guid.NewGuid(), null!);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UpdateUser_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateDto>())).ReturnsAsync(false);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.UpdateUser(Guid.NewGuid(), new UserUpdateDto { FirstName = "A" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateUser_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateDto>())).ReturnsAsync(true);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.UpdateUser(Guid.NewGuid(), new UserUpdateDto { FirstName = "A" });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.DeleteUser(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<IUserService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var controller = new UsersController(svcMock.Object);
        var result = await controller.DeleteUser(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }
}

