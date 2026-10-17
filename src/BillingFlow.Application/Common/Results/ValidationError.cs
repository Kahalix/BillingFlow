namespace BillingFlow.Application.Common.Results;

public record ValidationError(string Code, string Description, IDictionary<string, string[]> Errors)
    : Error(Code, Description, ErrorType.Validation);
