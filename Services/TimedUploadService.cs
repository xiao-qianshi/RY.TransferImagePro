using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RY.TransferImagePro.Data;
using RY.TransferImagePro.Domain.Entity;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RY.TransferImagePro.Common;

namespace RY.TransferImagePro.Services
{
    public class TimedUploadService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedUploadService> _logger;
        private readonly IOptions<AppSettings> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;

        public TimedUploadService(ILogger<TimedUploadService> logger, IServiceScopeFactory serviceScopeFactory,
            IOptions<AppSettings> options)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _options = options;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始上传文件定时任务。");

            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("停止上传文件定时任务。");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("执行文件上传任务...");
            if (string.IsNullOrWhiteSpace(_options.Value.FtpUrl))
            {
                _logger.LogError("未配置FTP地址");
                return;
            }
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            try
            {
               
                if (db.Set<ImageInformation>().Any(t => t.HasUploaded != true))
                {
                    var ftp = new FtpHelper(_options.Value.FtpUrl, _options.Value.FtpUsername, _options.Value.FtpPassword);
                    var list = db.Set<ImageInformation>().Where(t => t.HasUploaded != true).OrderBy(t => t.Id).ToList();
                    foreach (var record in list)
                    {
                        //上传FTP
                        if (ftp.Upload(new FileInfo(record.FullName),
                            record.CreateTime.ToString("yyyyMMddHHmmssfff") + record.FileExtension))
                        {
                            record.HasUploaded = true;
                            record.UploadTime = DateTime.Now;
                            db.Set<ImageInformation>().Update(record);
                        }
                        
                    }
                    //db.SaveChanges();
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An error occurred writing to the " +
                    "database. Error: {Message}", ex.Message);
            }
        }
    }
}