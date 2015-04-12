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
using System.Drawing; 

namespace DataBaseCapture
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private WriteableBitmap ImagenWriteablebitmap;
        private Int32Rect WriteablebitmapRect;
        private int WriteablebitmapStride;
        private DepthImageStream DepthStream;
        private byte[] DepthImagenPixeles;
        private short[] DepthValoresStream;
        bool moverK = false; 
        //private Image<Gray, Byte> depthFrameKinect;
        //private CascadeClassifier haar1;
        //dos manos fondo complicado no iluminacion 
        //private string path1 = @"C:\imagenClassifiersWitoutNoise\BackgroundSimple\Ilumination\twoHand\Noise\";
        //private string path2 = @"C:\imagenClassifiersWitoutNoise\BackgroundSimple\Ilumination\twoHand\NoNoise\3\";
        //:::::::::::::fin variables::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //:::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        }
        //:::::::::::::Constructor::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Call Methods::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EncuentraInicializaKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
        }
        //:::::::::::::end event::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //:::::::::::::Enseguida estan los metodos para desplegar los datos de profundidad de Kinect:::::::::::::::::::::::::::::::
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
            //imagenClasificar = PollDepth();
            //Display the result of the classifier, so the bytes of the imagen
            //are converted in a wriablebitmap.  
            //imageKinect.Source = imagetoWriteablebitmap(imageHaar1);

            displayDepth.Source = PollDepth(); 
        } //fin CompositionTarget_Rendering()  

        private WriteableBitmap PollDepth()
        {
            if (this.Kinect != null)
            {
                this.DepthStream = this.Kinect.DepthStream;
                this.DepthValoresStream = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength];
                //this.depthFrameKinect = new Image<Gray, Byte>(DepthStream.FrameWidth, DepthStream.FrameHeight);  
                Array.Clear(DepthImagenPixeles, 0, DepthImagenPixeles.Length); 

                this.ImagenWriteablebitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Gray8, null);
                this.WriteablebitmapRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
                this.WriteablebitmapStride = DepthStream.FrameWidth;

                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.DepthValoresStream);

                            int index = 0;
                            for (int i = 0; i < frame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValoresStream[i] >> 3;

                                if ((valorDistancia <= 1000))
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                }

                                /*if (valorDistancia == this.Kinect.DepthStream.UnknownDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                }
                                */
                                index++; //= index + 4;
                            }

                            //depthFrameKinect.Bytes = DepthImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            ImagenWriteablebitmap.WritePixels(WriteablebitmapRect,DepthImagenPixeles,WriteablebitmapStride,0);

            return ImagenWriteablebitmap; 
            //return depthFrameKinect;
        }//fin PollDepth() 


        //:::::::::::::Mover el tilt del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void moverKinect_Checked(object sender, RoutedEventArgs e)
        {
            moverK = true;
            anguloSlider.Value = (double)Kinect.ElevationAngle;
            anguloSlider.IsEnabled = true;
        }


        private void anguloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (moverK)
                Kinect.ElevationAngle = (int)anguloSlider.Value;
        }
        //:::::::::::::termina mover el tilt::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //::::::::::::Method to stop de Kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.Stop();
        }




    }//end class
}//end namespace
