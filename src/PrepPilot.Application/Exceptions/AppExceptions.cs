namespace PrepPilot.Application.Exceptions;

/// <summary>Resource does not exist (HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>A business rule was broken (HTTP 400).</summary>
public class BusinessRuleException(string message) : Exception(message);

/// <summary>Conflicts with current state, e.g. duplicate email (HTTP 409).</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Credentials are missing or wrong (HTTP 401).</summary>
public class UnauthorizedException(string message) : Exception(message);

/// <summary>An external dependency (the AI provider) failed or is not configured (HTTP 502).</summary>
public class ExternalServiceException(string message) : Exception(message);
