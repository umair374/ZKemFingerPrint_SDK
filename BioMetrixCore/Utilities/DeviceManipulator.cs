using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;


namespace BioMetrixCore
{
    internal class DeviceManipulator
    {

        public ICollection<UserInfo> GetAllUserInfo(ZkemClient objZkeeper, int machineNumber)
        {
            string sdwEnrollNumber = string.Empty, sName = string.Empty, sPassword = string.Empty, sTmpData = string.Empty;
            int iPrivilege = 0, iTmpLength = 0, iFlag = 0, idwFingerIndex;
            bool bEnabled = false;
            //int sdwEnrollNumber =0;

            ICollection<UserInfo> lstFPTemplates = new List<UserInfo>();

            objZkeeper.ReadAllUserID(machineNumber);
            objZkeeper.ReadAllTemplate(machineNumber);
            //DialogResult result = MessageBox.Show("Do you want to copy the fingerprint data to the database?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            DialogResult result = DialogResult.No;
            // GetAllUserInfo(int dwMachineNumber, ref int dwEnrollNumber, ref string Name, ref string Password, ref int Privilege, ref bool Enabled)
            while (objZkeeper.SSR_GetAllUserInfo(machineNumber, out sdwEnrollNumber, out sName, out sPassword, out iPrivilege, out bEnabled))
            //while (objZkeeper.GetAllUserInfo(machineNumber, ref sdwEnrollNumber, ref sName, ref sPassword, ref iPrivilege, ref bEnabled))
            {
                for (idwFingerIndex = 0; idwFingerIndex < 10; idwFingerIndex++)
                {
                    //idwFingerIndex = 6;
                    //string enrollNumberAsString = Convert.ToString(sdwEnrollNumber);
                    if (objZkeeper.GetUserTmpExStr(machineNumber, sdwEnrollNumber, idwFingerIndex, out iFlag, out sTmpData, out iTmpLength))
                    {
                        UserInfo fpInfo = new UserInfo();
                        fpInfo.MachineNumber = machineNumber;
                        fpInfo.EnrollNumber = sdwEnrollNumber;
                        fpInfo.Name = sName;
                        fpInfo.FingerIndex = idwFingerIndex;
                        fpInfo.TmpData = sTmpData;
                        fpInfo.Privelage = iPrivilege;
                        fpInfo.Password = sPassword;
                        fpInfo.Enabled = bEnabled;
                        fpInfo.iFlag = iFlag.ToString();

                        lstFPTemplates.Add(fpInfo);

                        

                        if (result == DialogResult.Yes)
                        {
                            using (OracleConnection conn = new OracleConnection("User Id=hr;Password=123456;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))"))
                            {
                                conn.Open();

                                using (OracleCommand cmd = new OracleCommand())
                                {
                                    cmd.Connection = conn;
                                    cmd.CommandText = @"
                                    INSERT INTO BiometricUserInfo 
                                    (MachineNumber,EnrollNumber, Name, FingerIndex, Privilege, Password, Enabled, iFlag,FINGERDATA)
                                    VALUES (:MachineNumber, :EnrollNumber, :Name, :FingerIndex, :Privilege, :Password,:Enabled,  :iFlag , :FINGERDATA)";
                                    
                                    cmd.Parameters.Add(new OracleParameter("MachineNumber", machineNumber));
                                    cmd.Parameters.Add(new OracleParameter("EnrollNumber", sdwEnrollNumber));
                                    cmd.Parameters.Add(new OracleParameter("Name", sName));
                                    cmd.Parameters.Add(new OracleParameter("FingerIndex", idwFingerIndex));
                                    //cmd.Parameters.Add(new OracleParameter("FINGERDATA", userInfo.TmpData)); // Save the fingerprint template
                                    cmd.Parameters.Add(new OracleParameter("Privilege", iPrivilege));
                                    cmd.Parameters.Add(new OracleParameter("Password", sPassword));
                                    cmd.Parameters.Add(new OracleParameter("Enabled", bEnabled ? 'Y' : 'N'));
                                    //cmd.Parameters.Add(new OracleParameter("Enabled", bEnabled));
                                    cmd.Parameters.Add(new OracleParameter("iFlag", iFlag.ToString()));
                                    cmd.Parameters.Add(new OracleParameter("FINGERDATA", sTmpData));

                                    cmd.ExecuteNonQuery();
                                }

                            }
                            
                        }

                        
                    }
                }

            }
            return lstFPTemplates;
        }

