﻿using IoTClient.Clients.Modbus;
using IoTClient.Common.Helpers;
using IoTClient.Enums;
using IoTClient.Models;
using IoTClient.Tool.Common;
using IoTServer.Common;
using IoTServer.Servers.Modbus;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoTClient.Tool.Controls
{
    public partial class ModbusRtuControl : UserControl
    {
        private IModbusClient client;
        private ModbusRtuServer server;

        int[] BaudRateList = new int[] { 9600, 4800, 2400, 1200, 600, 14400, 300, 19200, 110, 38400, 56000, 57600, 115200, 128000, 256000 };
        int[] DataBitList = new int[] { 8, 7, 6 };
        StopBits[] StopBitsList = new StopBits[] { StopBits.One, StopBits.Two, StopBits.OnePointFive };
        Parity[] ParityList = new Parity[] { Parity.None, Parity.Odd, Parity.Even };

        public ModbusRtuControl()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            Size = new Size(880, 450);
            groupBox2.Location = new Point(13, 5);
            groupBox2.Size = new Size(855, 50);
            groupBox1.Location = new Point(13, 55);
            groupBox1.Size = new Size(855, 50);
            groupBox3.Location = new Point(13, 105);
            groupBox3.Size = new Size(855, 50);
            txt_content.Location = new Point(13, 160);

            lab_address.Location = new Point(9, 22);
            txt_address.Location = new Point(39, 18);
            txt_address.Size = new Size(88, 21);
            but_read.Location = new Point(132, 17);

            but_brokenline.Location = new Point(209, 17);

            lab_value.Location = new Point(297, 22);
            txt_value.Location = new Point(319, 18);
            txt_value.Size = new Size(74, 21);
            but_write.Location = new Point(398, 17);

            txt_dataPackage.Location = new Point(490, 18);
            txt_dataPackage.Size = new Size(186, 21);
            but_sendData.Location = new Point(680, 17);

            chb_show_package.Location = new Point(776, 20);
            cmb_EndianFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_EndianFormat.SelectedIndex = 0;

            but_read.Enabled = false;
            but_brokenline.Enabled = false;
            but_write.Enabled = false;
            but_server_close.Enabled = false;
            but_close.Enabled = false;
            but_sendData.Enabled = false;
            UpdatePortNames();
            cb_portNameSend.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_portNameSend_server.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_parity.SelectedIndex = 0;
            cb_parity.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_baudRate.SelectedIndex = 2;
            toolTip1.SetToolTip(but_open, "按住Ctrl后连接将自动扫描串口连接参数");
            toolTip1.SetToolTip(txt_address, "支持批量读取，如4-3将会读取4、5、6地址对应的数据");
            txt_content.Text = @"小技巧:
1、按住Ctrl后点击连接将自动扫描串口连接参数组合
2、读取地址支持批量读取，如4-3将会读取4、5、6地址对应的数据
3、读取地址支持批量读取，如4、5、6、8、12";


            var config = ConnectionConfig.GetConfig();

            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_Address)) txt_address.Text = config.ModBusRtu_Address;
            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_Value)) txt_value.Text = config.ModBusRtu_Value;
            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_StationNumber)) txt_stationNumber.Text = config.ModBusRtu_StationNumber;
            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_PortName)) cb_portNameSend.SelectedItem = config.ModBusRtu_PortName;
            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_BaudRate)) cb_baudRate.SelectedItem = config.ModBusRtu_BaudRate;
            if (!string.IsNullOrWhiteSpace(config.ModBusRtu_DataBits)) txt_dataBit.Text = config.ModBusRtu_DataBits;
            txt_stopBit.Text = ((int)config.ModBusRtu_StopBits).ToString();
            cb_parity.SelectedIndex = (int)config.ModBusRtu_Parity;
            cmb_EndianFormat.SelectedItem = config.ModBusRtu_EndianFormat.ToString();
            switch (config.ModBusRtu_Datatype)
            {
                case "rd_coil": rd_coil.Checked = true; break;
                case "rd_discrete": rd_discrete.Checked = true; break;
                case "rd_short": rd_short.Checked = true; break;
                case "rd_ushort": rd_ushort.Checked = true; break;
                case "rd_int": rd_int.Checked = true; break;
                case "rd_uint": rd_uint.Checked = true; break;
                case "rd_long": rd_long.Checked = true; break;
                case "rd_ulong": rd_ulong.Checked = true; break;
                case "rd_float": rd_float.Checked = true; break;
                case "rd_double": rd_double.Checked = true; break;
            };
            chb_show_package.Checked = config.ModBusRtu_ShowPackage;
        }

        /// <summary>
        /// 更新串口名
        /// </summary>
        public void UpdatePortNames()
        {
            cb_portNameSend.DataSource = ModbusRtuClient.GetPortNames();
            cb_portNameSend_server.DataSource = ModbusRtuClient.GetPortNames();
        }


        /// <summary>
        /// 打开连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_open_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_content.Text.Contains("小技巧")) txt_content.Text = string.Empty;
                var PortName = cb_portNameSend.Text.ToString();
                var BaudRate = int.Parse(cb_baudRate.Text.ToString());
                var DataBits = int.Parse(txt_dataBit.Text.ToString());
                var StopBits = (StopBits)int.Parse(txt_stopBit.Text.ToString());
                var parity = cb_parity.SelectedIndex == 0 ? Parity.None : (cb_parity.SelectedIndex == 1 ? Parity.Odd : Parity.Even);
                client?.Close();
                EndianFormat format = EndianFormat.ABCD;
                switch (cmb_EndianFormat.SelectedIndex)
                {
                    case 0:
                        format = EndianFormat.ABCD;
                        break;
                    case 1:
                        format = EndianFormat.BADC;
                        break;
                    case 2:
                        format = EndianFormat.CDAB;
                        break;
                    case 3:
                        format = EndianFormat.DCBA;
                        break;
                }
                client = new ModbusRtuClient(PortName, BaudRate, DataBits, StopBits, parity, format: format);
                var result = client.Open();
                if (result.IsSucceed)
                {
                    but_open.Enabled = false;
                    cb_portNameSend.Enabled = false;
                    but_read.Enabled = true;
                    but_brokenline.Enabled = true;
                    but_write.Enabled = true;
                    but_open.Enabled = false;
                    but_close.Enabled = true;
                    but_sendData.Enabled = true;

                    //按了Ctrl后的鼠标点击
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        Task.Run(() =>
                        {
                            AutoOpenRead();
                        });
                    }
                    else
                    {
                        AppendText($"连接成功\t\t\t\t耗时：{result.TimeConsuming}ms");
                        ControlEnabledFalse();
                    }
                }
                else
                    AppendText($"连接失败：{result.Err}");

                var config = ConnectionConfig.GetConfig();
                config.ModBusRtu_PortName = PortName;
                config.ModBusRtu_BaudRate = BaudRate.ToString();
                config.ModBusRtu_DataBits = DataBits.ToString();
                config.ModBusRtu_StopBits = StopBits;
                config.ModBusRtu_Parity = parity;
                config.ModBusRtu_Value = txt_value.Text;
                config.ModBusRtu_Address = txt_address.Text;
                config.ModBusRtu_ShowPackage = chb_show_package.Checked;
                config.ModBusRtu_EndianFormat = format;                
                config.SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ControlEnabledFalse()
        {
            cmb_EndianFormat.Enabled = false;
            cb_portNameSend.Enabled = false;
            cb_baudRate.Enabled = false;
            txt_dataBit.Enabled = false;
            txt_stopBit.Enabled = false;
            cb_parity.Enabled = false;
            txt_stationNumber.Enabled = false;
        }

        private void ControlEnabledTrue()
        {
            cmb_EndianFormat.Enabled = true;
            cb_portNameSend.Enabled = true;
            cb_baudRate.Enabled = true;
            txt_dataBit.Enabled = true;
            txt_stopBit.Enabled = true;
            cb_parity.Enabled = true;
            txt_stationNumber.Enabled = true;
        }

        /// <summary>
        /// 自动扫描可连接串口属性
        /// </summary>
        /// <returns></returns>
        private void AutoOpenRead()
        {
            #region 当前选择的配置有效匹配
            FirstItem(ref BaudRateList, int.Parse(cb_baudRate.Text.ToString()));
            FirstItem(ref DataBitList, int.Parse(txt_dataBit.Text.ToString()));
            FirstItem(ref StopBitsList, (StopBits)int.Parse(txt_stopBit.Text.ToString()));
            var firstParity = cb_parity.SelectedIndex == 0 ? Parity.None : (cb_parity.SelectedIndex == 1 ? Parity.Odd : Parity.Even);
            FirstItem(ref ParityList, firstParity);
            #endregion
            byte.TryParse(txt_stationNumber.Text?.Trim(), out byte stationNumber);

            if (!client.ReadInt16("0", stationNumber).IsSucceed)
            {
                foreach (var baudRate in BaudRateList)
                {
                    foreach (var dataBit in DataBitList)
                    {
                        foreach (var stopBits in StopBitsList)
                        {
                            foreach (var parity in ParityList)
                            {
                                for (byte i = 0; i < 255; i++)
                                {
                                    stationNumber = (byte)(i + 1);
                                    if (but_open.Enabled) return;
                                    client?.Close();
                                    client = new ModbusRtuClient(cb_portNameSend.Text.ToString(), baudRate, dataBit, stopBits, parity);
                                    var result = client.Open();
                                    if (result.IsSucceed)
                                    {
                                        if (client.ReadInt16("0", stationNumber).IsSucceed)
                                        {
                                            AppendText($@"连接【成功】 端口:{cb_portNameSend.Text.ToString()} 波特率:{baudRate} 数据位:{dataBit} 停止位:{stopBits} 奇偶:{parity} 站号:{stationNumber}");
                                            return;
                                        }
                                        else
                                        {
                                            AppendText($@"连接失败 端口:{cb_portNameSend.Text.ToString()} 波特率:{baudRate} 数据位:{dataBit} 停止位:{stopBits} 奇偶:{parity} 站号:{stationNumber}");
                                        }
                                    }
                                    else
                                    {
                                        AppendText($"连接异常 端口:{cb_portNameSend.Text.ToString()} 波特率:{baudRate} 数据位:{dataBit} 停止位:{stopBits} 奇偶:{parity} 站号:{stationNumber} Err:{result.Err}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                AppendText($@"连接【成功】 端口:{cb_portNameSend.Text} 波特率:{cb_baudRate.Text} 数据位:{txt_dataBit.Text} 停止位:{txt_stopBit.Text} 奇偶:{cb_parity.Text} 站号:{stationNumber}");
            }
        }

        /// <summary>
        /// 移动项到数组第一个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="item"></param>
        private void FirstItem<T>(ref T[] list, T item)
        {
            if (list.Contains(item))
            {
                var temp = list.ToList();
                temp.Remove(item);
                temp.Insert(0, item);
                list = temp.ToArray();
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butClose_Click(object sender, EventArgs e)
        {
            client?.Close();
            AppendText("关闭连接");
            but_open.Enabled = true;
            but_close.Enabled = false;
            cb_portNameSend.Enabled = true;
            but_sendData.Enabled = false;
            ControlEnabledTrue();
        }

        /// <summary>
        /// 读数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_read_Click(object sender, EventArgs e)
        {
            byte.TryParse(txt_stationNumber.Text?.Trim(), out byte stationNumber);
            if (string.IsNullOrWhiteSpace(txt_address.Text))
            {
                MessageBox.Show("请输入地址");
                return;
            }
            dynamic result = null;
            try
            {
                var addressAndReadLength = txt_address.Text.Split('-');
                var addressAndReadNumber = txt_address.Text.Split(',', '、', '，');
                //批量读取
                if (addressAndReadLength.Length == 2)
                {
                    var address = int.Parse(addressAndReadLength[0]);
                    var readNumber = ushort.Parse(addressAndReadLength[1]);
                    ushort bLength = 1;
                    if (rd_coil.Checked || rd_discrete.Checked || rd_short.Checked || rd_ushort.Checked)
                        bLength = 1;
                    else if (rd_int.Checked || rd_uint.Checked || rd_float.Checked)
                        bLength = 2;
                    else if (rd_long.Checked || rd_ulong.Checked || rd_double.Checked)
                        bLength = 4;

                    var readLength = Convert.ToUInt16(bLength * readNumber);
                    byte functionCode;
                    if (rd_coil.Checked) functionCode = 1;
                    else if (rd_discrete.Checked) functionCode = 2;
                    else functionCode = 3;

                    result = client.Read(address.ToString(), stationNumber, functionCode, readLength: readLength, false);

                    if (result.IsSucceed)
                    {
                        AppendEmptyText();
                        byte[] rValue = result.Value;
                        rValue = rValue.Reverse().ToArray();
                        for (int i = 0; i < readNumber; i++)
                        {
                            var cAddress = (address + i * bLength).ToString();
                            if (rd_coil.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadCoil(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_discrete.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadDiscrete(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_short.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadInt16(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_ushort.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadUInt16(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_int.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadInt32(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_uint.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadUInt32(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_long.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadInt64(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_ulong.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadUInt64(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_float.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadFloat(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                            else if (rd_double.Checked)
                                AppendText($"[读取 {address + i * bLength} 成功]：{ client.ReadDouble(address.ToString(), cAddress, rValue).Value}\t\t耗时：{result.TimeConsuming}ms");
                        }
                    }
                    else
                        AppendText($"[读取 {txt_address.Text?.Trim()} 失败]：{result.Err}\t\t耗时：{result.TimeConsuming}ms");
                }
                //批量读取
                else if (addressAndReadNumber.Length >= 2)
                {
                    DataTypeEnum datatype = DataTypeEnum.None;
                    byte functionCode = 3;
                    //线圈
                    if (rd_coil.Checked)
                    {
                        datatype = DataTypeEnum.Bool;
                        functionCode = 1;
                    }
                    //离散
                    else if (rd_discrete.Checked)
                    {
                        datatype = DataTypeEnum.Bool;
                        functionCode = 2;
                    }
                    else if (rd_short.Checked) datatype = DataTypeEnum.Int16;
                    else if (rd_ushort.Checked) datatype = DataTypeEnum.UInt16;
                    else if (rd_int.Checked) datatype = DataTypeEnum.Int32;
                    else if (rd_uint.Checked) datatype = DataTypeEnum.UInt32;
                    else if (rd_long.Checked) datatype = DataTypeEnum.Int64;
                    else if (rd_ulong.Checked) datatype = DataTypeEnum.UInt64;
                    else if (rd_float.Checked) datatype = DataTypeEnum.Float;
                    else if (rd_double.Checked) datatype = DataTypeEnum.Double;

                    List<ModbusInput> addresses = new List<ModbusInput>();
                    foreach (var item in addressAndReadNumber)
                    {
                        addresses.Add(new ModbusInput()
                        {
                            Address = item,
                            DataType = datatype,
                            FunctionCode = functionCode,
                            StationNumber = stationNumber,
                        });
                    }

                    result = client.BatchRead(addresses);

                    if (result.IsSucceed)
                    {
                        AppendEmptyText();
                        foreach (var item in result.Value)
                        {
                            AppendText($"[读取 {item.Address} 成功]：{item.Value}\t\t耗时：{result.TimeConsuming}ms");
                        }
                    }
                    else
                        AppendText($"[读取 {txt_address.Text?.Trim()} 失败]：{result.Err}\t\t耗时：{result.TimeConsuming}ms");
                }
                //单个读取
                else
                {
                    if (rd_coil.Checked)
                    {
                        result = client.ReadCoil(txt_address.Text, stationNumber);
                    }
                    else if (rd_short.Checked)
                    {
                        result = client.ReadInt16(txt_address.Text, stationNumber);
                    }
                    else if (rd_ushort.Checked)
                    {
                        result = client.ReadUInt16(txt_address.Text, stationNumber);
                    }
                    else if (rd_int.Checked)
                    {
                        result = client.ReadInt32(txt_address.Text, stationNumber);
                    }
                    else if (rd_uint.Checked)
                    {
                        result = client.ReadUInt32(txt_address.Text, stationNumber);
                    }
                    else if (rd_long.Checked)
                    {
                        result = client.ReadInt64(txt_address.Text, stationNumber);
                    }
                    else if (rd_ulong.Checked)
                    {
                        result = client.ReadUInt64(txt_address.Text, stationNumber);
                    }
                    else if (rd_float.Checked)
                    {
                        result = client.ReadFloat(txt_address.Text, stationNumber);
                    }
                    else if (rd_double.Checked)
                    {
                        result = client.ReadDouble(txt_address.Text, stationNumber);
                    }
                    else if (rd_discrete.Checked)
                    {
                        result = client.ReadDiscrete(txt_address.Text, stationNumber);
                    }

                    if (result.IsSucceed)
                        AppendText($"[读取 {txt_address.Text?.Trim()} 成功]：{result.Value}\t\t耗时：{result.TimeConsuming}ms");
                    else
                        AppendText($"[读取 {txt_address.Text?.Trim()} 失败]：{result.Err}\t\t耗时：{result.TimeConsuming}ms");
                }

                var config = ConnectionConfig.GetConfig();
                config.ModBusRtu_Value = txt_value.Text;
                config.ModBusRtu_Address = txt_address.Text;
                config.ModBusRtu_StationNumber = txt_stationNumber.Text;
                config.ModBusRtu_ShowPackage = chb_show_package.Checked;
                config.ModBusRtu_Datatype = string.Empty;
                if (rd_coil.Checked) config.ModBusRtu_Datatype = "rd_coil";
                else if (rd_discrete.Checked) config.ModBusRtu_Datatype = "rd_discrete";
                else if (rd_short.Checked) config.ModBusRtu_Datatype = "rd_short";
                else if (rd_ushort.Checked) config.ModBusRtu_Datatype = "rd_ushort";
                else if (rd_int.Checked) config.ModBusRtu_Datatype = "rd_int";
                else if (rd_uint.Checked) config.ModBusRtu_Datatype = "rd_uint";
                else if (rd_long.Checked) config.ModBusRtu_Datatype = "rd_long";
                else if (rd_ulong.Checked) config.ModBusRtu_Datatype = "rd_ulong";
                else if (rd_float.Checked) config.ModBusRtu_Datatype = "rd_float";
                else if (rd_double.Checked) config.ModBusRtu_Datatype = "rd_double";
                config.SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (chb_show_package.Checked || (ModifierKeys & Keys.Control) == Keys.Control)
                {
                    AppendText($"[请求报文]{result?.Requst}");
                    AppendText($"[响应报文]{result?.Response}\r\n");
                }
            }
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_write_Click(object sender, EventArgs e)
        {
            byte.TryParse(txt_stationNumber.Text?.Trim(), out byte stationNumber);
            if (string.IsNullOrWhiteSpace(txt_address.Text))
            {
                MessageBox.Show("请输入地址");
                return;
            }
            if (string.IsNullOrWhiteSpace(txt_value.Text))
            {
                MessageBox.Show("请输入值");
                return;
            }
            try
            {
                var address = txt_address.Text?.Trim().Split('-')[0];
                dynamic result = null;
                if (rd_coil.Checked)
                {
                    if (!bool.TryParse(txt_value.Text?.Trim(), out bool coil))
                    {
                        if (txt_value.Text?.Trim() == "0")
                            coil = false;
                        else if (txt_value.Text?.Trim() == "1")
                            coil = true;
                        else
                        {
                            MessageBox.Show("请输入 True 或 False");
                            return;
                        }
                    }
                    result = client.Write(address, coil, stationNumber);
                }
                else if (rd_short.Checked)
                {
                    result = client.Write(address, short.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_ushort.Checked)
                {
                    result = client.Write(address, ushort.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_int.Checked)
                {
                    result = client.Write(address, int.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_uint.Checked)
                {
                    result = client.Write(address, uint.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_long.Checked)
                {
                    result = client.Write(address, long.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_ulong.Checked)
                {
                    result = client.Write(address, ulong.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_float.Checked)
                {
                    result = client.Write(address, float.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_double.Checked)
                {
                    result = client.Write(address, double.Parse(txt_value.Text?.Trim()), stationNumber);
                }
                else if (rd_discrete.Checked)
                {
                    AppendText($"离散类型只读");
                    return;
                }

                if (result.IsSucceed)
                    AppendText($"[写入 {address?.Trim()} 成功]：{txt_value.Text?.Trim()} OK\t\t耗时：{result.TimeConsuming}ms");
                else
                    AppendText($"[写入 {address?.Trim()} 失败]：{result.Err}\t\t耗时：{result.TimeConsuming}ms");
                if (chb_show_package.Checked || (ModifierKeys & Keys.Control) == Keys.Control)
                {
                    AppendText($"[请求报文]{result.Requst}");
                    AppendText($"[响应报文]{result.Response}\r\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 启动仿真服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_open_server_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_content.Text.Contains("小技巧")) txt_content.Text = string.Empty;
                var PortName = cb_portNameSend_server.Text.ToString();
                var BaudRate = int.Parse(cb_baudRate.Text.ToString());
                var DataBits = int.Parse(txt_dataBit.Text.ToString());
                var StopBits = (StopBits)int.Parse(txt_stopBit.Text.ToString());
                var parity = cb_parity.SelectedIndex == 0 ? Parity.None : (cb_parity.SelectedIndex == 1 ? Parity.Odd : Parity.Even);
                server?.Stop();
                server = new ModbusRtuServer(PortName, BaudRate, DataBits, StopBits, parity);
                server.Start();
                AppendText("开启仿真服务");
                but_server_open.Enabled = false;
                but_server_close.Enabled = true;
                cb_portNameSend_server.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 关闭仿真服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_close_server_Click(object sender, EventArgs e)
        {
            server?.Stop();
            AppendText("关闭仿真服务");
            but_server_open.Enabled = true;
            but_server_close.Enabled = false;
            cb_portNameSend_server.Enabled = true;
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void but_clear_data_Click(object sender, EventArgs e)
        {
            DataPersist.Clear();
            AppendText($"数据清空成功\r\n");
        }

        private void but_sendData_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt_dataPackage.Text))
                {
                    MessageBox.Show("请输入要发送的报文");
                    return;
                }
                var dataPackageString = txt_dataPackage.Text.Replace(" ", "");
                if (dataPackageString.Length % 2 != 0)
                {
                    MessageBox.Show("请输入正确的的报文");
                    return;
                }

                var dataPackage = DataConvert.StringToByteArray(txt_dataPackage.Text?.Trim(), false);
                var msg = client.SendPackage(dataPackage);
                AppendText($"[请求报文]{string.Join(" ", dataPackage.Select(t => t.ToString("X2")))}\r");
                AppendText($"[响应报文]{string.Join(" ", msg.Select(t => t.ToString("X2")))}\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                client.Close();
                client.Open();
            }
        }

        private void AppendText(string content)
        {
            txt_content.Invoke((Action)(() =>
            {
                txt_content.AppendText($"[{DateTime.Now.ToLongTimeString()}]{content}\r\n");
            }));
        }

        private void AppendEmptyText()
        {
            txt_content.Invoke((Action)(() =>
            {
                txt_content.AppendText($"\r\n");
            }));
        }

        private async void but_brokenline_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                byte.TryParse(txt_stationNumber.Text?.Trim(), out byte stationNumber);

                var constant = new BrokenLineChart(txt_address.Text);
                constant.Show();
                while (!constant.IsDisposed)
                {
                    await Task.Delay(800);

                    dynamic result = null;
                    if (rd_coil.Checked)
                        result = client.ReadCoil(txt_address.Text, stationNumber);
                    else if (rd_short.Checked)
                        result = client.ReadInt16(txt_address.Text, stationNumber);
                    else if (rd_ushort.Checked)
                        result = client.ReadUInt16(txt_address.Text, stationNumber);
                    else if (rd_int.Checked)
                        result = client.ReadInt32(txt_address.Text, stationNumber);
                    else if (rd_uint.Checked)
                        result = client.ReadUInt32(txt_address.Text, stationNumber);
                    else if (rd_long.Checked)
                        result = client.ReadInt64(txt_address.Text, stationNumber);
                    else if (rd_ulong.Checked)
                        result = client.ReadUInt64(txt_address.Text, stationNumber);
                    else if (rd_float.Checked)
                        result = client.ReadFloat(txt_address.Text, stationNumber);
                    else if (rd_double.Checked)
                        result = client.ReadDouble(txt_address.Text, stationNumber);
                    else if (rd_discrete.Checked)
                        result = client.ReadDiscrete(txt_address.Text, stationNumber);

                    if (result.IsSucceed)
                        constant.AddData(result.Value);
                }
            }
            catch (Exception)
            { }
        }
    }
}
