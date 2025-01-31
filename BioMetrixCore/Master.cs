﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Runtime.InteropServices.ComTypes;
using System.Data;
using System.Threading.Tasks;


namespace BioMetrixCore
{
    public partial class Master : Form
    {
        DeviceManipulator manipulator = new DeviceManipulator();
        public ZkemClient objZkeeper;
        private bool isDeviceConnected = false;
        string oradb = "User Id=hr;Password=123456;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.9)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))";
        public OracleConnection conn;
        

        public bool IsDeviceConnected
        {
            get { return isDeviceConnected; }
            set
            {
                isDeviceConnected = value;
                if (isDeviceConnected)
                {
                    ShowStatusBar("The device is connected !!", true);
                    btnConnect.Text = "Disconnect";
                    ToggleControls(true);
                }
                else
                {
                    ShowStatusBar("The device is diconnected !!", true);
                    objZkeeper.Disconnect();
                    btnConnect.Text = "Connect";
                    ToggleControls(false);
                }
            }
        }
        /// <summary>
        /// ///////////////////////////////////////////////////////
        /// </summary>
        /// <param name="value"></param>

        private void ToggleControls(bool value)
        {
            btnBeep.Enabled = value;
            btnClearLogs.Enabled = value;
            btnDownloadFingerPrint.Enabled = value;
            btnPullData.Enabled = value;
            btnPowerOff.Enabled = value;
            btnRestartDevice.Enabled = value;
            btnGetDeviceTime.Enabled = value;
            btnEnableDevice.Enabled = value;
            btnDisableDevice.Enabled = value;
            btnGetAllUserID.Enabled = value;
            btnUploadUserInfo.Enabled = value;
            tbxMachineNumber.Enabled = !value;
            tbxPort.Enabled = !value;
            tbxDeviceIP.Enabled = !value;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            btnTogglePrivilege.Enabled = false;
        }

        public Master()
        {
            InitializeComponent();
            ToggleControls(false);
            ShowStatusBar(string.Empty, true);
            DisplayEmpty();
        }


        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        ShowStatusBar("The device is switched off", true);
                        DisplayEmpty();
                        btnConnect.Text = "Connect";
                        ToggleControls(false);
                        break;
                    }

