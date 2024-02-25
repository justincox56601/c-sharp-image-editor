using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Remoting;
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


namespace image_editor_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapSource originalImage;
        private BitmapSource newImage;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void openImage(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg|All files (*.*)|*.*";
            if(openFileDialog.ShowDialog() == true)
            {
                BitmapImage img = new BitmapImage(new Uri(openFileDialog.FileName));
                img_box.Source = img;
                originalImage = img;
                newImage = img;
            }
        }

        public void saveImage(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save image as ";
            saveFileDialog.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            saveFileDialog.FileName = "image";
            saveFileDialog.DefaultExt = ".jpg";
 
            if (newImage != null && saveFileDialog.ShowDialog() == true)
            {

                JpegBitmapEncoder encoder = new JpegBitmapEncoder(); // This only saves as a jpg.  I should look up some day how to make this changeable
                encoder.Frames.Add(BitmapFrame.Create(newImage));
                using (var stream = saveFileDialog.OpenFile())
                {
                    encoder.Save(stream);
                }

            }
        }

        public void reloadImage(object sender, EventArgs e)
        {
            img_box.Source = originalImage;
            newImage = originalImage;
        }

        private byte[] getImageByteArray(BitmapSource image)
        {
            //returned pixel array is 4 entries per pixle [blue, green, red, alpha]
     
            int stride = (image.PixelWidth * image.Format.BitsPerPixel+7) / 8;
            byte[] pixels = new byte[stride * image.PixelHeight];
            image.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private BitmapSource convertBytesToImage(byte[] buffer, int width, int height, double dpiX, double dpiY, PixelFormat pixelFormat, int stride)
        {
            return BitmapSource.Create(width, height, dpiX, dpiY, pixelFormat, null, buffer, stride);
        }
        
        private void addEffect(IEffect effect)
        {
            //really would like to figure out how to bind the effect to the button, can skip all of the other button methods
            byte[] pixels = this.getImageByteArray(newImage);
            byte[] newPixels = effect.apply(pixels, newImage.PixelWidth, newImage.PixelHeight);

            BitmapSource bitmapImage = BitmapSource.Create(
                newImage.PixelWidth,
                newImage.PixelHeight,                
                96d,
                96d,
                PixelFormats.Pbgra32, //no idea if this is correct or not, but it is working for now
                null,
                newPixels,
                (newImage.PixelWidth * newImage.Format.BitsPerPixel + 7) / 8
            );

            newImage = bitmapImage;
            img_box.Source = bitmapImage;
            Trace.WriteLine("finished effect");

        }
        public void greyscale(object sender, EventArgs e)
        {
            addEffect(new GreyScale());
        }

        private void sepia(object sender, RoutedEventArgs e)
        {
            addEffect(new Sepia());
        }

        private void blur(object sender, RoutedEventArgs e)
        {
            addEffect(new Blur());
        }
    }

    interface IEffect
    {
        byte[] apply(byte[] bytes, int width, int height);
    }

    public class GreyScale: IEffect
    {
        public byte[] apply(byte[] bytes, int width, int height)
        {
            for (int i = 0; i < bytes.Length; i += 4)
            {
                int blue = bytes[i];
                int green = bytes[i + 1];
                int red = bytes[i + 2];

                int avg = (blue + green + red) / 3;

                bytes[i] = (byte)avg;
                bytes[i + 1] = (byte)avg;
                bytes[i + 2] = (byte)avg;
            }

            return bytes;
        }
    }

    public class Sepia : IEffect
    {
        public byte[] apply(byte[] bytes, int width, int height)
        {
            //bytes come in sets of 4.  BGRA
            //sepia filter
            for(int i= 0; i < bytes.Length;i += 4)
            {
                int sRed = (int)(0.189 * bytes[i] + 0.769 * bytes[i+1] + 0.393 * bytes[i+2]);
                int sGreen = (int)(0.168 * bytes[i] + 0.686 * bytes[i + 1] + 0.349 * bytes[i + 2]);
                int sBlue = (int)(0.131 * bytes[i] + 0.534 * bytes[i + 1] + 0.272 * bytes[i + 2]);

                bytes[i] = (byte)Math.Min(255, sBlue);
                bytes[i + 1] = (byte)Math.Min(255, sGreen);
                bytes[i + 2] = (byte)Math.Min(255, sRed);
            }

            return bytes;
        }
    }

    public class Blur: IEffect 
    {
        public byte[] apply(byte[] bytes, int width, int height)
        {
            byte[] newBytes = new byte[bytes.Length];
            int radius = 3;
            int step = 4;
            double weight = Math.Pow(2*radius+1, 2);

            for(int i = 0; i < bytes.Length; i ++)
            {
                double sum = 0;
                for(int j = -radius; j <= radius; j++)
                {
                    for(int k=-radius; k <= radius; k++)
                    {
                        int tempIndex = i + (j * step) + (k * step * width);
                        if(tempIndex >= 0 && tempIndex < bytes.Length)
                        {
                            sum += bytes[tempIndex];
                        }
                        
                    }
                }

                newBytes[i] = (byte)(sum / weight);
            }

            return newBytes;
        }
    }
}