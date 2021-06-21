using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using RY.TransferImagePro.Data;
using RY.TransferImagePro.Services;
using RY.VideoDAProject.CustomFilters;
using RY.VideoDAProject.CustomMiddleware;

namespace RY.TransferImagePro
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            WebHost = env;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment WebHost { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Images")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Images"));
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

            services.AddControllers(configure => configure.Filters.Add<RyExceptionFilter>());
            services.AddOptions<AppSettings>()
                .Configure(opt =>
                {
                    opt.HomePath = Directory.GetCurrentDirectory();
                    opt.ExecPath = Path.Combine(Directory.GetCurrentDirectory(), "FFMpeg");
                    opt.VideoUrl = Configuration["VideoUrl"];
                    opt.ImagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                    opt.AppStartTime = DateTime.Now;
                    opt.SlicesPeriod = int.TryParse(Configuration["SlicesPeriod"], out var i) ? i : 20;
                    opt.ImageFormat = Configuration["ImageFormat"] ?? "jpg";
                    opt.Command = Configuration["Command"] ?? "-f image2 -vf select='eq(pict_type\\,I)' -vsync 2 -qscale:v 2";
                    opt.PreserveCount =
                        long.TryParse(Configuration["PreserveCount"], out var count) ? count : 10000;
                    opt.FtpUrl = Configuration["FtpUrl"] ?? "";
                    opt.FtpUsername = Configuration["FtpUsername"] ?? "";
                    opt.FtpPassword = Configuration["FtpPassword"] ?? "";
                });
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(
                    "Data Source=" + Path.Combine(WebHost.ContentRootPath, "my-sqlite.db"),
                    serverDbContextOptionsBuilder =>
                    {
                        var minutes = (int)TimeSpan.FromMinutes(1).TotalSeconds;
                        serverDbContextOptionsBuilder.CommandTimeout(minutes);
                    });
            });
            services.AddHostedService<ImageTranformService>();
            services.AddHostedService<TimedObliterateService>();
            services.AddHostedService<TimedUploadService>();
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<RyExceptionMiddleware>();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Images")),
                RequestPath = "/Images"
            });

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
