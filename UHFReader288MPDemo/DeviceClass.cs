using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace UHFReader288MPDemo
{
    public class DeviceClass
    {
        private readonly int communicationTimeout = 1000;
        private IntPtr _devHandle;
        private uint _devIP;
        private string _devMac;
        private string _devName;
        private bool _isLogin = false;

        public DeviceClass(IntPtr devHandle, uint deviceIP, string deviceMac, string deviceName)
        {
            DevHandle = devHandle;
            DeviceIP = deviceIP;
            DeviceMac = deviceMac;
            DeviceName = deviceName;
        }

        public static uint Broadcast
        {
            get { return 0xffffffff; }
        }

        public IntPtr DevHandle
        {
            get { return _devHandle; }
            set { _devHandle = value; }
        }

        public uint DeviceIP
        {
            get { return _devIP; }
            set { _devIP = value; }
        }

        public string DeviceMac
        {
            get { return _devMac; }
            set { _devMac = value; }
        }

        public string DeviceName
        {
            get { return _devName; }
            set { _devName = value; }
        }

        public bool IsLogin
        {
            get { return _isLogin; }
        }

        public DevControl.TagErrorCode Login(string userName, string password)
        {
            DevControl.TagErrorCode eCode;
            StringBuilder nameBuf = new StringBuilder(userName);
            StringBuilder passwordBuf = new StringBuilder(password);
            eCode = DevControl.DM_AuthLogin(_devHandle, nameBuf, passwordBuf, communicationTimeout);
            _isLogin = (eCode == DevControl.TagErrorCode.DM_ERR_OK);
            return eCode;
        }

        public DevControl.TagErrorCode Logout()
        {
            DevControl.TagErrorCode eCode = DevControl.TagErrorCode.DM_ERR_OK;
            if (_isLogin == true)
            {
                eCode = DevControl.DM_LogOutDevice(_devHandle, communicationTimeout);
                _isLogin = false;
            }

            return eCode;
        }

        public DevControl.TagErrorCode ModifyPassword(string oldPassword, string newPassword)
        {
            StringBuilder passwordBuf = new StringBuilder(oldPassword);
            StringBuilder newPasswordBuf = new StringBuilder(newPassword);
            return DevControl.DM_ModifyPassword(_devHandle, passwordBuf, newPasswordBuf, communicationTimeout);
        }

        public DevControl.TagErrorCode RebootManage(RebootType rebootType)
        {
            DevControl.TagErrorCode eCode;

            switch (rebootType)
            {
                case RebootType.DefaultWithoutReboot:
                    eCode = DevControl.DM_LoadDefault(_devHandle, communicationTimeout);
                    break;

                case RebootType.DefaultAndReboot:
                    eCode = DevControl.DM_LoadDefault(_devHandle, communicationTimeout);
                    if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                    {
                        eCode = DevControl.DM_ResetDevice(_devHandle, communicationTimeout);
                    }
                    break;

                case RebootType.RebootWithoutSave:
                    eCode = DevControl.DM_ResetDeviceWithoutSave(_devHandle, communicationTimeout);
                    break;

                case RebootType.SaveAndReboot:
                    eCode = DevControl.DM_ResetDevice(_devHandle, communicationTimeout);
                    break;

                default:
                    eCode = DevControl.TagErrorCode.DM_ERR_ARG;
                    Debug.Fail("Not Support this RebootType!");
                    break;
            }
            return eCode;
        }

        public bool IsSupportChannel(int channelNum)
        {
            return DevControl.DM_IsComEnable(_devHandle, channelNum);
        }
    }

    public enum RebootType
    {
        /// <summary>
        /// Only restore default parameters and do not restart the device
        /// </summary>
        DefaultWithoutReboot,
        /// <summary>
        /// Restart the device and restore default parameters
        /// </summary>
        DefaultAndReboot,
        /// <summary>
        /// Restart the device without saving unsaved parameters
        /// </summary>
        RebootWithoutSave,
        /// <summary>
        /// Restart the device and save parameters
        /// </summary>
        SaveAndReboot
    };
}
