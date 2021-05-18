using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;
using System.Collections.ObjectModel;

///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    /// 
    public struct RGBPixel
    {
        public byte red, green, blue;
    }

    
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    public struct ColorNode
    {
        public int index;
        public double distance;
        public bool isVisited;
        public int predessesor;
        public RGBPixel Color;
    }
    
    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        public static List<RGBPixel> ColorsList = new List<RGBPixel>();
        public static RGBPixel[] palette = new RGBPixel[int.Parse(MainForm.temp.nudMaskSize.Value.ToString())];
        public static ColorNode[] sortedmst = new ColorNode[ColorsList.Count];
        public static bool[ , , ] DistinctArr;
        public static ColorNode[] FinalMST;
        public static double sum;
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            sum = 0;
            DistinctArr = new bool[256, 256, 256];
            ColorsList.Clear();
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                        //Add Distinct Colors to ColorsList
                        if(DistinctArr[Buffer[y,x].red , Buffer[y, x].green , Buffer[y, x].blue] == false)
                        {
                            DistinctArr[Buffer[y, x].red, Buffer[y, x].green, Buffer[y, x].blue] = true;
                            ColorsList.Add(Buffer[y,x]);
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }
            MSTCal();
            clustring();
            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }
            return Filtered;
        }
        public static void MSTCal()
        {
            sum = 0;
            FinalMST = new ColorNode[ColorsList.Count];
            Collection<ColorNode> UnVisited = new Collection<ColorNode>();
            pqueue PQueue = new pqueue();
            FinalMST[0].predessesor = -1;
            FinalMST[0].distance = 0;
            FinalMST[0].index = 0;
            FinalMST[0].isVisited = false;
            UnVisited.Add(FinalMST[0]);
            PQueue.Enqueue(FinalMST[0].distance, FinalMST[0]);
            for (int i = 1; i < ColorsList.Count; i++)
            {
                FinalMST[i].predessesor = -1;
                FinalMST[i].distance = 1000;
                FinalMST[i].index = i;
                FinalMST[i].isVisited = false;
                UnVisited.Add(FinalMST[i]);
                PQueue.Enqueue(FinalMST[i].distance, FinalMST[i]);
            }
            for (int i = 0; i < ColorsList.Count; i++)
            {
                ColorNode temp = PQueue.Dequeue();
                int currentindex = temp.index;
                FinalMST[temp.index].isVisited = true;
                UnVisited.Remove(temp);
                sum += FinalMST[currentindex].distance;
                for (int j = 0; j < UnVisited.Count; j++)
                {
                    double Diffrence = Math.Sqrt(Math.Pow(( ColorsList[currentindex].red- ColorsList[UnVisited[j].index].red), 2) +
                    Math.Pow((ColorsList[currentindex].green - ColorsList[UnVisited[j].index].green), 2) +
                    Math.Pow((ColorsList[currentindex].blue - ColorsList[UnVisited[j].index].blue), 2));
                    if (Diffrence != 0 && Diffrence < FinalMST[UnVisited[j].index].distance)
                    {
                        FinalMST[UnVisited[j].index].predessesor = currentindex;
                        FinalMST[UnVisited[j].index].distance = Diffrence;
                        PQueue.UpdatePriority(FinalMST[UnVisited[j].index], Diffrence);
                        UnVisited[j] = FinalMST[UnVisited[j].index];
                    }
                }
            }
            sortedmst = FinalMST;
            MessageBox.Show("Total Number of colors : " + ColorsList.Count + Environment.NewLine + "Total MST :" + sum.ToString());
        }
        
        public static List<List<RGBPixel>> clustring() {
            //bubble sorting O(K*D)
           
            int n = sortedmst.Length;
            int k = int.Parse(MainForm.temp.nudMaskSize.Value.ToString());
            int redtemp,greentemp,bluetemp;

            List<List<RGBPixel>> Clustrs= new List<List<RGBPixel>>();
            List<RGBPixel> clustercolors = new List<RGBPixel>();
            
            for (int i = 0; i < k - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (sortedmst[j].distance > sortedmst[j + 1].distance)
                    {
                        // swap temp and arr[i] 
                        ColorNode temp = sortedmst[j];
                        sortedmst[j] = sortedmst[j + 1];
                        sortedmst[j + 1] = temp;
                    }
                }
            }
            
            //cutting long edges O(K)
            for (int i = 0; i < k - 1; i++)
            {
                sortedmst[n-1].predessesor = -1;
                n--;
            }
            //get all colors of 1st clusters
           
            clustercolors.Add(ColorsList[0]);
            redtemp= ColorsList[0].red;
            greentemp = ColorsList[0].green;
            bluetemp = ColorsList[0].blue;
            for (int j = 0; j < n; j++)
            {
                if (sortedmst[j].predessesor == 0)
                {
                    clustercolors.Add(ColorsList[sortedmst[j].index]);
                    redtemp += ColorsList[sortedmst[j].index].red;
                    greentemp += ColorsList[sortedmst[j].index].green;
                    bluetemp += ColorsList[sortedmst[j].index].blue;
                }
            }
            
            palette[0].red = byte.Parse((redtemp / clustercolors.Count).ToString());
            palette[0].green = byte.Parse((greentemp / clustercolors.Count).ToString());
            palette[0].blue = byte.Parse((bluetemp / clustercolors.Count).ToString());
            Clustrs.Add(clustercolors);

            clustercolors = new List<RGBPixel>();
            n = sortedmst.Length -1;

            for(int i = 1; i < k ; i++)
            {
                clustercolors.Add(ColorsList[sortedmst[n].index]);
                redtemp = ColorsList[sortedmst[n].index].red;
                greentemp = ColorsList[sortedmst[n].index].green;
                bluetemp= ColorsList[sortedmst[n].index].blue;
                for (int j = 0; j < n; j++)
                {
                    if (sortedmst[j].predessesor == sortedmst[n].index)
                    {
                        clustercolors.Add(ColorsList[sortedmst[j].index]);
                        redtemp += ColorsList[sortedmst[j].index].red;
                        greentemp += ColorsList[sortedmst[j].index].green;
                        palette[i].blue += ColorsList[sortedmst[j].index].blue;
                    }
                }
                n--;
                Clustrs.Add(clustercolors);
                palette[i].red = byte.Parse((redtemp / clustercolors.Count).ToString());
                palette[i].green = byte.Parse((greentemp / clustercolors.Count).ToString());
                palette[i].blue = byte.Parse((bluetemp / clustercolors.Count).ToString());

                clustercolors = new List<RGBPixel>();
            }

                    return Clustrs;
        }
        

    }
}
