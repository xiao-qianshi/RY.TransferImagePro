#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RY.TransferImagePro.Data;
using RY.TransferImagePro.Domain.Entity;

namespace RY.TransferImagePro.Services
{
    public class TimedUploadService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedUploadService> _logger;
        private readonly IOptions<AppSettings> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private FtpClient? _ftpClient = null;
        private Timer? _timer = null;

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

            _timer = new Timer(TransferImage, null, TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5));

            _ftpClient = string.IsNullOrEmpty(_options.Value.FtpSite.Username)
                ? new FtpClient($"ftp://{_options.Value.FtpSite.Host}:{_options.Value.FtpSite.Port}")
                : new FtpClient($"ftp://{_options.Value.FtpSite.Host}", _options.Value.FtpSite.Port,
                    _options.Value.FtpSite.Username, _options.Value.FtpSite.Password);
            _ftpClient.EncryptionMode = FtpEncryptionMode.None;
            _ftpClient.DataConnectionType = FtpDataConnectionType.PASV;
            _ftpClient.Encoding = Encoding.UTF8;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("停止上传文件定时任务。");

            _timer?.Change(Timeout.Infinite, 0);

            _ftpClient?.Dispose();

            return Task.CompletedTask;
        }


        private void TransferImage(object? state)
        {
            _logger.LogInformation("执行文件上传任务...");

            _timer?.Change(Timeout.Infinite, 0);
            if (_ftpClient != null && !_ftpClient.IsConnected)
            {
                try
                {
                    _ftpClient.Connect();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    return;
                }
               
            }
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            try
            {
                if (db.Set<ImageInformation>().Any(t => t.HasUploaded != true))
                {
                    var list = db.Set<ImageInformation>().Where(t => t.HasUploaded != true).OrderBy(t => t.Id).Take(300).ToList();
                    foreach (var record in list)
                        if (File.Exists(record.FullName))
                        {
                            //上传FTP
                            UploadFile(record.FullName, record.CreateTime.ToString("yyyyMMdd") + "/"+ record.CreateTime.Hour.ToString() + "/" + record.CreateTime.Minute.ToString() + "/" + record.CreateTime.ToString("yyyyMMddHHmmssffffff") + record.FileExtension);
                            record.HasUploaded = true;
                            record.UploadTime = DateTime.Now;
                            db.Set<ImageInformation>().Update(record);
                        }
                        else
                        {
                            db.Set<ImageInformation>().Remove(record);
                        }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            if (_ftpClient != null && _ftpClient.IsConnected)
            {
                _ftpClient.Disconnect();
            }
            _timer?.Change(5000, 5000);
        }

        /// <summary>
        ///     上传单个文件
        /// </summary>
        /// <param name="sourcePath">文件源路径</param>
        /// <param name="destPath">上传到指定的ftp文件夹路径</param>
        private void UploadFile(string sourcePath, string destPath)
        {
            _ftpClient?.UploadFile(sourcePath, destPath, createRemoteDir: true);
        }
    }
}