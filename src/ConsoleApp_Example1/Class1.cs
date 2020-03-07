using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTracing;

namespace ConsoleApp_Example1
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly ILogger<Hello> _logger;

        public Hello(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<Hello>();
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);
            var helloString = $"Hello, {helloTo}!";
            span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                }
            );
            _logger.LogInformation(helloString);
            span.Log("WriteLine");
            span.Finish();
        }

        public static void Main(string[] args)
        {
            using (var loggerFactory = new LoggerFactory())
            {
                var helloTo = "lpe";
                using (var tracer = Tracing.Init("hello-world", loggerFactory))
                {
                    new Hello(tracer, loggerFactory).SayHello(helloTo);
                }
            }
        }
    }
}