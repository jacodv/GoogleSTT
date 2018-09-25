using System;
using System.IO;
using System.Reflection;
using GoogleSTT.CustomWebSocket;
using GoogleSTT.Hubs;
using GoogleSTT.Websockets;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoogleSTT
{
  public class Startup
  {
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      _log.Debug("Start Configuration services");
      services.Configure<CookiePolicyOptions>(options =>
      {
        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy = SameSiteMode.None;
      });

      //services.AddSingleton<ICustomWebSocketFactory, CustomWebSocketFactory>();
      //services.AddSingleton<ICustomWebSocketMessageHandler, CustomWebSocketMessageHandler>();

      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
      services.AddWebSocketManager();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
      _log.Debug("Start Configure");

      loggerFactory.AddLog4Net(); 


      _log.Debug("Added logging");

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
      }

      var webSocketOptions = new WebSocketOptions()
      {
        KeepAliveInterval = TimeSpan.FromSeconds(120),
        ReceiveBufferSize = 4 * 1024
      };
      app.UseWebSockets();
      app.MapWebSocketManager("/audiows", serviceProvider.GetService<AudioMessageHandler>());

      //app.UseWebSockets(webSocketOptions);
      //app.UseCustomWebSocketManager();

      _log.Debug("Added web sockets");


      DefaultFilesOptions options = new DefaultFilesOptions();
      options.DefaultFileNames.Clear();
      options.DefaultFileNames.Add("/index.html");
      app.UseDefaultFiles(options);
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.Use(async (context, next) =>
      {
        await next();
        if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
        {
          context.Request.Path = "/index.html";
          context.Response.StatusCode = 200;
          await next();
        }
      });
      
      _log.Debug("Configured web server");


      app.UseCookiePolicy();
      _log.Debug("Added CookiePolicy");

      //app.UseSignalR(routes =>  
      //{  
      //  routes.MapHub<ChatHub>("/chatHub");  
      //  routes.MapHub<AudioHub>("/audioHub");  
      //}); 

      app.UseMvc(routes =>
      {
        routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=Index}/{id?}");
      });

      _log.Debug("Added MVC");

    }
  }
}
