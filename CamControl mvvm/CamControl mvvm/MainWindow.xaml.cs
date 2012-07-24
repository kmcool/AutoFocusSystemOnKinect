//testtest
using CamControl_mvvm.ViewModel;

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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

//using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Emgu.CV.Structure;

//USBHID driver
using HID;
using Microsoft.Kinect;

using GalaSoft.MvvmLight.Messaging;


namespace CamControl_mvvm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        //kinectData
        private Image<Gray, Int16> depthImage = new Image<Gray, Int16>(640, 480);
        private Image<Bgra, Byte> colorImage = new Image<Bgra, Byte>(640, 480);
        private KinectSensor sensor;
        private short[] depthPixelData = new short[640 * 480];
        private byte[] colorPixelData = new byte[640 * 480 * 4];
        private ColorImagePoint[] _mappedDepthLocations;
        private ColorImagePoint[] colorCoordinate = new ColorImagePoint[640 * 480];
        private short[] depthRemapData = new short[640 * 480];
        

        //Object-tracking-algorithm
        private System.Drawing.Rectangle trackWindow;
        private MCvConnectedComp trackComp;
        private MCvBox2D trackBox;
        private int trackingFlag = 0;
        System.Windows.Point pointA;
        private bool imageVisible = false;
        public bool IsTracking = false;

        //Lens-Calibration
        static public string[] focalLength = new string[] { "3", "1.5", "1", "0.7", "0.5", "0.4", "0.35", "0.3" };
        static public string[] focalLength50mm = new string[] { "10","3","2","1.5","1","0.8","0.6","0.5","0.45" };
        static public int Lens = 1; //1 means 28mm, 2 means 50mm
        static public FileStream sFile;
        static int[] LensRollingCount = new int[100];
        private int LensOffset;

        
        //Webcam
        Capture capture = default(Capture);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        //Usb
        usb usbControll = new usb();

        public MainWindow()
        {
            InitializeComponent();
            capture = new Capture(0);

            this.Loaded +=new RoutedEventHandler(MainWindow_Loaded);
             this.Unloaded += new RoutedEventHandler(MainWindow_Unloaded);
            

           
            Closing += (s, e) => ViewModelLocator.Cleanup();

            Messenger.Default.Register<int>(this, "focalLength", false, m =>
            {
                label3.Content = m;
            });

            Messenger.Default.Register<int>(this, "LensOffset", false, m =>
            {
                LensOffset = m;
            });

            Messenger.Default.Register<bool>(this, "IsTracking", false, m =>
            {
                IsTracking = m;
            });

        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox srcBox = e.Source as ComboBox;
            switch (srcBox.SelectedItem.ToString())
            {
                case "System.Windows.Controls.ComboBoxItem: Canon FD 28mm f2.8":
                    sFile = new FileStream("CanonFD28f28.txt", FileMode.OpenOrCreate);
                    Lens = 1;
                    break;
                case "System.Windows.Controls.ComboBoxItem: Pentax FA 50mm f1.4":
                    sFile = new FileStream("PentaxFA50f1.4.txt", FileMode.OpenOrCreate);
                    Lens = 2;
                    break;
            }
            //sFile = new FileStream("")
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            initKinect();
        }

        void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            uninitKinect();
        }

        void initKinect()
        {
            //初始化sensor实例
            sensor = KinectSensor.KinectSensors[0];
            
            //初始化照相机
            sensor.DepthStream.Enable();
            sensor.ColorStream.Enable();

            //打开数据流
            sensor.Start();
        }

        void uninitKinect()
        {
            if (sensor == null)
            {
                return;
            }
            sensor.Stop();
            sensor = null;
        }


        private void sliderLens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //label1.Content = e.NewValue;
        }



        private void btnRst_Click(object sender, RoutedEventArgs e)
        {

        }




        private void canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void image1_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void canvas1_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            trackingFlag = 0;
            pointA = e.MouseDevice.GetPosition(this.canvas1);
        }

        private void canvas1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvas1.Children.Clear();
        }

        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point mousePoint = e.GetPosition(this.canvas1);
                canvas1.Children.Clear();
                rectDraw(pointA, mousePoint);
            }
        }

        private void rectDraw(System.Windows.Point a, System.Windows.Point b)
        {
            if (a.X > b.X)
            {
                double tmp;
                tmp = a.X;
                a.X = b.X;
                b.X = tmp;
            }
            if (a.Y > b.Y)
            {
                double tmp;
                tmp = a.Y;
                a.Y = b.Y;
                b.Y = tmp;
            }
            b.X -= a.X;
            b.Y -= a.Y;
            RectangleGeometry myRectangleGeometry = new RectangleGeometry();
            myRectangleGeometry.Rect = new Rect(a.X, a.Y, b.X, b.Y);

            trackWindow = new System.Drawing.Rectangle((int)a.X, (int)a.Y, (int)b.X, (int)b.Y);
            trackingFlag = 1;

            //myRectangleGeometry.Rect = new Rect(10, 10, 100, 100);
            System.Windows.Shapes.Path myPath = new System.Windows.Shapes.Path();
            //myPath.Fill = Brushes.LemonChiffon;
            myPath.Stroke = System.Windows.Media.Brushes.BlueViolet;
            myPath.StrokeThickness = 2;
            myPath.Data = myRectangleGeometry;

            this.canvas1.Children.Add(myPath);
        }

        private void btnCal_Click(object sender, RoutedEventArgs e)
        {
            CaliWindow caliWin = null;
            if (Lens == 1)
            {
                caliWin = new CaliWindow(focalLength.Length, focalLength);
                
            }
            if (Lens == 2)
            {
                caliWin = new CaliWindow(focalLength50mm.Length, focalLength50mm);
            }
            caliWin.Show();
        }


        void dataStream(object sender, EventArgs e)
        {
            {
                RangeF[] range = new RangeF[2];
                range[0] = new RangeF(0, 180);
                range[1] = new RangeF(0, 255);

                pollColorImageStream();
                pollDepthImageStream();

                //Color------------------
                Bitmap bitmapColor = new Bitmap(colorImage.Width, colorImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                BitmapData bmd = bitmapColor.LockBits(new System.Drawing.Rectangle(0, 0, colorImage.Width, colorImage.Height), ImageLockMode.ReadWrite, bitmapColor.PixelFormat);

                Marshal.Copy(colorPixelData, 0, bmd.Scan0, colorPixelData.Length);

                bitmapColor.UnlockBits(bmd);

                Image<Bgr, Byte> colorTemp = new Image<Bgr, Byte>(bitmapColor);
                //Color------------------end

                //depth------------------

                byte[] byteDepth = new byte[640 * 480];
                byte[] remap = new byte[640 * 480];
                sensor.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depthPixelData, ColorImageFormat.RgbResolution640x480Fps30, colorCoordinate);
                for (int y = 0; y < 480; y++)
                {
                    for (int x = 0; x < 640; x++)
                    {
                        int position = y * 640 + x;
                        short tempShort = depthPixelData[position];
                        //depthImage[y, x] = new Gray(tempShort);
                        byteDepth[position] = (byte)(tempShort >> 8);
                        //byteDepth[y, x] = new Gray((byte)(tempShort));

                        int positionRemap = colorCoordinate[position].Y * 640 + colorCoordinate[position].X;
                        if (positionRemap > 640 * 480)
                            continue;
                        depthRemapData[positionRemap] = depthPixelData[position];
                        remap[positionRemap] = (byte)(tempShort >> 8);
                        //byteDepth[y, x] = new Gray((byte)(tempShort));
                    }
                }
                Bitmap bitmapDepth = new Bitmap(depthImage.Width, depthImage.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                BitmapData bmd2 = bitmapDepth.LockBits(new System.Drawing.Rectangle(0, 0, depthImage.Width, depthImage.Height), ImageLockMode.ReadWrite, bitmapDepth.PixelFormat);

                Marshal.Copy(byteDepth, 0, bmd2.Scan0, byteDepth.Length);

                bitmapDepth.UnlockBits(bmd2);

                Image<Gray, Byte> depthTemp = new Image<Gray, Byte>(bitmapDepth);
                //depth------------------end

                Byte[] backFrame = new Byte[640 * 480];
                BitmapImage trackingOut = new BitmapImage();

                if (trackingFlag != 0)
                {
                    Image<Hsv, Byte> hsv = new Image<Hsv, Byte>(640, 480);
                    CvInvoke.cvCvtColor(colorTemp, hsv, COLOR_CONVERSION.CV_BGR2HSV);
                    Image<Gray, Byte> hue = hsv.Split()[0];
                    //range of hist is 180 or 256? not quite sure
                    DenseHistogram hist = new DenseHistogram(180, new RangeF(0.0f, 179.0f));

                    Image<Gray, Byte> mask = new Image<Gray, Byte>(trackWindow.Width, trackWindow.Height);

                    for (int y = 0; y < 480; y++)
                    {
                        for (int x = 0; x < 640; x++)
                        {
                            if (x >= trackWindow.X && x < trackWindow.X + trackWindow.Width && y >= trackWindow.Y && y < trackWindow.Y + trackWindow.Height)
                                mask[y - trackWindow.Y, x - trackWindow.X] = hue[y, x];
                        }
                    }
                    hist.Calculate(new IImage[] { mask }, false, null);
                    //maybe need to re-scale the hist to 0~255?

                    //back projection

                    IntPtr backProject = CvInvoke.cvCreateImage(hsv.Size, IPL_DEPTH.IPL_DEPTH_8U, 1);
                    CvInvoke.cvCalcBackProject(new IntPtr[1] { hue }, backProject, hist);

                    CvInvoke.cvErode(backProject, backProject, IntPtr.Zero, 3);

                    //CAMshift
                    CvInvoke.cvCamShift(backProject, trackWindow, new MCvTermCriteria(50, 0.1), out trackComp, out trackBox);
                    trackWindow = trackComp.rect;
                    if (trackWindow.Width < 5 || trackWindow.Height < 5)
                    {
                        if (trackWindow.Width < 5)
                        {
                            trackWindow.X = trackWindow.X + trackWindow.Width / 2 - 3;
                            trackWindow.Width = 6;
                        }
                        if (trackWindow.Height < 5)
                        {
                            trackWindow.Y = trackWindow.Y + trackWindow.Height / 2 - 3;
                            trackWindow.Height = 6;
                        }
                    }


                    Image<Bgr, Byte> showFrame = colorTemp;
                    showFrame.Draw(trackWindow, new Bgr(System.Drawing.Color.Blue), 2);

                    using (var stream = new MemoryStream())
                    {
                        showFrame.Bitmap.Save(stream, ImageFormat.Bmp);
                        trackingOut.BeginInit();
                        trackingOut.StreamSource = new MemoryStream(stream.ToArray());
                        trackingOut.EndInit();
                    }

                    //calculate the average depth of tracking object
                    int min = 65528, max = 0, num = 0;
                    UInt32 sum = 0;
                    for (int y = 0; y < trackWindow.Height; y++)
                    {
                        for (int x = 0; x < trackWindow.Width; x++)
                        {
                            int position = (trackWindow.X + x) + (trackWindow.Y + y) * 640;
                            ushort temp = (ushort)depthRemapData[position];
                            if (temp != 65528 && temp != 0)//black
                            {
                                if (temp < min)
                                    min = temp;
                                if (temp > max)
                                    max = temp;
                                sum += temp;
                                num++;
                            }
                        }
                    }
                    ushort average = 0;
                    if (num != 0)
                    {
                        average = (ushort)(sum / num);
                    }
                    //Int32 depthInches = (Int32)((average >> DepthImageFrame.PlayerIndexBitmaskWidth) * 0.0393700787);
                    //Int32 depthFt = depthInches / 12;
                    //depthInches = depthInches % 12;
                    Int32 depth = average >> DepthImageFrame.PlayerIndexBitmaskWidth;
                    textBlock1.Text = String.Format("{0}mm", depth);
                    Double distanceInMeter = (Double)depth / 1000;
                    Messenger.Default.Send<Double>(distanceInMeter, "Distance"); 
                }
               
                //if (rbtnColorFrame.IsChecked == true)
                //{
                    if (trackingFlag != 0)
                        image1.Source = trackingOut;
                    else
                        image1.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Bgr32, null, colorPixelData, 640 * 4);
                //    else
                //        imageOutputBig.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Bgr32, null, colorPixelData, 640 * 4);
                //    //imageOutputBig.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Gray8, null, backFrame, 640);
                //    imageOutputSmall.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Gray16, null, depthRemapData, 640 * 2);
                //}
                //else
                //{
                //    imageOutputSmall.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Bgr32, null, colorPixelData, 640 * 4);
                //    imageOutputBig.Source = BitmapSource.Create(640, 480, 96, 96, PixelFormats.Gray16, null, depthRemapData, 640 * 2);
                //}
                //if(CvInvoke.cvWaitKey(0)=='q')
                //{
                //    trackingFlag = 0;
                //}
            }
        }

        void pollDepthImageStream()
        {
            if (sensor == null)
            {
                //TODO: Display a message to plug-in a Kinect.
            }
            else
            {
                try
                {
                    using (DepthImageFrame frame = sensor.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {

                            frame.CopyPixelDataTo(depthPixelData);
                            _mappedDepthLocations = new ColorImagePoint[frame.PixelDataLength];
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Report an error message
                }
            }
        }

        void pollColorImageStream()
        {
            if (sensor == null)
            {
                //TODO: Display a message to plug-in a Kinect.
            }
            else
            {
                try
                {
                    using (ColorImageFrame frame = sensor.ColorStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(colorPixelData);


                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Report an error message
                }
            }
        }

        private void btnCon_Click(object sender, RoutedEventArgs e)
        {
            if (usbControll.USBConnect() == true)
            {
                btnCon.IsEnabled = false;

                
            }

        }

        private void btnRst_Click_1(object sender, RoutedEventArgs e)
        {
            //Messenger.Default.Send<bool>(true); 
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Algorithm Algo = new Algorithm();
            int Target = Algo.DistanceToLens50mm(e.NewValue, LensOffset);
            Messenger.Default.Send<int>(Target, "LensRollingTarget");
            label1.Content = e.NewValue;
           
            
        }

        private void btnVisiable_Click(object sender, RoutedEventArgs e)
        {
            if (imageVisible == true)
            {
                CompositionTarget.Rendering -= dataStream;
                imageVisible = false;
            } 
            else
            {
                imageVisible = true;
                CompositionTarget.Rendering += dataStream;
            }
            
        }

        private void btnTrack_Click(object sender, RoutedEventArgs e)
        {
            if (IsTracking)
            {
                Messenger.Default.Register<Double>(this, "Distance", false, m =>
                {
                    Algorithm Algo = new Algorithm();
                    if (Lens == 1)
                    {
                        usbControll.dataSend(Algo.DistanceToLens28mm(m, LensOffset).ToString());
                    }
                    else
                    {
                        usbControll.dataSend(Algo.DistanceToLens50mm(m, LensOffset).ToString());
                    }
                });
            } 
            else
            {
                MessageBox.Show("拜托，请先按Caliwindows里的Remember键");
            }
            
        }





    }
}