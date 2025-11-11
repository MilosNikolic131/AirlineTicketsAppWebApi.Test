using AirlineTicketsAppWebApi.Controllers;
using AirlineTicketsAppWebApi.Models;
using AirlineTicketsAppWebApi.Repositories;
using AirlineTicketsAppWebApi.Test.util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data.Common;

namespace AirlineTicketsAppWebApi.Test;

public class FlightController_Test
{

    private readonly Mock<IFlightRepository> _mockRepo;
    private readonly Mock<ILogger<FlightController>> _mockLogger;
    private readonly IConfiguration _mockConfig;
    private readonly FlightController _controller;

    public FlightController_Test()
    {
        _mockRepo = new Mock<IFlightRepository>();
        _mockLogger = new Mock<ILogger<FlightController>>();

        var configData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:SqlServerDb", "FakeConnectionString" }
        };
        _mockConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _controller = new FlightController(_mockConfig, _mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateFlight_InvalidModel_ReturnsBadRequest()
    {
        var invalidFlight = new FlightDto();
        _controller.ModelState.AddModelError("FlightFrom", "Required");

        var result = await _controller.CreateFlight(invalidFlight);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateFlight_ValidModel_ReturnsCreatedAtAction()
    {
        var flightDto = new FlightDto { FlightFrom = FlightDestination.BEOGRAD, FlightTo = FlightDestination.KRALJEVO, FlightDate = new DateTime(),
                                        NumOfLayovers = 0, NumOfSeats = 30};

        _mockRepo.Setup(r => r.CreateFlightAsync(flightDto))
                 .ReturnsAsync(42);

        var result = await _controller.CreateFlight(flightDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(FlightController.GetFlightById), createdResult.ActionName);

        var returnedDto = Assert.IsType<FlightDto>(createdResult.Value);
        Assert.Equal(42, returnedDto.FlightId);
    }

    [Fact]
    public async Task CreateFlight_DbException_Returns500()
    {
        var flightDto = new FlightDto
        {
            FlightFrom = FlightDestination.BEOGRAD,
            FlightTo = FlightDestination.KRALJEVO,
            FlightDate = new DateTime(),
            NumOfLayovers = 0,
            NumOfSeats = 30
        };
        _mockRepo.Setup(r => r.CreateFlightAsync(flightDto)).ThrowsAsync(new FakeDbException("Database failure"));

        var result = await _controller.CreateFlight(flightDto);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Failed to save flight", objectResult.Value);
    }

    [Fact]
    public async Task CreateFlight_GenericException_Returns500()
    {
        var flightDto = new FlightDto
        {
            FlightFrom = FlightDestination.BEOGRAD,
            FlightTo = FlightDestination.KRALJEVO,
            FlightDate = new DateTime(),
            NumOfLayovers = 0,
            NumOfSeats = 30
        };
        _mockRepo.Setup(r => r.CreateFlightAsync(flightDto))
                 .ThrowsAsync(new Exception("Unknown error"));

        var result = await _controller.CreateFlight(flightDto);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An internal error occurred", objectResult.Value);
    }
}