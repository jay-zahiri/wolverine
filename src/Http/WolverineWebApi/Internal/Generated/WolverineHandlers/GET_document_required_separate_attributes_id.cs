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
    // START: GET_document_required_separate_attributes_id
    public class GET_document_required_separate_attributes_id : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly Wolverine.Runtime.IWolverineRuntime _wolverineRuntime;
        private readonly Wolverine.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;

        public GET_document_required_separate_attributes_id(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, Wolverine.Runtime.IWolverineRuntime wolverineRuntime, Wolverine.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _wolverineRuntime = wolverineRuntime;
            _outboxedSessionFactory = outboxedSessionFactory;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            if (!System.Guid.TryParse((string)httpContext.GetRouteValue("id"), out var id))
            {
                httpContext.Response.StatusCode = 404;
                return;
            }


            var messageContext = new Wolverine.Runtime.MessageContext(_wolverineRuntime);
            // Building the Marten session
            await using var documentSession = _outboxedSessionFactory.OpenSession(messageContext);
            var invoice = await documentSession.LoadAsync<WolverineWebApi.Marten.Invoice>(id, httpContext.RequestAborted).ConfigureAwait(false);
            var problemDetails1 = WolverineWebApi.Marten.DocumentRequiredEndpoint.Load(invoice);
            // Evaluate whether the processing should stop if there are any problems
            if (!(ReferenceEquals(problemDetails1, Wolverine.Http.WolverineContinue.NoProblems)))
            {
                await WriteProblems(problemDetails1, httpContext).ConfigureAwait(false);
                return;
            }


            // 404 if this required object is null
            if (invoice == null)
            {
                httpContext.Response.StatusCode = 404;
                return;
            }

            
            // The actual HTTP request handler execution
            var invoice_response = WolverineWebApi.Marten.DocumentRequiredEndpoint.SeparateAttributes(invoice);

            
            // Save all pending changes to this Marten session
            await documentSession.SaveChangesAsync(httpContext.RequestAborted).ConfigureAwait(false);

            
            // Have to flush outgoing messages just in case Marten did nothing because of https://github.com/JasperFx/wolverine/issues/536
            await messageContext.FlushOutgoingMessagesAsync().ConfigureAwait(false);

            // Writing the response body to JSON because this was the first 'return variable' in the method signature
            await WriteJsonAsync(httpContext, invoice_response);
        }

    }

    // END: GET_document_required_separate_attributes_id
    
    
}

