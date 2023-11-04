using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    /// <summary>
    /// Exception handling object, centralized processing of error codes returned when operating DLL
    /// </summary>
    public class ErrorHandling
    {
        private static readonly string[] errorMsg = {
            "Operation Success",
            "Attribute error",
            "No login",
            "Validation fails",
            "socket error",
            "Memory error",
            "Time out",
            "Function parameter error",
            "Request and reply properties do not match",
            "Operation is not supported"
                                           };
        /// <summary>
        /// Get error description information
        /// </summary>
        /// <param name="eCode">tagErrorCode</param>
        /// <returns>error message</returns>
        public static string GetErrorMsg(DevControl.TagErrorCode eCode)
        {
            Debug.Assert((uint)eCode < errorMsg.Length);
            return errorMsg[(uint)eCode];
        }

        /// <summary>
        /// Centralized processing operations return error codes and generate error description information
        /// </summary>
        /// <param name="eCode">tagErrorCode</param>
        /// <returns>error message</returns>
        public static string HandleError(DevControl.TagErrorCode eCode)
        {
            string errorMsg;

            switch (eCode)
            {
                case DevControl.TagErrorCode.DM_ERR_OK:
                case DevControl.TagErrorCode.DM_ERR_PARA:
                case DevControl.TagErrorCode.DM_ERR_AUTHFAIL:
                case DevControl.TagErrorCode.DM_ERR_TIMEOUT:
                case DevControl.TagErrorCode.DM_ERR_OPR:
                    errorMsg = GetErrorMsg(eCode);
                    break;

                case DevControl.TagErrorCode.DM_ERR_NOAUTH:
                    errorMsg = GetErrorMsg(eCode);
                    break;

                case DevControl.TagErrorCode.DM_ERR_SOCKET:
                case DevControl.TagErrorCode.DM_ERR_MEM:
                case DevControl.TagErrorCode.DM_ERR_MATCH:
                case DevControl.TagErrorCode.DM_ERR_ARG:
                    errorMsg = GetErrorMsg(eCode);
                    Log.WriteError(errorMsg);
                    errorMsg = "Software internal error!";
                    break;

                default:
                    errorMsg = "Not support the wrong type!";
                    Debug.Fail(errorMsg);
                    break;
            };

            return errorMsg;
        }

        /// <summary>
        /// Centralized processing operations return error codes and generate error description information
        /// </summary>
        /// <param name="eCode">tagErrorCode</param>
        /// <returns>error message</returns>
        public static string HandleDeviceError(DevControl.TagErrorCode eCode, DeviceClass device)
        {
            if (eCode == DevControl.TagErrorCode.DM_ERR_NOAUTH)
            {
                LoginForm loginform = new LoginForm();
                DialogResult result = loginform.ShowDialog();
                if (result == DialogResult.OK)
                    eCode = device.Login(loginform.UserName, loginform.Password);
            }
            return HandleError(eCode);
        }
    }
}
