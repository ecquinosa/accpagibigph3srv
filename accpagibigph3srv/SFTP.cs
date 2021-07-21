using System;
using System.Collections.Generic;
using WinSCP;
using System.IO;


namespace accpagibigph3srv
{
    class SFTP
    {
        private delegate void dlgtProcess();        

        private static string configFile = "sftpubp";        

        private static string SFTP_HOST = ""; //"172.18.3.214";
        private static int SFTP_PORT = 0; //22;
        private static string SFTP_USER = ""; //"allcarduser";
        private static string SFTP_PASS = ""; //"allcardus3r@2019";
        private static string SFTP_SshHostKeyFingerprint = ""; //"ssh-ed25519 256 9d:f4:bc:3d:bc:7e:07:f9:ca:e1:74:05:22:09:89:c1";
        
        public static string SFTP_LOCALPATH_UF = ""; 
        private static string SFTP_SFTPPATH_UF_ZIP = ""; 
        private static string SFTP_SFTPPATH_PAGIBIGMEMUF = "";

        public static string SFTP_LOCALPATH_CR = "";
        private static string SFTP_SFTPPATH_CR_ZIP = "";
        private static string SFTP_SFTPPATH_PAGIBIGMEMCR = "";

        private static DAL dal = new DAL();

        public SFTP()
        {
            using (StreamReader sr = new StreamReader(configFile))
            {
                while (!sr.EndOfStream)
                {
                    string strLine = sr.ReadLine();
                    if (strLine.Trim() != "")
                    {
                        switch (strLine.Split('=')[0])
                        {
                            case "SFTP_HOST":
                                SFTP_HOST = strLine.Split('=')[1];
                                break;
                            case "SFTP_PORT":
                                SFTP_PORT = Convert.ToInt32(strLine.Split('=')[1]);
                                break;
                            case "SFTP_USER":
                                SFTP_USER = strLine.Split('=')[1];
                                break;
                            case "SFTP_PASS":
                                SFTP_PASS = strLine.Split('=')[1];
                                break;
                            case "SFTP_SshHostKeyFingerprint":
                                SFTP_SshHostKeyFingerprint = strLine.Split('=')[1];
                                break;
                            case "SFTP_LOCALPATH_UF":
                                SFTP_LOCALPATH_UF = strLine.Split('=')[1];
                                break;
                            case "SFTP_SFTPPATH_UF_ZIP":
                                SFTP_SFTPPATH_UF_ZIP = strLine.Split('=')[1];
                                break;
                            case "SFTP_SFTPPATH_PAGIBIGMEMUF":
                                SFTP_SFTPPATH_PAGIBIGMEMUF = strLine.Split('=')[1];
                                break;
                            case "SFTP_LOCALPATH_CR":
                                SFTP_LOCALPATH_CR = strLine.Split('=')[1];
                                break;
                            case "SFTP_SFTPPATH_CR_ZIP":
                                SFTP_SFTPPATH_CR_ZIP = strLine.Split('=')[1];
                                break;
                            case "SFTP_SFTPPATH_PAGIBIGMEMCR":
                                SFTP_SFTPPATH_PAGIBIGMEMCR = strLine.Split('=')[1];
                                break;
                        }
                    }

                }
                sr.Dispose();
                sr.Close();
            }
        }
                
        private static SessionOptions sessionOptions()
        {
            return new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = SFTP_HOST,
                UserName = SFTP_USER,
                Password = SFTP_PASS,
                PortNumber = SFTP_PORT,
                SshHostKeyFingerprint = SFTP_SshHostKeyFingerprint
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
                    SFTP_LOCALPATH = SFTP_LOCALPATH_UF;
                    SFTP_SFTPPATH_ZIP = SFTP_SFTPPATH_UF_ZIP;
                    SFTP_SFTPPATH_PAGIBIGMEM = SFTP_SFTPPATH_PAGIBIGMEMUF;
                }
                else
                {
                    SFTP_LOCALPATH = SFTP_LOCALPATH_CR;
                    SFTP_SFTPPATH_ZIP = SFTP_SFTPPATH_CR_ZIP;
                    SFTP_SFTPPATH_PAGIBIGMEM = SFTP_SFTPPATH_PAGIBIGMEMCR;
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
                    SFTP_LOCALPATH = SFTP_LOCALPATH_UF;
                    SFTP_SFTPPATH_ZIP = SFTP_SFTPPATH_UF_ZIP;
                    SFTP_SFTPPATH_PAGIBIGMEM = SFTP_SFTPPATH_PAGIBIGMEMUF;
                }
                else
                {
                    SFTP_LOCALPATH = SFTP_LOCALPATH_CR;
                    SFTP_SFTPPATH_ZIP = SFTP_SFTPPATH_CR_ZIP;
                    SFTP_SFTPPATH_PAGIBIGMEM = SFTP_SFTPPATH_PAGIBIGMEMCR;
                }

                string forTransferFolder = SFTP_LOCALPATH + @"\FOR_TRANSFER_ZIP";
                if(!isZip) forTransferFolder = SFTP_LOCALPATH + @"\FOR_TRANSFER_MEM";

