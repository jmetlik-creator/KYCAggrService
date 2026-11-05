using KycAggregation.Data;
using Logging.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KycAggregation.Api;

[ApiController]
public class KycDataController : ControllerBase
{
    private readonly KycAggregationService _service;
    private readonly ILogSink _logSink;
    private readonly ILogger<KycDataController> _logger;

    public KycDataController(KycAggregationService service, ILogSink logSink, ILogger<KycDataController> logger)
    {
        _service = service;
        _logSink = logSink;
        _logger = logger;
    }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("/kyc-data/{ssn}")]
    public async Task<IActionResult> GetAggregatedKycDataSummary(string ssn)
    {
        var requestTraceId = HttpContext.TraceIdentifier;
        var transactionId = Guid.NewGuid();

        try
        {
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycRequestReceived",
                ServiceName = "KycAggregation.Api",
                Level = "Information",
                Message = "KYC summary request received.",
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["operation"] = "GetAggregatedKycDataSummary",
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["requestHost"] = HttpContext.Request.Host.ToString()
                }
            });

            var response = await _service.GetAsync(ssn, transactionId);
            return Ok(response);
        }
        catch (InvalidSsnException ex)
        {
            _logger.LogWarning(ex, "Invalid SSN supplied.");
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycRequestValidationFailed",
                ServiceName = "KycAggregation.Api",
                Level = "Warning",
                Message = "Invalid SSN supplied.",
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method
                }
            });
            return BadRequest(new { error = ex.Message });
        }
        catch (CustomerDataApiException ex)
        {
            _logger.LogWarning(ex, "Customer data API returned {StatusCode} for SSN {Ssn}.", (int)ex.StatusCode, ssn);
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycCustomerDataApiFailure",
                ServiceName = "KycAggregation.Api",
                Level = ex.StatusCode == System.Net.HttpStatusCode.NotFound ? "Warning" : "Error",
                Message = ex.Message,
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["statusCode"] = (int)ex.StatusCode
                }
            });

            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = ex.Message });
            }

            return StatusCode((int)ex.StatusCode, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing KYC request for SSN {Ssn}.", ssn);
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycRequestFailed",
                ServiceName = "KycAggregation.Api",
                Level = "Error",
                Message = "Unexpected error while processing KYC request.",
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["statusCode"] = StatusCodes.Status500InternalServerError
                }
            });
            return Problem(
                detail: "An unexpected error occurred while processing the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error");
        }
    }

    [HttpGet("/kyc-data/{ssn}/full")]
    public async Task<IActionResult> GetFullAggregatedData(string ssn)
    {
        var requestTraceId = HttpContext.TraceIdentifier;
        var transactionId = Guid.NewGuid();

        try
        {
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycFullRequestReceived",
                ServiceName = "KycAggregation.Api",
                Level = "Information",
                Message = "KYC full data request received.",
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["operation"] = "GetFullAggregatedData",
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["requestHost"] = HttpContext.Request.Host.ToString()
                }
            });

            var response = await _service.GetFullAsync(ssn, transactionId);
            return Ok(response);
        }
        catch (InvalidSsnException ex)
        {
            _logger.LogWarning(ex, "Invalid SSN supplied.");
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycFullRequestValidationFailed",
                ServiceName = "KycAggregation.Api",
                Level = "Warning",
                Message = "Invalid SSN supplied.",
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method
                }
            });
            return BadRequest(new { error = ex.Message });
        }
        catch (CustomerDataApiException ex)
        {
            _logger.LogWarning(ex, "Customer data API returned {StatusCode} for SSN {Ssn}.", (int)ex.StatusCode, ssn);
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycFullCustomerDataApiFailure",
                ServiceName = "KycAggregation.Api",
                Level = ex.StatusCode == System.Net.HttpStatusCode.NotFound ? "Warning" : "Error",
                Message = ex.Message,
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["statusCode"] = (int)ex.StatusCode
                }
            });

            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { error = ex.Message });
            }

            return StatusCode((int)ex.StatusCode, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing full KYC request for SSN {Ssn}.", ssn);
            await _logSink.WriteAsync(new LogEvent
            {
                EventName = "KycFullRequestFailed",
                ServiceName = "KycAggregation.Api",
                Level = "Error",
                Message = "Unexpected error while processing KYC request.",
                Exception = ex.ToString(),
                CorrelationId = transactionId.ToString(),
                OccurredUtc = DateTime.UtcNow,
                Context = new Dictionary<string, object?>
                {
                    ["ssn"] = ssn,
                    ["transactionId"] = transactionId,
                    ["requestTraceId"] = requestTraceId,
                    ["requestPath"] = HttpContext.Request.Path,
                    ["requestMethod"] = HttpContext.Request.Method,
                    ["statusCode"] = StatusCodes.Status500InternalServerError
                }
            });
            return Problem(
                detail: "An unexpected error occurred while processing the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error");
        }
    }
}
