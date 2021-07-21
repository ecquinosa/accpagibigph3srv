using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace accpagibigph3srv
{
    class Utilities
    {
        public static string APP_NAME = "";

        private static string encryptionKey = "@cCP@g1bIgPH3*";

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

        public static void InitLogFolder()
        {
            if (!System.IO.Directory.Exists("Logs"))
                System.IO.Directory.CreateDirectory("Logs");
            if (!System.IO.Directory.Exists(@"Logs\" + DateTime.Now.ToString("MMddyyyy")))
                System.IO.Directory.CreateDirectory(@"Logs\" + DateTime.Now.ToString("MMddyyyy"));
        }

        public static void SaveToErrorLog(string strData)
        {
            InitLogFolder();
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"Logs\" + DateTime.Now.ToString("MMddyyyy") + @"\Error.txt", true);
                sw.WriteLine(strData);
                sw.Dispose();
                sw.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static void SaveToSystemLog(string strData)
        {
            InitLogFolder();
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"Logs\" + DateTime.Now.ToString("MMddyyyy") + @"\System.txt", true);
                sw.WriteLine(strData);
                sw.Dispose();
                sw.Close();
            }
            catch (Exception ex)
            {
            }
        }


    }

}