                if (!isZip)
                {
                    if (Directory.GetDirectories(forTransferFolder).Length == 0)
                    {
                        Console.WriteLine("{0}No daily folder(s) to sync", Utilities.TimeStamp());
                        Utilities.SaveToErrorLog(string.Format("{0}No daily folder(s) to sync", Utilities.TimeStamp()));
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
                        Console.WriteLine("{0}No zip file(s) to sync", Utilities.TimeStamp());
                        Utilities.SaveToErrorLog(string.Format("{0}No zip file(s) to sync", Utilities.TimeStamp()));
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

                dal.Dispose();
                dal = null;

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
                Console.WriteLine("{0}Upload of {1} succeeded", Utilities.TimeStamp(), Path.GetFileName(e.FileName));
                Utilities.SaveToSystemLog(string.Format("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error));
                File.Delete(e.FileName);

                if (Path.GetExtension(e.FileName).ToUpper() == ".TXT")
                {
                    //RenameFile(e.FileName);

                    if (!dal.UpdateSFTPTransferDateByPagIBIGMemFileName(Path.GetFileName(e.FileName)))
                        Utilities.SaveToErrorLog(string.Format("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error));
                }
                else
                {
                    if (!dal.UpdateSFTPTransferDate(Path.GetFileNameWithoutExtension(e.FileName),"ZIP"))
                        Utilities.SaveToErrorLog(string.Format("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error));
                }
                
            }
            else
            {
                FailedTransferredCntr += 1;
                Console.WriteLine("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error);
                Utilities.SaveToErrorLog(string.Format("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error));
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
                    Console.WriteLine("{0}Setting permissions of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Chmod.FileName), e.Chmod.Error);
                    Utilities.SaveToErrorLog(string.Format("{0}Setting permissions of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Chmod.FileName), e.Chmod.Error));
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
                    Console.WriteLine("{0}Setting timestamp of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Touch.FileName), e.Touch.Error);
                    Utilities.SaveToErrorLog(string.Format("{0}Setting timestamp of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Touch.FileName), e.Touch.Error));
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
                            fileInfo.Name, fileInfo.Length, fileInfo.LastWriteTime,fileInfo.FullName);
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

                    if (Path.GetFileNameWithoutExtension(sourceFile).Contains("UF")) sftp_path = string.Format(@"{0}/{1}",SFTP_SFTPPATH_PAGIBIGMEMUF, DateTime.Now.ToString("yyyyMMdd"));
                    else sftp_path = string.Format(@"{0}/{1}", SFTP_SFTPPATH_PAGIBIGMEMCR, DateTime.Now.ToString("yyyyMMdd"));

                    string _sourceFile = string.Format(@"{0}/{1}", sftp_path, Path.GetFileName(sourceFile));
                    string fromFile = Path.GetFileName(_sourceFile);
                    string toFile = Path.GetFileName(sourceFile).Replace("DUMP_", "");                    

                    if (session.FileExists(_sourceFile))
                    {   
                        session.MoveFile(_sourceFile, string.Format(@"{0}/{1}", sftp_path, toFile));                        
                        WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Filename is changed from " + fromFile + " to " + toFile));
                        isFileFound = true;
                    }
                    else
                    {                        
                        DateTime dtmStart = DateTime.Today.AddDays(-10);
                        DateTime dtmEnd = DateTime.Today.AddDays(3);
                        DateTime dtmRunningDate = dtmStart;

                        while (dtmEnd > dtmRunningDate)
                        {
                            if (Path.GetFileNameWithoutExtension(sourceFile).Contains("UF")) sftp_path = string.Format(@"{0}/{1}", SFTP_SFTPPATH_PAGIBIGMEMUF, dtmRunningDate.ToString("yyyyMMdd"));
                            else sftp_path = string.Format(@"{0}/{1}", SFTP_SFTPPATH_PAGIBIGMEMCR, dtmRunningDate.ToString("yyyyMMdd"));

                            _sourceFile = string.Format(@"{0}/{1}", sftp_path, Path.GetFileName(sourceFile));                                                        

                            if (session.FileExists(_sourceFile))
                            {
                                session.MoveFile(_sourceFile, string.Format(@"{0}/{1}", sftp_path, toFile));
                                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Filename is changed from " + fromFile + " to " + toFile));
                                isFileFound = true;
                                break;
                            }

                            dtmRunningDate = dtmRunningDate.Date.AddDays(1);
                        }
                    }
                }

                if(!isFileFound) WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Unable to find file " + Path.GetFileName(sourceFile)));

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Unable to find or failed to rename file " + Path.GetFileName(sourceFile)));
                return false;
            }
        }

        private static void WriteToLog(short type, string desc)
        {
            Console.WriteLine(desc);
            if (type == 0) Utilities.SaveToSystemLog(string.Format("[{0}] {1}", Utilities.APP_NAME, desc));
            if (type == 1) Utilities.SaveToErrorLog(string.Format("[{0}] {1}", Utilities.APP_NAME, desc));
        }
    }
}
