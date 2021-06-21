using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RY.TransferImagePro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, loggingbuilder) =>
                {
                    //�÷�����Ҫ����Microsoft.Extensions.Logging���ƿռ�

                    loggingbuilder.AddFilter("System", LogLevel.Warning); //���˵�ϵͳĬ�ϵ�һЩ��־
                    loggingbuilder.AddFilter("Microsoft", LogLevel.Warning); //���˵�ϵͳĬ�ϵ�һЩ��־

                    //���Log4Net

                    //var path = Directory.GetCurrentDirectory() + "\\log4net.config"; 
                    //������������ʾlog4net.config�������ļ�����Ӧ�ó����Ŀ¼�£�Ҳ����ָ�������ļ���·��
                    loggingbuilder.AddLog4Net();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        }
    }
}