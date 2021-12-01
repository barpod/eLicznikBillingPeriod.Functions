using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using eLicznikBillingPeriod;

namespace ELicznikBillingPeriod
{
    public class ELicznikBillingPeriodFunction
    {
        private readonly ILogger _logger;

        public ELicznikBillingPeriodFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ELicznikBillingPeriodFunction>();
        }

        [Function("eLicznikBillingPeriod")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", 
            Route = "eLicznikBillingPeriod/{startDate}/{endDate?}")] HttpRequestData req, string startDate, string endDate)
        {
            _logger.LogInformation($"C# HTTP trigger function processed a request: {startDate} - {endDate}.");

            var httpClient = new HttpClient();
            var start = DateTime.Parse(startDate);
            var end = endDate == null ? DateTime.Now : DateTime.Parse(endDate);
            var period = new Library.Period(start, end);
            var username = Environment.GetEnvironmentVariable("ELICZNIK_USERNAME");
            var password = Environment.GetEnvironmentVariable("ELICZNIK_PASSWORD");
            var meterNr = Environment.GetEnvironmentVariable("ELICZNIK_METER_NR");
            var connectionData = new Library.UserData( username, password, meterNr, 0.8);
            var fSharpAsync = Library.readingsForPeriodAsync(httpClient, period, connectionData);
            fSharpAsync.Wait();
            var res = fSharpAsync.Result;

            if (res.IsOk)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                string serialized = JsonSerializer.Serialize(res.ResultValue);
                
                response.WriteString(serialized);

                return response;
            }
            else 
            {
                _logger.LogWarning($"Reading data failed: {res.ErrorValue}");
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                return response;
            }

        }
    }
}
