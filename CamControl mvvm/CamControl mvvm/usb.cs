using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HID;
using System.Windows;
using System.Threading;
using CamControl_mvvm.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Threading;

namespace CamControl_mvvm
{
    class usb
    {
        //MainWindow mw = Application.Current.MainWindow as MainWindow;
       

        

        //USB-HID
        private static object syncObj = new object();
        HID.Hid hidControl = new HID.Hid();
        byte RecvId;
        byte[] RecvBuff = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int RecvCounter = 0;
        bool firstStep = true;
        /// <summary> 
        /// 定义一个代理 
        /// </summary> 
        private delegate void CrossThreadOperationControl();

        private void BackgroundProcess()//report r)
        {
                uint ADCvalue = 0;
                uint RollCounter = 0;
                int i = 0;
               
                while ((RecvBuff[i]!='$') && (i < RecvBuff.Length ))
                {

                    RecvBuff[i] = (byte)((RecvBuff[i] - 0x30) & 0x0f);
                    ADCvalue = ADCvalue + RecvBuff[i]*(uint)Math.Pow(10,i);
                    i++;
                    if (i == RecvBuff.Length - 1)
                    {
                        break;
                    }
                }

                for (int j = i; (RecvBuff[j] != '@') && (j < RecvBuff.Length ); j++)
                {
                    RecvBuff[j] = (byte)((RecvBuff[j] - 0x30) & 0x0f);
                    RollCounter = RollCounter + RecvBuff[j] * (uint)Math.Pow(10, j - i -1);
                    if (j == RecvBuff.Length - 1)
                    {
                        break;
                    }
                }
               

                if (i < 15)
                {
                    Messenger.Default.Send<int>(((int)ADCvalue / 40), "a1");
                    Messenger.Default.Send<int>(((int)RollCounter), "a2");
                }


        }

        public bool USBConnect()
        {
            string status = hidControl.OpenDevice(0x0483, 0x5750, "0000000").ToString();

            if (HID_RETURN.SUCCESS.ToString() == status)
            {
                MessageBox.Show("OK");
                hidControl.DeviceRemoved += new EventHandler(deviceRemoved);
                
                hidControl.DataReceived += new EventHandler<report>(dataReceived);

                Messenger.Default.Register<int>(this, "MotorDirect", false, n =>
                {
                    
                });

                Messenger.Default.Register<int>(this, "LensRollingTarget", false, n =>
                {
                    dataSend(n.ToString());
                });

                return true;
            }

            if (HID_RETURN.DEVICE_NOT_FIND.ToString() == status)
            {
                MessageBox.Show("NOT_FIND");
                return false;
            }
            if (HID_RETURN.DEVICE_OPENED.ToString() == status)
            {
                MessageBox.Show("Device opened");
                return false;
            }
            return false;
        }

        public void deviceRemoved(object hidControl, EventArgs e)
        {
            MessageBox.Show("removed");
            try
            {
                
                //mw.btnCon.IsEnabled = true;
            }
            catch
            {
                MessageBox.Show("SB");
            }
        }

        public void dataReceived(object hidControl, report r)
        {
            Thread newthread = new Thread(new ThreadStart(BackgroundProcess));
            newthread.Start();
            RecvBuff = r.reportBuff;
            RecvId = r.reportID;
            

        }

        /// <summary>
        /// 以字符串发送控制数据，输入string, 作为目标转动值
        /// </summary>
        public void dataSend(string direct)
        {
            byte reportId = 00;
            byte[] reportBuff = { 0xff, 0xff, 0xff, 0xff, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };//前4位作为保留控制字
            int i = 0;
            for (i = 0; i < direct.Length;i++ )
            {
                reportBuff[i+4] = (byte) direct[i];
            }
            reportBuff[i+4] = 0xff;
            HID.report sendBuffer = new HID.report(reportId, reportBuff);
            hidControl.Write(sendBuffer);
   
        }
    }
}
