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
using Microsoft.Kinect;
using System.IO; 



namespace PositiveImagenes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private WriteableBitmap ColorImagenBitmap;
        private WriteableBitmap DepthImagenBitmap;
        private Int32Rect ColorImagenRect;
        private Int32Rect DepthImagenRect;
        private int ColorImagenStride;
        private int DepthImagenStride;
        private byte[] ColorImagenPixeles;
        private byte[] DepthImagenPixeles;
        private short[] DepthValores;
        bool grabacion = false;
        bool moverKinect = false;
        string path;
        List<WriteableBitmap> imagenesColor = new List<WriteableBitmap>();
        List<WriteableBitmap> imagenesDepth = new List<WriteableBitmap>(); 
  
        //:::::::::::::fin variables:::::::::::::::::::::::::::::::::::::::::::::::



        //:::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent(); 
        }
        //:::::::::::::Fin Constructor::::::::::::::::::::::::::::::::::::::::::::



        //:::::::::::::Evento load window, al realizarse se llaman las demas funciones:::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EncuentraInicializaKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);  
        }
        //:::::::::::::fin load window:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 



        //:::::::::::::Enseguida estan los metodos para desplegar los datos del Kinect:::::::::::::::::::::::::::::::::::::::::::::

        private void EncuentraInicializaKinect()
        {
            Kinect = KinectSensor.KinectSensors.FirstOrDefault(); 

            try
            { 
                if (Kinect.Status == KinectStatus.Connected)
                {
                    Kinect.ColorStream.Enable();
                    Kinect.DepthStream.Enable();
                    Kinect.DepthStream.Range = DepthRange.Near; 
                    Kinect.Start(); 
                }
            }
            catch 
            {
                MessageBox.Show("El dispositivo Kinect no se encuentra conectado", "Error Kinect"); 
            }
        } //fin EncuentraKinect() 


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            Gray.Source = PollColor();
            depth.Source = PollDepth();
        } //fin CompositionTarget_Rendering()


        private WriteableBitmap PollColor()
        {
            if (this.Kinect != null)
            {
                ColorImageStream ColorStream = this.Kinect.ColorStream;
                this.ColorImagenBitmap = new WriteableBitmap(ColorStream.FrameWidth, ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this.ColorImagenRect = new Int32Rect(0, 0, ColorStream.FrameWidth, ColorStream.FrameHeight);
                this.ColorImagenStride = ColorStream.FrameWidth * ColorStream.FrameBytesPerPixel;
                this.ColorImagenPixeles = new byte[ColorStream.FramePixelDataLength];

                try
                {
                    using (ColorImageFrame frame = this.Kinect.ColorStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.ColorImagenPixeles);
                            this.ColorImagenBitmap.WritePixels(this.ColorImagenRect, this.ColorImagenPixeles, this.ColorImagenStride, 0);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            if (grabacion == true)
            {
                imagenesColor.Add(ColorImagenBitmap); 
            }

            return ColorImagenBitmap;
        }//fin PollColor()  


        private WriteableBitmap PollDepth()
        {
            if (this.Kinect != null)
            {
                DepthImageStream DepthStream = this.Kinect.DepthStream;
                this.DepthImagenBitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                this.DepthImagenRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
                this.DepthImagenStride = DepthStream.FrameWidth * 4;
                this.DepthValores = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength * 4];

                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.DepthValores);

                            int index = 0;
                            for (int i = 0; i < frame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValores[i] >> 3;

                                if (valorDistancia == this.Kinect.DepthStream.UnknownDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                    DepthImagenPixeles[index + 1] = 0;
                                    DepthImagenPixeles[index + 2] = 0;
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                    DepthImagenPixeles[index + 1] = 0;
                                    DepthImagenPixeles[index + 2] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                    DepthImagenPixeles[index + 1] = byteDistancia;
                                    DepthImagenPixeles[index + 2] = byteDistancia;
                                }
                                index = index + 4;
                            }

                            this.DepthImagenBitmap.WritePixels(this.DepthImagenRect, this.DepthImagenPixeles, this.DepthImagenStride, 0);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }

            }

            if (grabacion == true)
            {
                imagenesDepth.Add(DepthImagenBitmap); 
            }

            return DepthImagenBitmap;
        }//fin PollDepth()
        //:::::::::::::Fin de los metodos para manipular los datos del Kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::: 



        //:::::::::::::Grabacion de los datos::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Positivas_Checked(object sender, RoutedEventArgs e)
        {  
            Negativas.IsEnabled = false;
            path = @"C:\MyDataBase\Positivas\";
        }


        private void Negativas_Checked(object sender, RoutedEventArgs e)
        {
            Positivas.IsEnabled = false; 
            path = @"C:\MyDataBase\Negativas\";
        }

        
        private void Grabar_Click(object sender, RoutedEventArgs e)
        {
            grabacion = true;
            empezarGrabar.IsEnabled = true; 
        }


        private void empezarGrabar_Click(object sender, RoutedEventArgs e)
        {
            string pathRgb = path + "RGB\\";
            string pathDepth = path + "Depth\\";  

            Grabar.IsEnabled = false; 
            grabacion = false;

            RecordImagenes(imagenesColor, pathRgb);
            RecordImagenes(imagenesDepth, pathDepth);
            Grabar.IsEnabled = true;

            imagenesColor.Clear();
            imagenesDepth.Clear(); 
        }


        private void RecordImagenes(List<WriteableBitmap> ListaImagenes, string directorio)
        {
            string name;
            int i = 0; 

            foreach (WriteableBitmap wBitmap in ListaImagenes)
            {
                name = String.Format("{0}{1}{2}", directorio, "img"+i.ToString(), ".jpg"); 

                using (FileStream stream = new FileStream(name, FileMode.Create))
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(wBitmap));
                    encoder.Save(stream);
                    stream.Close(); 
                } 

                i++;
            }
        } 
        //:::::::::::::Termina Grabacion de los datos:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        


        //:::::::::::::Mover el tilt del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            moverKinect = true;
            anguloSlider.Value = (double)Kinect.ElevationAngle;
            anguloSlider.IsEnabled = true; 
        }


        private void anguloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (moverKinect)
                Kinect.ElevationAngle = (int)anguloSlider.Value; 
        }
        //:::::::::::::termina mover el tilt::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        

        //:::::::::::::Apaga el sensor cuando se cierra la ventana:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.Stop(); 
        }



        //:::::::::::termina unload window:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        


    }//Termina Class
}//Termina Namespace
