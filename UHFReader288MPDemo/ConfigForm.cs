using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    public partial class ConfigForm : Form
    {
        private AutoPropertyClass AutoProperty;
        private readonly DeviceClass CurrentDevice;

        public ConfigForm(DeviceClass SelectedDevice)
        {
            InitializeComponent();

            Debug.Assert(SelectedDevice != null, "Invalid SelectedDevice!");
            CurrentDevice = SelectedDevice;

            tabControl.TabPages.Remove(tabPage_PPP);
            tabControl.TabPages.Remove(tabPage_PPPoE);
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            AutoProperty = new AutoPropertyClass(CurrentDevice.DevHandle);

            // Basic Settings
            AutoProperty.AddPropertyParaMap("ServerName", DevControl.PARA_TYPES.DEVICENAME);
            AutoProperty.AddPropertyParaMap("TimeZone", DevControl.PARA_TYPES.TIMEZONE);
            AutoProperty.AddPropertyParaMap("TimeYear", DevControl.PARA_TYPES.LOCALTIME_YEAR);
            AutoProperty.AddPropertyParaMap("TimeMon", DevControl.PARA_TYPES.LOCALTIME_MONTH);
            AutoProperty.AddPropertyParaMap("TimeDay", DevControl.PARA_TYPES.LOCALTIME_DAY);
            AutoProperty.AddPropertyParaMap("TimeHour", DevControl.PARA_TYPES.LOCALTIME_HOUR);
            AutoProperty.AddPropertyParaMap("TimeMin", DevControl.PARA_TYPES.LOCALTIME_MINUTE);
            AutoProperty.AddPropertyParaMap("TimeSec", DevControl.PARA_TYPES.LOCALTIME_SECOND);
            AutoProperty.AddPropertyParaMap("TimeServer", DevControl.PARA_TYPES.TIMESERVER);
            AutoProperty.AddPropertyParaMap("WebConsole", DevControl.PARA_TYPES.WEBCONSOLE);
            AutoProperty.AddPropertyParaMap("TelnetConsole", DevControl.PARA_TYPES.TELNETCONSOLE);
            AutoProperty.AddPropertyParaMap("TerminalName", DevControl.PARA_TYPES.TERMINALNAME);

            // Network Settings;
            AutoProperty.AddPropertyParaMap("IpConfig", DevControl.PARA_TYPES.IPCONFIGURATION);
            AutoProperty.AddPropertyParaMap("BOOTP", DevControl.PARA_TYPES.BOOTP);
            AutoProperty.AddPropertyParaMap("DHCP", DevControl.PARA_TYPES.DHCP);
            AutoProperty.AddPropertyParaMap("AutoIP", DevControl.PARA_TYPES.AUTOIP);
            AutoProperty.AddPropertyParaMap("DHCPName", DevControl.PARA_TYPES.DHCPHOSTNAME);
            AutoProperty.AddPropertyParaMap("IpAddress", DevControl.PARA_TYPES.IPADDRESS);
            AutoProperty.AddPropertyParaMap("Subnet", DevControl.PARA_TYPES.SUBNET);
            AutoProperty.AddPropertyParaMap("Gateway", DevControl.PARA_TYPES.DEFAULTGATEWAY);
            AutoProperty.AddPropertyParaMap("PreferredDNS", DevControl.PARA_TYPES.PREFERREDDNSSERVER);
            AutoProperty.AddPropertyParaMap("AlternateDNS", DevControl.PARA_TYPES.ALTERNATEDNSSERVER);
            AutoProperty.AddPropertyParaMap("MacAddress", DevControl.PARA_TYPES.MACADDRESS);
            AutoProperty.AddPropertyParaMap("AutoNegotiate", DevControl.PARA_TYPES.AUTONEGOTIATE);
            AutoProperty.AddPropertyParaMap("NetcardSpeed", DevControl.PARA_TYPES.SPEED);
            AutoProperty.AddPropertyParaMap("NetcardDuplex", DevControl.PARA_TYPES.DUPLEX);

            // Server Settings;
            AutoProperty.AddPropertyParaMap("ARPTimeout", DevControl.PARA_TYPES.ARPCACHETIMEOUT);
            AutoProperty.AddPropertyParaMap("CPUMode", DevControl.PARA_TYPES.CPUPERFORMANCEMODE);
            AutoProperty.AddPropertyParaMap("HttpPort", DevControl.PARA_TYPES.HTTPSERVERPORT);
            AutoProperty.AddPropertyParaMap("MTU", DevControl.PARA_TYPES.MTUSIZE);

            // Serial Settings;
            AutoProperty.AddPropertyParaMap("SerialEnable", DevControl.PARA_TYPES.SERIALPORTOPTIONS);
            AutoProperty.AddPropertyParaMap("SerialProtocol", DevControl.PARA_TYPES.SERIALPORTPROTOCOL);
            AutoProperty.AddPropertyParaMap("SerialFIFO", DevControl.PARA_TYPES.FIFO);
            AutoProperty.AddPropertyParaMap("DataBits", DevControl.PARA_TYPES.DATABITS);
            AutoProperty.AddPropertyParaMap("FlowControl", DevControl.PARA_TYPES.FLOWCONTROL);
            AutoProperty.AddPropertyParaMap("BaudRate", DevControl.PARA_TYPES.BAUDRATE);
            AutoProperty.AddPropertyParaMap("SerialParity", DevControl.PARA_TYPES.PARITY);
            AutoProperty.AddPropertyParaMap("StopBits", DevControl.PARA_TYPES.STOPBITS);
            AutoProperty.AddPropertyParaMap("SerialPacking", DevControl.PARA_TYPES.ENABLEPACKING);
            AutoProperty.AddPropertyParaMap("IdlePacking", DevControl.PARA_TYPES.IDLEGAPTIME);
            AutoProperty.AddPropertyParaMap("2BytesPacking", DevControl.PARA_TYPES.MATCH2BYTESEQUENCE);
            AutoProperty.AddPropertyParaMap("Byte1Packing", DevControl.PARA_TYPES.FIRSTMATCHBYTE);
            AutoProperty.AddPropertyParaMap("Byte2Packing", DevControl.PARA_TYPES.LASTMATCHBYTE);
            AutoProperty.AddPropertyParaMap("FramePacking", DevControl.PARA_TYPES.SENDFRAMEONLY);
            AutoProperty.AddPropertyParaMap("TrailingPacking", DevControl.PARA_TYPES.SENDTRAILINGBYTES);

            // Connection Settings;
            // TCP
            AutoProperty.AddPropertyParaMap("ConnProtocol", DevControl.PARA_TYPES.NETPROTOCOL);
            AutoProperty.AddPropertyParaMap("ConnWorkMode", DevControl.PARA_TYPES.ACCEPTIONINCOMING);
            AutoProperty.AddPropertyParaMap("ConnActive", DevControl.PARA_TYPES.ACTIVECONNECT);
            AutoProperty.AddPropertyParaMap("ConnStartChar", DevControl.PARA_TYPES.STARTCHARACTER);
            AutoProperty.AddPropertyParaMap("ConnRemoteHost", DevControl.PARA_TYPES.REMOTEHOST);
            AutoProperty.AddPropertyParaMap("ConnRemotePort", DevControl.PARA_TYPES.REMOTEPORT);
            AutoProperty.AddPropertyParaMap("ConnLocalPort", DevControl.PARA_TYPES.LOCALPORT);
            AutoProperty.AddPropertyParaMap("ConnResponse", DevControl.PARA_TYPES.CONNECTRESPONSE);
            AutoProperty.AddPropertyParaMap("ConnDNS", DevControl.PARA_TYPES.DNSQUERYPERIOD);
            AutoProperty.AddPropertyParaMap("ConnHostList", DevControl.PARA_TYPES.USEHOSTLIST);
            AutoProperty.AddPropertyParaMap("ConnDSR", DevControl.PARA_TYPES.ONDSRDROP);
            AutoProperty.AddPropertyParaMap("ConnEOT", DevControl.PARA_TYPES.CHECKEOT);
            AutoProperty.AddPropertyParaMap("ConnHard", DevControl.PARA_TYPES.HARDDISCONNECT);
            AutoProperty.AddPropertyParaMap("ConnTimeout_M", DevControl.PARA_TYPES.INACTIVITYTIMEOUT_M);
            AutoProperty.AddPropertyParaMap("ConnTimeout_S", DevControl.PARA_TYPES.INACTIVITYTIMEOUT_S);
            AutoProperty.AddPropertyParaMap("ConnFlushInActive", DevControl.PARA_TYPES.INPUTWITHACTIVECONNECT);
            AutoProperty.AddPropertyParaMap("ConnFlushInPassive", DevControl.PARA_TYPES.INPUTWITHPASSIVECONNECT);
            AutoProperty.AddPropertyParaMap("ConnFlushInDis", DevControl.PARA_TYPES.INPUTATTIMEOFDISCONNECT);
            AutoProperty.AddPropertyParaMap("ConnFlushOutActive", DevControl.PARA_TYPES.OUTPUTWITHACTIVECONNECT);
            AutoProperty.AddPropertyParaMap("ConnFlushOutPassive", DevControl.PARA_TYPES.OUTPUTWITHPASSIVECONNECT);
            AutoProperty.AddPropertyParaMap("ConnFlushOutDis", DevControl.PARA_TYPES.OUTPUTATTIMEOFDISCONNECT);

            // UDP
            AutoProperty.AddPropertyParaMap("UdpDataGram", DevControl.PARA_TYPES.DATAGRAMTYPE);
            AutoProperty.AddPropertyParaMap("UdpIncoming", DevControl.PARA_TYPES.ACCEPTINCOMING);
            AutoProperty.AddPropertyParaMap("UdpMulLocalPort", DevControl.PARA_TYPES.UDPLOCALPORT);
            AutoProperty.AddPropertyParaMap("UdpMulRemotePort", DevControl.PARA_TYPES.UDPREMOTEPORT);
            AutoProperty.AddPropertyParaMap("UdpMulRemoteIP", DevControl.PARA_TYPES.UDPNETSEGMENT);
            AutoProperty.AddPropertyParaMap("UdpUniLocalPort", DevControl.PARA_TYPES.UDPUNICASTLOCALPORT);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP1", DevControl.PARA_TYPES.DEVICEADDRESSTABLE1_BEGINIP);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP2", DevControl.PARA_TYPES.DEVICEADDRESSTABLE1_ENDIP);
            AutoProperty.AddPropertyParaMap("UdpRemotePort1", DevControl.PARA_TYPES.DEVICEADDRESSTABLE1_PORT);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP3", DevControl.PARA_TYPES.DEVICEADDRESSTABLE2_BEGINIP);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP4", DevControl.PARA_TYPES.DEVICEADDRESSTABLE2_ENDIP);
            AutoProperty.AddPropertyParaMap("UdpRemotePort2", DevControl.PARA_TYPES.DEVICEADDRESSTABLE2_PORT);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP5", DevControl.PARA_TYPES.DEVICEADDRESSTABLE3_BEGINIP);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP6", DevControl.PARA_TYPES.DEVICEADDRESSTABLE3_ENDIP);
            AutoProperty.AddPropertyParaMap("UdpRemotePort3", DevControl.PARA_TYPES.DEVICEADDRESSTABLE3_PORT);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP7", DevControl.PARA_TYPES.DEVICEADDRESSTABLE4_BEGINIP);
            AutoProperty.AddPropertyParaMap("UdpRemoteIP8", DevControl.PARA_TYPES.DEVICEADDRESSTABLE4_ENDIP);
            AutoProperty.AddPropertyParaMap("UdpRemotePort4", DevControl.PARA_TYPES.DEVICEADDRESSTABLE4_PORT);

            // Hostlist Settings;   
            AutoProperty.AddPropertyParaMap("RetryCounter", DevControl.PARA_TYPES.RETRYCOUNTER);
            AutoProperty.AddPropertyParaMap("RetryTimeout", DevControl.PARA_TYPES.RETRYTIMEOUT);
            AutoProperty.AddPropertyParaMap("HostIp1", DevControl.PARA_TYPES.HOSTLIST1_IP);
            AutoProperty.AddPropertyParaMap("HostIp2", DevControl.PARA_TYPES.HOSTLIST2_IP);
            AutoProperty.AddPropertyParaMap("HostIp3", DevControl.PARA_TYPES.HOSTLIST3_IP);
            AutoProperty.AddPropertyParaMap("HostIp4", DevControl.PARA_TYPES.HOSTLIST4_IP);
            AutoProperty.AddPropertyParaMap("HostIp5", DevControl.PARA_TYPES.HOSTLIST5_IP);
            AutoProperty.AddPropertyParaMap("HostIp6", DevControl.PARA_TYPES.HOSTLIST6_IP);
            AutoProperty.AddPropertyParaMap("HostIp7", DevControl.PARA_TYPES.HOSTLIST7_IP);
            AutoProperty.AddPropertyParaMap("HostIp8", DevControl.PARA_TYPES.HOSTLIST8_IP);
            AutoProperty.AddPropertyParaMap("HostIp9", DevControl.PARA_TYPES.HOSTLIST9_IP);
            AutoProperty.AddPropertyParaMap("HostIp10", DevControl.PARA_TYPES.HOSTLIST10_IP);
            AutoProperty.AddPropertyParaMap("HostIp11", DevControl.PARA_TYPES.HOSTLIST11_IP);
            AutoProperty.AddPropertyParaMap("HostIp12", DevControl.PARA_TYPES.HOSTLIST12_IP);
            AutoProperty.AddPropertyParaMap("HostPort1", DevControl.PARA_TYPES.HOSTLIST1_PORT);
            AutoProperty.AddPropertyParaMap("HostPort2", DevControl.PARA_TYPES.HOSTLIST2_PORT);
            AutoProperty.AddPropertyParaMap("HostPort3", DevControl.PARA_TYPES.HOSTLIST3_PORT);
            AutoProperty.AddPropertyParaMap("HostPort4", DevControl.PARA_TYPES.HOSTLIST4_PORT);
            AutoProperty.AddPropertyParaMap("HostPort5", DevControl.PARA_TYPES.HOSTLIST5_PORT);
            AutoProperty.AddPropertyParaMap("HostPort6", DevControl.PARA_TYPES.HOSTLIST6_PORT);
            AutoProperty.AddPropertyParaMap("HostPort7", DevControl.PARA_TYPES.HOSTLIST7_PORT);
            AutoProperty.AddPropertyParaMap("HostPort8", DevControl.PARA_TYPES.HOSTLIST8_PORT);
            AutoProperty.AddPropertyParaMap("HostPort9", DevControl.PARA_TYPES.HOSTLIST9_PORT);
            AutoProperty.AddPropertyParaMap("HostPort10", DevControl.PARA_TYPES.HOSTLIST10_PORT);
            AutoProperty.AddPropertyParaMap("HostPort11", DevControl.PARA_TYPES.HOSTLIST11_PORT);
            AutoProperty.AddPropertyParaMap("HostPort12", DevControl.PARA_TYPES.HOSTLIST12_PORT);

            // Fill CHANNELNUM
            for (int channelNum = 0; channelNum < 32; channelNum++)
            {
                bool isSupport;
                isSupport = CurrentDevice.IsSupportChannel(channelNum);
                if (isSupport == true)
                {
                    comboBox_SerialChannel.Items.Add(channelNum);
                    comboBox_ConnChannel.Items.Add(channelNum);
                    comboBox_HostChannel.Items.Add(channelNum);
                }
            }

            comboBox_SerialChannel.SelectedIndex = 0;
            comboBox_ConnChannel.SelectedIndex = 0;
            comboBox_HostChannel.SelectedIndex = 0;

            comboBox_SerialChannel.SelectedIndexChanged += new EventHandler(ComboBox_Channel_SelectedIndexChanged);
            comboBox_ConnChannel.SelectedIndexChanged += new EventHandler(ComboBox_Channel_SelectedIndexChanged);
            comboBox_HostChannel.SelectedIndexChanged += new EventHandler(ComboBox_Channel_SelectedIndexChanged);

            netProtocolCB.SelectedItem = "UDP";
            comboBox_IpConfig.SelectedItem = "User Config";

            ReflashSelectedTabPage(tabControl.SelectedTab);
        }

        private void SubmitSelectedTabPage(TabPage currentTabPage)
        {
            string errMsg;
            DevControl.TagErrorCode eCode;

            if (currentTabPage.Equals(tabPage_Power))
            {
                RebootType rebootType;

                if (radioButton_Default.Checked)
                    rebootType = RebootType.DefaultWithoutReboot;
                else if (radioButton_DefaultReboot.Checked)
                    rebootType = RebootType.DefaultAndReboot;
                else if (radioButton_Reboot.Checked)
                    rebootType = RebootType.RebootWithoutSave;
                else
                    rebootType = RebootType.SaveAndReboot;

                eCode = CurrentDevice.RebootManage(rebootType);
                errMsg = ErrorHandling.HandleDeviceError(eCode, CurrentDevice);
                Lable_Message.Text = errMsg;
                MessageBox.Show(errMsg);
            }
            else if (currentTabPage.Equals(tabPage_Password))
            {
                string password = oldPwdTB.Text.Trim();
                string newPassword = newPwdTB.Text.Trim();

                if (password == "" || newPassword == "")
                {
                    MessageBox.Show("Password required!");
                    return;
                }

                if (newPassword != retypePwdTB.Text.Trim())
                {
                    MessageBox.Show("Password are not same!");
                    return;
                }

                eCode = CurrentDevice.ModifyPassword(password, newPassword);
                errMsg = ErrorHandling.HandleDeviceError(eCode, CurrentDevice);
                Lable_Message.Text = errMsg;
                MessageBox.Show(errMsg);
            }
            else
            {
                int channelNum = 0;
                if (currentTabPage.Equals(tabPage_Hostlist))
                    channelNum = (int)comboBox_HostChannel.SelectedItem;
                else if (currentTabPage.Equals(tabPage_Serial))
                    channelNum = (int)comboBox_SerialChannel.SelectedItem;
                else if (currentTabPage.Equals(tabPage_Connection))
                    channelNum = (int)comboBox_ConnChannel.SelectedItem;

                eCode = AutoProperty.SetParameter(currentTabPage, channelNum);
                errMsg = ErrorHandling.HandleDeviceError(eCode, CurrentDevice);
                Lable_Message.Text = errMsg;
                MessageBox.Show(errMsg);
            }
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (CurrentDevice.IsLogin == true)
            {
                DevControl.TagErrorCode eCode = CurrentDevice.Logout();
                ErrorHandling.HandleDeviceError(eCode, CurrentDevice);
            }
        }

        private void ReflashSelectedTabPage(TabPage currentTabPage)
        {
            int channelNum = 0;
            string errMsg;
            Lable_Message.Text = "";
            if (currentTabPage.Equals(tabPage_Power))
            {
                //do nothing;
            }
            else if (currentTabPage.Equals(tabPage_Password))
            {
                //do nothing;
            }
            else
            {
                if (currentTabPage.Equals(tabPage_Hostlist))
                    channelNum = (int)comboBox_HostChannel.SelectedItem;
                else if (currentTabPage.Equals(tabPage_Serial))
                    channelNum = (int)comboBox_SerialChannel.SelectedItem;
                else if (currentTabPage.Equals(tabPage_Connection))
                    channelNum = (int)comboBox_ConnChannel.SelectedItem;

                DevControl.TagErrorCode eCode = AutoProperty.GetParameter(currentTabPage, channelNum);
                errMsg = ErrorHandling.HandleDeviceError(eCode, CurrentDevice);
                Lable_Message.Text = errMsg;
            }
        }

        private void SelectedChannel(TabPage currentTabPage)
        {
            ReflashSelectedTabPage(currentTabPage);
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            ReflashSelectedTabPage(tabControl.SelectedTab);
        }

        private void Button_OK_Click(object sender, EventArgs e)
        {
            SubmitSelectedTabPage(tabControl.SelectedTab);
        }

        private void Button_Refresh_Click(object sender, EventArgs e)
        {
            ReflashSelectedTabPage(tabControl.SelectedTab);
        }

        private void ComboBox_Channel_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedChannel((TabPage)((Control)sender).Parent);
        }

        private void NetProtocolCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (netProtocolCB.SelectedItem.ToString() == "TCP")
            {
                panel_UDP.Visible = false;
                panel_TCP.Visible = true;
            }
            else if (netProtocolCB.SelectedItem.ToString() == "UDP")
            {
                panel_TCP.Visible = false;
                panel_UDP.Visible = true;
            }
            else
            {
                Debug.Fail("Invalid!");
            }
        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_IpConfig.SelectedItem.ToString() == "User Config")
            {
                panel_AutoIp.Visible = false;
                panel_StaticIp.Visible = true;
            }
            else if (comboBox_IpConfig.SelectedItem.ToString() == "Obtain Automatically")
            {
                panel_StaticIp.Visible = false;
                panel_AutoIp.Visible = true;
            }
            else
            {
                Debug.Fail("Invalid!");
            }
        }
    }
}