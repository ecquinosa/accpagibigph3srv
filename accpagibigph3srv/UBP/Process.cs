
using System.IO;
using System.Linq;
using System.Text;

namespace accpagibigph3srv.UBP
{
    class Process
    {

        private static string compressedFilesRepo = "UBP\\SFTP";

        public static bool PackDataForUBP(string path, string acctNo, ref string outputPath, ref string errMsg)
        {
            bool isPhotoExist = false;
            bool isSignatureExist = false;
            bool isLPANSIExist = false;
            bool isLBANSIExist = false;
            bool isRPANSIExist = false;
            bool isRBANSIExist = false;
            bool isLPWSQExist = false;
            bool isLBWSQExist = false;
            bool isRPWSQExist = false;
            bool isRBWSQExist = false;

            StringBuilder sb = new StringBuilder();

            outputPath = string.Format(@"{0}\{1}", compressedFilesRepo, acctNo);

            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            foreach (string file in Directory.GetFiles(path))
            {
                if (Path.GetFileName(file).Contains("_Photo"))
                {
                    File.Copy(file,string.Format(@"{0}\{1}ph", outputPath, acctNo) + Path.GetExtension(file),true);
                    isPhotoExist = true;
                }

                if (Path.GetFileName(file).Contains("_Signature"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}s.jpg", outputPath, acctNo), true);
                    isSignatureExist = true;
                }

                if (Path.GetFileName(file).Contains("_Lprimary.wsq"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPLP", outputPath, acctNo) + Path.GetExtension(file), true);
                    isLPWSQExist = true;
                }

                if (Path.GetFileName(file).Contains("_Lbackup.wsq"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPLB", outputPath, acctNo) + Path.GetExtension(file), true);
                    isLBWSQExist = true;
                }

                if (Path.GetFileName(file).Contains("_Rprimary.wsq"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPRP", outputPath, acctNo) + Path.GetExtension(file), true);
                    isRPWSQExist = true;
                }

                if (Path.GetFileName(file).Contains("_Rbackup.wsq"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPRB", outputPath, acctNo) + Path.GetExtension(file), true);
                    isRBWSQExist = true;
                }

                if (Path.GetFileName(file).Contains("_Lprimary.ansi-fmr"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPLP", outputPath, acctNo) + Path.GetExtension(file), true);
                    isLPANSIExist = true;
                }

                if (Path.GetFileName(file).Contains("_Lbackup.ansi-fmr"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPLB", outputPath, acctNo) + Path.GetExtension(file), true);
                    isLBANSIExist = true;
                }

                if (Path.GetFileName(file).Contains("_Rprimary.ansi-fmr"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPRP", outputPath, acctNo) + Path.GetExtension(file), true);
                    isRPANSIExist = true;
                }

                if (Path.GetFileName(file).Contains("_Rbackup.ansi-fmr"))
                {
                    File.Copy(file, string.Format(@"{0}\{1}FPRB", outputPath, acctNo) + Path.GetExtension(file), true);
                    isRBANSIExist = true;
                }
            }

            return true;
        }
    }
}
