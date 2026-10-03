using Microsoft.AspNetCore.Mvc;

namespace EventArch.Api.Idempotency;

/// <summary>
/// Marks an action that must receive an Idempotency-Key header and be safe to retry.
/// Use it on every action that moves money.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute() : TypeFilterAttribute(typeof(IdempotencyFilter));
