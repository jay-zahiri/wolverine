// <auto-generated/>
#pragma warning disable
using Wolverine.Persistence.Marten.Publishing;

namespace Internal.Generated.WolverineHandlers
{
    // START: IncrementManyAsyncHandler23252109
    public class IncrementManyAsyncHandler23252109 : Wolverine.Runtime.Handlers.MessageHandler
    {
        private readonly Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;

        public IncrementManyAsyncHandler23252109(Wolverine.Persistence.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory)
        {
            _outboxedSessionFactory = outboxedSessionFactory;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Wolverine.IMessageContext context, System.Threading.CancellationToken cancellation)
        {
            var letterHandler = new Wolverine.Persistence.Testing.Marten.LetterHandler();
            var incrementManyAsync = (Wolverine.Persistence.Testing.Marten.IncrementManyAsync)context.Envelope.Message;
            await using var documentSession = _outboxedSessionFactory.OpenSession(context);
            var eventStore = documentSession.Events;
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<Wolverine.Persistence.Testing.Marten.LetterAggregate>(incrementManyAsync.LetterAggregateId, cancellation).ConfigureAwait(false);

            var outgoing1 = await letterHandler.Handle(incrementManyAsync, eventStream.Aggregate, documentSession).ConfigureAwait(false);
            if (outgoing1 != null)
            {
                // Capturing any possible events returned from the command handlers
                eventStream.AppendMany(outgoing1);

            }

            await documentSession.SaveChangesAsync(cancellation).ConfigureAwait(false);
        }

    }

    // END: IncrementManyAsyncHandler23252109


}

