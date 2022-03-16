using System;
using System.Collections.Generic;
using WinSCP;
using System.IO;


namespace accpagibigph3srv
{
    class SFTP
    {
        private delegate void dlgtProcess();


        //private static DAL dal = new DAL();

        private static SessionOptions sessionOptions()
        {
            //Timeout = new TimeSpan(0, 2, 0); //2min

            return new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = Program.config.SftpHost,
                UserName = Program.config.SftpUser,
                Password = Program.config.SftpPass,
                PortNumber = Program.config.SftpPort,
                SshHostKeyFingerprint = Program.config.SftpSshHostKeyFingerprint,
                Timeout = new TimeSpan(0, Program.config.WinScpSessionTimeout, 0)
            };
        }

        public bool Upload_SFTP_Files(string memType, string path, bool IsZip, ref string errMsg)
        {
            try
            {
                string SFTP_LOCALPATH = "";
                string SFTP_SFTPPATH_ZIP = "";
                string SFTP_SFTPPATH_PAGIBIGMEM = "";

                if (memType == "UF")
                {
                    SFTP_LOCALPATH = Program.config.SftpLocalPathUF;
                    SFTP_SFTPPATH_ZIP = Program.config.SftpRemotePathUF_Zip;
                    SFTP_SFTPPATH_PAGIBIGMEM = Program.config.SftpRemotePathUF;
                }
                else
                {
                    SFTP_LOCALPATH = Program.config.SftpLocalPathCR;
                    SFTP_SFTPPATH_ZIP = Program.config.SftpRemotePathCR_Zip;
                    SFTP_SFTPPATH_PAGIBIGMEM = Program.config.SftpRemotePathCR;
                }


                int intFileCount = Directory.GetFiles(SFTP_LOCALPATH).Length;

                if (intFileCount == 0)
                {
                    errMsg = string.Format("[Upload] {0} is empty. No file to push.", SFTP_LOCALPATH);
                    return false;
                }

                using (Session session = new Session())
                {
                    session.DisableVersionCheck = true;
                    session.Open(sessionOptions());

                    // Upload files
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;
                    //transferOptions.ResumeSupport.State = TransferResumeSupportState.Smart;                  

                    //transferOptions.PreserveTimestamp = false;

                    //Console.Write(AppDomain.CurrentDomain.BaseDirectory);
                    string remotePath = SFTP_SFTPPATH_ZIP;
                    if (!IsZip) remotePath = SFTP_SFTPPATH_PAGIBIGMEM;

                    TransferOperationResult transferResult = null;
                    if (File.Exists(path))
                    {
                        {
                            if (!session.FileExists(remotePath + Path.GetFileName(path)))
                            {
                                transferResult = session.PutFiles(string.Format(@"{0}*", path), remotePath, false, transferOptions);
                            }

                            else
                            {
                                errMsg = string.Format("Upload_SFTP_Files(): Remote file exist " + Path.GetFileName(path));
                                return false;
                            }
                        }
                    }
                    else

                        transferResult = session.PutFiles(string.Format(@"{0}\*", SFTP_LOCALPATH), remotePath, false, transferOptions);


                    // Throw on any error
                    transferResult.Check();

                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        //Console.WriteLine(TimeStamp() + Path.GetFileName(transfer.FileName) + " transferred successfully");
                        //string strFilename = Path.GetFileName(transfer.FileName);
                        //File.Delete(transfer.FileName);
                    }
                }

                //Console.WriteLine("Success sftp transfer " + path);
                //System.Threading.Thread.Sleep(100);

                return true;

            }
            catch (Exception ex)
            {
                errMsg = string.Format("Upload_SFTP_Files(): Runtime error {0}", ex.Message);
                Console.WriteLine(errMsg);
                //Utilities.WriteToRTB(errMsg, ref rtb, ref tssl);
                return false;
            }
        }

        public bool SynchronizeDirectories(string memType, bool isZip, ref string errMsg)
        {
            try
            {
                string SFTP_LOCALPATH = "";
                string SFTP_SFTPPATH_ZIP = "";
                string SFTP_SFTPPATH_PAGIBIGMEM = "";

                if (memType == "UF")
                {
                    SFTP_LOCALPATH = Program.config.SftpLocalPathUF;
                    SFTP_SFTPPATH_ZIP = Program.config.SftpRemotePathUF_Zip;
                    SFTP_SFTPPATH_PAGIBIGMEM = Program.config.SftpRemotePathUF;
                }
                else
                {
                    SFTP_LOCALPATH = Program.config.SftpLocalPathCR;
                    SFTP_SFTPPATH_ZIP = Program.config.SftpRemotePathCR_Zip;
                    SFTP_SFTPPATH_PAGIBIGMEM = Program.config.SftpRemotePathCR;
                }

                string forTransferFolder = SFTP_LOCALPATH + @"\FOR_TRANSFER_ZIP";
                if (!isZip) forTransferFolder = SFTP_LOCALPATH + @"\FOR_TRANSFER_MEM";

                if (!isZip)
                {
                    if (Directory.GetDirectories(forTransferFolder).Length == 0)
                    {
                        Program.LogToErrorLog("No daily folder(s) to sync");
                        return true;
                    }
                }
                else
                {
                    int folderContents = 0;
                    folderContents += Directory.GetDirectories(forTransferFolder).Length;
                    folderContents += Directory.GetFiles(forTransferFolder).Length;

                    //if (Directory.GetDirectories(forTransferFolder).Length == 0)
                    if (folderContents == 0)
                    {
                        Program.LogToErrorLog("No zip file(s) to sync");
                        return true;
                    }

                    ////without sub folder by Date
                    //Console.WriteLine("{0}No zip file(s) to sync", Utilities.TimeStamp());
                    //Utilities.SaveToErrorLog(string.Format("{0}No zip file(s) to sync", Utilities.TimeStamp()));
                    //return true;
                }

                string sftpFolder = SFTP_SFTPPATH_ZIP;
                if (!isZip) sftpFolder = SFTP_SFTPPATH_PAGIBIGMEM;

                using (Session session = new Session())
                {
                    // Will continuously report progress of synchronization
                    session.FileTransferred += FileTransferred;

                    // Connect
                    session.Open(sessionOptions());

                    // Synchronize files
                    SynchronizationResult synchronizationResult;

                    try
                    {
                        //synchronizationResult = session.SynchronizeDirectories(SynchronizationMode.Remote, @"D:\ACCPAGIBIGPH3\UBP\FOR_TRANSFER_ZIP", "/upload/pagibig/MemberFiles/DataCaptureFiles",false);
                        synchronizationResult = session.SynchronizeDirectories(SynchronizationMode.Remote, forTransferFolder, sftpFolder, false);

                        // Throw on any error
                        synchronizationResult.Check();
                    }
                    catch (Exception ex2)
                    {
                        errMsg = string.Format("SynchronizeDirectories(2): Runtime error {0}", ex2.Message);
                        Console.WriteLine(errMsg);
                        SynchronizeDirectories(memType, isZip, ref errMsg);
                        return false;
                        //return false;
                    }
                }               

                return true;
            }
            catch (Exception ex)
            {
                errMsg = string.Format("SynchronizeDirectories(): Runtime error {0}", ex.Message);
                Console.WriteLine(errMsg);
                SynchronizeDirectories(memType, isZip, ref errMsg);
                return false;
            }
        }

        public static int SuccessTransferredCntr { get; set; }
        public static int FailedTransferredCntr { get; set; }


        private static void FileTransferred(object sender, TransferEventArgs e)
        {
            if (e.Error == null)
            {
                SuccessTransferredCntr += 1;
                Program.LogToSystemLog(String.Format("Upload of {0} succeeded", Path.GetFileName(e.FileName)));
                File.Delete(e.FileName);

                if (Path.GetExtension(e.FileName).ToUpper() == ".TXT")
                {
                    //RenameFile(e.FileName);

                    if (!Program.dal.UpdateSFTPTransferDateByPagIBIGMemFileName(Path.GetFileName(e.FileName)))
                        Program.LogToErrorLog(string.Format("Upload of {0} failed: {1}", Path.GetFileName(e.FileName), e.Error));
                }
                else
                {
                    if (!Program.dal.UpdateSFTPTransferDate(Path.GetFileNameWithoutExtension(e.FileName), "ZIP"))
                        Program.LogToErrorLog(string.Format("Upload of {0} failed: {1}", Path.GetFileName(e.FileName), e.Error));
                }

            }
            else
            {
                FailedTransferredCntr += 1;
                Program.LogToErrorLog(string.Format("Upload of {0} failed: {1}", Path.GetFileName(e.FileName), e.Error));
            }

            if (e.Chmod != null)
            {
                if (e.Chmod.Error == null)
                {
                    Console.WriteLine(
                        "{0}Permissions of {1} set to {2}", Utilities.TimeStamp(), Path.GetFileName(e.Chmod.FileName), e.Chmod.FilePermissions);
                }
                else
                {
                    Program.LogToErrorLog(string.Format("Setting permissions of {0} failed: {1}", Path.GetFileName(e.Chmod.FileName), e.Chmod.Error));
                }
            }
            else
            {
                //Console.WriteLine("{0}Permissions of {1} kept with their defaults", TimeStamp(), e.Destination);
            }

            if (e.Touch != null)
            {
                if (e.Touch.Error == null)
                {
                    Console.WriteLine(
                        "{0}Timestamp of {1} set to {2}", Utilities.TimeStamp(), Path.GetFileName(e.Touch.FileName), e.Touch.LastWriteTime);
                }
                else
                {
                    Program.LogToErrorLog(string.Format("Setting timestamp of {0} failed: {1}", Path.GetFileName(e.Touch.FileName), e.Touch.Error));
                }
            }
            else
            {
                // This should never happen during "local to remote" synchronization
                Console.WriteLine("{0}Timestamp of {1} kept with its default (current time)", Utilities.TimeStamp(), e.Destination);
            }
        }


        public bool ReadSFTPDirectory(string dir, ref string errMsg)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(dir + Environment.NewLine);

                using (Session session = new Session())
                {
                    // Connect
                    session.Timeout = TimeSpan.MaxValue;
                    session.Open(sessionOptions());

                    RemoteDirectoryInfo directory =
                        session.ListDirectory(dir);

                    //foreach (RemoteDirectoryInfo dirInfo in directory.)
                    //{
                    //    Console.WriteLine(
                    //        "{0} with size {1}, permissions {2} and last modification at {3}",
                    //        fileInfo.Name, fileInfo.Length, fileInfo.FilePermissions,                    

                    foreach (RemoteFileInfo fileInfo in directory.Files)
                    {
                        string log = string.Format(
                            "{0},{1},{2},{3}",
                            fileInfo.Name, fileInfo.Length, fileInfo.LastWriteTime, fileInfo.FullName);
                        Console.WriteLine(log);
                        sb.Append(log + Environment.NewLine);
                    }
                }

                File.WriteAllText(string.Format(@"D:\ACCPAGIBIGPH3\accpagibigph3srv - Copy\sftplist_{0}.txt", DateTime.Now.ToString("hhmmss")), sb.ToString());

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                errMsg = e.Message;
                return false;
            }
        }

        public static bool RenameFile(string sourceFile)
        {
            string sftp_path = "";
            try
            {
                bool isFileFound = false;

                using (Session session = new Session())
                {
                    // Will continuously report progress of synchronization                

                    // Connect
                    session.Open(sessionOptions());

                    if (Path.GetFileNameWithoutExtension(sourceFile).Contains("UF")) sftp_path = string.Format(@"{0}/{1}", Program.config.SftpRemotePathUF, DateTime.Now.ToString("yyyyMMdd"));
                    else sftp_path = string.Format(@"{0}/{1}", Program.config.SftpRemotePathCR, DateTime.Now.ToString("yyyyMMdd"));

                    string _sourceFile = string.Format(@"{0}/{1}", sftp_path, Path.GetFileName(sourceFile));
                    string fromFile = Path.GetFileName(_sourceFile);
                    string toFile = Path.GetFileName(sourceFile).Replace("DUMP_", "");

                    if (session.FileExists(_sourceFile))
                    {
                        session.MoveFile(_sourceFile, string.Format(@"{0}/{1}", sftp_path, toFile));
                        Program.LogToSystemLog(string.Format("{0}", "Filename is changed from " + fromFile + " to " + toFile));
                        isFileFound = true;
                    }
                    else
                    {
                        DateTime dtmStart = DateTime.Today.AddDays(-10);
                        DateTime dtmEnd = DateTime.Today.AddDays(3);
                        DateTime dtmRunningDate = dtmStart;

                        while (dtmEnd > dtmRunningDate)
                        {
                            if (Path.GetFileNameWithoutExtension(sourceFile).Contains("UF")) sftp_path = string.Format(@"{0}/{1}", Program.config.SftpRemotePathUF, dtmRunningDate.ToString("yyyyMMdd"));
                            else sftp_path = string.Format(@"{0}/{1}", Program.config.SftpRemotePathCR, dtmRunningDate.ToString("yyyyMMdd"));

                            _sourceFile = string.Format(@"{0}/{1}", sftp_path, Path.GetFileName(sourceFile));

                            if (session.FileExists(_sourceFile))
                            {
                                session.MoveFile(_sourceFile, string.Format(@"{0}/{1}", sftp_path, toFile));
                                Program.LogToSystemLog(string.Format("{0}", "Filename is changed from " + fromFile + " to " + toFile));
                                isFileFound = true;
                                break;
                            }

                            dtmRunningDate = dtmRunningDate.Date.AddDays(1);
                        }
                    }
                }

                if (!isFileFound) Program.LogToErrorLog(string.Format("{0}", "Unable to find file " + Path.GetFileName(sourceFile)));

                return true;
            }
            catch (Exception ex)
            {
                Program.LogToErrorLog(string.Format("{0}", "Unable to find or failed to rename file " + Path.GetFileName(sourceFile)));
                return false;
            }
        }
    }
}
