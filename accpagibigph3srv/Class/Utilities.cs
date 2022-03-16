using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace accpagibigph3srv
{
    class Utilities
    {
        public static string APP_NAME = "";

        private static string encryptionKey = "@cCP@g1bIgPH3*";
        public static DateTime SystemDate;
        public static string ConStr;

        public static string EncryptData(string data)
        {
            AllcardEncryptDecrypt.EncryptDecrypt enc = new AllcardEncryptDecrypt.EncryptDecrypt(encryptionKey);
            string encryptedData = enc.TripleDesEncryptText(data);
            enc = null;
            return encryptedData;
        }

        public static string DecryptData(string data)
        {
            AllcardEncryptDecrypt.EncryptDecrypt dec = new AllcardEncryptDecrypt.EncryptDecrypt(encryptionKey);
            string decryptedData = dec.TripleDesDecryptText(data);
            dec = null;
            return decryptedData;
        }

        public static string TimeStamp()
        {
            return DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + " ";
        }
        public static int IsProgramRunning(string Program)
        {
            System.Diagnostics.Process[] p;
            p = System.Diagnostics.Process.GetProcessesByName(Program.Replace(".exe", "").Replace(".EXE", ""));

            return p.Length;
        }

        public static bool DeleteFile(string strFile)
        {
            try
            {
                File.Delete(strFile);

                return true;
            }
            catch (Exception ex)
            {
                Program.LogToErrorLog(string.Format("{0} {1}", Path.GetFileName(strFile), "DeleteFile(): Runtime catched error " + ex.Message));
                return false;
            }
        }

        public static bool DeleteFolder(string dir)
        {
            try
            {
                Directory.Delete(dir, true);

                return true;
            }
            catch (Exception ex)
            {
                Program.LogToErrorLog(string.Format("{0} {1}", dir, "DeleteFolder(): Runtime catched error " + ex.Message));
                return false;
            }
        }

        public static bool MoveFolder(string dir1, string dir2)
        {
            try
            {
                if (!Directory.Exists(dir2)) Directory.Move(dir1, dir2);
                else
                {
                    Program.LogToErrorLog(string.Format("{0} already exists", dir2));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Program.LogToErrorLog(string.Format("Failed to move {0} to {1}. Runtime error catched {3}", dir1, dir2, ex.Message));
                return false;
            }
        }

        public static void CreatePreReqFolders(string workingFolder)
        {
            var folders = Program.config.WorkingFolders;
            foreach (var folder in folders.Split(','))
            {
                if (!Directory.Exists(string.Format(@"{0}\{1}", workingFolder, folder)))
                    Directory.CreateDirectory(string.Format(@"{0}\{1}", workingFolder, folder));
            }

            if (!workingFolder.Contains("RECARD")) if (!Directory.Exists(string.Format(@"{0}\RECARD", workingFolder)))
                    Directory.CreateDirectory(string.Format(@"{0}\RECARD", workingFolder));
        }

        public static bool IsFolderReserve(string folder)
        {
            bool bln = false;
            var workingFolders = Program.config.WorkingFolders;
            var reservedFolders = Program.config.ReservedFolders;
            foreach (var f in workingFolders.Split(','))
            {
                if (folder.ToUpper() == f.ToUpper()) return true;
            }
            foreach (var f in reservedFolders.Split(','))
            {
                if (folder.ToUpper() == f.ToUpper()) return true;
            }

            return bln;
        }

        public static void MoveFolderToExceptions(string workingFolder, string dir)
        {
            string folderName = dir.Substring(dir.LastIndexOf("\\") + 1);
            Directory.Move(dir, string.Format(@"{0}\EXCEPTIONS\{1}_{2}", workingFolder, folderName, DateTime.Now.ToString("yyyyMMdd_hhmmss")));
        }

        //private static void CombineFiles()
        //{
        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    foreach (string subDir in Directory.GetDirectories(@"F:\PAGIBIG\Temp"))
        //    {
        //        string folderName = subDir.Substring(subDir.LastIndexOf("\\") + 1);

        //        foreach (string _file in Directory.GetFiles(subDir))
        //        {
        //            using (StreamReader sr = new StreamReader(_file))
        //            {
        //                while (!sr.EndOfStream)
        //                {
        //                    string line = sr.ReadLine();
        //                    string logLine = "";
        //                    if (line.Trim() != "")
        //                    {
        //                        logLine = string.Format("{0}|{1}|{2}", line.Replace("\n", ""), Path.GetFileName(_file), folderName);
        //                        sb.Append(logLine + Environment.NewLine);
        //                        Console.WriteLine(logLine);
        //                    }
        //                }
        //                sr.Dispose();
        //                sr.Close();
        //            }
        //        }
        //    }

        //    File.WriteAllText(@"F:\PAGIBIG\Temp\conso.txt", sb.ToString());
        //}

        //private static void UploadSFTPBacklog(string dirPath)
        //{

        //    foreach (string _file in Directory.GetFiles(dirPath))
        //    {
        //        if (Path.GetExtension(_file).ToUpper() == ".TXT")
        //        {
        //            int totalRecord = File.ReadAllLines(_file).Length;
        //            int record = 1;

        //            LogToSystemLog(Path.GetFileName(_file));

        //            using (StreamReader sr = new StreamReader(_file))
        //            {
        //                while (!sr.EndOfStream)
        //                {
        //                    string line = sr.ReadLine();
        //                    if (line.ToString() != "")
        //                    {
        //                        try
        //                        {
        //                            string pagIBIGID = GetPagIBIGID(line);
        //                            string guid = line.Split('|')[0];
        //                            DateTime dtm = new FileInfo(_file).LastWriteTime;

        //                            //if (dtm.Year != 2020)
        //                            //{
        //                            if (!dal.AddSFTPv2("", pagIBIGID, guid, "TXT", Path.GetFileName(_file), dtm))
        //                                LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(_file), " failed to insert txt in sftp table. Error " + dal.ErrorMessage));
        //                            else
        //                            {
        //                                Console.WriteLine(Utilities.TimeStamp() + Path.GetFileName(_file) + ", " + dtm.ToString() + ", " + record.ToString("N0") + " of " + totalRecord.ToString("N0"));
        //                            }
        //                            //}
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            LogToErrorLog("UploadSFTPBacklog(): " + Path.GetFileName(_file) + ", " + line.Substring(0, 30) + ", " + ex.Message);
        //                        }

        //                        record += 1;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

    }

}
