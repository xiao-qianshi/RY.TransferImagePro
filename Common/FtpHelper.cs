using System;
using System.IO;
using System.Net;

namespace RY.TransferImagePro.Common
{
    internal class FtpHelper
    {
        // 默认常量定义
        private static readonly string rootPath = "/";
        private static readonly int defaultReadWriteTimeout = 300000;
        private static readonly int defaultFtpPort = 21;

        /// <summary>
        ///     拼接URL
        /// </summary>
        /// <param name="host">主机名</param>
        /// <param name="remotePath">地址</param>
        /// <param name="fileName">文件名</param>
        /// <returns>返回完整的URL</returns>
        private string UrlCombine(string host, string remotePath, string fileName)
        {
            var result = new Uri(new Uri(new Uri(host.TrimEnd('/')), remotePath), fileName).ToString();
            return result;
        }

        /// <summary>
        ///     创建连接
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="method">方法</param>
        /// <returns>返回 request对象</returns>
        private FtpWebRequest CreateConnection(string url, string method)
        {
            var request = (FtpWebRequest) WebRequest.Create(new Uri(url));
            request.Credentials = new NetworkCredential(Username, Password);
            request.Proxy = Proxy;
            request.KeepAlive = false;
            request.UseBinary = UserBinary;
            request.UsePassive = UsePassive;
            request.EnableSsl = EnableSsl;
            request.Method = method;
            //Console.WriteLine(request);
            return request;
        }

        /// <summary>
        ///     上传文件
        /// </summary>
        /// <param name="localFile">本地文件</param>
        /// <param name="remoteFileName">上传文件名</param>
        /// <returns>上传成功返回 true</returns>
        public bool Upload(FileInfo localFile, string remoteFileName)
        {
            var result = false;
            if (localFile.Exists)
            {
                try
                {
                    var url = UrlCombine(Host, RemotePath, remoteFileName);
                    var request = CreateConnection(url, WebRequestMethods.Ftp.UploadFile);

                    using (var rs = request.GetRequestStream())
                    using (var fs = localFile.OpenRead())
                    {
                        var buffer = new byte[1024 * 4];
                        var count = fs.Read(buffer, 0, buffer.Length);
                        while (count > 0)
                        {
                            rs.Write(buffer, 0, count);
                            count = fs.Read(buffer, 0, buffer.Length);
                        }

                        fs.Close();
                        result = true;
                    }
                }
                catch (WebException ex)
                {
                    // MessageBox.Show(ex.Message);
                }

                return result;
            }

            // 处理本地文件不存在的情况
            return false;
        }


        /// <summary>
        ///     下载文件
        /// </summary>
        /// <param name="serverName">服务器文件名称</param>
        /// <param name="localName">需要保存在本地的文件名称</param>
        /// <returns>下载成功返回 true</returns>
        public bool Download(string serverName, string localName)
        {
            var result = false;
            using (var fs = new FileStream(localName, FileMode.OpenOrCreate))
            {
                try
                {
                    var url = UrlCombine(Host, RemotePath, serverName);
                    //Console.WriteLine(url);

                    var request = CreateConnection(url, WebRequestMethods.Ftp.DownloadFile);
                    request.ContentOffset = fs.Length;
                    using (var response = (FtpWebResponse) request.GetResponse())
                    {
                        fs.Position = fs.Length;
                        var buffer = new byte[1024 * 4];
                        var count = response.GetResponseStream().Read(buffer, 0, buffer.Length);
                        while (count > 0)
                        {
                            fs.Write(buffer, 0, count);
                            count = response.GetResponseStream().Read(buffer, 0, buffer.Length);
                        }

                        response.GetResponseStream().Close();
                    }

                    result = true;
                }
                catch (WebException ex)
                {
                    // 处理ftp连接中的异常
                }
            }

            return result;
        }

        #region 设置初始化参数

        private readonly string host = string.Empty;

        public string Host => host ?? string.Empty;

        public string Username { get; } = string.Empty;

        public string Password { get; } = string.Empty;

        public IWebProxy Proxy { get; set; }

        public int Port { get; set; } = defaultFtpPort;

        public bool EnableSsl { get; }

        public bool UsePassive { get; set; } = true;

        public bool UserBinary { get; set; } = true;

        private string remotePath = rootPath;

        public string RemotePath
        {
            get => remotePath;
            set
            {
                var result = rootPath;
                if (!string.IsNullOrEmpty(value) && value != rootPath)
                    result = Path.Combine(Path.Combine(rootPath, value.TrimStart('/').TrimEnd('/')), "/"); // 进行路径的拼接
                remotePath = result;
            }
        }

        public int ReadWriteTimeout { get; set; } = defaultReadWriteTimeout;

        #endregion

        #region 构造函数

        public FtpHelper(string host, string username, string password)
            : this(host, username, password, defaultFtpPort, null, false, true, true, defaultReadWriteTimeout)
        {
        }

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="host">主机名</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="port">端口号 默认21</param>
        /// <param name="proxy">代理 默认没有</param>
        /// <param name="enableSsl">是否使用ssl 默认不用</param>
        /// <param name="useBinary">使用二进制</param>
        /// <param name="usePassive">获取或设置客户端应用程序的数据传输过程的行为</param>
        /// <param name="readWriteTimeout">读写超时时间 默认5min</param>
        public FtpHelper(string host, string username, string password, int port, IWebProxy proxy, bool enableSsl,
            bool useBinary, bool usePassive, int readWriteTimeout)
        {
            this.host = host.ToLower().StartsWith("ftp://") ? host : "ftp://" + host;
            Username = username;
            Password = password;
            Port = port;
            Proxy = proxy;
            EnableSsl = enableSsl;
            UserBinary = useBinary;
            UsePassive = usePassive;
            ReadWriteTimeout = readWriteTimeout;
        }

        #endregion

        ////main函数
        //static void Main(string[] args)
        //{
        //    FtpHelper ftpHelper = new FtpHelper("172.17.204.59", "", "");
        //    //下载
        //    //ftpHelper.Download("java.rar", "D:\\java1.rar");
        //    //上传
        //    FileInfo fileInfo = new FileInfo("d:\\java1.rar");
        //    ftpHelper.Upload(fileInfo, "aaa.rar");
        //}
    }
}