                default:
                    break;
            }

        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                ShowStatusBar(string.Empty, true);

                if (IsDeviceConnected)
                {
                    IsDeviceConnected = false;
                    this.Cursor = Cursors.Default;

                    return;
                }

                string ipAddress = tbxDeviceIP.Text.Trim();
                string port = tbxPort.Text.Trim();
                if (ipAddress == string.Empty || port == string.Empty)
                    throw new Exception("The Device IP Address and Port is mandotory !!");

                int portNumber = 4370;
                if (!int.TryParse(port, out portNumber))
                    throw new Exception("Not a valid port number");

                bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The Device IP is invalid !!");

                isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The device at " + ipAddress + ":" + port + " did not respond!!");

                objZkeeper = new ZkemClient(RaiseDeviceEvent);
                IsDeviceConnected = objZkeeper.Connect_Net(ipAddress, portNumber);

                if (IsDeviceConnected)
                {
                    string deviceInfo = manipulator.FetchDeviceInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                    lblDeviceInfo.Text = deviceInfo;
                }

            }
            catch (Exception ex)
            {
                ShowStatusBar(ex.Message, false);
            }
            this.Cursor = Cursors.Default;

        }


        public void ShowStatusBar(string message, bool type)
        {
            if (message.Trim() == string.Empty)
            {
                lblStatus.Visible = false;
                return;
            }

            lblStatus.Visible = true;
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.White;

            if (type)
                lblStatus.BackColor = Color.FromArgb(79, 208, 154);
            else
                lblStatus.BackColor = Color.FromArgb(230, 112, 134);
        }


        private void btnPingDevice_Click(object sender, EventArgs e)
        {
            ShowStatusBar(string.Empty, true);

            string ipAddress = tbxDeviceIP.Text.Trim();

            bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
            if (!isValidIpA)
                throw new Exception("The Device IP is invalid !!");

            isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
            if (isValidIpA)
                ShowStatusBar("The device is active", true);
            else
                ShowStatusBar("Could not read any response", false);
        }

        private void btnGetAllUserID_Click(object sender, EventArgs e)
        {
            try
            {

                btnTogglePrivilege.Enabled = false;
                btnClearDatabase.Enabled = false;
                btnDeleteUser.Enabled = false;
                BtnPush.Enabled = false;
                btnPushLog.Enabled = false;
                UploadToDevice.Enabled = false;
                ICollection<UserIDInfo> lstUserIDInfo = manipulator.GetAllUserID(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

                if (lstUserIDInfo != null && lstUserIDInfo.Count > 0)
                {
                    BindToGridView(lstUserIDInfo);
                    ShowStatusBar(lstUserIDInfo.Count + " records found !!", true);
                }
                else
                {
                    DisplayEmpty();
                    DisplayListOutput("No records found");
                }

            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }

        }

        private void btnBeep_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            objZkeeper.Beep(100);
        }

        private void btnDownloadFingerPrint_Click(object sender, EventArgs e)
        {
            try
            {
                btnClearDatabase.Enabled = false;
                btnPushLog.Enabled = false;
                UploadToDevice.Enabled = false;
                btnTogglePrivilege.Enabled = true;
                btnDeleteUser.Enabled = true;
                BtnPush.Enabled = true;
                ShowStatusBar(string.Empty, true);

                ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
                {
                    BindToGridView(lstFingerPrintTemplates);
                    ShowStatusBar(lstFingerPrintTemplates.Count + " records found !!", true);
                }
                else
                    DisplayListOutput("No records found");
            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }

        }

        //        private void btnDownloadFingerPrint_Click(object sender, EventArgs e)
        //        {
        //            try
        //            {
        //                ShowStatusBar(string.Empty, true);
        //                ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

        //                DialogResult result = MessageBox.Show("Do you want to copy the fingerprint data to the database?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        //                if (result == DialogResult.Yes)
        //                {


        //                    if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
        //                    {
        //                        using (OracleConnection conn = new OracleConnection("User Id=hr;Password=123456;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))"
        //))
        //                        {  conn.Open();

        //                                foreach (var userInfo in lstFingerPrintTemplates)
        //                                {
        //                                    int machinePrivilege = 0;
        //                                    int enrollData = 0;
        //                                    int passWord = 0;
        //                                    int enrollNumber = int.Parse(userInfo.EnrollNumber);


        //                                    bool isSuccess = objZkeeper.GetEnrollData(
        //                                        userInfo.MachineNumber,
        //                                        enrollNumber,
        //                                        userInfo.MachineNumber,
        //                                        userInfo.FingerIndex,
        //                                        ref machinePrivilege,
        //                                        ref enrollData,
        //                                        ref passWord);

        //                                    if (isSuccess)
        //                                    {
        //                                        using (OracleCommand cmd = new OracleCommand())
        //                                        {
        //                                            cmd.Connection = conn;
        //                                            cmd.CommandText = @"
        //                                    INSERT INTO BiometricUserInfo 
        //                                    (MachineNumber, EnrollNumber, Name, FingerIndex, TmpData, Privilege, Password, Enabled, iFlag)
        //                                    VALUES (:MachineNumber, :EnrollNumber, :Name, :FingerIndex, :TmpData, :Privilege, :Password, :Enabled, :iFlag)";

        //                                            // Set parameters
        //                                            cmd.Parameters.Add(new OracleParameter("MachineNumber", userInfo.MachineNumber));
        //                                            cmd.Parameters.Add(new OracleParameter("EnrollNumber", userInfo.EnrollNumber));
        //                                            cmd.Parameters.Add(new OracleParameter("Name", userInfo.Name));
        //                                            cmd.Parameters.Add(new OracleParameter("FingerIndex", userInfo.FingerIndex));
        //                                            cmd.Parameters.Add(new OracleParameter("TmpData", enrollData));  // Assuming enrollData is the fingerprint template
        //                                            cmd.Parameters.Add(new OracleParameter("Privilege", machinePrivilege));
        //                                            cmd.Parameters.Add(new OracleParameter("Password", passWord));
        //                                            cmd.Parameters.Add(new OracleParameter("Enabled", userInfo.Enabled));
        //                                            cmd.Parameters.Add(new OracleParameter("iFlag", userInfo.iFlag));


        //                                            cmd.ExecuteNonQuery();
        //                                        }
        //                                    }
        //                                }
        //                            }

        //                            BindToGridView(lstFingerPrintTemplates);
        //                        ShowStatusBar(lstFingerPrintTemplates.Count + " records inserted into the database.", true);
        //                    }
        //                    else
        //                    {
        //                        DisplayListOutput("No records found");
        //                    }
        //                }
        //                else
        //                {
        //                    // If the user chooses 'No', do nothing and return

        //                    BindToGridView(lstFingerPrintTemplates);
        //                    ShowStatusBar(lstFingerPrintTemplates.Count + " records inserted into the database.", true);

        //                    DisplayListOutput("Operation canceled by the user.");
        //                    ShowStatusBar("Operation canceled.", true);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                DisplayListOutput(ex.Message);
        //            }
        //        }

        private void btnPullData_Click(object sender, EventArgs e)
        {
            try
            {

                btnTogglePrivilege.Enabled = false;
                btnClearDatabase.Enabled = false;
                btnDeleteUser.Enabled = false;
                BtnPush.Enabled = false;
                btnPushLog.Enabled = true;
                UploadToDevice.Enabled = false;
                ShowStatusBar(string.Empty, true);

                ICollection<MachineInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

                if (lstMachineInfo != null && lstMachineInfo.Count > 0)
                {
                   
                    BindToGridView(lstMachineInfo);
                    ShowStatusBar(lstMachineInfo.Count + " records found !!", true);
                }
                else
                {
                    btnPushLog.Enabled = false;
                    DisplayListOutput("No records found");
                }
            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }

        }

        private void ClearGrid()
        {
            if (dgvRecords.Controls.Count > 2)
            { dgvRecords.Controls.RemoveAt(2); }


            dgvRecords.DataSource = null;
            dgvRecords.Controls.Clear();
            dgvRecords.Rows.Clear();
            dgvRecords.Columns.Clear();
        }
       
        private void BindToGridView(object list)
        {
            ClearGrid();
            dgvRecords.DataSource = list;
            dgvRecords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            UniversalStatic.ChangeGridProperties(dgvRecords);
        }

        private void DisplayListOutput(string message)
        {
            if (dgvRecords.Controls.Count > 2)
            { dgvRecords.Controls.RemoveAt(2); }

            ShowStatusBar(message, false);
        }

        private void DisplayEmpty()
        {
            ClearGrid();
            dgvRecords.Controls.Add(new DataEmpty());
        }

        private void pnlHeader_Paint(object sender, PaintEventArgs e)
        { UniversalStatic.DrawLineInFooter(pnlHeader, Color.FromArgb(204, 204, 204), 2); }

        private void btnPowerOff_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            var resultDia = DialogResult.None;
            resultDia = MessageBox.Show("Do you wish to Power Off the Device ??", "Power Off Device", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resultDia == DialogResult.Yes)
            {
                bool deviceOff = objZkeeper.PowerOffDevice(int.Parse(tbxMachineNumber.Text.Trim()));

            }

            this.Cursor = Cursors.Default;
        }

        private void btnRestartDevice_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            DialogResult rslt = MessageBox.Show("Do you wish to restart the device now ??", "Restart Device", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rslt == DialogResult.Yes)
            {
                if (objZkeeper.RestartDevice(int.Parse(tbxMachineNumber.Text.Trim())))
                    ShowStatusBar("The device is being restarted, Please wait...", true);
                else
                    ShowStatusBar("Operation failed,please try again", false);
            }

        }

        private void btnGetDeviceTime_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            int machineNumber = int.Parse(tbxMachineNumber.Text.Trim());
            int dwYear = 0;
            int dwMonth = 0;
            int dwDay = 0;
            int dwHour = 0;
            int dwMinute = 0;
            int dwSecond = 0;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            bool result = objZkeeper.GetDeviceTime(machineNumber, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute, ref dwSecond);

            string deviceTime = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, dwSecond).ToString();
            List<DeviceTimeInfo> lstDeviceInfo = new List<DeviceTimeInfo>();
            lstDeviceInfo.Add(new DeviceTimeInfo() { DeviceTime = deviceTime });
            BindToGridView(lstDeviceInfo);
        }

        private void btnEnableDevice_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            DialogResult result = MessageBox.Show("Are you sure you want to delete all users from the device?",
                                                  "Delete Confirmation",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                objZkeeper.ClearData(1, 5); // 5 for deleting fingerprint templates
                objZkeeper.ClearData(1, 2); // 2 for deleting user data
                MessageBox.Show("All users have been deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearGrid();
            }
            else
            {
                MessageBox.Show("Operation canceled.", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void btnDisableDevice_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;
            // This is of no use since i implemented zkemKeeper the other way
            bool deviceDisabled = objZkeeper.DisableDeviceWithTimeOut(int.Parse(tbxMachineNumber.Text.Trim()), 3000);
        }

        private void tbxPort_TextChanged(object sender, EventArgs e)
        { UniversalStatic.ValidateInteger(tbxPort); }

        private void tbxMachineNumber_TextChanged(object sender, EventArgs e)
        { UniversalStatic.ValidateInteger(tbxMachineNumber); }

        //private void btnUploadUserInfo_Click(object sender, EventArgs e)
        //{
        //    ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfoFromDatabase(objZkeeper, 1);
        //    if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
        //    {
        //        BindToGridView(lstFingerPrintTemplates);
        //        ShowStatusBar(lstFingerPrintTemplates.Count + " records found !!", true);
        //    }
        //    else
        //    {
        //        DisplayEmpty();
        //        DisplayListOutput("No records found");
        //    }
        //    //string str = manipulator.FetchDeviceInfo(objZkeeper, 1);
        //    //ShowStatusBar("DEVICE INFO : " + str, true);
        //}

        private void btnUploadUserInfo_Click(object sender, EventArgs e)
        {

            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = true;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = true;
            // Add you new UserInfo Here and  uncomment the below code
            //List<UserInfo> lstUserInfo = new List<UserInfo>();
            //manipulator.UploadFTPTemplate(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()), lstUserInfo);
            ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfoFromDatabase(1);
            if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
            {
                BindToGridView(lstFingerPrintTemplates);
                ShowStatusBar(lstFingerPrintTemplates.Count + " records found !!", true);
            }
            else
            {
                DisplayEmpty();
                DisplayListOutput("No records found");
            }
        }

        //private void BtnPush_Click_1(object sender, EventArgs e)
        //{


        //    conn = new OracleConnection(oradb);
        //    try
        //    {
        //        conn.Open();
        //        ShowStatusBar("Database Connected", true);

        //        for (int i = 0; i < dgvRecords.Rows.Count - 1; i++)
        //        {


        //            if (conn.State == ConnectionState.Open)
        //            {
        //                string sqlInsert = "insert into tbl_zkt_log (empid,sc_time,scc_date) ";
        //                sqlInsert += "values (LPAD(:p_empid,5,0),:p_sc_time,:p_scc_date)";

        //                OracleCommand cmdInsert = new OracleCommand();
        //                cmdInsert.CommandText = sqlInsert;
        //                cmdInsert.Connection = conn;
        //                OracleParameter pEmpnum = new OracleParameter();
        //                OracleParameter pSctime = new OracleParameter();
        //                OracleParameter pScdate = new OracleParameter();
        //                for (int j = 0; j < dgvRecords.Columns.Count; j++)
        //                {
        //                    /*writer.Write("\t" + dgvRecords.Rows[i].Cells[j].Value.ToString() + "\t" + "|");*/
        //                    switch (j)
        //                    {
        //                        case 1:


        //                            pEmpnum.Value = dgvRecords.Rows[i].Cells[j].Value.ToString();
        //                            pEmpnum.ParameterName = "p_empid";
        //                            break;
        //                        case 2:
        //                            DateTime sct = DateTime.Parse(dgvRecords.Rows[i].Cells[j].Value.ToString());
        //                            pSctime.DbType = DbType.DateTime;
        //                            pSctime.Value = sct;
        //                            pSctime.ParameterName = "p_sc_time";
        //                            break;
        //                        case 3:

        //                            DateTime scd = DateTime.Parse(dgvRecords.Rows[i].Cells[j].Value.ToString());
        //                            pScdate.DbType = DbType.Date;
        //                            pScdate.Value = scd;
        //                            pScdate.ParameterName = "p_scc_date";
        //                            break;
        //                        default:
        //                            break;
        //                    }


        //                }

        //                cmdInsert.Parameters.Add(pEmpnum);
        //                cmdInsert.Parameters.Add(pSctime);
        //                cmdInsert.Parameters.Add(pScdate);

        //                cmdInsert.ExecuteNonQuery();

        //                cmdInsert.Dispose();
        //            }
        //            /*writer.WriteLine("");
        //            writer.WriteLine("-----------------------------------------------------");*/
        //        }
        //        /*writer.Close();*/
        //        BtnPush.Enabled = false;
        //        DisplayListOutput("Data Exported");

        //    }
        //    catch (Exception ex)
        //    {
        //        DisplayListOutput(ex.Message);
        //    }



        //}

        private void BtnPush_Click_1(object sender, EventArgs e)
        {
            conn = new OracleConnection(oradb);
            try
            {
                conn.Open();
                ShowStatusBar("Database Connected", true);

                string deviceIP = tbxDeviceIP.Text; 

                for (int i = 0; i < dgvRecords.Rows.Count; i++)
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        string sqlInsert = "INSERT INTO BIOMETRICUSERINFOTEMP (MachineNumber, EnrollNumber, Name, FingerIndex, Privilege, Password, Enabled, iFlag, FINGERData, DeviceIP) ";
                        sqlInsert += "VALUES (:p_MachineNumber, :p_EnrollNumber, :p_Name, :p_FingerIndex, :p_Privilege, :p_Password, :p_Enabled, :p_iFlag, :p_FINGERData, :p_DeviceIP)";

                        OracleCommand cmdInsert = new OracleCommand();
                        cmdInsert.CommandText = sqlInsert;
                        cmdInsert.Connection = conn;

                        OracleParameter pMachineNumber = new OracleParameter();
                        OracleParameter pEnrollNumber = new OracleParameter();
                        OracleParameter pName = new OracleParameter();
                        OracleParameter pFingerIndex = new OracleParameter();
                        OracleParameter pPrivilege = new OracleParameter();
                        OracleParameter pPassword = new OracleParameter();
                        OracleParameter pEnabled = new OracleParameter();
                        OracleParameter piFlag = new OracleParameter();
                        OracleParameter pFINGERData = new OracleParameter();
                        OracleParameter pDeviceIP = new OracleParameter();

                        for (int j = 0; j < dgvRecords.Columns.Count; j++)
                        {
                            switch (j)
                            {
                                case 0: 
                                    pMachineNumber.Value = Convert.ToInt32(dgvRecords.Rows[i].Cells[j].Value);
                                    pMachineNumber.ParameterName = "p_MachineNumber";
                                    break;
                                case 1: 
                                    pEnrollNumber.Value = dgvRecords.Rows[i].Cells[j].Value.ToString();
                                    pEnrollNumber.ParameterName = "p_EnrollNumber";
                                    break;
                                case 2: 
                                    pName.Value = dgvRecords.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                                    pName.ParameterName = "p_Name";
                                    break;
                                case 3: 
                                    pFingerIndex.Value = Convert.ToInt32(dgvRecords.Rows[i].Cells[j].Value);
                                    pFingerIndex.ParameterName = "p_FingerIndex";
                                    break;
                                case 4: 
                                    pFINGERData.Value = dgvRecords.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                                    pFINGERData.ParameterName = "p_FINGERData";
                                    break;
                                case 5: 
                                    pPrivilege.Value = Convert.ToInt32(dgvRecords.Rows[i].Cells[j].Value);
                                    pPrivilege.ParameterName = "p_Privilege";
                                    break;
                                case 6: 
                                    pPassword.Value = dgvRecords.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                                    pPassword.ParameterName = "p_Password";
                                    break;
                                case 7: 
                                    pEnabled.Value = (bool)dgvRecords.Rows[i].Cells[j].Value ? 'Y' : 'N';
                                    pEnabled.ParameterName = "p_Enabled";
                                    break;
                                case 8: 
                                    piFlag.Value = dgvRecords.Rows[i].Cells[j].Value?.ToString() ?? string.Empty;
                                    piFlag.ParameterName = "p_iFlag";
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Set the DeviceIP parameter
                        pDeviceIP.Value = deviceIP;
                        pDeviceIP.ParameterName = "p_DeviceIP";

                        cmdInsert.Parameters.Add(pMachineNumber);
                        cmdInsert.Parameters.Add(pEnrollNumber);
                        cmdInsert.Parameters.Add(pName);
                        cmdInsert.Parameters.Add(pFingerIndex);
                        cmdInsert.Parameters.Add(pPrivilege);
                        cmdInsert.Parameters.Add(pPassword);
                        cmdInsert.Parameters.Add(pEnabled);
                        cmdInsert.Parameters.Add(piFlag);
                        cmdInsert.Parameters.Add(pFINGERData);
                        cmdInsert.Parameters.Add(pDeviceIP);

                        cmdInsert.ExecuteNonQuery();
                        cmdInsert.Dispose();
                    }
                }

                BtnPush.Enabled = false;
                DisplayListOutput("Data Exported");
            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }
        }


        //private void UploadToDevice_Click(object sender, EventArgs e)
        //{
        //   // var lstFingerPrintTemplates = manipulator.GetFingerPrintTemplatesFromGrid();
        //    List<UserInfo> lstFingerPrintTemplates = new List<UserInfo>();

        //    foreach (DataGridViewRow row in dgvRecords.Rows)
        //    {
        //        if (row.Cells["EnrollNumber"].Value != null)
        //        {
        //            UserInfo userInfo = new UserInfo();
        //            userInfo.EnrollNumber = row.Cells["EnrollNumber"].Value.ToString();
        //            userInfo.Name = row.Cells["Name"].Value?.ToString();
        //            userInfo.FingerIndex = Convert.ToInt32(row.Cells["FingerIndex"].Value);
        //            userInfo.Privelage = Convert.ToInt32(row.Cells["Privelage"].Value);
        //            userInfo.Password = row.Cells["Password"].Value?.ToString();
        //            userInfo.Enabled = Convert.ToBoolean(row.Cells["Enabled"].Value);
        //            userInfo.iFlag = row.Cells["iFlag"].Value?.ToString();
        //            userInfo.TmpData = row.Cells["TmpData"].Value?.ToString();

        //            lstFingerPrintTemplates.Add(userInfo);
        //        }
        //    }


        //    if (lstFingerPrintTemplates == null || lstFingerPrintTemplates.Count == 0)
        //    {
        //        MessageBox.Show("No data available for upload. Please fetch data first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        return;
        //    }

        //    DialogResult result = MessageBox.Show("Do you want to upload the data from the grid to the device?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        //    if (result == DialogResult.Yes)
        //    {
        //        ShowStatusBar("DATA IS UPLOADING, IT TAKES AROUND ABOUT 5-10 mins !!", true);
        //        ICollection<UserInfo> lstFingerPrintTemplates_ = manipulator.UploadFTPTemplate_return(objZkeeper, 1, lstFingerPrintTemplates);


        //        if (lstFingerPrintTemplates_ == null)
        //        {
        //            ShowStatusBar("Failed !!", true);
        //            MessageBox.Show("Failed to upload the data to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //        else
        //        {
        //            BindToGridView(lstFingerPrintTemplates_);
        //            ShowStatusBar(lstFingerPrintTemplates_.Count + " records found !!", true);
        //            MessageBox.Show("Data uploaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //    }

        //}

        private async void UploadToDevice_Click(object sender, EventArgs e)
        {
            // var lstFingerPrintTemplates = manipulator.GetFingerPrintTemplatesFromGrid();
            List<UserInfo> lstFingerPrintTemplates = new List<UserInfo>();

            foreach (DataGridViewRow row in dgvRecords.Rows)
            {
                if (row.Cells["EnrollNumber"].Value != null)
                {
                    UserInfo userInfo = new UserInfo();
                    userInfo.EnrollNumber = row.Cells["EnrollNumber"].Value.ToString();
                    userInfo.Name = row.Cells["Name"].Value?.ToString();
                    userInfo.FingerIndex = Convert.ToInt32(row.Cells["FingerIndex"].Value);
                    userInfo.Privelage = Convert.ToInt32(row.Cells["Privelage"].Value);
                    userInfo.Password = row.Cells["Password"].Value?.ToString();
                    userInfo.Enabled = Convert.ToBoolean(row.Cells["Enabled"].Value);
                    userInfo.iFlag = row.Cells["iFlag"].Value?.ToString();
                    userInfo.TmpData = row.Cells["TmpData"].Value?.ToString();

                    lstFingerPrintTemplates.Add(userInfo);
                }
            }

            if (lstFingerPrintTemplates == null || lstFingerPrintTemplates.Count == 0)
            {
                MessageBox.Show("No data available for upload. Please fetch data first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show("Do you want to upload the data from the grid to the device?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                ShowStatusBar("DATA IS UPLOADING, IT TAKES AROUND ABOUT 5-10 mins !!", true);

                // Run the upload process on a background thread
                var uploadTask = Task.Run(() =>
                {
                    return manipulator.UploadFTPTemplate_return(objZkeeper, 1, lstFingerPrintTemplates);
                });

                // Await the task to allow UI to remain responsive
                var lstFingerPrintTemplates_ = await uploadTask;

                if (lstFingerPrintTemplates_ == null)
                {
                    ShowStatusBar("Failed !!", true);
                    MessageBox.Show("Failed to upload the data to the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    BindToGridView(lstFingerPrintTemplates_);
                    ShowStatusBar(lstFingerPrintTemplates_.Count + " records found !!", true);
                    MessageBox.Show("Data uploaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnDeleteUser_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvRecords.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a user from the grid view.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataGridViewRow selectedRow = dgvRecords.SelectedRows[0];

                int machineNumber = Convert.ToInt32(selectedRow.Cells["MachineNumber"].Value);
                string enrollNumber = selectedRow.Cells["EnrollNumber"].Value.ToString();
                int backupNumber =12;
                int num = Convert.ToInt32(enrollNumber);

                // Call the method to delete enrollment data
                //bool result = objZkeeper.DeleteEnrollData(machineNumber, num,1, backupNumber);
                bool result = objZkeeper.SSR_DeleteEnrollData(machineNumber, enrollNumber, backupNumber);

                if (result)
                {
                    MessageBox.Show("User data deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                   // MessageBox.Show("Failed to delete user data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ShowStatusBar(string.Empty, true);

           

                ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
                {
                    BindToGridView(lstFingerPrintTemplates);
                    ShowStatusBar(lstFingerPrintTemplates.Count + " records found !!", true);
                }

                bool dataExists = objZkeeper.SSR_GetUserInfo(machineNumber, enrollNumber, out _, out _, out _, out _);

                if (!dataExists)
                {
                    MessageBox.Show($"Enrollment data for user {enrollNumber} was deleted, but the operation returned false.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearDatabase_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete all data from the database?",
                                          "Delete Confirmation",
                                          MessageBoxButtons.YesNo,
                                          MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (conn = new OracleConnection(oradb))
                    {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand("DELETE FROM BIOMETRICUSERINFOTEMP", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("All data has been deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnUploadUserInfo_Click(objZkeeper,e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while deleting data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // If the user cancels, do nothing
                MessageBox.Show("Operation canceled.", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnPushLog_Click(object sender, EventArgs e)
        {
            conn = new OracleConnection(oradb);
            try
            {
                conn.Open();
                ShowStatusBar("Database Connected", true);

                for (int i = 0; i < dgvRecords.Rows.Count; i++)
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        string sqlInsert = "INSERT INTO tbl_zkt_log (empid, sc_time, scc_date, IP_ADDRESS) ";
                        sqlInsert += "VALUES (LPAD(:p_empid, 5, 0), :p_sc_time, :p_scc_date, :p_ip_address)";

                        OracleCommand cmdInsert = new OracleCommand();
                        cmdInsert.CommandText = sqlInsert;
                        cmdInsert.Connection = conn;

                        OracleParameter pEmpnum = new OracleParameter();
                        OracleParameter pSctime = new OracleParameter();
                        OracleParameter pScdate = new OracleParameter();
                        OracleParameter pIPAddress = new OracleParameter();

                        for (int j = 0; j < dgvRecords.Columns.Count; j++)
                        {
                            switch (j)
                            {
                                case 1:
                                    pEmpnum.Value = dgvRecords.Rows[i].Cells[j].Value.ToString();
                                    pEmpnum.ParameterName = "p_empid";
                                    break;
                                case 2:
                                    DateTime sct = DateTime.Parse(dgvRecords.Rows[i].Cells[j].Value.ToString());
                                    pSctime.DbType = DbType.DateTime;
                                    pSctime.Value = sct;
                                    pSctime.ParameterName = "p_sc_time";
                                    break;
                                case 3:
                                    DateTime scd = DateTime.Parse(dgvRecords.Rows[i].Cells[j].Value.ToString());
                                    pScdate.DbType = DbType.Date;
                                    pScdate.Value = scd;
                                    pScdate.ParameterName = "p_scc_date";
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Assign the IP address to the parameter
                        pIPAddress.Value = tbxDeviceIP.Text;
                        pIPAddress.ParameterName = "p_ip_address";

                        // Add parameters to the command
                        cmdInsert.Parameters.Add(pEmpnum);
                        cmdInsert.Parameters.Add(pSctime);
                        cmdInsert.Parameters.Add(pScdate);
                        cmdInsert.Parameters.Add(pIPAddress);

                        // Execute the command
                        cmdInsert.ExecuteNonQuery();
                        cmdInsert.Dispose();
                    }
                }

                btnPushLog.Enabled = false;
                DisplayListOutput("Data Exported");
            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }
        }


        private void btnTogglePrivilege_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvRecords.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a user from the grid view.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataGridViewRow selectedRow = dgvRecords.SelectedRows[0];

                int machineNumber = Convert.ToInt32(selectedRow.Cells["MachineNumber"].Value);
                string enrollNumber = selectedRow.Cells["EnrollNumber"].Value.ToString();

                bool userExists = objZkeeper.SSR_GetUserInfo(machineNumber, enrollNumber, out string name, out string password, out int privilege, out bool enabled);

                if (userExists)
                {
                    int newPrivilege = (privilege == 0) ? 3 : 0;

                    bool updateResult = objZkeeper.SSR_SetUserInfo(machineNumber, enrollNumber, name, password, newPrivilege, enabled);

                    if (updateResult)
                    {
                        MessageBox.Show("User privilege updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ICollection<UserInfo> lstFingerPrintTemplates = manipulator.GetAllUserInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                        if (lstFingerPrintTemplates != null && lstFingerPrintTemplates.Count > 0)
                        {
                            BindToGridView(lstFingerPrintTemplates);
                            ShowStatusBar(lstFingerPrintTemplates.Count + " records found !!", true);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to update user privilege.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("User not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            btnTogglePrivilege.Enabled = false;
            btnClearDatabase.Enabled = false;
            btnDeleteUser.Enabled = false;
            BtnPush.Enabled = false;
            btnPushLog.Enabled = false;
            UploadToDevice.Enabled = false;

            DialogResult result = MessageBox.Show("Are you sure you want to delete all log data from the device?",
                                                  "Delete Logs Confirmation",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                int machineNumber = 1;
                bool logCleared = objZkeeper.ClearGLog(machineNumber);

                if (logCleared)
                {
                    MessageBox.Show("All log data has been deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearGrid();
                }
                else
                {
                    MessageBox.Show("Failed to clear log data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Operation canceled.", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }


        }

       
    }
}
