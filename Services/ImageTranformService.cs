using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RY.TransferImagePro.Data;
using RY.TransferImagePro.Domain.Entity;
using Xabe.FFmpeg;

namespace RY.TransferImagePro.Services
{
    public class ImageTranformService : BackgroundService
    {
        private readonly ILogger<ImageTranformService> _logger;
        private readonly IOptions<AppSettings> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private string _filePath = string.Empty;

        //private readonly AppDbContext _dbContext;
        public ImageTranformService(ILogger<ImageTranformService> logger, IOptions<AppSettings> options,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _options = options;
            _serviceScopeFactory = serviceScopeFactory;
            //_dbContext = dbContext;
            FFmpeg.SetExecutablesPath(_options.Value.ExecPath);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(_options.Value.VideoUrl + DateTime.Now);
            //var filePath = Path.Combine(_options.Value.ImagePath, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
            //CreateDir(filePath);
            //var fileName = "%05d.jpeg";
            //var fileFullPath = Path.Combine(filePath, fileName);

            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    _filePath = Path.Combine(_options.Value.ImagePath, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                    CreateDir(_filePath);
                    var fileName = $"%05d.{_options.Value.ImageFormat}";
                    var fileFullPath = Path.Combine(_filePath, fileName);
                    var conversion = FFmpeg.Conversions.New()
                            .AddParameter(
                                $" -i {_options.Value.VideoUrl} -t {TimeSpan.FromSeconds(_options.Value.SlicesPeriod).ToFFmpeg()} {_options.Value.Command} {fileFullPath} ")
                        ;
                    conversion.OnDataReceived += Conversion_OnDataReceived;
                    await conversion.Start(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
        }

        private void Conversion_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            var createTime = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(_filePath) && Directory.Exists(_filePath))
            {
                var latestFileName = Directory.GetFiles(_filePath, "*.jpg").OrderByDescending(t => t).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(latestFileName))
                {
                    var fileinfo = new FileInfo(latestFileName);
                    Task.Run(() =>
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<AppDbContext>();

                        try
                        {
                            db.ImageInformations.Add(new ImageInformation
                                {
                                    FileName = fileinfo.Name,
                                    FullName = fileinfo.FullName,
                                    CreateTime = createTime,
                                    FileExtension = fileinfo.Extension,
                                    FileSize = fileinfo.Length,
                                    Location = fileinfo.DirectoryName
                                }
                            );
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "An error occurred writing to the " +
                                "database. Error: {Message}", ex.Message);
                        }
                    });
                }
            }
        }

        private void CreateDir(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}