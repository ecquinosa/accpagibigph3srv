
using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;

namespace accpagibigph3srv
{
    class DAL : IDisposable

    {

        private string ConStr = Properties.Settings.Default.DbaseConStr;
        private DataTable dtResult;
        private DataSet dsResult;
        private object objResult;
        private IDataReader _readerResult;
        private string strErrorMessage;

        private SqlConnection con;
        private SqlCommand cmd;
        private SqlDataAdapter da;

        public string ErrorMessage
        {
            get { return strErrorMessage; }
        }

        public DataTable TableResult
        {
            get { return dtResult; }
        }

        public object ObjectResult
        {
            get { return objResult; }
        }

        public DAL()
        {
            
        }

        public DAL(bool IsCentralDB)
        {
            if (IsCentralDB) ConStr = Properties.Settings.Default.CentralDbaseConStr;
        }

        public void ClearAllPools()
        {
            SqlConnection.ClearAllPools();
        }

        private void OpenConnection()
        {
            if (con == null) con = new SqlConnection(ConStr);
        }

        private void CloseConnection()
        {
            if (cmd != null) cmd.Dispose();
            if (da != null) da.Dispose();
            if (_readerResult != null)
            {
                _readerResult.Close();
                _readerResult.Dispose();
            }
            if (con != null)
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            ClearAllPools();
        }

