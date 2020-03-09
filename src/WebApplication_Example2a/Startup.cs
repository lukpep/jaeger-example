using System;
using System.Collections.Generic;
using System.Linq;
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
using WebApplication_Example2;

namespace WebApplication_Example2a
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTracing();
            services.AddSingleton<ITracer>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var samplerConfiguration = new Configuration.SamplerConfiguration(loggerFactory)
                    .WithType(ConstSampler.Type)
                    .WithParam(1);

                var reporterConfiguration = new Configuration.ReporterConfiguration(loggerFactory)
                    .WithLogSpans(true);

                var tracer = (Tracer)new Configuration("NSA-SERVICE", loggerFactory)
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

            app.UseRouting();

            app.UseMiddleware<JaegerHttpMiddleware>();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/hello", async context =>
                {
                    var tracer = context.RequestServices.GetService<ITracer>();
                    using (var scope = tracer.BuildSpan("service-is-working").StartActive(true))
                    {
                        await Task.Delay(20);
                    }
                    await context.Response.WriteAsync("Hello from service 2!");
                });
            });
        }
    }
}