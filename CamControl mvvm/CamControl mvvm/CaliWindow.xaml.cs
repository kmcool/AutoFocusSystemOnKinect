using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Threading;


namespace CamControl_mvvm
{
    /// <summary>
    /// Interaction logic for CaliWindow.xaml
    /// </summary>
    /// 

    public partial class CaliWindow : Window
    {

        static int[] LensRollingCount = new int[100];
        int rbtSeleted = 0;
        int RollingCount = 0;
        RadioButton[] rbtnList = new RadioButton[100];
        bool MotorFWD = false;
        System.Threading.Thread ConlySend;
        int direct;
        int lbFocalLengthSeleted;
        Algorithm Algo = new Algorithm();
        public string[] focalLength;



        public CaliWindow(int i, string[] focallength)
        {
            InitializeComponent();
            label1.Content = 50;
            focalLength = focallength;
            Messenger.Default.Register<int>(this,"a1", false, n =>
            {
                BarAxisX.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => BarAxisX.Value = n));
            }
             );
            Messenger.Default.Register<int>(this, "a2", false, n =>
            {
                label1.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => label1.Content = n));
                RollingCount = n;
            }
 );  
            CreateButton(i);
        }

        private void CreateScroll()
        {

        }

        private void CreateButton(int x)
        {


            btnDelete.IsEnabled = false;
            cvsSelectFL.Children.Clear();
            //四个方向的边距都是5
            double width = 100;
            double height = 30;

            for (int i = 0; i < x; i++)
            {
                RadioButton bt = new RadioButton()
                {
                    Width = width,
                    Height = height
                };

                Canvas.SetTop(bt, i * height + 5);
                //这两句很关键。按钮在Canvas中的定位与它自己的Left以及Top不是一个概念
                bt.Content = focalLength[i];
                bt.GroupName = "rbtnFL";
                bt.FontSize = 15;
                cvsSelectFL.Children.Add(bt);
                rbtnList[i] = bt;
                bt.Click += new RoutedEventHandler(rbt_Click);
                
                
            }


        }

        private void rbt_Click(object sender, RoutedEventArgs e)
        {
            RadioButton srcButton = e.Source as RadioButton;
            int i = Array.IndexOf(focalLength, srcButton.Content);
            label1.Content = focalLength[i];
            rbtSeleted = i;
            Messenger.Default.Send<int>(i,"focalLength");
            

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.CaliWindow1.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            String tmp = "";
            for (int i = 0; i < focalLength.Length; i++ )
            {
                tmp = tmp +focalLength[i]+"@"+LensRollingCount[i].ToString() + ";\r\n";
            }
            try
            {
                byte[] data = new UTF8Encoding().GetBytes(tmp);
                MainWindow.sFile.Write(data, 0, data.Length);
                MainWindow.sFile.Flush();
                MainWindow.sFile.Close();
                this.CaliWindow1.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Warning: The calibration file has not been saved!");
            }

        }

        public void updateBarAxisX(int x)
        {
            if (x >= 0 && x <= 100)
            {

            }
        }


        private void btnFar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            direct = 1; // 1 代表前进
            ConlySend = new System.Threading.Thread(new System.Threading.ThreadStart(usbSender));
            ConlySend.Start();
        }

        private void btnFar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ConlySend.Abort();
        }

        private void btnNear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            direct = 2; // 2 代表后退
            ConlySend = new System.Threading.Thread(new System.Threading.ThreadStart(usbSender));
            ConlySend.Start();
        }

        private void btnNear_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ConlySend.Abort();
        }


        private void usbSender()
        {
            while (true)
            Messenger.Default.Send<int>(direct, "MotorDirect");
        }

        private void btnRem_Click(object sender, RoutedEventArgs e)
        {
            LensRollingCount[rbtSeleted] = RollingCount;
            lbFocalLength.Items.Add(rbtnList[rbtSeleted].Content.ToString() + ":" + RollingCount);
            Messenger.Default.Send<int>(RollingCount, "LensOffset");
            Messenger.Default.Send<bool>(true, "IsTracking");
            MessageBox.Show("Offset is :"+LensRollingCount[0]);
            
        }



        private void lbFocalLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnDelete.IsEnabled = true;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            lbFocalLength.Items.RemoveAt(lbFocalLength.SelectedIndex);
            btnDelete.IsEnabled = false;
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            byte[] loadbuff = new byte[100];
            MainWindow.sFile.Read(loadbuff, 0,loadbuff.Length);
            
            for (int i = 0; i < loadbuff.Length && i<90 ; i++ )
            {
                if (loadbuff[i] == 0x40)
                {
                    int j = (int)(loadbuff[i - 1]- 0x30);
                    string tmp = "";
                    for (int k = i+1; loadbuff[k]!=';' && k<loadbuff.Length ; k++)
                    {
                        tmp +=(char) loadbuff[k];
                        i = k;
                    }
                    LensRollingCount[j] = int.Parse(tmp);
                    
                }
               
            }
            MainWindow.sFile.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
        int Target = Algo.DistanceToLens50mm(3, LensRollingCount[0]);
        Messenger.Default.Send<int>(Target, "LensRollingTarget");
        MessageBox.Show(Target.ToString());
        }

    }
}
