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
public class CategoriesControllerTests
{
    [Fact]
    public async Task GetCategories_ReturnsOk_WithCategories()
    {
        var svcMock = new Mock<ICategoryService>();
        var categories = new List<CategoryReadDto>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };
        svcMock.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.GetCategories();

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public async Task GetCategory_ReturnsNotFound_WhenMissing()
    {
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CategoryReadDto?)null);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.GetCategory(1);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCategory_ReturnsOk_WhenFound()
    {
        var dto = new CategoryReadDto { Id = 1, Name = "X" };
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.GetCategory(dto.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task CreateCategory_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<ICategoryService>();
        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.CreateCategory(null!);

        result.Result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task CreateCategory_ReturnsCreated_WhenSuccess()
    {
        var created = new CategoryReadDto { Id = 3, Name = "New" };
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.CreateAsync(It.IsAny<CategoryCreateDto>())).ReturnsAsync(created);

        var controller = new CategoriesController(svcMock.Object);
        var dto = new CategoryCreateDto { Name = "New" };

        var result = await controller.CreateCategory(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsBadRequest_WhenDtoNull()
    {
        var svcMock = new Mock<ICategoryService>();
        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.UpdateCategory(1, null!);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<CategoryUpdateDto>())).ReturnsAsync(false);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.UpdateCategory(1, new CategoryUpdateDto { Name = "A" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<CategoryUpdateDto>())).ReturnsAsync(true);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.UpdateCategory(1, new CategoryUpdateDto { Name = "A" });

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNotFound_WhenServiceFalse()
    {
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<int>())).ReturnsAsync(false);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.DeleteCategory(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent_WhenSuccess()
    {
        var svcMock = new Mock<ICategoryService>();
        svcMock.Setup(s => s.DeleteAsync(It.IsAny<int>())).ReturnsAsync(true);

        var controller = new CategoriesController(svcMock.Object);

        var result = await controller.DeleteCategory(1);

        result.Should().BeOfType<NoContentResult>();
    }
}

