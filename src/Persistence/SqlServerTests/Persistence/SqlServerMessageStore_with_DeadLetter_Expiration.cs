using IntegrationTests;
using JasperFx.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Wolverine;
using Wolverine.ComplianceTests;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS;
using Wolverine.RDBMS.Durability;
using Wolverine.RDBMS.Polling;
using Wolverine.SqlServer;
using Wolverine.Tracking;

namespace SqlServerTests.Persistence;

public class SqlServerMessageStore_with_DeadLetter_Expiration : MessageStoreCompliance
{
    public override async Task<IHost> BuildCleanHost()
    {
        var host = await Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.PersistMessagesWithSqlServer(Servers.SqlServerConnectionString, "receiver2");
                
                // This setting changes the internal message storage identity
                opts.Durability.DeadLetterQueueExpirationEnabled = true;
            })
            .StartAsync();

        var persistence = (IMessageDatabase)host.Services.GetRequiredService<IMessageStore>();
        await persistence.Admin.ClearAllAsync();

        return host;
    }
    
    [Fact]
    public async Task execute_the_dead_letter_queue_expirations()
    {
        var list = new List<Envelope>();

        for (var i = 0; i < 10; i++)
        {
            var envelope = ObjectMother.Envelope();
            envelope.Id = Guid.Parse($"00000000-0000-0000-0000-00000000000{i}");
            envelope.Status = EnvelopeStatus.Incoming;


            list.Add(envelope);
        }

        await thePersistence.Inbox.StoreIncomingAsync(list.ToArray());


        var ex = new DivideByZeroException("Kaboom!");

        var report2 = new ErrorReport(list[2], ex);
        var report3 = new ErrorReport(list[3], ex);
        var report4 = new ErrorReport(list[4], ex);

        await thePersistence.Inbox.MoveToDeadLetterStorageAsync(report2.Envelope, ex);
        await thePersistence.Inbox.MoveToDeadLetterStorageAsync(report3.Envelope, ex);
        await thePersistence.Inbox.MoveToDeadLetterStorageAsync(report4.Envelope, ex);

        // Default is 10 days, so we're way in the future
        var expiredTimeInFuture = DateTimeOffset.UtcNow.Add(30.Days());

        var runtime = theHost.GetRuntime();
        var op = new DeleteExpiredDeadLetterMessagesOperation((IMessageDatabase)runtime.Storage, NullLogger.Instance,
            expiredTimeInFuture);
        var operation = new DatabaseOperationBatch((IMessageDatabase)runtime.Storage, [op]);

        await theHost.InvokeAsync(operation);

        var counts = await thePersistence.Admin.FetchCountsAsync();
        counts.DeadLetter.ShouldBe(0);
    }
}