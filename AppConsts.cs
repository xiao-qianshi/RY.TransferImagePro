using System;

namespace RY.TransferImagePro
{
    public class AppSettings
    {
        public string HomePath { get; set; }
        public string ExecPath { get; set; }
        public string ImagePath { get; set; }
        public DateTime AppStartTime { get; set; }
        public string VideoUrl { get; set; }
        public long PreserveCount { get; set; } = 10000;

        public int SlicesPeriod { get; set; } = 20;

        public string ImageFormat { get; set; } = "jpg";

        public string Command { get; set; } = "-f image2 -vf select='eq(pict_type\\,I)' -vsync 2 -qscale:v 2";

        public string FtpUrl { get; set; }
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }
    }
}