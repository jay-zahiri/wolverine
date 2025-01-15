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
    // START: POST_api_todo_examinefirst
    public class POST_api_todo_examinefirst : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly Wolverine.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;
        private readonly Wolverine.Runtime.IWolverineRuntime _wolverineRuntime;

        public POST_api_todo_examinefirst(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, Wolverine.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory, Wolverine.Runtime.IWolverineRuntime wolverineRuntime) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _outboxedSessionFactory = outboxedSessionFactory;
            _wolverineRuntime = wolverineRuntime;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var messageContext = new Wolverine.Runtime.MessageContext(_wolverineRuntime);
            // Building the Marten session
            await using var documentSession = _outboxedSessionFactory.OpenSession(messageContext);
            // Reading the request body via JSON deserialization
            var (command, jsonContinue) = await ReadJsonAsync<WolverineWebApi.Todos.ExamineFirst>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            
            // Try to load the existing saga document
            var todo2 = await documentSession.LoadAsync<WolverineWebApi.Todos.Todo2>(command.Todo2Id, httpContext.RequestAborted).ConfigureAwait(false);
            // 404 if this required object is null
            if (todo2 == null)
            {
                httpContext.Response.StatusCode = 404;
                return;
            }

            var result1 = WolverineWebApi.Todos.ExamineFirstHandler.Before(todo2);
            // Evaluate whether or not the execution should be stopped based on the IResult value
            if (result1 != null && !(result1 is Wolverine.Http.WolverineContinue))
            {
                await result1.ExecuteAsync(httpContext).ConfigureAwait(false);
                return;
            }


            
            // The actual HTTP request handler execution
            WolverineWebApi.Todos.ExamineFirstHandler.Handle(command);

            // Wolverine automatically sets the status code to 204 for empty responses
            if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = 204;
        }

    }

    // END: POST_api_todo_examinefirst
    
    
}
