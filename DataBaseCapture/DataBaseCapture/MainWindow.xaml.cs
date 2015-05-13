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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;  

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
        private DepthImagePixel[] DepthPixels; 
        private DepthImageStream DepthStream;
        private int WriteablebitmapStride;
        private byte[] DepthImagenPixeles;
        private short[] DepthValoresStream;
        private Image<Gray, Byte> depthFrameKinect;
        
        private int minDepth;
        private int maxDepth;        
        
        private bool moverK = false;
        private string path;
        private string nombre = "darien";
        private bool grabar = false; 
        private int i = 30;
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
                    //Kinect.ColorStream.Enable();
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
            Image<Bgra, Byte> imageKinectBGR;
            Image<Gray, Byte> imagenKinectGray; 

            imagenKinectGray = PollDepth();
            imageKinectBGR = imagenKinectGray.Convert<Bgra, Byte>(); 

            displayDepth.Source =imagetoWriteablebitmap(imagenKinectGray);

            if (grabar &&  i<130)
            {
                guardaimagen(imagenKinectGray, path, nombre, i-30);
                i++;
            } 

        } //fin CompositionTarget_Rendering()  

        private Image<Gray,Byte> PollDepth()
        {
            Image<Bgr, Byte> depthFrameKinectBGR = new Image<Bgr, Byte>(640,480); 
            

            if (this.Kinect != null)
            {
                this.DepthStream = this.Kinect.DepthStream;
                //this.DepthValoresStream = new short[DepthStream.FramePixelDataLength];
                this.DepthPixels = new DepthImagePixel[DepthStream.FramePixelDataLength]; 
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength*sizeof(int)];
                this.depthFrameKinect = new Image<Gray, Byte>(DepthStream.FrameWidth, DepthStream.FrameHeight);
                
                Array.Clear(DepthImagenPixeles, 0, DepthImagenPixeles.Length); 
                
                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyDepthImagePixelDataTo(this.DepthPixels); 

                            minDepth = 400;
                            maxDepth = 3000; 

                            int index = 0;
                            for (int i = 0; i < DepthPixels.Length; ++i)
                            {
                                short depth = DepthPixels[i].Depth;
 
                                /*int valorDistancia = DepthValoresStream[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                                if ((valorDistancia <= 800))
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5)); 
                                    DepthImagenPixeles[index] = byteDistancia;
                                }
                  
                                index++; //= index + 4;
                                 */ 

                                byte intensity = (byte)( (depth >=minDepth) && (depth<=maxDepth) ? depth : 0);  

                                DepthImagenPixeles[index++] =intensity; 
                                DepthImagenPixeles[index++] =intensity; 
                                DepthImagenPixeles[index++] =intensity;

                                ++index; 
                            }

                            depthFrameKinectBGR.Bytes = DepthImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            depthFrameKinect = depthFrameKinectBGR.Convert<Gray, Byte>(); 
            depthFrameKinect = removeNoise(depthFrameKinect,3); 

            return depthFrameKinect;
        }//fin PollDepth() 


        //:::::::::::::Method to convert a byte[] to a writeablebitmap::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private WriteableBitmap imagetoWriteablebitmap(Image<Gray, Byte> frameHand)
        {
            Image<Bgra, Byte> frameBGR = new Image<Bgra, Byte>(DepthStream.FrameWidth, DepthStream.FrameHeight); 
            byte[] imagenPixels = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight];

            this.ImagenWriteablebitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
            this.WriteablebitmapRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
            this.WriteablebitmapStride = DepthStream.FrameWidth*4;

            frameBGR = frameHand.Convert<Bgra, Byte>();
            imagenPixels = frameBGR.Bytes;

            ImagenWriteablebitmap.WritePixels(WriteablebitmapRect, imagenPixels, WriteablebitmapStride, 0);

            return ImagenWriteablebitmap;
        }//end 


        //::::::::::::Method to remove the noise, using median filters::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private Image<Gray, Byte> removeNoise(Image<Gray, Byte> imagenKinet, int sizeWindow)
        {
            Image<Gray, Byte> imagenSinRuido;

            imagenSinRuido = imagenKinet.SmoothMedian(sizeWindow);

            return imagenSinRuido;
        }//endremoveNoise 


        //:::::::::::::Method to saves the images with tha detection ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        private void guardaimagen(Image<Gray, Byte> imagen, string path, string nombre, int i)
        {
            imagen.Save(path + nombre + i.ToString() + ".png");
        }


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

        private void iluminacion_Checked(object sender, RoutedEventArgs e)
        {
            path = @"C:\DataBaseHand\ilumination\";
            grabar = true; 
        }

        private void noiluminacion_Checked(object sender, RoutedEventArgs e)
        {
            path = @"C:\DataBaseHand\noilumination\";
            grabar = true; 
        } 

        //::::::::::::Method to stop de Kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.DepthStream.Disable();
            Kinect.Stop();
        }//end 






    }//end class
}//end namespace
