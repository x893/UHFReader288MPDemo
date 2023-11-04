using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UHFReader288MPDemo
{
    internal delegate void SearchCallBack(IntPtr dev, IntPtr data);

    public class DevControl
    {
        private const string DLL_NAME = "dmdll.dll";

        public enum TagErrorCode
        {
            DM_ERR_OK,			/* no error */
            DM_ERR_PARA,		/* parameter error */
            DM_ERR_NOAUTH,		/* */
            DM_ERR_AUTHFAIL,	/* auth fail */
            DM_ERR_SOCKET,		/* socket error */
            DM_ERR_MEM,			/* */
            DM_ERR_TIMEOUT,
            DM_ERR_ARG,
            DM_ERR_MATCH,		/* parameters in command and reply are not match */
            DM_ERR_OPR,
            DM_ERR_MAX
        };

        internal enum DataType
        {
            PARA_TYPE_STRING,
            PARA_TYPE_UCHAR,
            PARA_TYPE_USHORT,
            PARA_TYPE_ULONG,
            PARA_TYPE_UCHAR_HEX,
            PARA_TYPE_INVALID
        };
        /// <summary>
        /// Initialize the system
        /// </summary>
        /// <param name="searchCB">The callback function after the device is found</param>
        /// <returns>Is initialization successful?</returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_Init(SearchCallBack searchCB, IntPtr data);

        /// <summary>
        /// Recycling system
        /// </summary>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_DeInit();

        /// <summary>
        /// Search for devices on the network. The searched devices will be returned through the search callback function SearchCallBack.
        /// </summary>
        /// <param name="deviceIP">IP to search, if searching all, please use 255.255.255.255</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_SearchDevice(uint deviceIP, int timeout);

        /// <summary>
        /// Returns basic information of the device, mainly used in search callbacks
        /// </summary>
        /// <param name="devhandle">Internal handle</param>
        /// <param name="ipaddr">IP address</param>
        /// <param name="macaddr">MAC address</param>
        /// <param name="devname">Device name</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_GetDeviceInfo(IntPtr devhandle, ref uint ipaddr, StringBuilder macaddr, StringBuilder devname);

        /// <summary>
        /// Log into device
        /// </summary>
        /// <param name="devHandle">Device internal handle</param>
        /// <param name="name">username</param>
        /// <param name="password">password</param>
        /// <param name="timeout">overtime time</param>
        /// <returns>Login results</returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_AuthLogin(IntPtr devHandle, StringBuilder name, StringBuilder password, int timeout);

        /// <summary>
        /// Change device password
        /// </summary>
        /// <param name="devHandle">Device internal handle</param>
        /// <param name="oldPassword">The user's current password</param>
        /// <param name="newPassword">User's new password</param>
        /// <param name="timeout">overtime time</param>
        /// <returns>Login results</returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_ModifyPassword(IntPtr devHandle, StringBuilder oldPassword, StringBuilder newPassword, int timeout);

        /// <summary>
        /// Create a list of acquisition and configuration parameters
        /// </summary>
        /// <param name="devHandle">Internal handle</param>
        /// <returns>If creation fails, return empty</returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr paralistCreate(IntPtr devHandle);

        /// <summary>
        /// Destroy a parameter list
        /// </summary>
        /// <param name="list">The pointer returned by the create function</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode paralistDestroy(IntPtr list);

        /// <summary>
        /// Add a parameter to the parameter list
        /// </summary>
        /// <param name="list">list pointer</param>
        /// <param name="chanNo">Channel number</param>
        /// <param name="paraType">Parameter Type</param>
        /// <param name="valueLen">Configured data length</param>
        /// <param name="value">Configuration data</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode paralist_addnode(IntPtr list, int chanNo, PARA_TYPES paraType, int valueLen, byte[] value);

        /// <summary>
        /// Add a parameter to the parameter list
        /// </summary>
        /// <param name="list">list pointer</param>
        /// <param name="chanNo">Channel number</param>
        /// <param name="paraType">Parameter Type</param>
        /// <returns></returns>
        internal static TagErrorCode ParaList_addnode(IntPtr list, int chanNo, PARA_TYPES paraType)
        {
            return paralist_addnode(list, chanNo, paraType, 0, null);
        }

        /// <summary>
        /// Destroy a parameter in the parameter list
        /// </summary>
        /// <param name="list">list pointer</param>
        /// <param name="chanNo">Channel number</param>
        /// <param name="paraType">Parameter Type</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode paralist_delnode(IntPtr list, int chanNo, PARA_TYPES paraType);

        /// <summary>
        /// Query and get results from the get parameter list
        /// </summary>
        /// <param name="list">list</param>
        /// <param name="chanNo">Channel number</param>
        /// <param name="paraType">Parameter Type</param>
        /// <param name="valueType">type of data</param>
        /// <param name="valueLen">Data length</param>
        /// <param name="value">Data value</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode paralist_getnode(IntPtr list, int chanNo, PARA_TYPES paraType, ref int valueLen, byte[] value);

        /// <summary>
        /// Query the obtained parameter list to obtain the results, and the results are all strings
        /// </summary>
        /// <param name="paraType">Parameter Type</param>
        /// <param name="valueType">Data buffer</param>
        /// <param name="valueLen">Data length</param>
        /// <param name="value">String data buffer</param>
        /// /// <param name="valueLen">String data length</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_Value2String(PARA_TYPES eParaType, byte[] bufferValue, int nValueLen, StringBuilder bufferString, ref int nStringLen);

        /// <summary>
        /// Query the obtained parameter list to obtain the results, and the results are all strings
        /// </summary>
        /// <param name="paraType">Parameter Type</param>
        /// <param name="valueType">Data buffer</param>
        /// <param name="valueLen">Data length</param>
        /// <param name="value">String data buffer</param>
        /// /// <param name="valueLen">String data length</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_String2Value(PARA_TYPES eParaType, StringBuilder bufferString, int nStringLen, byte[] bufferValue, ref int nValueLen);

        /// <summary>
        /// Verify that parameters are valid
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="chanNo">Channel number</param>
        /// <param name="paraType">Parameter Type</param>
        /// <param name="valuelen">Parameter length</param>
        /// <param name="value">Parameter value</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_CheckPara(IntPtr devHandle, int chanNo, PARA_TYPES paraType, int valuelen, byte[] value);

        /// <summary>
        /// Get the parameters in the list from the device
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="list">list</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_GetPara(IntPtr devHandle, IntPtr list, int timeout);

        /// <summary>
        /// Configure the parameters in the device list
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="list">list</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_SetPara(IntPtr devHandle, IntPtr list, int timeout);

        /// <summary>
        /// Restart the device and save the current setting parameters
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_ResetDevice(IntPtr devHandle, int timeout);

        /// <summary>
        /// Restart the device without saving the current setting parameters
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_ResetDeviceWithoutSave(IntPtr devHandle, int timeout);

        /// <summary>
        /// Restore the device's default parameters without saving or restarting the device
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="timeout">overtime time</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_LoadDefault(IntPtr devHandle, int timeout);

        /// <summary>
        /// Log out of device
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_LogOutDevice(IntPtr devHandle, int timeout);

        /// <summary>
        /// Release search to device
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern TagErrorCode DM_FreeDevice(IntPtr devHandle);

        /// <summary>
        /// Check whether the device supports the specified COM
        /// </summary>
        /// <param name="devHandle">Device handle</param>
        /// <param name="comNum">COM number</param>
        /// <returns>BOOLEAN</returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool DM_IsComEnable(IntPtr devHandle, int comNum);

        internal enum PARA_TYPES
        {
            BAUDRATE,
            DATABITS,
            STOPBITS,
            PARITY,
            FLOWCONTROL,
            FIFO,
            ENABLEPACKING,
            IDLEGAPTIME,
            MATCH2BYTESEQUENCE,
            SENDFRAMEONLY,
            SENDTRAILINGBYTES,
            INPUTWITHACTIVECONNECT,
            OUTPUTWITHACTIVECONNECT,
            INPUTWITHPASSIVECONNECT,
            OUTPUTWITHPASSIVECONNECT,
            INPUTATTIMEOFDISCONNECT,
            OUTPUTATTIMEOFDISCONNECT,
            IPCONFIGURATION,				/* */
            AUTONEGOTIATE,
            MACADDRESS,						/* ethernet interface mac address */
            SPEED,
            DUPLEX,
            NETPROTOCOL,
            ACCEPTINCOMING,
            ARPCACHETIMEOUT,
            TCPKEEPACTIVE,
            CPUPERFORMANCEMODE,
            HTTPSERVERPORT,
            MTUSIZE,
            RETRYCOUNTER,
            IPADDRESS,						/* ethernet interface ip address(static) */
            FIRMWARE,
            UPTIME,
            SERIALNO,
            RETRYTIMEOUT,
            HOSTLIST1_IP,
            HOSTLIST1_PORT,
            HOSTLIST2_IP,
            HOSTLIST2_PORT,
            HOSTLIST3_IP,
            HOSTLIST3_PORT,
            HOSTLIST4_IP,
            HOSTLIST4_PORT,
            HOSTLIST5_IP,
            HOSTLIST5_PORT,
            HOSTLIST6_IP,
            HOSTLIST6_PORT,
            HOSTLIST7_IP,
            HOSTLIST7_PORT,
            HOSTLIST8_IP,
            HOSTLIST8_PORT,
            HOSTLIST9_IP,
            HOSTLIST9_PORT,
            HOSTLIST10_IP,
            HOSTLIST10_PORT,
            HOSTLIST11_IP,
            HOSTLIST11_PORT,
            HOSTLIST12_IP,
            HOSTLIST12_PORT,
            FIRSTMATCHBYTE,
            LASTMATCHBYTE,
            DATAGRAMTYPE,
            DEVICEADDRESSTABLE1_BEGINIP,
            UDPLOCALPORT,
            UDPREMOTEPORT,
            UDPNETSEGMENT,
            DEVICEADDRESSTABLE2_BEGINIP,
            DEVICEADDRESSTABLE2_PORT,
            DEVICEADDRESSTABLE2_ENDIP,
            DEVICEADDRESSTABLE3_BEGINIP,
            DEVICEADDRESSTABLE3_PORT,
            DEVICEADDRESSTABLE3_ENDIP,
            DEVICEADDRESSTABLE4_BEGINIP,
            DEVICEADDRESSTABLE4_PORT,
            DEVICEADDRESSTABLE4_ENDIP,
            UDPUNICASTLOCALPORT,
            ACCEPTIONINCOMING,
            ACTIVECONNECT,
            STARTCHARACTER,
            ONDSRDROP,
            HARDDISCONNECT,
            CHECKEOT,
            INACTIVITYTIMEOUT_M,
            INACTIVITYTIMEOUT_S,
            LOCALPORT,
            REMOTEHOST,
            REMOTEPORT,
            DNSQUERYPERIOD,
            DEVICEADDRESSTABLE1_ENDIP,
            DEVICEADDRESSTABLE1_PORT,
            CONNECTRESPONSE,
            TERMINALNAME,
            USEHOSTLIST,
            EMAILADDRESS,
            EMAILUSERNAME,
            EMAILPASSWORD,
            EMAILINPUTTRIGGERMESSAGE,
            EMAILADDRESS1,
            EMAILADDRESS2,
            EMAILADDRESS3,
            POP3DOMAINNAME,
            SMTPDOMAINNAME,
            POP3PORT,
            SMTPPORT,
            COLDSTART,
            DCDCHANGED,
            DSRCHANGED,
            WARMSTART,
            AUTHENTICATIONFAILURE,
            IPADDRESSCHANGED,
            ENABLESERIALTRIGGERINPUT,
            SERIALCHANNEL,
            SERIALDATASIZE,
            SERIALMATCHDATA1,
            SERIALMATCHDATA2,
            EMAILTRIGGERSUBJECT,
            PRIORITY,
            INPUTPRIORITY,
            INPUTMINNOTIFICATIONINTERVAL,
            MINNOTIFICATIONINTERVAL,
            RENOTIFICATIONINTERVAL,
            NEWUSERPSW,
            BOOTP,
            DHCP,
            AUTOIP,
            DHCPHOSTNAME,
            SUBNET,
            DEFAULTGATEWAY,
            DEVICENAME,				/* device name, server name */
            TIMEZONE,
            LOCALTIME_YEAR,
            LOCALTIME_MONTH,
            LOCALTIME_DAY,
            LOCALTIME_HOUR,
            LOCALTIME_MINUTE,
            LOCALTIME_SECOND,
            TIMESERVER,
            WEBCONSOLE,
            TELNETCONSOLE,
            PASSWORDCHANGED,
            SERIALPORTOPTIONS,
            PREFERREDDNSSERVER,
            ALTERNATEDNSSERVER,
            SERIALMATCHDATA3,
            INPUT1,
            INPUT2,
            IO1,
            IO2,
            IO1TYPE,
            IO2TYPE,
            IO1STATE,
            IO2STATE,
            SERIALPORTPROTOCOL,
            FIRMWAREID,
            PPPOEUSERNAME,
            PPPOEPASSWORD,
            PPPOEWORKMODE,
            PPPOEMAXREDIALTIMES,
            PPPOEREDIALINTERVAL,
            PPPOEIDLETIME,
            PPPOESTATUS,
            PPPOEIP,
            PPPOEGATEWAY,
            PPPOEDNS1,
            PPPOEDNS2,
            ENABLEBACKUPLINK,

            END_OF_PARA_TYPES
        };

        internal static TagErrorCode GetParaStringValue(IntPtr paraList, int chanNo, PARA_TYPES paraType, ref int valueLen, StringBuilder value)
        {
            byte[] bufferValue = new byte[100];
            int getLen = bufferValue.Length;
            TagErrorCode errCode;
            errCode = paralist_getnode(paraList, chanNo, paraType, ref getLen, bufferValue);
            if (errCode == TagErrorCode.DM_ERR_OK)
                errCode = DM_Value2String(paraType, bufferValue, getLen, value, ref valueLen);
            return errCode;
        }
    }
}

