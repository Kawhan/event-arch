using Microsoft.AspNetCore.Hosting;

namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// A separate system with a tiny Outbox batch and few attempts, so a test can fill a whole
/// batch with broken messages. It has its own containers: broken messages here never
/// slow down the tests that share <see cref="EventArchApiFactory"/>.
/// </summary>
public sealed class SmallOutboxFactory : EventArchApiFactory
{
    public const int BatchSize = 5;
    public const int MaxAttempts = 3;

    protected override void ConfigureTestSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Outbox:BatchSize", BatchSize.ToString());
        builder.UseSetting("Outbox:MaxAttempts", MaxAttempts.ToString());
    }
}

[CollectionDefinition(Name)]
public sealed class SmallOutboxCollection : ICollectionFixture<SmallOutboxFactory>
{
    public const string Name = "SmallOutbox";
}
