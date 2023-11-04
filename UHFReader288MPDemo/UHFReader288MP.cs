using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using UHF;

namespace UHFReader288MPDemo
{
    public partial class UHFReader288MP : Form
    {
        #region Constant
        public const int USER = 0x0400;
        public const int WM_SENDTAG = USER + 101;
        public const int WM_SENDTAGSTAT = USER + 102;
        public const int WM_SENDSTATU = USER + 103;
        public const int WM_SENDBUFF = USER + 104;
        public const int WM_MIXTAG = USER + 105;
        public const int WM_SHOWNUM = USER + 106;
        public const int WM_FASTID = USER + 107;
        #endregion

        #region Variables
        private DeviceClass SelectedDevice;
        private byte fComAdr = 0xff;
        private int ferrorcode;
        private byte fBaud;
        private double fdminfre;
        private double fdmaxfre;
        private int fCmdRet = 30;
        private bool fisinventoryscan_6B;
        private byte[] fPassWord = new byte[4];
        private readonly ArrayList list = new ArrayList();
        private readonly List<string> epclist = new List<string>();
        private readonly List<string> tidlist = new List<string>();
        private string fInventory_EPC_List;
        private int frmcomportindex;
        private byte Target = 0;
        private byte InAnt = 0;
        private byte Scantime = 0;
        private byte FastFlag = 0;
        private byte Qvalue = 0;
        private byte Session = 0;
        private int total_tagnum = 0;
        private int CardNum = 0;
        private int total_time = 0;
        private int targettimes = 0;
        private byte TIDFlag = 0;
        private byte tidLen = 0;
        private byte tidAddr = 0;
        private int AA_times = 0;
        private int CommunicationTime = 0;
        private static List<DeviceClass> DevList;
        private static readonly SearchCallBack searchCallBack = new SearchCallBack(DeviceSearchCB);
        private string ReadTypes = "";
        private string epcandtid = "";
        private int lastnum = 0;
        private readonly byte[] antlist = new byte[4];
        private volatile bool fIsInventoryScan = false;
        private volatile bool toStopThread = false;
        private Thread mythread = null;

        private byte[] ReadAdr = new byte[2];
        private byte[] Psd = new byte[4];
        private byte ReadLen = 0;
        private byte ReadMem = 0;

        private readonly CTcpList tcplist = new CTcpList();
        private Thread listenThread = null;
        private Socket newsock = null;
        private Socket m_client;
        private Thread clientThread = null;
        private Thread ReadThread = null;
        private volatile bool fIsBuffScan = false;
        #endregion

        private delegate void WriteLogUnSafe(LogRichTextBox logRichTxt, string strLog, int nType);
        RFIDCallBack elegateRFIDCallBack;

        public UHFReader288MP()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            DevList = new List<DeviceClass>();

            DevControl.TagErrorCode eCode = DevControl.DM_Init(searchCallBack, IntPtr.Zero);
            if (eCode != DevControl.TagErrorCode.DM_ERR_OK)
                throw new Exception(ErrorHandling.HandleError(eCode));

            elegateRFIDCallBack = new RFIDCallBack(GetUid);

            gpb_rs232.Enabled = false;
            gpb_tcp.Enabled = false;
            rb_rs232.Checked = true;
            ComboBox_COM.SelectedIndex = 0;
            ComboBox_baud2.SelectedIndex = 3;
            com_Q.SelectedIndex = 4;
            com_Target.SelectedIndex = 0;

            for (int i = 0; i < 256; i++)
            {
                com_scantime.Items.Add(Convert.ToString(i) + "*100ms");
                comboBox_maxtime.Items.Add(Convert.ToString(i) + "*100ms");
            }
            com_scantime.SelectedIndex = 20;
            comboBox_maxtime.SelectedIndex = 0;
            com_S.SelectedIndex = 4;

            DisabledForm();

            radioButton_band2.Checked = true;
            ComboBox_baud.SelectedIndex = 3;
            ComboBox_PowerDbm.SelectedIndex = 30;

            for (int i = 0; i < 256; i++)
                com_MFliterTime.Items.Add(i.ToString() + "*1s");

            for (int i = 1; i < 256; i++)
                ComboBox_RelayTime.Items.Add(Convert.ToString(i));

            ComboBox_RelayTime.SelectedIndex = 0;
            ComboBox_RelayTime.SelectedIndex = 0;

            com_MFliterTime.SelectedIndex = 0;
            com_MRPTime.SelectedIndex = 0;
            com_MQ.SelectedIndex = 4;
            com_MS.SelectedIndex = 4;
            com_Mmode.SelectedIndex = 0;
            com_wpower.SelectedIndex = 30;
            com_retrytimes.SelectedIndex = 3;
            com_MixMem.SelectedIndex = 2;
            cbbAnt.SelectedIndex = 0;
        }

        #region DllImport
        [DllImport("User32.dll", EntryPoint = "PostMessage")]
        private static extern int PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, string lParam);

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        #endregion

        private static void DeviceSearchCB(IntPtr dev, IntPtr data)
        {
            uint ipAddr = 0;
            StringBuilder devname = new StringBuilder(100);
            StringBuilder macAdd = new StringBuilder(100);

            DevControl.TagErrorCode eCode = DevControl.DM_GetDeviceInfo(dev, ref ipAddr, macAdd, devname);
            if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                DevList.Add(new DeviceClass(dev, ipAddr, macAdd.ToString(), devname.ToString()));
            else
                Log.WriteError(ErrorHandling.GetErrorMsg(eCode));
        }

        private static IPAddress GetIPAddress(uint interIP)
        {
            return new IPAddress((uint)IPAddress.HostToNetworkOrder((int)interIP));
        }

        private void GetUid(IntPtr p, Int32 nEvt)
        {
            RFIDTag ce = (RFIDTag)Marshal.PtrToStructure(p, typeof(RFIDTag));
            Invoke((EventHandler)delegate
            {
                IntPtr hWnd = FindWindow(null, Text);
                if (hWnd != IntPtr.Zero)
                {
                    if (rb_mix.Checked)
                    {
                        int gnum = ce.PacketParam;
                        if (gnum < 0x80)
                        {
                            //EPC number
                            lastnum = gnum;
                            epcandtid = ce.UID;
                        }
                        else
                        {
                            // accompanying data
                            if (((lastnum & 0x3F) == ((gnum & 0x3F) - 1)) || ((lastnum & 0x3F) == 127 && ((gnum & 0x3F) == 0)))//相邻的滚码
                            {
                                epcandtid = epcandtid + "-" + ce.UID;
                                if (hWnd != IntPtr.Zero)
                                {
                                    int Antnum = ce.ANT;
                                    string str_ant = Convert.ToString(Antnum, 2).PadLeft(4, '0');
                                    string para = str_ant + "," + epcandtid + "," + ce.RSSI.ToString();
                                    SendMessage(hWnd, WM_MIXTAG, IntPtr.Zero, para);
                                }
                            }
                            else
                            {
                                epcandtid = "";
                            }
                        }
                    }
                    else if (rb_fastid.Checked)
                    {
                        int Antnum = ce.ANT;
                        string str_ant = Convert.ToString(Antnum, 2).PadLeft(4, '0');
                        string epclen = Convert.ToString(ce.LEN, 16);
                        if (epclen.Length == 1)
                            epclen = "0" + epclen;
                        string para = str_ant + "," + epclen + ce.UID + "," + ce.RSSI.ToString() + " ";
                        SendMessage(hWnd, WM_FASTID, IntPtr.Zero, para);
                    }
                    else
                    {
                        int Antnum = ce.ANT;
                        string str_ant = Convert.ToString(Antnum, 2).PadLeft(4, '0');
                        string epclen = Convert.ToString(ce.LEN, 16);
                        if (epclen.Length == 1)
                            epclen = "0" + epclen;
                        string para = str_ant + "," + epclen + ce.UID + "," + ce.RSSI.ToString() + " ";
                        SendMessage(hWnd, WM_SENDTAG, IntPtr.Zero, para);
                    }
                }
                total_tagnum++;
                CardNum++;
            });

        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_SENDTAG:
                    {
                        string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                        string str_ant = tagInfo.Substring(0, 4);
                        tagInfo = tagInfo.Substring(5);
                        int index = tagInfo.IndexOf(',');
                        string sEPC = tagInfo.Substring(0, index);
                        string str_epclen = sEPC.Substring(0, 2);
                        byte epclen = Convert.ToByte(str_epclen, 16);
                        sEPC = sEPC.Substring(2);
                        index++;
                        string RSSI = tagInfo.Substring(index);

                        if (!(dataGridView1.DataSource is DataTable dt))
                        {
                            dt = new DataTable();
                            dt.Columns.Add("Column1", Type.GetType("System.String"));
                            dt.Columns.Add("Column2", Type.GetType("System.String"));
                            dt.Columns.Add("Column3", Type.GetType("System.String"));
                            dt.Columns.Add("Column4", Type.GetType("System.String"));
                            dt.Columns.Add("Column5", Type.GetType("System.String"));
                            DataRow dr = dt.NewRow();
                            dr["Column1"] = (dt.Rows.Count + 1).ToString();
                            dr["Column2"] = sEPC;
                            dr["Column3"] = "1";
                            dr["Column4"] = RSSI;
                            dr["Column5"] = str_ant;
                            dt.Rows.Add(dr);
                            if (rb_fastid.Checked)
                            {
                                if ((epclen & 0x80) == 0)
                                {
                                    if (epclist.IndexOf(sEPC) == -1)
                                        epclist.Add(sEPC);
                                    lxLedControl1.Text = epclist.Count.ToString();
                                }
                                else
                                {
                                    int len = epclen & 0x7F;
                                    string myepc = sEPC.Substring(0, (len - 12) * 2);
                                    string mytid = sEPC.Substring((len - 12) * 2, 24);
                                    if (epclist.IndexOf(myepc) == -1)
                                        epclist.Add(myepc);
                                    if (tidlist.IndexOf(mytid) == -1)
                                        tidlist.Add(mytid);
                                    lxLedControl1.Text = epclist.Count.ToString();
                                    lxLedControl6.Text = tidlist.Count.ToString();

                                }
                            }
                            else if (rb_epc.Checked)
                                lxLedControl1.Text = dt.Rows.Count.ToString();
                            else if (rb_tid.Checked)
                                lxLedControl6.Text = dt.Rows.Count.ToString();

                            lxLedControl5.Text = dt.Rows.Count.ToString();
                            comboBox_EPC.Items.Add(sEPC);
                        }
                        else
                        {
                            DataRow[] dr;
                            dr = dt.Select("Column2='" + sEPC + "'");
                            if (dr.Length == 0)
                            {
                                DataRow dr2 = dt.NewRow();
                                dr2["Column1"] = (dt.Rows.Count + 1).ToString();
                                dr2["Column2"] = sEPC;
                                dr2["Column3"] = "1";
                                dr2["Column4"] = RSSI;
                                dr2["Column5"] = str_ant;
                                dt.Rows.Add(dr2);
                                if (rb_fastid.Checked)
                                {
                                    if ((epclen & 0x80) == 0)
                                    {
                                        // Only EPC
                                        if (epclist.IndexOf(sEPC) == -1)
                                            epclist.Add(sEPC);
                                        lxLedControl1.Text = epclist.Count.ToString();
                                    }
                                    else
                                    {
                                        int len = epclen & 0x7F;
                                        string myepc = sEPC.Substring(0, (len - 12) * 2);
                                        string mytid = sEPC.Substring((len - 12) * 2, 24);

                                        if (epclist.IndexOf(myepc) == -1)
                                            epclist.Add(myepc);
                                        if (tidlist.IndexOf(mytid) == -1)
                                            tidlist.Add(mytid);

                                        lxLedControl1.Text = epclist.Count.ToString();
                                        lxLedControl6.Text = tidlist.Count.ToString();

                                    }
                                }
                                else if (rb_epc.Checked)
                                    lxLedControl1.Text = dt.Rows.Count.ToString();
                                else if (rb_tid.Checked)
                                    lxLedControl6.Text = dt.Rows.Count.ToString();

                                lxLedControl5.Text = (Environment.TickCount - total_time).ToString();
                                comboBox_EPC.Items.Add(sEPC);
                            }
                            else     // epc number already exists
                            {
                                int cnt = int.Parse(dr[0]["Column3"].ToString());
                                cnt++;
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column3"] = cnt.ToString();
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column4"] = RSSI;
                                int ant1 = Convert.ToInt32(dr[0]["Column5"].ToString(), 2);
                                int ant2 = Convert.ToInt32(str_ant, 2);
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column5"] = Convert.ToString((ant1 | ant2), 2).PadLeft(4, '0');
                            }
                        }
                        bool flagset = (dataGridView1.DataSource == null);
                        dataGridView1.DataSource = dt;

                        if (flagset)
                        {
                            dataGridView1.Columns["Column1"].HeaderText = "NO.";
                            dataGridView1.Columns["Column1"].Width = 80;
                            dataGridView1.Columns["Column2"].HeaderText = "EPC";
                            dataGridView1.Columns["Column2"].Width = 300;
                            dataGridView1.Columns["Column3"].HeaderText = "Times";
                            dataGridView1.Columns["Column3"].Width = 80;
                            dataGridView1.Columns["Column4"].HeaderText = "RSSI";
                            dataGridView1.Columns["Column4"].Width = 80;
                            dataGridView1.Columns["Column5"].HeaderText = "Ant(4-1)";
                            dataGridView1.Columns["Column5"].Width = 100;
                        }
                        break;
                    }
                case WM_MIXTAG:
                    {
                        string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                        string sEPC, sData;
                        string str_ant = tagInfo.Substring(0, 4);
                        tagInfo = tagInfo.Substring(5);

                        int index = tagInfo.IndexOf(',');
                        sEPC = tagInfo.Substring(0, index);
                        int n = sEPC.IndexOf("-");
                        sData = sEPC.Substring(n + 1);
                        sEPC = sEPC.Substring(0, n);
                        index++;
                        string RSSI = tagInfo.Substring(index);

                        if (!(dataGridView1.DataSource is DataTable dt))
                        {
                            dt = new DataTable();
                            dt.Columns.Add("Column1", Type.GetType("System.String"));
                            dt.Columns.Add("Column2", Type.GetType("System.String"));
                            dt.Columns.Add("Column3", Type.GetType("System.String"));
                            dt.Columns.Add("Column4", Type.GetType("System.String"));
                            dt.Columns.Add("Column5", Type.GetType("System.String"));
                            dt.Columns.Add("Column6", Type.GetType("System.String"));
                            DataRow dr = dt.NewRow();
                            dr["Column1"] = (dt.Rows.Count + 1).ToString();
                            dr["Column2"] = sEPC;
                            dr["Column3"] = sData;
                            dr["Column4"] = "1";
                            dr["Column5"] = RSSI;
                            dr["Column6"] = str_ant;
                            dt.Rows.Add(dr);
                            lxLedControl1.Text = dt.Rows.Count.ToString();
                            lxLedControl5.Text = dt.Rows.Count.ToString();
                            comboBox_EPC.Items.Add(sEPC);
                        }
                        else
                        {
                            DataRow[] dr;
                            dr = dt.Select("Column2='" + sEPC + "'");
                            if (dr.Length == 0)
                            {
                                DataRow dr2 = dt.NewRow();
                                dr2["Column1"] = (dt.Rows.Count + 1).ToString();
                                dr2["Column2"] = sEPC;
                                dr2["Column3"] = sData;
                                dr2["Column4"] = "1";
                                dr2["Column5"] = RSSI;
                                dr2["Column6"] = str_ant;
                                dt.Rows.Add(dr2);
                                lxLedControl1.Text = dt.Rows.Count.ToString();
                                lxLedControl5.Text = (Environment.TickCount - total_time).ToString();
                                comboBox_EPC.Items.Add(sEPC);
                            }
                            else
                            {
                                int cnt = int.Parse(dr[0]["Column4"].ToString());
                                cnt++;
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column4"] = cnt.ToString();
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column5"] = RSSI;
                                int ant1 = Convert.ToInt32(dr[0]["Column6"].ToString(), 2);
                                int ant2 = Convert.ToInt32(str_ant, 2);
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column6"] = Convert.ToString((ant1 | ant2), 2).PadLeft(4, '0');
                            }
                        }
                        bool flagset = (dataGridView1.DataSource == null);
                        dataGridView1.DataSource = dt;

                        if (flagset)
                        {
                            dataGridView1.Columns["Column1"].HeaderText = "No.";
                            dataGridView1.Columns["Column1"].Width = 60;

                            dataGridView1.Columns["Column2"].HeaderText = "EPC";
                            dataGridView1.Columns["Column2"].Width = 280;

                            dataGridView1.Columns["Column3"].HeaderText = "Data";
                            dataGridView1.Columns["Column3"].Width = 150;

                            dataGridView1.Columns["Column4"].HeaderText = "Times";
                            dataGridView1.Columns["Column4"].Width = 60;

                            dataGridView1.Columns["Column5"].HeaderText = "RSSI";
                            dataGridView1.Columns["Column5"].Width = 60;

                            dataGridView1.Columns["Column6"].HeaderText = "Ant(4-1)";
                            dataGridView1.Columns["Column6"].Width = 60;
                        }
                        break;
                    }
                case WM_FASTID:
                    {
                        string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                        string sTID = "";
                        string str_ant = tagInfo.Substring(0, 4);
                        tagInfo = tagInfo.Substring(5);
                        int index = tagInfo.IndexOf(',');
                        string sEPC = tagInfo.Substring(0, index);
                        string str_epclen = sEPC.Substring(0, 2);
                        byte nlen = Convert.ToByte(str_epclen, 16);

                        if ((nlen & 0x80) == 0)
                        {
                            sEPC = sEPC.Substring(2);
                            if (epclist.IndexOf(sEPC) == -1)
                                epclist.Add(sEPC);
                            lxLedControl1.Text = epclist.Count.ToString();
                        }
                        else
                        {
                            int epclen = (nlen & 0x7F) - 12;
                            sTID = sEPC.Substring(2 + epclen * 2, 24);
                            sEPC = sEPC.Substring(2, epclen * 2);
                            if (epclist.IndexOf(sEPC) == -1)
                            {
                                epclist.Add(sEPC);
                            }
                            if (tidlist.IndexOf(sTID) == -1)
                            {
                                tidlist.Add(sTID);
                            }
                            lxLedControl1.Text = epclist.Count.ToString();
                            lxLedControl6.Text = tidlist.Count.ToString();

                        }
                        index++;
                        string RSSI = tagInfo.Substring(index);

                        if (!(dataGridView1.DataSource is DataTable dt))
                        {
                            dt = new DataTable();
                            dt.Columns.Add("Column1", Type.GetType("System.String"));
                            dt.Columns.Add("Column2", Type.GetType("System.String"));
                            dt.Columns.Add("Column3", Type.GetType("System.String"));
                            dt.Columns.Add("Column4", Type.GetType("System.String"));
                            dt.Columns.Add("Column5", Type.GetType("System.String"));
                            dt.Columns.Add("Column6", Type.GetType("System.String"));
                            DataRow dr = dt.NewRow();
                            dr["Column1"] = (dt.Rows.Count + 1).ToString();
                            dr["Column2"] = sEPC;
                            dr["Column3"] = sTID;
                            dr["Column4"] = "1";
                            dr["Column5"] = RSSI;
                            dr["Column6"] = str_ant;
                            dt.Rows.Add(dr);
                            lxLedControl5.Text = dt.Rows.Count.ToString();
                            comboBox_EPC.Items.Add(sEPC);
                        }
                        else
                        {
                            DataRow[] dr;
                            dr = dt.Select("Column2='" + sEPC + "' and Column3='" + sTID + "'");
                            if (dr.Length == 0)
                            {
                                // EPC number does not exist
                                DataRow dr2 = dt.NewRow();
                                dr2["Column1"] = (dt.Rows.Count + 1).ToString();
                                dr2["Column2"] = sEPC;
                                dr2["Column3"] = sTID;
                                dr2["Column4"] = "1";
                                dr2["Column5"] = RSSI;
                                dr2["Column6"] = str_ant;
                                dt.Rows.Add(dr2);
                                lxLedControl5.Text = (Environment.TickCount - total_time).ToString();
                                comboBox_EPC.Items.Add(sEPC);
                            }
                            else
                            {
                                // EPC number already exists
                                int cnt = int.Parse(dr[0]["Column4"].ToString());
                                cnt++;
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column4"] = cnt.ToString();
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column5"] = RSSI;
                                int ant1 = Convert.ToInt32(dr[0]["Column6"].ToString(), 2);
                                int ant2 = Convert.ToInt32(str_ant, 2);
                                dt.Rows[dt.Rows.IndexOf(dr[0])]["Column6"] = Convert.ToString((ant1 | ant2), 2).PadLeft(4, '0');
                            }
                        }
                        bool flagset = (dataGridView1.DataSource == null);
                        dataGridView1.DataSource = dt;

                        if (flagset)
                        {
                            dataGridView1.Columns["Column1"].HeaderText = "No.";
                            dataGridView1.Columns["Column1"].Width = 60;

                            dataGridView1.Columns["Column2"].HeaderText = "EPC";
                            dataGridView1.Columns["Column2"].Width = 280;

                            dataGridView1.Columns["Column3"].HeaderText = "Data";
                            dataGridView1.Columns["Column3"].Width = 150;

                            dataGridView1.Columns["Column4"].HeaderText = "Times";
                            dataGridView1.Columns["Column4"].Width = 60;

                            dataGridView1.Columns["Column5"].HeaderText = "RSSI";
                            dataGridView1.Columns["Column5"].Width = 60;

                            dataGridView1.Columns["Column6"].HeaderText = "Ant(4-1)";
                            dataGridView1.Columns["Column6"].Width = 60;
                        }
                        break;
                    }
                case WM_SENDTAGSTAT:
                    {
                        string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                        int index = tagInfo.IndexOf(',');
                        string tagRate = tagInfo.Substring(0, index);
                        index++;
                        string str = tagInfo.Substring(index);
                        index = str.IndexOf(',');
                        string tagNum = str.Substring(0, index);
                        index++;
                        string cmdTime = str.Substring(index);

                        lxLedControl2.Text = tagRate;
                        lxLedControl3.Text = cmdTime;
                        lxLedControl4.Text = tagNum;
                        break;
                    }
                case WM_SENDSTATU:
                    {
                        string Info = Marshal.PtrToStringAnsi(m.LParam);
                        fCmdRet = Convert.ToInt32(Info);
                        string strLog = "Inventory: " + GetReturnCodeDesc(fCmdRet);
                        WriteLog(lrtxtLog, strLog, 1);
                        break;
                    }
                case WM_SENDBUFF:
                    {
                        string tagInfo = Marshal.PtrToStringAnsi(m.LParam);
                        int index = tagInfo.IndexOf(',');
                        string tagNum = tagInfo.Substring(0, index);
                        index++;

                        string str = tagInfo.Substring(index);
                        index = str.IndexOf(',');
                        string cmdTime = str.Substring(0, index);
                        index++;

                        str = str.Substring(index);
                        index = str.IndexOf(',');
                        string tagRate = str.Substring(0, index);
                        index++;

                        str = str.Substring(index);
                        string total_tagnum = str;

                        lxLed_BNum.Text = tagNum;
                        lxLed_Bcmdsud.Text = tagRate;
                        lxLed_cmdTime.Text = cmdTime;
                        lxLed_Btoltag.Text = total_tagnum;
                        WriteLog(lrtxtLog, "Buffer-Inventiry:Operation success", 1);
                        break;
                    }
                case WM_SHOWNUM:
                    lxLedControl5.Text = (Environment.TickCount - total_time).ToString();
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        private void WriteLog(LogRichTextBox logRichTxt, string strLog, int nType)
        {
            if (this.InvokeRequired)
            {
                WriteLogUnSafe InvokeWriteLog = new WriteLogUnSafe(WriteLog);
                this.Invoke(InvokeWriteLog, new object[] { logRichTxt, strLog, nType });
            }
            else
            {

                if ((ckClearOperationRec.Checked) && (lrtxtLog.Lines.Length > 20))
                    lrtxtLog.Clear();
                if ((nType == 0) || (nType == 0x26) || (nType == 0x01) || (nType == 0x02) || (nType == 0xFB))
                {
                    logRichTxt.AppendTextEx(strLog, Color.Indigo);
                }
                else
                {
                    logRichTxt.AppendTextEx(strLog, Color.Red);
                }

                logRichTxt.Select(logRichTxt.TextLength, 0);
                logRichTxt.ScrollToCaret();
            }
        }

        #region HexStringToByteArray
        public static byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();

        }
        #endregion

        #region GetReturnCodeDesc
        private string GetReturnCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                case 0x26:
                    return "success";
                case 0x01:
                    return "Return before Inventory finished";
                case 0x02:
                    return "the Inventory-scan-time overflow";
                case 0x03:
                    return "More Data";
                case 0x04:
                    return "Reader module MCU is Full";
                case 0x05:
                    return "Access Password Error";
                case 0x09:
                    return "Destroy Password Error";
                case 0x0a:
                    return "Destroy Password Error Cannot be Zero";
                case 0x0b:
                    return "Tag Not Support the command";
                case 0x0c:
                    return "Use the commmand,Access Password Cannot be Zero";
                case 0x0d:
                    return "Tag is protected,cannot set it again";
                case 0x0e:
                    return "Tag is unprotected,no need to reset it";
                case 0x10:
                    return "There is some locked bytes,write fail";
                case 0x11:
                    return "can not lock it";
                case 0x12:
                    return "is locked,cannot lock it again";
                case 0x13:
                    return "Parameter Save Fail,Can Use Before Power";
                case 0x14:
                    return "Cannot adjust";
                case 0x15:
                    return "Return before Inventory finished";
                case 0x16:
                    return "Inventory-Scan-Time overflow";
                case 0x17:
                    return "More Data";
                case 0x18:
                    return "Reader module MCU is full";
                case 0x19:
                    return "'Not Support Command Or AccessPassword Cannot be Zero";
                case 0x1A:
                    return "Tag custom function error";
                case 0xF8:
                    return "Check antenna error";
                case 0xF9:
                    return "Command execute error";
                case 0xFA:
                    return "Get Tag,Poor Communication,Inoperable";
                case 0xFB:
                    return "No Tag Operable";
                case 0xFC:
                    return "Tag Return ErrorCode";
                case 0xFD:
                    return "Command length wrong";
                case 0xFE:
                    return "Illegal command";
                case 0xFF:
                    return "Parameter Error";
                case 0x30:
                    return "Communication error";
                case 0x31:
                    return "CRC checksummat error";
                case 0x32:
                    return "Return data length error";
                case 0x33:
                    return "Communication busy";
                case 0x34:
                    return "Busy,command is being executed";
                case 0x35:
                    return "ComPort Opened";
                case 0x36:
                    return "ComPort Closed";
                case 0x37:
                    return "Invalid Handle";
                case 0x38:
                    return "Invalid Port";
                case 0xEE:
                    return "Return Command Error";
                default:
                    return "";
            }
        }
        private string GetErrorCodeDesc(int cmdRet)
        {
            switch (cmdRet)
            {
                case 0x00:
                    return "Other error";
                case 0x03:
                    return "Memory out or pc not support";
                case 0x04:
                    return "Memory Locked and unwritable";
                case 0x0b:
                    return "No Power,memory write operation cannot be executed";
                case 0x0f:
                    return "Not Special Error,tag not support special errorcode";
                default:
                    return "";
            }
        }
        #endregion

        private void DisabledForm()
        {
            lxLedControl1.Text =
            lxLedControl2.Text =
            lxLedControl3.Text =
            lxLedControl4.Text =
            lxLedControl5.Text = "0";

            dataGridView1.DataSource = null;
            comboBox_EPC.Items.Clear();
            text_RDVersion.Text =
            TextBox_Serial.Text = "";

            btIventoryG2.Text = "Start";
            timer_answer.Enabled =
            panel1.Enabled =
            panel4.Enabled =
            panel5.Enabled =
            panel8.Enabled =
            panel9.Enabled =
            panel10.Enabled =
            gpb_address.Enabled =
            gpb_antconfig.Enabled =
            gpb_baud.Enabled =
            gpb_GPIO.Enabled =
            gpb_beep.Enabled =
            gpb_RDVersion.Enabled =
            gpb_checkant.Enabled =
            gpb_DBM.Enabled =
            gpb_Serial.Enabled =
            gpb_Relay.Enabled =
            gpb_OutputRep.Enabled =
            gpb_Freq.Enabled =
            gbp_buff.Enabled =
            btDefault.Enabled =
            btGetInformation.Enabled =
            group_maxtime.Enabled =
            gbp_wpower.Enabled =
            gbp_Retry.Enabled =
            gbp_DRM.Enabled =
            gbCmdTemperature.Enabled =
            gbReturnLoss.Enabled =
            btDefault.Enabled = false;
        }

        private void EnabledForm()
        {
            panel1.Enabled =
            panel4.Enabled =
            panel5.Enabled =
            panel8.Enabled =
            panel9.Enabled =
            panel10.Enabled =
            gpb_address.Enabled =
            gpb_antconfig.Enabled =
            gpb_baud.Enabled =
            gpb_GPIO.Enabled =
            gpb_beep.Enabled =
            gpb_RDVersion.Enabled =
            gpb_checkant.Enabled =
            gpb_DBM.Enabled =
            gpb_Serial.Enabled =
            gpb_Relay.Enabled =
            gpb_OutputRep.Enabled =
            gpb_Freq.Enabled =
            gbp_buff.Enabled =
            group_maxtime.Enabled =
            gbp_wpower.Enabled =
            gbp_Retry.Enabled =
            gbp_DRM.Enabled =
            gbCmdTemperature.Enabled =
            gbReturnLoss.Enabled =
            btGetInformation.Enabled =
            btDefault.Enabled = true;
        }

        private void RB_RS232_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_rs232.Checked)
            {
                if ((frmcomportindex > 0) && (frmcomportindex < 256))
                {
                    if (frmcomportindex > 0)
                        fCmdRet = RWDev.CloseNetPort(frmcomportindex);
                    if (fCmdRet == 0)
                    {
                        frmcomportindex = -1;
                        btConnectTcp.Enabled = true;
                        btDisConnectTcp.Enabled = false;
                        DisabledForm();
                        btConnectTcp.ForeColor = Color.Indigo;
                        btDisConnectTcp.ForeColor = Color.Black;
                        SetButtonBold(btConnectTcp);
                        SetButtonBold(btDisConnectTcp);
                    }
                    if (fCmdRet != 0)
                    {
                        WriteLog(lrtxtLog, "TCP close failed: " + GetReturnCodeDesc(fCmdRet), 1);
                        return;
                    }
                    else
                    {
                        WriteLog(lrtxtLog, "TCP close success", 0);
                    }
                }

                gpb_rs232.Enabled = true;
                btDisConnect232.Enabled = false;

                BtConnect232.ForeColor = Color.Indigo;
                SetButtonBold(BtConnect232);

                if (btConnectTcp.Font.Bold)
                    SetButtonBold(btConnectTcp);

                gpb_tcp.Enabled = false;
            }
        }

        private void RB_Tcp_CheckedChanged(object sender, EventArgs e)
        {
            if (rb_tcp.Checked)
            {
                if ((frmcomportindex > 0) && (frmcomportindex < 256))
                {
                    if (frmcomportindex > 0)
                        fCmdRet = RWDev.CloseSpecComPort(frmcomportindex);
                    if (fCmdRet == 0)
                    {
                        frmcomportindex = -1;
                        DisabledForm();
                        BtConnect232.Enabled = true;
                        btDisConnect232.Enabled = false;

                        BtConnect232.ForeColor = Color.Indigo;
                        btDisConnect232.ForeColor = Color.Black;
                        SetButtonBold(BtConnect232);
                        SetButtonBold(btDisConnect232);
                    }
                    if (fCmdRet != 0)
                    {
                        WriteLog(lrtxtLog, "COM close failed: " + GetReturnCodeDesc(fCmdRet), 1);
                        return;
                    }
                    else
                    {
                        WriteLog(lrtxtLog, "COM close success", 0);
                    }
                }
                gpb_tcp.Enabled = true;
                btDisConnectTcp.Enabled = false;

                btConnectTcp.ForeColor = Color.Indigo;
                if (BtConnect232.Font.Bold)
                    SetButtonBold(BtConnect232);

                SetButtonBold(btConnectTcp);
                gpb_rs232.Enabled = false;
            }
        }

        private void SetButtonBold(Button btnBold)
        {
            Font oldFont = btnBold.Font;
            Font newFont = new Font(oldFont, oldFont.Style ^ FontStyle.Bold);
            btnBold.Font = newFont;
        }

        private void BtConnect232_Click(object sender, EventArgs e)
        {
            int portNum = ComboBox_COM.SelectedIndex + 1;
            int FrmPortIndex = 0;
            string strException = string.Empty;
            fBaud = Convert.ToByte(ComboBox_baud2.SelectedIndex);
            if (fBaud > 2)
                fBaud = Convert.ToByte(fBaud + 2);
            fComAdr = 255; // Broadcast
            fCmdRet = RWDev.OpenComPort(portNum, ref fComAdr, fBaud, ref FrmPortIndex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Connect reader failed: " + GetReturnCodeDesc(fCmdRet), 1);
                return;
            }
            else
            {
                frmcomportindex = FrmPortIndex;
                WriteLog(lrtxtLog, "Connect: " + ComboBox_COM.Text + "@" + ComboBox_baud2.Text, 0);
            }

            EnabledForm();

            BtConnect232.Enabled = false;
            btDisConnect232.Enabled = true;

            BtConnect232.ForeColor = Color.Black;
            btDisConnect232.ForeColor = Color.Indigo;
            SetButtonBold(BtConnect232);
            SetButtonBold(btDisConnect232);
            BtGetInformation_Click(null, null);

            if (FrmPortIndex > 0)
                RWDev.InitRFIDCallBack(elegateRFIDCallBack, true, FrmPortIndex);
        }

        private void BtDisConnect232_Click(object sender, EventArgs e)
        {
            if (frmcomportindex > 0)
                fCmdRet = RWDev.CloseSpecComPort(frmcomportindex);
            if (fCmdRet == 0)
            {
                frmcomportindex = -1;
                DisabledForm();
                BtConnect232.Enabled = true;
                btDisConnect232.Enabled = false;

                BtConnect232.ForeColor = Color.Indigo;
                btDisConnect232.ForeColor = Color.Black;
                SetButtonBold(BtConnect232);
                SetButtonBold(btDisConnect232);
            }
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "COM close failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "COM close success", 0);
        }

        private void BtDisConnectTcp_Click(object sender, EventArgs e)
        {
            if (frmcomportindex > 0)
                fCmdRet = RWDev.CloseNetPort(frmcomportindex);
            if (fCmdRet == 0)
            {
                frmcomportindex = -1;
                btConnectTcp.Enabled = true;
                btDisConnectTcp.Enabled = false;
                DisabledForm();
                btConnectTcp.ForeColor = Color.Indigo;
                btDisConnectTcp.ForeColor = Color.Black;
                SetButtonBold(btConnectTcp);
                SetButtonBold(btDisConnectTcp);
            }
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "TCP close failed " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "TCP close success", 0);
        }

        private void BtConnectTcp_Click(object sender, EventArgs e)
        {
            try
            {
                string strException = string.Empty;
                string ipAddress = ipServerIP.IpAddressStr;
                int nPort = Convert.ToInt32(tb_Port.Text);
                fComAdr = 255;
                int FrmPortIndex = 0;
                fCmdRet = RWDev.OpenNetPort(nPort, ipAddress, ref fComAdr, ref FrmPortIndex);
                if (fCmdRet != 0)
                {
                    WriteLog(lrtxtLog, "Connect reader failed: " + GetReturnCodeDesc(fCmdRet), 1);
                    return;
                }
                else
                {
                    frmcomportindex = FrmPortIndex;
                    WriteLog(lrtxtLog, "Connect: " + ipAddress + "@" + nPort.ToString(), 0);
                }


                EnabledForm();
                btConnectTcp.Enabled = false;
                btDisConnectTcp.Enabled = true;

                btConnectTcp.ForeColor = Color.Black;
                btDisConnectTcp.ForeColor = Color.Indigo;
                SetButtonBold(btConnectTcp);
                SetButtonBold(btDisConnectTcp);
                BtGetInformation_Click(null, null);
                if (FrmPortIndex > 0)
                    RWDev.InitRFIDCallBack(elegateRFIDCallBack, true, FrmPortIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtFlashCl_Click(object sender, EventArgs e)
        {
            if (tabControl2.SelectedTab == tabPage_answer)
            {
                lxLedControl1.Text = "0";
                lxLedControl2.Text = "0";
                lxLedControl3.Text = "0";
                lxLedControl4.Text = "0";
                lxLedControl5.Text = "0";
                lxLedControl6.Text = "0";
                epclist.Clear();
                tidlist.Clear();
                dataGridView1.DataSource = null;
            }

            if (tabControl2.SelectedTab == tabPage_Buff)
            {
                lxLed_BNum.Text = "0";
                lxLed_Bcmdsud.Text = "0";
                lxLed_Btoltag.Text = "0";
                lxLed_Btoltime.Text = "0";
                lxLed_cmdTime.Text = "0";
                dataGridView3.Rows.Clear();
            }

            if (tabControl2.SelectedTab == tabPage_Realtime)
            {
                lxLed_Mtag.Text = "0";
                lxLed_Mtime.Text = "0";
                total_time = Environment.TickCount;
                dataGridView4.Rows.Clear();

            }

            if (Maintab.SelectedTab == Main_Page3)
            {
                text_R6B.Text = "";
                text_6BUID.Text = "";
                text_Statu6B.Text = "";
            }
            if (tabControl3.SelectedTab == tabPage8)
                stcprecv.Clear();

            if (tabControl3.SelectedTab == tabPage9)
            {
                ctctrecv.Clear();
                ctctsend.Text = "";
            }
            total_tagnum = 0;
            total_time = Environment.TickCount;
            comboBox_EPC.Items.Clear();
            lrtxtLog.Clear();

        }

        private void BtIventoryG2_Click(object sender, EventArgs e)
        {
            if (text_readadr.Text.Length != 4 ||
                text_readLen.Text.Length != 2 ||
                text_readpsd.Text.Length != 8)
            {
                MessageBox.Show("Mix inventory parameter error!!!");
                return;
            }

            if (btIventoryG2.Text == "Start")
            {
                if (rb_mix.Checked)
                {
                    ReadMem = (byte)com_MixMem.SelectedIndex;
                    ReadAdr = HexStringToByteArray(text_readadr.Text);
                    ReadLen = Convert.ToByte(text_readLen.Text, 16);
                    Psd = HexStringToByteArray(text_readpsd.Text);
                }

                lxLedControl1.Text =
                lxLedControl2.Text =
                lxLedControl3.Text =
                lxLedControl4.Text =
                lxLedControl5.Text =
                lxLedControl6.Text = "0";

                epclist.Clear();
                tidlist.Clear();
                dataGridView1.DataSource = null;
                lrtxtLog.Clear();
                comboBox_EPC.Items.Clear();
                text_epc.Text = "";
                AA_times = 0;
                Scantime = Convert.ToByte(com_scantime.SelectedIndex);
                if (checkBox_rate.Checked)
                    Qvalue = Convert.ToByte(com_Q.SelectedIndex | 0x80);
                else
                    Qvalue = Convert.ToByte(com_Q.SelectedIndex);


                Session = Convert.ToByte(com_S.SelectedIndex);
                if (Session == 4)
                    Session = 255;

                if (rb_epc.Checked)
                {
                    TIDFlag = 0;
                }
                else if (rb_fastid.Checked)
                {
                    TIDFlag = 0;
                    Qvalue = Convert.ToByte(com_Q.SelectedIndex | 0x20);
                }
                else if (rb_tid.Checked)
                {
                    TIDFlag = 1;
                    tidAddr = (byte)(Convert.ToInt32(text_readadr.Text, 16) & 0x00FF);
                    tidLen = Convert.ToByte(text_readLen.Text, 16);
                }

                total_tagnum = 0;
                targettimes = Convert.ToInt32(text_target.Text);
                total_time = Environment.TickCount;
                fIsInventoryScan = false;
                btIventoryG2.BackColor = Color.Indigo;
                btIventoryG2.Text = "Stop";
                Array.Clear(antlist, 0, 4);
                if (check_ant1.Checked)
                {
                    antlist[0] = 1;
                    InAnt = 0x80;
                }
                if (check_ant2.Checked)
                {
                    antlist[1] = 1;
                    InAnt = 0x81;
                }
                if (check_ant3.Checked)
                {
                    antlist[2] = 1;
                    InAnt = 0x82;
                }
                if (check_ant4.Checked)
                {
                    antlist[3] = 1;
                    InAnt = 0x83;
                }
                Target = (byte)com_Target.SelectedIndex;
                toStopThread = false;
                if (fIsInventoryScan == false)
                {
                    mythread = new Thread(new ThreadStart(Inventory))
                    {
                        IsBackground = true
                    };
                    mythread.Start();
                    timer_answer.Enabled = true;
                }
                rb_mix.Enabled = false;
                rb_epc.Enabled = false;
                rb_tid.Enabled = false;
                rb_fastid.Enabled = false;
            }
            else
            {
                toStopThread = true;
                btIventoryG2.Enabled = false;
                btIventoryG2.BackColor = Color.Transparent;
                btIventoryG2.Text = "Stoping";
            }
        }

        #region Flash_G2
        private void Flash_G2()
        {
            byte Ant = 0;
            int TagNum = 0;
            int Totallen = 0;
            byte[] EPC = new byte[50000];
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            byte MaskFlag = 0;
            int cbtime = Environment.TickCount;
            CardNum = 0;
            fCmdRet = RWDev.Inventory_G2(ref fComAdr, Qvalue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, tidAddr, tidLen, TIDFlag, Target, InAnt, Scantime, FastFlag, EPC, ref Ant, ref Totallen, ref TagNum, frmcomportindex);
            int cmdTime = Environment.TickCount - cbtime;//命令时间
            if ((fCmdRet != 0x01) && (fCmdRet != 0x02) && (fCmdRet != 0xF8) && (fCmdRet != 0xF9) && (fCmdRet != 0xEE) && (fCmdRet != 0xFF) && (fCmdRet != 0xFE))
            {
                if (rb_tcp.Checked)
                {
                    if ((frmcomportindex > 0) && (frmcomportindex < 256))
                    {
                        fCmdRet = RWDev.CloseNetPort(frmcomportindex);
                        if (fCmdRet == 0) frmcomportindex = -1;
                        Thread.Sleep(1000);
                    }
                    fComAdr = 255;
                    string ipAddress = ipServerIP.IpAddressStr;
                    int nPort = Convert.ToInt32(tb_Port.Text);
                    fCmdRet = RWDev.OpenNetPort(nPort, ipAddress, ref fComAdr, ref frmcomportindex);
                    if (fCmdRet == 0)
                    {
                        if (frmcomportindex > 0)
                            RWDev.InitRFIDCallBack(elegateRFIDCallBack, true, frmcomportindex);
                    }
                }
            }
            if (fCmdRet == 0x30)
                CardNum = 0;

            if (CardNum == 0)
            {
                if (Session > 1)
                {
                    // Not getting the label, only updating the status bar
                    AA_times++;
                }
            }
            else
                AA_times = 0;

            IntPtr hWnd;
            if (fCmdRet == 1 ||
                fCmdRet == 2 ||
                fCmdRet == 0xFB ||
                fCmdRet == 0x26)
            {
                if (cmdTime > CommunicationTime)
                {
                    // Subtract the communication time equal to the actual time of the tag
                    cmdTime -= CommunicationTime;
                }
                if (cmdTime > 0)
                {
                    // The speed is equal to the number of pictures/time
                    int tagrate = (CardNum * 1000) / cmdTime;
                    hWnd = FindWindow(null, Text);
                    if (hWnd != IntPtr.Zero)
                    {
                        string para = tagrate.ToString() + "," + total_tagnum.ToString() + "," + cmdTime.ToString();
                        SendMessage(hWnd, WM_SENDTAGSTAT, IntPtr.Zero, para);
                    }
                }
            }

            hWnd = FindWindow(null, Text);
            if (hWnd != IntPtr.Zero)
                SendMessage(hWnd, WM_SENDSTATU, IntPtr.Zero, fCmdRet.ToString());
        }
        #endregion

        #region FlashMix_G2
        private void FlashMix_G2()
        {
            byte Ant = 0;
            int TagNum = 0;
            int Totallen = 0;
            byte[] EPC = new byte[50000];
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            byte MaskFlag = 0;
            int cbtime = Environment.TickCount;
            CardNum = 0;
            fCmdRet = RWDev.InventoryMix_G2(ref fComAdr, Qvalue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, ReadMem, ReadAdr, ReadLen, Psd, Target, InAnt, Scantime, FastFlag, EPC, ref Ant, ref Totallen, ref TagNum, frmcomportindex);
            int cmdTime = Environment.TickCount - cbtime;
            if ((fCmdRet != 0x01) && (fCmdRet != 0x02) && (fCmdRet != 0xF8) && (fCmdRet != 0xF9) && (fCmdRet != 0xEE) && (fCmdRet != 0xFF) && (fCmdRet != 0xFE))
            {
                if (rb_tcp.Checked)
                {
                    if ((frmcomportindex > 0) && (frmcomportindex < 256))
                    {
                        fCmdRet = RWDev.CloseNetPort(frmcomportindex);
                        if (fCmdRet == 0) frmcomportindex = -1;
                        Thread.Sleep(1000);
                    }
                    fComAdr = 255;
                    string ipAddress = ipServerIP.IpAddressStr;
                    int nPort = Convert.ToInt32(tb_Port.Text);
                    fCmdRet = RWDev.OpenNetPort(nPort, ipAddress, ref fComAdr, ref frmcomportindex);
                    if (fCmdRet == 0)
                    {
                        if (frmcomportindex > 0)
                            RWDev.InitRFIDCallBack(elegateRFIDCallBack, true, frmcomportindex);
                    }
                }
            }
            if (CardNum == 0)
            {
                if (Session > 1)
                    AA_times++; // Not getting the label, only updating the status bar
            }
            else
                AA_times = 0;

            IntPtr hWnd;

            if (fCmdRet == 1 ||
                fCmdRet == 2 ||
                fCmdRet == 0xFB ||
                fCmdRet == 0x26)
            {
                if (cmdTime > CommunicationTime)
                {
                    // Subtract the communication time equal to the actual time of the tag
                    cmdTime -= CommunicationTime;
                }
                if (cmdTime > 0)
                {
                    // The speed is equal to the number of pictures/time
                    int tagrate = (CardNum * 1000) / cmdTime;
                    hWnd = FindWindow(null, Text);
                    if (hWnd != IntPtr.Zero)
                    {
                        string para = tagrate.ToString() + "," + total_tagnum.ToString() + "," + cmdTime.ToString();
                        SendMessage(hWnd, WM_SENDTAGSTAT, IntPtr.Zero, para);
                    }
                }

            }
            hWnd = FindWindow(null, Text);
            if (hWnd != IntPtr.Zero)
            {
                string para = fCmdRet.ToString();
                SendMessage(hWnd, WM_SENDSTATU, IntPtr.Zero, para);
            }
        }
        #endregion

        private void Inventory()
        {
            fIsInventoryScan = true;
            while (!toStopThread)
            {
                if (Session == 255)
                {
                    FastFlag = 0;
                    if (rb_mix.Checked)
                        FlashMix_G2();
                    else
                        Flash_G2();

                }
                else
                {
                    for (int m = 0; m < 4; m++)
                    {
                        switch (m)
                        {
                            case 0:
                                InAnt = 0x80;
                                break;
                            case 1:
                                InAnt = 0x81;
                                break;
                            case 2:
                                InAnt = 0x82;
                                break;
                            case 3:
                                InAnt = 0x83;
                                break;
                        }
                        FastFlag = 1;
                        if (antlist[m] == 1)
                        {
                            if (Session > 1) // s2,s3
                            {
                                if ((check_num.Checked) && (AA_times + 1 > targettimes))
                                {
                                    // If the card is not read for 2 consecutive times,
                                    // the A/B status switches.
                                    Target = Convert.ToByte(1 - Target);
                                    AA_times = 0;
                                }
                            }
                            if (rb_mix.Checked)
                                FlashMix_G2();
                            else
                                Flash_G2();
                        }
                    }
                }
                Thread.Sleep(5);
            }
            Invoke((EventHandler)delegate
            {

                if (fIsInventoryScan)
                {
                    toStopThread = true;
                    btIventoryG2.Text = "Start";
                    mythread.Abort();
                    timer_answer.Enabled = false;
                    fIsInventoryScan = false;
                }

                timer_answer.Enabled = false;
                fIsInventoryScan = false;
                rb_mix.Enabled = true;
                rb_epc.Enabled = true;
                rb_tid.Enabled = true;
                rb_fastid.Enabled = true;
                btIventoryG2.Enabled = true;
            });

        }

        private void Timer_answer_Tick(object sender, EventArgs e)
        {
            IntPtr hWnd = FindWindow(null, Text);

            // Check whether the current statistics window is open
            if (hWnd != IntPtr.Zero)
                SendMessage(hWnd, WM_SHOWNUM, IntPtr.Zero, fCmdRet.ToString());
        }

        private void RB_Chinese_band2_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (int i = 0; i < 20; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(920.125 + i * 0.25) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(920.125 + i * 0.25));
            }
            ComboBox_dmaxfre.SelectedIndex = 19;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 10;
        }

        private void RB_US_band_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (int i = 0; i < 50; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902.75 + i * 0.5) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(902.75 + i * 0.5));
            }
            ComboBox_dmaxfre.SelectedIndex = 49;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 25;
        }

        private void RB_Korean_band_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (int i = 0; i < 32; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(917.1 + i * 0.2) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(917.1 + i * 0.2));
            }
            ComboBox_dmaxfre.SelectedIndex = 31;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 16;
        }

        private void RB_EU_band_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox_dminfre.Items.Clear();
            ComboBox_dmaxfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (int i = 0; i < 15; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(865.1 + i * 0.2) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(865.1 + i * 0.2));
            }
            ComboBox_dmaxfre.SelectedIndex = 14;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 7;
        }

        private void CheckBox_SameFre_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBox_SameFre.Checked)
                ComboBox_dmaxfre.SelectedIndex = ComboBox_dminfre.SelectedIndex;
        }

        private bool CheckCRC(string s)
        {
            int current_crc_value;
            byte crcL, crcH;
            byte[] data = HexStringToByteArray(s);
            current_crc_value = 0xFFFF;
            for (int i = 0; i <= (data.Length - 1); i++)
            {
                current_crc_value ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    current_crc_value =
                        ((current_crc_value & 0x01) != 0)
                        ? (current_crc_value >> 1) ^ 0x8408
                        : (current_crc_value >> 1);
                }
            }
            crcL = Convert.ToByte(current_crc_value & 0xFF);
            crcH = Convert.ToByte((current_crc_value >> 8) & 0xFF);
            return (crcH == 0 && crcL == 0);
        }

        private void Maintab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
            {
                toStopThread = true;
                btIventoryG2.Enabled = false;
                btIventoryG2.BackColor = Color.Transparent;
                btIventoryG2.Text = "Start";
            }
            if (fIsBuffScan)
            {
                toStopThread = true;// Flag, the thread that receives the data determines that stop is true,
                                    // and will automatically exit the thread under normal circumstances.
                ReadThread.Abort(); // If the thread cannot exit, it will be forced to terminate.
                timer_Buff.Enabled = false;
                fIsInventoryScan = false;
            }

            timer_answer.Enabled =
            timer_EAS.Enabled =
            Timer_Test_6B.Enabled =
            timer_Buff.Enabled =
            timer_RealTime.Enabled =
            pictureBox2.Visible = false;

            btCheckEASAlarm.Text = "Detect";

            btIventoryG2.Text =
            btStartBuff.Text =
            btStartMactive.Text = "Start";

            btIventoryG2.BackColor = Color.Transparent;
            btStartBuff.BackColor = Color.Transparent;
            btStartMactive.BackColor = Color.Transparent;
            btCheckEASAlarm.BackColor = Color.Transparent;
            btInventory6B.Text = "Start";
            btInventory6B.BackColor = Color.Transparent;

            if (comboBox_EPC.Text == "" && comboBox_EPC.Items.Count > 0)
                comboBox_EPC.SelectedIndex = 0;

            if ((ReadTypes == "16") || (ReadTypes == "21"))
            {
                group_ant1.Enabled = false;
                check_ant1.Checked = true;
                check_ant2.Checked = false;
                check_ant3.Checked = false;
                check_ant4.Checked = false;
            }
            else
                group_ant1.Enabled = (com_S.SelectedIndex < 4);
        }

        private void BtClearBuffer_Click(object sender, EventArgs e)
        {
            fCmdRet = RWDev.ClearTagBuffer(ref fComAdr, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Clear data failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Clear datda success", 0);
        }

        private void TB_Port_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }

        private void Text_Address_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ("0123456789ABCDEF".IndexOf(Char.ToUpper(e.KeyChar)) < 0);
        }

        private void BtGetInformation_Click(object sender, EventArgs e)
        {
            byte TrType = 0;
            byte[] VersionInfo = new byte[2];
            byte ReaderType = 0;
            byte ScanTime = 0;
            byte dmaxfre = 0;
            byte dminfre = 0;
            byte powerdBm = 0;
            byte FreBand;
            byte Ant = 0;
            byte BeepEn = 0;
            byte OutputRep = 0;
            byte CheckAnt = 0;
            text_RDVersion.Text = "";

            int ctime = Environment.TickCount;
            fCmdRet = RWDev.GetReaderInformation(ref fComAdr, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref Ant, ref BeepEn, ref OutputRep, ref CheckAnt, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Get Reader Information failed:" + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                CommunicationTime = Environment.TickCount - ctime;
                comboBox_maxtime.SelectedIndex = ScanTime;
                switch (ReaderType)
                {
                    case 0x20: // 2882
                        ReadTypes = "20";
                        check_int2.Visible = true;
                        gpb_checkant.Enabled = true;
                        gpb_antconfig.Enabled = true;
                        if (tabControl1.TabPages.IndexOf(tabPage_Module) == -1)
                            tabControl1.TabPages.Add(tabPage_Module);
                        if (tabControl2.TabPages.IndexOf(tabPage_Realtime) == -1)
                            tabControl2.TabPages.Add(tabPage_Realtime);
                        gbp_buff.Enabled = true;
                        if (tabControl2.TabPages.IndexOf(tabPage_Buff) == -1)
                            tabControl2.TabPages.Add(tabPage_Buff);
                        text_RDVersion.Text = "UHFReader288MP--" + Convert.ToString(VersionInfo[0], 10).PadLeft(2, '0') + "." + Convert.ToString(VersionInfo[1], 10).PadLeft(2, '0');
                        break;

                    case 0x21:// 9882M
                        ReadTypes = "21";
                        check_int2.Visible = true;
                        gpb_checkant.Enabled = false;
                        gpb_antconfig.Enabled = false;
                        tabControl1.TabPages.Remove(tabPage_Module);
                        tabControl2.TabPages.Remove(tabPage_Realtime);
                        gbp_buff.Enabled = false;
                        tabControl2.TabPages.Remove(tabPage_Buff);
                        text_RDVersion.Text = "UHFReader82--" + Convert.ToString(VersionInfo[0], 10).PadLeft(2, '0') + "." + Convert.ToString(VersionInfo[1], 10).PadLeft(2, '0');
                        break;
                    default:
                        text_RDVersion.Text = "0x" + Convert.ToString(ReaderType, 16).PadLeft(2, '0').ToUpper() + " -- " + Convert.ToString(VersionInfo[0], 10).PadLeft(2, '0') + "." + Convert.ToString(VersionInfo[1], 10).PadLeft(2, '0');
                        break;
                }

                ComboBox_PowerDbm.SelectedIndex = Convert.ToInt32(powerdBm);
                text_address.Text = Convert.ToString(fComAdr, 16).PadLeft(2, '0');
                FreBand = Convert.ToByte(((dmaxfre & 0xc0) >> 4) | (dminfre >> 6));
                switch (FreBand)
                {
                    case 1:
                        radioButton_band1.Checked = true;
                        fdminfre = 920.125 + (dminfre & 0x3F) * 0.25;
                        fdmaxfre = 920.125 + (dmaxfre & 0x3F) * 0.25;
                        break;
                    case 2:
                        radioButton_band2.Checked = true;
                        fdminfre = 902.75 + (dminfre & 0x3F) * 0.5;
                        fdmaxfre = 902.75 + (dmaxfre & 0x3F) * 0.5;
                        break;
                    case 3:
                        radioButton_band3.Checked = true;
                        fdminfre = 917.1 + (dminfre & 0x3F) * 0.2;
                        fdmaxfre = 917.1 + (dmaxfre & 0x3F) * 0.2;
                        break;
                    case 4:
                        radioButton_band4.Checked = true;
                        fdminfre = 865.1 + (dminfre & 0x3F) * 0.2;
                        fdmaxfre = 865.1 + (dmaxfre & 0x3F) * 0.2;
                        break;
                    case 8:
                        radioButton_band8.Checked = true;
                        fdminfre = 840.125 + (dminfre & 0x3F) * 0.25;
                        fdmaxfre = 840.125 + (dmaxfre & 0x3F) * 0.25;
                        break;
                    case 12:
                        radioButton_band12.Checked = true;
                        fdminfre = 902 + (dminfre & 0x3F) * 0.5;
                        fdmaxfre = 902 + (dmaxfre & 0x3F) * 0.5;
                        break;
                }

                if (fdmaxfre != fdminfre)
                    CheckBox_SameFre.Checked = false;

                ComboBox_dminfre.SelectedIndex = dminfre & 0x3F;
                ComboBox_dmaxfre.SelectedIndex = dmaxfre & 0x3F;
                switch (BeepEn)
                {
                    case 1:
                        Radio_beepEn.Checked = true;
                        break;
                    case 0:
                        Radio_beepDis.Checked = true;
                        break;
                }

                check_ant1.Checked =
                checkant1.Checked = ((Ant & 0x01) == 1);

                check_ant2.Checked =
                checkant2.Checked = ((Ant & 0x02) == 2);

                check_ant3.Checked =
                checkant3.Checked = ((Ant & 0x04) == 4);

                check_ant4.Checked =
                checkant4.Checked = ((Ant & 0x08) == 8);

                check_OutputRep1.Checked = ((OutputRep & 0x01) == 1);
                check_OutputRep2.Checked = ((OutputRep & 0x02) == 2);
                check_OutputRep3.Checked = ((OutputRep & 0x04) == 4);
                check_OutputRep4.Checked = ((OutputRep & 0x08) == 8);

                if (CheckAnt == 0)
                    rb_Closecheckant.Checked = true;
                else
                    rb_Opencheckant.Checked = true;

                WriteLog(lrtxtLog, "Get Reader Information success", 0);
            }
        }

        private void BtDefault_Click(object sender, EventArgs e)
        {
            byte aNewComAdr, powerDbm, dminfre, dmaxfre, scantime;
            dminfre = 128;
            dmaxfre = 49;
            aNewComAdr = 0x00;
            powerDbm = 30;
            fBaud = 5;
            scantime = 0;
            ComboBox_baud.SelectedIndex = 3;
            fCmdRet = RWDev.SetAddress(ref fComAdr, aNewComAdr, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set Reader address failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set Reader address success", 0);

            fCmdRet = RWDev.SetRfPower(ref fComAdr, powerDbm, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set Power failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set power success ", 0);

            fCmdRet = RWDev.SetRegion(ref fComAdr, dmaxfre, dminfre, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set Region failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set Region success", 0);

            fCmdRet = RWDev.SetBaudRate(ref fComAdr, fBaud, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set baud rate failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set baud rate success ", 0);

            fCmdRet = RWDev.SetInventoryScanTime(ref fComAdr, scantime, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set inventory scan time failed:： " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set inventory scan time success", 0);
            BtGetInformation_Click(null, null);
        }

        private void BtAddress_Click(object sender, EventArgs e)
        {
            byte aNewComAdr = Convert.ToByte(text_address.Text, 16);
            fCmdRet = RWDev.SetAddress(ref fComAdr, aNewComAdr, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set reader address failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set reader address success ", 0);
        }

        private void BtFreq_Click(object sender, EventArgs e)
        {
            byte dminfre, dmaxfre;
            int band = 2;
            if (radioButton_band1.Checked)
                band = 1;
            if (radioButton_band2.Checked)
                band = 2;
            if (radioButton_band3.Checked)
                band = 3;
            if (radioButton_band4.Checked)
                band = 4;
            if (radioButton_band8.Checked)
                band = 8;
            if (radioButton_band12.Checked)
                band = 12;
            dminfre = Convert.ToByte(((band & 3) << 6) | (ComboBox_dminfre.SelectedIndex & 0x3F));
            dmaxfre = Convert.ToByte(((band & 0x0c) << 4) | (ComboBox_dmaxfre.SelectedIndex & 0x3F));
            fCmdRet = RWDev.SetRegion(ref fComAdr, dmaxfre, dminfre, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set region failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set region success", 0);
        }

        private void BT_DBM_Click(object sender, EventArgs e)
        {
            byte powerDbm = (byte)ComboBox_PowerDbm.SelectedIndex;
            fCmdRet = RWDev.SetRfPower(ref fComAdr, powerDbm, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set power failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
            {
                WriteLog(lrtxtLog, "Set power success", 0);
            }
        }

        private void BtBaudRate_Click(object sender, EventArgs e)
        {
            byte fBaud = (byte)ComboBox_baud.SelectedIndex;
            if (fBaud > 2)
                fBaud = (byte)(fBaud + 2);
            fCmdRet = RWDev.SetBaudRate(ref fComAdr, fBaud, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set baud rate failed " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set baud rate success ", 0);
        }

        private void BtSerial_Click(object sender, EventArgs e)
        {
            byte[] SeriaNo = new byte[4];
            TextBox_Serial.Text = "";
            fCmdRet = RWDev.GetSeriaNo(ref fComAdr, SeriaNo, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Get serial number failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
            {
                TextBox_Serial.Text = ByteArrayToHexString(SeriaNo);
                WriteLog(lrtxtLog, "Get serial number success ", 0);
            }
        }

        private void Button_Beep_Click(object sender, EventArgs e)
        {
            byte BeepEn = (Radio_beepEn.Checked) ? (byte)1 : (byte)0;
            fCmdRet = RWDev.SetBeepNotification(ref fComAdr, BeepEn, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set beep failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set beep success", 0);
        }

        private void BtRelay_Click(object sender, EventArgs e)
        {
            byte RelayTime = Convert.ToByte(ComboBox_RelayTime.SelectedIndex + 1);
            fCmdRet = RWDev.SetRelay(ref fComAdr, RelayTime, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set relay failed:" + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set relay success", 0);
        }

        private void BtOutputRep_Click(object sender, EventArgs e)
        {
            byte OutputRep = 0;
            if (check_OutputRep1.Checked)
                OutputRep = Convert.ToByte(OutputRep | 0x01);
            if (check_OutputRep2.Checked)
                OutputRep = Convert.ToByte(OutputRep | 0x02);
            if (check_OutputRep3.Checked)
                OutputRep = Convert.ToByte(OutputRep | 0x04);
            if (check_OutputRep3.Checked)
                OutputRep = Convert.ToByte(OutputRep | 0x08);
            fCmdRet = RWDev.SetNotificationPulseOutput(ref fComAdr, OutputRep, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set notification pulse output failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set notification pulse output success ", 0);
        }

        private void Button_SetGPIO_Click(object sender, EventArgs e)
        {
            byte OutputPin = 0;
            if (check_out1.Checked)
                OutputPin = Convert.ToByte(OutputPin | 0x01);
            if (check_out2.Checked)
                OutputPin = Convert.ToByte(OutputPin | 0x02);

            fCmdRet = RWDev.SetGPIO(ref fComAdr, OutputPin, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set GPIO failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set GPIO success", 0);
        }

        private void Button_GetGPIO_Click(object sender, EventArgs e)
        {
            byte OutputPin = 0;
            fCmdRet = RWDev.GetGPIOStatus(ref fComAdr, ref OutputPin, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Get GPIO failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                check_out1.Checked = ((OutputPin & 0x10) == 0x10);
                check_out2.Checked = ((OutputPin & 0x20) == 0x20);
                check_int1.Checked = ((OutputPin & 0x01) == 1);
                check_int2.Checked = ((OutputPin & 0x02) == 2);
                WriteLog(lrtxtLog, "Get GPIO success ", 0);
            }
        }

        private void BtSetcheckant_Click(object sender, EventArgs e)
        {
            byte CheckAnt = (rb_Opencheckant.Checked) ? (byte)1 : (byte)0;
            fCmdRet = RWDev.SetCheckAnt(ref fComAdr, CheckAnt, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set antenna check failed: " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set antenna check success", 0);
        }

        private void Button_Ant_Click(object sender, EventArgs e)
        {
            byte ANT = 0;
            if (checkant1.Checked) ANT = Convert.ToByte(ANT | 1);
            if (checkant2.Checked) ANT = Convert.ToByte(ANT | 2);
            if (checkant3.Checked) ANT = Convert.ToByte(ANT | 4);
            if (checkant4.Checked) ANT = Convert.ToByte(ANT | 8);

            byte ANT1 = ANT;
            fCmdRet = RWDev.SetAntennaMultiplexing(ref fComAdr, ANT, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Antenna config failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                check_ant1.Checked = ((ANT1 & 0x01) == 1);
                check_ant2.Checked = ((ANT1 & 0x02) == 2);
                check_ant3.Checked = ((ANT1 & 0x04) == 4);
                check_ant4.Checked = ((ANT1 & 0x08) == 8);
                WriteLog(lrtxtLog, "Antenna config success", 0);
            }
        }

        private void BtSelectTag_Click(object sender, EventArgs e)
        {
            text_epc.Text = comboBox_EPC.Text;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            maskadr_textbox.Enabled =
            maskLen_textBox.Enabled =
            maskData_textBox.Enabled =
            R_EPC.Enabled =
            R_TID.Enabled =
            R_User.Enabled = checkBox1.Checked;
        }

        private void BtRead_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Mem = 0;
            string str = ""; ;
            byte[] CardData = new byte[320];
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            text_WriteData.Text = "";
            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text == "") || (maskLen_textBox.Text == "") || (maskData_textBox.Text == ""))
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked)
                    MaskMem = 1;
                if (R_TID.Checked)
                    MaskMem = 2;
                if (R_User.Checked)
                    MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;

            if (text_WordPtr.Text == "" ||
                text_RWlen.Text == "" ||
                text_AccessCode2.Text.Length != 8)
            {
                return;
            }
            WordPtr = (byte)Convert.ToInt32(text_WordPtr.Text, 16);
            byte Num = Convert.ToByte(text_RWlen.Text);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            for (int p = 0; p < 10; p++)
            {
                fCmdRet = RWDev.ReadData_G2(ref fComAdr, EPC, ENum, Mem, WordPtr, Num, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, CardData, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0) break;
            }
            if (fCmdRet != 0)
            {

                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Read failed " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Read failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                byte[] daw = new byte[Num * 2];
                Array.Copy(CardData, daw, Num * 2);
                text_WriteData.Text = ByteArrayToHexString(daw);
                string strLog = "Read success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Mem = 0;
            string str = ""; ;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text == "") ||
                    (maskLen_textBox.Text == "") ||
                    (maskData_textBox.Text == ""))
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;

            if (text_WordPtr.Text == "" ||
                text_AccessCode2.Text.Length != 8)
                return;

            string epcstr = text_WriteData.Text;
            if (epcstr.Length % 4 != 0 ||
                epcstr.Length == 0)
            {
                MessageBox.Show("Input data by word.", "Write");
                return;
            }
            byte WNum = Convert.ToByte(epcstr.Length / 4);
            byte[] Writedata = HexStringToByteArray(epcstr);
            WordPtr = (byte)Convert.ToInt32(text_WordPtr.Text, 16);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            if ((checkBox_pc.Checked) && (C_EPC.Checked))
            {
                WordPtr = 1;
                WNum = Convert.ToByte(epcstr.Length / 4 + 1);
                Writedata = HexStringToByteArray(textBox_pc.Text + epcstr);
            }
            for (int p = 0; p < 10; p++)
            {
                fCmdRet = RWDev.WriteData_G2(ref fComAdr, EPC, WNum, ENum, Mem, WordPtr, Writedata, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0) break;
            }
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Write failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Write failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Write success", 0);
        }

        private void CheckBox_pc_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_pc.Checked && C_EPC.Checked)
            {
                text_WordPtr.Text = "0002";
                text_WordPtr.ReadOnly = true;
                int m, n;
                n = text_WriteData.Text.Length;
                if (n % 4 == 0)
                {
                    m = n / 4;
                    m = (m & 0x3F) << 3;
                    textBox_pc.Text = Convert.ToString(m, 16).PadLeft(2, '0') + "00";
                }
            }
            else
                text_WordPtr.ReadOnly = false;
        }

        private void Text_WriteData_TextChanged(object sender, EventArgs e)
        {
            int m, n;
            n = text_WriteData.Text.Length;
            if ((checkBox_pc.Checked) && (n % 4 == 0) && (C_EPC.Checked))
            {
                m = n / 4;
                m = (m & 0x3F) << 3;
                textBox_pc.Text = Convert.ToString(m, 16).PadLeft(2, '0') + "00";
            }
        }

        private void C_EPC_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_pc.Checked)
            {
                text_WordPtr.Text = "0002";
                text_WordPtr.ReadOnly = true;
            }
            else
                text_WordPtr.ReadOnly = false;
        }

        private void BtBlockWrite_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Mem = 0;
            string str = ""; ;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text == "") ||
                    (maskLen_textBox.Text == "") ||
                    (maskData_textBox.Text == ""))
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;

            if (text_WordPtr.Text == "" ||
                text_AccessCode2.Text.Length != 8)
                return;

            string epcstr = text_WriteData.Text;
            if (epcstr.Length % 4 != 0 ||
                epcstr.Length == 0)
            {
                MessageBox.Show("Input data by word.", "Write");
                return;
            }
            byte WNum = Convert.ToByte(epcstr.Length / 4);
            byte[] Writedata = HexStringToByteArray(epcstr);
            WordPtr = (byte)Convert.ToInt32(text_WordPtr.Text, 16);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            if ((checkBox_pc.Checked) && (C_EPC.Checked))
            {
                WordPtr = 1;
                WNum = Convert.ToByte(epcstr.Length / 4 + 1);
                Writedata = HexStringToByteArray(textBox_pc.Text + epcstr);
            }
            for (int p = 0; p < 10; p++)
            {
                fCmdRet = RWDev.BlockWrite_G2(ref fComAdr, EPC, WNum, ENum, Mem, WordPtr, Writedata, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0)
                    break;
            }
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Block write failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Block write failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Block write success", 0);
        }

        private void BtBlockErase_Click(object sender, EventArgs e)
        {
            byte WordPtr, ENum;
            byte Mem = 0;
            string str = ""; ;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            text_WriteData.Text = "";
            if (checkBox1.Checked)
            {
                if (maskadr_textbox.Text == "" ||
                    maskLen_textBox.Text == "" ||
                    maskData_textBox.Text == "")
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;

            if (text_WordPtr.Text == "" ||
                text_RWlen.Text == "" ||
                text_AccessCode2.Text.Length != 8)
            {
                return;
            }
            WordPtr = (byte)Convert.ToInt32(text_WordPtr.Text, 16);
            byte Num = Convert.ToByte(text_RWlen.Text);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            fCmdRet = RWDev.BlockErase_G2(ref fComAdr, EPC, ENum, Mem, WordPtr, Num, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Block erase failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Block erase failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Block erase success", 0);
        }

        private void BtSetProtectState_Click(object sender, EventArgs e)
        {
            byte select = 0;
            byte setprotect = 0;
            string str = "";
            byte ENum;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text.Length != 4) ||
                    (maskLen_textBox.Text.Length != 2) ||
                    (maskData_textBox.Text.Length % 2 != 0) &&
                    (maskData_textBox.Text.Length == 0))
                {
                    MessageBox.Show("Mask data error", "Information");
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (Edit_AccessCode6.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!", "Information");
                return;
            }
            fPassWord = HexStringToByteArray(Edit_AccessCode6.Text);
            if (P_kill.Checked)
                select = 0x00;
            else if (p_pass.Checked)
                select = 0x01;
            else if (P_EPC.Checked)
                select = 0x02;
            else if (P_TID.Checked)
                select = 0x03;
            else if (P_User.Checked)
                select = 0x04;

            if (NoProect2.Checked)
                setprotect = 0x00;
            else if (Proect2.Checked)
                setprotect = 0x02;
            else if (Always2.Checked)
            {
                setprotect = 0x01;
                if (MessageBox.Show(this, "Set unlock forever?", "Information", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                    return;
            }
            else if (AlwaysNot2.Checked)
            {
                setprotect = 0x03;
                if (MessageBox.Show(this, "Set lock forever?", "Information", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                    return;
            }
            fCmdRet = RWDev.Lock_G2(ref fComAdr, EPC, ENum, select, setprotect, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Lock failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Lock failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Lock success", 0);
        }

        private void BtDestroyCard_Click(object sender, EventArgs e)
        {
            string str = "";
            byte ENum;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];

            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text.Length != 4) ||
                    (maskLen_textBox.Text.Length != 2) ||
                    (maskData_textBox.Text.Length % 2 != 0) &&
                    (maskData_textBox.Text.Length == 0))
                {
                    MessageBox.Show("Mask data error!", "Information");
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked)
                    MaskMem = 1;
                if (R_TID.Checked)
                    MaskMem = 2;
                if (R_User.Checked)
                    MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                if (check_selecttag.Checked)
                    str = text_epc.Text;
                else
                    str = "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (text_DestroyCode.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!", "Information");
                return;
            }
            fPassWord = HexStringToByteArray(text_DestroyCode.Text);
            fCmdRet = RWDev.KillTag_G2(ref fComAdr, EPC, ENum, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Kill tag failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Kill tag failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Kill tag success", 0);
        }

        private void BtWriteEPC_G2_Click(object sender, EventArgs e)
        {
            byte ENum;
            if (text_AccessCode3.Text.Length < 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!", "Information");
                return;
            }
            if ((text_WriteEPC.Text.Length % 4) != 0 || text_WriteEPC.Text.Length == 0)
            {
                MessageBox.Show("Please input Data by words in hexadecimal form!'+#13+#10+'For example: 1234、12345678", "information");
                return;
            }

            ENum = Convert.ToByte(text_WriteEPC.Text.Length / 4);
            byte[] EPC = HexStringToByteArray(text_WriteEPC.Text);
            fPassWord = HexStringToByteArray(text_AccessCode3.Text);
            fCmdRet = RWDev.WriteEPC_G2(ref fComAdr, fPassWord, EPC, ENum, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Write EPC failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Write EPC failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                if (comboBox_EPC.Items.IndexOf(text_WriteEPC.Text) == -1)
                    comboBox_EPC.Items.Add(text_WriteEPC.Text);
                WriteLog(lrtxtLog, "Write EPC success ", 0);
            }
        }

        private void BtSetReadProtect_G2_Click(object sender, EventArgs e)
        {
            string str = "";
            byte ENum;
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            if (checkBox1.Checked)
            {
                if ((maskadr_textbox.Text.Length != 4) ||
                    (maskLen_textBox.Text.Length != 2) ||
                    (maskData_textBox.Text.Length % 2 != 0) &&
                    (maskData_textBox.Text.Length == 0))
                {
                    MessageBox.Show("Mask data error", "Information");
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                if (check_selecttag.Checked)
                    str = text_epc.Text;
                else
                    str = "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (text_AccessCode4.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!Please input again!!", "information");
                return;
            }
            fPassWord = HexStringToByteArray(text_AccessCode4.Text);
            fCmdRet = RWDev.SetPrivacyByEPC_G2(ref fComAdr, EPC, ENum, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {

                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Set privacy by EPC failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Set privacy by EPC failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Set privacy by EPC success", 0);
        }

        private void BtSetMultiReadProtect_G2_Click(object sender, EventArgs e)
        {
            if (text_AccessCode4.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!Please input again!!", "information");
                return;
            }
            fPassWord = HexStringToByteArray(text_AccessCode4.Text);
            fCmdRet = RWDev.SetPrivacyWithoutEPC_G2(ref fComAdr, fPassWord, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {

                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Set privacy without EPC failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Set privacy without EPC failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Set privacy without EPC success: ", 0);
        }

        private void BtRemoveReadProtect_G2_Click(object sender, EventArgs e)
        {
            if (text_AccessCode4.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!Please input again!!", "information");
                return;
            }
            fPassWord = HexStringToByteArray(text_AccessCode4.Text);
            fCmdRet = RWDev.ResetPrivacy_G2(ref fComAdr, fPassWord, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Reset privacy failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Reset privacy failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Reset privacy success ", 0);
        }

        private void BtCheckReadProtected_G2_Click(object sender, EventArgs e)
        {
            byte readpro = 2;
            fCmdRet = RWDev.CheckPrivacy_G2(ref fComAdr, ref readpro, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {

                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Check privacy faile: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Check privacy faile: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                string strLog =
                    (readpro == 0)
                    ? " 'Check privacy success'command return=0x00" + "(Single Tag is unprotected)"
                    : (readpro == 1)
                    ? " 'Check privacy success'command return=0x01" + "(Single Tag is protected)"
                    : "";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtSetEASAlarm_G2_Click(object sender, EventArgs e)
        {
            byte EAS = 0;
            byte ENum;
            string str = "";
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            text_WriteData.Text = "";
            if (checkBox1.Checked)
            {
                if (maskadr_textbox.Text == "" ||
                    maskLen_textBox.Text == "" ||
                    maskData_textBox.Text == "")
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked)
                    MaskMem = 1;
                if (R_TID.Checked)
                    MaskMem = 2;
                if (R_User.Checked)
                    MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                if (check_selecttag.Checked)
                    str = text_epc.Text;
                else
                    str = "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (text_AccessCode5.Text.Length != 8)
            {
                MessageBox.Show("Access Password Less Than 8 digit!Please input again!!", "information");
                return;
            }
            fPassWord = HexStringToByteArray(text_AccessCode5.Text);
            if (Alarm_G2.Checked)
                EAS = 1;

            fCmdRet = RWDev.EASConfigure_G2(ref fComAdr, EPC, ENum, fPassWord, EAS, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "EAS Configure failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                string strLog =
                    (Alarm_G2.Checked)
                    ? " 'EAS Configure' command return=0x00" + "(Set EAS Alarm successfully)"
                    : " 'EAS Configure' command return=0x00" + "(Clear EAS Alarm successfully)";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtCheckEASAlarm_Click(object sender, EventArgs e)
        {
            timer_EAS.Enabled = !timer_EAS.Enabled;
            if (!timer_EAS.Enabled)
            {
                pictureBox2.Visible = false;
                btCheckEASAlarm.Text = "Detect";
                btCheckEASAlarm.BackColor = Color.Transparent;
            }
            else
            {
                fIsInventoryScan = false;
                btCheckEASAlarm.Text = "Stop";
                btCheckEASAlarm.BackColor = Color.Indigo;
            }
        }

        private void Timer_EAS_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan)
                return;
            fIsInventoryScan = true;
            fCmdRet = RWDev.EASAlarm_G2(ref fComAdr, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "No EAS Alarm", 1);
                pictureBox2.Visible = false;
            }
            else
            {
                WriteLog(lrtxtLog, "EAS Alarm", 0);
                pictureBox2.Visible = true;
            }
            fIsInventoryScan = false;
        }

        private void BtInventory6B_Click(object sender, EventArgs e)
        {
            Timer_Test_6B.Enabled = !Timer_Test_6B.Enabled;
            if (!Timer_Test_6B.Enabled)
            {
                btInventory6B.Text = "Start";
                btInventory6B.BackColor = Color.Transparent;
            }
            else
            {
                fisinventoryscan_6B = false;
                ListView_ID_6B.Items.Clear();
                btInventory6B.BackColor = Color.Indigo;
                btInventory6B.Text = "Stop";
            }
        }

        private void ChangeSubItem1(ListViewItem ListItem, int subItemIndex, string ItemText, string ant, string RSSI)
        {
            if (subItemIndex == 1)
            {
                if (ListItem.SubItems[subItemIndex].Text != ItemText)
                {
                    ListItem.SubItems[subItemIndex].Text = ItemText;
                    ListItem.SubItems[subItemIndex + 2].Text = "1";
                    ListItem.SubItems[subItemIndex + 1].Text = ant;
                }
                else
                {
                    ListItem.SubItems[subItemIndex + 2].Text = Convert.ToString(Convert.ToUInt32(ListItem.SubItems[subItemIndex + 2].Text) + 1);
                    if (Convert.ToUInt32(ListItem.SubItems[subItemIndex + 2].Text) > 9999)
                        ListItem.SubItems[subItemIndex + 2].Text = "1";
                    ListItem.SubItems[subItemIndex + 1].Text = Convert.ToString(Convert.ToInt32(ListItem.SubItems[subItemIndex + 1].Text, 2) | Convert.ToInt32(ant, 2), 2).PadLeft(4, '0');

                }
                ListItem.SubItems[subItemIndex + 3].Text = RSSI;
            }
        }

        private void Inventory_6B()
        {
            byte[] ID_6B = new byte[2000];
            byte[] ID2_6B = new byte[5000];
            bool isonlistview;
            string temps;
            string s, ss, sID;
            ListViewItem aListItem;
            int i, j;
            byte StartAddress;
            byte ant = 0;
            if (rb_single.Checked)
            {
                fCmdRet = RWDev.InventorySingle_6B(ref fComAdr, ref ant, ID_6B, frmcomportindex);
                if (fCmdRet == 0)
                {
                    byte[] daw = new byte[10];
                    Array.Copy(ID_6B, daw, 10);
                    temps = ByteArrayToHexString(daw);
                    string RSSI = daw[9].ToString();
                    temps = temps.Substring(2, 16);
                    if (!list.Contains(temps))
                        list.Add(temps);

                    string sant = Convert.ToString(ant, 2).PadLeft(4, '0');
                    isonlistview = false;
                    for (i = 0; i < ListView_ID_6B.Items.Count; i++)
                    {
                        if (temps == ListView_ID_6B.Items[i].SubItems[1].Text)
                        {
                            aListItem = ListView_ID_6B.Items[i];
                            ChangeSubItem1(aListItem, 1, temps, sant, RSSI);
                            isonlistview = true;
                            break;
                        }
                    }
                    if (!isonlistview)
                    {
                        aListItem = ListView_ID_6B.Items.Add((ListView_ID_6B.Items.Count + 1).ToString());
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        aListItem.SubItems.Add("");
                        s = temps;
                        ChangeSubItem1(aListItem, 1, s, sant, RSSI);
                    }
                }
            }
            if (rb_mutiple.Checked)
            {
                byte Condition = 1;
                // The 4 conditions selected here are labels that are not all 0
                ss = "0000000000000000";
                byte[] daw = HexStringToByteArray(ss);
                byte mask = 0xFF;
                StartAddress = 0;
                int CardNum = 0;
                fCmdRet = RWDev.InventoryMultiple_6B(ref fComAdr, Condition, StartAddress, mask, daw, ref ant, ID2_6B, ref CardNum, frmcomportindex);
                if (fCmdRet == 0x15 ||
                    fCmdRet == 0x16 ||
                    fCmdRet == 0x17 ||
                    fCmdRet == 0x18 ||
                    fCmdRet == 0xFB)
                {
                    byte[] daw1 = new byte[CardNum * 10];
                    Array.Copy(ID2_6B, daw1, CardNum * 10);
                    temps = ByteArrayToHexString(daw1);
                    string sant = Convert.ToString(ant, 2).PadLeft(4, '0');
                    for (i = 0; i < CardNum; i++)
                    {
                        sID = temps.Substring(20 * i + 2, 16);
                        string RSSI = temps.Substring(20 * i + 18, 2);
                        RSSI = Convert.ToByte(RSSI, 16).ToString();
                        if ((sID.Length) != 16)
                            return;
                        if (CardNum == 0)
                            return;
                        isonlistview = false;
                        for (j = 0; j < ListView_ID_6B.Items.Count; j++)
                        {
                            if (sID == ListView_ID_6B.Items[j].SubItems[1].Text)
                            {
                                aListItem = ListView_ID_6B.Items[j];
                                ChangeSubItem1(aListItem, 1, sID, sant, RSSI);
                                isonlistview = true;
                                break;
                            }
                        }
                        if (!isonlistview)
                        {
                            aListItem = ListView_ID_6B.Items.Add((ListView_ID_6B.Items.Count + 1).ToString());
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            aListItem.SubItems.Add("");
                            s = sID;
                            ChangeSubItem1(aListItem, 1, s, sant, RSSI);
                        }
                    }
                }
            }
            WriteLog(lrtxtLog, "18000-6B Query", 0);
        }

        private void Timer_Test_6B_Tick(object sender, EventArgs e)
        {
            if (fisinventoryscan_6B)
                return;
            fisinventoryscan_6B = true;
            Inventory_6B();
            fisinventoryscan_6B = false;
        }

        private void ListView_ID_6B_DoubleClick(object sender, EventArgs e)
        {
            if (ListView_ID_6B.SelectedIndices.Count > 0 &&
                ListView_ID_6B.SelectedIndices[0] != -1)
            {
                text_6BUID.Text = ListView_ID_6B.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void BtRead6B_Click(object sender, EventArgs e)
        {
            byte[] CardData = new byte[320];
            byte Num, StartAddress;
            if (text_6BUID.Text == "")
            {
                MessageBox.Show("Select one tag in the list");
                return;
            }
            byte[] ID_6B = HexStringToByteArray(text_6BUID.Text);
            if (text_R6BAddr.Text == "")
                return;
            StartAddress = Convert.ToByte(text_R6BAddr.Text, 16);
            if (text_R6BLen.Text == "")
                return;
            Num = Convert.ToByte(text_R6BLen.Text, 16);
            fCmdRet = RWDev.ReadData_6B(ref fComAdr, ID_6B, StartAddress, Num, CardData, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Read data failed: " + "tag return error=0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Read data failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                byte[] data = new byte[Num];
                Array.Copy(CardData, data, Num);
                text_R6B.Text = ByteArrayToHexString(data);
                WriteLog(lrtxtLog, "Read data success ", 0);
            }
        }

        private void BtWrite6B_Click(object sender, EventArgs e)
        {
            byte StartAddress;
            byte Writedatalen;
            int writtenbyte = 0;
            if (text_6BUID.Text == "")
            {
                MessageBox.Show("Select one tag in the list");
                return;
            }

            byte[] ID_6B = HexStringToByteArray(text_6BUID.Text);
            if (text_W6BAddr.Text == "")
                return;
            StartAddress = Convert.ToByte(text_W6BAddr.Text, 16);
            if (text_W6BLen.Text == "")
                return;
            Writedatalen = Convert.ToByte(text_W6BLen.Text, 16);

            if ((text_W6B.Text == "") | ((text_W6B.Text.Length / 2) != Writedatalen) | ((text_W6B.Text.Length % 2) != 0))
                return;
            byte[] Writedata = HexStringToByteArray(text_W6B.Text);
            fCmdRet = RWDev.WriteData_6B(ref fComAdr, ID_6B, StartAddress, Writedata, Writedatalen, ref writtenbyte, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Write data failed: " + "tag return error=0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Write data failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Write data success", 0);

        }

        private void Text_W6BLen_TextChanged(object sender, EventArgs e)
        {
            text_W6B.MaxLength = Convert.ToInt32(text_W6BLen.Text, 16) * 2;
        }

        private void BtLock6B_Click(object sender, EventArgs e)
        {
            byte Address;
            if (text_6BUID.Text == "")
            {
                MessageBox.Show("Please select a label from the list");
                return;
            }
            byte[] ID_6B = HexStringToByteArray(text_6BUID.Text);
            if (text_lock6b.Text == "")
                return;
            Address = Convert.ToByte(text_lock6b.Text, 16);
            if (MessageBox.Show(this, "Lock forever?", "information", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                return;
            fCmdRet = RWDev.Lock_6B(ref fComAdr, ID_6B, Address, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Lock failed: " + "tag return error=0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Lock failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
                WriteLog(lrtxtLog, "Lock success", 0);
        }

        private void BtCheckLock6B_Click(object sender, EventArgs e)
        {
            byte Address, ReLockState = 2;
            if (text_6BUID.Text == "")
            {
                MessageBox.Show("Select one tag in the list");
                return;
            }
            byte[] ID_6B = HexStringToByteArray(text_6BUID.Text);
            if (text_checkaddr.Text == "")
                return;
            Address = Convert.ToByte(text_checkaddr.Text, 16);
            fCmdRet = RWDev.CheckLock_6B(ref fComAdr, ID_6B, Address, ref ReLockState, ref ferrorcode, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Detect lock failed: " + "tag return error=0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Detect lock failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                if (ReLockState == 0)
                    text_Statu6B.Text = "Unlocked";
                if (ReLockState == 1)
                    text_Statu6B.Text = "Locked";
                WriteLog(lrtxtLog, "Detect lock success", 0);
            }

        }

        private void ReflashDeviceListView(List<DeviceClass> deviceList)
        {
            DeviceListView.Items.Clear();
            foreach (DeviceClass device in deviceList)
            {
                IPAddress ipAddr = GetIPAddress(device.DeviceIP);
                ListViewItem deviceListViewItem = new ListViewItem(new string[] { device.DeviceName, ipAddr.ToString(), device.DeviceMac })
                {
                    ImageIndex = 0
                };
                DeviceListView.Items.Add(deviceListViewItem);
            }
        }

        private void ClearDeviceListView()
        {
            DevControl.TagErrorCode eCode;
            List<DeviceClass> deviceList = DevList;

            foreach (DeviceClass device in deviceList)
            {
                eCode = DevControl.DM_FreeDevice(device.DevHandle);
                Debug.Assert(eCode == DevControl.TagErrorCode.DM_ERR_OK);
            }
            // Clear the device list and clear the corresponding display controls
            DevList.Clear();
            ReflashDeviceListView(DevList);
        }

        private bool SearchDevice(uint targetIP)
        {
            ClearDeviceListView();
            DevControl.TagErrorCode eCode = DevControl.DM_SearchDevice(targetIP, 1500);
            if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
            {
                ReflashDeviceListView(DevList);
                return true;
            }
            else
            {
                MessageBox.Show(ErrorHandling.HandleError(eCode));
                return false;
            }
        }

        private void ConfigSelectedDevice()
        {
            if (DeviceListView.SelectedIndices.Count > 0
                && DeviceListView.SelectedIndices[0] != -1)
            {
                // Find the corresponding device object through the index value
                // selected by the user in the display control;
                DeviceClass currentDevice = DevList[DeviceListView.SelectedIndices[0]];

                LoginForm loginform = new LoginForm();
                DialogResult result = loginform.ShowDialog();
                if (result == DialogResult.OK)
                {
                    DevControl.TagErrorCode eCode = currentDevice.Login(loginform.UserName, loginform.Password);
                    if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                    {
                        SelectedDevice = currentDevice;
                        ConfigForm deviceConfigForm = new ConfigForm(SelectedDevice);
                        deviceConfigForm.ShowDialog(this);
                        deviceConfigForm.Dispose();
                    }
                    else
                    {
                        MessageBox.Show(ErrorHandling.HandleError(eCode));
                    }
                }
                loginform.Dispose();
            }
        }

        private void SearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SearchDevice(DeviceClass.Broadcast);
        }

        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigSelectedDevice();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearDeviceListView();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void IEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (DeviceListView.SelectedIndices.Count > 0 &&
                    DeviceListView.SelectedIndices[0] != -1)
                {
                    DeviceClass currentDevice = DevList[DeviceListView.SelectedIndices[0]];
                    Process.Start("iexplore.exe", "HTTP://" + GetIPAddress(currentDevice.DeviceIP).ToString());
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex);
            }
        }

        private void TelnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (DeviceListView.SelectedIndices.Count > 0
                    && DeviceListView.SelectedIndices[0] != -1)
                {
                    DeviceClass currentDevice = DevList[DeviceListView.SelectedIndices[0]];
                    Process.Start("telnet.exe", GetIPAddress(currentDevice.DeviceIP).ToString());
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex);
            }
        }

        private void PingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (DeviceListView.SelectedIndices.Count > 0
                    && DeviceListView.SelectedIndices[0] != -1)
                {
                    DeviceClass currentDevice = DevList[DeviceListView.SelectedIndices[0]];
                    Process.Start("ping.exe", GetIPAddress(currentDevice.DeviceIP).ToString() + " -t");
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DevControl.TagErrorCode eCode = DevControl.DM_DeInit();
            if (eCode != DevControl.TagErrorCode.DM_ERR_OK)
                ErrorHandling.HandleError(eCode);
        }

        private void DeviceListView_DoubleClick(object sender, EventArgs e)
        {
            ConfigSelectedDevice();
        }

        private void BtFlashROM_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, " if change to flush mode,need restart power to restore.are you sure do this?", "Information", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                return;
            fCmdRet = RWDev.SetFlashRom(ref fComAdr, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Change to flush mode failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                string strLog = "Change to flush mode success ";
                WriteLog(lrtxtLog, strLog, 0);
                if (frmcomportindex > 0 && frmcomportindex < 256)
                    BtDisConnect232_Click(null, null);
            }
        }

        public class CTcpList
        {
            public Socket[] tempSocket = new Socket[100];
            public string[] ip = new string[100];
            public int[] port = new int[100];
        }

        private void StartListening() //main listening thread
        {
            int port = Convert.ToInt32(stcpport.Text);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);//绑定端口
            newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//tcp协议
            newsock.Bind(ipep);
            newsock.Listen(10);
            while (true)
            {
                try
                {
                    // Waiting for a connection request from the TCP client
                    Socket client = newsock.Accept();
                    //Find gaps and insert information
                    for (int m = 0; m < 100; m++)
                    {
                        if (tcplist.ip[m] == null)
                        {
                            int nport = ((IPEndPoint)client.RemoteEndPoint).Port;
                            IPAddress ip = ((IPEndPoint)client.RemoteEndPoint).Address;
                            tcplist.tempSocket[m] = client;
                            tcplist.ip[m] = ip.ToString();
                            tcplist.port[m] = nport;
                            Invoke((EventHandler)delegate
                            {
                                listtcp.Items.Add(ip.ToString() + ":" + nport.ToString());
                            });
                            break;
                        }
                    }
                    ParameterizedThreadStart ParStart = new ParameterizedThreadStart(ServiceClient);
                    Thread clientService = new Thread(ParStart)
                    {
                        IsBackground = true
                    };
                    object o = client;
                    clientService.Start(o);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private void BtListen_Click(object sender, EventArgs e)
        {
            stcpport.Enabled = false;
            btListen.Enabled = false;
            listenThread = new Thread(new ThreadStart(StartListening))
            {
                IsBackground = true
            };
            listenThread.Start();
        }

        private static string ByteArrayToHexString2(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadLeft(3, ' '));
            return sb.ToString().ToUpper();

        }

        private void ServiceClient(object ParObject)
        {
            Socket tempSocket = (Socket)ParObject;
            IPEndPoint newclient = (IPEndPoint)tempSocket.RemoteEndPoint;

            //  NetworkStream ns = new NetworkStream(tempSocket);
            //  StreamReader sr = new StreamReader(ns);
            byte[] myReadBuffer = new byte[1024];
            IPAddress ip = (newclient).Address;
            int nport = (newclient).Port;
            int count = 0;
            string temp = "";
            while (true)
            {
                try
                {
                    count = tempSocket.Receive(myReadBuffer);
                    if (count > 0)
                    {
                        byte[] data = new byte[count];
                        Array.Copy(myReadBuffer, data, count);
                        temp = ByteArrayToHexString2(data);
                        Invoke((EventHandler)delegate
                        {
                            stcprecv.AppendText(Environment.NewLine + ip.ToString() + ":" + nport.ToString() + " " + temp);
                            stcprecv.Select(stcprecv.TextLength, 0);
                            stcprecv.ScrollToCaret();
                        });
                    }
                    else
                    {
                        Invoke((EventHandler)delegate
                        {
                            int m = 0;
                            for (m = 0; m < 100; m++)
                            {
                                if (tcplist.ip[m] == ip.ToString() && (tcplist.port[m] == nport))//找到
                                {
                                    int n = listtcp.Items.IndexOf(ip.ToString() + ":" + nport.ToString());
                                    listtcp.Items.RemoveAt(n);
                                    tcplist.tempSocket[m].Close();
                                    tcplist.ip[m] = null;
                                    tcplist.port[m] = 0;
                                    tcplist.tempSocket[m] = null;
                                    break;
                                }
                            }
                        });
                        break;
                    }
                }
                catch
                {
                    // Find disconnected device IP
                    Invoke((EventHandler)delegate
                    {
                        int m = 0;
                        for (m = 0; m < 100; m++)
                        {
                            if (tcplist.ip[m] == ip.ToString() && (tcplist.port[m] == nport))//找到
                            {
                                int n = listtcp.Items.IndexOf(ip.ToString() + ":" + nport.ToString());
                                listtcp.Items.RemoveAt(n);
                                tcplist.tempSocket[m].Close();
                                tcplist.ip[m] = null;
                                tcplist.port[m] = 0;
                                tcplist.tempSocket[m] = null;
                                break;
                            }
                        }
                    });
                    break;
                }
            }
        }

        private void BtStop_Click(object sender, EventArgs e)
        {
            for (int m = 0; m < 100; m++)
            {
                if (tcplist.tempSocket[m] != null)
                {

                    int n = listtcp.Items.IndexOf(tcplist.ip[m] + ":" + tcplist.port[m].ToString());
                    listtcp.Items.RemoveAt(n);
                    tcplist.tempSocket[m].Close();
                    tcplist.ip[m] = null;
                    tcplist.port[m] = 0;
                    tcplist.tempSocket[m] = null;
                }
            }

            newsock?.Close();
            listenThread?.Abort();

            stcpport.Enabled = true;
            btListen.Enabled = true;
        }

        private void BtTcpConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = tcpremoteIp.IpAddressStr;
                int nipport = Convert.ToInt32(remotePort.Text);
                m_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ie = new IPEndPoint(IPAddress.Parse(ip), nipport);
                m_client.Connect(ie);
                if (m_client.Connected)
                {
                    tcpremoteIp.Enabled = false;
                    remotePort.Enabled = false;
                    bttcpconnect.Enabled = false;
                    bttcpsend.Enabled = true;
                    clientThread = new Thread(new ThreadStart(StartRead))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
                else
                {
                    tcpremoteIp.Enabled = true;
                    remotePort.Enabled = true;
                    bttcpconnect.Enabled = true;
                    bttcpsend.Enabled = false;
                }

            }
            catch (SocketException)
            {
                tcpremoteIp.Enabled = true;
                remotePort.Enabled = true;
                bttcpconnect.Enabled = true;
                bttcpsend.Enabled = false;
            }
        }

        private void StartRead()
        {
            byte[] buffs = new byte[2048];
            IPEndPoint newclient = (IPEndPoint)m_client.RemoteEndPoint;
            IPAddress ip = (newclient).Address;
            int nport = (newclient).Port;
            while (true)
            {
                try
                {
                    int count = m_client.Receive(buffs);
                    if (count > 0)
                    {
                        byte[] data = new byte[count];
                        Array.Copy(buffs, data, count);
                        string temp = ByteArrayToHexString2(data);
                        Invoke((EventHandler)delegate
                        {
                            ctctrecv.AppendText(Environment.NewLine + ip.ToString() + ":" + nport.ToString() + " " + temp);
                            ctctrecv.Select(stcprecv.TextLength, 0);
                            ctctrecv.ScrollToCaret();
                        });
                    }
                }
                catch
                {
                    if (m_client.Connected)
                    {
                        m_client.Shutdown(SocketShutdown.Both);
                        m_client.Close();
                    }
                    Invoke((EventHandler)delegate
                    {
                        tcpremoteIp.Enabled = true;
                        remotePort.Enabled = true;
                        bttcpconnect.Enabled = true;
                    });
                    break;
                }
            }
        }

        private void BtTcpSend_Click(object sender, EventArgs e)
        {
            if (m_client.Connected)
            {
                string temp = ctctsend.Text.Replace(" ", "");
                if (temp == "") return;
                if (temp.Length % 2 != 0)
                    temp += "0";
                byte[] buff = HexStringToByteArray(temp);
                m_client.Send(buff);
            }
        }

        private void BtTcpDisconnect_Click(object sender, EventArgs e)
        {
            if (m_client.Connected)
            {
                m_client.Shutdown(SocketShutdown.Both);
                m_client.Close();
                clientThread.Abort();
                tcpremoteIp.Enabled = true;
                remotePort.Enabled = true;
                bttcpconnect.Enabled = true;
                bttcpsend.Enabled = false;
            }
        }

        private void Com_S_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (com_S.SelectedIndex > 1)
            {
                check_num.Enabled = true;
            }
            else
            {
                check_num.Enabled = false;
            }
            if (com_S.SelectedIndex == 4)
            {
                group_ant1.Enabled = false;
                com_scantime.Enabled = false;
            }
            else
            {
                group_ant1.Enabled = true;
                com_scantime.Enabled = true;
            }
        }

        private void BtSetEPCandTIDLen_Click(object sender, EventArgs e)
        {
            byte SaveLen = (rb128.Checked) ? (byte)0 : (byte)1;
            fCmdRet = RWDev.SetSaveLen(ref fComAdr, SaveLen, frmcomportindex);
            if (fCmdRet == 0)
            {
                WriteLog(lrtxtLog, "Set save length success ", 0);
            }
            else
            {
                WriteLog(lrtxtLog, "Set save length failed: " + GetReturnCodeDesc(fCmdRet), 0);
            }
        }

        private void BtGetEPCandTIDLen_Click(object sender, EventArgs e)
        {
            byte SaveLen = 0;
            fCmdRet = RWDev.GetSaveLen(ref fComAdr, ref SaveLen, frmcomportindex);
            if (fCmdRet == 0)
            {
                if (SaveLen == 0)
                    rb128.Checked = true;
                else
                    rb496.Checked = true;
                string strLog = "Get save length success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
            else
            {
                string strLog = "Get save length failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtReadBuff_Click(object sender, EventArgs e)
        {
            int Totallen = 0;
            int CardNum = 0;
            byte[] pEPCList = new byte[30000];
            //lxLed_BNum.Text = "0";
            //lxLed_Bcmdsud.Text = "0";
            //lxLed_Btoltag.Text = "0";
            //lxLed_Btoltime.Text = "0";
            //lxLed_cmdTime.Text = "0";
            dataGridView3.Rows.Clear();
            fCmdRet = RWDev.ReadBuffer_G2(ref fComAdr, ref Totallen, ref CardNum, pEPCList, frmcomportindex);
            if (fCmdRet == 1)
            {
                int m = 0;
                byte[] daw = new byte[Totallen];
                Array.Copy(pEPCList, daw, Totallen);
                for (int i = 0; i < CardNum; i++)
                {
                    string ant = Convert.ToString(daw[m], 2).PadLeft(4, '0');
                    int len = daw[m + 1];
                    byte[] EPC = new byte[len];
                    Array.Copy(daw, m + 2, EPC, 0, len);
                    string sEPC = ByteArrayToHexString(EPC);
                    string RSSI = daw[m + 2 + len].ToString();
                    string sCount = daw[m + 3 + len].ToString();
                    m = m + 4 + len;
                    string[] arr = new string[6];
                    arr[0] = (dataGridView3.RowCount + 1).ToString();
                    arr[1] = sEPC;
                    arr[2] = len.ToString();
                    arr[3] = ant;
                    arr[4] = RSSI;
                    arr[5] = sCount;
                    dataGridView3.Rows.Insert(dataGridView3.RowCount, arr);
                }
                lxLed_BNum.Text = dataGridView3.RowCount.ToString();
                string strLog = "Read buffer success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
            else
            {
                lxLed_BNum.Text = "0";
                string strLog = "Read buffer failed! ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtClearBuff_Click(object sender, EventArgs e)
        {
            fCmdRet = RWDev.ClearBuffer_G2(ref fComAdr, frmcomportindex);
            if (fCmdRet == 0)
            {
                string strLog = "Clear buffer success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
            else
            {
                string strLog = "Clear buffer failed";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtRandCbuff_Click(object sender, EventArgs e)
        {
            BtReadBuff_Click(null, null);
            BtClearBuff_Click(null, null);
        }

        private void BtQueryBuffNum_Click(object sender, EventArgs e)
        {
            int Count = 0;
            //lxLed_Bcmdsud.Text = "0";
            //lxLed_cmdTime.Text = "0";
            lxLed_BNum.Text = "0";
            //lxLed_Btoltag.Text = "0";
            //lxLed_Btoltime.Text = "0";
            fCmdRet = RWDev.GetBufferCnt_G2(ref fComAdr, ref Count, frmcomportindex);
            if (fCmdRet == 0)
            {
                lxLed_BNum.Text = Count.ToString();
                string strLog = "Get buffer tag number success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
            else
            {
                string strLog = "Get buffer tag number failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtStartBuff_Click(object sender, EventArgs e)
        {
            if (btStartBuff.Text == "Start")
            {
                if (rb_bepc.Checked)
                    TIDFlag = 0;
                else
                    TIDFlag = 1;
                total_time = Environment.TickCount;
                total_tagnum = 0;
                btStartBuff.BackColor = Color.Indigo;
                btStartBuff.Text = "Stop";
                toStopThread = false;
                if (fIsBuffScan == false)
                {
                    ReadThread = new Thread(new ThreadStart(ReadProcess));
                    ReadThread.Start();
                }
                timer_Buff.Enabled = true;
            }
            else
            {
                btStartBuff.BackColor = Color.Transparent;
                btStartBuff.Text = "Start";
                if (fIsBuffScan)
                {
                    toStopThread = true;//标志，接收数据线程判断stop为true，正常情况下会自动退出线程            
                    ReadThread.Abort();
                    fIsBuffScan = false;
                }
                timer_Buff.Enabled = false;
            }
        }

        private void GetBuffData()
        {
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            byte AdrTID = 0;
            byte LenTID = 6;
            byte MaskFlag = 0;
            int cbtime = Environment.TickCount;
            int TagNum = 0;
            int BufferCount = 0;
            Target = 0;
            Scantime = 0x14;
            Qvalue = 6;
            if (TIDFlag == 0)
                Session = 255;
            else
                Session = 1;

            FastFlag = 0;
            fCmdRet = RWDev.InventoryBuffer_G2(ref fComAdr, Qvalue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, AdrTID, LenTID, TIDFlag, Target, InAnt, Scantime, FastFlag, ref BufferCount, ref TagNum, frmcomportindex);
            int x_time = Environment.TickCount - cbtime;//命令时间
            if (fCmdRet == 0)
            {
                IntPtr hWnd = FindWindow(null, Text);
                if (hWnd != IntPtr.Zero)
                {
                    total_tagnum += TagNum;
                    int tagrate = (TagNum * 1000) / x_time;
                    string para = BufferCount.ToString() + "," + x_time.ToString() + "," + tagrate.ToString() + "," + total_tagnum.ToString();
                    SendMessage(hWnd, WM_SENDBUFF, IntPtr.Zero, para);
                }
            }
        }

        private void ReadProcess()
        {
            fIsBuffScan = true;
            while (!toStopThread)
            {
                GetBuffData();
            }
            fIsBuffScan = false;
        }

        private void Timer_Buff_Tick(object sender, EventArgs e)
        {
            lxLed_Btoltime.Text = (Environment.TickCount - total_time).ToString();
        }

        private void BtExtRead_Click(object sender, EventArgs e)
        {
            byte ENum;
            byte Mem = 0;
            string str = ""; ;
            byte[] CardData = new byte[320];
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            text_WriteData.Text = "";
            if (checkBox1.Checked)
            {
                if (maskadr_textbox.Text == "" ||
                    maskLen_textBox.Text == "" ||
                    maskData_textBox.Text == "")
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                str = (check_selecttag.Checked) ? text_epc.Text : "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;
            if (text_WordPtr.Text == "" ||
                text_RWlen.Text == "" ||
                text_AccessCode2.Text.Length != 8)
            {
                return;
            }
            byte[] WordPtr = HexStringToByteArray(text_WordPtr.Text);
            byte Num = Convert.ToByte(text_RWlen.Text);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            for (int p = 0; p < 10; p++)
            {
                fCmdRet = RWDev.ExtReadData_G2(ref fComAdr, EPC, ENum, Mem, WordPtr, Num, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, CardData, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0) break;
            }
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Extense read failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Extense read failed:  " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                byte[] daw = new byte[Num * 2];
                Array.Copy(CardData, daw, Num * 2);
                text_WriteData.Text = ByteArrayToHexString(daw);
                WriteLog(lrtxtLog, "Extense read success", 0);
            }
        }

        private void BtExtWrite_Click(object sender, EventArgs e)
        {
            byte ENum;
            byte Mem = 0;
            string str = "";
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[100];
            if (checkBox1.Checked)
            {
                if (maskadr_textbox.Text == "" ||
                    maskLen_textBox.Text == "" ||
                    maskData_textBox.Text == "")
                {
                    return;
                }
                ENum = 255;
                if (R_EPC.Checked) MaskMem = 1;
                if (R_TID.Checked) MaskMem = 2;
                if (R_User.Checked) MaskMem = 3;
                MaskAdr = HexStringToByteArray(maskadr_textbox.Text);
                MaskLen = Convert.ToByte(maskLen_textBox.Text, 16);
                MaskData = HexStringToByteArray(maskData_textBox.Text);
            }
            else
            {
                if (check_selecttag.Checked)
                    str = text_epc.Text;
                else
                    str = "";
                ENum = Convert.ToByte(str.Length / 4);
            }
            byte[] EPC = HexStringToByteArray(str);
            if (C_Reserve.Checked)
                Mem = 0;
            if (C_EPC.Checked)
                Mem = 1;
            if (C_TID.Checked)
                Mem = 2;
            if (C_User.Checked)
                Mem = 3;

            if (text_WordPtr.Text == "" || text_AccessCode2.Text.Length != 8)
                return;

            string epcstr = text_WriteData.Text;
            if (epcstr.Length % 4 != 0 || epcstr.Length == 0)
            {
                MessageBox.Show("Input data by word", "Write");
                return;
            }
            byte WNum = Convert.ToByte(epcstr.Length / 4);
            byte[] Writedata = HexStringToByteArray(epcstr);
            byte[] WordPtr = HexStringToByteArray(text_WordPtr.Text);
            fPassWord = HexStringToByteArray(text_AccessCode2.Text);
            if ((checkBox_pc.Checked) && (C_EPC.Checked))
            {
                WordPtr = HexStringToByteArray("0001");
                WNum = Convert.ToByte(epcstr.Length / 4 + 1);
                Writedata = HexStringToByteArray(textBox_pc.Text + epcstr);
            }
            for (int p = 0; p < 10; p++)
            {
                fCmdRet = RWDev.ExtWriteData_G2(ref fComAdr, EPC, WNum, ENum, Mem, WordPtr, Writedata, fPassWord, MaskMem, MaskAdr, MaskLen, MaskData, ref ferrorcode, frmcomportindex);
                if (fCmdRet == 0) break;
            }
            if (fCmdRet != 0)
            {
                string strLog =
                    (fCmdRet == 0xFC)
                    ? "Extense write failed: " + "Return = 0x" + Convert.ToString(ferrorcode, 16) + "(" + GetErrorCodeDesc(ferrorcode) + ")"
                    : "Extense write failed:  " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                WriteLog(lrtxtLog, "Extense write success", 0);
            }
        }

        private void BtSetMaxtime_Click(object sender, EventArgs e)
        {
            byte Scantime = Convert.ToByte(comboBox_maxtime.SelectedIndex);
            fCmdRet = RWDev.SetInventoryScanTime(ref fComAdr, Scantime, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Set inventory scan time failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                WriteLog(lrtxtLog, "Set inventory scan time success", 0);
            }
        }

        private void Panel9_Paint(object sender, PaintEventArgs e)
        {

        }

        private void BtSetMmode_Click(object sender, EventArgs e)
        {
            byte ReadMode = (byte)com_Mmode.SelectedIndex;
            fCmdRet = RWDev.SetReadMode(ref fComAdr, ReadMode, frmcomportindex);

            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Set read mode failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                WriteLog(lrtxtLog, "Set read mode success ", 0);
            }
        }

        private void BtMSetParameter_Click(object sender, EventArgs e)
        {
            byte[] Parameter = new byte[200];
            byte MaskMem = 0;
            byte[] MaskAdr = new byte[2];
            byte MaskLen = 0;
            byte[] MaskData = new byte[200];
            byte MaskFlag = 0;
            byte AdrTID = 0;
            byte LenTID = 0;
            byte TIDFlag = 0;
            if (MRB_G2.Checked)
            {
                Parameter[0] = 0;
            }
            else
            {
                Parameter[0] = 1;
            }

            Parameter[1] = (byte)com_MRPTime.SelectedIndex;
            Parameter[2] = (byte)com_MFliterTime.SelectedIndex;
            Parameter[3] = (byte)com_MQ.SelectedIndex;
            Parameter[4] = (byte)com_MS.SelectedIndex;
            if (Parameter[4] > 3) Parameter[4] = 255;
            if (checkBox_mask.Checked)
            {
                if (RBM_EPC.Checked)
                {
                    MaskMem = 1;
                }
                else if (RBM_TID.Checked)
                {
                    MaskMem = 2;
                }
                else if (RBM_USER.Checked)
                {
                    MaskMem = 3;
                }
                if ((txt_Maddr.Text.Length != 4) || (txt_Mlen.Text.Length != 2) || (txt_Mdata.Text.Length % 2 != 0))
                {
                    MessageBox.Show("Mask error!", "information");
                    return;
                }
                MaskAdr = HexStringToByteArray(txt_Maddr.Text);
                int len = Convert.ToInt32(txt_Mlen.Text, 16);
                MaskLen = (byte)len;
                MaskData = HexStringToByteArray(txt_Mdata.Text);
                MaskFlag = 1;
            }
            if (checkBox_tid.Checked)
            {
                AdrTID = Convert.ToByte(txt_mtidaddr.Text, 16);
                LenTID = Convert.ToByte(txt_Mtidlen.Text, 16);
                TIDFlag = 1;
            }
            fCmdRet = RWDev.SetReadParameter(ref fComAdr, Parameter, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, AdrTID, LenTID, TIDFlag, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Set read parameter failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                string strLog = "Set read parameter success ";
                WriteLog(lrtxtLog, strLog, 0);
            }

        }

        private void BtMGetParameter_Click(object sender, EventArgs e)
        {
            byte[] Parameter = new byte[200];
            fCmdRet = RWDev.GetReadParameter(ref fComAdr, Parameter, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Get read parameter failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                com_Mmode.SelectedIndex = Parameter[0];
                if (Parameter[1] == 0)
                    MRB_G2.Checked = true;
                else
                    MRB_6B.Checked = true;

                com_MRPTime.SelectedIndex = Parameter[2];
                com_MFliterTime.SelectedIndex = Parameter[3];
                com_MQ.SelectedIndex = Parameter[4];
                if (Parameter[5] == 255)
                    com_MS.SelectedIndex = 4;
                else
                    com_MS.SelectedIndex = Parameter[5];

                if (Parameter[6] == 1)
                    RBM_EPC.Checked = true;
                else if (Parameter[6] == 2)
                    RBM_TID.Checked = true;
                else if (Parameter[6] == 3)
                    RBM_USER.Checked = true;

                byte[] maskaddr = new byte[2];
                Array.Copy(Parameter, 7, maskaddr, 0, 2);
                txt_Maddr.Text = ByteArrayToHexString(maskaddr);
                txt_Mlen.Text = Convert.ToString(Parameter[9], 16).PadLeft(2, '0');
                byte[] data = new byte[32];
                Array.Copy(Parameter, 10, data, 0, 32);
                string temp = ByteArrayToHexString(data);
                int len = Parameter[9];

                if ((len % 8) == 0)
                    len /= 8;
                else
                    len = len / 8 + 1;

                if (len <= 32)
                    temp = temp.Substring(0, len * 2);

                txt_Mdata.Text = temp;
                txt_mtidaddr.Text = Convert.ToString(Parameter[42], 16).PadLeft(2, '0');
                txt_Mtidlen.Text = Convert.ToString(Parameter[43], 16).PadLeft(2, '0');
                string strLog = "Get read parameter success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtStartMactive_Click(object sender, EventArgs e)
        {
            timer_RealTime.Enabled = !timer_RealTime.Enabled;
            if (!timer_RealTime.Enabled)
            {
                btStartMactive.Text = "Start";
                btStartMactive.BackColor = Color.Transparent;
            }
            else
            {
                fInventory_EPC_List = "";
                total_time = Environment.TickCount;
                lxLed_Mtag.Text = "0";
                lxLed_Mtime.Text = "0";
                dataGridView4.Rows.Clear();
                btStartMactive.BackColor = Color.Indigo;
                fIsInventoryScan = false;
                btStartMactive.Text = "Stop";
            }
        }

        private void GetRealTimeData()
        {
            byte[] ScanModeData = new byte[40960];
            int nLen, NumLen;
            string temp1;
            string RSSI;
            string AntStr;
            string lenstr;
            string EPCStr;
            int ValidDatalength;
            string temp;
            ValidDatalength = 0;
            DataGridViewRow rows;

            fCmdRet = RWDev.ReadActiveModeData(ScanModeData, ref ValidDatalength, frmcomportindex);
            if (fCmdRet == 0)
            {
                try
                {
                    byte[] daw = new byte[ValidDatalength];
                    Array.Copy(ScanModeData, 0, daw, 0, ValidDatalength);
                    temp = ByteArrayToHexString(daw);
                    fInventory_EPC_List += temp;
                    nLen = fInventory_EPC_List.Length;
                    while (fInventory_EPC_List.Length > 18)
                    {
                        // Find header position flag string
                        string FlagStr = Convert.ToString(fComAdr, 16).PadLeft(2, '0') + "EE00";
                        int nindex = fInventory_EPC_List.IndexOf(FlagStr);

                        if (nindex > 1)
                            fInventory_EPC_List = fInventory_EPC_List.Substring(nindex - 2);
                        else
                        {
                            fInventory_EPC_List = fInventory_EPC_List.Substring(2);
                            continue;
                        }

                        // Get the length of the first frame
                        NumLen = Convert.ToInt32(fInventory_EPC_List.Substring(0, 2), 16) * 2 + 2;
                        if (fInventory_EPC_List.Length < NumLen)
                            break;

                        temp1 = fInventory_EPC_List.Substring(0, NumLen);
                        fInventory_EPC_List = fInventory_EPC_List.Substring(NumLen);
                        if (!CheckCRC(temp1))
                            continue;

                        AntStr = Convert.ToString(Convert.ToInt32(temp1.Substring(8, 2), 16), 2).PadLeft(4, '0');
                        lenstr = Convert.ToString(Convert.ToInt32(temp1.Substring(10, 2), 16), 10);
                        EPCStr = temp1.Substring(12, temp1.Length - 18);
                        RSSI = temp1.Substring(temp1.Length - 6, 2);
                        bool isonlistview = false;
                        for (int i = 0; i < dataGridView4.RowCount; i++)
                        {
                            if ((dataGridView4.Rows[i].Cells[1].Value != null) && (EPCStr == dataGridView4.Rows[i].Cells[1].Value.ToString()))
                            {
                                rows = dataGridView4.Rows[i];
                                rows.Cells[3].Value = AntStr;
                                rows.Cells[4].Value = RSSI;
                                isonlistview = true;
                                break;
                            }
                        }
                        if (!isonlistview)
                        {
                            string[] arr = new string[6];
                            arr[0] = (dataGridView4.RowCount + 1).ToString();
                            arr[1] = EPCStr;
                            arr[2] = lenstr;
                            arr[3] = AntStr;
                            arr[4] = RSSI;
                            dataGridView4.Rows.Insert(dataGridView4.RowCount, arr);
                        }
                        lxLed_Mtime.Text = (Environment.TickCount - total_time).ToString();
                    }
                }
                catch { }
            }
            lxLed_Mtag.Text = dataGridView4.RowCount.ToString();
        }
        private void Timer_RealTime_Tick(object sender, EventArgs e)
        {
            if (fIsInventoryScan) return;
            fIsInventoryScan = true;
            GetRealTimeData();
            fIsInventoryScan = false;
        }

        private void RB_Chinese_band1_CheckedChanged(object sender, EventArgs e)
        {
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (int i = 0; i < 20; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(840.125 + i * 0.25) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(840.125 + i * 0.25) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(840.125 + i * 0.25));
            }
            ComboBox_dmaxfre.SelectedIndex = 19;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 10;
        }

        private void BtSetWpower_Click(object sender, EventArgs e)
        {
            byte WritePower = (byte)(com_wpower.SelectedIndex);
            if (!rb_wp1.Checked)
                WritePower = Convert.ToByte(WritePower | 0x80);

            fCmdRet = RWDev.WriteRfPower(ref fComAdr, WritePower, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set failed： " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set success ", 0);
        }

        private void BtGetWpower_Click(object sender, EventArgs e)
        {
            byte WritePower = 0;
            fCmdRet = RWDev.ReadRfPower(ref fComAdr, ref WritePower, frmcomportindex);
            if (fCmdRet != 0)
            {
                WriteLog(lrtxtLog, "Get failed: " + GetReturnCodeDesc(fCmdRet), 1);
            }
            else
            {
                if ((WritePower & 0x80) == 0)
                {
                    rb_wp1.Checked = true;
                    com_wpower.SelectedIndex = Convert.ToInt32(WritePower);
                }
                else
                {
                    com_wpower.SelectedIndex = Convert.ToInt32(WritePower & 0x3F);
                    rb_wp2.Checked = true;
                }
                WriteLog(lrtxtLog, "Get success ", 0);
            }
        }

        private void BtSetRetry_Click(object sender, EventArgs e)
        {
            byte RetryTime = (byte)(com_retrytimes.SelectedIndex | 0x80);
            fCmdRet = RWDev.RetryTimes(ref fComAdr, ref RetryTime, frmcomportindex);
            if (fCmdRet != 0)
                WriteLog(lrtxtLog, "Set failed： " + GetReturnCodeDesc(fCmdRet), 1);
            else
                WriteLog(lrtxtLog, "Set success", 0);
        }

        private void BtGetRetry_Click(object sender, EventArgs e)
        {
            byte Times = 0;
            fCmdRet = RWDev.RetryTimes(ref fComAdr, ref Times, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Get failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                com_retrytimes.SelectedIndex = Convert.ToInt32(Times);
                string strLog = "Get success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void RB_EPC_CheckedChanged(object sender, EventArgs e)
        {
            gbp_MixRead.Enabled = false;
            com_S.Items.Clear();
            com_S.Items.Add("0");
            com_S.Items.Add("1");
            com_S.Items.Add("2");
            com_S.Items.Add("3");
            com_S.Items.Add("Auto");
            com_S.SelectedIndex = 4;
        }

        private void RB_Mix_Click(object sender, EventArgs e)
        {

        }

        private void RB_Mix_CheckedChanged(object sender, EventArgs e)
        {
            gbp_MixRead.Enabled = true;
            com_S.Items.Clear();
            com_S.Items.Add("0");
            com_S.Items.Add("1");
            com_S.Items.Add("2");
            com_S.Items.Add("3");
            com_S.SelectedIndex = 0;
            com_MixMem.Enabled = true;
            text_readpsd.Enabled = true;
            gbp_MixRead.Text = "Mix";
        }

        private void RB_TID_CheckedChanged(object sender, EventArgs e)
        {
            gbp_MixRead.Enabled = true;
            com_S.Items.Clear();
            com_S.Items.Add("0");
            com_S.Items.Add("1");
            com_S.Items.Add("2");
            com_S.Items.Add("3");
            com_S.SelectedIndex = 0;
            com_MixMem.Enabled = false;
            text_readpsd.Enabled = false;
            gbp_MixRead.Text = "TID";
        }

        private void RB_FastId_CheckedChanged(object sender, EventArgs e)
        {
            gbp_MixRead.Enabled = false;
            com_S.Items.Clear();
            com_S.Items.Add("0");
            com_S.Items.Add("1");
            com_S.Items.Add("2");
            com_S.Items.Add("3");
            com_S.Items.Add("Auto");
            com_S.SelectedIndex = 4;
        }

        private void BtSetDRM_Click(object sender, EventArgs e)
        {
            byte DRM = 0;
            if (DRM_CLOSE.Checked) DRM = 0;
            if (DRM_OPEN.Checked) DRM = 1;
            fCmdRet = RWDev.SetDRM(ref fComAdr, DRM, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Set DRM failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                string strLog = "Set DRM success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtGetDRM_Click(object sender, EventArgs e)
        {
            byte DRM = 0;
            fCmdRet = RWDev.GetDRM(ref fComAdr, ref DRM, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Get DRM failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                if (DRM == 0) DRM_CLOSE.Checked = true;
                if (DRM == 1) DRM_OPEN.Checked = true;
                string strLog = "Get DRM success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void Check_int2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void BtGetReaderTemperature_Click(object sender, EventArgs e)
        {

            byte PlusMinus = 0;
            byte Temperature = 0;
            string temp = "";
            txtReaderTemperature.Text = "";
            fCmdRet = RWDev.GetReaderTemperature(ref fComAdr, ref PlusMinus, ref Temperature, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Get Temperature failed: " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                if (PlusMinus == 0)
                    temp = "-";
                temp += (Temperature.ToString() + "°C");
                txtReaderTemperature.Text = temp;
                string strLog = "Get Temperature success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void BtReturnLoss_Click(object sender, EventArgs e)
        {
            byte Ant = (byte)cbbAnt.SelectedIndex;
            byte ReturnLoss = 0;
            string temp = cmbReturnLossFreq.Text;

            float freq = Convert.ToSingle(Convert.ToSingle(temp) * 1000);
            int freq0 = Convert.ToInt32(freq);
            temp = Convert.ToString(freq0, 16).PadLeft(8, '0');
            byte[] TestFreq = HexStringToByteArray(temp);
            textReturnLoss.Text = "";
            fCmdRet = RWDev.MeasureReturnLoss(ref fComAdr, TestFreq, Ant, ref ReturnLoss, frmcomportindex);
            if (fCmdRet != 0)
            {
                string strLog = "Get failed:  " + GetReturnCodeDesc(fCmdRet);
                WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                textReturnLoss.Text = ReturnLoss.ToString() + "dB";
                string strLog = "Get success ";
                WriteLog(lrtxtLog, strLog, 0);
            }
        }

        private void RB_US_band3_CheckedChanged(object sender, EventArgs e)
        {
            int i;
            ComboBox_dmaxfre.Items.Clear();
            ComboBox_dminfre.Items.Clear();
            cmbReturnLossFreq.Items.Clear();
            for (i = 0; i < 53; i++)
            {
                ComboBox_dminfre.Items.Add(Convert.ToString(902 + i * 0.5) + " MHz");
                ComboBox_dmaxfre.Items.Add(Convert.ToString(902 + i * 0.5) + " MHz");
                cmbReturnLossFreq.Items.Add(Convert.ToString(902 + i * 0.5));
            }
            ComboBox_dmaxfre.SelectedIndex = 52;
            ComboBox_dminfre.SelectedIndex = 0;
            cmbReturnLossFreq.SelectedIndex = 26;
        }

        #region Form definition
        private readonly System.ComponentModel.IContainer componentManager = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (componentManager != null))
            {
                componentManager.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UHFReader288MP));
            this.lrtxtLog = new UHFReader288MPDemo.LogRichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Maintab = new System.Windows.Forms.TabControl();
            this.Main_Page1 = new System.Windows.Forms.TabPage();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_Basic = new System.Windows.Forms.TabPage();
            this.gbReturnLoss = new System.Windows.Forms.GroupBox();
            this.cbbAnt = new System.Windows.Forms.ComboBox();
            this.label120 = new System.Windows.Forms.Label();
            this.label121 = new System.Windows.Forms.Label();
            this.cmbReturnLossFreq = new System.Windows.Forms.ComboBox();
            this.label122 = new System.Windows.Forms.Label();
            this.textReturnLoss = new System.Windows.Forms.TextBox();
            this.btReturnLoss = new System.Windows.Forms.Button();
            this.gbCmdTemperature = new System.Windows.Forms.GroupBox();
            this.btnGetReaderTemperature = new System.Windows.Forms.Button();
            this.txtReaderTemperature = new System.Windows.Forms.TextBox();
            this.gbp_DRM = new System.Windows.Forms.GroupBox();
            this.DRM_CLOSE = new System.Windows.Forms.RadioButton();
            this.DRM_OPEN = new System.Windows.Forms.RadioButton();
            this.bt_GetDRM = new System.Windows.Forms.Button();
            this.bt_setDRM = new System.Windows.Forms.Button();
            this.gbp_Retry = new System.Windows.Forms.GroupBox();
            this.com_retrytimes = new System.Windows.Forms.ComboBox();
            this.bt_Getretry = new System.Windows.Forms.Button();
            this.bt_Setretry = new System.Windows.Forms.Button();
            this.gbp_wpower = new System.Windows.Forms.GroupBox();
            this.label112 = new System.Windows.Forms.Label();
            this.com_wpower = new System.Windows.Forms.ComboBox();
            this.rb_wp2 = new System.Windows.Forms.RadioButton();
            this.rb_wp1 = new System.Windows.Forms.RadioButton();
            this.bt_GetWpower = new System.Windows.Forms.Button();
            this.bt_SetWpower = new System.Windows.Forms.Button();
            this.group_maxtime = new System.Windows.Forms.GroupBox();
            this.comboBox_maxtime = new System.Windows.Forms.ComboBox();
            this.btSetMaxtime = new System.Windows.Forms.Button();
            this.gbp_buff = new System.Windows.Forms.GroupBox();
            this.rb496 = new System.Windows.Forms.RadioButton();
            this.rb128 = new System.Windows.Forms.RadioButton();
            this.btGetEPCandTIDLen = new System.Windows.Forms.Button();
            this.btSetEPCandTIDLen = new System.Windows.Forms.Button();
            this.btGetInformation = new System.Windows.Forms.Button();
            this.btDefault = new System.Windows.Forms.Button();
            this.gpb_antconfig = new System.Windows.Forms.GroupBox();
            this.Button_Ant = new System.Windows.Forms.Button();
            this.checkant4 = new System.Windows.Forms.CheckBox();
            this.checkant3 = new System.Windows.Forms.CheckBox();
            this.checkant2 = new System.Windows.Forms.CheckBox();
            this.checkant1 = new System.Windows.Forms.CheckBox();
            this.gpb_checkant = new System.Windows.Forms.GroupBox();
            this.btSetcheckant = new System.Windows.Forms.Button();
            this.rb_Closecheckant = new System.Windows.Forms.RadioButton();
            this.rb_Opencheckant = new System.Windows.Forms.RadioButton();
            this.gpb_GPIO = new System.Windows.Forms.GroupBox();
            this.Button_GetGPIO = new System.Windows.Forms.Button();
            this.Button_SetGPIO = new System.Windows.Forms.Button();
            this.check_int2 = new System.Windows.Forms.CheckBox();
            this.check_int1 = new System.Windows.Forms.CheckBox();
            this.check_out2 = new System.Windows.Forms.CheckBox();
            this.check_out1 = new System.Windows.Forms.CheckBox();
            this.gpb_OutputRep = new System.Windows.Forms.GroupBox();
            this.btOutputRep = new System.Windows.Forms.Button();
            this.check_OutputRep4 = new System.Windows.Forms.CheckBox();
            this.check_OutputRep3 = new System.Windows.Forms.CheckBox();
            this.check_OutputRep2 = new System.Windows.Forms.CheckBox();
            this.check_OutputRep1 = new System.Windows.Forms.CheckBox();
            this.gpb_Relay = new System.Windows.Forms.GroupBox();
            this.label38 = new System.Windows.Forms.Label();
            this.ComboBox_RelayTime = new System.Windows.Forms.ComboBox();
            this.label37 = new System.Windows.Forms.Label();
            this.btRelay = new System.Windows.Forms.Button();
            this.gpb_baud = new System.Windows.Forms.GroupBox();
            this.ComboBox_baud = new System.Windows.Forms.ComboBox();
            this.btBaudRate = new System.Windows.Forms.Button();
            this.gpb_DBM = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.ComboBox_PowerDbm = new System.Windows.Forms.ComboBox();
            this.BT_DBM = new System.Windows.Forms.Button();
            this.gpb_Freq = new System.Windows.Forms.GroupBox();
            this.radioButton_band12 = new System.Windows.Forms.RadioButton();
            this.radioButton_band8 = new System.Windows.Forms.RadioButton();
            this.CheckBox_SameFre = new System.Windows.Forms.CheckBox();
            this.radioButton_band4 = new System.Windows.Forms.RadioButton();
            this.radioButton_band3 = new System.Windows.Forms.RadioButton();
            this.radioButton_band2 = new System.Windows.Forms.RadioButton();
            this.radioButton_band1 = new System.Windows.Forms.RadioButton();
            this.btFreq = new System.Windows.Forms.Button();
            this.ComboBox_dmaxfre = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.ComboBox_dminfre = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.gpb_beep = new System.Windows.Forms.GroupBox();
            this.Radio_beepDis = new System.Windows.Forms.RadioButton();
            this.Radio_beepEn = new System.Windows.Forms.RadioButton();
            this.Button_Beep = new System.Windows.Forms.Button();
            this.gpb_RDVersion = new System.Windows.Forms.GroupBox();
            this.text_RDVersion = new System.Windows.Forms.TextBox();
            this.gpb_Serial = new System.Windows.Forms.GroupBox();
            this.TextBox_Serial = new System.Windows.Forms.TextBox();
            this.btSerial = new System.Windows.Forms.Button();
            this.gpb_address = new System.Windows.Forms.GroupBox();
            this.text_address = new System.Windows.Forms.TextBox();
            this.btaddress = new System.Windows.Forms.Button();
            this.gpb_tcp = new System.Windows.Forms.GroupBox();
            this.tb_Port = new System.Windows.Forms.TextBox();
            this.ipServerIP = new UHFReader288MPDemo.IpAddressTextBox();
            this.btDisConnectTcp = new System.Windows.Forms.Button();
            this.btConnectTcp = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.gpb_rs232 = new System.Windows.Forms.GroupBox();
            this.btDisConnect232 = new System.Windows.Forms.Button();
            this.BtConnect232 = new System.Windows.Forms.Button();
            this.ComboBox_baud2 = new System.Windows.Forms.ComboBox();
            this.label47 = new System.Windows.Forms.Label();
            this.ComboBox_COM = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rb_tcp = new System.Windows.Forms.RadioButton();
            this.rb_rs232 = new System.Windows.Forms.RadioButton();
            this.tabPage_Module = new System.Windows.Forms.TabPage();
            this.panel9 = new System.Windows.Forms.Panel();
            this.btMSetParameter = new System.Windows.Forms.Button();
            this.btMGetParameter = new System.Windows.Forms.Button();
            this.groupBox39 = new System.Windows.Forms.GroupBox();
            this.btSetMmode = new System.Windows.Forms.Button();
            this.com_Mmode = new System.Windows.Forms.ComboBox();
            this.label111 = new System.Windows.Forms.Label();
            this.groupBox38 = new System.Windows.Forms.GroupBox();
            this.checkBox_tid = new System.Windows.Forms.CheckBox();
            this.txt_Mtidlen = new System.Windows.Forms.TextBox();
            this.label103 = new System.Windows.Forms.Label();
            this.txt_mtidaddr = new System.Windows.Forms.TextBox();
            this.label104 = new System.Windows.Forms.Label();
            this.groupBox34 = new System.Windows.Forms.GroupBox();
            this.checkBox_mask = new System.Windows.Forms.CheckBox();
            this.txt_Mdata = new System.Windows.Forms.TextBox();
            this.label100 = new System.Windows.Forms.Label();
            this.txt_Mlen = new System.Windows.Forms.TextBox();
            this.label101 = new System.Windows.Forms.Label();
            this.txt_Maddr = new System.Windows.Forms.TextBox();
            this.label102 = new System.Windows.Forms.Label();
            this.RBM_USER = new System.Windows.Forms.RadioButton();
            this.RBM_TID = new System.Windows.Forms.RadioButton();
            this.RBM_EPC = new System.Windows.Forms.RadioButton();
            this.groupBox33 = new System.Windows.Forms.GroupBox();
            this.com_MS = new System.Windows.Forms.ComboBox();
            this.label99 = new System.Windows.Forms.Label();
            this.com_MQ = new System.Windows.Forms.ComboBox();
            this.label98 = new System.Windows.Forms.Label();
            this.groupBox32 = new System.Windows.Forms.GroupBox();
            this.com_MFliterTime = new System.Windows.Forms.ComboBox();
            this.label97 = new System.Windows.Forms.Label();
            this.groupBox28 = new System.Windows.Forms.GroupBox();
            this.com_MRPTime = new System.Windows.Forms.ComboBox();
            this.label96 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.MRB_6B = new System.Windows.Forms.RadioButton();
            this.MRB_G2 = new System.Windows.Forms.RadioButton();
            this.Main_Page2 = new System.Windows.Forms.TabPage();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage_answer = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lxLedControl6 = new UHFReader288MPDemo.LxLedControl();
            this.label119 = new System.Windows.Forms.Label();
            this.gbp_MixRead = new System.Windows.Forms.GroupBox();
            this.text_readpsd = new System.Windows.Forms.TextBox();
            this.label118 = new System.Windows.Forms.Label();
            this.text_readLen = new System.Windows.Forms.TextBox();
            this.label117 = new System.Windows.Forms.Label();
            this.text_readadr = new System.Windows.Forms.TextBox();
            this.label115 = new System.Windows.Forms.Label();
            this.com_MixMem = new System.Windows.Forms.ComboBox();
            this.label114 = new System.Windows.Forms.Label();
            this.lxLedControl5 = new UHFReader288MPDemo.LxLedControl();
            this.label9 = new System.Windows.Forms.Label();
            this.lxLedControl4 = new UHFReader288MPDemo.LxLedControl();
            this.label8 = new System.Windows.Forms.Label();
            this.lxLedControl3 = new UHFReader288MPDemo.LxLedControl();
            this.label7 = new System.Windows.Forms.Label();
            this.lxLedControl2 = new UHFReader288MPDemo.LxLedControl();
            this.label6 = new System.Windows.Forms.Label();
            this.lxLedControl1 = new UHFReader288MPDemo.LxLedControl();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBox_rate = new System.Windows.Forms.CheckBox();
            this.label80 = new System.Windows.Forms.Label();
            this.text_target = new System.Windows.Forms.TextBox();
            this.check_num = new System.Windows.Forms.CheckBox();
            this.com_Target = new System.Windows.Forms.ComboBox();
            this.label79 = new System.Windows.Forms.Label();
            this.com_scantime = new System.Windows.Forms.ComboBox();
            this.label39 = new System.Windows.Forms.Label();
            this.com_S = new System.Windows.Forms.ComboBox();
            this.label32 = new System.Windows.Forms.Label();
            this.com_Q = new System.Windows.Forms.ComboBox();
            this.label31 = new System.Windows.Forms.Label();
            this.group_ant1 = new System.Windows.Forms.GroupBox();
            this.check_ant4 = new System.Windows.Forms.CheckBox();
            this.check_ant3 = new System.Windows.Forms.CheckBox();
            this.check_ant2 = new System.Windows.Forms.CheckBox();
            this.check_ant1 = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rb_fastid = new System.Windows.Forms.RadioButton();
            this.rb_mix = new System.Windows.Forms.RadioButton();
            this.rb_tid = new System.Windows.Forms.RadioButton();
            this.rb_epc = new System.Windows.Forms.RadioButton();
            this.btIventoryG2 = new System.Windows.Forms.Button();
            this.tabPage_Realtime = new System.Windows.Forms.TabPage();
            this.panel10 = new System.Windows.Forms.Panel();
            this.dataGridView4 = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox41 = new System.Windows.Forms.GroupBox();
            this.lxLed_Mtime = new UHFReader288MPDemo.LxLedControl();
            this.label113 = new System.Windows.Forms.Label();
            this.lxLed_Mtag = new UHFReader288MPDemo.LxLedControl();
            this.label116 = new System.Windows.Forms.Label();
            this.groupBox42 = new System.Windows.Forms.GroupBox();
            this.btStartMactive = new System.Windows.Forms.Button();
            this.tabPage_Buff = new System.Windows.Forms.TabPage();
            this.panel8 = new System.Windows.Forms.Panel();
            this.groupBox27 = new System.Windows.Forms.GroupBox();
            this.btQueryBuffNum = new System.Windows.Forms.Button();
            this.btRandCbuff = new System.Windows.Forms.Button();
            this.btClearBuff = new System.Windows.Forms.Button();
            this.btReadBuff = new System.Windows.Forms.Button();
            this.groupBox29 = new System.Windows.Forms.GroupBox();
            this.rb_btid = new System.Windows.Forms.RadioButton();
            this.rb_bepc = new System.Windows.Forms.RadioButton();
            this.btStartBuff = new System.Windows.Forms.Button();
            this.dataGridView3 = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox30 = new System.Windows.Forms.GroupBox();
            this.lxLed_cmdTime = new UHFReader288MPDemo.LxLedControl();
            this.label86 = new System.Windows.Forms.Label();
            this.lxLed_Btoltime = new UHFReader288MPDemo.LxLedControl();
            this.label83 = new System.Windows.Forms.Label();
            this.label91 = new System.Windows.Forms.Label();
            this.lxLed_Btoltag = new UHFReader288MPDemo.LxLedControl();
            this.label92 = new System.Windows.Forms.Label();
            this.lxLed_Bcmdsud = new UHFReader288MPDemo.LxLedControl();
            this.label94 = new System.Windows.Forms.Label();
            this.lxLed_BNum = new UHFReader288MPDemo.LxLedControl();
            this.label95 = new System.Windows.Forms.Label();
            this.tabPage_RW = new System.Windows.Forms.TabPage();
            this.panel4 = new System.Windows.Forms.Panel();
            this.groupBox21 = new System.Windows.Forms.GroupBox();
            this.btCheckReadProtected_G2 = new System.Windows.Forms.Button();
            this.btRemoveReadProtect_G2 = new System.Windows.Forms.Button();
            this.btSetMultiReadProtect_G2 = new System.Windows.Forms.Button();
            this.btSetReadProtect_G2 = new System.Windows.Forms.Button();
            this.text_AccessCode4 = new System.Windows.Forms.TextBox();
            this.label34 = new System.Windows.Forms.Label();
            this.groupBox20 = new System.Windows.Forms.GroupBox();
            this.btWriteEPC_G2 = new System.Windows.Forms.Button();
            this.text_AccessCode3 = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.text_WriteEPC = new System.Windows.Forms.TextBox();
            this.label33 = new System.Windows.Forms.Label();
            this.groupBox18 = new System.Windows.Forms.GroupBox();
            this.btDestroyCard = new System.Windows.Forms.Button();
            this.text_DestroyCode = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.groupBox16 = new System.Windows.Forms.GroupBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btCheckEASAlarm = new System.Windows.Forms.Button();
            this.btSetEASAlarm_G2 = new System.Windows.Forms.Button();
            this.groupBox17 = new System.Windows.Forms.GroupBox();
            this.NoAlarm_G2 = new System.Windows.Forms.RadioButton();
            this.Alarm_G2 = new System.Windows.Forms.RadioButton();
            this.text_AccessCode5 = new System.Windows.Forms.TextBox();
            this.label28 = new System.Windows.Forms.Label();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.btSetProtectState = new System.Windows.Forms.Button();
            this.Edit_AccessCode6 = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.groupBox14 = new System.Windows.Forms.GroupBox();
            this.AlwaysNot2 = new System.Windows.Forms.RadioButton();
            this.Always2 = new System.Windows.Forms.RadioButton();
            this.Proect2 = new System.Windows.Forms.RadioButton();
            this.NoProect2 = new System.Windows.Forms.RadioButton();
            this.groupBox15 = new System.Windows.Forms.GroupBox();
            this.p_pass = new System.Windows.Forms.RadioButton();
            this.P_User = new System.Windows.Forms.RadioButton();
            this.P_TID = new System.Windows.Forms.RadioButton();
            this.P_EPC = new System.Windows.Forms.RadioButton();
            this.P_kill = new System.Windows.Forms.RadioButton();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.btExtWrite = new System.Windows.Forms.Button();
            this.btExtRead = new System.Windows.Forms.Button();
            this.textBox_pc = new System.Windows.Forms.TextBox();
            this.checkBox_pc = new System.Windows.Forms.CheckBox();
            this.btBlockWrite = new System.Windows.Forms.Button();
            this.btBlockErase = new System.Windows.Forms.Button();
            this.btWrite = new System.Windows.Forms.Button();
            this.btRead = new System.Windows.Forms.Button();
            this.text_WriteData = new System.Windows.Forms.TextBox();
            this.text_AccessCode2 = new System.Windows.Forms.TextBox();
            this.text_RWlen = new System.Windows.Forms.TextBox();
            this.text_WordPtr = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.groupBox12 = new System.Windows.Forms.GroupBox();
            this.C_User = new System.Windows.Forms.RadioButton();
            this.C_TID = new System.Windows.Forms.RadioButton();
            this.C_EPC = new System.Windows.Forms.RadioButton();
            this.C_Reserve = new System.Windows.Forms.RadioButton();
            this.groupBox31 = new System.Windows.Forms.GroupBox();
            this.maskData_textBox = new System.Windows.Forms.TextBox();
            this.label60 = new System.Windows.Forms.Label();
            this.groupBox40 = new System.Windows.Forms.GroupBox();
            this.R_User = new System.Windows.Forms.RadioButton();
            this.R_TID = new System.Windows.Forms.RadioButton();
            this.R_EPC = new System.Windows.Forms.RadioButton();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.maskLen_textBox = new System.Windows.Forms.TextBox();
            this.label44 = new System.Windows.Forms.Label();
            this.maskadr_textbox = new System.Windows.Forms.TextBox();
            this.label43 = new System.Windows.Forms.Label();
            this.groupBox10 = new System.Windows.Forms.GroupBox();
            this.btSelectTag = new System.Windows.Forms.Button();
            this.comboBox_EPC = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.text_epc = new System.Windows.Forms.TextBox();
            this.check_selecttag = new System.Windows.Forms.CheckBox();
            this.Main_Page3 = new System.Windows.Forms.TabPage();
            this.panel5 = new System.Windows.Forms.Panel();
            this.groupBox23 = new System.Windows.Forms.GroupBox();
            this.text_Statu6B = new System.Windows.Forms.TextBox();
            this.btCheckLock6B = new System.Windows.Forms.Button();
            this.btLock6B = new System.Windows.Forms.Button();
            this.text_checkaddr = new System.Windows.Forms.TextBox();
            this.label52 = new System.Windows.Forms.Label();
            this.text_lock6b = new System.Windows.Forms.TextBox();
            this.label51 = new System.Windows.Forms.Label();
            this.groupBox22 = new System.Windows.Forms.GroupBox();
            this.text_W6B = new System.Windows.Forms.TextBox();
            this.label48 = new System.Windows.Forms.Label();
            this.btWrite6B = new System.Windows.Forms.Button();
            this.text_W6BLen = new System.Windows.Forms.TextBox();
            this.label49 = new System.Windows.Forms.Label();
            this.text_W6BAddr = new System.Windows.Forms.TextBox();
            this.label50 = new System.Windows.Forms.Label();
            this.text_R6B = new System.Windows.Forms.TextBox();
            this.label36 = new System.Windows.Forms.Label();
            this.btRead6B = new System.Windows.Forms.Button();
            this.text_R6BLen = new System.Windows.Forms.TextBox();
            this.label35 = new System.Windows.Forms.Label();
            this.text_R6BAddr = new System.Windows.Forms.TextBox();
            this.label30 = new System.Windows.Forms.Label();
            this.text_6BUID = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.ListView_ID_6B = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox19 = new System.Windows.Forms.GroupBox();
            this.rb_mutiple = new System.Windows.Forms.RadioButton();
            this.rb_single = new System.Windows.Forms.RadioButton();
            this.btInventory6B = new System.Windows.Forms.Button();
            this.Main_Page4 = new System.Windows.Forms.TabPage();
            this.tabControl3 = new System.Windows.Forms.TabControl();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.DeviceListView = new System.Windows.Forms.ListView();
            this.deviceName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.deviceIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.deviceMac = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.iEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.telnetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.languageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.englishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabPage8 = new System.Windows.Forms.TabPage();
            this.label63 = new System.Windows.Forms.Label();
            this.stcprecv = new UHFReader288MPDemo.LogRichTextBox();
            this.groupBox25 = new System.Windows.Forms.GroupBox();
            this.listtcp = new System.Windows.Forms.ListBox();
            this.groupBox24 = new System.Windows.Forms.GroupBox();
            this.btStop = new System.Windows.Forms.Button();
            this.btListen = new System.Windows.Forms.Button();
            this.stcpport = new System.Windows.Forms.TextBox();
            this.label62 = new System.Windows.Forms.Label();
            this.tabPage9 = new System.Windows.Forms.TabPage();
            this.groupBox26 = new System.Windows.Forms.GroupBox();
            this.bttcpdisconnect = new System.Windows.Forms.Button();
            this.bttcpconnect = new System.Windows.Forms.Button();
            this.remotePort = new System.Windows.Forms.TextBox();
            this.label71 = new System.Windows.Forms.Label();
            this.tcpremoteIp = new UHFReader288MPDemo.IpAddressTextBox();
            this.label70 = new System.Windows.Forms.Label();
            this.bttcpsend = new System.Windows.Forms.Button();
            this.label69 = new System.Windows.Forms.Label();
            this.ctctsend = new System.Windows.Forms.TextBox();
            this.label68 = new System.Windows.Forms.Label();
            this.ctctrecv = new UHFReader288MPDemo.LogRichTextBox();
            this.btFlashCl = new System.Windows.Forms.Button();
            this.timer_answer = new System.Windows.Forms.Timer(this.components);
            this.timer_EAS = new System.Windows.Forms.Timer(this.components);
            this.Timer_Test_6B = new System.Windows.Forms.Timer(this.components);
            this.timer_Buff = new System.Windows.Forms.Timer(this.components);
            this.timer_RealTime = new System.Windows.Forms.Timer(this.components);
            this.ckClearOperationRec = new System.Windows.Forms.CheckBox();
            this.Maintab.SuspendLayout();
            this.Main_Page1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage_Basic.SuspendLayout();
            this.gbReturnLoss.SuspendLayout();
            this.gbCmdTemperature.SuspendLayout();
            this.gbp_DRM.SuspendLayout();
            this.gbp_Retry.SuspendLayout();
            this.gbp_wpower.SuspendLayout();
            this.group_maxtime.SuspendLayout();
            this.gbp_buff.SuspendLayout();
            this.gpb_antconfig.SuspendLayout();
            this.gpb_checkant.SuspendLayout();
            this.gpb_GPIO.SuspendLayout();
            this.gpb_OutputRep.SuspendLayout();
            this.gpb_Relay.SuspendLayout();
            this.gpb_baud.SuspendLayout();
            this.gpb_DBM.SuspendLayout();
            this.gpb_Freq.SuspendLayout();
            this.gpb_beep.SuspendLayout();
            this.gpb_RDVersion.SuspendLayout();
            this.gpb_Serial.SuspendLayout();
            this.gpb_address.SuspendLayout();
            this.gpb_tcp.SuspendLayout();
            this.gpb_rs232.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage_Module.SuspendLayout();
            this.panel9.SuspendLayout();
            this.groupBox39.SuspendLayout();
            this.groupBox38.SuspendLayout();
            this.groupBox34.SuspendLayout();
            this.groupBox33.SuspendLayout();
            this.groupBox32.SuspendLayout();
            this.groupBox28.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.Main_Page2.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage_answer.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl6)).BeginInit();
            this.gbp_MixRead.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl1)).BeginInit();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.group_ant1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage_Realtime.SuspendLayout();
            this.panel10.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).BeginInit();
            this.groupBox41.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Mtime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Mtag)).BeginInit();
            this.groupBox42.SuspendLayout();
            this.tabPage_Buff.SuspendLayout();
            this.panel8.SuspendLayout();
            this.groupBox27.SuspendLayout();
            this.groupBox29.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).BeginInit();
            this.groupBox30.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_cmdTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Btoltime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Btoltag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Bcmdsud)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_BNum)).BeginInit();
            this.tabPage_RW.SuspendLayout();
            this.panel4.SuspendLayout();
            this.groupBox21.SuspendLayout();
            this.groupBox20.SuspendLayout();
            this.groupBox18.SuspendLayout();
            this.groupBox16.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.groupBox17.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.groupBox14.SuspendLayout();
            this.groupBox15.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.groupBox12.SuspendLayout();
            this.groupBox31.SuspendLayout();
            this.groupBox40.SuspendLayout();
            this.groupBox10.SuspendLayout();
            this.Main_Page3.SuspendLayout();
            this.panel5.SuspendLayout();
            this.groupBox23.SuspendLayout();
            this.groupBox22.SuspendLayout();
            this.groupBox19.SuspendLayout();
            this.Main_Page4.SuspendLayout();
            this.tabControl3.SuspendLayout();
            this.tabPage6.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.tabPage8.SuspendLayout();
            this.groupBox25.SuspendLayout();
            this.groupBox24.SuspendLayout();
            this.tabPage9.SuspendLayout();
            this.groupBox26.SuspendLayout();
            this.SuspendLayout();
            // 
            // lrtxtLog
            // 
            this.lrtxtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lrtxtLog.EnableAutoDragDrop = true;
            this.lrtxtLog.Location = new System.Drawing.Point(-1, 685);
            this.lrtxtLog.Name = "lrtxtLog";
            this.lrtxtLog.Size = new System.Drawing.Size(1080, 96);
            this.lrtxtLog.TabIndex = 0;
            this.lrtxtLog.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 659);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Operation records:";
            // 
            // Maintab
            // 
            this.Maintab.Controls.Add(this.Main_Page1);
            this.Maintab.Controls.Add(this.Main_Page2);
            this.Maintab.Controls.Add(this.Main_Page3);
            this.Maintab.Controls.Add(this.Main_Page4);
            this.Maintab.Dock = System.Windows.Forms.DockStyle.Top;
            this.Maintab.ItemSize = new System.Drawing.Size(72, 23);
            this.Maintab.Location = new System.Drawing.Point(0, 0);
            this.Maintab.Multiline = true;
            this.Maintab.Name = "Maintab";
            this.Maintab.SelectedIndex = 0;
            this.Maintab.Size = new System.Drawing.Size(1079, 652);
            this.Maintab.TabIndex = 3;
            this.Maintab.SelectedIndexChanged += new System.EventHandler(this.Maintab_SelectedIndexChanged);
            // 
            // Main_Page1
            // 
            this.Main_Page1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Main_Page1.Controls.Add(this.tabControl1);
            this.Main_Page1.Location = new System.Drawing.Point(4, 27);
            this.Main_Page1.Name = "Main_Page1";
            this.Main_Page1.Padding = new System.Windows.Forms.Padding(3);
            this.Main_Page1.Size = new System.Drawing.Size(1071, 621);
            this.Main_Page1.TabIndex = 0;
            this.Main_Page1.Text = "   Command  ";
            this.Main_Page1.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_Basic);
            this.tabControl1.Controls.Add(this.tabPage_Module);
            this.tabControl1.HotTrack = true;
            this.tabControl1.ItemSize = new System.Drawing.Size(84, 23);
            this.tabControl1.Location = new System.Drawing.Point(1, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1065, 615);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.Maintab_SelectedIndexChanged);
            // 
            // tabPage_Basic
            // 
            this.tabPage_Basic.Controls.Add(this.gbReturnLoss);
            this.tabPage_Basic.Controls.Add(this.gbCmdTemperature);
            this.tabPage_Basic.Controls.Add(this.gbp_DRM);
            this.tabPage_Basic.Controls.Add(this.gbp_Retry);
            this.tabPage_Basic.Controls.Add(this.gbp_wpower);
            this.tabPage_Basic.Controls.Add(this.group_maxtime);
            this.tabPage_Basic.Controls.Add(this.gbp_buff);
            this.tabPage_Basic.Controls.Add(this.btGetInformation);
            this.tabPage_Basic.Controls.Add(this.btDefault);
            this.tabPage_Basic.Controls.Add(this.gpb_antconfig);
            this.tabPage_Basic.Controls.Add(this.gpb_checkant);
            this.tabPage_Basic.Controls.Add(this.gpb_GPIO);
            this.tabPage_Basic.Controls.Add(this.gpb_OutputRep);
            this.tabPage_Basic.Controls.Add(this.gpb_Relay);
            this.tabPage_Basic.Controls.Add(this.gpb_baud);
            this.tabPage_Basic.Controls.Add(this.gpb_DBM);
            this.tabPage_Basic.Controls.Add(this.gpb_Freq);
            this.tabPage_Basic.Controls.Add(this.gpb_beep);
            this.tabPage_Basic.Controls.Add(this.gpb_RDVersion);
            this.tabPage_Basic.Controls.Add(this.gpb_Serial);
            this.tabPage_Basic.Controls.Add(this.gpb_address);
            this.tabPage_Basic.Controls.Add(this.gpb_tcp);
            this.tabPage_Basic.Controls.Add(this.gpb_rs232);
            this.tabPage_Basic.Controls.Add(this.groupBox1);
            this.tabPage_Basic.Location = new System.Drawing.Point(4, 27);
            this.tabPage_Basic.Name = "tabPage_Basic";
            this.tabPage_Basic.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Basic.Size = new System.Drawing.Size(1057, 584);
            this.tabPage_Basic.TabIndex = 0;
            this.tabPage_Basic.Text = "  Basic  ";
            this.tabPage_Basic.UseVisualStyleBackColor = true;
            // 
            // gbReturnLoss
            // 
            this.gbReturnLoss.BackColor = System.Drawing.Color.Transparent;
            this.gbReturnLoss.Controls.Add(this.cbbAnt);
            this.gbReturnLoss.Controls.Add(this.label120);
            this.gbReturnLoss.Controls.Add(this.label121);
            this.gbReturnLoss.Controls.Add(this.cmbReturnLossFreq);
            this.gbReturnLoss.Controls.Add(this.label122);
            this.gbReturnLoss.Controls.Add(this.textReturnLoss);
            this.gbReturnLoss.Controls.Add(this.btReturnLoss);
            this.gbReturnLoss.ForeColor = System.Drawing.Color.Black;
            this.gbReturnLoss.Location = new System.Drawing.Point(704, 365);
            this.gbReturnLoss.Name = "gbReturnLoss";
            this.gbReturnLoss.Size = new System.Drawing.Size(341, 50);
            this.gbReturnLoss.TabIndex = 77;
            this.gbReturnLoss.TabStop = false;
            this.gbReturnLoss.Text = "Measuring antenna ports Return Loss";
            // 
            // cbbAnt
            // 
            this.cbbAnt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbAnt.FormattingEnabled = true;
            this.cbbAnt.Items.AddRange(new object[] {
            "Ant1",
            "Ant2",
            "Ant3",
            "Ant4"});
            this.cbbAnt.Location = new System.Drawing.Point(198, 20);
            this.cbbAnt.Name = "cbbAnt";
            this.cbbAnt.Size = new System.Drawing.Size(70, 21);
            this.cbbAnt.TabIndex = 16;
            // 
            // label120
            // 
            this.label120.AutoSize = true;
            this.label120.Location = new System.Drawing.Point(86, 24);
            this.label120.Name = "label120";
            this.label120.Size = new System.Drawing.Size(18, 13);
            this.label120.TabIndex = 15;
            this.label120.Text = "@";
            // 
            // label121
            // 
            this.label121.AutoSize = true;
            this.label121.Location = new System.Drawing.Point(170, 24);
            this.label121.Name = "label121";
            this.label121.Size = new System.Drawing.Size(29, 13);
            this.label121.TabIndex = 14;
            this.label121.Text = "MHz";
            // 
            // cmbReturnLossFreq
            // 
            this.cmbReturnLossFreq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReturnLossFreq.FormattingEnabled = true;
            this.cmbReturnLossFreq.Location = new System.Drawing.Point(97, 20);
            this.cmbReturnLossFreq.Name = "cmbReturnLossFreq";
            this.cmbReturnLossFreq.Size = new System.Drawing.Size(71, 21);
            this.cmbReturnLossFreq.TabIndex = 13;
            // 
            // label122
            // 
            this.label122.AutoSize = true;
            this.label122.Location = new System.Drawing.Point(11, 24);
            this.label122.Name = "label122";
            this.label122.Size = new System.Drawing.Size(24, 13);
            this.label122.TabIndex = 12;
            this.label122.Text = "RL:";
            // 
            // textReturnLoss
            // 
            this.textReturnLoss.Location = new System.Drawing.Point(40, 20);
            this.textReturnLoss.Name = "textReturnLoss";
            this.textReturnLoss.ReadOnly = true;
            this.textReturnLoss.Size = new System.Drawing.Size(44, 20);
            this.textReturnLoss.TabIndex = 11;
            this.textReturnLoss.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btReturnLoss
            // 
            this.btReturnLoss.Location = new System.Drawing.Point(276, 16);
            this.btReturnLoss.Name = "btReturnLoss";
            this.btReturnLoss.Size = new System.Drawing.Size(55, 25);
            this.btReturnLoss.TabIndex = 10;
            this.btReturnLoss.Text = "Get";
            this.btReturnLoss.UseVisualStyleBackColor = true;
            this.btReturnLoss.Click += new System.EventHandler(this.BtReturnLoss_Click);
            // 
            // gbCmdTemperature
            // 
            this.gbCmdTemperature.Controls.Add(this.btnGetReaderTemperature);
            this.gbCmdTemperature.Controls.Add(this.txtReaderTemperature);
            this.gbCmdTemperature.ForeColor = System.Drawing.Color.Black;
            this.gbCmdTemperature.Location = new System.Drawing.Point(702, 302);
            this.gbCmdTemperature.Name = "gbCmdTemperature";
            this.gbCmdTemperature.Size = new System.Drawing.Size(342, 53);
            this.gbCmdTemperature.TabIndex = 76;
            this.gbCmdTemperature.TabStop = false;
            this.gbCmdTemperature.Text = "Working temperature monitoring";
            // 
            // btnGetReaderTemperature
            // 
            this.btnGetReaderTemperature.Location = new System.Drawing.Point(233, 20);
            this.btnGetReaderTemperature.Name = "btnGetReaderTemperature";
            this.btnGetReaderTemperature.Size = new System.Drawing.Size(90, 25);
            this.btnGetReaderTemperature.TabIndex = 0;
            this.btnGetReaderTemperature.Text = "Get";
            this.btnGetReaderTemperature.UseVisualStyleBackColor = true;
            this.btnGetReaderTemperature.Click += new System.EventHandler(this.BtGetReaderTemperature_Click);
            // 
            // txtReaderTemperature
            // 
            this.txtReaderTemperature.Location = new System.Drawing.Point(6, 22);
            this.txtReaderTemperature.Name = "txtReaderTemperature";
            this.txtReaderTemperature.ReadOnly = true;
            this.txtReaderTemperature.Size = new System.Drawing.Size(120, 20);
            this.txtReaderTemperature.TabIndex = 1;
            this.txtReaderTemperature.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // gbp_DRM
            // 
            this.gbp_DRM.Controls.Add(this.DRM_CLOSE);
            this.gbp_DRM.Controls.Add(this.DRM_OPEN);
            this.gbp_DRM.Controls.Add(this.bt_GetDRM);
            this.gbp_DRM.Controls.Add(this.bt_setDRM);
            this.gbp_DRM.Location = new System.Drawing.Point(355, 467);
            this.gbp_DRM.Name = "gbp_DRM";
            this.gbp_DRM.Size = new System.Drawing.Size(343, 52);
            this.gbp_DRM.TabIndex = 75;
            this.gbp_DRM.TabStop = false;
            this.gbp_DRM.Text = "DRM status";
            // 
            // DRM_CLOSE
            // 
            this.DRM_CLOSE.AutoSize = true;
            this.DRM_CLOSE.Location = new System.Drawing.Point(103, 22);
            this.DRM_CLOSE.Name = "DRM_CLOSE";
            this.DRM_CLOSE.Size = new System.Drawing.Size(51, 17);
            this.DRM_CLOSE.TabIndex = 12;
            this.DRM_CLOSE.Text = "Close";
            this.DRM_CLOSE.UseVisualStyleBackColor = true;
            // 
            // DRM_OPEN
            // 
            this.DRM_OPEN.AutoSize = true;
            this.DRM_OPEN.Location = new System.Drawing.Point(13, 22);
            this.DRM_OPEN.Name = "DRM_OPEN";
            this.DRM_OPEN.Size = new System.Drawing.Size(51, 17);
            this.DRM_OPEN.TabIndex = 11;
            this.DRM_OPEN.Text = "Open";
            this.DRM_OPEN.UseVisualStyleBackColor = true;
            // 
            // bt_GetDRM
            // 
            this.bt_GetDRM.Location = new System.Drawing.Point(247, 18);
            this.bt_GetDRM.Name = "bt_GetDRM";
            this.bt_GetDRM.Size = new System.Drawing.Size(77, 25);
            this.bt_GetDRM.TabIndex = 9;
            this.bt_GetDRM.Text = "Get";
            this.bt_GetDRM.UseVisualStyleBackColor = true;
            this.bt_GetDRM.Click += new System.EventHandler(this.BtGetDRM_Click);
            // 
            // bt_setDRM
            // 
            this.bt_setDRM.Location = new System.Drawing.Point(158, 18);
            this.bt_setDRM.Name = "bt_setDRM";
            this.bt_setDRM.Size = new System.Drawing.Size(77, 25);
            this.bt_setDRM.TabIndex = 8;
            this.bt_setDRM.Text = "Set";
            this.bt_setDRM.UseVisualStyleBackColor = true;
            this.bt_setDRM.Click += new System.EventHandler(this.BtSetDRM_Click);
            // 
            // gbp_Retry
            // 
            this.gbp_Retry.Controls.Add(this.com_retrytimes);
            this.gbp_Retry.Controls.Add(this.bt_Getretry);
            this.gbp_Retry.Controls.Add(this.bt_Setretry);
            this.gbp_Retry.Location = new System.Drawing.Point(354, 410);
            this.gbp_Retry.Name = "gbp_Retry";
            this.gbp_Retry.Size = new System.Drawing.Size(343, 52);
            this.gbp_Retry.TabIndex = 74;
            this.gbp_Retry.TabStop = false;
            this.gbp_Retry.Text = "Retry write times set and get";
            // 
            // com_retrytimes
            // 
            this.com_retrytimes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_retrytimes.FormattingEnabled = true;
            this.com_retrytimes.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7"});
            this.com_retrytimes.Location = new System.Drawing.Point(14, 21);
            this.com_retrytimes.Name = "com_retrytimes";
            this.com_retrytimes.Size = new System.Drawing.Size(100, 21);
            this.com_retrytimes.TabIndex = 26;
            // 
            // bt_Getretry
            // 
            this.bt_Getretry.Location = new System.Drawing.Point(247, 18);
            this.bt_Getretry.Name = "bt_Getretry";
            this.bt_Getretry.Size = new System.Drawing.Size(77, 25);
            this.bt_Getretry.TabIndex = 9;
            this.bt_Getretry.Text = "Get";
            this.bt_Getretry.UseVisualStyleBackColor = true;
            this.bt_Getretry.Click += new System.EventHandler(this.BtGetRetry_Click);
            // 
            // bt_Setretry
            // 
            this.bt_Setretry.Location = new System.Drawing.Point(158, 18);
            this.bt_Setretry.Name = "bt_Setretry";
            this.bt_Setretry.Size = new System.Drawing.Size(77, 25);
            this.bt_Setretry.TabIndex = 8;
            this.bt_Setretry.Text = "Set";
            this.bt_Setretry.UseVisualStyleBackColor = true;
            this.bt_Setretry.Click += new System.EventHandler(this.BtSetRetry_Click);
            // 
            // gbp_wpower
            // 
            this.gbp_wpower.Controls.Add(this.label112);
            this.gbp_wpower.Controls.Add(this.com_wpower);
            this.gbp_wpower.Controls.Add(this.rb_wp2);
            this.gbp_wpower.Controls.Add(this.rb_wp1);
            this.gbp_wpower.Controls.Add(this.bt_GetWpower);
            this.gbp_wpower.Controls.Add(this.bt_SetWpower);
            this.gbp_wpower.Location = new System.Drawing.Point(6, 425);
            this.gbp_wpower.Name = "gbp_wpower";
            this.gbp_wpower.Size = new System.Drawing.Size(343, 74);
            this.gbp_wpower.TabIndex = 73;
            this.gbp_wpower.TabStop = false;
            this.gbp_wpower.Text = "Write Power Set and get";
            // 
            // label112
            // 
            this.label112.AutoSize = true;
            this.label112.Location = new System.Drawing.Point(120, 48);
            this.label112.Name = "label112";
            this.label112.Size = new System.Drawing.Size(28, 13);
            this.label112.TabIndex = 27;
            this.label112.Text = "dBm";
            // 
            // com_wpower
            // 
            this.com_wpower.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_wpower.FormattingEnabled = true;
            this.com_wpower.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30"});
            this.com_wpower.Location = new System.Drawing.Point(14, 44);
            this.com_wpower.Name = "com_wpower";
            this.com_wpower.Size = new System.Drawing.Size(100, 21);
            this.com_wpower.TabIndex = 26;
            // 
            // rb_wp2
            // 
            this.rb_wp2.AutoSize = true;
            this.rb_wp2.Location = new System.Drawing.Point(157, 20);
            this.rb_wp2.Name = "rb_wp2";
            this.rb_wp2.Size = new System.Drawing.Size(58, 17);
            this.rb_wp2.TabIndex = 11;
            this.rb_wp2.Text = "Enable";
            this.rb_wp2.UseVisualStyleBackColor = true;
            // 
            // rb_wp1
            // 
            this.rb_wp1.AutoSize = true;
            this.rb_wp1.Checked = true;
            this.rb_wp1.Location = new System.Drawing.Point(15, 20);
            this.rb_wp1.Name = "rb_wp1";
            this.rb_wp1.Size = new System.Drawing.Size(60, 17);
            this.rb_wp1.TabIndex = 10;
            this.rb_wp1.TabStop = true;
            this.rb_wp1.Text = "Disable";
            this.rb_wp1.UseVisualStyleBackColor = true;
            // 
            // bt_GetWpower
            // 
            this.bt_GetWpower.Location = new System.Drawing.Point(247, 42);
            this.bt_GetWpower.Name = "bt_GetWpower";
            this.bt_GetWpower.Size = new System.Drawing.Size(77, 25);
            this.bt_GetWpower.TabIndex = 9;
            this.bt_GetWpower.Text = "Get";
            this.bt_GetWpower.UseVisualStyleBackColor = true;
            this.bt_GetWpower.Click += new System.EventHandler(this.BtGetWpower_Click);
            // 
            // bt_SetWpower
            // 
            this.bt_SetWpower.Location = new System.Drawing.Point(158, 42);
            this.bt_SetWpower.Name = "bt_SetWpower";
            this.bt_SetWpower.Size = new System.Drawing.Size(77, 25);
            this.bt_SetWpower.TabIndex = 8;
            this.bt_SetWpower.Text = "Set";
            this.bt_SetWpower.UseVisualStyleBackColor = true;
            this.bt_SetWpower.Click += new System.EventHandler(this.BtSetWpower_Click);
            // 
            // group_maxtime
            // 
            this.group_maxtime.Controls.Add(this.comboBox_maxtime);
            this.group_maxtime.Controls.Add(this.btSetMaxtime);
            this.group_maxtime.Location = new System.Drawing.Point(6, 314);
            this.group_maxtime.Name = "group_maxtime";
            this.group_maxtime.Size = new System.Drawing.Size(342, 44);
            this.group_maxtime.TabIndex = 72;
            this.group_maxtime.TabStop = false;
            this.group_maxtime.Text = "Max inventory time";
            // 
            // comboBox_maxtime
            // 
            this.comboBox_maxtime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_maxtime.FormattingEnabled = true;
            this.comboBox_maxtime.Location = new System.Drawing.Point(94, 16);
            this.comboBox_maxtime.Name = "comboBox_maxtime";
            this.comboBox_maxtime.Size = new System.Drawing.Size(122, 21);
            this.comboBox_maxtime.TabIndex = 26;
            // 
            // btSetMaxtime
            // 
            this.btSetMaxtime.Location = new System.Drawing.Point(233, 15);
            this.btSetMaxtime.Name = "btSetMaxtime";
            this.btSetMaxtime.Size = new System.Drawing.Size(90, 25);
            this.btSetMaxtime.TabIndex = 23;
            this.btSetMaxtime.Text = "Set";
            this.btSetMaxtime.UseVisualStyleBackColor = true;
            this.btSetMaxtime.Click += new System.EventHandler(this.BtSetMaxtime_Click);
            // 
            // gbp_buff
            // 
            this.gbp_buff.Controls.Add(this.rb496);
            this.gbp_buff.Controls.Add(this.rb128);
            this.gbp_buff.Controls.Add(this.btGetEPCandTIDLen);
            this.gbp_buff.Controls.Add(this.btSetEPCandTIDLen);
            this.gbp_buff.Location = new System.Drawing.Point(5, 363);
            this.gbp_buff.Name = "gbp_buff";
            this.gbp_buff.Size = new System.Drawing.Size(343, 60);
            this.gbp_buff.TabIndex = 71;
            this.gbp_buff.TabStop = false;
            this.gbp_buff.Text = "Saved EPC/TID length";
            // 
            // rb496
            // 
            this.rb496.AutoSize = true;
            this.rb496.Location = new System.Drawing.Point(93, 27);
            this.rb496.Name = "rb496";
            this.rb496.Size = new System.Drawing.Size(54, 17);
            this.rb496.TabIndex = 11;
            this.rb496.Text = "496bit";
            this.rb496.UseVisualStyleBackColor = true;
            // 
            // rb128
            // 
            this.rb128.AutoSize = true;
            this.rb128.Checked = true;
            this.rb128.Location = new System.Drawing.Point(15, 27);
            this.rb128.Name = "rb128";
            this.rb128.Size = new System.Drawing.Size(54, 17);
            this.rb128.TabIndex = 10;
            this.rb128.TabStop = true;
            this.rb128.Text = "128bit";
            this.rb128.UseVisualStyleBackColor = true;
            // 
            // btGetEPCandTIDLen
            // 
            this.btGetEPCandTIDLen.Location = new System.Drawing.Point(247, 24);
            this.btGetEPCandTIDLen.Name = "btGetEPCandTIDLen";
            this.btGetEPCandTIDLen.Size = new System.Drawing.Size(77, 25);
            this.btGetEPCandTIDLen.TabIndex = 9;
            this.btGetEPCandTIDLen.Text = "Get";
            this.btGetEPCandTIDLen.UseVisualStyleBackColor = true;
            this.btGetEPCandTIDLen.Click += new System.EventHandler(this.BtGetEPCandTIDLen_Click);
            // 
            // btSetEPCandTIDLen
            // 
            this.btSetEPCandTIDLen.Location = new System.Drawing.Point(158, 24);
            this.btSetEPCandTIDLen.Name = "btSetEPCandTIDLen";
            this.btSetEPCandTIDLen.Size = new System.Drawing.Size(77, 25);
            this.btSetEPCandTIDLen.TabIndex = 8;
            this.btSetEPCandTIDLen.Text = "Set";
            this.btSetEPCandTIDLen.UseVisualStyleBackColor = true;
            this.btSetEPCandTIDLen.Click += new System.EventHandler(this.BtSetEPCandTIDLen_Click);
            // 
            // btGetInformation
            // 
            this.btGetInformation.Location = new System.Drawing.Point(704, 421);
            this.btGetInformation.Name = "btGetInformation";
            this.btGetInformation.Size = new System.Drawing.Size(169, 47);
            this.btGetInformation.TabIndex = 58;
            this.btGetInformation.Text = "Get reader information";
            this.btGetInformation.UseVisualStyleBackColor = true;
            this.btGetInformation.Click += new System.EventHandler(this.BtGetInformation_Click);
            // 
            // btDefault
            // 
            this.btDefault.Location = new System.Drawing.Point(883, 421);
            this.btDefault.Name = "btDefault";
            this.btDefault.Size = new System.Drawing.Size(169, 47);
            this.btDefault.TabIndex = 57;
            this.btDefault.Text = "Default setting";
            this.btDefault.UseVisualStyleBackColor = true;
            this.btDefault.Click += new System.EventHandler(this.BtDefault_Click);
            // 
            // gpb_antconfig
            // 
            this.gpb_antconfig.Controls.Add(this.Button_Ant);
            this.gpb_antconfig.Controls.Add(this.checkant4);
            this.gpb_antconfig.Controls.Add(this.checkant3);
            this.gpb_antconfig.Controls.Add(this.checkant2);
            this.gpb_antconfig.Controls.Add(this.checkant1);
            this.gpb_antconfig.Location = new System.Drawing.Point(354, 359);
            this.gpb_antconfig.Name = "gpb_antconfig";
            this.gpb_antconfig.Size = new System.Drawing.Size(342, 46);
            this.gpb_antconfig.TabIndex = 54;
            this.gpb_antconfig.TabStop = false;
            this.gpb_antconfig.Text = "Antenna config";
            // 
            // Button_Ant
            // 
            this.Button_Ant.Location = new System.Drawing.Point(233, 13);
            this.Button_Ant.Name = "Button_Ant";
            this.Button_Ant.Size = new System.Drawing.Size(90, 25);
            this.Button_Ant.TabIndex = 5;
            this.Button_Ant.Text = "Set";
            this.Button_Ant.UseVisualStyleBackColor = true;
            this.Button_Ant.Click += new System.EventHandler(this.Button_Ant_Click);
            // 
            // checkant4
            // 
            this.checkant4.AutoSize = true;
            this.checkant4.Location = new System.Drawing.Point(177, 16);
            this.checkant4.Name = "checkant4";
            this.checkant4.Size = new System.Drawing.Size(54, 17);
            this.checkant4.TabIndex = 4;
            this.checkant4.Text = "ANT4";
            this.checkant4.UseVisualStyleBackColor = true;
            // 
            // checkant3
            // 
            this.checkant3.AutoSize = true;
            this.checkant3.Location = new System.Drawing.Point(120, 16);
            this.checkant3.Name = "checkant3";
            this.checkant3.Size = new System.Drawing.Size(54, 17);
            this.checkant3.TabIndex = 3;
            this.checkant3.Text = "ANT3";
            this.checkant3.UseVisualStyleBackColor = true;
            // 
            // checkant2
            // 
            this.checkant2.AutoSize = true;
            this.checkant2.Location = new System.Drawing.Point(62, 16);
            this.checkant2.Name = "checkant2";
            this.checkant2.Size = new System.Drawing.Size(54, 17);
            this.checkant2.TabIndex = 2;
            this.checkant2.Text = "ANT2";
            this.checkant2.UseVisualStyleBackColor = true;
            // 
            // checkant1
            // 
            this.checkant1.AutoSize = true;
            this.checkant1.Location = new System.Drawing.Point(8, 16);
            this.checkant1.Name = "checkant1";
            this.checkant1.Size = new System.Drawing.Size(54, 17);
            this.checkant1.TabIndex = 1;
            this.checkant1.Text = "ANT1";
            this.checkant1.UseVisualStyleBackColor = true;
            // 
            // gpb_checkant
            // 
            this.gpb_checkant.Controls.Add(this.btSetcheckant);
            this.gpb_checkant.Controls.Add(this.rb_Closecheckant);
            this.gpb_checkant.Controls.Add(this.rb_Opencheckant);
            this.gpb_checkant.Location = new System.Drawing.Point(354, 301);
            this.gpb_checkant.Name = "gpb_checkant";
            this.gpb_checkant.Size = new System.Drawing.Size(342, 55);
            this.gpb_checkant.TabIndex = 53;
            this.gpb_checkant.TabStop = false;
            this.gpb_checkant.Text = "Antenna checked";
            // 
            // btSetcheckant
            // 
            this.btSetcheckant.Location = new System.Drawing.Point(233, 21);
            this.btSetcheckant.Name = "btSetcheckant";
            this.btSetcheckant.Size = new System.Drawing.Size(90, 25);
            this.btSetcheckant.TabIndex = 2;
            this.btSetcheckant.Text = "Set";
            this.btSetcheckant.UseVisualStyleBackColor = true;
            this.btSetcheckant.Click += new System.EventHandler(this.BtSetcheckant_Click);
            // 
            // rb_Closecheckant
            // 
            this.rb_Closecheckant.AutoSize = true;
            this.rb_Closecheckant.Location = new System.Drawing.Point(7, 35);
            this.rb_Closecheckant.Name = "rb_Closecheckant";
            this.rb_Closecheckant.Size = new System.Drawing.Size(149, 17);
            this.rb_Closecheckant.TabIndex = 1;
            this.rb_Closecheckant.Text = "Disable antenna detection";
            this.rb_Closecheckant.UseVisualStyleBackColor = true;
            // 
            // rb_Opencheckant
            // 
            this.rb_Opencheckant.AutoSize = true;
            this.rb_Opencheckant.Checked = true;
            this.rb_Opencheckant.Location = new System.Drawing.Point(7, 15);
            this.rb_Opencheckant.Name = "rb_Opencheckant";
            this.rb_Opencheckant.Size = new System.Drawing.Size(147, 17);
            this.rb_Opencheckant.TabIndex = 0;
            this.rb_Opencheckant.TabStop = true;
            this.rb_Opencheckant.Text = "Enable antenna detection";
            this.rb_Opencheckant.UseVisualStyleBackColor = true;
            // 
            // gpb_GPIO
            // 
            this.gpb_GPIO.Controls.Add(this.Button_GetGPIO);
            this.gpb_GPIO.Controls.Add(this.Button_SetGPIO);
            this.gpb_GPIO.Controls.Add(this.check_int2);
            this.gpb_GPIO.Controls.Add(this.check_int1);
            this.gpb_GPIO.Controls.Add(this.check_out2);
            this.gpb_GPIO.Controls.Add(this.check_out1);
            this.gpb_GPIO.Location = new System.Drawing.Point(354, 252);
            this.gpb_GPIO.Name = "gpb_GPIO";
            this.gpb_GPIO.Size = new System.Drawing.Size(693, 47);
            this.gpb_GPIO.TabIndex = 49;
            this.gpb_GPIO.TabStop = false;
            this.gpb_GPIO.Text = "GPIO Operation";
            // 
            // Button_GetGPIO
            // 
            this.Button_GetGPIO.Location = new System.Drawing.Point(585, 15);
            this.Button_GetGPIO.Name = "Button_GetGPIO";
            this.Button_GetGPIO.Size = new System.Drawing.Size(90, 25);
            this.Button_GetGPIO.TabIndex = 9;
            this.Button_GetGPIO.Text = "Get";
            this.Button_GetGPIO.UseVisualStyleBackColor = true;
            this.Button_GetGPIO.Click += new System.EventHandler(this.Button_GetGPIO_Click);
            // 
            // Button_SetGPIO
            // 
            this.Button_SetGPIO.Location = new System.Drawing.Point(483, 15);
            this.Button_SetGPIO.Name = "Button_SetGPIO";
            this.Button_SetGPIO.Size = new System.Drawing.Size(90, 25);
            this.Button_SetGPIO.TabIndex = 8;
            this.Button_SetGPIO.Text = "Set";
            this.Button_SetGPIO.UseVisualStyleBackColor = true;
            this.Button_SetGPIO.Click += new System.EventHandler(this.Button_SetGPIO_Click);
            // 
            // check_int2
            // 
            this.check_int2.AutoSize = true;
            this.check_int2.Enabled = false;
            this.check_int2.Location = new System.Drawing.Point(302, 20);
            this.check_int2.Name = "check_int2";
            this.check_int2.Size = new System.Drawing.Size(43, 17);
            this.check_int2.TabIndex = 5;
            this.check_int2.Text = "IN2";
            this.check_int2.UseVisualStyleBackColor = true;
            this.check_int2.CheckedChanged += new System.EventHandler(this.Check_int2_CheckedChanged);
            // 
            // check_int1
            // 
            this.check_int1.AutoSize = true;
            this.check_int1.Enabled = false;
            this.check_int1.Location = new System.Drawing.Point(227, 20);
            this.check_int1.Name = "check_int1";
            this.check_int1.Size = new System.Drawing.Size(43, 17);
            this.check_int1.TabIndex = 4;
            this.check_int1.Text = "IN1";
            this.check_int1.UseVisualStyleBackColor = true;
            // 
            // check_out2
            // 
            this.check_out2.AutoSize = true;
            this.check_out2.Location = new System.Drawing.Point(83, 20);
            this.check_out2.Name = "check_out2";
            this.check_out2.Size = new System.Drawing.Size(55, 17);
            this.check_out2.TabIndex = 1;
            this.check_out2.Text = "OUT2";
            this.check_out2.UseVisualStyleBackColor = true;
            // 
            // check_out1
            // 
            this.check_out1.AutoSize = true;
            this.check_out1.Location = new System.Drawing.Point(8, 20);
            this.check_out1.Name = "check_out1";
            this.check_out1.Size = new System.Drawing.Size(55, 17);
            this.check_out1.TabIndex = 0;
            this.check_out1.Text = "OUT1";
            this.check_out1.UseVisualStyleBackColor = true;
            // 
            // gpb_OutputRep
            // 
            this.gpb_OutputRep.Controls.Add(this.btOutputRep);
            this.gpb_OutputRep.Controls.Add(this.check_OutputRep4);
            this.gpb_OutputRep.Controls.Add(this.check_OutputRep3);
            this.gpb_OutputRep.Controls.Add(this.check_OutputRep2);
            this.gpb_OutputRep.Controls.Add(this.check_OutputRep1);
            this.gpb_OutputRep.Location = new System.Drawing.Point(705, 205);
            this.gpb_OutputRep.Name = "gpb_OutputRep";
            this.gpb_OutputRep.Size = new System.Drawing.Size(342, 46);
            this.gpb_OutputRep.TabIndex = 48;
            this.gpb_OutputRep.TabStop = false;
            this.gpb_OutputRep.Text = "Set notification pluse output";
            // 
            // btOutputRep
            // 
            this.btOutputRep.Location = new System.Drawing.Point(234, 14);
            this.btOutputRep.Name = "btOutputRep";
            this.btOutputRep.Size = new System.Drawing.Size(90, 25);
            this.btOutputRep.TabIndex = 5;
            this.btOutputRep.Text = "Set";
            this.btOutputRep.UseVisualStyleBackColor = true;
            this.btOutputRep.Click += new System.EventHandler(this.BtOutputRep_Click);
            // 
            // check_OutputRep4
            // 
            this.check_OutputRep4.AutoSize = true;
            this.check_OutputRep4.Location = new System.Drawing.Point(184, 17);
            this.check_OutputRep4.Name = "check_OutputRep4";
            this.check_OutputRep4.Size = new System.Drawing.Size(55, 17);
            this.check_OutputRep4.TabIndex = 4;
            this.check_OutputRep4.Text = "OUT4";
            this.check_OutputRep4.UseVisualStyleBackColor = true;
            // 
            // check_OutputRep3
            // 
            this.check_OutputRep3.AutoSize = true;
            this.check_OutputRep3.Location = new System.Drawing.Point(125, 17);
            this.check_OutputRep3.Name = "check_OutputRep3";
            this.check_OutputRep3.Size = new System.Drawing.Size(55, 17);
            this.check_OutputRep3.TabIndex = 3;
            this.check_OutputRep3.Text = "OUT3";
            this.check_OutputRep3.UseVisualStyleBackColor = true;
            // 
            // check_OutputRep2
            // 
            this.check_OutputRep2.AutoSize = true;
            this.check_OutputRep2.Location = new System.Drawing.Point(66, 17);
            this.check_OutputRep2.Name = "check_OutputRep2";
            this.check_OutputRep2.Size = new System.Drawing.Size(55, 17);
            this.check_OutputRep2.TabIndex = 2;
            this.check_OutputRep2.Text = "OUT2";
            this.check_OutputRep2.UseVisualStyleBackColor = true;
            // 
            // check_OutputRep1
            // 
            this.check_OutputRep1.AutoSize = true;
            this.check_OutputRep1.Location = new System.Drawing.Point(8, 17);
            this.check_OutputRep1.Name = "check_OutputRep1";
            this.check_OutputRep1.Size = new System.Drawing.Size(55, 17);
            this.check_OutputRep1.TabIndex = 1;
            this.check_OutputRep1.Text = "OUT1";
            this.check_OutputRep1.UseVisualStyleBackColor = true;
            // 
            // gpb_Relay
            // 
            this.gpb_Relay.Controls.Add(this.label38);
            this.gpb_Relay.Controls.Add(this.ComboBox_RelayTime);
            this.gpb_Relay.Controls.Add(this.label37);
            this.gpb_Relay.Controls.Add(this.btRelay);
            this.gpb_Relay.Location = new System.Drawing.Point(354, 205);
            this.gpb_Relay.Name = "gpb_Relay";
            this.gpb_Relay.Size = new System.Drawing.Size(342, 46);
            this.gpb_Relay.TabIndex = 31;
            this.gpb_Relay.TabStop = false;
            this.gpb_Relay.Text = "Relay control";
            // 
            // label38
            // 
            this.label38.AutoSize = true;
            this.label38.Location = new System.Drawing.Point(182, 20);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(36, 13);
            this.label38.TabIndex = 26;
            this.label38.Text = "*50ms";
            // 
            // ComboBox_RelayTime
            // 
            this.ComboBox_RelayTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_RelayTime.FormattingEnabled = true;
            this.ComboBox_RelayTime.Location = new System.Drawing.Point(82, 16);
            this.ComboBox_RelayTime.Name = "ComboBox_RelayTime";
            this.ComboBox_RelayTime.Size = new System.Drawing.Size(96, 21);
            this.ComboBox_RelayTime.TabIndex = 25;
            // 
            // label37
            // 
            this.label37.AutoSize = true;
            this.label37.Location = new System.Drawing.Point(4, 21);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(71, 13);
            this.label37.TabIndex = 24;
            this.label37.Text = "Release time:";
            // 
            // btRelay
            // 
            this.btRelay.Location = new System.Drawing.Point(233, 14);
            this.btRelay.Name = "btRelay";
            this.btRelay.Size = new System.Drawing.Size(90, 25);
            this.btRelay.TabIndex = 23;
            this.btRelay.Text = "Set";
            this.btRelay.UseVisualStyleBackColor = true;
            this.btRelay.Click += new System.EventHandler(this.BtRelay_Click);
            // 
            // gpb_baud
            // 
            this.gpb_baud.Controls.Add(this.ComboBox_baud);
            this.gpb_baud.Controls.Add(this.btBaudRate);
            this.gpb_baud.Location = new System.Drawing.Point(706, 133);
            this.gpb_baud.Name = "gpb_baud";
            this.gpb_baud.Size = new System.Drawing.Size(342, 65);
            this.gpb_baud.TabIndex = 30;
            this.gpb_baud.TabStop = false;
            this.gpb_baud.Text = "RS232/485 baud rate";
            // 
            // ComboBox_baud
            // 
            this.ComboBox_baud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_baud.FormattingEnabled = true;
            this.ComboBox_baud.Items.AddRange(new object[] {
            "9600bps",
            "19200bps",
            "38400bps",
            "57600bps",
            "115200bps"});
            this.ComboBox_baud.Location = new System.Drawing.Point(69, 27);
            this.ComboBox_baud.Name = "ComboBox_baud";
            this.ComboBox_baud.Size = new System.Drawing.Size(121, 21);
            this.ComboBox_baud.TabIndex = 24;
            // 
            // btBaudRate
            // 
            this.btBaudRate.Location = new System.Drawing.Point(233, 24);
            this.btBaudRate.Name = "btBaudRate";
            this.btBaudRate.Size = new System.Drawing.Size(90, 25);
            this.btBaudRate.TabIndex = 23;
            this.btBaudRate.Text = "Set";
            this.btBaudRate.UseVisualStyleBackColor = true;
            this.btBaudRate.Click += new System.EventHandler(this.BtBaudRate_Click);
            // 
            // gpb_DBM
            // 
            this.gpb_DBM.Controls.Add(this.label10);
            this.gpb_DBM.Controls.Add(this.ComboBox_PowerDbm);
            this.gpb_DBM.Controls.Add(this.BT_DBM);
            this.gpb_DBM.Location = new System.Drawing.Point(354, 133);
            this.gpb_DBM.Name = "gpb_DBM";
            this.gpb_DBM.Size = new System.Drawing.Size(342, 67);
            this.gpb_DBM.TabIndex = 29;
            this.gpb_DBM.TabStop = false;
            this.gpb_DBM.Text = "Power";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(182, 28);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(28, 13);
            this.label10.TabIndex = 25;
            this.label10.Text = "dBm";
            // 
            // ComboBox_PowerDbm
            // 
            this.ComboBox_PowerDbm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_PowerDbm.FormattingEnabled = true;
            this.ComboBox_PowerDbm.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30"});
            this.ComboBox_PowerDbm.Location = new System.Drawing.Point(82, 25);
            this.ComboBox_PowerDbm.Name = "ComboBox_PowerDbm";
            this.ComboBox_PowerDbm.Size = new System.Drawing.Size(96, 21);
            this.ComboBox_PowerDbm.TabIndex = 24;
            // 
            // BT_DBM
            // 
            this.BT_DBM.Location = new System.Drawing.Point(233, 25);
            this.BT_DBM.Name = "BT_DBM";
            this.BT_DBM.Size = new System.Drawing.Size(90, 25);
            this.BT_DBM.TabIndex = 23;
            this.BT_DBM.Text = "Set";
            this.BT_DBM.UseVisualStyleBackColor = true;
            this.BT_DBM.Click += new System.EventHandler(this.BT_DBM_Click);
            // 
            // gpb_Freq
            // 
            this.gpb_Freq.Controls.Add(this.radioButton_band12);
            this.gpb_Freq.Controls.Add(this.radioButton_band8);
            this.gpb_Freq.Controls.Add(this.CheckBox_SameFre);
            this.gpb_Freq.Controls.Add(this.radioButton_band4);
            this.gpb_Freq.Controls.Add(this.radioButton_band3);
            this.gpb_Freq.Controls.Add(this.radioButton_band2);
            this.gpb_Freq.Controls.Add(this.radioButton_band1);
            this.gpb_Freq.Controls.Add(this.btFreq);
            this.gpb_Freq.Controls.Add(this.ComboBox_dmaxfre);
            this.gpb_Freq.Controls.Add(this.label15);
            this.gpb_Freq.Controls.Add(this.ComboBox_dminfre);
            this.gpb_Freq.Controls.Add(this.label14);
            this.gpb_Freq.Location = new System.Drawing.Point(354, 55);
            this.gpb_Freq.Name = "gpb_Freq";
            this.gpb_Freq.Size = new System.Drawing.Size(695, 76);
            this.gpb_Freq.TabIndex = 28;
            this.gpb_Freq.TabStop = false;
            this.gpb_Freq.Text = "Region";
            // 
            // radioButton_band12
            // 
            this.radioButton_band12.AutoSize = true;
            this.radioButton_band12.Location = new System.Drawing.Point(247, 48);
            this.radioButton_band12.Name = "radioButton_band12";
            this.radioButton_band12.Size = new System.Drawing.Size(73, 17);
            this.radioButton_band12.TabIndex = 18;
            this.radioButton_band12.TabStop = true;
            this.radioButton_band12.Text = "US band3";
            this.radioButton_band12.UseVisualStyleBackColor = true;
            this.radioButton_band12.CheckedChanged += new System.EventHandler(this.RB_US_band3_CheckedChanged);
            // 
            // radioButton_band8
            // 
            this.radioButton_band8.AutoSize = true;
            this.radioButton_band8.Location = new System.Drawing.Point(10, 18);
            this.radioButton_band8.Name = "radioButton_band8";
            this.radioButton_band8.Size = new System.Drawing.Size(96, 17);
            this.radioButton_band8.TabIndex = 17;
            this.radioButton_band8.TabStop = true;
            this.radioButton_band8.Text = "Chinese band1";
            this.radioButton_band8.UseVisualStyleBackColor = true;
            this.radioButton_band8.CheckedChanged += new System.EventHandler(this.RB_Chinese_band1_CheckedChanged);
            // 
            // CheckBox_SameFre
            // 
            this.CheckBox_SameFre.AutoSize = true;
            this.CheckBox_SameFre.Location = new System.Drawing.Point(585, 17);
            this.CheckBox_SameFre.Name = "CheckBox_SameFre";
            this.CheckBox_SameFre.Size = new System.Drawing.Size(55, 17);
            this.CheckBox_SameFre.TabIndex = 16;
            this.CheckBox_SameFre.Text = "Single";
            this.CheckBox_SameFre.UseVisualStyleBackColor = true;
            this.CheckBox_SameFre.CheckedChanged += new System.EventHandler(this.CheckBox_SameFre_CheckedChanged);
            // 
            // radioButton_band4
            // 
            this.radioButton_band4.AutoSize = true;
            this.radioButton_band4.Location = new System.Drawing.Point(122, 48);
            this.radioButton_band4.Name = "radioButton_band4";
            this.radioButton_band4.Size = new System.Drawing.Size(67, 17);
            this.radioButton_band4.TabIndex = 15;
            this.radioButton_band4.TabStop = true;
            this.radioButton_band4.Text = "EU band";
            this.radioButton_band4.UseVisualStyleBackColor = true;
            this.radioButton_band4.CheckedChanged += new System.EventHandler(this.RB_EU_band_CheckedChanged);
            // 
            // radioButton_band3
            // 
            this.radioButton_band3.AutoSize = true;
            this.radioButton_band3.Location = new System.Drawing.Point(10, 44);
            this.radioButton_band3.Name = "radioButton_band3";
            this.radioButton_band3.Size = new System.Drawing.Size(86, 17);
            this.radioButton_band3.TabIndex = 14;
            this.radioButton_band3.TabStop = true;
            this.radioButton_band3.Text = "Korean band";
            this.radioButton_band3.UseVisualStyleBackColor = true;
            this.radioButton_band3.CheckedChanged += new System.EventHandler(this.RB_Korean_band_CheckedChanged);
            // 
            // radioButton_band2
            // 
            this.radioButton_band2.AutoSize = true;
            this.radioButton_band2.Location = new System.Drawing.Point(247, 17);
            this.radioButton_band2.Name = "radioButton_band2";
            this.radioButton_band2.Size = new System.Drawing.Size(67, 17);
            this.radioButton_band2.TabIndex = 13;
            this.radioButton_band2.TabStop = true;
            this.radioButton_band2.Text = "US band";
            this.radioButton_band2.UseVisualStyleBackColor = true;
            this.radioButton_band2.CheckedChanged += new System.EventHandler(this.RB_US_band_CheckedChanged);
            // 
            // radioButton_band1
            // 
            this.radioButton_band1.AutoSize = true;
            this.radioButton_band1.Location = new System.Drawing.Point(122, 18);
            this.radioButton_band1.Name = "radioButton_band1";
            this.radioButton_band1.Size = new System.Drawing.Size(96, 17);
            this.radioButton_band1.TabIndex = 12;
            this.radioButton_band1.TabStop = true;
            this.radioButton_band1.Text = "Chinese band2";
            this.radioButton_band1.UseVisualStyleBackColor = true;
            this.radioButton_band1.CheckedChanged += new System.EventHandler(this.RB_Chinese_band2_CheckedChanged);
            // 
            // btFreq
            // 
            this.btFreq.Location = new System.Drawing.Point(585, 40);
            this.btFreq.Name = "btFreq";
            this.btFreq.Size = new System.Drawing.Size(90, 25);
            this.btFreq.TabIndex = 11;
            this.btFreq.Text = "Set";
            this.btFreq.UseVisualStyleBackColor = true;
            this.btFreq.Click += new System.EventHandler(this.BtFreq_Click);
            // 
            // ComboBox_dmaxfre
            // 
            this.ComboBox_dmaxfre.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_dmaxfre.FormattingEnabled = true;
            this.ComboBox_dmaxfre.Location = new System.Drawing.Point(377, 46);
            this.ComboBox_dmaxfre.Name = "ComboBox_dmaxfre";
            this.ComboBox_dmaxfre.Size = new System.Drawing.Size(100, 21);
            this.ComboBox_dmaxfre.TabIndex = 10;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(332, 50);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(42, 13);
            this.label15.TabIndex = 9;
            this.label15.Text = "Maxfre:";
            // 
            // ComboBox_dminfre
            // 
            this.ComboBox_dminfre.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_dminfre.FormattingEnabled = true;
            this.ComboBox_dminfre.Location = new System.Drawing.Point(377, 14);
            this.ComboBox_dminfre.Name = "ComboBox_dminfre";
            this.ComboBox_dminfre.Size = new System.Drawing.Size(100, 21);
            this.ComboBox_dminfre.TabIndex = 8;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(332, 18);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(39, 13);
            this.label14.TabIndex = 7;
            this.label14.Text = "Minfre:";
            // 
            // gpb_beep
            // 
            this.gpb_beep.Controls.Add(this.Radio_beepDis);
            this.gpb_beep.Controls.Add(this.Radio_beepEn);
            this.gpb_beep.Controls.Add(this.Button_Beep);
            this.gpb_beep.Location = new System.Drawing.Point(707, 5);
            this.gpb_beep.Name = "gpb_beep";
            this.gpb_beep.Size = new System.Drawing.Size(342, 48);
            this.gpb_beep.TabIndex = 27;
            this.gpb_beep.TabStop = false;
            this.gpb_beep.Text = "Beep";
            // 
            // Radio_beepDis
            // 
            this.Radio_beepDis.AutoSize = true;
            this.Radio_beepDis.Checked = true;
            this.Radio_beepDis.Location = new System.Drawing.Point(123, 20);
            this.Radio_beepDis.Name = "Radio_beepDis";
            this.Radio_beepDis.Size = new System.Drawing.Size(51, 17);
            this.Radio_beepDis.TabIndex = 10;
            this.Radio_beepDis.TabStop = true;
            this.Radio_beepDis.Text = "Close";
            this.Radio_beepDis.UseVisualStyleBackColor = true;
            // 
            // Radio_beepEn
            // 
            this.Radio_beepEn.AutoSize = true;
            this.Radio_beepEn.Location = new System.Drawing.Point(33, 20);
            this.Radio_beepEn.Name = "Radio_beepEn";
            this.Radio_beepEn.Size = new System.Drawing.Size(51, 17);
            this.Radio_beepEn.TabIndex = 9;
            this.Radio_beepEn.Text = "Open";
            this.Radio_beepEn.UseVisualStyleBackColor = true;
            // 
            // Button_Beep
            // 
            this.Button_Beep.Location = new System.Drawing.Point(232, 15);
            this.Button_Beep.Name = "Button_Beep";
            this.Button_Beep.Size = new System.Drawing.Size(90, 25);
            this.Button_Beep.TabIndex = 8;
            this.Button_Beep.Text = "Set";
            this.Button_Beep.UseVisualStyleBackColor = true;
            this.Button_Beep.Click += new System.EventHandler(this.Button_Beep_Click);
            // 
            // gpb_RDVersion
            // 
            this.gpb_RDVersion.Controls.Add(this.text_RDVersion);
            this.gpb_RDVersion.Location = new System.Drawing.Point(354, 5);
            this.gpb_RDVersion.Name = "gpb_RDVersion";
            this.gpb_RDVersion.Size = new System.Drawing.Size(342, 48);
            this.gpb_RDVersion.TabIndex = 26;
            this.gpb_RDVersion.TabStop = false;
            this.gpb_RDVersion.Text = "Firmware version";
            // 
            // text_RDVersion
            // 
            this.text_RDVersion.Location = new System.Drawing.Point(95, 16);
            this.text_RDVersion.MaxLength = 2;
            this.text_RDVersion.Name = "text_RDVersion";
            this.text_RDVersion.ReadOnly = true;
            this.text_RDVersion.Size = new System.Drawing.Size(160, 20);
            this.text_RDVersion.TabIndex = 24;
            // 
            // gpb_Serial
            // 
            this.gpb_Serial.Controls.Add(this.TextBox_Serial);
            this.gpb_Serial.Controls.Add(this.btSerial);
            this.gpb_Serial.Location = new System.Drawing.Point(6, 263);
            this.gpb_Serial.Name = "gpb_Serial";
            this.gpb_Serial.Size = new System.Drawing.Size(342, 47);
            this.gpb_Serial.TabIndex = 6;
            this.gpb_Serial.TabStop = false;
            this.gpb_Serial.Text = "Reader serial number\n";
            // 
            // TextBox_Serial
            // 
            this.TextBox_Serial.Location = new System.Drawing.Point(95, 14);
            this.TextBox_Serial.MaxLength = 8;
            this.TextBox_Serial.Name = "TextBox_Serial";
            this.TextBox_Serial.ReadOnly = true;
            this.TextBox_Serial.Size = new System.Drawing.Size(121, 20);
            this.TextBox_Serial.TabIndex = 24;
            // 
            // btSerial
            // 
            this.btSerial.Location = new System.Drawing.Point(233, 14);
            this.btSerial.Name = "btSerial";
            this.btSerial.Size = new System.Drawing.Size(90, 25);
            this.btSerial.TabIndex = 23;
            this.btSerial.Text = "Get";
            this.btSerial.UseVisualStyleBackColor = true;
            this.btSerial.Click += new System.EventHandler(this.BtSerial_Click);
            // 
            // gpb_address
            // 
            this.gpb_address.Controls.Add(this.text_address);
            this.gpb_address.Controls.Add(this.btaddress);
            this.gpb_address.Location = new System.Drawing.Point(6, 212);
            this.gpb_address.Name = "gpb_address";
            this.gpb_address.Size = new System.Drawing.Size(342, 49);
            this.gpb_address.TabIndex = 5;
            this.gpb_address.TabStop = false;
            this.gpb_address.Text = "Reader address";
            // 
            // text_address
            // 
            this.text_address.Location = new System.Drawing.Point(95, 15);
            this.text_address.MaxLength = 2;
            this.text_address.Name = "text_address";
            this.text_address.Size = new System.Drawing.Size(121, 20);
            this.text_address.TabIndex = 24;
            this.text_address.Text = "00";
            this.text_address.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // btaddress
            // 
            this.btaddress.Location = new System.Drawing.Point(233, 15);
            this.btaddress.Name = "btaddress";
            this.btaddress.Size = new System.Drawing.Size(90, 25);
            this.btaddress.TabIndex = 23;
            this.btaddress.Text = "Set";
            this.btaddress.UseVisualStyleBackColor = true;
            this.btaddress.Click += new System.EventHandler(this.BtAddress_Click);
            // 
            // gpb_tcp
            // 
            this.gpb_tcp.Controls.Add(this.tb_Port);
            this.gpb_tcp.Controls.Add(this.ipServerIP);
            this.gpb_tcp.Controls.Add(this.btDisConnectTcp);
            this.gpb_tcp.Controls.Add(this.btConnectTcp);
            this.gpb_tcp.Controls.Add(this.label2);
            this.gpb_tcp.Controls.Add(this.label4);
            this.gpb_tcp.Location = new System.Drawing.Point(5, 133);
            this.gpb_tcp.Name = "gpb_tcp";
            this.gpb_tcp.Size = new System.Drawing.Size(343, 77);
            this.gpb_tcp.TabIndex = 4;
            this.gpb_tcp.TabStop = false;
            this.gpb_tcp.Text = "TCP/IP";
            // 
            // tb_Port
            // 
            this.tb_Port.Location = new System.Drawing.Point(96, 43);
            this.tb_Port.Name = "tb_Port";
            this.tb_Port.Size = new System.Drawing.Size(121, 20);
            this.tb_Port.TabIndex = 22;
            this.tb_Port.Text = "27011";
            this.tb_Port.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TB_Port_KeyPress);
            // 
            // ipServerIP
            // 
            this.ipServerIP.IpAddressStr = "192.168.0.250";
            this.ipServerIP.Location = new System.Drawing.Point(96, 16);
            this.ipServerIP.Name = "ipServerIP";
            this.ipServerIP.Size = new System.Drawing.Size(121, 23);
            this.ipServerIP.TabIndex = 21;
            this.ipServerIP.Tag = "";
            // 
            // btDisConnectTcp
            // 
            this.btDisConnectTcp.Enabled = false;
            this.btDisConnectTcp.Location = new System.Drawing.Point(234, 43);
            this.btDisConnectTcp.Name = "btDisConnectTcp";
            this.btDisConnectTcp.Size = new System.Drawing.Size(90, 25);
            this.btDisConnectTcp.TabIndex = 20;
            this.btDisConnectTcp.Text = "Disconnect";
            this.btDisConnectTcp.UseVisualStyleBackColor = true;
            this.btDisConnectTcp.Click += new System.EventHandler(this.BtDisConnectTcp_Click);
            // 
            // btConnectTcp
            // 
            this.btConnectTcp.Location = new System.Drawing.Point(234, 14);
            this.btConnectTcp.Name = "btConnectTcp";
            this.btConnectTcp.Size = new System.Drawing.Size(90, 25);
            this.btConnectTcp.TabIndex = 19;
            this.btConnectTcp.Text = "Connect";
            this.btConnectTcp.UseVisualStyleBackColor = true;
            this.btConnectTcp.Click += new System.EventHandler(this.BtConnectTcp_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 17;
            this.label2.Text = "Port:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(27, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "IP Addr:";
            // 
            // gpb_rs232
            // 
            this.gpb_rs232.Controls.Add(this.btDisConnect232);
            this.gpb_rs232.Controls.Add(this.BtConnect232);
            this.gpb_rs232.Controls.Add(this.ComboBox_baud2);
            this.gpb_rs232.Controls.Add(this.label47);
            this.gpb_rs232.Controls.Add(this.ComboBox_COM);
            this.gpb_rs232.Controls.Add(this.label3);
            this.gpb_rs232.Location = new System.Drawing.Point(5, 55);
            this.gpb_rs232.Name = "gpb_rs232";
            this.gpb_rs232.Size = new System.Drawing.Size(343, 76);
            this.gpb_rs232.TabIndex = 3;
            this.gpb_rs232.TabStop = false;
            this.gpb_rs232.Text = "RS232";
            // 
            // btDisConnect232
            // 
            this.btDisConnect232.Enabled = false;
            this.btDisConnect232.Location = new System.Drawing.Point(234, 44);
            this.btDisConnect232.Name = "btDisConnect232";
            this.btDisConnect232.Size = new System.Drawing.Size(90, 25);
            this.btDisConnect232.TabIndex = 20;
            this.btDisConnect232.Text = "Disconnect";
            this.btDisConnect232.UseVisualStyleBackColor = true;
            this.btDisConnect232.Click += new System.EventHandler(this.BtDisConnect232_Click);
            // 
            // BtConnect232
            // 
            this.BtConnect232.Location = new System.Drawing.Point(234, 14);
            this.BtConnect232.Name = "BtConnect232";
            this.BtConnect232.Size = new System.Drawing.Size(90, 25);
            this.BtConnect232.TabIndex = 19;
            this.BtConnect232.Text = "Connect";
            this.BtConnect232.UseVisualStyleBackColor = true;
            this.BtConnect232.Click += new System.EventHandler(this.BtConnect232_Click);
            // 
            // ComboBox_baud2
            // 
            this.ComboBox_baud2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_baud2.FormattingEnabled = true;
            this.ComboBox_baud2.Items.AddRange(new object[] {
            "9600bps",
            "19200bps",
            "38400bps",
            "57600bps",
            "115200bps"});
            this.ComboBox_baud2.Location = new System.Drawing.Point(96, 47);
            this.ComboBox_baud2.Name = "ComboBox_baud2";
            this.ComboBox_baud2.Size = new System.Drawing.Size(121, 21);
            this.ComboBox_baud2.TabIndex = 18;
            // 
            // label47
            // 
            this.label47.AutoSize = true;
            this.label47.Location = new System.Drawing.Point(21, 52);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(56, 13);
            this.label47.TabIndex = 17;
            this.label47.Text = "Baud rate:";
            // 
            // ComboBox_COM
            // 
            this.ComboBox_COM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBox_COM.FormattingEnabled = true;
            this.ComboBox_COM.Items.AddRange(new object[] {
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9"});
            this.ComboBox_COM.Location = new System.Drawing.Point(96, 16);
            this.ComboBox_COM.Name = "ComboBox_COM";
            this.ComboBox_COM.Size = new System.Drawing.Size(121, 21);
            this.ComboBox_COM.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Serial Port:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rb_tcp);
            this.groupBox1.Controls.Add(this.rb_rs232);
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(343, 48);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connect Type";
            // 
            // rb_tcp
            // 
            this.rb_tcp.AutoSize = true;
            this.rb_tcp.Location = new System.Drawing.Point(190, 20);
            this.rb_tcp.Name = "rb_tcp";
            this.rb_tcp.Size = new System.Drawing.Size(61, 17);
            this.rb_tcp.TabIndex = 1;
            this.rb_tcp.Text = "TCP/IP";
            this.rb_tcp.UseVisualStyleBackColor = true;
            this.rb_tcp.CheckedChanged += new System.EventHandler(this.RB_Tcp_CheckedChanged);
            // 
            // rb_rs232
            // 
            this.rb_rs232.AutoSize = true;
            this.rb_rs232.Location = new System.Drawing.Point(33, 20);
            this.rb_rs232.Name = "rb_rs232";
            this.rb_rs232.Size = new System.Drawing.Size(58, 17);
            this.rb_rs232.TabIndex = 0;
            this.rb_rs232.Text = "RS232";
            this.rb_rs232.UseVisualStyleBackColor = true;
            this.rb_rs232.CheckedChanged += new System.EventHandler(this.RB_RS232_CheckedChanged);
            // 
            // tabPage_Module
            // 
            this.tabPage_Module.Controls.Add(this.panel9);
            this.tabPage_Module.Location = new System.Drawing.Point(4, 27);
            this.tabPage_Module.Name = "tabPage_Module";
            this.tabPage_Module.Size = new System.Drawing.Size(1057, 584);
            this.tabPage_Module.TabIndex = 2;
            this.tabPage_Module.Text = "Real-time-inventory setting";
            this.tabPage_Module.UseVisualStyleBackColor = true;
            // 
            // panel9
            // 
            this.panel9.Controls.Add(this.btMSetParameter);
            this.panel9.Controls.Add(this.btMGetParameter);
            this.panel9.Controls.Add(this.groupBox39);
            this.panel9.Controls.Add(this.groupBox38);
            this.panel9.Controls.Add(this.groupBox34);
            this.panel9.Controls.Add(this.groupBox33);
            this.panel9.Controls.Add(this.groupBox32);
            this.panel9.Controls.Add(this.groupBox28);
            this.panel9.Controls.Add(this.groupBox3);
            this.panel9.Location = new System.Drawing.Point(0, 0);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(1059, 548);
            this.panel9.TabIndex = 0;
            this.panel9.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel9_Paint);
            // 
            // btMSetParameter
            // 
            this.btMSetParameter.Location = new System.Drawing.Point(928, 128);
            this.btMSetParameter.Name = "btMSetParameter";
            this.btMSetParameter.Size = new System.Drawing.Size(119, 36);
            this.btMSetParameter.TabIndex = 86;
            this.btMSetParameter.Text = "Set Parameter";
            this.btMSetParameter.UseVisualStyleBackColor = true;
            this.btMSetParameter.Click += new System.EventHandler(this.BtMSetParameter_Click);
            // 
            // btMGetParameter
            // 
            this.btMGetParameter.Location = new System.Drawing.Point(928, 80);
            this.btMGetParameter.Name = "btMGetParameter";
            this.btMGetParameter.Size = new System.Drawing.Size(119, 36);
            this.btMGetParameter.TabIndex = 85;
            this.btMGetParameter.Text = "Get Parameter";
            this.btMGetParameter.UseVisualStyleBackColor = true;
            this.btMGetParameter.Click += new System.EventHandler(this.BtMGetParameter_Click);
            // 
            // groupBox39
            // 
            this.groupBox39.Controls.Add(this.btSetMmode);
            this.groupBox39.Controls.Add(this.com_Mmode);
            this.groupBox39.Controls.Add(this.label111);
            this.groupBox39.Location = new System.Drawing.Point(656, 75);
            this.groupBox39.Name = "groupBox39";
            this.groupBox39.Size = new System.Drawing.Size(265, 94);
            this.groupBox39.TabIndex = 84;
            this.groupBox39.TabStop = false;
            this.groupBox39.Text = "Work Mode";
            // 
            // btSetMmode
            // 
            this.btSetMmode.Location = new System.Drawing.Point(170, 61);
            this.btSetMmode.Name = "btSetMmode";
            this.btSetMmode.Size = new System.Drawing.Size(90, 25);
            this.btSetMmode.TabIndex = 2;
            this.btSetMmode.Text = "Set";
            this.btSetMmode.UseVisualStyleBackColor = true;
            this.btSetMmode.Click += new System.EventHandler(this.BtSetMmode_Click);
            // 
            // com_Mmode
            // 
            this.com_Mmode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_Mmode.FormattingEnabled = true;
            this.com_Mmode.Items.AddRange(new object[] {
            "Answer mode",
            "Real-time-inventory mode",
            "trigger-mode"});
            this.com_Mmode.Location = new System.Drawing.Point(92, 25);
            this.com_Mmode.Name = "com_Mmode";
            this.com_Mmode.Size = new System.Drawing.Size(168, 21);
            this.com_Mmode.TabIndex = 1;
            // 
            // label111
            // 
            this.label111.AutoSize = true;
            this.label111.Location = new System.Drawing.Point(7, 28);
            this.label111.Name = "label111";
            this.label111.Size = new System.Drawing.Size(70, 13);
            this.label111.TabIndex = 0;
            this.label111.Text = "Mode Select:";
            // 
            // groupBox38
            // 
            this.groupBox38.Controls.Add(this.checkBox_tid);
            this.groupBox38.Controls.Add(this.txt_Mtidlen);
            this.groupBox38.Controls.Add(this.label103);
            this.groupBox38.Controls.Add(this.txt_mtidaddr);
            this.groupBox38.Controls.Add(this.label104);
            this.groupBox38.Location = new System.Drawing.Point(436, 75);
            this.groupBox38.Name = "groupBox38";
            this.groupBox38.Size = new System.Drawing.Size(214, 94);
            this.groupBox38.TabIndex = 83;
            this.groupBox38.TabStop = false;
            this.groupBox38.Text = "TID Parameter Setting";
            // 
            // checkBox_tid
            // 
            this.checkBox_tid.AutoSize = true;
            this.checkBox_tid.Location = new System.Drawing.Point(160, 65);
            this.checkBox_tid.Name = "checkBox_tid";
            this.checkBox_tid.Size = new System.Drawing.Size(59, 17);
            this.checkBox_tid.TabIndex = 10;
            this.checkBox_tid.Text = "Enable";
            this.checkBox_tid.UseVisualStyleBackColor = true;
            // 
            // txt_Mtidlen
            // 
            this.txt_Mtidlen.Location = new System.Drawing.Point(87, 63);
            this.txt_Mtidlen.MaxLength = 2;
            this.txt_Mtidlen.Name = "txt_Mtidlen";
            this.txt_Mtidlen.Size = new System.Drawing.Size(60, 20);
            this.txt_Mtidlen.TabIndex = 5;
            this.txt_Mtidlen.Text = "04";
            // 
            // label103
            // 
            this.label103.AutoSize = true;
            this.label103.Location = new System.Drawing.Point(13, 66);
            this.label103.Name = "label103";
            this.label103.Size = new System.Drawing.Size(68, 13);
            this.label103.TabIndex = 4;
            this.label103.Text = "Length(Hex):";
            // 
            // txt_mtidaddr
            // 
            this.txt_mtidaddr.Location = new System.Drawing.Point(138, 25);
            this.txt_mtidaddr.MaxLength = 2;
            this.txt_mtidaddr.Name = "txt_mtidaddr";
            this.txt_mtidaddr.Size = new System.Drawing.Size(64, 20);
            this.txt_mtidaddr.TabIndex = 3;
            this.txt_mtidaddr.Text = "02";
            // 
            // label104
            // 
            this.label104.AutoSize = true;
            this.label104.Location = new System.Drawing.Point(13, 28);
            this.label104.Name = "label104";
            this.label104.Size = new System.Drawing.Size(97, 13);
            this.label104.TabIndex = 0;
            this.label104.Text = "Start address(Hex):";
            // 
            // groupBox34
            // 
            this.groupBox34.Controls.Add(this.checkBox_mask);
            this.groupBox34.Controls.Add(this.txt_Mdata);
            this.groupBox34.Controls.Add(this.label100);
            this.groupBox34.Controls.Add(this.txt_Mlen);
            this.groupBox34.Controls.Add(this.label101);
            this.groupBox34.Controls.Add(this.txt_Maddr);
            this.groupBox34.Controls.Add(this.label102);
            this.groupBox34.Controls.Add(this.RBM_USER);
            this.groupBox34.Controls.Add(this.RBM_TID);
            this.groupBox34.Controls.Add(this.RBM_EPC);
            this.groupBox34.Location = new System.Drawing.Point(8, 75);
            this.groupBox34.Name = "groupBox34";
            this.groupBox34.Size = new System.Drawing.Size(420, 94);
            this.groupBox34.TabIndex = 82;
            this.groupBox34.TabStop = false;
            this.groupBox34.Text = "Mask setting";
            // 
            // checkBox_mask
            // 
            this.checkBox_mask.AutoSize = true;
            this.checkBox_mask.Location = new System.Drawing.Point(323, 14);
            this.checkBox_mask.Name = "checkBox_mask";
            this.checkBox_mask.Size = new System.Drawing.Size(59, 17);
            this.checkBox_mask.TabIndex = 9;
            this.checkBox_mask.Text = "Enable";
            this.checkBox_mask.UseVisualStyleBackColor = true;
            // 
            // txt_Mdata
            // 
            this.txt_Mdata.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txt_Mdata.Location = new System.Drawing.Point(94, 66);
            this.txt_Mdata.Name = "txt_Mdata";
            this.txt_Mdata.Size = new System.Drawing.Size(316, 20);
            this.txt_Mdata.TabIndex = 8;
            // 
            // label100
            // 
            this.label100.AutoSize = true;
            this.label100.Location = new System.Drawing.Point(6, 73);
            this.label100.Name = "label100";
            this.label100.Size = new System.Drawing.Size(58, 13);
            this.label100.TabIndex = 7;
            this.label100.Text = "Data(Hex):";
            // 
            // txt_Mlen
            // 
            this.txt_Mlen.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txt_Mlen.Location = new System.Drawing.Point(348, 36);
            this.txt_Mlen.MaxLength = 2;
            this.txt_Mlen.Name = "txt_Mlen";
            this.txt_Mlen.Size = new System.Drawing.Size(62, 20);
            this.txt_Mlen.TabIndex = 6;
            this.txt_Mlen.Text = "00";
            // 
            // label101
            // 
            this.label101.AutoSize = true;
            this.label101.Location = new System.Drawing.Point(247, 43);
            this.label101.Name = "label101";
            this.label101.Size = new System.Drawing.Size(68, 13);
            this.label101.TabIndex = 5;
            this.label101.Text = "Length(Hex):";
            // 
            // txt_Maddr
            // 
            this.txt_Maddr.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txt_Maddr.Location = new System.Drawing.Point(137, 36);
            this.txt_Maddr.MaxLength = 4;
            this.txt_Maddr.Name = "txt_Maddr";
            this.txt_Maddr.Size = new System.Drawing.Size(55, 20);
            this.txt_Maddr.TabIndex = 4;
            this.txt_Maddr.Text = "0000";
            // 
            // label102
            // 
            this.label102.AutoSize = true;
            this.label102.Location = new System.Drawing.Point(6, 43);
            this.label102.Name = "label102";
            this.label102.Size = new System.Drawing.Size(97, 13);
            this.label102.TabIndex = 3;
            this.label102.Text = "Start address(Hex):";
            // 
            // RBM_USER
            // 
            this.RBM_USER.AutoSize = true;
            this.RBM_USER.Location = new System.Drawing.Point(247, 15);
            this.RBM_USER.Name = "RBM_USER";
            this.RBM_USER.Size = new System.Drawing.Size(47, 17);
            this.RBM_USER.TabIndex = 2;
            this.RBM_USER.Text = "User";
            this.RBM_USER.UseVisualStyleBackColor = true;
            // 
            // RBM_TID
            // 
            this.RBM_TID.AutoSize = true;
            this.RBM_TID.Location = new System.Drawing.Point(136, 15);
            this.RBM_TID.Name = "RBM_TID";
            this.RBM_TID.Size = new System.Drawing.Size(43, 17);
            this.RBM_TID.TabIndex = 1;
            this.RBM_TID.Text = "TID";
            this.RBM_TID.UseVisualStyleBackColor = true;
            // 
            // RBM_EPC
            // 
            this.RBM_EPC.AutoSize = true;
            this.RBM_EPC.Checked = true;
            this.RBM_EPC.Location = new System.Drawing.Point(20, 15);
            this.RBM_EPC.Name = "RBM_EPC";
            this.RBM_EPC.Size = new System.Drawing.Size(46, 17);
            this.RBM_EPC.TabIndex = 0;
            this.RBM_EPC.TabStop = true;
            this.RBM_EPC.Text = "EPC";
            this.RBM_EPC.UseVisualStyleBackColor = true;
            // 
            // groupBox33
            // 
            this.groupBox33.Controls.Add(this.com_MS);
            this.groupBox33.Controls.Add(this.label99);
            this.groupBox33.Controls.Add(this.com_MQ);
            this.groupBox33.Controls.Add(this.label98);
            this.groupBox33.Location = new System.Drawing.Point(656, 6);
            this.groupBox33.Name = "groupBox33";
            this.groupBox33.Size = new System.Drawing.Size(394, 61);
            this.groupBox33.TabIndex = 81;
            this.groupBox33.TabStop = false;
            this.groupBox33.Text = "Mutiple query parameter";
            // 
            // com_MS
            // 
            this.com_MS.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_MS.FormattingEnabled = true;
            this.com_MS.Items.AddRange(new object[] {
            "S0",
            "S1",
            "S2",
            "S3",
            "AUTO"});
            this.com_MS.Location = new System.Drawing.Point(267, 25);
            this.com_MS.Name = "com_MS";
            this.com_MS.Size = new System.Drawing.Size(117, 21);
            this.com_MS.TabIndex = 3;
            // 
            // label99
            // 
            this.label99.AutoSize = true;
            this.label99.Location = new System.Drawing.Point(194, 28);
            this.label99.Name = "label99";
            this.label99.Size = new System.Drawing.Size(50, 13);
            this.label99.TabIndex = 2;
            this.label99.Text = "Session：";
            // 
            // com_MQ
            // 
            this.com_MQ.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_MQ.FormattingEnabled = true;
            this.com_MQ.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15"});
            this.com_MQ.Location = new System.Drawing.Point(47, 25);
            this.com_MQ.Name = "com_MQ";
            this.com_MQ.Size = new System.Drawing.Size(117, 21);
            this.com_MQ.TabIndex = 1;
            // 
            // label98
            // 
            this.label98.AutoSize = true;
            this.label98.Location = new System.Drawing.Point(7, 28);
            this.label98.Name = "label98";
            this.label98.Size = new System.Drawing.Size(21, 13);
            this.label98.TabIndex = 0;
            this.label98.Text = "Q：";
            // 
            // groupBox32
            // 
            this.groupBox32.Controls.Add(this.com_MFliterTime);
            this.groupBox32.Controls.Add(this.label97);
            this.groupBox32.Location = new System.Drawing.Point(436, 6);
            this.groupBox32.Name = "groupBox32";
            this.groupBox32.Size = new System.Drawing.Size(214, 61);
            this.groupBox32.TabIndex = 80;
            this.groupBox32.TabStop = false;
            this.groupBox32.Text = "Tag Filter";
            // 
            // com_MFliterTime
            // 
            this.com_MFliterTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_MFliterTime.FormattingEnabled = true;
            this.com_MFliterTime.Location = new System.Drawing.Point(87, 25);
            this.com_MFliterTime.Name = "com_MFliterTime";
            this.com_MFliterTime.Size = new System.Drawing.Size(117, 21);
            this.com_MFliterTime.TabIndex = 1;
            // 
            // label97
            // 
            this.label97.AutoSize = true;
            this.label97.Location = new System.Drawing.Point(7, 28);
            this.label97.Name = "label97";
            this.label97.Size = new System.Drawing.Size(54, 13);
            this.label97.TabIndex = 0;
            this.label97.Text = "Filter time:";
            // 
            // groupBox28
            // 
            this.groupBox28.Controls.Add(this.com_MRPTime);
            this.groupBox28.Controls.Add(this.label96);
            this.groupBox28.Location = new System.Drawing.Point(214, 6);
            this.groupBox28.Name = "groupBox28";
            this.groupBox28.Size = new System.Drawing.Size(214, 61);
            this.groupBox28.TabIndex = 79;
            this.groupBox28.TabStop = false;
            this.groupBox28.Text = "Pulse interval";
            // 
            // com_MRPTime
            // 
            this.com_MRPTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_MRPTime.FormattingEnabled = true;
            this.com_MRPTime.Items.AddRange(new object[] {
            "10ms",
            "20ms",
            "30ms",
            "50ms",
            "100ms"});
            this.com_MRPTime.Location = new System.Drawing.Point(73, 25);
            this.com_MRPTime.Name = "com_MRPTime";
            this.com_MRPTime.Size = new System.Drawing.Size(131, 21);
            this.com_MRPTime.TabIndex = 1;
            // 
            // label96
            // 
            this.label96.AutoSize = true;
            this.label96.Location = new System.Drawing.Point(7, 28);
            this.label96.Name = "label96";
            this.label96.Size = new System.Drawing.Size(62, 13);
            this.label96.TabIndex = 0;
            this.label96.Text = "Pulse Time:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.MRB_6B);
            this.groupBox3.Controls.Add(this.MRB_G2);
            this.groupBox3.Location = new System.Drawing.Point(8, 6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(200, 61);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Protocl";
            // 
            // MRB_6B
            // 
            this.MRB_6B.AutoSize = true;
            this.MRB_6B.Location = new System.Drawing.Point(106, 22);
            this.MRB_6B.Name = "MRB_6B";
            this.MRB_6B.Size = new System.Drawing.Size(71, 17);
            this.MRB_6B.TabIndex = 1;
            this.MRB_6B.Text = "18000-6B";
            this.MRB_6B.UseVisualStyleBackColor = true;
            // 
            // MRB_G2
            // 
            this.MRB_G2.AutoSize = true;
            this.MRB_G2.Checked = true;
            this.MRB_G2.Location = new System.Drawing.Point(10, 22);
            this.MRB_G2.Name = "MRB_G2";
            this.MRB_G2.Size = new System.Drawing.Size(76, 17);
            this.MRB_G2.TabIndex = 0;
            this.MRB_G2.TabStop = true;
            this.MRB_G2.Text = "EPCC1-G2";
            this.MRB_G2.UseVisualStyleBackColor = true;
            // 
            // Main_Page2
            // 
            this.Main_Page2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.Main_Page2.Controls.Add(this.tabControl2);
            this.Main_Page2.Location = new System.Drawing.Point(4, 27);
            this.Main_Page2.Name = "Main_Page2";
            this.Main_Page2.Padding = new System.Windows.Forms.Padding(3);
            this.Main_Page2.Size = new System.Drawing.Size(1071, 621);
            this.Main_Page2.TabIndex = 1;
            this.Main_Page2.Text = "EPCC1-G2 Test";
            this.Main_Page2.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage_answer);
            this.tabControl2.Controls.Add(this.tabPage_Realtime);
            this.tabControl2.Controls.Add(this.tabPage_Buff);
            this.tabControl2.Controls.Add(this.tabPage_RW);
            this.tabControl2.ItemSize = new System.Drawing.Size(120, 23);
            this.tabControl2.Location = new System.Drawing.Point(-1, 6);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(1071, 612);
            this.tabControl2.TabIndex = 0;
            this.tabControl2.SelectedIndexChanged += new System.EventHandler(this.Maintab_SelectedIndexChanged);
            // 
            // tabPage_answer
            // 
            this.tabPage_answer.Controls.Add(this.panel1);
            this.tabPage_answer.Location = new System.Drawing.Point(4, 27);
            this.tabPage_answer.Name = "tabPage_answer";
            this.tabPage_answer.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_answer.Size = new System.Drawing.Size(1063, 581);
            this.tabPage_answer.TabIndex = 0;
            this.tabPage_answer.Text = "Answer Mode";
            this.tabPage_answer.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lxLedControl6);
            this.panel1.Controls.Add(this.label119);
            this.panel1.Controls.Add(this.gbp_MixRead);
            this.panel1.Controls.Add(this.lxLedControl5);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.lxLedControl4);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.lxLedControl3);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.lxLedControl2);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.lxLedControl1);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.group_ant1);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Enabled = false;
            this.panel1.Location = new System.Drawing.Point(2, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1066, 574);
            this.panel1.TabIndex = 0;
            // 
            // lxLedControl6
            // 
            this.lxLedControl6.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl6.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl6.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl6.BevelRate = 0.5F;
            this.lxLedControl6.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl6.BorderWidth = 3;
            this.lxLedControl6.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl6.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl6.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLedControl6.HighlightOpaque = ((byte)(50));
            this.lxLedControl6.Location = new System.Drawing.Point(712, 247);
            this.lxLedControl6.Name = "lxLedControl6";
            this.lxLedControl6.RoundCorner = true;
            this.lxLedControl6.ShowHighlight = true;
            this.lxLedControl6.Size = new System.Drawing.Size(334, 92);
            this.lxLedControl6.TabIndex = 31;
            this.lxLedControl6.Text = "0";
            this.lxLedControl6.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl6.TotalCharCount = 7;
            // 
            // label119
            // 
            this.label119.AutoSize = true;
            this.label119.Location = new System.Drawing.Point(709, 226);
            this.label119.Name = "label119";
            this.label119.Size = new System.Drawing.Size(68, 13);
            this.label119.TabIndex = 30;
            this.label119.Text = "TID Number:";
            // 
            // gbp_MixRead
            // 
            this.gbp_MixRead.Controls.Add(this.text_readpsd);
            this.gbp_MixRead.Controls.Add(this.label118);
            this.gbp_MixRead.Controls.Add(this.text_readLen);
            this.gbp_MixRead.Controls.Add(this.label117);
            this.gbp_MixRead.Controls.Add(this.text_readadr);
            this.gbp_MixRead.Controls.Add(this.label115);
            this.gbp_MixRead.Controls.Add(this.com_MixMem);
            this.gbp_MixRead.Controls.Add(this.label114);
            this.gbp_MixRead.Enabled = false;
            this.gbp_MixRead.Location = new System.Drawing.Point(396, 1);
            this.gbp_MixRead.Name = "gbp_MixRead";
            this.gbp_MixRead.Size = new System.Drawing.Size(222, 77);
            this.gbp_MixRead.TabIndex = 29;
            this.gbp_MixRead.TabStop = false;
            this.gbp_MixRead.Text = "Mix";
            // 
            // text_readpsd
            // 
            this.text_readpsd.Location = new System.Drawing.Point(64, 46);
            this.text_readpsd.MaxLength = 8;
            this.text_readpsd.Name = "text_readpsd";
            this.text_readpsd.Size = new System.Drawing.Size(65, 20);
            this.text_readpsd.TabIndex = 7;
            this.text_readpsd.Text = "00000000";
            this.text_readpsd.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label118
            // 
            this.label118.AutoSize = true;
            this.label118.Location = new System.Drawing.Point(6, 50);
            this.label118.Name = "label118";
            this.label118.Size = new System.Drawing.Size(56, 13);
            this.label118.TabIndex = 6;
            this.label118.Text = "Password:";
            // 
            // text_readLen
            // 
            this.text_readLen.Location = new System.Drawing.Point(176, 48);
            this.text_readLen.MaxLength = 2;
            this.text_readLen.Name = "text_readLen";
            this.text_readLen.Size = new System.Drawing.Size(41, 20);
            this.text_readLen.TabIndex = 5;
            this.text_readLen.Text = "04";
            this.text_readLen.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label117
            // 
            this.label117.AutoSize = true;
            this.label117.Location = new System.Drawing.Point(136, 52);
            this.label117.Name = "label117";
            this.label117.Size = new System.Drawing.Size(28, 13);
            this.label117.TabIndex = 4;
            this.label117.Text = "Len:";
            // 
            // text_readadr
            // 
            this.text_readadr.Location = new System.Drawing.Point(176, 14);
            this.text_readadr.MaxLength = 4;
            this.text_readadr.Name = "text_readadr";
            this.text_readadr.Size = new System.Drawing.Size(41, 20);
            this.text_readadr.TabIndex = 3;
            this.text_readadr.Text = "0002";
            this.text_readadr.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label115
            // 
            this.label115.AutoSize = true;
            this.label115.Location = new System.Drawing.Point(136, 18);
            this.label115.Name = "label115";
            this.label115.Size = new System.Drawing.Size(32, 13);
            this.label115.TabIndex = 2;
            this.label115.Text = "Addr:";
            // 
            // com_MixMem
            // 
            this.com_MixMem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_MixMem.FormattingEnabled = true;
            this.com_MixMem.Items.AddRange(new object[] {
            "Password",
            "EPC",
            "TID",
            "User"});
            this.com_MixMem.Location = new System.Drawing.Point(64, 13);
            this.com_MixMem.Name = "com_MixMem";
            this.com_MixMem.Size = new System.Drawing.Size(66, 21);
            this.com_MixMem.TabIndex = 1;
            // 
            // label114
            // 
            this.label114.AutoSize = true;
            this.label114.Location = new System.Drawing.Point(5, 17);
            this.label114.Name = "label114";
            this.label114.Size = new System.Drawing.Size(33, 13);
            this.label114.TabIndex = 0;
            this.label114.Text = "Mem:";
            // 
            // lxLedControl5
            // 
            this.lxLedControl5.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl5.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl5.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl5.BevelRate = 0.5F;
            this.lxLedControl5.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl5.BorderWidth = 3;
            this.lxLedControl5.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl5.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl5.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLedControl5.HighlightOpaque = ((byte)(50));
            this.lxLedControl5.Location = new System.Drawing.Point(886, 486);
            this.lxLedControl5.Name = "lxLedControl5";
            this.lxLedControl5.RoundCorner = true;
            this.lxLedControl5.ShowHighlight = true;
            this.lxLedControl5.Size = new System.Drawing.Size(158, 42);
            this.lxLedControl5.TabIndex = 28;
            this.lxLedControl5.Text = "0";
            this.lxLedControl5.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl5.TotalCharCount = 7;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label9.Location = new System.Drawing.Point(884, 465);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(98, 13);
            this.label9.TabIndex = 27;
            this.label9.Text = "Total-cmd-time(ms):";
            // 
            // lxLedControl4
            // 
            this.lxLedControl4.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl4.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl4.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl4.BevelRate = 0.5F;
            this.lxLedControl4.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl4.BorderWidth = 3;
            this.lxLedControl4.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl4.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl4.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLedControl4.HighlightOpaque = ((byte)(50));
            this.lxLedControl4.Location = new System.Drawing.Point(711, 485);
            this.lxLedControl4.Name = "lxLedControl4";
            this.lxLedControl4.RoundCorner = true;
            this.lxLedControl4.ShowHighlight = true;
            this.lxLedControl4.Size = new System.Drawing.Size(164, 42);
            this.lxLedControl4.TabIndex = 26;
            this.lxLedControl4.Text = "0";
            this.lxLedControl4.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl4.TotalCharCount = 7;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label8.Location = new System.Drawing.Point(709, 465);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 13);
            this.label8.TabIndex = 25;
            this.label8.Text = "Total tag number:";
            // 
            // lxLedControl3
            // 
            this.lxLedControl3.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl3.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl3.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl3.BevelRate = 0.5F;
            this.lxLedControl3.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl3.BorderWidth = 3;
            this.lxLedControl3.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl3.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl3.ForeColor = System.Drawing.Color.Purple;
            this.lxLedControl3.HighlightOpaque = ((byte)(50));
            this.lxLedControl3.Location = new System.Drawing.Point(886, 398);
            this.lxLedControl3.Name = "lxLedControl3";
            this.lxLedControl3.RoundCorner = true;
            this.lxLedControl3.ShowHighlight = true;
            this.lxLedControl3.Size = new System.Drawing.Size(160, 30);
            this.lxLedControl3.TabIndex = 24;
            this.lxLedControl3.Text = "0";
            this.lxLedControl3.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl3.TotalCharCount = 10;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label7.Location = new System.Drawing.Point(884, 372);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "cmd time(ms):";
            // 
            // lxLedControl2
            // 
            this.lxLedControl2.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl2.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl2.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl2.BevelRate = 0.5F;
            this.lxLedControl2.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl2.BorderWidth = 3;
            this.lxLedControl2.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl2.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl2.ForeColor = System.Drawing.Color.Purple;
            this.lxLedControl2.HighlightOpaque = ((byte)(50));
            this.lxLedControl2.Location = new System.Drawing.Point(711, 398);
            this.lxLedControl2.Name = "lxLedControl2";
            this.lxLedControl2.RoundCorner = true;
            this.lxLedControl2.ShowHighlight = true;
            this.lxLedControl2.Size = new System.Drawing.Size(160, 30);
            this.lxLedControl2.TabIndex = 22;
            this.lxLedControl2.Text = "0";
            this.lxLedControl2.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl2.TotalCharCount = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label6.Location = new System.Drawing.Point(709, 372);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 21;
            this.label6.Text = "Speed:";
            // 
            // lxLedControl1
            // 
            this.lxLedControl1.BackColor = System.Drawing.Color.Transparent;
            this.lxLedControl1.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLedControl1.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLedControl1.BevelRate = 0.5F;
            this.lxLedControl1.BorderColor = System.Drawing.Color.Lavender;
            this.lxLedControl1.BorderWidth = 3;
            this.lxLedControl1.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLedControl1.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLedControl1.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLedControl1.HighlightOpaque = ((byte)(50));
            this.lxLedControl1.Location = new System.Drawing.Point(712, 119);
            this.lxLedControl1.Name = "lxLedControl1";
            this.lxLedControl1.RoundCorner = true;
            this.lxLedControl1.ShowHighlight = true;
            this.lxLedControl1.Size = new System.Drawing.Size(334, 92);
            this.lxLedControl1.TabIndex = 20;
            this.lxLedControl1.Text = "0";
            this.lxLedControl1.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLedControl1.TotalCharCount = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(709, 99);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(71, 13);
            this.label5.TabIndex = 19;
            this.label5.Text = "EPC Number:";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.dataGridView1);
            this.groupBox5.Location = new System.Drawing.Point(4, 86);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(678, 482);
            this.groupBox5.TabIndex = 18;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Tag list(No Repeat)";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(6, 22);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(664, 456);
            this.dataGridView1.TabIndex = 1;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBox_rate);
            this.groupBox4.Controls.Add(this.label80);
            this.groupBox4.Controls.Add(this.text_target);
            this.groupBox4.Controls.Add(this.check_num);
            this.groupBox4.Controls.Add(this.com_Target);
            this.groupBox4.Controls.Add(this.label79);
            this.groupBox4.Controls.Add(this.com_scantime);
            this.groupBox4.Controls.Add(this.label39);
            this.groupBox4.Controls.Add(this.com_S);
            this.groupBox4.Controls.Add(this.label32);
            this.groupBox4.Controls.Add(this.com_Q);
            this.groupBox4.Controls.Add(this.label31);
            this.groupBox4.Location = new System.Drawing.Point(619, 0);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(437, 77);
            this.groupBox4.TabIndex = 17;
            this.groupBox4.TabStop = false;
            // 
            // checkBox_rate
            // 
            this.checkBox_rate.AutoSize = true;
            this.checkBox_rate.Location = new System.Drawing.Point(317, 50);
            this.checkBox_rate.Name = "checkBox_rate";
            this.checkBox_rate.Size = new System.Drawing.Size(92, 17);
            this.checkBox_rate.TabIndex = 37;
            this.checkBox_rate.Text = "Return Speed";
            this.checkBox_rate.UseVisualStyleBackColor = true;
            // 
            // label80
            // 
            this.label80.AutoSize = true;
            this.label80.Location = new System.Drawing.Point(102, 52);
            this.label80.Name = "label80";
            this.label80.Size = new System.Drawing.Size(168, 13);
            this.label80.TabIndex = 36;
            this.label80.Text = "times no tag.then A/B conversion ";
            // 
            // text_target
            // 
            this.text_target.ForeColor = System.Drawing.Color.Red;
            this.text_target.Location = new System.Drawing.Point(55, 46);
            this.text_target.MaxLength = 2;
            this.text_target.Name = "text_target";
            this.text_target.Size = new System.Drawing.Size(43, 20);
            this.text_target.TabIndex = 35;
            this.text_target.Text = "5";
            this.text_target.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.text_target.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TB_Port_KeyPress);
            // 
            // check_num
            // 
            this.check_num.AutoSize = true;
            this.check_num.Checked = true;
            this.check_num.CheckState = System.Windows.Forms.CheckState.Checked;
            this.check_num.Location = new System.Drawing.Point(7, 50);
            this.check_num.Name = "check_num";
            this.check_num.Size = new System.Drawing.Size(52, 17);
            this.check_num.TabIndex = 34;
            this.check_num.Text = "Read";
            this.check_num.UseVisualStyleBackColor = true;
            // 
            // com_Target
            // 
            this.com_Target.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_Target.FormattingEnabled = true;
            this.com_Target.Items.AddRange(new object[] {
            "A",
            "B"});
            this.com_Target.Location = new System.Drawing.Point(384, 15);
            this.com_Target.Name = "com_Target";
            this.com_Target.Size = new System.Drawing.Size(51, 21);
            this.com_Target.TabIndex = 33;
            // 
            // label79
            // 
            this.label79.AutoSize = true;
            this.label79.Location = new System.Drawing.Point(339, 20);
            this.label79.Name = "label79";
            this.label79.Size = new System.Drawing.Size(41, 13);
            this.label79.TabIndex = 32;
            this.label79.Text = "Target:";
            // 
            // com_scantime
            // 
            this.com_scantime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_scantime.FormattingEnabled = true;
            this.com_scantime.Location = new System.Drawing.Point(258, 15);
            this.com_scantime.Name = "com_scantime";
            this.com_scantime.Size = new System.Drawing.Size(77, 21);
            this.com_scantime.TabIndex = 31;
            // 
            // label39
            // 
            this.label39.AutoSize = true;
            this.label39.Location = new System.Drawing.Point(191, 20);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(58, 13);
            this.label39.TabIndex = 30;
            this.label39.Text = "ScanTime:";
            // 
            // com_S
            // 
            this.com_S.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_S.FormattingEnabled = true;
            this.com_S.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "AUTO"});
            this.com_S.Location = new System.Drawing.Point(125, 21);
            this.com_S.Name = "com_S";
            this.com_S.Size = new System.Drawing.Size(59, 21);
            this.com_S.TabIndex = 29;
            this.com_S.SelectedIndexChanged += new System.EventHandler(this.Com_S_SelectedIndexChanged);
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(71, 20);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(50, 13);
            this.label32.TabIndex = 28;
            this.label32.Text = "Session：";
            // 
            // com_Q
            // 
            this.com_Q.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_Q.FormattingEnabled = true;
            this.com_Q.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15"});
            this.com_Q.Location = new System.Drawing.Point(18, 15);
            this.com_Q.Name = "com_Q";
            this.com_Q.Size = new System.Drawing.Size(51, 21);
            this.com_Q.TabIndex = 27;
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(3, 20);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(18, 13);
            this.label31.TabIndex = 26;
            this.label31.Text = "Q:";
            // 
            // group_ant1
            // 
            this.group_ant1.Controls.Add(this.check_ant4);
            this.group_ant1.Controls.Add(this.check_ant3);
            this.group_ant1.Controls.Add(this.check_ant2);
            this.group_ant1.Controls.Add(this.check_ant1);
            this.group_ant1.Location = new System.Drawing.Point(262, 1);
            this.group_ant1.Name = "group_ant1";
            this.group_ant1.Size = new System.Drawing.Size(135, 77);
            this.group_ant1.TabIndex = 16;
            this.group_ant1.TabStop = false;
            // 
            // check_ant4
            // 
            this.check_ant4.AutoSize = true;
            this.check_ant4.ForeColor = System.Drawing.Color.RoyalBlue;
            this.check_ant4.Location = new System.Drawing.Point(80, 47);
            this.check_ant4.Name = "check_ant4";
            this.check_ant4.Size = new System.Drawing.Size(54, 17);
            this.check_ant4.TabIndex = 37;
            this.check_ant4.Text = "ANT4";
            this.check_ant4.UseVisualStyleBackColor = true;
            // 
            // check_ant3
            // 
            this.check_ant3.AutoSize = true;
            this.check_ant3.ForeColor = System.Drawing.Color.RoyalBlue;
            this.check_ant3.Location = new System.Drawing.Point(20, 47);
            this.check_ant3.Name = "check_ant3";
            this.check_ant3.Size = new System.Drawing.Size(54, 17);
            this.check_ant3.TabIndex = 36;
            this.check_ant3.Text = "ANT3";
            this.check_ant3.UseVisualStyleBackColor = true;
            // 
            // check_ant2
            // 
            this.check_ant2.AutoSize = true;
            this.check_ant2.ForeColor = System.Drawing.Color.RoyalBlue;
            this.check_ant2.Location = new System.Drawing.Point(80, 17);
            this.check_ant2.Name = "check_ant2";
            this.check_ant2.Size = new System.Drawing.Size(54, 17);
            this.check_ant2.TabIndex = 35;
            this.check_ant2.Text = "ANT2";
            this.check_ant2.UseVisualStyleBackColor = true;
            // 
            // check_ant1
            // 
            this.check_ant1.AutoSize = true;
            this.check_ant1.Checked = true;
            this.check_ant1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.check_ant1.ForeColor = System.Drawing.Color.RoyalBlue;
            this.check_ant1.Location = new System.Drawing.Point(20, 17);
            this.check_ant1.Name = "check_ant1";
            this.check_ant1.Size = new System.Drawing.Size(54, 17);
            this.check_ant1.TabIndex = 34;
            this.check_ant1.Text = "ANT1";
            this.check_ant1.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rb_fastid);
            this.groupBox2.Controls.Add(this.rb_mix);
            this.groupBox2.Controls.Add(this.rb_tid);
            this.groupBox2.Controls.Add(this.rb_epc);
            this.groupBox2.Controls.Add(this.btIventoryG2);
            this.groupBox2.Location = new System.Drawing.Point(5, 1);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(257, 77);
            this.groupBox2.TabIndex = 15;
            this.groupBox2.TabStop = false;
            // 
            // rb_fastid
            // 
            this.rb_fastid.AutoSize = true;
            this.rb_fastid.Location = new System.Drawing.Point(123, 47);
            this.rb_fastid.Name = "rb_fastid";
            this.rb_fastid.Size = new System.Drawing.Size(56, 17);
            this.rb_fastid.TabIndex = 4;
            this.rb_fastid.Text = "FastID";
            this.rb_fastid.UseVisualStyleBackColor = true;
            this.rb_fastid.CheckedChanged += new System.EventHandler(this.RB_FastId_CheckedChanged);
            // 
            // rb_mix
            // 
            this.rb_mix.AutoSize = true;
            this.rb_mix.Location = new System.Drawing.Point(207, 47);
            this.rb_mix.Name = "rb_mix";
            this.rb_mix.Size = new System.Drawing.Size(44, 17);
            this.rb_mix.TabIndex = 3;
            this.rb_mix.Text = "MIX";
            this.rb_mix.UseVisualStyleBackColor = true;
            this.rb_mix.CheckedChanged += new System.EventHandler(this.RB_Mix_CheckedChanged);
            this.rb_mix.Click += new System.EventHandler(this.RB_Mix_Click);
            // 
            // rb_tid
            // 
            this.rb_tid.AutoSize = true;
            this.rb_tid.Location = new System.Drawing.Point(207, 21);
            this.rb_tid.Name = "rb_tid";
            this.rb_tid.Size = new System.Drawing.Size(43, 17);
            this.rb_tid.TabIndex = 2;
            this.rb_tid.Text = "TID";
            this.rb_tid.UseVisualStyleBackColor = true;
            this.rb_tid.CheckedChanged += new System.EventHandler(this.RB_TID_CheckedChanged);
            // 
            // rb_epc
            // 
            this.rb_epc.AutoSize = true;
            this.rb_epc.Checked = true;
            this.rb_epc.Location = new System.Drawing.Point(123, 22);
            this.rb_epc.Name = "rb_epc";
            this.rb_epc.Size = new System.Drawing.Size(46, 17);
            this.rb_epc.TabIndex = 1;
            this.rb_epc.TabStop = true;
            this.rb_epc.Text = "EPC";
            this.rb_epc.UseVisualStyleBackColor = true;
            this.rb_epc.CheckedChanged += new System.EventHandler(this.RB_EPC_CheckedChanged);
            // 
            // btIventoryG2
            // 
            this.btIventoryG2.BackColor = System.Drawing.Color.Transparent;
            this.btIventoryG2.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btIventoryG2.ForeColor = System.Drawing.Color.DarkBlue;
            this.btIventoryG2.Location = new System.Drawing.Point(6, 22);
            this.btIventoryG2.Name = "btIventoryG2";
            this.btIventoryG2.Size = new System.Drawing.Size(109, 43);
            this.btIventoryG2.TabIndex = 0;
            this.btIventoryG2.Text = "Start";
            this.btIventoryG2.UseVisualStyleBackColor = false;
            this.btIventoryG2.Click += new System.EventHandler(this.BtIventoryG2_Click);
            // 
            // tabPage_Realtime
            // 
            this.tabPage_Realtime.Controls.Add(this.panel10);
            this.tabPage_Realtime.Location = new System.Drawing.Point(4, 27);
            this.tabPage_Realtime.Name = "tabPage_Realtime";
            this.tabPage_Realtime.Size = new System.Drawing.Size(1063, 581);
            this.tabPage_Realtime.TabIndex = 4;
            this.tabPage_Realtime.Text = "Real-time-inventory";
            this.tabPage_Realtime.UseVisualStyleBackColor = true;
            // 
            // panel10
            // 
            this.panel10.Controls.Add(this.dataGridView4);
            this.panel10.Controls.Add(this.groupBox41);
            this.panel10.Controls.Add(this.groupBox42);
            this.panel10.Location = new System.Drawing.Point(1, 0);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(1063, 577);
            this.panel10.TabIndex = 0;
            // 
            // dataGridView4
            // 
            this.dataGridView4.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.dataGridView4.AllowUserToAddRows = false;
            this.dataGridView4.AllowUserToDeleteRows = false;
            this.dataGridView4.AllowUserToResizeColumns = false;
            this.dataGridView4.AllowUserToResizeRows = false;
            this.dataGridView4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dataGridView4.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridView4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView4.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView4.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn10,
            this.dataGridViewTextBoxColumn11,
            this.Column1,
            this.dataGridViewTextBoxColumn14,
            this.dataGridViewTextBoxColumn15});
            this.dataGridView4.Location = new System.Drawing.Point(358, 22);
            this.dataGridView4.MultiSelect = false;
            this.dataGridView4.Name = "dataGridView4";
            this.dataGridView4.ReadOnly = true;
            this.dataGridView4.RowHeadersVisible = false;
            this.dataGridView4.RowTemplate.Height = 23;
            this.dataGridView4.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dataGridView4.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView4.Size = new System.Drawing.Size(702, 551);
            this.dataGridView4.TabIndex = 26;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.HeaderText = "No.";
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn10.Width = 80;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.HeaderText = "EPC";
            this.dataGridViewTextBoxColumn11.Name = "dataGridViewTextBoxColumn11";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            this.dataGridViewTextBoxColumn11.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn11.Width = 310;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Length";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.HeaderText = "Antenna";
            this.dataGridViewTextBoxColumn14.Name = "dataGridViewTextBoxColumn14";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            this.dataGridViewTextBoxColumn14.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.HeaderText = "RSSI";
            this.dataGridViewTextBoxColumn15.Name = "dataGridViewTextBoxColumn15";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            this.dataGridViewTextBoxColumn15.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // groupBox41
            // 
            this.groupBox41.Controls.Add(this.lxLed_Mtime);
            this.groupBox41.Controls.Add(this.label113);
            this.groupBox41.Controls.Add(this.lxLed_Mtag);
            this.groupBox41.Controls.Add(this.label116);
            this.groupBox41.Location = new System.Drawing.Point(3, 79);
            this.groupBox41.Name = "groupBox41";
            this.groupBox41.Size = new System.Drawing.Size(349, 243);
            this.groupBox41.TabIndex = 25;
            this.groupBox41.TabStop = false;
            this.groupBox41.Text = "Data";
            // 
            // lxLed_Mtime
            // 
            this.lxLed_Mtime.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_Mtime.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_Mtime.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_Mtime.BevelRate = 0.5F;
            this.lxLed_Mtime.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_Mtime.BorderWidth = 3;
            this.lxLed_Mtime.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_Mtime.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_Mtime.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLed_Mtime.HighlightOpaque = ((byte)(50));
            this.lxLed_Mtime.Location = new System.Drawing.Point(22, 176);
            this.lxLed_Mtime.Name = "lxLed_Mtime";
            this.lxLed_Mtime.RoundCorner = true;
            this.lxLed_Mtime.ShowHighlight = true;
            this.lxLed_Mtime.Size = new System.Drawing.Size(186, 50);
            this.lxLed_Mtime.TabIndex = 32;
            this.lxLed_Mtime.Text = "0";
            this.lxLed_Mtime.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_Mtime.TotalCharCount = 7;
            // 
            // label113
            // 
            this.label113.AutoSize = true;
            this.label113.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label113.Location = new System.Drawing.Point(20, 146);
            this.label113.Name = "label113";
            this.label113.Size = new System.Drawing.Size(79, 13);
            this.label113.TabIndex = 31;
            this.label113.Text = "Total Time(ms):";
            // 
            // lxLed_Mtag
            // 
            this.lxLed_Mtag.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_Mtag.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_Mtag.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_Mtag.BevelRate = 0.5F;
            this.lxLed_Mtag.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_Mtag.BorderWidth = 3;
            this.lxLed_Mtag.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_Mtag.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_Mtag.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLed_Mtag.HighlightOpaque = ((byte)(50));
            this.lxLed_Mtag.Location = new System.Drawing.Point(22, 75);
            this.lxLed_Mtag.Name = "lxLed_Mtag";
            this.lxLed_Mtag.RoundCorner = true;
            this.lxLed_Mtag.ShowHighlight = true;
            this.lxLed_Mtag.Size = new System.Drawing.Size(186, 50);
            this.lxLed_Mtag.TabIndex = 22;
            this.lxLed_Mtag.Text = "0";
            this.lxLed_Mtag.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_Mtag.TotalCharCount = 7;
            // 
            // label116
            // 
            this.label116.AutoSize = true;
            this.label116.Location = new System.Drawing.Point(21, 44);
            this.label116.Name = "label116";
            this.label116.Size = new System.Drawing.Size(69, 13);
            this.label116.TabIndex = 21;
            this.label116.Text = "Tag Number:";
            // 
            // groupBox42
            // 
            this.groupBox42.Controls.Add(this.btStartMactive);
            this.groupBox42.Location = new System.Drawing.Point(4, 0);
            this.groupBox42.Name = "groupBox42";
            this.groupBox42.Size = new System.Drawing.Size(348, 72);
            this.groupBox42.TabIndex = 24;
            this.groupBox42.TabStop = false;
            // 
            // btStartMactive
            // 
            this.btStartMactive.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btStartMactive.ForeColor = System.Drawing.Color.DarkBlue;
            this.btStartMactive.Location = new System.Drawing.Point(21, 17);
            this.btStartMactive.Name = "btStartMactive";
            this.btStartMactive.Size = new System.Drawing.Size(296, 43);
            this.btStartMactive.TabIndex = 0;
            this.btStartMactive.Text = "Start";
            this.btStartMactive.UseVisualStyleBackColor = true;
            this.btStartMactive.Click += new System.EventHandler(this.BtStartMactive_Click);
            // 
            // tabPage_Buff
            // 
            this.tabPage_Buff.Controls.Add(this.panel8);
            this.tabPage_Buff.Location = new System.Drawing.Point(4, 27);
            this.tabPage_Buff.Name = "tabPage_Buff";
            this.tabPage_Buff.Size = new System.Drawing.Size(1063, 581);
            this.tabPage_Buff.TabIndex = 3;
            this.tabPage_Buff.Text = "Buffer operation";
            this.tabPage_Buff.UseVisualStyleBackColor = true;
            // 
            // panel8
            // 
            this.panel8.Controls.Add(this.groupBox27);
            this.panel8.Controls.Add(this.groupBox29);
            this.panel8.Controls.Add(this.dataGridView3);
            this.panel8.Controls.Add(this.groupBox30);
            this.panel8.Location = new System.Drawing.Point(-2, 1);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(1067, 575);
            this.panel8.TabIndex = 1;
            // 
            // groupBox27
            // 
            this.groupBox27.Controls.Add(this.btQueryBuffNum);
            this.groupBox27.Controls.Add(this.btRandCbuff);
            this.groupBox27.Controls.Add(this.btClearBuff);
            this.groupBox27.Controls.Add(this.btReadBuff);
            this.groupBox27.Location = new System.Drawing.Point(315, 5);
            this.groupBox27.Name = "groupBox27";
            this.groupBox27.Size = new System.Drawing.Size(370, 77);
            this.groupBox27.TabIndex = 25;
            this.groupBox27.TabStop = false;
            // 
            // btQueryBuffNum
            // 
            this.btQueryBuffNum.ForeColor = System.Drawing.Color.Blue;
            this.btQueryBuffNum.Location = new System.Drawing.Point(194, 46);
            this.btQueryBuffNum.Name = "btQueryBuffNum";
            this.btQueryBuffNum.Size = new System.Drawing.Size(151, 25);
            this.btQueryBuffNum.TabIndex = 3;
            this.btQueryBuffNum.Text = "Get buffer tag number";
            this.btQueryBuffNum.UseVisualStyleBackColor = true;
            this.btQueryBuffNum.Click += new System.EventHandler(this.BtQueryBuffNum_Click);
            // 
            // btRandCbuff
            // 
            this.btRandCbuff.ForeColor = System.Drawing.Color.Blue;
            this.btRandCbuff.Location = new System.Drawing.Point(194, 14);
            this.btRandCbuff.Name = "btRandCbuff";
            this.btRandCbuff.Size = new System.Drawing.Size(151, 25);
            this.btRandCbuff.TabIndex = 2;
            this.btRandCbuff.Text = "Read and Clear Buffer";
            this.btRandCbuff.UseVisualStyleBackColor = true;
            this.btRandCbuff.Click += new System.EventHandler(this.BtRandCbuff_Click);
            // 
            // btClearBuff
            // 
            this.btClearBuff.ForeColor = System.Drawing.Color.Blue;
            this.btClearBuff.Location = new System.Drawing.Point(31, 46);
            this.btClearBuff.Name = "btClearBuff";
            this.btClearBuff.Size = new System.Drawing.Size(151, 25);
            this.btClearBuff.TabIndex = 1;
            this.btClearBuff.Text = "Clear Buffer";
            this.btClearBuff.UseVisualStyleBackColor = true;
            this.btClearBuff.Click += new System.EventHandler(this.BtClearBuff_Click);
            // 
            // btReadBuff
            // 
            this.btReadBuff.ForeColor = System.Drawing.Color.Blue;
            this.btReadBuff.Location = new System.Drawing.Point(31, 14);
            this.btReadBuff.Name = "btReadBuff";
            this.btReadBuff.Size = new System.Drawing.Size(151, 25);
            this.btReadBuff.TabIndex = 0;
            this.btReadBuff.Text = "Read Buffer";
            this.btReadBuff.UseVisualStyleBackColor = true;
            this.btReadBuff.Click += new System.EventHandler(this.BtReadBuff_Click);
            // 
            // groupBox29
            // 
            this.groupBox29.Controls.Add(this.rb_btid);
            this.groupBox29.Controls.Add(this.rb_bepc);
            this.groupBox29.Controls.Add(this.btStartBuff);
            this.groupBox29.Location = new System.Drawing.Point(8, 5);
            this.groupBox29.Name = "groupBox29";
            this.groupBox29.Size = new System.Drawing.Size(307, 77);
            this.groupBox29.TabIndex = 23;
            this.groupBox29.TabStop = false;
            // 
            // rb_btid
            // 
            this.rb_btid.AutoSize = true;
            this.rb_btid.Location = new System.Drawing.Point(214, 46);
            this.rb_btid.Name = "rb_btid";
            this.rb_btid.Size = new System.Drawing.Size(43, 17);
            this.rb_btid.TabIndex = 2;
            this.rb_btid.Text = "TID";
            this.rb_btid.UseVisualStyleBackColor = true;
            // 
            // rb_bepc
            // 
            this.rb_bepc.AutoSize = true;
            this.rb_bepc.Checked = true;
            this.rb_bepc.Location = new System.Drawing.Point(214, 22);
            this.rb_bepc.Name = "rb_bepc";
            this.rb_bepc.Size = new System.Drawing.Size(46, 17);
            this.rb_bepc.TabIndex = 1;
            this.rb_bepc.TabStop = true;
            this.rb_bepc.Text = "EPC";
            this.rb_bepc.UseVisualStyleBackColor = true;
            // 
            // btStartBuff
            // 
            this.btStartBuff.BackColor = System.Drawing.Color.Transparent;
            this.btStartBuff.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btStartBuff.ForeColor = System.Drawing.Color.DarkBlue;
            this.btStartBuff.Location = new System.Drawing.Point(18, 22);
            this.btStartBuff.Name = "btStartBuff";
            this.btStartBuff.Size = new System.Drawing.Size(171, 43);
            this.btStartBuff.TabIndex = 0;
            this.btStartBuff.Text = "Start";
            this.btStartBuff.UseVisualStyleBackColor = false;
            this.btStartBuff.Click += new System.EventHandler(this.BtStartBuff_Click);
            // 
            // dataGridView3
            // 
            this.dataGridView3.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.dataGridView3.AllowUserToAddRows = false;
            this.dataGridView3.AllowUserToDeleteRows = false;
            this.dataGridView3.AllowUserToResizeColumns = false;
            this.dataGridView3.AllowUserToResizeRows = false;
            this.dataGridView3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dataGridView3.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridView3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView3.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView3.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7,
            this.dataGridViewTextBoxColumn8,
            this.dataGridViewTextBoxColumn9,
            this.Column7});
            this.dataGridView3.Location = new System.Drawing.Point(3, 314);
            this.dataGridView3.MultiSelect = false;
            this.dataGridView3.Name = "dataGridView3";
            this.dataGridView3.ReadOnly = true;
            this.dataGridView3.RowHeadersVisible = false;
            this.dataGridView3.RowTemplate.Height = 23;
            this.dataGridView3.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dataGridView3.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView3.Size = new System.Drawing.Size(1057, 251);
            this.dataGridView3.TabIndex = 21;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "No.";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn5.Width = 80;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "EPC";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn6.Width = 520;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.HeaderText = "Lenth";
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.HeaderText = "RSSI";
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn8.Width = 150;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "RSSI";
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column7
            // 
            this.Column7.HeaderText = "Times";
            this.Column7.Name = "Column7";
            this.Column7.ReadOnly = true;
            // 
            // groupBox30
            // 
            this.groupBox30.Controls.Add(this.lxLed_cmdTime);
            this.groupBox30.Controls.Add(this.label86);
            this.groupBox30.Controls.Add(this.lxLed_Btoltime);
            this.groupBox30.Controls.Add(this.label83);
            this.groupBox30.Controls.Add(this.label91);
            this.groupBox30.Controls.Add(this.lxLed_Btoltag);
            this.groupBox30.Controls.Add(this.label92);
            this.groupBox30.Controls.Add(this.lxLed_Bcmdsud);
            this.groupBox30.Controls.Add(this.label94);
            this.groupBox30.Controls.Add(this.lxLed_BNum);
            this.groupBox30.Controls.Add(this.label95);
            this.groupBox30.Location = new System.Drawing.Point(7, 88);
            this.groupBox30.Name = "groupBox30";
            this.groupBox30.Size = new System.Drawing.Size(1053, 484);
            this.groupBox30.TabIndex = 20;
            this.groupBox30.TabStop = false;
            // 
            // lxLed_cmdTime
            // 
            this.lxLed_cmdTime.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_cmdTime.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_cmdTime.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_cmdTime.BevelRate = 0.5F;
            this.lxLed_cmdTime.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_cmdTime.BorderWidth = 3;
            this.lxLed_cmdTime.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_cmdTime.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_cmdTime.ForeColor = System.Drawing.Color.Purple;
            this.lxLed_cmdTime.HighlightOpaque = ((byte)(50));
            this.lxLed_cmdTime.Location = new System.Drawing.Point(492, 140);
            this.lxLed_cmdTime.Name = "lxLed_cmdTime";
            this.lxLed_cmdTime.RoundCorner = true;
            this.lxLed_cmdTime.ShowHighlight = true;
            this.lxLed_cmdTime.Size = new System.Drawing.Size(150, 41);
            this.lxLed_cmdTime.TabIndex = 34;
            this.lxLed_cmdTime.Text = "0";
            this.lxLed_cmdTime.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_cmdTime.TotalCharCount = 7;
            // 
            // label86
            // 
            this.label86.AutoSize = true;
            this.label86.ForeColor = System.Drawing.Color.Teal;
            this.label86.Location = new System.Drawing.Point(490, 124);
            this.label86.Name = "label86";
            this.label86.Size = new System.Drawing.Size(71, 13);
            this.label86.TabIndex = 33;
            this.label86.Text = "cmd time(ms):";
            // 
            // lxLed_Btoltime
            // 
            this.lxLed_Btoltime.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_Btoltime.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_Btoltime.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_Btoltime.BevelRate = 0.5F;
            this.lxLed_Btoltime.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_Btoltime.BorderWidth = 3;
            this.lxLed_Btoltime.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_Btoltime.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_Btoltime.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLed_Btoltime.HighlightOpaque = ((byte)(50));
            this.lxLed_Btoltime.Location = new System.Drawing.Point(677, 148);
            this.lxLed_Btoltime.Name = "lxLed_Btoltime";
            this.lxLed_Btoltime.RoundCorner = true;
            this.lxLed_Btoltime.ShowHighlight = true;
            this.lxLed_Btoltime.Size = new System.Drawing.Size(172, 32);
            this.lxLed_Btoltime.TabIndex = 32;
            this.lxLed_Btoltime.Text = "0";
            this.lxLed_Btoltime.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_Btoltime.TotalCharCount = 10;
            // 
            // label83
            // 
            this.label83.AutoSize = true;
            this.label83.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label83.Location = new System.Drawing.Point(5, 194);
            this.label83.Name = "label83";
            this.label83.Size = new System.Drawing.Size(48, 13);
            this.label83.TabIndex = 22;
            this.label83.Text = "Tag List:";
            // 
            // label91
            // 
            this.label91.AutoSize = true;
            this.label91.ForeColor = System.Drawing.Color.Teal;
            this.label91.Location = new System.Drawing.Point(675, 127);
            this.label91.Name = "label91";
            this.label91.Size = new System.Drawing.Size(75, 13);
            this.label91.TabIndex = 31;
            this.label91.Text = "Total time(ms):";
            // 
            // lxLed_Btoltag
            // 
            this.lxLed_Btoltag.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_Btoltag.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_Btoltag.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_Btoltag.BevelRate = 0.5F;
            this.lxLed_Btoltag.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_Btoltag.BorderWidth = 3;
            this.lxLed_Btoltag.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_Btoltag.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_Btoltag.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLed_Btoltag.HighlightOpaque = ((byte)(50));
            this.lxLed_Btoltag.Location = new System.Drawing.Point(677, 74);
            this.lxLed_Btoltag.Name = "lxLed_Btoltag";
            this.lxLed_Btoltag.RoundCorner = true;
            this.lxLed_Btoltag.ShowHighlight = true;
            this.lxLed_Btoltag.Size = new System.Drawing.Size(172, 32);
            this.lxLed_Btoltag.TabIndex = 30;
            this.lxLed_Btoltag.Text = "0";
            this.lxLed_Btoltag.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_Btoltag.TotalCharCount = 10;
            // 
            // label92
            // 
            this.label92.AutoSize = true;
            this.label92.ForeColor = System.Drawing.Color.Teal;
            this.label92.Location = new System.Drawing.Point(675, 54);
            this.label92.Name = "label92";
            this.label92.Size = new System.Drawing.Size(72, 13);
            this.label92.TabIndex = 29;
            this.label92.Text = "Total number:";
            // 
            // lxLed_Bcmdsud
            // 
            this.lxLed_Bcmdsud.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_Bcmdsud.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_Bcmdsud.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_Bcmdsud.BevelRate = 0.5F;
            this.lxLed_Bcmdsud.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_Bcmdsud.BorderWidth = 3;
            this.lxLed_Bcmdsud.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_Bcmdsud.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_Bcmdsud.ForeColor = System.Drawing.Color.Purple;
            this.lxLed_Bcmdsud.HighlightOpaque = ((byte)(50));
            this.lxLed_Bcmdsud.Location = new System.Drawing.Point(492, 65);
            this.lxLed_Bcmdsud.Name = "lxLed_Bcmdsud";
            this.lxLed_Bcmdsud.RoundCorner = true;
            this.lxLed_Bcmdsud.ShowHighlight = true;
            this.lxLed_Bcmdsud.Size = new System.Drawing.Size(150, 41);
            this.lxLed_Bcmdsud.TabIndex = 26;
            this.lxLed_Bcmdsud.Text = "0";
            this.lxLed_Bcmdsud.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            this.lxLed_Bcmdsud.TotalCharCount = 7;
            // 
            // label94
            // 
            this.label94.AutoSize = true;
            this.label94.ForeColor = System.Drawing.Color.Teal;
            this.label94.Location = new System.Drawing.Point(490, 44);
            this.label94.Name = "label94";
            this.label94.Size = new System.Drawing.Size(41, 13);
            this.label94.TabIndex = 25;
            this.label94.Text = "Speed:";
            // 
            // lxLed_BNum
            // 
            this.lxLed_BNum.BackColor = System.Drawing.Color.Transparent;
            this.lxLed_BNum.BackColor_1 = System.Drawing.Color.Transparent;
            this.lxLed_BNum.BackColor_2 = System.Drawing.Color.DarkRed;
            this.lxLed_BNum.BevelRate = 0.5F;
            this.lxLed_BNum.BorderColor = System.Drawing.Color.Lavender;
            this.lxLed_BNum.BorderWidth = 3;
            this.lxLed_BNum.FadedColor = System.Drawing.SystemColors.ControlLight;
            this.lxLed_BNum.FocusedBorderColor = System.Drawing.Color.LightCoral;
            this.lxLed_BNum.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lxLed_BNum.HighlightOpaque = ((byte)(50));
            this.lxLed_BNum.Location = new System.Drawing.Point(101, 43);
            this.lxLed_BNum.Name = "lxLed_BNum";
            this.lxLed_BNum.RoundCorner = true;
            this.lxLed_BNum.ShowHighlight = true;
            this.lxLed_BNum.Size = new System.Drawing.Size(356, 138);
            this.lxLed_BNum.TabIndex = 22;
            this.lxLed_BNum.Text = "0";
            this.lxLed_BNum.TextAlignment = UHFReader288MPDemo.LxLedControl.Alignment.Right;
            // 
            // label95
            // 
            this.label95.AutoSize = true;
            this.label95.Location = new System.Drawing.Point(99, 24);
            this.label95.Name = "label95";
            this.label95.Size = new System.Drawing.Size(69, 13);
            this.label95.TabIndex = 21;
            this.label95.Text = "Tag Number:";
            // 
            // tabPage_RW
            // 
            this.tabPage_RW.Controls.Add(this.panel4);
            this.tabPage_RW.Location = new System.Drawing.Point(4, 27);
            this.tabPage_RW.Name = "tabPage_RW";
            this.tabPage_RW.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_RW.Size = new System.Drawing.Size(1063, 581);
            this.tabPage_RW.TabIndex = 2;
            this.tabPage_RW.Text = "Read/Write Tag";
            this.tabPage_RW.UseVisualStyleBackColor = true;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.groupBox21);
            this.panel4.Controls.Add(this.groupBox20);
            this.panel4.Controls.Add(this.groupBox18);
            this.panel4.Controls.Add(this.groupBox16);
            this.panel4.Controls.Add(this.groupBox13);
            this.panel4.Controls.Add(this.groupBox11);
            this.panel4.Controls.Add(this.groupBox31);
            this.panel4.Controls.Add(this.groupBox10);
            this.panel4.Location = new System.Drawing.Point(-2, -3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1067, 609);
            this.panel4.TabIndex = 0;
            // 
            // groupBox21
            // 
            this.groupBox21.Controls.Add(this.btCheckReadProtected_G2);
            this.groupBox21.Controls.Add(this.btRemoveReadProtect_G2);
            this.groupBox21.Controls.Add(this.btSetMultiReadProtect_G2);
            this.groupBox21.Controls.Add(this.btSetReadProtect_G2);
            this.groupBox21.Controls.Add(this.text_AccessCode4);
            this.groupBox21.Controls.Add(this.label34);
            this.groupBox21.Location = new System.Drawing.Point(346, 391);
            this.groupBox21.Name = "groupBox21";
            this.groupBox21.Size = new System.Drawing.Size(310, 158);
            this.groupBox21.TabIndex = 23;
            this.groupBox21.TabStop = false;
            this.groupBox21.Text = "Read Protection-NXP";
            // 
            // btCheckReadProtected_G2
            // 
            this.btCheckReadProtected_G2.Location = new System.Drawing.Point(12, 129);
            this.btCheckReadProtected_G2.Name = "btCheckReadProtected_G2";
            this.btCheckReadProtected_G2.Size = new System.Drawing.Size(285, 25);
            this.btCheckReadProtected_G2.TabIndex = 6;
            this.btCheckReadProtected_G2.Text = "Detect Privacy   ";
            this.btCheckReadProtected_G2.UseVisualStyleBackColor = true;
            this.btCheckReadProtected_G2.Click += new System.EventHandler(this.BtCheckReadProtected_G2_Click);
            // 
            // btRemoveReadProtect_G2
            // 
            this.btRemoveReadProtect_G2.Location = new System.Drawing.Point(12, 102);
            this.btRemoveReadProtect_G2.Name = "btRemoveReadProtect_G2";
            this.btRemoveReadProtect_G2.Size = new System.Drawing.Size(285, 25);
            this.btRemoveReadProtect_G2.TabIndex = 5;
            this.btRemoveReadProtect_G2.Text = "Reset Privacy";
            this.btRemoveReadProtect_G2.UseVisualStyleBackColor = true;
            this.btRemoveReadProtect_G2.Click += new System.EventHandler(this.BtRemoveReadProtect_G2_Click);
            // 
            // btSetMultiReadProtect_G2
            // 
            this.btSetMultiReadProtect_G2.Location = new System.Drawing.Point(12, 74);
            this.btSetMultiReadProtect_G2.Name = "btSetMultiReadProtect_G2";
            this.btSetMultiReadProtect_G2.Size = new System.Drawing.Size(285, 25);
            this.btSetMultiReadProtect_G2.TabIndex = 4;
            this.btSetMultiReadProtect_G2.Text = "Set Privacy Without EPC";
            this.btSetMultiReadProtect_G2.UseVisualStyleBackColor = true;
            this.btSetMultiReadProtect_G2.Click += new System.EventHandler(this.BtSetMultiReadProtect_G2_Click);
            // 
            // btSetReadProtect_G2
            // 
            this.btSetReadProtect_G2.Location = new System.Drawing.Point(12, 44);
            this.btSetReadProtect_G2.Name = "btSetReadProtect_G2";
            this.btSetReadProtect_G2.Size = new System.Drawing.Size(285, 25);
            this.btSetReadProtect_G2.TabIndex = 3;
            this.btSetReadProtect_G2.Text = "Set Privacy By EPC";
            this.btSetReadProtect_G2.UseVisualStyleBackColor = true;
            this.btSetReadProtect_G2.Click += new System.EventHandler(this.BtSetReadProtect_G2_Click);
            // 
            // text_AccessCode4
            // 
            this.text_AccessCode4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_AccessCode4.Location = new System.Drawing.Point(196, 15);
            this.text_AccessCode4.MaxLength = 8;
            this.text_AccessCode4.Name = "text_AccessCode4";
            this.text_AccessCode4.Size = new System.Drawing.Size(100, 20);
            this.text_AccessCode4.TabIndex = 2;
            this.text_AccessCode4.Text = "00000000";
            this.text_AccessCode4.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Location = new System.Drawing.Point(15, 22);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(84, 13);
            this.label34.TabIndex = 1;
            this.label34.Text = "Password:(Hex):";
            // 
            // groupBox20
            // 
            this.groupBox20.Controls.Add(this.btWriteEPC_G2);
            this.groupBox20.Controls.Add(this.text_AccessCode3);
            this.groupBox20.Controls.Add(this.label27);
            this.groupBox20.Controls.Add(this.text_WriteEPC);
            this.groupBox20.Controls.Add(this.label33);
            this.groupBox20.Location = new System.Drawing.Point(8, 463);
            this.groupBox20.Name = "groupBox20";
            this.groupBox20.Size = new System.Drawing.Size(332, 86);
            this.groupBox20.TabIndex = 22;
            this.groupBox20.TabStop = false;
            this.groupBox20.Text = "Write EPC";
            // 
            // btWriteEPC_G2
            // 
            this.btWriteEPC_G2.Location = new System.Drawing.Point(229, 48);
            this.btWriteEPC_G2.Name = "btWriteEPC_G2";
            this.btWriteEPC_G2.Size = new System.Drawing.Size(90, 25);
            this.btWriteEPC_G2.TabIndex = 4;
            this.btWriteEPC_G2.Text = "Write EPC";
            this.btWriteEPC_G2.UseVisualStyleBackColor = true;
            this.btWriteEPC_G2.Click += new System.EventHandler(this.BtWriteEPC_G2_Click);
            // 
            // text_AccessCode3
            // 
            this.text_AccessCode3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_AccessCode3.Location = new System.Drawing.Point(155, 48);
            this.text_AccessCode3.MaxLength = 8;
            this.text_AccessCode3.Name = "text_AccessCode3";
            this.text_AccessCode3.Size = new System.Drawing.Size(57, 20);
            this.text_AccessCode3.TabIndex = 3;
            this.text_AccessCode3.Text = "00000000";
            this.text_AccessCode3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Location = new System.Drawing.Point(5, 54);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(84, 13);
            this.label27.TabIndex = 2;
            this.label27.Text = "Password:(Hex):";
            // 
            // text_WriteEPC
            // 
            this.text_WriteEPC.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_WriteEPC.Location = new System.Drawing.Point(77, 18);
            this.text_WriteEPC.MaxLength = 60;
            this.text_WriteEPC.Name = "text_WriteEPC";
            this.text_WriteEPC.Size = new System.Drawing.Size(242, 20);
            this.text_WriteEPC.TabIndex = 1;
            this.text_WriteEPC.Text = "0000";
            this.text_WriteEPC.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Location = new System.Drawing.Point(6, 23);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(31, 13);
            this.label33.TabIndex = 0;
            this.label33.Text = "EPC:";
            // 
            // groupBox18
            // 
            this.groupBox18.Controls.Add(this.btDestroyCard);
            this.groupBox18.Controls.Add(this.text_DestroyCode);
            this.groupBox18.Controls.Add(this.label26);
            this.groupBox18.Location = new System.Drawing.Point(8, 391);
            this.groupBox18.Name = "groupBox18";
            this.groupBox18.Size = new System.Drawing.Size(332, 61);
            this.groupBox18.TabIndex = 20;
            this.groupBox18.TabStop = false;
            this.groupBox18.Text = "Kill Tag";
            // 
            // btDestroyCard
            // 
            this.btDestroyCard.Location = new System.Drawing.Point(229, 23);
            this.btDestroyCard.Name = "btDestroyCard";
            this.btDestroyCard.Size = new System.Drawing.Size(90, 25);
            this.btDestroyCard.TabIndex = 3;
            this.btDestroyCard.Text = "Kill tag";
            this.btDestroyCard.UseVisualStyleBackColor = true;
            this.btDestroyCard.Click += new System.EventHandler(this.BtDestroyCard_Click);
            // 
            // text_DestroyCode
            // 
            this.text_DestroyCode.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_DestroyCode.Location = new System.Drawing.Point(155, 25);
            this.text_DestroyCode.MaxLength = 8;
            this.text_DestroyCode.Name = "text_DestroyCode";
            this.text_DestroyCode.Size = new System.Drawing.Size(56, 20);
            this.text_DestroyCode.TabIndex = 2;
            this.text_DestroyCode.Text = "00000000";
            this.text_DestroyCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(6, 28);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(97, 13);
            this.label26.TabIndex = 1;
            this.label26.Text = "Kill Password:(Hex)";
            // 
            // groupBox16
            // 
            this.groupBox16.Controls.Add(this.pictureBox2);
            this.groupBox16.Controls.Add(this.btCheckEASAlarm);
            this.groupBox16.Controls.Add(this.btSetEASAlarm_G2);
            this.groupBox16.Controls.Add(this.groupBox17);
            this.groupBox16.Controls.Add(this.text_AccessCode5);
            this.groupBox16.Controls.Add(this.label28);
            this.groupBox16.Location = new System.Drawing.Point(662, 259);
            this.groupBox16.Name = "groupBox16";
            this.groupBox16.Size = new System.Drawing.Size(397, 289);
            this.groupBox16.TabIndex = 19;
            this.groupBox16.TabStop = false;
            this.groupBox16.Text = "EAS Alarm";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(63, 121);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(137, 148);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 14;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Visible = false;
            // 
            // btCheckEASAlarm
            // 
            this.btCheckEASAlarm.Location = new System.Drawing.Point(280, 84);
            this.btCheckEASAlarm.Name = "btCheckEASAlarm";
            this.btCheckEASAlarm.Size = new System.Drawing.Size(90, 25);
            this.btCheckEASAlarm.TabIndex = 13;
            this.btCheckEASAlarm.Text = "Detect";
            this.btCheckEASAlarm.UseVisualStyleBackColor = true;
            this.btCheckEASAlarm.Click += new System.EventHandler(this.BtCheckEASAlarm_Click);
            // 
            // btSetEASAlarm_G2
            // 
            this.btSetEASAlarm_G2.Location = new System.Drawing.Point(280, 27);
            this.btSetEASAlarm_G2.Name = "btSetEASAlarm_G2";
            this.btSetEASAlarm_G2.Size = new System.Drawing.Size(90, 25);
            this.btSetEASAlarm_G2.TabIndex = 12;
            this.btSetEASAlarm_G2.Text = "Configure";
            this.btSetEASAlarm_G2.UseVisualStyleBackColor = true;
            this.btSetEASAlarm_G2.Click += new System.EventHandler(this.BtSetEASAlarm_G2_Click);
            // 
            // groupBox17
            // 
            this.groupBox17.Controls.Add(this.NoAlarm_G2);
            this.groupBox17.Controls.Add(this.Alarm_G2);
            this.groupBox17.Location = new System.Drawing.Point(9, 51);
            this.groupBox17.Name = "groupBox17";
            this.groupBox17.Size = new System.Drawing.Size(249, 59);
            this.groupBox17.TabIndex = 11;
            this.groupBox17.TabStop = false;
            // 
            // NoAlarm_G2
            // 
            this.NoAlarm_G2.AutoSize = true;
            this.NoAlarm_G2.Location = new System.Drawing.Point(113, 22);
            this.NoAlarm_G2.Name = "NoAlarm_G2";
            this.NoAlarm_G2.Size = new System.Drawing.Size(68, 17);
            this.NoAlarm_G2.TabIndex = 1;
            this.NoAlarm_G2.Text = "No Alarm";
            this.NoAlarm_G2.UseVisualStyleBackColor = true;
            // 
            // Alarm_G2
            // 
            this.Alarm_G2.AutoSize = true;
            this.Alarm_G2.Checked = true;
            this.Alarm_G2.Location = new System.Drawing.Point(6, 22);
            this.Alarm_G2.Name = "Alarm_G2";
            this.Alarm_G2.Size = new System.Drawing.Size(51, 17);
            this.Alarm_G2.TabIndex = 0;
            this.Alarm_G2.TabStop = true;
            this.Alarm_G2.Text = "Alarm";
            this.Alarm_G2.UseVisualStyleBackColor = true;
            // 
            // text_AccessCode5
            // 
            this.text_AccessCode5.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_AccessCode5.Location = new System.Drawing.Point(158, 24);
            this.text_AccessCode5.MaxLength = 8;
            this.text_AccessCode5.Name = "text_AccessCode5";
            this.text_AccessCode5.Size = new System.Drawing.Size(100, 20);
            this.text_AccessCode5.TabIndex = 10;
            this.text_AccessCode5.Text = "00000000";
            this.text_AccessCode5.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Location = new System.Drawing.Point(6, 28);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(84, 13);
            this.label28.TabIndex = 9;
            this.label28.Text = "Password:(Hex):";
            // 
            // groupBox13
            // 
            this.groupBox13.Controls.Add(this.btSetProtectState);
            this.groupBox13.Controls.Add(this.Edit_AccessCode6);
            this.groupBox13.Controls.Add(this.label25);
            this.groupBox13.Controls.Add(this.groupBox14);
            this.groupBox13.Controls.Add(this.groupBox15);
            this.groupBox13.Location = new System.Drawing.Point(5, 259);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Size = new System.Drawing.Size(651, 131);
            this.groupBox13.TabIndex = 18;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "Set Protect For Reading Or Writing";
            // 
            // btSetProtectState
            // 
            this.btSetProtectState.Location = new System.Drawing.Point(551, 91);
            this.btSetProtectState.Name = "btSetProtectState";
            this.btSetProtectState.Size = new System.Drawing.Size(90, 25);
            this.btSetProtectState.TabIndex = 6;
            this.btSetProtectState.Text = "Lock";
            this.btSetProtectState.UseVisualStyleBackColor = true;
            this.btSetProtectState.Click += new System.EventHandler(this.BtSetProtectState_Click);
            // 
            // Edit_AccessCode6
            // 
            this.Edit_AccessCode6.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.Edit_AccessCode6.Location = new System.Drawing.Point(551, 48);
            this.Edit_AccessCode6.MaxLength = 8;
            this.Edit_AccessCode6.Name = "Edit_AccessCode6";
            this.Edit_AccessCode6.Size = new System.Drawing.Size(90, 20);
            this.Edit_AccessCode6.TabIndex = 5;
            this.Edit_AccessCode6.Text = "00000000";
            this.Edit_AccessCode6.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(503, 18);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(84, 13);
            this.label25.TabIndex = 4;
            this.label25.Text = "Password:(Hex):";
            // 
            // groupBox14
            // 
            this.groupBox14.Controls.Add(this.AlwaysNot2);
            this.groupBox14.Controls.Add(this.Always2);
            this.groupBox14.Controls.Add(this.Proect2);
            this.groupBox14.Controls.Add(this.NoProect2);
            this.groupBox14.Location = new System.Drawing.Point(4, 74);
            this.groupBox14.Name = "groupBox14";
            this.groupBox14.Size = new System.Drawing.Size(476, 48);
            this.groupBox14.TabIndex = 3;
            this.groupBox14.TabStop = false;
            // 
            // AlwaysNot2
            // 
            this.AlwaysNot2.AutoSize = true;
            this.AlwaysNot2.Location = new System.Drawing.Point(321, 20);
            this.AlwaysNot2.Name = "AlwaysNot2";
            this.AlwaysNot2.Size = new System.Drawing.Size(85, 17);
            this.AlwaysNot2.TabIndex = 3;
            this.AlwaysNot2.Text = "Lock forever\n";
            this.AlwaysNot2.UseVisualStyleBackColor = true;
            // 
            // Always2
            // 
            this.Always2.AutoSize = true;
            this.Always2.Location = new System.Drawing.Point(178, 20);
            this.Always2.Name = "Always2";
            this.Always2.Size = new System.Drawing.Size(95, 17);
            this.Always2.TabIndex = 2;
            this.Always2.Text = "Unlock forever\n";
            this.Always2.UseVisualStyleBackColor = true;
            // 
            // Proect2
            // 
            this.Proect2.AutoSize = true;
            this.Proect2.Location = new System.Drawing.Point(98, 20);
            this.Proect2.Name = "Proect2";
            this.Proect2.Size = new System.Drawing.Size(49, 17);
            this.Proect2.TabIndex = 1;
            this.Proect2.Text = "Lock";
            this.Proect2.UseVisualStyleBackColor = true;
            // 
            // NoProect2
            // 
            this.NoProect2.AutoSize = true;
            this.NoProect2.Checked = true;
            this.NoProect2.Location = new System.Drawing.Point(9, 20);
            this.NoProect2.Name = "NoProect2";
            this.NoProect2.Size = new System.Drawing.Size(63, 17);
            this.NoProect2.TabIndex = 0;
            this.NoProect2.TabStop = true;
            this.NoProect2.Text = "UnLock";
            this.NoProect2.UseVisualStyleBackColor = true;
            // 
            // groupBox15
            // 
            this.groupBox15.Controls.Add(this.p_pass);
            this.groupBox15.Controls.Add(this.P_User);
            this.groupBox15.Controls.Add(this.P_TID);
            this.groupBox15.Controls.Add(this.P_EPC);
            this.groupBox15.Controls.Add(this.P_kill);
            this.groupBox15.Location = new System.Drawing.Point(4, 14);
            this.groupBox15.Name = "groupBox15";
            this.groupBox15.Size = new System.Drawing.Size(476, 56);
            this.groupBox15.TabIndex = 2;
            this.groupBox15.TabStop = false;
            // 
            // p_pass
            // 
            this.p_pass.AutoSize = true;
            this.p_pass.Checked = true;
            this.p_pass.Location = new System.Drawing.Point(133, 20);
            this.p_pass.Name = "p_pass";
            this.p_pass.Size = new System.Drawing.Size(109, 17);
            this.p_pass.TabIndex = 4;
            this.p_pass.TabStop = true;
            this.p_pass.Text = "Access Password";
            this.p_pass.UseVisualStyleBackColor = true;
            // 
            // P_User
            // 
            this.P_User.AutoSize = true;
            this.P_User.Location = new System.Drawing.Point(404, 20);
            this.P_User.Name = "P_User";
            this.P_User.Size = new System.Drawing.Size(47, 17);
            this.P_User.TabIndex = 3;
            this.P_User.Text = "User";
            this.P_User.UseVisualStyleBackColor = true;
            // 
            // P_TID
            // 
            this.P_TID.AutoSize = true;
            this.P_TID.Location = new System.Drawing.Point(341, 20);
            this.P_TID.Name = "P_TID";
            this.P_TID.Size = new System.Drawing.Size(43, 17);
            this.P_TID.TabIndex = 2;
            this.P_TID.Text = "TID";
            this.P_TID.UseVisualStyleBackColor = true;
            // 
            // P_EPC
            // 
            this.P_EPC.AutoSize = true;
            this.P_EPC.Location = new System.Drawing.Point(276, 20);
            this.P_EPC.Name = "P_EPC";
            this.P_EPC.Size = new System.Drawing.Size(46, 17);
            this.P_EPC.TabIndex = 1;
            this.P_EPC.Text = "EPC";
            this.P_EPC.UseVisualStyleBackColor = true;
            // 
            // P_kill
            // 
            this.P_kill.AutoSize = true;
            this.P_kill.Location = new System.Drawing.Point(9, 20);
            this.P_kill.Name = "P_kill";
            this.P_kill.Size = new System.Drawing.Size(87, 17);
            this.P_kill.TabIndex = 0;
            this.P_kill.Text = "Kill Password";
            this.P_kill.UseVisualStyleBackColor = true;
            // 
            // groupBox11
            // 
            this.groupBox11.Controls.Add(this.btExtWrite);
            this.groupBox11.Controls.Add(this.btExtRead);
            this.groupBox11.Controls.Add(this.textBox_pc);
            this.groupBox11.Controls.Add(this.checkBox_pc);
            this.groupBox11.Controls.Add(this.btBlockWrite);
            this.groupBox11.Controls.Add(this.btBlockErase);
            this.groupBox11.Controls.Add(this.btWrite);
            this.groupBox11.Controls.Add(this.btRead);
            this.groupBox11.Controls.Add(this.text_WriteData);
            this.groupBox11.Controls.Add(this.text_AccessCode2);
            this.groupBox11.Controls.Add(this.text_RWlen);
            this.groupBox11.Controls.Add(this.text_WordPtr);
            this.groupBox11.Controls.Add(this.label21);
            this.groupBox11.Controls.Add(this.label22);
            this.groupBox11.Controls.Add(this.label23);
            this.groupBox11.Controls.Add(this.label24);
            this.groupBox11.Controls.Add(this.groupBox12);
            this.groupBox11.Location = new System.Drawing.Point(5, 129);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(1055, 129);
            this.groupBox11.TabIndex = 17;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "Read Data / Write Data / Block Erase";
            // 
            // btExtWrite
            // 
            this.btExtWrite.Location = new System.Drawing.Point(950, 100);
            this.btExtWrite.Name = "btExtWrite";
            this.btExtWrite.Size = new System.Drawing.Size(77, 25);
            this.btExtWrite.TabIndex = 33;
            this.btExtWrite.Text = "Ext Write";
            this.btExtWrite.UseVisualStyleBackColor = true;
            this.btExtWrite.Click += new System.EventHandler(this.BtExtWrite_Click);
            // 
            // btExtRead
            // 
            this.btExtRead.Location = new System.Drawing.Point(950, 60);
            this.btExtRead.Name = "btExtRead";
            this.btExtRead.Size = new System.Drawing.Size(77, 25);
            this.btExtRead.TabIndex = 32;
            this.btExtRead.Text = "Ext Read";
            this.btExtRead.UseVisualStyleBackColor = true;
            this.btExtRead.Click += new System.EventHandler(this.BtExtRead_Click);
            // 
            // textBox_pc
            // 
            this.textBox_pc.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.textBox_pc.Location = new System.Drawing.Point(616, 62);
            this.textBox_pc.Name = "textBox_pc";
            this.textBox_pc.ReadOnly = true;
            this.textBox_pc.Size = new System.Drawing.Size(138, 20);
            this.textBox_pc.TabIndex = 29;
            this.textBox_pc.Text = "0800";
            // 
            // checkBox_pc
            // 
            this.checkBox_pc.AutoSize = true;
            this.checkBox_pc.Location = new System.Drawing.Point(405, 64);
            this.checkBox_pc.Name = "checkBox_pc";
            this.checkBox_pc.Size = new System.Drawing.Size(152, 17);
            this.checkBox_pc.TabIndex = 28;
            this.checkBox_pc.Text = "Auto Compute and add PC";
            this.checkBox_pc.UseVisualStyleBackColor = true;
            this.checkBox_pc.CheckedChanged += new System.EventHandler(this.CheckBox_pc_CheckedChanged);
            // 
            // btBlockWrite
            // 
            this.btBlockWrite.Location = new System.Drawing.Point(770, 100);
            this.btBlockWrite.Name = "btBlockWrite";
            this.btBlockWrite.Size = new System.Drawing.Size(77, 25);
            this.btBlockWrite.TabIndex = 17;
            this.btBlockWrite.Text = "BlockWrite";
            this.btBlockWrite.UseVisualStyleBackColor = true;
            this.btBlockWrite.Click += new System.EventHandler(this.BtBlockWrite_Click);
            // 
            // btBlockErase
            // 
            this.btBlockErase.Location = new System.Drawing.Point(861, 100);
            this.btBlockErase.Name = "btBlockErase";
            this.btBlockErase.Size = new System.Drawing.Size(77, 25);
            this.btBlockErase.TabIndex = 13;
            this.btBlockErase.Text = "BlockErace";
            this.btBlockErase.UseVisualStyleBackColor = true;
            this.btBlockErase.Click += new System.EventHandler(this.BtBlockErase_Click);
            // 
            // btWrite
            // 
            this.btWrite.Location = new System.Drawing.Point(861, 60);
            this.btWrite.Name = "btWrite";
            this.btWrite.Size = new System.Drawing.Size(77, 25);
            this.btWrite.TabIndex = 12;
            this.btWrite.Text = "Write";
            this.btWrite.UseVisualStyleBackColor = true;
            this.btWrite.Click += new System.EventHandler(this.BtWrite_Click);
            // 
            // btRead
            // 
            this.btRead.Location = new System.Drawing.Point(770, 60);
            this.btRead.Name = "btRead";
            this.btRead.Size = new System.Drawing.Size(77, 25);
            this.btRead.TabIndex = 11;
            this.btRead.Text = "Read";
            this.btRead.UseVisualStyleBackColor = true;
            this.btRead.Click += new System.EventHandler(this.BtRead_Click);
            // 
            // text_WriteData
            // 
            this.text_WriteData.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_WriteData.Location = new System.Drawing.Point(529, 22);
            this.text_WriteData.Name = "text_WriteData";
            this.text_WriteData.Size = new System.Drawing.Size(504, 20);
            this.text_WriteData.TabIndex = 10;
            this.text_WriteData.Text = "0000";
            this.text_WriteData.TextChanged += new System.EventHandler(this.Text_WriteData_TextChanged);
            this.text_WriteData.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // text_AccessCode2
            // 
            this.text_AccessCode2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_AccessCode2.Location = new System.Drawing.Point(205, 94);
            this.text_AccessCode2.MaxLength = 8;
            this.text_AccessCode2.Name = "text_AccessCode2";
            this.text_AccessCode2.Size = new System.Drawing.Size(118, 20);
            this.text_AccessCode2.TabIndex = 9;
            this.text_AccessCode2.Text = "00000000";
            this.text_AccessCode2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // text_RWlen
            // 
            this.text_RWlen.Location = new System.Drawing.Point(205, 62);
            this.text_RWlen.MaxLength = 2;
            this.text_RWlen.Name = "text_RWlen";
            this.text_RWlen.Size = new System.Drawing.Size(117, 20);
            this.text_RWlen.TabIndex = 8;
            this.text_RWlen.Text = "4";
            this.text_RWlen.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TB_Port_KeyPress);
            // 
            // text_WordPtr
            // 
            this.text_WordPtr.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_WordPtr.Location = new System.Drawing.Point(205, 21);
            this.text_WordPtr.MaxLength = 4;
            this.text_WordPtr.Name = "text_WordPtr";
            this.text_WordPtr.Size = new System.Drawing.Size(117, 20);
            this.text_WordPtr.TabIndex = 7;
            this.text_WordPtr.Text = "0000";
            this.text_WordPtr.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(402, 25);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(115, 13);
            this.label21.TabIndex = 5;
            this.label21.Text = "Read/Write data(Hex):";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(7, 99);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(84, 13);
            this.label22.TabIndex = 4;
            this.label22.Text = "Password:(Hex):";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(6, 66);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(69, 13);
            this.label23.TabIndex = 3;
            this.label23.Text = "Length(Dec):";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(6, 25);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(100, 13);
            this.label24.TabIndex = 2;
            this.label24.Text = "Start address:(Hex):";
            // 
            // groupBox12
            // 
            this.groupBox12.Controls.Add(this.C_User);
            this.groupBox12.Controls.Add(this.C_TID);
            this.groupBox12.Controls.Add(this.C_EPC);
            this.groupBox12.Controls.Add(this.C_Reserve);
            this.groupBox12.Location = new System.Drawing.Point(402, 88);
            this.groupBox12.Name = "groupBox12";
            this.groupBox12.Size = new System.Drawing.Size(353, 34);
            this.groupBox12.TabIndex = 1;
            this.groupBox12.TabStop = false;
            // 
            // C_User
            // 
            this.C_User.AutoSize = true;
            this.C_User.Location = new System.Drawing.Point(207, 12);
            this.C_User.Name = "C_User";
            this.C_User.Size = new System.Drawing.Size(47, 17);
            this.C_User.TabIndex = 3;
            this.C_User.Text = "User";
            this.C_User.UseVisualStyleBackColor = true;
            this.C_User.CheckedChanged += new System.EventHandler(this.C_EPC_CheckedChanged);
            // 
            // C_TID
            // 
            this.C_TID.AutoSize = true;
            this.C_TID.Location = new System.Drawing.Point(149, 12);
            this.C_TID.Name = "C_TID";
            this.C_TID.Size = new System.Drawing.Size(43, 17);
            this.C_TID.TabIndex = 2;
            this.C_TID.Text = "TID";
            this.C_TID.UseVisualStyleBackColor = true;
            this.C_TID.CheckedChanged += new System.EventHandler(this.C_EPC_CheckedChanged);
            // 
            // C_EPC
            // 
            this.C_EPC.AutoSize = true;
            this.C_EPC.Checked = true;
            this.C_EPC.Location = new System.Drawing.Point(90, 12);
            this.C_EPC.Name = "C_EPC";
            this.C_EPC.Size = new System.Drawing.Size(46, 17);
            this.C_EPC.TabIndex = 1;
            this.C_EPC.TabStop = true;
            this.C_EPC.Text = "EPC";
            this.C_EPC.UseVisualStyleBackColor = true;
            this.C_EPC.CheckedChanged += new System.EventHandler(this.C_EPC_CheckedChanged);
            // 
            // C_Reserve
            // 
            this.C_Reserve.AutoSize = true;
            this.C_Reserve.Location = new System.Drawing.Point(4, 12);
            this.C_Reserve.Name = "C_Reserve";
            this.C_Reserve.Size = new System.Drawing.Size(71, 17);
            this.C_Reserve.TabIndex = 0;
            this.C_Reserve.Text = "Password";
            this.C_Reserve.UseVisualStyleBackColor = true;
            this.C_Reserve.CheckedChanged += new System.EventHandler(this.C_EPC_CheckedChanged);
            // 
            // groupBox31
            // 
            this.groupBox31.Controls.Add(this.maskData_textBox);
            this.groupBox31.Controls.Add(this.label60);
            this.groupBox31.Controls.Add(this.groupBox40);
            this.groupBox31.Controls.Add(this.checkBox1);
            this.groupBox31.Controls.Add(this.maskLen_textBox);
            this.groupBox31.Controls.Add(this.label44);
            this.groupBox31.Controls.Add(this.maskadr_textbox);
            this.groupBox31.Controls.Add(this.label43);
            this.groupBox31.Location = new System.Drawing.Point(5, 51);
            this.groupBox31.Name = "groupBox31";
            this.groupBox31.Size = new System.Drawing.Size(1056, 75);
            this.groupBox31.TabIndex = 16;
            this.groupBox31.TabStop = false;
            this.groupBox31.Text = "Mask";
            // 
            // maskData_textBox
            // 
            this.maskData_textBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.maskData_textBox.Enabled = false;
            this.maskData_textBox.Location = new System.Drawing.Point(528, 48);
            this.maskData_textBox.Name = "maskData_textBox";
            this.maskData_textBox.Size = new System.Drawing.Size(505, 20);
            this.maskData_textBox.TabIndex = 8;
            this.maskData_textBox.Text = "00";
            this.maskData_textBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label60
            // 
            this.label60.AutoSize = true;
            this.label60.Location = new System.Drawing.Point(403, 52);
            this.label60.Name = "label60";
            this.label60.Size = new System.Drawing.Size(61, 13);
            this.label60.TabIndex = 7;
            this.label60.Text = "Data(Hex)：";
            // 
            // groupBox40
            // 
            this.groupBox40.Controls.Add(this.R_User);
            this.groupBox40.Controls.Add(this.R_TID);
            this.groupBox40.Controls.Add(this.R_EPC);
            this.groupBox40.Location = new System.Drawing.Point(402, 11);
            this.groupBox40.Name = "groupBox40";
            this.groupBox40.Size = new System.Drawing.Size(513, 32);
            this.groupBox40.TabIndex = 6;
            this.groupBox40.TabStop = false;
            // 
            // R_User
            // 
            this.R_User.AutoSize = true;
            this.R_User.Enabled = false;
            this.R_User.Location = new System.Drawing.Point(381, 12);
            this.R_User.Name = "R_User";
            this.R_User.Size = new System.Drawing.Size(47, 17);
            this.R_User.TabIndex = 2;
            this.R_User.Text = "User";
            this.R_User.UseVisualStyleBackColor = true;
            // 
            // R_TID
            // 
            this.R_TID.AutoSize = true;
            this.R_TID.Enabled = false;
            this.R_TID.Location = new System.Drawing.Point(218, 11);
            this.R_TID.Name = "R_TID";
            this.R_TID.Size = new System.Drawing.Size(43, 17);
            this.R_TID.TabIndex = 1;
            this.R_TID.Text = "TID";
            this.R_TID.UseVisualStyleBackColor = true;
            // 
            // R_EPC
            // 
            this.R_EPC.AutoSize = true;
            this.R_EPC.Checked = true;
            this.R_EPC.Enabled = false;
            this.R_EPC.Location = new System.Drawing.Point(59, 11);
            this.R_EPC.Name = "R_EPC";
            this.R_EPC.Size = new System.Drawing.Size(46, 17);
            this.R_EPC.TabIndex = 0;
            this.R_EPC.TabStop = true;
            this.R_EPC.Text = "EPC";
            this.R_EPC.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(948, 22);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(59, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Enable";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // maskLen_textBox
            // 
            this.maskLen_textBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.maskLen_textBox.Enabled = false;
            this.maskLen_textBox.Location = new System.Drawing.Point(148, 48);
            this.maskLen_textBox.MaxLength = 2;
            this.maskLen_textBox.Name = "maskLen_textBox";
            this.maskLen_textBox.Size = new System.Drawing.Size(174, 20);
            this.maskLen_textBox.TabIndex = 4;
            this.maskLen_textBox.Text = "00";
            this.maskLen_textBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label44
            // 
            this.label44.AutoSize = true;
            this.label44.Location = new System.Drawing.Point(12, 52);
            this.label44.Name = "label44";
            this.label44.Size = new System.Drawing.Size(71, 13);
            this.label44.TabIndex = 3;
            this.label44.Text = "Length(Hex)：";
            // 
            // maskadr_textbox
            // 
            this.maskadr_textbox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.maskadr_textbox.Enabled = false;
            this.maskadr_textbox.Location = new System.Drawing.Point(148, 20);
            this.maskadr_textbox.MaxLength = 4;
            this.maskadr_textbox.Name = "maskadr_textbox";
            this.maskadr_textbox.Size = new System.Drawing.Size(174, 20);
            this.maskadr_textbox.TabIndex = 2;
            this.maskadr_textbox.Text = "0000";
            this.maskadr_textbox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label43
            // 
            this.label43.AutoSize = true;
            this.label43.Location = new System.Drawing.Point(12, 26);
            this.label43.Name = "label43";
            this.label43.Size = new System.Drawing.Size(100, 13);
            this.label43.TabIndex = 1;
            this.label43.Text = "Start address(Hex)：";
            // 
            // groupBox10
            // 
            this.groupBox10.Controls.Add(this.btSelectTag);
            this.groupBox10.Controls.Add(this.comboBox_EPC);
            this.groupBox10.Controls.Add(this.label16);
            this.groupBox10.Controls.Add(this.text_epc);
            this.groupBox10.Controls.Add(this.check_selecttag);
            this.groupBox10.Location = new System.Drawing.Point(5, 4);
            this.groupBox10.Name = "groupBox10";
            this.groupBox10.Size = new System.Drawing.Size(1057, 44);
            this.groupBox10.TabIndex = 0;
            this.groupBox10.TabStop = false;
            // 
            // btSelectTag
            // 
            this.btSelectTag.ForeColor = System.Drawing.Color.RoyalBlue;
            this.btSelectTag.Location = new System.Drawing.Point(943, 14);
            this.btSelectTag.Name = "btSelectTag";
            this.btSelectTag.Size = new System.Drawing.Size(90, 25);
            this.btSelectTag.TabIndex = 4;
            this.btSelectTag.Text = "Select";
            this.btSelectTag.UseVisualStyleBackColor = true;
            this.btSelectTag.Click += new System.EventHandler(this.BtSelectTag_Click);
            // 
            // comboBox_EPC
            // 
            this.comboBox_EPC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_EPC.FormattingEnabled = true;
            this.comboBox_EPC.Location = new System.Drawing.Point(570, 15);
            this.comboBox_EPC.Name = "comboBox_EPC";
            this.comboBox_EPC.Size = new System.Drawing.Size(345, 21);
            this.comboBox_EPC.TabIndex = 3;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label16.Location = new System.Drawing.Point(503, 20);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(44, 13);
            this.label16.TabIndex = 2;
            this.label16.Text = "Tag list:";
            // 
            // text_epc
            // 
            this.text_epc.Location = new System.Drawing.Point(113, 15);
            this.text_epc.Name = "text_epc";
            this.text_epc.Size = new System.Drawing.Size(345, 20);
            this.text_epc.TabIndex = 1;
            // 
            // check_selecttag
            // 
            this.check_selecttag.AutoSize = true;
            this.check_selecttag.Checked = true;
            this.check_selecttag.CheckState = System.Windows.Forms.CheckState.Checked;
            this.check_selecttag.ForeColor = System.Drawing.Color.RoyalBlue;
            this.check_selecttag.Location = new System.Drawing.Point(17, 17);
            this.check_selecttag.Name = "check_selecttag";
            this.check_selecttag.Size = new System.Drawing.Size(89, 17);
            this.check_selecttag.TabIndex = 0;
            this.check_selecttag.Text = "Selected tag:";
            this.check_selecttag.UseVisualStyleBackColor = true;
            // 
            // Main_Page3
            // 
            this.Main_Page3.Controls.Add(this.panel5);
            this.Main_Page3.Location = new System.Drawing.Point(4, 27);
            this.Main_Page3.Name = "Main_Page3";
            this.Main_Page3.Padding = new System.Windows.Forms.Padding(3);
            this.Main_Page3.Size = new System.Drawing.Size(1071, 621);
            this.Main_Page3.TabIndex = 2;
            this.Main_Page3.Text = "18000-6B Test";
            this.Main_Page3.UseVisualStyleBackColor = true;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.groupBox23);
            this.panel5.Controls.Add(this.groupBox22);
            this.panel5.Controls.Add(this.text_6BUID);
            this.panel5.Controls.Add(this.label29);
            this.panel5.Controls.Add(this.ListView_ID_6B);
            this.panel5.Controls.Add(this.groupBox19);
            this.panel5.Location = new System.Drawing.Point(-1, -1);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1069, 616);
            this.panel5.TabIndex = 0;
            // 
            // groupBox23
            // 
            this.groupBox23.Controls.Add(this.text_Statu6B);
            this.groupBox23.Controls.Add(this.btCheckLock6B);
            this.groupBox23.Controls.Add(this.btLock6B);
            this.groupBox23.Controls.Add(this.text_checkaddr);
            this.groupBox23.Controls.Add(this.label52);
            this.groupBox23.Controls.Add(this.text_lock6b);
            this.groupBox23.Controls.Add(this.label51);
            this.groupBox23.Location = new System.Drawing.Point(561, 465);
            this.groupBox23.Name = "groupBox23";
            this.groupBox23.Size = new System.Drawing.Size(495, 119);
            this.groupBox23.TabIndex = 21;
            this.groupBox23.TabStop = false;
            // 
            // text_Statu6B
            // 
            this.text_Statu6B.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_Statu6B.ForeColor = System.Drawing.Color.Red;
            this.text_Statu6B.Location = new System.Drawing.Point(252, 81);
            this.text_Statu6B.MaxLength = 2;
            this.text_Statu6B.Name = "text_Statu6B";
            this.text_Statu6B.ReadOnly = true;
            this.text_Statu6B.Size = new System.Drawing.Size(111, 20);
            this.text_Statu6B.TabIndex = 19;
            // 
            // btCheckLock6B
            // 
            this.btCheckLock6B.Location = new System.Drawing.Point(383, 72);
            this.btCheckLock6B.Name = "btCheckLock6B";
            this.btCheckLock6B.Size = new System.Drawing.Size(100, 32);
            this.btCheckLock6B.TabIndex = 18;
            this.btCheckLock6B.Text = "Check lock";
            this.btCheckLock6B.UseVisualStyleBackColor = true;
            this.btCheckLock6B.Click += new System.EventHandler(this.BtCheckLock6B_Click);
            // 
            // btLock6B
            // 
            this.btLock6B.Location = new System.Drawing.Point(383, 22);
            this.btLock6B.Name = "btLock6B";
            this.btLock6B.Size = new System.Drawing.Size(100, 32);
            this.btLock6B.TabIndex = 17;
            this.btLock6B.Text = "Lock";
            this.btLock6B.UseVisualStyleBackColor = true;
            this.btLock6B.Click += new System.EventHandler(this.BtLock6B_Click);
            // 
            // text_checkaddr
            // 
            this.text_checkaddr.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_checkaddr.Location = new System.Drawing.Point(202, 81);
            this.text_checkaddr.MaxLength = 2;
            this.text_checkaddr.Name = "text_checkaddr";
            this.text_checkaddr.Size = new System.Drawing.Size(44, 20);
            this.text_checkaddr.TabIndex = 16;
            this.text_checkaddr.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label52
            // 
            this.label52.AutoSize = true;
            this.label52.Location = new System.Drawing.Point(47, 84);
            this.label52.Name = "label52";
            this.label52.Size = new System.Drawing.Size(129, 13);
            this.label52.TabIndex = 15;
            this.label52.Text = "Check lock address(Hex):";
            // 
            // text_lock6b
            // 
            this.text_lock6b.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_lock6b.Location = new System.Drawing.Point(202, 31);
            this.text_lock6b.MaxLength = 2;
            this.text_lock6b.Name = "text_lock6b";
            this.text_lock6b.Size = new System.Drawing.Size(44, 20);
            this.text_lock6b.TabIndex = 14;
            this.text_lock6b.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label51
            // 
            this.label51.AutoSize = true;
            this.label51.Location = new System.Drawing.Point(71, 35);
            this.label51.Name = "label51";
            this.label51.Size = new System.Drawing.Size(99, 13);
            this.label51.TabIndex = 0;
            this.label51.Text = "Lock address(Hex):";
            // 
            // groupBox22
            // 
            this.groupBox22.Controls.Add(this.text_W6B);
            this.groupBox22.Controls.Add(this.label48);
            this.groupBox22.Controls.Add(this.btWrite6B);
            this.groupBox22.Controls.Add(this.text_W6BLen);
            this.groupBox22.Controls.Add(this.label49);
            this.groupBox22.Controls.Add(this.text_W6BAddr);
            this.groupBox22.Controls.Add(this.label50);
            this.groupBox22.Controls.Add(this.text_R6B);
            this.groupBox22.Controls.Add(this.label36);
            this.groupBox22.Controls.Add(this.btRead6B);
            this.groupBox22.Controls.Add(this.text_R6BLen);
            this.groupBox22.Controls.Add(this.label35);
            this.groupBox22.Controls.Add(this.text_R6BAddr);
            this.groupBox22.Controls.Add(this.label30);
            this.groupBox22.Location = new System.Drawing.Point(563, 76);
            this.groupBox22.Name = "groupBox22";
            this.groupBox22.Size = new System.Drawing.Size(495, 389);
            this.groupBox22.TabIndex = 20;
            this.groupBox22.TabStop = false;
            this.groupBox22.Text = "Read/Write Data";
            // 
            // text_W6B
            // 
            this.text_W6B.Location = new System.Drawing.Point(122, 254);
            this.text_W6B.Multiline = true;
            this.text_W6B.Name = "text_W6B";
            this.text_W6B.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.text_W6B.Size = new System.Drawing.Size(359, 128);
            this.text_W6B.TabIndex = 18;
            this.text_W6B.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label48
            // 
            this.label48.AutoSize = true;
            this.label48.Location = new System.Drawing.Point(18, 254);
            this.label48.Name = "label48";
            this.label48.Size = new System.Drawing.Size(93, 13);
            this.label48.TabIndex = 17;
            this.label48.Text = "Write data(Hex):   ";
            // 
            // btWrite6B
            // 
            this.btWrite6B.Location = new System.Drawing.Point(381, 210);
            this.btWrite6B.Name = "btWrite6B";
            this.btWrite6B.Size = new System.Drawing.Size(100, 32);
            this.btWrite6B.TabIndex = 16;
            this.btWrite6B.Text = "Write";
            this.btWrite6B.UseVisualStyleBackColor = true;
            this.btWrite6B.Click += new System.EventHandler(this.BtWrite6B_Click);
            // 
            // text_W6BLen
            // 
            this.text_W6BLen.Location = new System.Drawing.Point(294, 216);
            this.text_W6BLen.MaxLength = 2;
            this.text_W6BLen.Name = "text_W6BLen";
            this.text_W6BLen.Size = new System.Drawing.Size(44, 20);
            this.text_W6BLen.TabIndex = 15;
            this.text_W6BLen.TextChanged += new System.EventHandler(this.Text_W6BLen_TextChanged);
            this.text_W6BLen.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Location = new System.Drawing.Point(187, 220);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(92, 13);
            this.label49.TabIndex = 14;
            this.label49.Text = "Write length(Hex):";
            // 
            // text_W6BAddr
            // 
            this.text_W6BAddr.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_W6BAddr.Location = new System.Drawing.Point(133, 217);
            this.text_W6BAddr.MaxLength = 2;
            this.text_W6BAddr.Name = "text_W6BAddr";
            this.text_W6BAddr.Size = new System.Drawing.Size(44, 20);
            this.text_W6BAddr.TabIndex = 13;
            this.text_W6BAddr.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Location = new System.Drawing.Point(18, 220);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(106, 13);
            this.label50.TabIndex = 12;
            this.label50.Text = "Start address(Hex):   ";
            // 
            // text_R6B
            // 
            this.text_R6B.Location = new System.Drawing.Point(122, 67);
            this.text_R6B.Multiline = true;
            this.text_R6B.Name = "text_R6B";
            this.text_R6B.ReadOnly = true;
            this.text_R6B.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.text_R6B.Size = new System.Drawing.Size(359, 128);
            this.text_R6B.TabIndex = 11;
            // 
            // label36
            // 
            this.label36.AutoSize = true;
            this.label36.Location = new System.Drawing.Point(18, 67);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(94, 13);
            this.label36.TabIndex = 10;
            this.label36.Text = "Read data(Hex):   ";
            // 
            // btRead6B
            // 
            this.btRead6B.Location = new System.Drawing.Point(381, 22);
            this.btRead6B.Name = "btRead6B";
            this.btRead6B.Size = new System.Drawing.Size(100, 32);
            this.btRead6B.TabIndex = 9;
            this.btRead6B.Text = "read";
            this.btRead6B.UseVisualStyleBackColor = true;
            this.btRead6B.Click += new System.EventHandler(this.BtRead6B_Click);
            // 
            // text_R6BLen
            // 
            this.text_R6BLen.Location = new System.Drawing.Point(294, 27);
            this.text_R6BLen.MaxLength = 2;
            this.text_R6BLen.Name = "text_R6BLen";
            this.text_R6BLen.Size = new System.Drawing.Size(44, 20);
            this.text_R6BLen.TabIndex = 8;
            this.text_R6BLen.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label35
            // 
            this.label35.AutoSize = true;
            this.label35.Location = new System.Drawing.Point(193, 31);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(93, 13);
            this.label35.TabIndex = 7;
            this.label35.Text = "Read length(Hex):";
            // 
            // text_R6BAddr
            // 
            this.text_R6BAddr.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.text_R6BAddr.Location = new System.Drawing.Point(133, 27);
            this.text_R6BAddr.MaxLength = 2;
            this.text_R6BAddr.Name = "text_R6BAddr";
            this.text_R6BAddr.Size = new System.Drawing.Size(44, 20);
            this.text_R6BAddr.TabIndex = 6;
            this.text_R6BAddr.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Text_Address_KeyPress);
            // 
            // label30
            // 
            this.label30.AutoSize = true;
            this.label30.Location = new System.Drawing.Point(18, 31);
            this.label30.Name = "label30";
            this.label30.Size = new System.Drawing.Size(106, 13);
            this.label30.TabIndex = 5;
            this.label30.Text = "Start address(Hex):   ";
            // 
            // text_6BUID
            // 
            this.text_6BUID.Location = new System.Drawing.Point(696, 42);
            this.text_6BUID.Name = "text_6BUID";
            this.text_6BUID.ReadOnly = true;
            this.text_6BUID.Size = new System.Drawing.Size(214, 20);
            this.text_6BUID.TabIndex = 19;
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(559, 48);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(111, 13);
            this.label29.TabIndex = 18;
            this.label29.Text = "Current Selected UID:";
            // 
            // ListView_ID_6B
            // 
            this.ListView_ID_6B.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.ListView_ID_6B.AllowDrop = true;
            this.ListView_ID_6B.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader2,
            this.columnHeader3});
            this.ListView_ID_6B.FullRowSelect = true;
            this.ListView_ID_6B.GridLines = true;
            this.ListView_ID_6B.HideSelection = false;
            this.ListView_ID_6B.HotTracking = true;
            this.ListView_ID_6B.HoverSelection = true;
            this.ListView_ID_6B.Location = new System.Drawing.Point(10, 89);
            this.ListView_ID_6B.Name = "ListView_ID_6B";
            this.ListView_ID_6B.Size = new System.Drawing.Size(524, 522);
            this.ListView_ID_6B.TabIndex = 17;
            this.ListView_ID_6B.UseCompatibleStateImageBehavior = false;
            this.ListView_ID_6B.View = System.Windows.Forms.View.Details;
            this.ListView_ID_6B.DoubleClick += new System.EventHandler(this.ListView_ID_6B_DoubleClick);
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "No.";
            this.columnHeader5.Width = 50;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "ID";
            this.columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader6.Width = 230;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Ant(4,3,2,1)";
            this.columnHeader7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader7.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Times";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "RSSI";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // groupBox19
            // 
            this.groupBox19.Controls.Add(this.rb_mutiple);
            this.groupBox19.Controls.Add(this.rb_single);
            this.groupBox19.Controls.Add(this.btInventory6B);
            this.groupBox19.Location = new System.Drawing.Point(10, 5);
            this.groupBox19.Name = "groupBox19";
            this.groupBox19.Size = new System.Drawing.Size(524, 77);
            this.groupBox19.TabIndex = 16;
            this.groupBox19.TabStop = false;
            // 
            // rb_mutiple
            // 
            this.rb_mutiple.AutoSize = true;
            this.rb_mutiple.Location = new System.Drawing.Point(377, 38);
            this.rb_mutiple.Name = "rb_mutiple";
            this.rb_mutiple.Size = new System.Drawing.Size(59, 17);
            this.rb_mutiple.TabIndex = 2;
            this.rb_mutiple.Text = "Mutiple";
            this.rb_mutiple.UseVisualStyleBackColor = true;
            // 
            // rb_single
            // 
            this.rb_single.AutoSize = true;
            this.rb_single.Checked = true;
            this.rb_single.Location = new System.Drawing.Point(246, 38);
            this.rb_single.Name = "rb_single";
            this.rb_single.Size = new System.Drawing.Size(54, 17);
            this.rb_single.TabIndex = 1;
            this.rb_single.TabStop = true;
            this.rb_single.Text = "Single";
            this.rb_single.UseVisualStyleBackColor = true;
            // 
            // btInventory6B
            // 
            this.btInventory6B.BackColor = System.Drawing.Color.Transparent;
            this.btInventory6B.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btInventory6B.ForeColor = System.Drawing.Color.DarkBlue;
            this.btInventory6B.Location = new System.Drawing.Point(18, 22);
            this.btInventory6B.Name = "btInventory6B";
            this.btInventory6B.Size = new System.Drawing.Size(171, 43);
            this.btInventory6B.TabIndex = 0;
            this.btInventory6B.Text = "Start";
            this.btInventory6B.UseVisualStyleBackColor = false;
            this.btInventory6B.Click += new System.EventHandler(this.BtInventory6B_Click);
            // 
            // Main_Page4
            // 
            this.Main_Page4.Controls.Add(this.tabControl3);
            this.Main_Page4.Location = new System.Drawing.Point(4, 27);
            this.Main_Page4.Name = "Main_Page4";
            this.Main_Page4.Padding = new System.Windows.Forms.Padding(3);
            this.Main_Page4.Size = new System.Drawing.Size(1071, 621);
            this.Main_Page4.TabIndex = 3;
            this.Main_Page4.Text = "Network module config";
            this.Main_Page4.UseVisualStyleBackColor = true;
            // 
            // tabControl3
            // 
            this.tabControl3.Controls.Add(this.tabPage6);
            this.tabControl3.Controls.Add(this.tabPage8);
            this.tabControl3.Controls.Add(this.tabPage9);
            this.tabControl3.ItemSize = new System.Drawing.Size(60, 23);
            this.tabControl3.Location = new System.Drawing.Point(-1, 4);
            this.tabControl3.Name = "tabControl3";
            this.tabControl3.SelectedIndex = 0;
            this.tabControl3.Size = new System.Drawing.Size(1072, 608);
            this.tabControl3.TabIndex = 0;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.DeviceListView);
            this.tabPage6.Controls.Add(this.menuStrip);
            this.tabPage6.Location = new System.Drawing.Point(4, 27);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage6.Size = new System.Drawing.Size(1064, 577);
            this.tabPage6.TabIndex = 0;
            this.tabPage6.Text = "   TCP config       ";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // DeviceListView
            // 
            this.DeviceListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.deviceName,
            this.deviceIP,
            this.deviceMac});
            this.DeviceListView.FullRowSelect = true;
            this.DeviceListView.HideSelection = false;
            this.DeviceListView.Location = new System.Drawing.Point(8, 32);
            this.DeviceListView.Name = "DeviceListView";
            this.DeviceListView.Size = new System.Drawing.Size(1050, 538);
            this.DeviceListView.TabIndex = 5;
            this.DeviceListView.UseCompatibleStateImageBehavior = false;
            this.DeviceListView.View = System.Windows.Forms.View.Details;
            this.DeviceListView.DoubleClick += new System.EventHandler(this.DeviceListView_DoubleClick);
            // 
            // deviceName
            // 
            this.deviceName.Text = "Name";
            this.deviceName.Width = 220;
            // 
            // deviceIP
            // 
            this.deviceIP.Text = "IP Address";
            this.deviceIP.Width = 280;
            // 
            // deviceMac
            // 
            this.deviceMac.Text = "Mac";
            this.deviceMac.Width = 245;
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.languageToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(3, 3);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1058, 24);
            this.menuStrip.TabIndex = 4;
            this.menuStrip.Text = "menuStrip";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.searchToolStripMenuItem,
            this.configToolStripMenuItem,
            this.clearToolStripMenuItem,
            this.toolStripSeparator4,
            this.exitToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(72, 20);
            this.toolStripMenuItem1.Text = "&Operation";
            // 
            // searchToolStripMenuItem
            // 
            this.searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            this.searchToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.searchToolStripMenuItem.Text = "&Search";
            this.searchToolStripMenuItem.Click += new System.EventHandler(this.SearchToolStripMenuItem_Click);
            // 
            // configToolStripMenuItem
            // 
            this.configToolStripMenuItem.Name = "configToolStripMenuItem";
            this.configToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.configToolStripMenuItem.Text = "&Config";
            this.configToolStripMenuItem.Click += new System.EventHandler(this.ConfigToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.clearToolStripMenuItem.Text = "&Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.ClearToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(107, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.iEToolStripMenuItem,
            this.telnetToolStripMenuItem,
            this.pingToolStripMenuItem});
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(41, 20);
            this.toolStripMenuItem2.Text = "&Tool";
            // 
            // iEToolStripMenuItem
            // 
            this.iEToolStripMenuItem.Name = "iEToolStripMenuItem";
            this.iEToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.iEToolStripMenuItem.Text = "&IE";
            this.iEToolStripMenuItem.Click += new System.EventHandler(this.IEToolStripMenuItem_Click);
            // 
            // telnetToolStripMenuItem
            // 
            this.telnetToolStripMenuItem.Name = "telnetToolStripMenuItem";
            this.telnetToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.telnetToolStripMenuItem.Text = "&Telnet";
            this.telnetToolStripMenuItem.Click += new System.EventHandler(this.TelnetToolStripMenuItem_Click);
            // 
            // pingToolStripMenuItem
            // 
            this.pingToolStripMenuItem.Name = "pingToolStripMenuItem";
            this.pingToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.pingToolStripMenuItem.Text = "&Ping";
            this.pingToolStripMenuItem.Click += new System.EventHandler(this.PingToolStripMenuItem_Click);
            // 
            // languageToolStripMenuItem
            // 
            this.languageToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.englishToolStripMenuItem});
            this.languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            this.languageToolStripMenuItem.Size = new System.Drawing.Size(71, 20);
            this.languageToolStripMenuItem.Text = "&Language";
            // 
            // englishToolStripMenuItem
            // 
            this.englishToolStripMenuItem.Name = "englishToolStripMenuItem";
            this.englishToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.englishToolStripMenuItem.Text = "&English";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            // 
            // tabPage8
            // 
            this.tabPage8.Controls.Add(this.label63);
            this.tabPage8.Controls.Add(this.stcprecv);
            this.tabPage8.Controls.Add(this.groupBox25);
            this.tabPage8.Controls.Add(this.groupBox24);
            this.tabPage8.Location = new System.Drawing.Point(4, 27);
            this.tabPage8.Name = "tabPage8";
            this.tabPage8.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage8.Size = new System.Drawing.Size(1064, 577);
            this.tabPage8.TabIndex = 2;
            this.tabPage8.Text = "   TCP Server   ";
            this.tabPage8.UseVisualStyleBackColor = true;
            // 
            // label63
            // 
            this.label63.AutoSize = true;
            this.label63.Location = new System.Drawing.Point(204, 135);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(99, 13);
            this.label63.TabIndex = 3;
            this.label63.Text = "Receive data(Hex):";
            // 
            // stcprecv
            // 
            this.stcprecv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.stcprecv.EnableAutoDragDrop = true;
            this.stcprecv.Location = new System.Drawing.Point(202, 152);
            this.stcprecv.Name = "stcprecv";
            this.stcprecv.Size = new System.Drawing.Size(617, 412);
            this.stcprecv.TabIndex = 2;
            this.stcprecv.Text = "";
            // 
            // groupBox25
            // 
            this.groupBox25.Controls.Add(this.listtcp);
            this.groupBox25.Location = new System.Drawing.Point(512, 6);
            this.groupBox25.Name = "groupBox25";
            this.groupBox25.Size = new System.Drawing.Size(307, 115);
            this.groupBox25.TabIndex = 1;
            this.groupBox25.TabStop = false;
            this.groupBox25.Text = "Client";
            // 
            // listtcp
            // 
            this.listtcp.FormattingEnabled = true;
            this.listtcp.Location = new System.Drawing.Point(8, 22);
            this.listtcp.Name = "listtcp";
            this.listtcp.Size = new System.Drawing.Size(287, 82);
            this.listtcp.TabIndex = 0;
            // 
            // groupBox24
            // 
            this.groupBox24.Controls.Add(this.btStop);
            this.groupBox24.Controls.Add(this.btListen);
            this.groupBox24.Controls.Add(this.stcpport);
            this.groupBox24.Controls.Add(this.label62);
            this.groupBox24.Location = new System.Drawing.Point(202, 6);
            this.groupBox24.Name = "groupBox24";
            this.groupBox24.Size = new System.Drawing.Size(301, 115);
            this.groupBox24.TabIndex = 0;
            this.groupBox24.TabStop = false;
            this.groupBox24.Text = "Server Setting";
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(191, 68);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(92, 36);
            this.btStop.TabIndex = 3;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.BtStop_Click);
            // 
            // btListen
            // 
            this.btListen.Location = new System.Drawing.Point(93, 68);
            this.btListen.Name = "btListen";
            this.btListen.Size = new System.Drawing.Size(92, 36);
            this.btListen.TabIndex = 2;
            this.btListen.Text = "Listen";
            this.btListen.UseVisualStyleBackColor = true;
            this.btListen.Click += new System.EventHandler(this.BtListen_Click);
            // 
            // stcpport
            // 
            this.stcpport.Location = new System.Drawing.Point(93, 27);
            this.stcpport.Name = "stcpport";
            this.stcpport.Size = new System.Drawing.Size(187, 20);
            this.stcpport.TabIndex = 1;
            this.stcpport.Text = "6000";
            this.stcpport.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TB_Port_KeyPress);
            // 
            // label62
            // 
            this.label62.AutoSize = true;
            this.label62.Location = new System.Drawing.Point(17, 35);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(56, 13);
            this.label62.TabIndex = 0;
            this.label62.Text = " TCP Port:";
            // 
            // tabPage9
            // 
            this.tabPage9.Controls.Add(this.groupBox26);
            this.tabPage9.Controls.Add(this.bttcpsend);
            this.tabPage9.Controls.Add(this.label69);
            this.tabPage9.Controls.Add(this.ctctsend);
            this.tabPage9.Controls.Add(this.label68);
            this.tabPage9.Controls.Add(this.ctctrecv);
            this.tabPage9.Location = new System.Drawing.Point(4, 27);
            this.tabPage9.Name = "tabPage9";
            this.tabPage9.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage9.Size = new System.Drawing.Size(1064, 577);
            this.tabPage9.TabIndex = 3;
            this.tabPage9.Text = "   TCP Client   ";
            this.tabPage9.UseVisualStyleBackColor = true;
            // 
            // groupBox26
            // 
            this.groupBox26.Controls.Add(this.bttcpdisconnect);
            this.groupBox26.Controls.Add(this.bttcpconnect);
            this.groupBox26.Controls.Add(this.remotePort);
            this.groupBox26.Controls.Add(this.label71);
            this.groupBox26.Controls.Add(this.tcpremoteIp);
            this.groupBox26.Controls.Add(this.label70);
            this.groupBox26.Location = new System.Drawing.Point(209, 6);
            this.groupBox26.Name = "groupBox26";
            this.groupBox26.Size = new System.Drawing.Size(617, 68);
            this.groupBox26.TabIndex = 9;
            this.groupBox26.TabStop = false;
            this.groupBox26.Text = "Setting";
            // 
            // bttcpdisconnect
            // 
            this.bttcpdisconnect.Location = new System.Drawing.Point(521, 27);
            this.bttcpdisconnect.Name = "bttcpdisconnect";
            this.bttcpdisconnect.Size = new System.Drawing.Size(90, 25);
            this.bttcpdisconnect.TabIndex = 26;
            this.bttcpdisconnect.Text = "Disconnect";
            this.bttcpdisconnect.UseVisualStyleBackColor = true;
            this.bttcpdisconnect.Click += new System.EventHandler(this.BtTcpDisconnect_Click);
            // 
            // bttcpconnect
            // 
            this.bttcpconnect.Location = new System.Drawing.Point(415, 27);
            this.bttcpconnect.Name = "bttcpconnect";
            this.bttcpconnect.Size = new System.Drawing.Size(90, 25);
            this.bttcpconnect.TabIndex = 25;
            this.bttcpconnect.Text = "Connect";
            this.bttcpconnect.UseVisualStyleBackColor = true;
            this.bttcpconnect.Click += new System.EventHandler(this.BtTcpConnect_Click);
            // 
            // remotePort
            // 
            this.remotePort.Location = new System.Drawing.Point(268, 27);
            this.remotePort.Name = "remotePort";
            this.remotePort.Size = new System.Drawing.Size(94, 20);
            this.remotePort.TabIndex = 24;
            this.remotePort.Text = "27011";
            // 
            // label71
            // 
            this.label71.AutoSize = true;
            this.label71.Location = new System.Drawing.Point(227, 32);
            this.label71.Name = "label71";
            this.label71.Size = new System.Drawing.Size(29, 13);
            this.label71.TabIndex = 23;
            this.label71.Text = "Port:";
            // 
            // tcpremoteIp
            // 
            this.tcpremoteIp.IpAddressStr = "192.168.0.250";
            this.tcpremoteIp.Location = new System.Drawing.Point(85, 27);
            this.tcpremoteIp.Name = "tcpremoteIp";
            this.tcpremoteIp.Size = new System.Drawing.Size(121, 23);
            this.tcpremoteIp.TabIndex = 22;
            this.tcpremoteIp.Tag = "";
            // 
            // label70
            // 
            this.label70.AutoSize = true;
            this.label70.Location = new System.Drawing.Point(14, 32);
            this.label70.Name = "label70";
            this.label70.Size = new System.Drawing.Size(54, 13);
            this.label70.TabIndex = 0;
            this.label70.Text = "Server IP:";
            // 
            // bttcpsend
            // 
            this.bttcpsend.Enabled = false;
            this.bttcpsend.Location = new System.Drawing.Point(730, 86);
            this.bttcpsend.Name = "bttcpsend";
            this.bttcpsend.Size = new System.Drawing.Size(90, 25);
            this.bttcpsend.TabIndex = 8;
            this.bttcpsend.Text = "Send";
            this.bttcpsend.UseVisualStyleBackColor = true;
            this.bttcpsend.Click += new System.EventHandler(this.BtTcpSend_Click);
            // 
            // label69
            // 
            this.label69.AutoSize = true;
            this.label69.Location = new System.Drawing.Point(211, 93);
            this.label69.Name = "label69";
            this.label69.Size = new System.Drawing.Size(84, 13);
            this.label69.TabIndex = 7;
            this.label69.Text = "Send data(Hex):";
            // 
            // ctctsend
            // 
            this.ctctsend.Location = new System.Drawing.Point(209, 116);
            this.ctctsend.Multiline = true;
            this.ctctsend.Name = "ctctsend";
            this.ctctsend.Size = new System.Drawing.Size(617, 93);
            this.ctctsend.TabIndex = 6;
            this.ctctsend.Text = "04 FF 21 19 95";
            // 
            // label68
            // 
            this.label68.AutoSize = true;
            this.label68.Location = new System.Drawing.Point(211, 218);
            this.label68.Name = "label68";
            this.label68.Size = new System.Drawing.Size(99, 13);
            this.label68.TabIndex = 5;
            this.label68.Text = "Receive data(Hex):";
            // 
            // ctctrecv
            // 
            this.ctctrecv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ctctrecv.EnableAutoDragDrop = true;
            this.ctctrecv.Location = new System.Drawing.Point(209, 234);
            this.ctctrecv.Name = "ctctrecv";
            this.ctctrecv.Size = new System.Drawing.Size(617, 329);
            this.ctctrecv.TabIndex = 4;
            this.ctctrecv.Text = "";
            // 
            // btFlashCl
            // 
            this.btFlashCl.Location = new System.Drawing.Point(939, 653);
            this.btFlashCl.Name = "btFlashCl";
            this.btFlashCl.Size = new System.Drawing.Size(100, 25);
            this.btFlashCl.TabIndex = 5;
            this.btFlashCl.Text = "Refresh";
            this.btFlashCl.UseVisualStyleBackColor = true;
            this.btFlashCl.Click += new System.EventHandler(this.BtFlashCl_Click);
            // 
            // timer_answer
            // 
            this.timer_answer.Interval = 50;
            this.timer_answer.Tick += new System.EventHandler(this.Timer_answer_Tick);
            // 
            // timer_EAS
            // 
            this.timer_EAS.Tick += new System.EventHandler(this.Timer_EAS_Tick);
            // 
            // Timer_Test_6B
            // 
            this.Timer_Test_6B.Tick += new System.EventHandler(this.Timer_Test_6B_Tick);
            // 
            // timer_Buff
            // 
            this.timer_Buff.Interval = 50;
            this.timer_Buff.Tick += new System.EventHandler(this.Timer_Buff_Tick);
            // 
            // timer_RealTime
            // 
            this.timer_RealTime.Interval = 50;
            this.timer_RealTime.Tick += new System.EventHandler(this.Timer_RealTime_Tick);
            // 
            // ckClearOperationRec
            // 
            this.ckClearOperationRec.AutoSize = true;
            this.ckClearOperationRec.Checked = true;
            this.ckClearOperationRec.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckClearOperationRec.Location = new System.Drawing.Point(134, 659);
            this.ckClearOperationRec.Name = "ckClearOperationRec";
            this.ckClearOperationRec.Size = new System.Drawing.Size(75, 17);
            this.ckClearOperationRec.TabIndex = 18;
            this.ckClearOperationRec.Text = "Auto-Clear";
            this.ckClearOperationRec.UseVisualStyleBackColor = true;
            // 
            // UHFReader288MP
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1079, 783);
            this.Controls.Add(this.ckClearOperationRec);
            this.Controls.Add(this.btFlashCl);
            this.Controls.Add(this.Maintab);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lrtxtLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "UHFReader288MP";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "UHFReader288MP Demo V2.3";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Maintab.ResumeLayout(false);
            this.Main_Page1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage_Basic.ResumeLayout(false);
            this.gbReturnLoss.ResumeLayout(false);
            this.gbReturnLoss.PerformLayout();
            this.gbCmdTemperature.ResumeLayout(false);
            this.gbCmdTemperature.PerformLayout();
            this.gbp_DRM.ResumeLayout(false);
            this.gbp_DRM.PerformLayout();
            this.gbp_Retry.ResumeLayout(false);
            this.gbp_wpower.ResumeLayout(false);
            this.gbp_wpower.PerformLayout();
            this.group_maxtime.ResumeLayout(false);
            this.gbp_buff.ResumeLayout(false);
            this.gbp_buff.PerformLayout();
            this.gpb_antconfig.ResumeLayout(false);
            this.gpb_antconfig.PerformLayout();
            this.gpb_checkant.ResumeLayout(false);
            this.gpb_checkant.PerformLayout();
            this.gpb_GPIO.ResumeLayout(false);
            this.gpb_GPIO.PerformLayout();
            this.gpb_OutputRep.ResumeLayout(false);
            this.gpb_OutputRep.PerformLayout();
            this.gpb_Relay.ResumeLayout(false);
            this.gpb_Relay.PerformLayout();
            this.gpb_baud.ResumeLayout(false);
            this.gpb_DBM.ResumeLayout(false);
            this.gpb_DBM.PerformLayout();
            this.gpb_Freq.ResumeLayout(false);
            this.gpb_Freq.PerformLayout();
            this.gpb_beep.ResumeLayout(false);
            this.gpb_beep.PerformLayout();
            this.gpb_RDVersion.ResumeLayout(false);
            this.gpb_RDVersion.PerformLayout();
            this.gpb_Serial.ResumeLayout(false);
            this.gpb_Serial.PerformLayout();
            this.gpb_address.ResumeLayout(false);
            this.gpb_address.PerformLayout();
            this.gpb_tcp.ResumeLayout(false);
            this.gpb_tcp.PerformLayout();
            this.gpb_rs232.ResumeLayout(false);
            this.gpb_rs232.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage_Module.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.groupBox39.ResumeLayout(false);
            this.groupBox39.PerformLayout();
            this.groupBox38.ResumeLayout(false);
            this.groupBox38.PerformLayout();
            this.groupBox34.ResumeLayout(false);
            this.groupBox34.PerformLayout();
            this.groupBox33.ResumeLayout(false);
            this.groupBox33.PerformLayout();
            this.groupBox32.ResumeLayout(false);
            this.groupBox32.PerformLayout();
            this.groupBox28.ResumeLayout(false);
            this.groupBox28.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.Main_Page2.ResumeLayout(false);
            this.tabControl2.ResumeLayout(false);
            this.tabPage_answer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl6)).EndInit();
            this.gbp_MixRead.ResumeLayout(false);
            this.gbp_MixRead.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLedControl1)).EndInit();
            this.groupBox5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.group_ant1.ResumeLayout(false);
            this.group_ant1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabPage_Realtime.ResumeLayout(false);
            this.panel10.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).EndInit();
            this.groupBox41.ResumeLayout(false);
            this.groupBox41.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Mtime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Mtag)).EndInit();
            this.groupBox42.ResumeLayout(false);
            this.tabPage_Buff.ResumeLayout(false);
            this.panel8.ResumeLayout(false);
            this.groupBox27.ResumeLayout(false);
            this.groupBox29.ResumeLayout(false);
            this.groupBox29.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).EndInit();
            this.groupBox30.ResumeLayout(false);
            this.groupBox30.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_cmdTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Btoltime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Btoltag)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_Bcmdsud)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lxLed_BNum)).EndInit();
            this.tabPage_RW.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.groupBox21.ResumeLayout(false);
            this.groupBox21.PerformLayout();
            this.groupBox20.ResumeLayout(false);
            this.groupBox20.PerformLayout();
            this.groupBox18.ResumeLayout(false);
            this.groupBox18.PerformLayout();
            this.groupBox16.ResumeLayout(false);
            this.groupBox16.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.groupBox17.ResumeLayout(false);
            this.groupBox17.PerformLayout();
            this.groupBox13.ResumeLayout(false);
            this.groupBox13.PerformLayout();
            this.groupBox14.ResumeLayout(false);
            this.groupBox14.PerformLayout();
            this.groupBox15.ResumeLayout(false);
            this.groupBox15.PerformLayout();
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.groupBox12.ResumeLayout(false);
            this.groupBox12.PerformLayout();
            this.groupBox31.ResumeLayout(false);
            this.groupBox31.PerformLayout();
            this.groupBox40.ResumeLayout(false);
            this.groupBox40.PerformLayout();
            this.groupBox10.ResumeLayout(false);
            this.groupBox10.PerformLayout();
            this.Main_Page3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.groupBox23.ResumeLayout(false);
            this.groupBox23.PerformLayout();
            this.groupBox22.ResumeLayout(false);
            this.groupBox22.PerformLayout();
            this.groupBox19.ResumeLayout(false);
            this.groupBox19.PerformLayout();
            this.Main_Page4.ResumeLayout(false);
            this.tabControl3.ResumeLayout(false);
            this.tabPage6.ResumeLayout(false);
            this.tabPage6.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.tabPage8.ResumeLayout(false);
            this.tabPage8.PerformLayout();
            this.groupBox25.ResumeLayout(false);
            this.groupBox24.ResumeLayout(false);
            this.groupBox24.PerformLayout();
            this.tabPage9.ResumeLayout(false);
            this.tabPage9.PerformLayout();
            this.groupBox26.ResumeLayout(false);
            this.groupBox26.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Timer timer_RealTime;
        private System.Windows.Forms.Timer timer_answer;
        private System.Windows.Forms.Timer timer_EAS;
        private System.Windows.Forms.Timer Timer_Test_6B;
        private System.Windows.Forms.Timer timer_Buff;

        private LogRichTextBox lrtxtLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl Maintab;
        private System.Windows.Forms.TabPage Main_Page1;
        private System.Windows.Forms.TabPage Main_Page2;
        private System.Windows.Forms.TabPage Main_Page3;
        private System.Windows.Forms.TabPage Main_Page4;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage_Basic;
        private System.Windows.Forms.GroupBox gpb_rs232;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rb_tcp;
        private System.Windows.Forms.RadioButton rb_rs232;
        private System.Windows.Forms.Button btDisConnect232;
        private System.Windows.Forms.Button BtConnect232;
        private System.Windows.Forms.ComboBox ComboBox_baud2;
        private System.Windows.Forms.Label label47;
        internal System.Windows.Forms.ComboBox ComboBox_COM;
        internal System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox gpb_tcp;
        private System.Windows.Forms.Button btDisConnectTcp;
        private System.Windows.Forms.Button btConnectTcp;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tb_Port;
        private IpAddressTextBox ipServerIP;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage_answer;
        private System.Windows.Forms.Button btFlashCl;
        private System.Windows.Forms.Panel panel1;
        private LxLedControl lxLedControl5;
        private System.Windows.Forms.Label label9;
        private LxLedControl lxLedControl4;
        private System.Windows.Forms.Label label8;
        private LxLedControl lxLedControl3;
        private System.Windows.Forms.Label label7;
        private LxLedControl lxLedControl2;
        private System.Windows.Forms.Label label6;
        private LxLedControl lxLedControl1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ComboBox com_scantime;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.ComboBox com_S;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.ComboBox com_Q;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.GroupBox group_ant1;
        public System.Windows.Forms.CheckBox check_ant4;
        public System.Windows.Forms.CheckBox check_ant3;
        public System.Windows.Forms.CheckBox check_ant2;
        public System.Windows.Forms.CheckBox check_ant1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rb_tid;
        private System.Windows.Forms.RadioButton rb_epc;
        private System.Windows.Forms.Button btIventoryG2;
        private System.Windows.Forms.GroupBox gpb_address;
        private System.Windows.Forms.TextBox text_address;
        private System.Windows.Forms.Button btaddress;
        private System.Windows.Forms.GroupBox gpb_Serial;
        private System.Windows.Forms.TextBox TextBox_Serial;
        private System.Windows.Forms.Button btSerial;
        private System.Windows.Forms.GroupBox gpb_beep;
        private System.Windows.Forms.GroupBox gpb_RDVersion;
        private System.Windows.Forms.TextBox text_RDVersion;
        private System.Windows.Forms.RadioButton Radio_beepDis;
        private System.Windows.Forms.RadioButton Radio_beepEn;
        private System.Windows.Forms.Button Button_Beep;
        private System.Windows.Forms.GroupBox gpb_Freq;
        private System.Windows.Forms.RadioButton radioButton_band4;
        private System.Windows.Forms.RadioButton radioButton_band3;
        private System.Windows.Forms.RadioButton radioButton_band2;
        private System.Windows.Forms.RadioButton radioButton_band1;
        private System.Windows.Forms.Button btFreq;
        private System.Windows.Forms.ComboBox ComboBox_dmaxfre;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox ComboBox_dminfre;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox CheckBox_SameFre;
        private System.Windows.Forms.GroupBox gpb_DBM;
        private System.Windows.Forms.Button BT_DBM;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox ComboBox_PowerDbm;
        private System.Windows.Forms.GroupBox gpb_baud;
        private System.Windows.Forms.Button btBaudRate;
        private System.Windows.Forms.ComboBox ComboBox_baud;
        private System.Windows.Forms.GroupBox gpb_Relay;
        private System.Windows.Forms.Button btRelay;
        private System.Windows.Forms.Label label38;
        private System.Windows.Forms.ComboBox ComboBox_RelayTime;
        private System.Windows.Forms.Label label37;
        private System.Windows.Forms.GroupBox gpb_OutputRep;
        private System.Windows.Forms.Button btOutputRep;
        private System.Windows.Forms.CheckBox check_OutputRep4;
        private System.Windows.Forms.CheckBox check_OutputRep3;
        private System.Windows.Forms.CheckBox check_OutputRep2;
        private System.Windows.Forms.CheckBox check_OutputRep1;
        private System.Windows.Forms.GroupBox gpb_antconfig;
        private System.Windows.Forms.Button Button_Ant;
        private System.Windows.Forms.CheckBox checkant4;
        private System.Windows.Forms.CheckBox checkant3;
        private System.Windows.Forms.CheckBox checkant2;
        private System.Windows.Forms.CheckBox checkant1;
        private System.Windows.Forms.GroupBox gpb_checkant;
        private System.Windows.Forms.Button btSetcheckant;
        private System.Windows.Forms.RadioButton rb_Closecheckant;
        private System.Windows.Forms.RadioButton rb_Opencheckant;
        private System.Windows.Forms.GroupBox gpb_GPIO;
        private System.Windows.Forms.Button Button_GetGPIO;
        private System.Windows.Forms.Button Button_SetGPIO;
        private System.Windows.Forms.CheckBox check_int2;
        private System.Windows.Forms.CheckBox check_int1;
        private System.Windows.Forms.CheckBox check_out2;
        private System.Windows.Forms.CheckBox check_out1;
        private System.Windows.Forms.Button btGetInformation;
        private System.Windows.Forms.Button btDefault;
        private System.Windows.Forms.TabPage tabPage_RW;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.GroupBox groupBox10;
        private System.Windows.Forms.TextBox text_epc;
        private System.Windows.Forms.CheckBox check_selecttag;
        private System.Windows.Forms.Button btSelectTag;
        private System.Windows.Forms.ComboBox comboBox_EPC;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.GroupBox groupBox31;
        private System.Windows.Forms.TextBox maskData_textBox;
        private System.Windows.Forms.Label label60;
        private System.Windows.Forms.GroupBox groupBox40;
        private System.Windows.Forms.RadioButton R_User;
        private System.Windows.Forms.RadioButton R_TID;
        private System.Windows.Forms.RadioButton R_EPC;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TextBox maskLen_textBox;
        private System.Windows.Forms.Label label44;
        private System.Windows.Forms.TextBox maskadr_textbox;
        private System.Windows.Forms.Label label43;
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.TextBox textBox_pc;
        private System.Windows.Forms.CheckBox checkBox_pc;
        private System.Windows.Forms.Button btBlockWrite;
        private System.Windows.Forms.Button btBlockErase;
        private System.Windows.Forms.Button btWrite;
        private System.Windows.Forms.Button btRead;
        private System.Windows.Forms.TextBox text_WriteData;
        private System.Windows.Forms.TextBox text_AccessCode2;
        private System.Windows.Forms.TextBox text_RWlen;
        private System.Windows.Forms.TextBox text_WordPtr;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.GroupBox groupBox12;
        private System.Windows.Forms.RadioButton C_User;
        private System.Windows.Forms.RadioButton C_TID;
        private System.Windows.Forms.RadioButton C_EPC;
        private System.Windows.Forms.RadioButton C_Reserve;
        private System.Windows.Forms.GroupBox groupBox13;
        private System.Windows.Forms.Button btSetProtectState;
        private System.Windows.Forms.TextBox Edit_AccessCode6;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.GroupBox groupBox14;
        private System.Windows.Forms.RadioButton AlwaysNot2;
        private System.Windows.Forms.RadioButton Always2;
        private System.Windows.Forms.RadioButton Proect2;
        private System.Windows.Forms.RadioButton NoProect2;
        private System.Windows.Forms.GroupBox groupBox15;
        private System.Windows.Forms.RadioButton P_User;
        private System.Windows.Forms.RadioButton P_TID;
        private System.Windows.Forms.RadioButton P_EPC;
        private System.Windows.Forms.RadioButton P_kill;
        private System.Windows.Forms.RadioButton p_pass;
        private System.Windows.Forms.GroupBox groupBox16;
        private System.Windows.Forms.Button btCheckEASAlarm;
        private System.Windows.Forms.Button btSetEASAlarm_G2;
        private System.Windows.Forms.GroupBox groupBox17;
        private System.Windows.Forms.RadioButton NoAlarm_G2;
        private System.Windows.Forms.RadioButton Alarm_G2;
        private System.Windows.Forms.TextBox text_AccessCode5;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.GroupBox groupBox21;
        private System.Windows.Forms.Button btCheckReadProtected_G2;
        private System.Windows.Forms.Button btRemoveReadProtect_G2;
        private System.Windows.Forms.Button btSetMultiReadProtect_G2;
        private System.Windows.Forms.Button btSetReadProtect_G2;
        private System.Windows.Forms.TextBox text_AccessCode4;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.GroupBox groupBox20;
        private System.Windows.Forms.Button btWriteEPC_G2;
        private System.Windows.Forms.TextBox text_AccessCode3;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.TextBox text_WriteEPC;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.GroupBox groupBox18;
        private System.Windows.Forms.Button btDestroyCard;
        private System.Windows.Forms.TextBox text_DestroyCode;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ListView ListView_ID_6B;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.GroupBox groupBox19;
        private System.Windows.Forms.RadioButton rb_mutiple;
        private System.Windows.Forms.RadioButton rb_single;
        private System.Windows.Forms.Button btInventory6B;
        private System.Windows.Forms.TextBox text_6BUID;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.GroupBox groupBox22;
        private System.Windows.Forms.GroupBox groupBox23;
        private System.Windows.Forms.TextBox text_R6BLen;
        private System.Windows.Forms.Label label35;
        private System.Windows.Forms.TextBox text_R6BAddr;
        private System.Windows.Forms.Label label30;
        private System.Windows.Forms.TextBox text_R6B;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.Button btRead6B;
        private System.Windows.Forms.TextBox text_W6B;
        private System.Windows.Forms.Label label48;
        private System.Windows.Forms.Button btWrite6B;
        private System.Windows.Forms.TextBox text_W6BLen;
        private System.Windows.Forms.Label label49;
        private System.Windows.Forms.TextBox text_W6BAddr;
        private System.Windows.Forms.Label label50;
        private System.Windows.Forms.TextBox text_lock6b;
        private System.Windows.Forms.Label label51;
        private System.Windows.Forms.TextBox text_Statu6B;
        private System.Windows.Forms.Button btCheckLock6B;
        private System.Windows.Forms.Button btLock6B;
        private System.Windows.Forms.TextBox text_checkaddr;
        private System.Windows.Forms.Label label52;
        private System.Windows.Forms.TabControl tabControl3;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem iEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem telnetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem languageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem englishToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ListView DeviceListView;
        private System.Windows.Forms.ColumnHeader deviceName;
        private System.Windows.Forms.ColumnHeader deviceIP;
        private System.Windows.Forms.ColumnHeader deviceMac;
        private System.Windows.Forms.TabPage tabPage8;
        private System.Windows.Forms.GroupBox groupBox25;
        private System.Windows.Forms.ListBox listtcp;
        private System.Windows.Forms.GroupBox groupBox24;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button btListen;
        private System.Windows.Forms.TextBox stcpport;
        private System.Windows.Forms.Label label62;
        private System.Windows.Forms.Label label63;
        private LogRichTextBox stcprecv;
        private System.Windows.Forms.TabPage tabPage9;
        private System.Windows.Forms.Button bttcpsend;
        private System.Windows.Forms.Label label69;
        private System.Windows.Forms.TextBox ctctsend;
        private System.Windows.Forms.Label label68;
        private LogRichTextBox ctctrecv;
        private System.Windows.Forms.GroupBox groupBox26;
        private System.Windows.Forms.Button bttcpdisconnect;
        private System.Windows.Forms.Button bttcpconnect;
        private System.Windows.Forms.TextBox remotePort;
        private System.Windows.Forms.Label label71;
        private IpAddressTextBox tcpremoteIp;
        private System.Windows.Forms.Label label70;
        private System.Windows.Forms.TextBox text_target;
        private System.Windows.Forms.CheckBox check_num;
        private System.Windows.Forms.ComboBox com_Target;
        private System.Windows.Forms.Label label79;
        private System.Windows.Forms.Label label80;
        private System.Windows.Forms.GroupBox gbp_buff;
        private System.Windows.Forms.RadioButton rb496;
        private System.Windows.Forms.RadioButton rb128;
        private System.Windows.Forms.Button btGetEPCandTIDLen;
        private System.Windows.Forms.Button btSetEPCandTIDLen;
        private System.Windows.Forms.TabPage tabPage_Buff;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.GroupBox groupBox27;
        private System.Windows.Forms.Button btQueryBuffNum;
        private System.Windows.Forms.Button btRandCbuff;
        private System.Windows.Forms.Button btClearBuff;
        private System.Windows.Forms.Button btReadBuff;
        private System.Windows.Forms.GroupBox groupBox29;
        private System.Windows.Forms.RadioButton rb_btid;
        private System.Windows.Forms.RadioButton rb_bepc;
        private System.Windows.Forms.Button btStartBuff;
        private System.Windows.Forms.Label label83;
        private System.Windows.Forms.DataGridView dataGridView3;
        private System.Windows.Forms.GroupBox groupBox30;
        private LxLedControl lxLed_cmdTime;
        private System.Windows.Forms.Label label86;
        private LxLedControl lxLed_Btoltime;
        private System.Windows.Forms.Label label91;
        private LxLedControl lxLed_Btoltag;
        private System.Windows.Forms.Label label92;
        private LxLedControl lxLed_Bcmdsud;
        private System.Windows.Forms.Label label94;
        private LxLedControl lxLed_BNum;
        private System.Windows.Forms.Label label95;
        private System.Windows.Forms.Button btExtWrite;
        private System.Windows.Forms.Button btExtRead;
        private System.Windows.Forms.GroupBox group_maxtime;
        private System.Windows.Forms.Button btSetMaxtime;
        private System.Windows.Forms.ComboBox comboBox_maxtime;
        private System.Windows.Forms.CheckBox checkBox_rate;
        private System.Windows.Forms.TabPage tabPage_Module;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.GroupBox groupBox32;
        private System.Windows.Forms.ComboBox com_MFliterTime;
        private System.Windows.Forms.Label label97;
        private System.Windows.Forms.GroupBox groupBox28;
        private System.Windows.Forms.ComboBox com_MRPTime;
        private System.Windows.Forms.Label label96;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton MRB_6B;
        private System.Windows.Forms.RadioButton MRB_G2;
        private System.Windows.Forms.GroupBox groupBox33;
        private System.Windows.Forms.ComboBox com_MS;
        private System.Windows.Forms.Label label99;
        private System.Windows.Forms.ComboBox com_MQ;
        private System.Windows.Forms.Label label98;
        private System.Windows.Forms.GroupBox groupBox38;
        private System.Windows.Forms.TextBox txt_Mtidlen;
        private System.Windows.Forms.Label label103;
        private System.Windows.Forms.TextBox txt_mtidaddr;
        private System.Windows.Forms.Label label104;
        private System.Windows.Forms.GroupBox groupBox34;
        private System.Windows.Forms.TextBox txt_Mdata;
        private System.Windows.Forms.Label label100;
        private System.Windows.Forms.TextBox txt_Mlen;
        private System.Windows.Forms.Label label101;
        private System.Windows.Forms.TextBox txt_Maddr;
        private System.Windows.Forms.Label label102;
        private System.Windows.Forms.RadioButton RBM_USER;
        private System.Windows.Forms.RadioButton RBM_TID;
        private System.Windows.Forms.RadioButton RBM_EPC;
        private System.Windows.Forms.Button btMGetParameter;
        private System.Windows.Forms.GroupBox groupBox39;
        private System.Windows.Forms.Button btSetMmode;
        private System.Windows.Forms.ComboBox com_Mmode;
        private System.Windows.Forms.Label label111;
        private System.Windows.Forms.Button btMSetParameter;
        private System.Windows.Forms.TabPage tabPage_Realtime;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.DataGridView dataGridView4;
        private System.Windows.Forms.GroupBox groupBox41;
        private LxLedControl lxLed_Mtime;
        private System.Windows.Forms.Label label113;
        private LxLedControl lxLed_Mtag;
        private System.Windows.Forms.Label label116;
        private System.Windows.Forms.GroupBox groupBox42;
        private System.Windows.Forms.Button btStartMactive;
        private System.Windows.Forms.CheckBox checkBox_tid;
        private System.Windows.Forms.CheckBox checkBox_mask;
        private System.Windows.Forms.RadioButton radioButton_band8;
        private System.Windows.Forms.GroupBox gbp_wpower;
        private System.Windows.Forms.RadioButton rb_wp2;
        private System.Windows.Forms.RadioButton rb_wp1;
        private System.Windows.Forms.Button bt_GetWpower;
        private System.Windows.Forms.Button bt_SetWpower;
        private System.Windows.Forms.Label label112;
        private System.Windows.Forms.ComboBox com_wpower;
        private System.Windows.Forms.GroupBox gbp_Retry;
        private System.Windows.Forms.ComboBox com_retrytimes;
        private System.Windows.Forms.Button bt_Getretry;
        private System.Windows.Forms.Button bt_Setretry;
        private System.Windows.Forms.GroupBox gbp_MixRead;
        private System.Windows.Forms.ComboBox com_MixMem;
        private System.Windows.Forms.Label label114;
        private System.Windows.Forms.RadioButton rb_mix;
        private System.Windows.Forms.TextBox text_readpsd;
        private System.Windows.Forms.Label label118;
        private System.Windows.Forms.TextBox text_readLen;
        private System.Windows.Forms.Label label117;
        private System.Windows.Forms.TextBox text_readadr;
        private System.Windows.Forms.Label label115;
        private System.Windows.Forms.RadioButton rb_fastid;
        private LxLedControl lxLedControl6;
        private System.Windows.Forms.Label label119;
        private System.Windows.Forms.GroupBox gbp_DRM;
        private System.Windows.Forms.RadioButton DRM_CLOSE;
        private System.Windows.Forms.RadioButton DRM_OPEN;
        private System.Windows.Forms.Button bt_GetDRM;
        private System.Windows.Forms.Button bt_setDRM;
        private System.Windows.Forms.GroupBox gbReturnLoss;
        private System.Windows.Forms.Label label120;
        private System.Windows.Forms.Label label121;
        private System.Windows.Forms.ComboBox cmbReturnLossFreq;
        private System.Windows.Forms.Label label122;
        private System.Windows.Forms.TextBox textReturnLoss;
        private System.Windows.Forms.Button btReturnLoss;
        private System.Windows.Forms.GroupBox gbCmdTemperature;
        private System.Windows.Forms.Button btnGetReaderTemperature;
        private System.Windows.Forms.TextBox txtReaderTemperature;
        private System.Windows.Forms.ComboBox cbbAnt;
        private System.Windows.Forms.CheckBox ckClearOperationRec;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column7;
        private System.Windows.Forms.RadioButton radioButton_band12;
        #endregion
    }
}
