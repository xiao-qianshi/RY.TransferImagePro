using System;
using System.Collections.Concurrent;
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
        /// <summary>
        ///     判断是否有视频输入
        /// </summary>
        private static bool _isRecording;

        private ConcurrentQueue<string> _concurrentQueue;

        private readonly ILogger<ImageTranformService> _logger;
        private readonly IOptions<AppSettings> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private string _filePath = string.Empty;

        private Timer _timer;

        //private readonly AppDbContext _dbContext;
        public ImageTranformService(ILogger<ImageTranformService> logger, IOptions<AppSettings> options,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _options = options;
            _serviceScopeFactory = serviceScopeFactory;
            //_dbContext = dbContext;
            FFmpeg.SetExecutablesPath(_options.Value.ExecPath);
            _concurrentQueue = new ConcurrentQueue<string>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(MonitorWork, null, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(5));
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }

        /// <summary>
        ///     检测视频输入状态
        /// </summary>
        /// <param name="state"></param>
        private void MonitorWork(object? state)
        {
            _timer?.Change(Timeout.Infinite, 0);
            try
            {
                var mediaInfo = FFmpeg.GetMediaInfo(_options.Value.VideoUrl).GetAwaiter().GetResult();
                if (mediaInfo.VideoStreams.Any(t => t.Bitrate > 350)) //存在波特率大于350的视频源
                {
                    _timer?.Change(60000, 5000);
                    _isRecording = true;
                }
                else
                {
                    _logger.LogInformation("无视频输入，地址" + _options.Value.VideoUrl);
                    _isRecording = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("检测视频源出错：" + ex.Message);
            }

            _timer?.Change(15000, 5000);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(_options.Value.VideoUrl + "" + DateTime.Now);
            //var filePath = Path.Combine(_options.Value.ImagePath, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
            //CreateDir(filePath);
            //var fileName = "%05d.jpeg";
            //var fileFullPath = Path.Combine(filePath, fileName);

            while (!stoppingToken.IsCancellationRequested /*&& _isRecording*/)
            {
                try
                {
                    _filePath = Path.Combine(_options.Value.ImagePath, "tempdir", DateTime.Now.ToString("yyyyMMdd"),
                        DateTime.Now.Hour.ToString(), DateTime.Now.ToString("mmss"));
                    _concurrentQueue.Enqueue(_filePath);
                    CreateDir(_filePath);
                    var fileName = $"%08d.{_options.Value.ImageFormat}";
                    var fileFullPath = Path.Combine(_filePath, fileName);
                    var conversion = FFmpeg.Conversions.New()
                            .AddParameter(
                                $" -i {_options.Value.VideoUrl} -t {TimeSpan.FromSeconds(_options.Value.SlicesPeriod).ToFFmpeg()} {_options.Value.Command} {fileFullPath} ")
                        ;
                    //conversion.OnDataReceived += Conversion_OnDataReceived;
                    conversion.OnProgress += Conversion_OnProgress;
                    await conversion.Start(stoppingToken);
                    //await Task.Run(() =>
                    //{
                    //    var files = Directory.GetFiles(_filePath, $"*.{_options.Value.ImageFormat}")
                    //        .OrderBy(t => t);
                    //    if (!files.Any()) return;
                    //    using var scope = _serviceScopeFactory.CreateScope();
                    //    var scopedServices = scope.ServiceProvider;
                    //    var db = scopedServices.GetRequiredService<AppDbContext>();
                    //    foreach (var file in files)
                    //    {
                    //        var infile = new FileInfo(file);
                    //        db.ImageInformations.Add(new ImageInformation
                    //            {
                    //                FileName = infile.Name,
                    //                FullName = infile.FullName,
                    //                CreateTime = infile.CreationTime,
                    //                FileExtension = infile.Extension,
                    //                FileSize = infile.Length,
                    //                Location = infile.DirectoryName
                    //            }
                    //        );
                    //    }

                    //    db.SaveChanges();
                    //});
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
            }
                
        }

        private void Conversion_OnProgress(object sender, Xabe.FFmpeg.Events.ConversionProgressEventArgs args)
        {
            //throw new NotImplementedException();
            //_logger.LogInformation(args.Percent.ToString());
            if (args.Percent < 100) return;
            var q = _concurrentQueue.TryDequeue(out var fileResult);
            if (!q) return;
            Task.Run(() =>
            {
                var files = Directory.GetFiles(fileResult, $"*.{_options.Value.ImageFormat}")
                    .OrderBy(t => t);
                if (!files.Any()) return;
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                foreach (var file in files)
                {
                    var infile = new FileInfo(file);
                    db.ImageInformations.Add(new ImageInformation
                        {
                            FileName = infile.Name,
                            FullName = infile.FullName,
                            CreateTime = infile.CreationTime,
                            FileExtension = infile.Extension,
                            FileSize = infile.Length,
                            Location = infile.DirectoryName
                        }
                    );
                }

                db.SaveChanges();
            });
        }

        private void Conversion_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            _logger.LogInformation(e.Data);
            var createTime = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(_filePath) && Directory.Exists(_filePath))
            {
                var latestFileName = Directory.GetFiles(_filePath, $"*.{_options.Value.ImageFormat}")
                    .OrderByDescending(t => t).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(latestFileName))
                {
                    //var newPath = Path.Combine(_options.Value.ImagePath, DateTime.Now.ToString("yyyyMMdd"),DateTime.Now.Hour.ToString());
                    //CreateDir(newPath);
                    //var finalFileName = Path.Combine(newPath, $"{DateTime.Now:yyyyMMddHHmmssffffff}.{_options.Value.ImageFormat}");
                    //Directory.Move(latestFileName, finalFileName);
                    var infile = new FileInfo(latestFileName);
                    Task.Run(() =>
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<AppDbContext>();

                        try
                        {
                            db.ImageInformations.Add(new ImageInformation
                                {
                                    FileName = infile.Name,
                                    FullName = infile.FullName,
                                    CreateTime = createTime,
                                    FileExtension = infile.Extension,
                                    FileSize = infile.Length,
                                    Location = infile.DirectoryName
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