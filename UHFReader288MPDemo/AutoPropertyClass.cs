using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace UHFReader288MPDemo
{
    class AutoPropertyClass
    {
        private readonly IntPtr _devHandle;
        private readonly PropertyParaMapClass _propertyParaMap;

        public AutoPropertyClass(IntPtr devHandle)
        {
            _devHandle = devHandle;
            _propertyParaMap = new PropertyParaMapClass();
        }

        public bool AddPropertyParaMap(string propertyName, DevControl.PARA_TYPES paraType)
        {
            return _propertyParaMap.AddPropertyParaMap(propertyName, paraType);
        }

        private DevControl.TagErrorCode GetParameterFromProperty(Control ownerPage, int channelNum, IntPtr paraList)
        {
            IntPtr setParaList = paraList;
            DevControl.TagErrorCode eCode = DevControl.TagErrorCode.DM_ERR_OK;

            Debug.Assert(ownerPage != null);
            Debug.Assert(paraList != IntPtr.Zero);

            foreach (Control control in ownerPage.Controls)
            {
                if (!control.Enabled)
                    continue;

                if (control.Controls.Count != 0)
                {
                    eCode = GetParameterFromProperty(control, channelNum, paraList);
                    if (eCode != DevControl.TagErrorCode.DM_ERR_OK)
                        break;
                }

                if (control.AccessibleName != null)
                {
                    DevControl.PARA_TYPES paraType = DevControl.PARA_TYPES.END_OF_PARA_TYPES;
                    if (_propertyParaMap.FindParaByProperty(control.AccessibleName, ref paraType))
                    {
                        StringBuilder bufferString = new StringBuilder();

                        Type controlType = control.GetType();
                        if (controlType == typeof(TextBox))
                        {
                            if (((TextBox)control).ReadOnly == true)
                                continue;

                            bufferString.Append(((TextBox)control).Text.Trim());
                        }
                        else if (controlType == typeof(CheckBox))
                        {
                            if (((CheckBox)control).Checked)
                                bufferString.Append('1');
                            else
                                bufferString.Append('0');
                        }
                        else if (controlType == typeof(ComboBox))
                            bufferString.Append(((ComboBox)control).SelectedIndex.ToString());
                        else if (controlType == typeof(NumericUpDown))
                            bufferString.Append(((NumericUpDown)control).Value.ToString());
                        else
                        {
                            Debug.Fail("must support new controlType!");
                            continue;
                        }

                        // Convert the read string format input parameter value to the operation
                        // interface input type, and then check the legality of the input parameter.
                        // If it is legal, add it to the parameter list;
                        byte[] valueBuf = new byte[100];
                        int valueLen = valueBuf.Length;
                        eCode = DevControl.DM_String2Value(paraType, bufferString, bufferString.Length, valueBuf, ref valueLen);
                        if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                        {
                            eCode = DevControl.DM_CheckPara(_devHandle, channelNum, paraType, valueLen, valueBuf);
                            if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                                DevControl.paralist_addnode(setParaList, channelNum, paraType, valueLen, valueBuf);
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
            }

            return eCode;
        }

        private bool SetParameterToProperty(IntPtr paralist, Control ownerPage, int channelNum)
        {
            if (paralist == IntPtr.Zero)
                return false;

            foreach (Control control in ownerPage.Controls)
            {
                if (control.Controls.Count != 0)
                    SetParameterToProperty(paralist, control, channelNum);

                if (control.AccessibleName != null)
                {
                    bool isFind;

                    DevControl.PARA_TYPES paraType = DevControl.PARA_TYPES.END_OF_PARA_TYPES;
                    isFind = _propertyParaMap.FindParaByProperty(control.AccessibleName, ref paraType);
                    if (isFind == true)
                    {
                        byte[] valueBuf = new byte[100];
                        int valueLen = valueBuf.Length;

                        DevControl.TagErrorCode eCode = DevControl.paralist_getnode(paralist, channelNum, paraType, ref valueLen, valueBuf);
                        if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
                        {
                            if (valueLen == 0)
                            {
                                control.Enabled = false;
                                continue;
                            }

                            StringBuilder bufferString = new StringBuilder(100);
                            int stringLen = bufferString.Capacity;
                            eCode = DevControl.DM_Value2String(paraType, valueBuf, valueLen, bufferString, ref stringLen);
                            Debug.Assert(eCode == DevControl.TagErrorCode.DM_ERR_OK);

                            Type controlType = control.GetType();
                            if (controlType == typeof(TextBox))
                            {
                                ((TextBox)control).Text = bufferString.ToString();
                            }
                            else if (controlType == typeof(CheckBox))
                            {
                                if (bufferString[0] == '1')
                                    ((CheckBox)control).Checked = true;
                                else if (bufferString[0] == '0')
                                    ((CheckBox)control).Checked = false;
                                else
                                    Debug.Fail("out of range!");
                            }
                            else if (controlType == typeof(ComboBox))
                            {
                                if (valueBuf[0] < ((ComboBox)control).Items.Count)
                                    ((ComboBox)control).SelectedIndex = int.Parse(bufferString.ToString());
                                else
                                    Debug.Fail("out of range!");
                            }
                            else if (controlType == typeof(NumericUpDown))
                            {
                                ((NumericUpDown)(control)).Value = decimal.Parse(bufferString.ToString());
                            }
                            else
                            {
                                Debug.Fail("must support new controlType!");
                                continue;
                            }
                        }
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// According to the corresponding mapping control in the container control,
        /// request the corresponding configured parameters from the remote device
        /// </summary>
        /// <param name="ownerPage">Container control</param>
        /// <param name="channelNum">The channel number to which the parameter belongs</param>
        /// <param name="paraList">Request parameter list</param>
        public void GetQueryParaList(Control ownerPage, int channelNum, IntPtr paraList)
        {
            foreach (Control control in ownerPage.Controls)
            {
                if ((control.Enabled == false))
                    continue;

                if (control.Controls.Count != 0)
                    GetQueryParaList(control, channelNum, paraList);

                if (control.AccessibleName != null)
                {
                    DevControl.PARA_TYPES paraType = DevControl.PARA_TYPES.END_OF_PARA_TYPES;
                    if (_propertyParaMap.FindParaByProperty(control.AccessibleName, ref paraType))
                        DevControl.ParaList_addnode(paraList, channelNum, paraType);
                }
            }
        }

        /// <summary>
        /// According to the corresponding mapping control in the container control,
        /// obtain parameters from the device and fill in the corresponding mapping control
        /// </summary>
        /// <param name="ownerPage">Container control</param>
        /// <param name="channelNum">The channel number to which the parameter belongs</param>
        /// <returns>tagErrorCode</returns>
        public DevControl.TagErrorCode GetParameter(Control ownerPage, int channelNum)
        {
            IntPtr getParaList;
            // Get its current configuration parameters from the remote device;
            getParaList = DevControl.paralistCreate(_devHandle);
            if (getParaList == IntPtr.Zero)
                return DevControl.TagErrorCode.DM_ERR_MEM;

            GetQueryParaList(ownerPage, channelNum, getParaList);

            DevControl.TagErrorCode errCode = DevControl.DM_GetPara(_devHandle, getParaList, 1000);
            if (errCode == DevControl.TagErrorCode.DM_ERR_OK)
                SetParameterToProperty(getParaList, ownerPage, channelNum);

            DevControl.paralistDestroy(getParaList);

            return errCode;
        }

        /// <summary>
        /// Configure local configuration parameters to the remote device
        /// according to the corresponding mapping control in the container control
        /// </summary>
        /// <param name="ownerPage">Container control</param>
        /// <param name="channelNum">The channel number to which the parameter belongs</param>
        /// <returns>tagErrorCode</returns>
        public DevControl.TagErrorCode SetParameter(Control ownerPage, int channelNum)
        {
            IntPtr setParaList;
            DevControl.TagErrorCode eCode;

            // Set configuration parameters to the corresponding remote device
            setParaList = DevControl.paralistCreate(_devHandle);
            if (setParaList == IntPtr.Zero)
                return DevControl.TagErrorCode.DM_ERR_MEM;

            eCode = GetParameterFromProperty(ownerPage, channelNum, setParaList);
            if (eCode == DevControl.TagErrorCode.DM_ERR_OK)
            {
                Debug.Assert(setParaList != IntPtr.Zero);
                eCode = DevControl.DM_SetPara(_devHandle, setParaList, 1000);
                DevControl.paralistDestroy(setParaList);
            }
            return eCode;
        }
    }

    class PropertyParaMapClass
    {
        private readonly string[] propertyParaMap;

        public PropertyParaMapClass()
        {
            int maxPara = (int)DevControl.PARA_TYPES.END_OF_PARA_TYPES;
            propertyParaMap = new string[maxPara];
        }

        /// <summary>
        /// Add the mapping relationship between controls and PARA_TYPES
        /// </summary>
        /// <param name="propertyName">控件标识；</param>
        /// <param name="paraType">DevControl.PARA_TYPES</param>
        /// <returns>added successfully</returns>
        public bool AddPropertyParaMap(string propertyName, DevControl.PARA_TYPES paraType)
        {
            int mapIndex = (int)paraType;

            Debug.Assert(mapIndex < propertyParaMap.Length);
            Debug.Assert(propertyParaMap[mapIndex] == null);

            propertyParaMap[mapIndex] = propertyName;

            return true;
        }

        /// <summary>
        /// Use the control identifier to find its corresponding PARA_TYPES
        /// </summary>
        /// <param name="propertyName">Control ID</param>
        /// <param name="paraType">DevControl.PARA_TYPES</param>
        /// <returns>added successfully</returns>
        public bool FindParaByProperty(string propertyName, ref DevControl.PARA_TYPES paraType)
        {
            int index = 0;
            foreach (string name in propertyParaMap)
            {
                if (propertyName.Equals(name))
                {
                    paraType = (DevControl.PARA_TYPES)index;
                    return true;
                }
                index++;
            }
            return false;
        }

        /// <summary>
        /// Use PARA_TYPES to find the corresponding control identifier
        /// </summary>
        /// <param name="paraType">DevControl.PARA_TYPES</param>
        /// <returns>Corresponding control ID</returns>
        public string FindPropertyByPara(DevControl.PARA_TYPES paraType)
        {
            return propertyParaMap[(int)paraType];
        }
    }
}
