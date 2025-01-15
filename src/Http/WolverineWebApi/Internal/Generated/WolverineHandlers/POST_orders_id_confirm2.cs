// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;
using Wolverine.Marten.Publishing;
using Wolverine.Runtime;

namespace Internal.Generated.WolverineHandlers
{
    // START: POST_orders_id_confirm2
    public class POST_orders_id_confirm2 : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly Wolverine.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;
        private readonly Wolverine.Runtime.IWolverineRuntime _wolverineRuntime;

        public POST_orders_id_confirm2(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, Wolverine.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory, Wolverine.Runtime.IWolverineRuntime wolverineRuntime) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _outboxedSessionFactory = outboxedSessionFactory;
            _wolverineRuntime = wolverineRuntime;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var messageContext = new Wolverine.Runtime.MessageContext(_wolverineRuntime);
            // Reading the request body via JSON deserialization
            var (command, jsonContinue) = await ReadJsonAsync<WolverineWebApi.Marten.ConfirmOrder>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            await using var documentSession = _outboxedSessionFactory.OpenSession(messageContext);
            var eventStore = documentSession.Events;
            var aggregateId = command.OrderId;
            
            // Loading Marten aggregate
            var eventStream = await eventStore.FetchForWriting<WolverineWebApi.Marten.Order>(aggregateId, httpContext.RequestAborted).ConfigureAwait(false);

            if (!System.Guid.TryParse((string)httpContext.GetRouteValue("id"), out var id))
            {
                httpContext.Response.StatusCode = 404;
                return;
            }


            
            // The actual HTTP request handler execution
            (var updatedAggregate, var events) = WolverineWebApi.Marten.MarkItemEndpoint.ConfirmDifferent(command, eventStream.Aggregate);

            
            // Outgoing, cascaded message
            await messageContext.EnqueueCascadingAsync(updatedAggregate).ConfigureAwait(false);

            if (events != null)
            {
                
                // Capturing any possible events returned from the command handlers
                eventStream.AppendMany(events);

            }

            await documentSession.SaveChangesAsync(httpContext.RequestAborted).ConfigureAwait(false);
            var order_response = await eventStore.FetchLatest<WolverineWebApi.Marten.Order>(aggregateId, httpContext.RequestAborted);
            // Writing the response body to JSON because this was the first 'return variable' in the method signature
            await WriteJsonAsync(httpContext, order_response);
            
            // Have to flush outgoing messages just in case Marten did nothing because of https://github.com/JasperFx/wolverine/issues/536
            await messageContext.FlushOutgoingMessagesAsync().ConfigureAwait(false);

        }

    }

    // END: POST_orders_id_confirm2
    
    
}
