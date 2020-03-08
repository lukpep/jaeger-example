using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTracing;

namespace ConsoleApp_Example1
{
    internal class NsaDataAccessor
    {
        private readonly ITracer _tracer;
        private readonly ILogger<NsaDataAccessor> _logger;

        private NsaDataAccessor(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<NsaDataAccessor>();
        }

        private async Task<string> PrepareSecretData()
        {
            using (var scope = _tracer.BuildSpan("prepare-classified-data").StartActive(true))
            {
                const string helloString = "Hello, this is totally not any secret data!";
                await SomeLongInternalFunction();
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "contacting NSA for ... something",
                    ["value"] = helloString
                });
                return helloString;
            }
        }

        private async Task SomeLongInternalFunction()
        {
            using (var scope = _tracer.BuildSpan("getting-data-from-secret-database").StartActive(true))
            {
                scope.Span.Log("getting it");
                await Task.Delay(10);
            }
        }

        private void PrintData(string helloString)
        {
            using (var scope = _tracer.BuildSpan("sending-data-to-satellite").StartActive(true))
            {
                _logger.LogInformation(helloString);
                scope.Span.Log("Data send");
            }
        }

        private async Task GetData()
        {
            using (var scope = _tracer.BuildSpan("show-secret-data").StartActive(true))
            {
                var data = await PrepareSecretData();
                PrintData(data);
            }
        }

        public static async Task Main(string[] args)
        {
            var a = new LoggerFactory();
            
            using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
            {
                using (var tracer = Tracing.Init("secret-data-service", loggerFactory))
                {
                    await new NsaDataAccessor(tracer, loggerFactory).GetData();
                }
            }
        }
    }
}