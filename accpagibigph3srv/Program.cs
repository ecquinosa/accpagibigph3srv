
using System;
using System.Data;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using accpagibigph3srv.Class;

namespace accpagibigph3srv
{
    class Program
    {

        #region Constructors       

        private static string FileCntr = "FileCntr";
        private static string ProcessType = "";

        private delegate void dlgtProcess();

        private static System.Threading.Thread _thread;

        private static string configFile = AppDomain.CurrentDomain.BaseDirectory + "config";

        public static Config config;
        public static DAL dal = null;

        #endregion       

        static void Main()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            Utilities.APP_NAME = Path.GetFileName(codeBase);
            Utilities.SystemDate = DateTime.Now;
            ////temp
            //Utilities.APP_NAME = Utilities.APP_NAME.Replace(".exe", "TXT.exe");
            //Utilities.APP_NAME = Utilities.APP_NAME.Replace(".exe","ZIP.exe");

            LogToSystemLog(string.Format("Application started [{0}]", Utilities.APP_NAME));

            if (Utilities.IsProgramRunning(Utilities.APP_NAME) > 1) return;

            while (!Initv2()) System.Threading.Thread.Sleep(5000);

            System.Threading.Thread.Sleep(5000);

            dlgtProcess _delegate = new dlgtProcess(RunProcess);
            _delegate.Invoke();
            _delegate = null;
        }      

        private static bool Initv2()
        {
            try
            {
                LogToSystemLog("Checking config...");
                if (!File.Exists(configFile))
                {
                    LogToErrorLog("Init(): Config file is missing");
                    //sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Config file is missing");
                    return false;
                }

                try
                {
                    config = new Config();
                    var configData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(configFile));
                    config = configData[0];

                    Utilities.ConStr = config.DbaseConStr;
                    dal = new DAL();

                    //check dbase connection
                    if (!dal.IsConnectionOK(Utilities.ConStr))
                    {
                        LogToErrorLog("Init(): Connection to database failed. " + dal.ErrorMessage);
                        //sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Connection to database failed. " + dal.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogToErrorLog("Init(): Error reading config file. Runtime catched error " + ex.Message);
                    //sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Error reading config file. Runtime catched error " + ex.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToErrorLog("Init(): Runtime catched error " + ex.Message);
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
                LogToErrorLog("ProgramThread(): Runtime catched error " + ex.Message);
            }
        }

        private static void RunProcess()
        {
            LogToSystemLog("Creating pre-req folders...");
            Utilities.CreatePreReqFolders(config.BankRepo);
            Utilities.CreatePreReqFolders(string.Format(@"{0}\RECARD", config.BankRepo));

            string UBP_REPO_RECARD = string.Format(@"{0}\RECARD", config.BankRepo);

            if (Utilities.APP_NAME.Contains("TXT"))
            {
                LogToSystemLog("Segregation of textfiles and folders...");
                SegregateMemFileAndFolders(config.BankRepo);
                SegregateMemFileAndFolders(UBP_REPO_RECARD);

                //DateTime cancelledMemFileTime_From = Convert.ToDateTime(string.Format("{0} {1}", DateTime.Now.Date.ToShortDateString(), config.GenerateCancelledMemFileFrom));
                //DateTime cancelledMemFileTime_To = Convert.ToDateTime(string.Format("{0} {1}", DateTime.Now.Date.ToShortDateString(), config.GenerateCancelledMemFileTo));

                //if (DateTime.Now >= cancelledMemFileTime_From & DateTime.Now <= cancelledMemFileTime_To)
                //{
                //    LogToSystemLog("Consolidation of fc textfiles...");
                //    if (GenerateCancelledMemFile())
                //    {
                //        ConsolidateMemFile(config.BankRepo, "FOR_PAGIBIGMEMCONSO_FC", config.EncryptPagibigMemUF);
                //        ConsolidateMemFile(UBP_REPO_RECARD, "FOR_PAGIBIGMEMCONSO_FC", config.EncryptPagibigMemCR);
                //    }
                //}

                LogToSystemLog("Consolidation of textfiles...");
                ConsolidateMemFile(config.BankRepo, "FOR_PAGIBIGMEMCONSO", config.EncryptPagibigMemUF);
                ConsolidateMemFile(UBP_REPO_RECARD, "FOR_PAGIBIGMEMCONSO", config.EncryptPagibigMemCR);

                if (config.SendToSftp == 1)
                {
                    LogToSystemLog("Synchronizing local folder and sftp folder [" + Utilities.APP_NAME + "]...");
                    SynchonizeFolder(config.BankRepo, "UF", false);
                    SynchonizeFolder(UBP_REPO_RECARD, "CR", false);
                }

            }

            if (Utilities.APP_NAME.Contains("ZIP"))
            {
                LogToSystemLog("Compressing folder(s)...");
                ZipFolders(config.BankRepo);
                ZipFolders(UBP_REPO_RECARD);

                if (config.SendToSftp == 1)
                {
                    LogToSystemLog("Synchronizing local folder and sftp folder [" + Utilities.APP_NAME + "]...");
                    SynchonizeFolder(config.BankRepo, "UF", true);
                    SynchonizeFolder(UBP_REPO_RECARD, "CR", true);
                }
            }

            HouseKeeping(config.BankRepo);
            HouseKeeping(UBP_REPO_RECARD);

            LogToSystemLog(string.Format("Application close [{0}]", Utilities.APP_NAME));
        }     

        public static string RemoveSpecialCharacters(string str)
        {
            //return System.Text.RegularExpressions.Regex.Replace(str, "[^a-zA-Z0-9()-@_.|/ ]+", "", System.Text.RegularExpressions.RegexOptions.Compiled);
            //return System.Text.RegularExpressions.Regex.Replace(str, "[^a-zA-Z0-9'#-()&+ ]+", "", System.Text.RegularExpressions.RegexOptions.Compiled).Replace("Ñ","N").Replace("ñ", "n").Replace("\"", "");
            return str.Replace("Ñ", "N").Replace("ñ", "n").Replace("#", " ").Replace("-", " ").Replace("(", " ").Replace(")", " ").Replace("&", " ").Replace("'", " ").Replace("\"", " ").Replace("+", " ").Replace("�", "N");
            // + Ñ ñ 
            //#-()&"'+Ññ 
        }     
        

        public static void SegregateMemFileAndFolders(string workingFolder)
        {
            string _FOR_PAGIBIGMEMCONSO = string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder);
            string _FOR_ZIP_PROCESS = string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder);