        private void ExecuteNonQuery(CommandType cmdType)
        {
            cmd.CommandType = cmdType;

            // If con.State = ConnectionState.Open Then con.Close()
            // con.Open()
            if (con.State == ConnectionState.Closed)
                con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        private void _ExecuteScalar(CommandType cmdType)
        {
            cmd.CommandType = cmdType;

            // If con.State = ConnectionState.Open Then con.Close()
            // con.Open()
            if (con.State == ConnectionState.Closed) con.Open();
            object _obj;
            _obj = cmd.ExecuteScalar();
            con.Close();

            objResult = _obj;
        }

        private void _ExecuteReader(CommandType cmdType)
        {
            cmd.CommandType = cmdType;

            // If con.State = ConnectionState.Open Then con.Close()
            // con.Open()
            if (con.State == ConnectionState.Closed)
                con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            _readerResult = reader;
        }

        private void FillDataAdapter(CommandType cmdType)
        {
            cmd.CommandTimeout = 0;
            cmd.CommandType = cmdType;
            da = new SqlDataAdapter(cmd);
            DataTable _dt = new DataTable();
            da.Fill(_dt);
            dtResult = _dt;
        }

        public bool IsConnectionOK(string strConString = "")
        {
            try
            {
                if (strConString != "")
                    ConStr = strConString;
                OpenConnection();

                con.Open();
                con.Close();

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool SelectQuery(string strQuery)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand(strQuery, con);

                FillDataAdapter(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool SelectCancelledLoanDeductionByDate(string dtmDate)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                OpenConnection();

                sb.AppendLine("SELECT ID, PagibigID, RefNum FROM dbo.tbl_DCS_LoanDeduction ");
                sb.AppendLine(string.Format("WHERE (BankID = 1) AND (Cancelled_Date BETWEEN '{0} 00:00:00' AND '{0} 23:59:59') AND (CancelledkycFile_Date IS NULL)", dtmDate));

                cmd = new SqlCommand(sb.ToString(), con);

                FillDataAdapter(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool ExecuteQuery(string strQuery)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand(strQuery, con);

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool ExecuteScalar(string strQuery)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand(strQuery, con);

                _ExecuteScalar(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGUIDByMID(string mid)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("SELECT GUID FROM tbl_SFTP WHERE PagIBIGID=@PagIBIGID", con);
                cmd.Parameters.AddWithValue("PagIBIGID", mid);

                _ExecuteScalar(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool AddSFTP(string refNum, string pagIBIGID, string GUID, string type)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("prcAddSFTP", con);
                cmd.Parameters.AddWithValue("RefNum", refNum);
                cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                cmd.Parameters.AddWithValue("Type", type);

                ExecuteNonQuery(CommandType.StoredProcedure);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool AddSFTPv2(string refNum, string pagIBIGID, string GUID, string type, string remark, DateTime dtm)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("prcAddSFTPv2", con);
                cmd.Parameters.AddWithValue("RefNum", refNum);
                cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                cmd.Parameters.AddWithValue("Type", type);
                cmd.Parameters.AddWithValue("PagIbigMemConsoDate", dtm);
                cmd.Parameters.AddWithValue("Remark", remark);
                cmd.Parameters.AddWithValue("SFTPTransferDate", dtm);

                ExecuteNonQuery(CommandType.StoredProcedure);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTPZipProcessDate(string refNum, string pagIBIGID, string GUID)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("prcUpdateSFTPZipProcessDate", con);
                //cmd.Parameters.AddWithValue("RefNum", refNum);
                //cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                
                ExecuteNonQuery(CommandType.StoredProcedure);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTP(string refNum, string pagIBIGID, string GUID, string type)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("prcAddSFTP", con);
                cmd.Parameters.AddWithValue("RefNum", refNum);
                cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                cmd.Parameters.AddWithValue("Type", type);

                ExecuteNonQuery(CommandType.StoredProcedure);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdatePagIBIGMemConso(string pagibiMemFileName, string pagIBIGID, string GUID)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("UPDATE tbl_SFTP SET PagIbigMemConsoDate=GETDATE(),Remark=@Remark WHERE PagIBIGID=@PagIBIGID AND GUID=@GUID AND PagIbigMemConsoDate IS NULL AND Type='TXT'", con);
                cmd.Parameters.AddWithValue("Remark", pagibiMemFileName);
                cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                
                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdatePagIBIGMemConsov2(string pagibiMemFileName, string pagIBIGID, string GUID, DateTime dtm)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("UPDATE tbl_SFTP SET PagIbigMemConsoDate=@PagIbigMemConsoDate,Remark=@Remark WHERE PagIBIGID=@PagIBIGID AND GUID=@GUID AND PagIbigMemConsoDate IS NULL AND Type='TXT'", con);
                cmd.Parameters.AddWithValue("PagIbigMemConsoDate", dtm);
                cmd.Parameters.AddWithValue("Remark", pagibiMemFileName);
                cmd.Parameters.AddWithValue("PagIBIGID", pagIBIGID);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTPTransferDate(string GUID, string type)
        {
            try
            {
                OpenConnection();
                if(type=="TXT")
                    cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=GETDATE() WHERE GUID=@GUID AND Type=@Type AND PagIbigMemConsoDate IS NOT NULL AND SFTPTransferDate IS NULL", con);
                else
                    cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=GETDATE() WHERE GUID=@GUID AND Type=@Type AND ZipProcessDate IS NOT NULL AND SFTPTransferDate IS NULL", con);

                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                cmd.Parameters.AddWithValue("Type", type);

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTPTransferDatev2(string GUID, string type, DateTime dtm)
        {
            try
            {
                OpenConnection();
                if (type == "TXT")
                    cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=@SFTPTransferDate WHERE GUID=@GUID AND Type=@Type AND PagIbigMemConsoDate IS NOT NULL AND SFTPTransferDate IS NULL", con);
                else
                    cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=@SFTPTransferDate WHERE GUID=@GUID AND Type=@Type AND ZipProcessDate IS NOT NULL AND SFTPTransferDate IS NULL", con);

                cmd.Parameters.AddWithValue("SFTPTransferDate", dtm);
                cmd.Parameters.AddWithValue("GUID", Utilities.EncryptData(GUID));
                cmd.Parameters.AddWithValue("Type", type);

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTPTransferDateByPagIBIGMemFileName(string pagibigMemFileName)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=GETDATE() WHERE Remark=@Remark AND Type='TXT' AND PagIbigMemConsoDate IS NOT NULL AND SFTPTransferDate IS NULL", con);
                
                cmd.Parameters.AddWithValue("Remark", pagibigMemFileName);

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UpdateSFTPTransferDateByPagIBIGMemFileNamev2(string pagibigMemFileName, DateTime dtm)
        {
            try
            {
                OpenConnection();
                cmd = new SqlCommand("UPDATE tbl_SFTP SET SFTPTransferDate=@SFTPTransferDate WHERE Remark=@Remark AND Type='TXT' AND PagIbigMemConsoDate IS NOT NULL AND SFTPTransferDate IS NULL", con);

                cmd.Parameters.AddWithValue("SFTPTransferDate", dtm);
                cmd.Parameters.AddWithValue("Remark", pagibigMemFileName);

                ExecuteNonQuery(CommandType.Text);

                return true;
            }
            catch (Exception ex)
            {
                strErrorMessage = ex.Message;
                return false;
            }
        }

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    CloseConnection();
                }



                // Note disposing has been done.
                disposed = true;

            }
        }

    }
}
