using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace accpagibigph3srv
{
    class Config
    {
        public string WSRepo { get; set; }
        public string BankRepo { get; set; }
        public string DbaseConStr { get; set; }
        public string SftpHost { get; set; }
        public int SftpPort { get; set; }
        public string SftpUser { get; set; }
        public string SftpPass { get; set; }
        public string SftpKeyPath { get; set; }
        public int SendToSftp { get; set; }
        public string SftpSshHostKeyFingerprint { get; set; }
        public string BankWS { get; set; }
        public string SftpLocalPathUF { get; set; }
        public string SftpLocalPathCR { get; set; }
        public string SftpRemotePathUF { get; set; }
        public string SftpRemotePathCR { get; set; }
        public string SftpRemotePathUF_Zip { get; set; }
        public string SftpRemotePathCR_Zip { get; set; }
        public string GenerateCancelledMemFileFrom { get; set; }
        public string GenerateCancelledMemFileTo { get; set; }
        public int ProcessInterval { get; set; }
        public short EncryptPagibigMemUF { get; set; }
        public short EncryptPagibigMemCR { get; set; }
        public short WinScpSessionTimeout { get; set; }
        public string WorkingFolders { get; set; }
        public string ReservedFolders { get; set; }

    }
}
