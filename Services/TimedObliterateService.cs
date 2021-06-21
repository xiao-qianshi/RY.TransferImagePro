using System;
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

namespace RY.TransferImagePro.Services
{
    public class TimedObliterateService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedObliterateService> _logger;
        private readonly IOptions<AppSettings> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;

        public TimedObliterateService(ILogger<TimedObliterateService> logger, IServiceScopeFactory serviceScopeFactory,
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
            _logger.LogInformation("开始删除文件定时任务。");

            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("停止删除文件定时任务。");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("执行文件删除任务...");
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            try
            {
                var count = db.Set<ImageInformation>().LongCount();
                if (count <= _options.Value.PreserveCount) return;
                var i = (int) (count - _options.Value.PreserveCount);
                var list = db.Set<ImageInformation>().OrderBy(t => t.Id).Take(i).ToList();
                foreach (var record in list.Where(record => File.Exists(record.FullName)))
                    File.Delete(record.FullName);
                db.Set<ImageInformation>().RemoveRange(list);
                db.SaveChanges();
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