            foreach (string subDir in Directory.GetDirectories(workingFolder))
            {
                string acctNo = subDir.Substring(subDir.LastIndexOf("\\") + 1);

                if (!Utilities.IsFolderReserve(acctNo))
                {
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
                                    {
                                        try
                                        {
                                            File.Move(sourceFile, destiFile);
                                            if (!dal.AddSFTP("", GetPagIBIGID(strLine), strLine.Split('|')[0], "TXT"))
                                                LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(destiFile), " failed to insert txt in sftp table. Error " + dal.ErrorMessage));
                                        }
                                        catch { }

                                        try
                                        {
                                            if (Utilities.MoveFolder(subDir, destiFolderZIP_acctNo))
                                            {
                                                DirectoryCopy(destiFolderZIP_acctNo, destiFolderDONE, true);
                                                if (!dal.AddSFTP("", GetPagIBIGID(strLine), strLine.Split('|')[0], "ZIP"))
                                                    LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(destiFile), " failed to insert zip in sftp table. Error " + dal.ErrorMessage));
                                            }
                                            else Utilities.MoveFolderToExceptions(workingFolder, subDir);
                                        }
                                        catch { }
                                    }
                                    else
                                        if (!Utilities.MoveFolder(subDir, destiFolderZIP_acctNo))
                                        Utilities.MoveFolderToExceptions(workingFolder, subDir);
                                    else
                                            if (!dal.AddSFTP("", strLine.Split('|')[32], strLine.Split('|')[0], "ZIP"))
                                        LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(destiFile), " failed to insert zip in sftp table. Error " + dal.ErrorMessage));
                                }
                                else
                                {
                                    LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(destiFile), " exist in " + destiFileDONE));
                                    Utilities.MoveFolderToExceptions(workingFolder, subDir);
                                }
                            }
                            else
                                if (!Utilities.MoveFolder(subDir, destiFolderZIP_acctNo)) Utilities.MoveFolderToExceptions(workingFolder, subDir);


                        }
                        catch (Exception ex)
                        {
                            LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(destiFile), ".  Error " + ex.Message));
                        }
                    }
                    else
                    {
                        LogToErrorLog(string.Format("{0}{1}", subDir, ".  Incomplete files"));
                    }
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
            //if (workingFolder.Contains("RECARD")) destiFolderZIP = string.Format(@"{0}\{1}", _FOR_TRANSFER_ZIP, sourceFileDate);
            destiFolderZIP = string.Format(@"{0}\{1}", _FOR_TRANSFER_ZIP, sourceFileDate);

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
                        LogToErrorLog("Failed compressing " + acctNo);
                    }
                    else
                    {
                        LogToSystemLog("Success compressing " + acctNo);
                        sbForDeletion.AppendLine(subDir2);
                        if (!dal.UpdateSFTPZipProcessDate("", "", acctNo))
                            LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(zipFile), " failed to update ZipProcessDate in sftp table. Error " + dal.ErrorMessage));
                    }
                }
            }

            foreach (string subDir in sbForDeletion.ToString().Split('\r'))
            {
                if (subDir.Replace("\n", "") != "") Directory.Delete(subDir.Replace("\n", ""), true);
            }
        }        

        //private static bool GenerateCancelledMemFile()
        //{
        //    DAL dal = new DAL();
        //    DAL dalCentral = new DAL(true);
        //    if (dalCentral.SelectCancelledLoanDeductionByDate(DateTime.Now.ToShortDateString()))
        //    {

        //        foreach (DataRow rw in dalCentral.TableResult.Rows)
        //        {
        //            bank_ws.ACC_MS_WEBSERVICE ws = new bank_ws.ACC_MS_WEBSERVICE();
        //            string mid = rw["pagibigid"].ToString();
        //            if (dal.GetGUIDByMID(mid))
        //            {
        //                if (dal.ObjectResult != null)
        //                {
        //                    try
        //                    {
        //                        var response = ws.GenerateCancelledMemFile(rw["refnum"].ToString(), Utilities.DecryptData(dal.ObjectResult.ToString()));
        //                        if (!response.IsSuccess)
        //                        {
        //                            LogToErrorLog(string.Format("{0}", "GenerateCancelledMemFile(): MID " + mid + "Error " + response.ErrorMessage));
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        LogToErrorLog(string.Format("{0}", "GenerateCancelledMemFile(): MID " + mid + "Error " + ex.Message));
        //                    }
        //                }
        //                else

        //                    LogToErrorLog(string.Format("{0}", "GetGUIDByMID(): No record found in sftp table for MID " + mid));
        //            }
        //            else
        //                LogToErrorLog(string.Format("{0}", "GetGUIDByMID(): MID " + mid + ". Error " + dal.ErrorMessage));
        //        }

        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //    dalCentral.Dispose();
        //    dalCentral = null;
        //    //dal.Dispose();
        //    //dal = null;

        //    return true;
        //}

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

                                LogToSystemLog(string.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd"), Path.GetFileName(file)) + " is moved to done");
                            }
                            else
                            {
                            }

                        }
                        catch (Exception ex)
                        {
                            LogToErrorLog(string.Format("{0}", "ConsolidateMemFile(): Error " + ex.Message));
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

                        LogToSystemLog(Path.GetFileName(PAGIBIGMEMUF_FILE) + " - " + sbAcctNos.ToString());

                        _fileFolder = string.Format(@"{0}\DONE\{1}", workingFolder, DateTime.Now.ToString("yyyy-MM-dd"));

                        if (isEncrypt == 0)
                        {
                            File.WriteAllText(PAGIBIGMEMUF_FILE, sbTempPAGIBIGMEMU.ToString());
                            File.Copy(PAGIBIGMEMUF_FILE, string.Format(@"{0}\{1}_{2}.txt", _fileFolder, Path.GetFileNameWithoutExtension(PAGIBIGMEMUF_FILE), DateTime.Now.ToString("hhmmss")));
                        }
                        else
                        {
                            MemuFCR_EncDec.EncDec ed = new MemuFCR_EncDec.EncDec(Properties.Settings.Default.AESKey);
                            ed.InputData = sbTempPAGIBIGMEMU.ToString();
                            if (ed.EncryptData())
                            {
                                File.WriteAllText(string.Format(@"{0}\{1}_{2}.txt", _fileFolder, Path.GetFileNameWithoutExtension(PAGIBIGMEMUF_FILE), DateTime.Now.ToString("hhmmss")), sbTempPAGIBIGMEMU.ToString());
                                File.WriteAllText(PAGIBIGMEMUF_FILE, ed.OutputData);
                                LogToSystemLog(string.Format("{0}", Path.GetFileName(PAGIBIGMEMUF_FILE) + " encryption success"));
                            }
                            else
                            {
                                File.WriteAllText(string.Format(@"{0}\{1}", subDir, Path.GetFileName(PAGIBIGMEMUF_FILE)), sbTempPAGIBIGMEMU.ToString());
                                LogToErrorLog(string.Format("{0}", Path.GetFileName(PAGIBIGMEMUF_FILE) + " encryption failed. Error " + ed.ErrorMessage));
                            }
                            ed = null;
                        }

                        LogToSystemLog(string.Format("{0}", "Updating sftp table for " + Path.GetFileName(PAGIBIGMEMUF_FILE) + "..."));
                        foreach (string line in sbTempPAGIBIGMEMU.ToString().Split('\r'))
                        {
                            string[] lineArr = line.Split('|');
                            if (!dal.UpdatePagIBIGMemConso(Path.GetFileName(PAGIBIGMEMUF_FILE), GetPagIBIGID(line).Replace("\n", ""), lineArr[0].Replace("\n", "")))
                                LogToErrorLog(string.Format("{0}{1}", Path.GetFileName(PAGIBIGMEMUF_FILE), " failed to update txt in sftp table. Error " + dal.ErrorMessage));
                        }

                        LogToSystemLog(string.Format("{0}", "Deleting fileTempPAGIBIGMEMU" + Path.GetFileName(PAGIBIGMEMUF_FILE) + "..."));
                        Utilities.DeleteFile(fileTempPAGIBIGMEMU);
                    }
                    catch (Exception ex)
                    {
                        LogToErrorLog("Failed to finalize fileTempPAGIBIGMEMU. Error " + ex.Message);
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

            if (!sftp.SynchronizeDirectories(memType, isZip, ref errMsg)) LogToErrorLog(string.Format("{0}", "SynchronizeDirectories failed. Error " + errMsg));
            else LogToSystemLog(string.Format("{0}", "SynchronizeDirectories is success"));
            sftp = null;
        }

        private static void ReadSFTPDirectory()
        {
            string errMsg = "";
            SFTP sftp = new SFTP();
            string dir1 = "/upload/pagibig/MemberFiles/DataCaptureFiles";
            string dir2 = "/upload/pagibig/MemberFiles/DataCaptureFiles/ForMigration";
            string dir3 = "/upload/pagibig/MemberFiles/DataCaptureFiles/DONE";

            if (!sftp.ReadSFTPDirectory(dir1, ref errMsg)) LogToErrorLog(string.Format("{0}", dir1 + " failed. Error " + errMsg));
            else LogToSystemLog(string.Format("{0}", dir1 + " is done"));

            if (!sftp.ReadSFTPDirectory(dir2, ref errMsg)) LogToErrorLog(string.Format("{0}", dir2 + " failed. Error " + errMsg));
            else LogToSystemLog(string.Format("{0}", dir2 + " is done"));

            if (!sftp.ReadSFTPDirectory(dir3, ref errMsg)) LogToErrorLog(string.Format("{0}", dir3 + " failed. Error " + errMsg));
            else LogToSystemLog(string.Format("{0}", dir3 + " is done"));

            sftp = null;
        }

        private static void HouseKeeping(string workingFolder)
        {
            //housekeeping
            LogToSystemLog("Housekeeping...");
            string forTransferFolder = string.Format(@"{0}\FOR_TRANSFER_ZIP", workingFolder);
            if (Utilities.APP_NAME.Contains("TXT")) forTransferFolder = string.Format(@"{0}\FOR_TRANSFER_MEM", workingFolder);

            foreach (string subDir in Directory.GetDirectories(forTransferFolder))
            {
                //delete empty folder
                if (Directory.GetFiles(subDir).Length == 0) Utilities.DeleteFolder(subDir);

                //if (APP_NAME.Contains("TXT")) { if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir); }
                //else { if (Directory.GetDirectories(subDir).Length == 0) DeleteFolder(subDir); }
            }

            foreach (string subDir in Directory.GetDirectories(string.Format(@"{0}\FOR_PAGIBIGMEMCONSO", workingFolder)))
            {
                //delete empty folder
                if (Directory.GetFiles(subDir).Length == 0) Utilities.DeleteFolder(subDir);
            }

            if (Utilities.APP_NAME.Contains("ZIP"))
            {
                foreach (string subDir in Directory.GetDirectories(string.Format(@"{0}\FOR_ZIP_PROCESS", workingFolder)))
                {
                    //delete empty folder
                    if (Directory.GetDirectories(subDir).Length == 0) Utilities.DeleteFolder(subDir);

                    //if (Directory.GetFiles(subDir).Length == 0) DeleteFolder(subDir);
                }
            }
        }      


        #region Helpers

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

        public static void LogToSystemLog(string logDesc)
        {
            Console.WriteLine(Utilities.TimeStamp() + logDesc);
            Log.SaveToSystemLog(Utilities.TimeStamp() + logDesc);
        }

        public static void LogToErrorLog(string logDesc)
        {
            Console.WriteLine(Utilities.TimeStamp() + logDesc);
            Log.SaveToErrorLog(Utilities.TimeStamp() + logDesc);
        }   

        #endregion

    }
}

