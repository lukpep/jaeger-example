using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jaeger;
using Jaeger.Samplers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Util;

namespace WebApplication_Example2
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddOpenTracing();
            services.AddSingleton<ITracer>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var samplerConfiguration = new Configuration.SamplerConfiguration(loggerFactory)
                    .WithType(ConstSampler.Type)
                    .WithParam(1);

                var reporterConfiguration = new Configuration.ReporterConfiguration(loggerFactory)
                    .WithLogSpans(true);

                var tracer = (Tracer)new Configuration("NSA-API", loggerFactory)
                    .WithSampler(samplerConfiguration)
                    .WithReporter(reporterConfiguration)
                    .GetTracer();

                GlobalTracer.Register(tracer);

                return tracer;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<JaegerHttpMiddleware>();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
                endpoints.MapGet("/ex", async context => { throw new ArgumentException("Oh well"); });
                endpoints.MapGet("/call", async context =>
                {
                    var factory = context.RequestServices.GetService<IHttpClientFactory>();
                    var tracer = context.RequestServices.GetService<ITracer>();
                    
                    using (var scope = tracer.BuildSpan("some-action").StartActive(true))
                    {
                        await Task.Delay(10);
                        scope.Span.Log("took some action");
                    }
                    
                    var client = factory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        "http://localhost:5001/hello");
                    
                    var response = await client.SendAsync(request);

                    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                });
            });
        }
    }
}