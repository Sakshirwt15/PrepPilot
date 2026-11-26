using Microsoft.AspNetCore.Mvc;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;

namespace PrepPilot.Api.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController(ICompanyService service) : ControllerBase
{
    /// <summary>All companies with the number of mock papers available.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CompanySummaryDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    /// <summary>A company's typical hiring pattern and its mock papers.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<CompanyDetailDto>> Get(string slug, CancellationToken ct) =>
        Ok(await service.GetAsync(slug, ct));
}

[ApiController]
[Route("api/mock-tests")]
public class MockTestsController(IMockTestService service) : ControllerBase
{
    /// <summary>Lists mock tests, optionally filtered by company slug and difficulty.</summary>
    [HttpGet]
    public async Task<ActionResult<List<MockTestSummaryDto>>> List(
        [FromQuery] string? company, [FromQuery] string? difficulty, CancellationToken ct) =>
        Ok(await service.ListAsync(company, difficulty, ct));

    /// <summary>Questions for taking a test. Correct answers are never included.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MockTestDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, ct));
}
