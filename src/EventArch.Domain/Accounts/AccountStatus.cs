namespace EventArch.Domain.Accounts;

/// <summary>
/// Lifecycle of an account: Active ⇄ Frozen, and either one → Closed (final).
/// </summary>
public enum AccountStatus
{
    Active,
    Frozen,
    Closed
}
