// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;
using WolverineWebApi;

namespace Internal.Generated.WolverineHandlers
{
    // START: GET_querystring_decimal
    public class GET_querystring_decimal : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly WolverineWebApi.Recorder _recorder;

        public GET_querystring_decimal(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, WolverineWebApi.Recorder recorder) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _recorder = recorder;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            System.Decimal amount = default;
            System.Decimal.TryParse(httpContext.Request.Query["amount"], System.Globalization.CultureInfo.InvariantCulture, out amount);
            // Just saying hello in the code! Also testing the usage of attributes to customize endpoints
            
            // The actual HTTP request handler execution
            var result_of_UseQueryStringParsing = WolverineWebApi.TestEndpoints.UseQueryStringParsing(_recorder, amount);

            await WriteString(httpContext, result_of_UseQueryStringParsing);
        }

    }

    // END: GET_querystring_decimal
    
    
}
