using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Microsoft.Kinect;
using Microsoft.Win32;
using System.Windows.Threading;



namespace KinectDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        private Int32 brightness_val = 0;
        private Int32 contrast_val = 0;

        private BitmapImage temp_img = null;

        private System.Windows.Point origin;
        private System.Windows.Point start;
        private KinectSensor sensor = null;
        public MainWindow()
        {

            InitializeComponent();

            TransformGroup group = new TransformGroup();

            ScaleTransform xform = new ScaleTransform();
            group.Children.Add(xform);

            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);

            main_img.RenderTransform = group;

            main_img.MouseWheel += this.image_MouseWheel;
            main_img.MouseLeftButtonDown += image_MouseLeftButtonDown;
            main_img.MouseLeftButtonUp += image_MouseLeftButtonUp;
            main_img.MouseMove += image_MouseMove;
              

            foreach (var potentialSensor in KinectSensor.KinectSensors)
             {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                break;
    

                }
            }
            if (sensor != null)
            {
                KinectControl kinect = new KinectControl(sensor);
                
                kinect.VoiceCommandEvent += kinect_listener;
                kinect.GestureEvent += kinect_GestureEvent;
                /*
                if (kinect.VoiceCommandEvent == null)
                {
                    text_box.AppendText("No listener\r");
                }
                else
                    text_box.AppendText("Listener!\r");
                 */
                //text_box.AppendText(kinect.VoiceCommandEvent.ToString()+"\r");
            }
        }

        void kinect_GestureEvent(object sender, GestureEventArgs e)
        {
            switch (e.commandType)
            {
                case "BRIGHTNESS":
                    this.Dispatcher.Invoke(DispatcherPriority.Render,  new Action(() => this.changeBrightness(e.propertyValue)));
                    //changeBrightness(e.propertyValue);
                    break;
                case "CONTRAST":
                    this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => this.changeContrast(e.propertyValue)));
                    //changeContrast(e.propertyValue);
                    break;
                case "ZOOM":
                    this.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => this.changeZoom(e.propertyValue)));
                    break;
            }
            this.text_box.AppendText("Kinect says: " + e.debugOutput + "\r"); //Log to screen
            this.text_box.ScrollToEnd(); //Move cursor in log to ende.ToString
        }
        
        private void kinect_listener(object sender, VoiceCommandEventArgs e)
        {
            this.text_box.AppendText("Kinect says: "+e.debugOutput+"\r"); //Log to screen
            this.text_box.ScrollToEnd(); //Move cursor in log to ende.ToString
        }
          

        
        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            main_img.ReleaseMouseCapture();
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!main_img.IsMouseCaptured) return;

            var tt = (TranslateTransform)((TransformGroup)main_img.RenderTransform).Children.First(tr => tr is TranslateTransform);
            Vector v = start - e.GetPosition(border);
            tt.X = origin.X - v.X;
            tt.Y = origin.Y - v.Y;
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            main_img.CaptureMouse();
            var tt = (TranslateTransform)((TransformGroup)main_img.RenderTransform).Children.First(tr => tr is TranslateTransform);
            start = e.GetPosition(border);
            origin = new System.Windows.Point(tt.X, tt.Y);
        }

        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TransformGroup transformGroup = (TransformGroup)main_img.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            double zoom = e.Delta > 0 ? .2 : -.2;
            transform.ScaleX += zoom;
            transform.ScaleY += zoom;
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Open_Image(object sender, ExecutedRoutedEventArgs e)
        { //When 'Open' button is pressed, call this method, allow only common image file extensions
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.png;*.bmp;*.gif|All Files|*.*";
            if (ofd.ShowDialog(this) == true)
            {
                //Set a (BitmapImage) temp_img for editing (brightness / contrast), and also set the (Image) main_img for viewing
                temp_img = new BitmapImage(new Uri(ofd.FileName));
                main_img.Source = temp_img;
            }
        }


        private void brite_up_Click(object sender, RoutedEventArgs e)
        { //Button listener for Brightness UP button

            if (temp_img != null) //Only do this if we already have a file loaded
            { 
                if (brightness_val < 255) //Limit brightness to 255
                { 
                    brightness_val = brightness_val + 5;
                }
                //Set the display image (main_img) to a transform of the edit image (temp_img)
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val));
                this.text_box.AppendText("Brightness UP 5 to " + brightness_val + "\r"); //Log to screen
                this.text_box.ScrollToEnd(); //Move cursor in log to end
               
            }
            
        }

        private void brite_down_Click(object sender, RoutedEventArgs e)
        {
            if (temp_img != null) //Only do this if we already have a file loaded
            {
                if (brightness_val > -255) //Limit brightness to -255
                {
                    brightness_val = brightness_val - 5;
                }
                //Set the display image (main_img) to a transform of the edit image (temp_img)
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val ));
                this.text_box.AppendText("Brightness DOWN -5 to " + brightness_val + "\r"); //Log to screen
                this.text_box.ScrollToEnd();  //Move cursor in log to end
            }
        }

        private void zoom_out_Click(object sender, RoutedEventArgs e)
        {
            //Button listener for the Zoom OUT button

            //Create a transform group to zoom the image
            TransformGroup transformGroup = (TransformGroup)main_img.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            double zoom = -.2; // Scale of the zoom 1=100%
            transform.ScaleX += zoom; //Perform zoom on x and y axis (to preserve aspect ratio)
            transform.ScaleY += zoom;
            this.text_box.AppendText("Zooming OUT " + zoom*100 + " percent\r"); //Write to onscren log
            this.text_box.ScrollToEnd(); //Scrolll to end of log
        }

        private void zoom_in_Click(object sender, RoutedEventArgs e)
        {
            //Button listener for the Zoom OUT button

            //Create a transform group to zoom the image
            TransformGroup transformGroup = (TransformGroup)main_img.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];

            double zoom = .1; //Scale of the zoom 1=100%
            transform.ScaleX += zoom; //Perform zoom on x and y axis (to preserve aspect ratio)
            transform.ScaleY += zoom;
            this.text_box.AppendText("Zooming IN " + zoom * 100 + " percent\r"); //Write to onscren log
            this.text_box.ScrollToEnd(); //Scrolll to end of log
        }

        private void contrast_up_Click(object sender, RoutedEventArgs e)
        { //Button Listener for Contrast UP button
            if (temp_img != null) // only edit contrast if image is already loaded
            {
                if (contrast_val < 100)
                {
                    contrast_val = contrast_val + 5;
                }
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val));
                this.text_box.AppendText("Contrast UP 5 to " + contrast_val + "\r");
                this.text_box.ScrollToEnd();
            }
        }

        private void contrast_down_Click(object sender, RoutedEventArgs e)
        {
            if (temp_img != null)
            {
                if (contrast_val > -100)
                {
                    contrast_val = contrast_val - 5;
                }
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val));
                this.text_box.AppendText("Contrast DOWN 5 to " + contrast_val + "\r");
                this.text_box.ScrollToEnd();
            }
        }



        System.Drawing.Bitmap AdjustContrastBrightnessMatrix(System.Drawing.Bitmap img, int cont, int brite)
        {
            if (cont == 0 && brite== 0) // No change, so just return
                return img;

            float c = (float) (100 + cont) / 100F;
            float t = (1F - c) / 2F;
            float sb = (float)brite / 255F;
            float sb_help = 0;
            if (sb != 0)
            {
                sb_help = 1F;
            }

            float[][] colorMatrixElements =
                           { 
                                 new float[] {c,  0,  0,  0, 0},
                                 new float[] {0,  c,  0,  0, 0},
                                 new float[] {0,  0,  c,  0, 0},
                                 new float[] {0,  0,  0,  1, 0},
                                 new float[] {t+sb, t+sb, t+sb, sb_help, 1}
                           };

            System.Drawing.Imaging.ColorMatrix cm = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
            System.Drawing.Imaging.ImageAttributes imgattr = new System.Drawing.Imaging.ImageAttributes();
            System.Drawing.Rectangle rc = new System.Drawing.Rectangle(0, 0, img.Width, img.Height);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(img);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            imgattr.SetColorMatrix(cm);
            g.DrawImage(img, rc, 0, 0, img.Width, img.Height, System.Drawing.GraphicsUnit.Pixel, imgattr);

            imgattr.Dispose();
            g.Dispose();
            return img;
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                // return bitmap; <-- leads to problems, stream is closed/closing ...
                return new Bitmap(bitmap);
            }
        }

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using(MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
        }

        private void changeBrightness(double value)
        {
            if (temp_img != null) //Only do this if we already have a file loaded
            {
                if (brightness_val < 250) //Limit brightness to 255
                {
                    if (value > 0.2)
                    {
                        brightness_val += 5;
                    }
                    else if ( brightness_val > -250 && value < -0.2 )
                    {
                        brightness_val -= 5;
                    }
                }
                //Set the display image (main_img) to a transform of the edit image (temp_img)
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val));
                //this.text_box.AppendText("Brightness UP 5 to " + brightness_val + "\r"); //Log to screen
                //this.text_box.ScrollToEnd(); //Move cursor in log to end

            }
        }

        private void changeContrast(double value)
        {
            if (temp_img != null) //Only do this if we already have a file loaded
            {
                if (contrast_val < 98) //Limit brightness to 255
                {
                    if (value > 0.1)
                    {
                        contrast_val += 2;
                    }
                    else if (contrast_val > -98 && value < -0.1)
                    {
                        contrast_val -= 2;
                    }
                }
                //Set the display image (main_img) to a transform of the edit image (temp_img)
                main_img.Source = Bitmap2BitmapImage(AdjustContrastBrightnessMatrix(BitmapImage2Bitmap(temp_img), contrast_val, brightness_val));
                //this.text_box.AppendText("Brightness Changing\r"); //Log to screen
                //this.text_box.ScrollToEnd(); //Move cursor in log to end

            }
        }
        private void changeZoom(double value)
        {
            //Create a transform group to zoom the image
            TransformGroup transformGroup = (TransformGroup)main_img.RenderTransform;
            ScaleTransform transform = (ScaleTransform)transformGroup.Children[0];
            double zoom = 0;
            if (value > 0.2)
            {
                zoom = .05; //Scale of the zoom 1=100%
            }
            else if (value < -0.2)
            {
                zoom = -.05;
            }
            transform.ScaleX += zoom; //Perform zoom on x and y axis (to preserve aspect ratio)
            transform.ScaleY += zoom;
            //this.text_box.AppendText("Zooming\r"); //Write to onscren log
            //this.text_box.ScrollToEnd(); //Scrolll to end of log
        }
        
    }
}
