namespace EventArch.Api.Controllers;

// HTTP request bodies. Kept apart from commands so route values (like the account id)
// are not duplicated in the body and the public API can evolve independently.

public sealed record OpenAccountRequest(string HolderName);

public sealed record AmountRequest(decimal Amount);

public sealed record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);

public sealed record OpenAccountResponse(Guid AccountId);

public sealed record TransferResponse(Guid TransferId);