        public ICollection<UserInfo> GetAllUserInfoFromDatabase(ZkemClient objZkeeper, int machineNumber)
        {
            List<UserInfo> lstFPTemplates = new List<UserInfo>();

            DialogResult result = MessageBox.Show("WANT TO COPY DATA FROM DATABASE TO DEVICE?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                using (OracleConnection conn = new OracleConnection("User Id=hr;Password=123456;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))"))
                {
                    conn.Open();

                    using (OracleCommand cmd = new OracleCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT MachineNumber, EnrollNumber, Name, FingerIndex, Privilege, Password, Enabled, iFlag, FINGERData FROM BIOMETRICUSERINFOTEMP WHERE MachineNumber = :MachineNumber";
                        cmd.Parameters.Add(new OracleParameter("MachineNumber", machineNumber));

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UserInfo fpInfo = new UserInfo();
                                fpInfo.MachineNumber = reader.GetInt32(0);
                                fpInfo.EnrollNumber = reader.GetString(1);
                                if (reader.IsDBNull(2))
                                {
                                    fpInfo.Name = " ";
                                }
                                else
                                {
                                    fpInfo.Name = reader.GetString(2);
                                }

                                fpInfo.FingerIndex = reader.GetInt32(3);
                                fpInfo.Privelage = reader.GetInt32(4);

                                if (reader.IsDBNull(5))
                                {
                                    fpInfo.Password = " ";
                                }
                                else
                                {
                                    fpInfo.Password = reader.GetString(5);
                                }
                                if (reader.GetString(6) == "Y")
                                {
                                    fpInfo.Enabled = true;
                                }
                                else
                                {
                                    fpInfo.Enabled = false;
                                }

                                // fpInfo.Enabled = reader.GetString(6) == "1"; // Assuming '1' is true and '0' is false
                                fpInfo.iFlag = reader.GetInt32(7).ToString();
                                if (reader.IsDBNull(8))
                                {
                                    fpInfo.TmpData = " ";
                                }  
                                else
                                {
                                    fpInfo.TmpData = reader.GetString(8);
                                }

                                //fpInfo.TmpData = reader.GetString(8); // Assuming this is the fingerprint template

                                lstFPTemplates.Add(fpInfo);
                            }
                        }
                    }
                }

                bool isSuccess = UploadFTPTemplate(objZkeeper, machineNumber, lstFPTemplates);
                if (!isSuccess)
                {
                    MessageBox.Show("Failed to upload the data to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }else
                    MessageBox.Show("DATA UPLOADED.");
            }

            return lstFPTemplates;
        }


        public ICollection<UserInfo> GetAllUserInfoFromDatabase(int machineNumber)
        {
            List<UserInfo> lstFPTemplates = new List<UserInfo>();

            using (OracleConnection conn = new OracleConnection("User Id=hr;Password=123456;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))"))
            {
                conn.Open();

                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT MachineNumber, EnrollNumber, Name, FingerIndex, Privilege, Password, Enabled, iFlag, FINGERData FROM BIOMETRICUSERINFOTEMP WHERE MachineNumber = :MachineNumber";
                    cmd.Parameters.Add(new OracleParameter("MachineNumber", machineNumber));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UserInfo fpInfo = new UserInfo();
                            fpInfo.MachineNumber = reader.GetInt32(0);
                            fpInfo.EnrollNumber = reader.GetString(1);
                            fpInfo.Name = reader.IsDBNull(2) ? " " : reader.GetString(2);
                            fpInfo.FingerIndex = reader.GetInt32(3);
                            fpInfo.Privelage = reader.GetInt32(4);
                            fpInfo.Password = reader.IsDBNull(5) ? " " : reader.GetString(5);
                            fpInfo.Enabled = reader.GetString(6) == "Y";
                            fpInfo.iFlag = reader.GetInt32(7).ToString();
                            fpInfo.TmpData = reader.IsDBNull(8) ? " " : reader.GetString(8);

                            lstFPTemplates.Add(fpInfo);
                        }
                    }
                }
            }

            return lstFPTemplates;
        }

        public ICollection<MachineInfo> GetLogData(ZkemClient objZkeeper, int machineNumber)
        {
            string dwEnrollNumber1 = "";
            int dwVerifyMode = 0;
            int dwInOutMode = 0;
            int dwYear = 0;
            int dwMonth = 0;
            int dwDay = 0;
            int dwHour = 0;
            int dwMinute = 0;
            int dwSecond = 0;
            int dwWorkCode = 0;

            ICollection<MachineInfo> lstEnrollData = new List<MachineInfo>();

            objZkeeper.ReadAllGLogData(machineNumber);

            while (objZkeeper.SSR_GetGeneralLogData(machineNumber, out dwEnrollNumber1, out dwVerifyMode, out dwInOutMode, out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode))


            {
                string inputDate = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond).ToString();

                MachineInfo objInfo = new MachineInfo();
                objInfo.MachineNumber = machineNumber;
                objInfo.IndRegID = int.Parse(dwEnrollNumber1);
                objInfo.DateTimeRecord = inputDate;

                lstEnrollData.Add(objInfo);
            }

            return lstEnrollData;
        }

        public ICollection<UserIDInfo> GetAllUserID(ZkemClient objZkeeper, int machineNumber)
        {
            int dwEnrollNumber = 0;
            int dwEMachineNumber = 0;
            int dwBackUpNumber = 0;
            int dwMachinePrivelage = 0;
            int dwEnabled = 0;

            ICollection<UserIDInfo> lstUserIDInfo = new List<UserIDInfo>();

            while (objZkeeper.GetAllUserID(machineNumber, ref dwEnrollNumber, ref dwEMachineNumber, ref dwBackUpNumber, ref dwMachinePrivelage, ref dwEnabled))
            {
                UserIDInfo userID = new UserIDInfo();
                userID.BackUpNumber = dwBackUpNumber;
                userID.Enabled = dwEnabled;
                userID.EnrollNumber = dwEnrollNumber;
                userID.MachineNumber = dwEMachineNumber;
                userID.Privelage = dwMachinePrivelage;
                lstUserIDInfo.Add(userID);
            }
            return lstUserIDInfo;
        }

        public void GetGeneratLog(ZkemClient objZkeeper, int machineNumber, string enrollNo)
        {
            string name = null;
            string password = null;
            int previlage = 0;
            bool enabled = false;
            byte[] byTmpData = new byte[2000];
            int tempLength = 0;

            int idwFingerIndex = 0;// [ <--- Enter your fingerprint index here ]
            int iFlag = 0;

            objZkeeper.ReadAllTemplate(machineNumber);

            while (objZkeeper.SSR_GetUserInfo(machineNumber, enrollNo, out name, out password, out previlage, out enabled))
            {
                if (objZkeeper.GetUserTmpEx(machineNumber, enrollNo, idwFingerIndex, out iFlag, out byTmpData[0], out tempLength))
                {
                    break;
                }
            }
        }


        public bool PushUserDataToDevice(ZkemClient objZkeeper, int machineNumber, string enrollNo)
        {
            string userName = string.Empty;
            string password = string.Empty;
            int privelage = 1;
            return objZkeeper.SSR_SetUserInfo(machineNumber, enrollNo, userName, password, privelage, true);
        }

        public bool UploadFTPTemplate(ZkemClient objZkeeper, int machineNumber, List<UserInfo> lstUserInfo)
        {
            string sdwEnrollNumber = string.Empty, sName = string.Empty, sTmpData = string.Empty;
            int idwFingerIndex = 0, iPrivilege = 0, iFlag = 1, iUpdateFlag = 1;
            string sPassword = "";
            string sEnabled = "";
            bool bEnabled = false;
            objZkeeper.EnableDevice(machineNumber, false); // Disable the device before starting
            if (!objZkeeper.BeginBatchUpdate(machineNumber, iUpdateFlag))
            {
                MessageBox.Show("Failed to begin batch update.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (objZkeeper.BeginBatchUpdate(machineNumber, iUpdateFlag))
            {
                string sLastEnrollNumber = "";

                for (int i = 0; i < lstUserInfo.Count; i++)
                {
                    sdwEnrollNumber = lstUserInfo[i].EnrollNumber;
                    sName = lstUserInfo[i].Name;
                    idwFingerIndex = lstUserInfo[i].FingerIndex;
                    sTmpData = lstUserInfo[i].TmpData;
                    iPrivilege = lstUserInfo[i].Privelage;
                    sPassword = lstUserInfo[i].Password;
                    sEnabled = lstUserInfo[i].Enabled.ToString();
                    iFlag = Convert.ToInt32(lstUserInfo[i].iFlag);
                    bEnabled = lstUserInfo[i].Enabled;

                    if (sdwEnrollNumber != sLastEnrollNumber)
                    {
                        bool userInfoSet = objZkeeper.SSR_SetUserInfo(machineNumber, sdwEnrollNumber, sName, sPassword, iPrivilege, bEnabled);
                        //byte[] fingerprintTemplate = Convert.FromBase64String(sTmpData);
                        bool tmpDataSet = objZkeeper.SetUserTmpExStr(machineNumber, sdwEnrollNumber, idwFingerIndex, iFlag, sTmpData);

                        if (!userInfoSet || !tmpDataSet)
                        {
                            objZkeeper.EnableDevice(machineNumber, true);
                            MessageBox.Show($"Failed to upload user {sdwEnrollNumber} to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        bool tmpDataSet = objZkeeper.SetUserTmpExStr(machineNumber, sdwEnrollNumber, idwFingerIndex, iFlag, sTmpData);
                        if (!tmpDataSet)
                        {
                            MessageBox.Show($"Failed to upload template for user {sdwEnrollNumber} to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }

                    

                    sLastEnrollNumber = sdwEnrollNumber;
                }
                objZkeeper.RefreshData(machineNumber);
                objZkeeper.EnableDevice(machineNumber, true);
                
                ICollection<UserInfo> lstFingerPrintTemplates = GetAllUserInfo(objZkeeper, machineNumber);
                
            return true;
            }
            else
            {
                MessageBox.Show("Failed to begin batch update.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        public ICollection<UserInfo> UploadFTPTemplate_return (ZkemClient objZkeeper, int machineNumber, List<UserInfo> lstUserInfo)
        {
            string sdwEnrollNumber = string.Empty, sName = string.Empty, sTmpData = string.Empty;
            int idwFingerIndex = 0, iPrivilege = 0, iFlag = 1, iUpdateFlag = 1;
            string sPassword = "";
            bool bEnabled = false;

            objZkeeper.ClearData(machineNumber, 5);     
            objZkeeper.ClearData(machineNumber, 2);
            objZkeeper.EnableDevice(machineNumber, false); 

            if (!objZkeeper.BeginBatchUpdate(machineNumber, iUpdateFlag))
            {
                MessageBox.Show("Failed to begin batch update.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null; 
            }

            string sLastEnrollNumber = "";

            foreach (var userInfo in lstUserInfo)
            {
                sdwEnrollNumber = userInfo.EnrollNumber;
                sName = userInfo.Name;
                idwFingerIndex = userInfo.FingerIndex;
                sTmpData = userInfo.TmpData;
                iPrivilege = userInfo.Privelage;
                sPassword = userInfo.Password;
                bEnabled = userInfo.Enabled;
                iFlag = Convert.ToInt32(userInfo.iFlag);

                bool userInfoSet = false;
                bool tmpDataSet = false;

                if (sdwEnrollNumber != sLastEnrollNumber)
                {
                    userInfoSet = objZkeeper.SSR_SetUserInfo(machineNumber, sdwEnrollNumber, sName, sPassword, iPrivilege, bEnabled);
                    //byte[] fingerprintTemplate = Convert.FromBase64String(sTmpData);
                    tmpDataSet = objZkeeper.SetUserTmpExStr(machineNumber, sdwEnrollNumber, idwFingerIndex, iFlag, sTmpData);

                    if (!userInfoSet || !tmpDataSet)
                    {
                        objZkeeper.EnableDevice(machineNumber, true);
                        MessageBox.Show($"Failed to upload user {sdwEnrollNumber} to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }

                else
                {
                    tmpDataSet = objZkeeper.SetUserTmpExStr(machineNumber, sdwEnrollNumber, idwFingerIndex, iFlag, sTmpData);
                    if (!tmpDataSet)
                    {
                        MessageBox.Show($"Failed to upload template for user {sdwEnrollNumber} to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }


                sLastEnrollNumber = sdwEnrollNumber;
            }

            objZkeeper.BatchUpdate(machineNumber);
            objZkeeper.RefreshData(machineNumber);
            objZkeeper.EnableDevice(machineNumber, true);

            // Retrieve the updated list of user info from the device
            ICollection<UserInfo> lstFingerPrintTemplates = GetAllUserInfo(objZkeeper, machineNumber);

            return lstFingerPrintTemplates; // Return the retrieved data
        }


        public object ClearData(ZkemClient objZkeeper, int machineNumber, ClearFlag clearFlag)
        {
            int iDataFlag = (int)clearFlag;

            if (objZkeeper.ClearData(machineNumber, iDataFlag))
                return objZkeeper.RefreshData(machineNumber);
            else
            {
                int idwErrorCode = 0;
                objZkeeper.GetLastError(ref idwErrorCode);
                return idwErrorCode;
            }
        }

        public bool ClearGLog(ZkemClient objZkeeper, int machineNumber)
        {
            return objZkeeper.ClearGLog(machineNumber);
        }


        public string FetchDeviceInfo(ZkemClient objZkeeper, int machineNumber)
        {
            StringBuilder sb = new StringBuilder();

            string returnValue = string.Empty;


            objZkeeper.GetFirmwareVersion(machineNumber, ref returnValue);
            if (returnValue.Trim() != string.Empty)
            {
                sb.Append("Firmware V: ");
                sb.Append(returnValue);
                sb.Append(",");
            }


            returnValue = string.Empty;
            objZkeeper.GetVendor(ref returnValue);
            if (returnValue.Trim() != string.Empty)
            {
                sb.Append("Vendor: ");
                sb.Append(returnValue);
                sb.Append(",");
            }

            string sWiegandFmt = string.Empty;
            objZkeeper.GetWiegandFmt(machineNumber, ref sWiegandFmt);

            returnValue = string.Empty;
            objZkeeper.GetSDKVersion(ref returnValue);
            if (returnValue.Trim() != string.Empty)
            {
                sb.Append("SDK V: ");
                sb.Append(returnValue);
                sb.Append(",");
            }

            returnValue = string.Empty;
            objZkeeper.GetSerialNumber(machineNumber, out returnValue);
            if (returnValue.Trim() != string.Empty)
            {
                sb.Append("Serial No: ");
                sb.Append(returnValue);
                sb.Append(",");
            }

            returnValue = string.Empty;
            objZkeeper.GetDeviceMAC(machineNumber, ref returnValue);
            if (returnValue.Trim() != string.Empty)
            {
                sb.Append("Device MAC: ");
                sb.Append(returnValue);
            }

            return sb.ToString();
        }




    }
}
