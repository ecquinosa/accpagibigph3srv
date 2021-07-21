
using System;
using System.Data;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace accpagibigph3srv
{
    class Program
    {

        #region Constructors
        
        private static string WS_REPO = "";
        private static string UBP_REPO = "";
        private static int PROCESS_INTERVAL_SECONDS = 120;
        private static short SEND_TO_SFTP;
        private static short ENCRYPT_PAGIBIGMEM_UF;
        private static short ENCRYPT_PAGIBIGMEM_CR;
        private static string GENERATE_CANCELLED_MEMFILE_TIME_FROM = "";
        private static string GENERATE_CANCELLED_MEMFILE_TIME_TO = "";
        //private static string DBASE_CONSTR = ""; 

        private static string FileCntr = "FileCntr";
        private static string ProcessType = "";

        private delegate void dlgtProcess();

        private static System.Threading.Thread _thread;

        private static string configFile = AppDomain.CurrentDomain.BaseDirectory + "config";

        private static DAL dal = new DAL();

        #endregion

        private static void FindLine(string mid, ref string line)
        {
            foreach (string _file in Directory.GetFiles(@"F:\Projects\accpagibigph3srv_sftp_monitoring\accpagibigph3srv_sftp_monitoring\bin\Debug\New folder"))
            {
                using (StreamReader sr2 = new StreamReader(_file))
                {
                    while (!sr2.EndOfStream)
                    {
                        string line2 = sr2.ReadLine();

                        if (line2.Trim() != "")
                        {
                            if (mid == line2.Split('|')[32])
                            {
                                line = "|" + Path.GetFileNameWithoutExtension(_file).Split('_')[0] + ".txt|" + new FileInfo(_file).LastWriteTime.ToString();
                                return;
                            }
                            else if (mid == line2.Split('|')[31])
                            {
                                line = "|" + Path.GetFileNameWithoutExtension(_file).Split('_')[0] + ".txt|" + new FileInfo(_file).LastWriteTime.ToString();
                                return;
                            }
                            
                        }
                    }
                    sr2.Dispose();
                    sr2.Close();
                }
            }
        }

        static void Main()//string[] args)
        {           
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            Utilities.APP_NAME = Path.GetFileName(codeBase);

            ////temp
            //Utilities.APP_NAME = Utilities.APP_NAME.Replace(".exe", "TXT.exe");
            //Utilities.APP_NAME = Utilities.APP_NAME.Replace(".exe","ZIP.exe");

            WriteToLog(0, string.Format("{0}Application started [{1}]", Utilities.TimeStamp(), Utilities.APP_NAME));            

            if (IsProgramRunning(Utilities.APP_NAME) > 1) return;

            while (!Init()) System.Threading.Thread.Sleep(5000);           

            System.Threading.Thread.Sleep(5000);
         

            dlgtProcess _delegate = new dlgtProcess(RunProcess);
            _delegate.Invoke();
            _delegate = null;
        }

        private void Misc()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (StreamReader sr = new StreamReader(@"F:\guid_list.txt"))
            {
                while (!sr.EndOfStream)
                {
                    string strLine = sr.ReadLine();
                    if (strLine.Trim() != "")
                    {
                        sb.AppendLine(strLine.Split('|')[0] + "|" + strLine.Split('|')[1] + "|" + Utilities.DecryptData(strLine.Split('|')[1]) + "|" + strLine.Split('|')[2]);
                    }
                }
            }

            System.IO.File.WriteAllText(@"F:\guid_list2.txt", sb.ToString());

            //CombineFiles();

            return;

            ////temp
            //string dirTemp = @"F:\Projects\accpagibigph3srv_sftp_monitoring\New folder (2)";
            ////foreach (string subDir in Directory.GetDirectories(dirTemp))
            ////{
            ////    UploadSFTPBacklog(subDir);
            ////}

            //UploadSFTPBacklog(dirTemp);
            //return;

            //StartThread();
            ReadSFTPDirectory();
            //SFTP sftp = new SFTP();
            //tp.RenameFile("", "");
            return;
        }

        private static bool Init()
        {
            try
            {
                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Checking config..."));

                if (!File.Exists(configFile))
                {
                    WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Config file is missing"));
                    return false;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(configFile))
                    {
                        while (!sr.EndOfStream)
                        {
                            string strLine = sr.ReadLine();
                            switch (strLine.Trim().Split('=')[0].ToUpper())
                            {
                                case "WS_REPO":
                                    WS_REPO = strLine.Trim().Split('=')[1];
                                    break;
                                case "UBP_REPO":
                                    UBP_REPO = strLine.Trim().Split('=')[1];
                                    break;
                                case "PROCESS_INTERVAL_SECONDS":
                                    PROCESS_INTERVAL_SECONDS = Convert.ToInt32(strLine.Trim().Split('=')[1]);
                                    break;
                                case "SEND_TO_SFTP":
                                    SEND_TO_SFTP = Convert.ToInt16(strLine.Trim().Split('=')[1]);
                                    break;
                                case "ENCRYPT_PAGIBIGMEM_UF":
                                    ENCRYPT_PAGIBIGMEM_UF = Convert.ToInt16(strLine.Trim().Split('=')[1]);
                                    break;
                                case "ENCRYPT_PAGIBIGMEM_CR":
                                    ENCRYPT_PAGIBIGMEM_CR = Convert.ToInt16(strLine.Trim().Split('=')[1]);
                                    break;
                                //case "DBASE_CONSTR":
                                //    DBASE_CONSTR = Convert.ToInt16(strLine.Trim().Split('=')[1]);
                                //    break;
                                case "GENERATE_CANCELLED_MEMFILE_TIME":
                                    GENERATE_CANCELLED_MEMFILE_TIME_FROM = strLine.Trim().Split('=')[1].Split('-')[0];
                                    GENERATE_CANCELLED_MEMFILE_TIME_TO = strLine.Trim().Split('=')[1].Split('-')[1];
                                    break;                                    
                            }
                        }

                        sr.Dispose();
                        sr.Close();
                    }
                }
                catch (Exception ex)
                {
                    WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Init(): Error reading config file. Runtime catched error " + ex.Message));
                    return false;
                }

                if(dal.IsConnectionOK())
                    WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Init(): Connection to database is success"));
                else
                    WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Init(): Connection to database failed. " + dal.ErrorMessage));


                return true;
            }
            catch (Exception ex)
            {
                WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "Init(): Runtime catched error " + ex.Message));
                return false;
            }
        }

        private static void StartThread()
        {
            System.Threading.Thread objNewThread = new System.Threading.Thread(Thread);
            objNewThread.Start();
            _thread = objNewThread;
        }

        private static void Thread()
        {
            try
            {
                while (true)
                {
                    dlgtProcess _delegate = new dlgtProcess(RunProcess);
                    _delegate.Invoke();
                    _delegate = null;
                }
            }
            catch (Exception ex)
            {
                Utilities.SaveToErrorLog(Utilities.TimeStamp() + "ProgramThread(): Runtime catched error " + ex.Message);
            }
        }

        private static void RunProcess()
        {
            WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Creating pre-req folders..."));
            CreatePreReqFolders(UBP_REPO);
            CreatePreReqFolders(string.Format(@"{0}\RECARD", UBP_REPO));

            string UBP_REPO_RECARD = string.Format(@"{0}\RECARD", UBP_REPO);

            if (Utilities.APP_NAME.Contains("TXT"))
            {
                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Segregation of textfiles and folders..."));
                SegregateMemFileAndFolders(UBP_REPO);
                SegregateMemFileAndFolders(UBP_REPO_RECARD);

                DateTime cancelledMemFileTime_From = Convert.ToDateTime(string.Format("{0} {1}",DateTime.Now.Date.ToShortDateString(),GENERATE_CANCELLED_MEMFILE_TIME_FROM));
                DateTime cancelledMemFileTime_To = Convert.ToDateTime(string.Format("{0} {1}", DateTime.Now.Date.ToShortDateString(), GENERATE_CANCELLED_MEMFILE_TIME_TO));

                if (DateTime.Now >= cancelledMemFileTime_From & DateTime.Now <= cancelledMemFileTime_To)
                {
                    WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Consolidation of fc textfiles..."));
                    if (GenerateCancelledMemFile())
                    {
                        ConsolidateMemFile(UBP_REPO, "FOR_PAGIBIGMEMCONSO_FC", ENCRYPT_PAGIBIGMEM_UF);
                        ConsolidateMemFile(UBP_REPO_RECARD, "FOR_PAGIBIGMEMCONSO_FC", ENCRYPT_PAGIBIGMEM_CR);
                    }                    
                }                

                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Consolidation of textfiles..."));
                ConsolidateMemFile(UBP_REPO, "FOR_PAGIBIGMEMCONSO", ENCRYPT_PAGIBIGMEM_UF);
                ConsolidateMemFile(UBP_REPO_RECARD, "FOR_PAGIBIGMEMCONSO", ENCRYPT_PAGIBIGMEM_CR);

                if (SEND_TO_SFTP == 1)
                {
                    WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Synchronizing local folder and sftp folder [" + Utilities.APP_NAME + "]..."));
                    SynchonizeFolder(UBP_REPO, "UF", false);
                    SynchonizeFolder(UBP_REPO_RECARD, "CR", false);
                }

            }

            if (Utilities.APP_NAME.Contains("ZIP"))
            {
                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Compressing folder(s)..."));
                ZipFolders(UBP_REPO);                
                ZipFolders(UBP_REPO_RECARD);

                if (SEND_TO_SFTP == 1)
                {
                    WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Synchronizing local folder and sftp folder [" + Utilities.APP_NAME + "]..."));
                    SynchonizeFolder(UBP_REPO, "UF", true);
                    SynchonizeFolder(UBP_REPO_RECARD, "CR", true);
                }
            }

            HouseKeeping(UBP_REPO);
            HouseKeeping(UBP_REPO_RECARD);

            WriteToLog(0, string.Format("{0}Application close [{1}]", Utilities.TimeStamp(), Utilities.APP_NAME));
        }

        private static bool DeleteFile(string strFile)
        {
            try
            {
                File.Delete(strFile);

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog(1, string.Format("{0}{1} {2}", Utilities.TimeStamp(), Path.GetFileName(strFile), "DeleteFile(): Runtime catched error " + ex.Message));
                return false;
            }
        }

        private static bool DeleteFolder(string dir)
        {
            try
            {
                Directory.Delete(dir, true);

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog(1, string.Format("{0}{1} {2}", Utilities.TimeStamp(), dir, "DeleteFolder(): Runtime catched error " + ex.Message));
                return false;
            }
        }

        private static bool MoveFolder(string dir1, string dir2)
        {
            try
            {
                if (!Directory.Exists(dir2)) Directory.Move(dir1, dir2);
                else
                {
                    WriteToLog(1, string.Format("{0}{1} already exists", Utilities.TimeStamp(), dir2));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog(1, string.Format("{0}Failed to move {1} to {2}. Runtime error catched {3}", Utilities.TimeStamp(), dir1, dir2, ex.Message));
                return false;
            }
        }

        public static string RemoveSpecialCharacters(string str)
        {
            //return System.Text.RegularExpressions.Regex.Replace(str, "[^a-zA-Z0-9()-@_.|/ ]+", "", System.Text.RegularExpressions.RegexOptions.Compiled);
            //return System.Text.RegularExpressions.Regex.Replace(str, "[^a-zA-Z0-9'#-()&+ ]+", "", System.Text.RegularExpressions.RegexOptions.Compiled).Replace("Ñ","N").Replace("ñ", "n").Replace("\"", "");
            return str.Replace("Ñ", "N").Replace("ñ", "n").Replace("#", " ").Replace("-", " ").Replace("(", " ").Replace(")", " ").Replace("&", " ").Replace("'", " ").Replace("\"", " ").Replace("+", " ").Replace("�", "N");
            // + Ñ ñ 
            //#-()&"'+Ññ 
        }

        private static void CreatePreReqFolders(string workingFolder)
        {
            if (!Directory.Exists(string.Format(@"{0}\DONE", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\DONE", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\FOR_TRANSFER_ZIP", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\FOR_TRANSFER_ZIP", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO_FC", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO_FC", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\FOR_TRANSFER_MEM", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\FOR_TRANSFER_MEM", workingFolder));
            if (!Directory.Exists(string.Format(@"{0}\EXCEPTIONS", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\EXCEPTIONS", workingFolder));

            if (!workingFolder.Contains("RECARD")) if (!Directory.Exists(string.Format(@"{0}\RECARD", workingFolder))) Directory.CreateDirectory(string.Format(@"{0}\RECARD", workingFolder));
        }

        public static void SegregateMemFileAndFolders(string workingFolder)
        {
            string _FOR_PAGIBIGMEMCONSO = string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder);
            string _FOR_ZIP_PROCESS = string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder);

            foreach (string subDir in Directory.GetDirectories(workingFolder))
            {
                string acctNo = subDir.Substring(subDir.LastIndexOf("\\") + 1);
                switch (acctNo)
                {
                    case "archives":
                    case "DONE":
                    case "EXCEPTIONS":
                    case "RECARD":
                    case "FOR_TRANSFER_MEM":
                    case "FOR_TRANSFER_ZIP":
                    case "FOR_ZIP_PROCESS":
                    case "FOR_PAGIBIGMEMCONSO":
                    case "FOR_PAGIBIGMEMCONSO_FC":
                        break;
                    default:

                        if (Directory.GetFiles(subDir).Length == 12)
                        {
                            string sourceFile = string.Format(@"{0}\{1}.txt", subDir, acctNo);
                            string sourceFileDate = DateTime.Now.ToString("yyyy-MM-dd");    //new DirectoryInfo(subDir).CreationTime.ToString("yyyy-MM-dd");
                            string destiFile = string.Format(@"{0}\{1}\{2}.txt", _FOR_PAGIBIGMEMCONSO, sourceFileDate, acctNo);
                            string destiFileDONE = string.Format(@"{0}\DONE\{1}\{2}.txt", workingFolder, sourceFileDate, acctNo);
                            string destiFolderDONE = string.Format(@"{0}\DONE\{1}\{2}", workingFolder, sourceFileDate, acctNo);
                            string destiFolderMEM = string.Format(@"{0}\{1}", _FOR_PAGIBIGMEMCONSO, sourceFileDate);
                            string destiFolderZIP = string.Format(@"{0}\{1}", _FOR_ZIP_PROCESS, sourceFileDate);

                            string destiFolderZIP_acctNo = string.Format(@"{0}\{1}", destiFolderZIP, acctNo);

                            try
                            {
                                if (File.Exists(sourceFile))
                                {
                                    if (!Directory.Exists(destiFolderMEM)) Directory.CreateDirectory(destiFolderMEM);
                                    if (!Directory.Exists(destiFolderZIP)) Directory.CreateDirectory(destiFolderZIP);

                                    string strLine = File.ReadAllText(sourceFile);

                                    if (!File.Exists(destiFileDONE))
                                    {
                                        //check if file exist in FOR_PAGIBIGMEMCONSO
                                        if (!File.Exists(destiFile))
                                        {try
                                            {
                                                File.Move(sourceFile, destiFile);
                                                if (!dal.AddSFTP("", GetPagIBIGID(strLine), strLine.Split('|')[0], "TXT"))
                                                    WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(destiFile), " failed to insert txt in sftp table. Error " + dal.ErrorMessage));                                                
                                            }
                                            catch { }

                                            try
                                            {
                                                if (MoveFolder(subDir, destiFolderZIP_acctNo))
                                                {
                                                    DirectoryCopy(destiFolderZIP_acctNo, destiFolderDONE, true);
                                                    if (!dal.AddSFTP("", GetPagIBIGID(strLine), strLine.Split('|')[0], "ZIP"))
                                                        WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(destiFile), " failed to insert zip in sftp table. Error " + dal.ErrorMessage));
                                                }
                                                else MoveFolderToExceptions(workingFolder, subDir);
                                            }
                                            catch { }
                                        }
                                        else
                                            if (!MoveFolder(subDir, destiFolderZIP_acctNo)) 
                                                MoveFolderToExceptions(workingFolder, subDir);
                                            else
                                                if (!dal.AddSFTP("", strLine.Split('|')[32], strLine.Split('|')[0], "ZIP"))
                                                    WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(destiFile), " failed to insert zip in sftp table. Error " + dal.ErrorMessage));
                                    }
                                    else
                                    {
                                        WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(destiFile), " exist in " + destiFileDONE));
                                        MoveFolderToExceptions(workingFolder, subDir);
                                    }
                                }
                                else
                                    if (!MoveFolder(subDir, destiFolderZIP_acctNo)) MoveFolderToExceptions(workingFolder, subDir);


                            }
                            catch (Exception ex)
                            {
                                WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(destiFile), ".  Error " + ex.Message));
                            }
                        }
                        else
                        {
                            WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), subDir, ".  Incomplete files"));
                        }

                        break;
                }
            }
        }

        public static void ZipFolders(string workingFolder)
        {
            string sourceFileDate = DateTime.Now.ToString("yyyyMMdd");    //new DirectoryInfo(subDir).CreationTime.ToString("yyyy-MM-dd");
            string _FOR_ZIP_PROCESS = string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder);
            string _FOR_TRANSFER_ZIP = string.Format(@"{0}\FOR_TRANSFER_ZIP", workingFolder);            
            string destiFolderZIP = string.Format(@"{0}", _FOR_TRANSFER_ZIP);          

            //with date if RECARD
            if (workingFolder.Contains("RECARD")) destiFolderZIP = string.Format(@"{0}\{1}", _FOR_TRANSFER_ZIP, sourceFileDate);

            if (!Directory.Exists(destiFolderZIP)) Directory.CreateDirectory(destiFolderZIP);

            System.Text.StringBuilder sbForDeletion = new System.Text.StringBuilder();

            foreach (string subDir in Directory.GetDirectories(_FOR_ZIP_PROCESS))
            {

                foreach (string subDir2 in Directory.GetDirectories(subDir))
                {
                    string zipFile = "";
                    string acctNo = subDir2.Substring(subDir2.LastIndexOf("\\") + 1);

                    if (!FileCompression.Compress(subDir2, string.Format(@"{0}\{1}", destiFolderZIP, acctNo), ref zipFile))
                    {
                        WriteToLog(1, Utilities.TimeStamp() + "Failed compressing " + acctNo);
                    }
                    else
                    {                        
                        WriteToLog(0, Utilities.TimeStamp() + "Success compressing " + acctNo);
                        sbForDeletion.AppendLine(subDir2);
                        if (!dal.UpdateSFTPZipProcessDate("", "", acctNo))
                            WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(zipFile), " failed to update ZipProcessDate in sftp table. Error " + dal.ErrorMessage));
                    }
                }                
            }

            foreach (string subDir in sbForDeletion.ToString().Split('\r'))
            {
                if(subDir.Replace("\n", "") != "")Directory.Delete(subDir.Replace("\n", ""), true);
            }
        }

        private static void MoveFolderToExceptions(string workingFolder, string dir)
        {
            string folderName = dir.Substring(dir.LastIndexOf("\\") + 1);
            Directory.Move(dir, string.Format(@"{0}\EXCEPTIONS\{1}_{2}", workingFolder, folderName, DateTime.Now.ToString("yyyyMMdd_hhmmss")));
        }

        private static bool GenerateCancelledMemFile()
        {
            DAL dal = new DAL();
            DAL dalCentral = new DAL(true);
            if (dalCentral.SelectCancelledLoanDeductionByDate(DateTime.Now.ToShortDateString()))
            {

                foreach(DataRow rw in dalCentral.TableResult.Rows)
                {
                    bank_ws.ACC_MS_WEBSERVICE ws = new bank_ws.ACC_MS_WEBSERVICE();
                    string mid = rw["pagibigid"].ToString();
                    if (dal.GetGUIDByMID(mid))
                    {
                        if (dal.ObjectResult != null)
                        {
                            try
                            {
                                var response = ws.GenerateCancelledMemFile(rw["refnum"].ToString(), Utilities.DecryptData(dal.ObjectResult.ToString()));
                                if (!response.IsSuccess)
                                {
                                    WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "GenerateCancelledMemFile(): MID " + mid + "Error " + response.ErrorMessage));
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "GenerateCancelledMemFile(): MID " + mid + "Error " + ex.Message));
                            }
                        }
                        else
                            WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "GetGUIDByMID(): No record found in sftp table for MID " + mid));
                    }
                    else
                        WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "GetGUIDByMID(): MID " + mid + ". Error " + dal.ErrorMessage));
                }

                return true;
            }
            else
            {
                return false;
            }
            dalCentral.Dispose();
            dalCentral = null;
            dal.Dispose();
            dal = null;

            return true;
        }

        private static void ConsolidateMemFile(string workingFolder, string pagibigMemConsoFolder, short isEncrypt)
        {
            string _fileFolder = "";
            string _workingFolder = string.Format(@"{0}\{1}", workingFolder, pagibigMemConsoFolder);

            foreach (string subDir in Directory.GetDirectories(_workingFolder))
            {
                System.Text.StringBuilder sbReport = new System.Text.StringBuilder();
                string fileTempPAGIBIGMEMU = string.Format("{0}\\TempPAGIBIGMEMU.txt", _workingFolder);

                foreach (string file in Directory.GetFiles(subDir))
                {
                    if (!Path.GetFileName(file).Contains("PAGIBIGMEM"))
                    {
                        try
                        {
                            string fcFile = "";
                            if (pagibigMemConsoFolder.Contains("_FC")) fcFile = "_FC";
                            string destiFile = string.Format(@"{0}\DONE\{1}\{2}{3}.txt", workingFolder, DateTime.Now.ToString("yyyy-MM-dd"), Path.GetFileNameWithoutExtension(file), fcFile);

                            if (!File.Exists(destiFile))
                            {
                                string fileData = File.ReadAllText(file).Replace("0RCKAGALINGAN", "0RKAGALINGAN");

                                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                                for (int i = 0; i <= (fileData.Split('|').Length - 1); i++)
                                {
                                    switch (i)
                                    {
                                        case 0:
                                            sb.Append(fileData.Split('|')[i].Trim());
                                            break;
                                        case 1:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 2:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 3:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 12:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 13:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 17:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        case 25:
                                            sb.Append("|" + RemoveSpecialCharacters(fileData.Split('|')[i]).Trim());
                                            break;
                                        default:
                                            sb.Append("|" + fileData.Split('|')[i].Trim());
                                            break;
                                    }
                                }

                                fileData = sb.ToString();

                                using (StreamWriter sw = new StreamWriter(fileTempPAGIBIGMEMU, true))
                                {
                                    sw.WriteLine(fileData);
                                    sw.Dispose();
                                    sw.Close();
                                }

                                sbReport.AppendLine(fileData);

                                _fileFolder = string.Format(@"{0}\DONE\{1}", workingFolder, DateTime.Now.ToString("yyyy-MM-dd"));
                                if (!Directory.Exists(_fileFolder)) Directory.CreateDirectory(_fileFolder);


                                File.Move(file, destiFile);
                                WriteToLog(0, string.Format("{0}{1} {2}", Utilities.TimeStamp(), DateTime.Now.ToString("yyyy-MM-dd"), Path.GetFileName(file)) + " is moved to done");
                            }
                            else
                            {
                            }

                        }
                        catch (Exception ex)
                        {
                            WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "ConsolidateMemFile(): Error " + ex.Message));
                        }
                    }
                }

                if (sbReport.ToString() != "")
                {
                    if (!workingFolder.Contains("RECARD"))
                    {
                        if (!pagibigMemConsoFolder.Contains("_FC"))
                        {
                            if (Properties.Settings.Default.CONSO_CNTR == -1)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_CNTR = 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE == DateTime.Now.Date)
                            {
                                Properties.Settings.Default.CONSO_CNTR = Properties.Settings.Default.CONSO_CNTR + 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE != DateTime.Now.Date)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_CNTR = 1;
                                Properties.Settings.Default.CONSO_RECARD_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = 1;
                            }
                        }
                        else
                        {
                            if (Properties.Settings.Default.CONSOFC_CNTR == -1)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSOFC_CNTR = 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE == DateTime.Now.Date)
                            {
                                Properties.Settings.Default.CONSOFC_CNTR = Properties.Settings.Default.CONSOFC_CNTR + 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE != DateTime.Now.Date)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_CNTR = 1;
                                Properties.Settings.Default.CONSO_RECARD_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = 1;
                            }
                        }
                    }
                    else
                    {
                        if (!pagibigMemConsoFolder.Contains("_FC"))
                        {
                            if (Properties.Settings.Default.CONSO_RECARD_CNTR == -1)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_RECARD_CNTR = 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE == DateTime.Now.Date)
                            {
                                Properties.Settings.Default.CONSO_RECARD_CNTR = Properties.Settings.Default.CONSO_RECARD_CNTR + 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE != DateTime.Now.Date)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_CNTR = 1;
                                Properties.Settings.Default.CONSO_RECARD_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = 1;
                            }

                        }
                        else
                        {
                            if (Properties.Settings.Default.CONSOFC_RECARD_CNTR == -1)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE == DateTime.Now.Date)
                            {
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = Properties.Settings.Default.CONSOFC_RECARD_CNTR + 1;
                            }
                            else if (Properties.Settings.Default.SYS_DATE != DateTime.Now.Date)
                            {
                                Properties.Settings.Default.SYS_DATE = DateTime.Now.Date;
                                Properties.Settings.Default.CONSO_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_CNTR = 1;
                                Properties.Settings.Default.CONSO_RECARD_CNTR = 1;
                                Properties.Settings.Default.CONSOFC_RECARD_CNTR = 1;
                            }
                        }
                    }

                    Properties.Settings.Default.Save();
                    Properties.Settings.Default.Reload();

                    _fileFolder = string.Format(@"{0}\{1}", string.Format(@"{0}\FOR_TRANSFER_MEM", workingFolder), DateTime.Now.ToString("yyyyMMdd"));
                    if (!Directory.Exists(_fileFolder)) Directory.CreateDirectory(_fileFolder);

                    string forCancellation_FileNamePadder = "";
                    if (pagibigMemConsoFolder.Contains("_FC")) forCancellation_FileNamePadder = "_FOR CANCELLATION";

                    int fileCntr = 0;
                    if (!workingFolder.Contains("RECARD"))
                    {
                        if (!pagibigMemConsoFolder.Contains("_FC")) fileCntr = Properties.Settings.Default.CONSO_CNTR;
                        else fileCntr = Properties.Settings.Default.CONSOFC_CNTR;
                    }
                    else
                    {
                        if (!pagibigMemConsoFolder.Contains("_FC")) fileCntr = Properties.Settings.Default.CONSO_RECARD_CNTR;
                        else fileCntr = Properties.Settings.Default.CONSOFC_RECARD_CNTR;
                    }

                        string PAGIBIGMEMUF_FILE = "";
                    if (!workingFolder.Contains("RECARD")) PAGIBIGMEMUF_FILE = string.Format(@"{0}\{1}{2}.txt", _fileFolder, "PAGIBIGMEMUF" + DateTime.Now.ToString("MMddyy") + fileCntr.ToString().PadLeft(3, '0'), forCancellation_FileNamePadder);
                    else PAGIBIGMEMUF_FILE = string.Format(@"{0}\{1}{2}.txt", _fileFolder, "PAGIBIGMEMCR" + DateTime.Now.ToString("MMddyy") + fileCntr.ToString().PadLeft(3, '0'), forCancellation_FileNamePadder);

                    ////revision on 01/04/2020
                    //string PAGIBIGMEMUF_FILE = "";
                    //if (!workingFolder.Contains("RECARD")) PAGIBIGMEMUF_FILE = string.Format(@"{0}\{1}.txt", _fileFolder, "DUMP_PAGIBIGMEMUF" + DateTime.Now.ToString("MMddyy") + Properties.Settings.Default.CONSO_CNTR.ToString().PadLeft(3, '0'));
                    //else PAGIBIGMEMUF_FILE = string.Format(@"{0}\{1}.txt", _fileFolder, "DUMP_PAGIBIGMEMCR" + DateTime.Now.ToString("MMddyy") + Properties.Settings.Default.CONSO_RECARD_CNTR.ToString().PadLeft(3, '0'));

                    try
                    {
                        System.Text.StringBuilder sbTempPAGIBIGMEMU = new System.Text.StringBuilder();
                        System.Text.StringBuilder sbAcctNos = new System.Text.StringBuilder();
                        using (StreamReader srTempPAGIBIGMEMU = new StreamReader(fileTempPAGIBIGMEMU))
                        {
                            while (!srTempPAGIBIGMEMU.EndOfStream)
                            {
                                var line = srTempPAGIBIGMEMU.ReadLine();
                                if (line.ToString().Trim() != "")
                                {
                                    if (sbTempPAGIBIGMEMU.ToString() != "")
                                    {
                                        sbTempPAGIBIGMEMU.Append(Environment.NewLine);
                                        sbAcctNos.Append(",");
                                    }
                                    sbTempPAGIBIGMEMU.Append(line);
                                    sbAcctNos.Append(line.Split('|')[0]);
                                }
                            }
                            srTempPAGIBIGMEMU.Dispose();
                            srTempPAGIBIGMEMU.Close();
                        }

                        Utilities.SaveToSystemLog(Utilities.TimeStamp() + Path.GetFileName(PAGIBIGMEMUF_FILE) + " - " + sbAcctNos.ToString());

                        _fileFolder = string.Format(@"{0}\DONE\{1}", workingFolder, DateTime.Now.ToString("yyyy-MM-dd"));                        

                        if (isEncrypt == 0)
                        {
                            File.WriteAllText(PAGIBIGMEMUF_FILE, sbTempPAGIBIGMEMU.ToString());
                            File.Copy(PAGIBIGMEMUF_FILE, string.Format(@"{0}\{1}_{2}.txt", _fileFolder, Path.GetFileNameWithoutExtension(PAGIBIGMEMUF_FILE),DateTime.Now.ToString("hhmmss")));
                        }
                        else
                        {
                            MemuFCR_EncDec.EncDec ed = new MemuFCR_EncDec.EncDec(Properties.Settings.Default.AESKey);
                            ed.InputData = sbTempPAGIBIGMEMU.ToString();
                            if (ed.EncryptData())
                            {
                                File.WriteAllText(string.Format(@"{0}\{1}_{2}.txt", _fileFolder, Path.GetFileNameWithoutExtension(PAGIBIGMEMUF_FILE), DateTime.Now.ToString("hhmmss")), sbTempPAGIBIGMEMU.ToString());
                                File.WriteAllText(PAGIBIGMEMUF_FILE, ed.OutputData);
                                WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), Path.GetFileName(PAGIBIGMEMUF_FILE) + " encryption success"));
                            }
                            else
                            {
                                File.WriteAllText(string.Format(@"{0}\{1}", subDir, Path.GetFileName(PAGIBIGMEMUF_FILE)), sbTempPAGIBIGMEMU.ToString());
                                WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), Path.GetFileName(PAGIBIGMEMUF_FILE) + " encryption failed. Error " + ed.ErrorMessage));
                            }
                            ed = null;
                        }

                        WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Updating sftp table for " + Path.GetFileName(PAGIBIGMEMUF_FILE) + "..."));
                        foreach (string line in sbTempPAGIBIGMEMU.ToString().Split('\r'))
                        {
                            string[] lineArr = line.Split('|');                          
                            if (!dal.UpdatePagIBIGMemConso(Path.GetFileName(PAGIBIGMEMUF_FILE), GetPagIBIGID(line).Replace("\n",""), lineArr[0].Replace("\n", "")))
                                WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(PAGIBIGMEMUF_FILE), " failed to update txt in sftp table. Error " + dal.ErrorMessage));
                        }

                        WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "Deleting fileTempPAGIBIGMEMU" + Path.GetFileName(PAGIBIGMEMUF_FILE) + "..."));
                        DeleteFile(fileTempPAGIBIGMEMU);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(Utilities.TimeStamp() + "Failed to finalize fileTempPAGIBIGMEMU. Error " + ex.Message);
                        Utilities.SaveToErrorLog(Utilities.TimeStamp() + "Failed to finalize fileTempPAGIBIGMEMU. Error " + ex.Message);
                    }
                }
            }
        }

        private static string GetPagIBIGID(string line)
        {
            string[] lineArr = line.Split('|');
            string pagibigID = lineArr[32];
            if (pagibigID.Trim().Length != 12) pagibigID = lineArr[31];
            else
            {
                try
                {
                    int int_MID = Convert.ToInt32(pagibigID.Trim().Substring(0, 4));

                    switch (int_MID)
                    {
                        case 8410:
                            pagibigID = lineArr[31];
                            break;
                        default:
                            break;
                    }
                }
                catch { pagibigID = lineArr[31]; }
            }

            return pagibigID.Trim();
        }

        private static void SynchonizeFolder(string workingFolder, string memType, bool isZip)
        {
            string errMsg = "";
            SFTP sftp = new SFTP();
            if (!sftp.SynchronizeDirectories(memType, isZip, ref errMsg)) WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), "SynchronizeDirectories failed. Error " + errMsg));
            else WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), "SynchronizeDirectories is success"));
            //WriteToLog(0, string.Format("{0}{1}Success: {2}   Failed: {3}", Utilities.TimeStamp(), sftp.);
            sftp = null;          
        }

        private static void ReadSFTPDirectory()
        {
            string errMsg = "";
            SFTP sftp = new SFTP();
            string dir1 = "/upload/pagibig/MemberFiles/DataCaptureFiles";            
            string dir2 = "/upload/pagibig/MemberFiles/DataCaptureFiles/ForMigration";
            string dir3 = "/upload/pagibig/MemberFiles/DataCaptureFiles/DONE";

            if (!sftp.ReadSFTPDirectory(dir1, ref errMsg)) WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), dir1 + " failed. Error " + errMsg));
            else WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), dir1 + " is done"));

            if (!sftp.ReadSFTPDirectory(dir2, ref errMsg)) WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), dir2 + " failed. Error " + errMsg));
            else WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), dir2 + " is done"));

            if (!sftp.ReadSFTPDirectory(dir3, ref errMsg)) WriteToLog(1, string.Format("{0}{1}", Utilities.TimeStamp(), dir3 + " failed. Error " + errMsg));
            else WriteToLog(0, string.Format("{0}{1}", Utilities.TimeStamp(), dir3 + " is done"));
            //WriteToLog(0, string.Format("{0}{1}Success: {2}   Failed: {3}", Utilities.TimeStamp(), sftp.);
            sftp = null;
        }

        private static void HouseKeeping(string workingFolder)
        {
            //housekeeping            
            WriteToLog(0, string.Format("{0}Housekeeping...", Utilities.TimeStamp()));
            string forTransferFolder = string.Format(@"{0}\FOR_TRANSFER_ZIP", workingFolder);
            if (Utilities.APP_NAME.Contains("TXT")) forTransferFolder = string.Format(@"{0}\FOR_TRANSFER_MEM", workingFolder);

            foreach (string subDir in Directory.GetDirectories(forTransferFolder))
            {
                //delete empty folder
                if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir);

                //if (APP_NAME.Contains("TXT")) { if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir); }
                //else { if (Directory.GetDirectories(subDir).Length == 0) DeleteFolder(subDir); }
            }

            foreach (string subDir in Directory.GetDirectories(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder)))
            {
                //delete empty folder
                if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir);
            }

            if (Utilities.APP_NAME.Contains("ZIP"))
            {
                foreach (string subDir in Directory.GetDirectories(string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder)))
                {
                    //delete empty folder
                    if (Directory.GetDirectories(subDir).Length == 0) DeleteFolder(subDir);

                    //if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir);
                }
            }
        }

        private static void CombineFiles()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string subDir in Directory.GetDirectories(@"F:\PAGIBIG\Temp"))
            {
                string folderName = subDir.Substring(subDir.LastIndexOf("\\") + 1);

                foreach (string _file in Directory.GetFiles(subDir))
                {                    
                    using (StreamReader sr = new StreamReader(_file))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            string logLine = "";
                            if (line.Trim() != "")
                            {
                                logLine = string.Format("{0}|{1}|{2}", line.Replace("\n", ""), Path.GetFileName(_file), folderName);
                                sb.Append(logLine + Environment.NewLine);
                                Console.WriteLine(logLine);
                            }
                        }
                        sr.Dispose();
                        sr.Close();
                    }
                }
            }

            File.WriteAllText(@"F:\PAGIBIG\Temp\conso.txt", sb.ToString());
        }

        private static void UploadSFTPBacklog(string dirPath)
        {
            
            foreach (string _file in Directory.GetFiles(dirPath))
            {
                if (Path.GetExtension(_file).ToUpper() == ".TXT")
                {
                    int totalRecord = File.ReadAllLines(_file).Length;
                    int record = 1;

                    WriteToLog(0, Utilities.TimeStamp() + Path.GetFileName(_file));

                    using (StreamReader sr = new StreamReader(_file))
                    {                        
                        while(!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (line.ToString() != "")
                            {
                                try
                                {
                                    string pagIBIGID = GetPagIBIGID(line);
                                    string guid = line.Split('|')[0];
                                    DateTime dtm = new FileInfo(_file).LastWriteTime;

                                    //if (dtm.Year != 2020)
                                    //{
                                        if (!dal.AddSFTPv2("", pagIBIGID, guid, "TXT", Path.GetFileName(_file), dtm))
                                            WriteToLog(1, string.Format("{0}{1}{2}", Utilities.TimeStamp(), Path.GetFileName(_file), " failed to insert txt in sftp table. Error " + dal.ErrorMessage));
                                        else
                                        {
                                            Console.WriteLine(Utilities.TimeStamp() + Path.GetFileName(_file) + ", " + dtm.ToString() + ", " + record.ToString("N0") + " of " + totalRecord.ToString("N0"));
                                        }
                                    //}
                                }
                                catch (Exception ex)
                                {
                                    WriteToLog(1, Utilities.TimeStamp() + "UploadSFTPBacklog(): " + Path.GetFileName(_file) + ", " + line.Substring(0, 30) + ", " + ex.Message);                                    
                                }

                                record += 1;
                            }
                        }
                    }
                }
            }
        }


        #region Helpers

        private static int IsProgramRunning(string Program)
        {
            System.Diagnostics.Process[] p;
            p = System.Diagnostics.Process.GetProcessesByName(Program.Replace(".exe", "").Replace(".EXE", ""));

            return p.Length;
        }

        private static void WriteToLog(short type, string desc)
        {
            Console.WriteLine(desc);
            if (type == 0) Utilities.SaveToSystemLog(string.Format("[{0}] {1}", Utilities.APP_NAME,desc));
            if (type == 1) Utilities.SaveToErrorLog(string.Format("[{0}] {1}", Utilities.APP_NAME, desc));
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }


        #endregion
    }
}

