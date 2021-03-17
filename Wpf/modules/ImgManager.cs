using Alturos.Yolo.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AI_Vid_Demo
{
    public enum ImageProcessTypes
    {
        Normal = -1,
        InvertY = 0,
        ImageProc_B2 = 1,
        ImageProc_B3 = 2,
        CvtBGR2GRAY2BGR = 3,
        DoCLAHEtoSaturation = 4,
        ImageProc_B6 = 5
    }

    public struct MatchingResult
    {
        public int CurrentDistance;
        public int AccumulatedDistance;
        public int[] DistanceList;
        public int Length;
        public int Index;
        public int SaveDistance;

        public MatchingResult(int currentDistance, int accumulatedDistance, int[] distanceList, int length, int index, int save)
        {
            CurrentDistance = currentDistance;
            AccumulatedDistance = accumulatedDistance;
            DistanceList = distanceList;
            Length = length;
            Index = index;
            SaveDistance = save;
        }
    }

    public static class ImgManager
    {
        private static readonly string IMG_storage_directory;
        private static Mat[] IMG_img_set;
        private static readonly Dictionary<string, Scalar> ClassColors;
        private static readonly float XResizeCoefficient;
        private static readonly float YResizeCoefficient;
        private static readonly int MatchingLength;
        #region
        private static MatchingResult[] MatchingResult;
        #endregion
        private static readonly int NumFeatures;
        private static readonly ORB Orb;
        private static Mat PriorMask;
        private static Mat CurrentMask;
        private static KeyPoint[] PriorKeyPoints;
        private static KeyPoint[] CurrentKeyPoints;
        private static readonly BFMatcher BFMatcher;
        private static Point2f[] SrcCoords;
        private static Point2f[] DstCoords;
        //private static readonly double RansacReprojThreshold;
        private static readonly int ErrorDistance;
        private static readonly List<Func<Mat, Mat>> ImageProcessor;
        private static readonly byte Threshold;
        private static readonly double CLAHEClipLimit;
        private static readonly Size CLAHETileGridSize;
        private static int Sp;
        private static int Ep;
        public static int[] Asp { get; }
        public static int[] Aep { get; }
        private static readonly int mappingRange;
        private static readonly float[] Lut;
        private static Mat DebugLut;
        static int pre_index = 0;
        private static int add_matching = 0;
        private static Mat[] diff_list = new Mat[5];
        private static int diff_index = 5;

        private static Mat resizedPriorImage = new Mat();
        private static Mat resizedCurrentImage = new Mat();
        private static Mat priordiff = new Mat();
        private static Mat currentdiff = new Mat();
        private static Mat[] split_prior = new Mat[3];
        private static Mat[] split_current = new Mat[3];
        private static Mat subimg_1, subimg_2, subimg_3;
        private static Mat subimg_1_pal;
        private static Stopwatch[] five = new Stopwatch[5];
        private static Mat BW = new Mat();

        public static int ImageProcB6State
        {
            get; set;
        }
        static ImgManager()
        {
            IMG_storage_directory = @"C:\DATA";
            IMG_img_set = new Mat[1];
            ClassColors = new Dictionary<string, Scalar>();
            SetClassColors();
            //ResizingCoefficient = 0.25F;
            XResizeCoefficient = 1.0F;
            YResizeCoefficient = 0.2F;
            MatchingLength = 4;

            for (int i = 0; i < 5; i++)
                five[i] = new Stopwatch();

            for (int i = 0; i < diff_list.Length; i++)
                diff_list[i] = new Mat();



            NumFeatures = 500;
            Orb = ORB.Create(NumFeatures);
            PriorMask = new Mat();
            CurrentMask = new Mat();
            PriorKeyPoints = new KeyPoint[NumFeatures];
            CurrentKeyPoints = new KeyPoint[NumFeatures];
            BFMatcher = new BFMatcher(NormTypes.Hamming, true);
            SrcCoords = new Point2f[4];
            DstCoords = new Point2f[4];
            //RansacReprojThreshold = 5.0D;
            ErrorDistance = 100000;
            ImageProcessor = new List<Func<Mat, Mat>>();
            Threshold = 125;
            CLAHEClipLimit = 2.5F;
            CLAHETileGridSize = new Size(8, 8);
            Sp = 220;
            Ep = 240;
            ImageProcB6State = 4;
            Asp = new int[] { 0, 100, 150, 200, 220 };
            Aep = new int[] { 100, 150, 200, 240, 240 };
            mappingRange = 250;
            Lut = new float[256];
            for (int index = 1; index < Sp; index++)
            {
                int element = (int)(Lut[index - 1] + (255.0F - mappingRange) / (256.0F - Ep + Sp));
                Lut[index] = (byte)element;
                //Lut[index] = (int)(Lut[index - 1] + (255.0F - mappingRange) / (256.0F - Ep + Sp));
            }
            for (int index = Sp; index < Ep; index++)
            {
                int element = (int)(Lut[index - 1] + (mappingRange) / (Ep + Sp));
                Lut[index] = (byte)element;
                //Lut[index] = (int)(Lut[index - 1] + (mappingRange) / (Ep + Sp));
            }
            for (int index = Ep; index < 256; index++)
            {
                int element = (int)(Lut[index - 1] + (255.0F - mappingRange) / (256.0F - Ep + Sp));
                Lut[index] = (byte)element;
            }
            SetImageProcessor();
        }

        private static void SetClassColors()
        {
        }

        private static void SetImageProcessor()
        {
            ImageProcessor.Add(item: InvertY);
            ImageProcessor.Add(item: ImageProc_B2);
            ImageProcessor.Add(item: ImageProc_B3);
            ImageProcessor.Add(item: CvtBGR2GRAY2BGR);
            ImageProcessor.Add(item: DoCLAHEtoSaturation);
            ImageProcessor.Add(item: ImageProc_B6);
        }

        public static int GetErrorDistance()
        {
            return ErrorDistance;
        }

        public static Mat IMG_post_process(in Mat source_image, in IEnumerable<YoloItem> detection_result, int distance)
        {
            Mat resultImage = new Mat();
            try
            {
                resultImage = source_image.Clone();
                foreach (YoloItem resultItem in detection_result)
                {
                    Point leftBottom = new Point(x: resultItem.X, y: resultItem.Y);
                    Point rightTop = new Point(x: resultItem.X + resultItem.Width, y: resultItem.Y + resultItem.Height);
                    Cv2.Rectangle(img: resultImage, pt1: leftBottom, pt2: rightTop, color: ClassColors[resultItem.Type]);
                }
                return resultImage;
            }
            catch (Exception exception)
            {
                DateTime currentDate = DateTime.Now;
                Console.WriteLine(value: exception.ToString());
                return resultImage;
            }
        }

        public static int IMG_calculate_movement_distance(in Mat prior_image, in Mat current_image)
        {
            int range = 10;
            int mini = range;
            try
            {
                if (BW.Empty())
                {
                    Cv2.Resize(prior_image, resizedPriorImage, new OpenCvSharp.Size(1920, 108));
                    Cv2.CvtColor(resizedPriorImage, resizedPriorImage, ColorConversionCodes.BGR2YCrCb);
                    split_prior = resizedPriorImage.Split();
                    priordiff = Cv2.Abs(split_prior[1] - new Scalar(127));
                    Cv2.Threshold(priordiff, priordiff, 20, 255, ThresholdTypes.Binary);
                    BW = priordiff;
                    return 0;
                }
                else
                {
                    Cv2.Resize(prior_image, resizedPriorImage, new OpenCvSharp.Size(1920, 108));
                    Cv2.CvtColor(resizedPriorImage, resizedPriorImage, ColorConversionCodes.BGR2YCrCb);
                    split_prior = resizedPriorImage.Split();
                    priordiff = Cv2.Abs(split_prior[1] - new Scalar(127));
                    Cv2.Threshold(priordiff, priordiff, 20, 255, ThresholdTypes.Binary);
                    Rect rect_1 = new Rect(range, 0, priordiff.Width - 2 * range, priordiff.Height);
                    long[] result = new long[1 + 2 * range];
                    subimg_1_pal = new Mat(BW, rect_1);
                    for (int i = 0; i < 1 + range * 2; i++)
                    {
                        Mat subimg_2_pal, subimg_3_pal;
                        Rect rect_2 = new Rect(i, 0, priordiff.Width - 2 * range, priordiff.Height);
                        subimg_2_pal = new Mat(priordiff, rect_2);
                        subimg_3_pal = new Mat(subimg_1_pal.Size(), subimg_1_pal.Type());

                        Cv2.Absdiff(subimg_1_pal, subimg_2_pal, subimg_3_pal);

                        int error = Cv2.CountNonZero(subimg_3_pal);
                        result[i] = error;
                    }

                    mini = Array.IndexOf(result, result.Min());
                    MatchingResult[0].AccumulatedDistance += (mini - range);
                    MatchingResult[1].AccumulatedDistance += (mini - range);
                    BW = priordiff;
                    return mini - range;
                }
            }
            catch (Exception exception)
            {
                if (diff_index > 1005)
                    diff_index = 5 + diff_index % 5;
                diff_index++;
                DateTime current_date = DateTime.Now;
                Console.WriteLine("matching try-catch");
                return -1;
                //return distance;
            }
        }


        public static void UpdateMatchingResult(int index)
        {
            MatchingResult[index].DistanceList[MatchingResult[index].Index] = MatchingResult[index].AccumulatedDistance;
            MatchingResult[index].Index = (MatchingResult[index].Index + 1) % MatchingLength;
            MatchingResult[index].SaveDistance += MatchingResult[index].AccumulatedDistance;
            MatchingResult[index].AccumulatedDistance = 0;
        }

        public static int GetAddMatching()
        {
            return MatchingResult[0].DistanceList[(MatchingResult[0].Index + 1) % MatchingLength] + MatchingResult[0].DistanceList[(MatchingResult[0].Index + 2) % MatchingLength];
        }

        public static void ResetAddMatching()
        {
            add_matching = 0;
        }

        public static int GetSaveDistance(int index)
        {
            return MatchingResult[index].SaveDistance;
        }

        public static void ResetSaveDistance(int index)
        {
            MatchingResult[index].SaveDistance = 0;
        }

        public static void UpdateMatchingResult_2(int index)
        {
            for (int i = 0; i < MatchingLength; i++)
                MatchingResult[index].DistanceList[i] = 0;
            MatchingResult[index].DistanceList[MatchingResult[index].Index] = MatchingResult[index].AccumulatedDistance;
            MatchingResult[index].Index = (MatchingResult[index].Index + 1) % MatchingLength;
            MatchingResult[index].SaveDistance += MatchingResult[index].AccumulatedDistance;
            MatchingResult[index].AccumulatedDistance = 0;
        }

        public static int GetMatchingResult(int index)
        {
            int result = 0;
            result = (MatchingResult[index].AccumulatedDistance
                + MatchingResult[index].DistanceList[(MatchingResult[index].Index + 2) % MatchingLength]
                + MatchingResult[index].DistanceList[(MatchingResult[index].Index + 3) % MatchingLength]);
            return result;
        }

        public static int GetAccumulatedDistance(int index)
        {
            return MatchingResult[index].AccumulatedDistance;
        }

        public static Mat ImageProcess(in Mat source_image, int type)
        {
            try
            {
                if ((type >= 0) && (type <= 5))
                {
                    return ImageProcessor[type](source_image);
                }
                else
                {
                    return source_image; //source_image;
                }
            }
            catch (Exception exception)
            {
                DateTime currentDate = DateTime.Now;
                Console.WriteLine(value: exception.ToString());
                Mat empty = new Mat();
                return empty;
            }
        }

        private static Mat InvertY(Mat source_image)
        {
            Mat res = new Mat();
            Cv2.CvtColor(src: source_image, dst: res, code: ColorConversionCodes.BGR2YUV);
            Mat[] channelSplit = Cv2.Split(res);
            Cv2.BitwiseNot(channelSplit[0], channelSplit[0]);
            Cv2.Merge(channelSplit, res);
            Cv2.CvtColor(src: res, dst: res, code: ColorConversionCodes.YUV2BGR);
            return res;
        }

        private static unsafe Mat ImageProc_B2(Mat source_image)
        {
            Mat grayImage = new Mat();
            Mat res = source_image.Clone();
            Cv2.CvtColor(src: source_image, dst: grayImage, code: ColorConversionCodes.BGR2GRAY);
            for (int rowIndex = 0; rowIndex < grayImage.Rows; rowIndex++)
            {
                Byte* grayImagePtr = (Byte*)grayImage.Ptr(rowIndex).ToPointer();
                Vec3b* bgrImagePtr = (Vec3b*)res.Ptr(rowIndex).ToPointer();
                for (int columnIndex = 0; columnIndex < res.Cols; columnIndex++)
                {
                    byte grayImagePixel = grayImagePtr[columnIndex];
                    if (grayImagePixel < Threshold)
                    {
                        bgrImagePtr[columnIndex].Item0 = (byte)Threshold;
                        bgrImagePtr[columnIndex].Item1 = (byte)Threshold;
                        bgrImagePtr[columnIndex].Item2 = (byte)Threshold;
                    }
                }
            }
            return res;
        }

        private unsafe static Mat ImageProc_B3(Mat source_image)
        {
            Mat grayImage = new Mat();
            Mat res = new Mat();
            Cv2.CvtColor(src: source_image, dst: grayImage, code: ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(src: grayImage, dst: res, code: ColorConversionCodes.GRAY2BGR);
            for (int rowIndex = 0; rowIndex < grayImage.Rows; rowIndex++)
            {
                Byte* grayImagePtr = (Byte*)grayImage.Ptr(rowIndex).ToPointer();
                Vec3b* srcImagePtr = (Vec3b*)source_image.Ptr(rowIndex).ToPointer();
                Vec3b* resImagePtr = (Vec3b*)res.Ptr(rowIndex).ToPointer();
                for (int columnIndex = 0; columnIndex < res.Cols; columnIndex++)
                {
                    byte grayImagePixel = grayImagePtr[columnIndex];
                    if (grayImagePixel < Threshold)
                    {
                        resImagePtr[columnIndex].Item0 = srcImagePtr[columnIndex].Item0;
                        resImagePtr[columnIndex].Item1 = srcImagePtr[columnIndex].Item1;
                        resImagePtr[columnIndex].Item2 = srcImagePtr[columnIndex].Item2;
                    }
                }
            }
            return res;
        }

        private static Mat CvtBGR2GRAY2BGR(Mat source_image)
        {
            Mat grayImage = new Mat();
            Mat res = new Mat();
            Cv2.CvtColor(src: source_image, dst: grayImage, code: ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(src: grayImage, dst: res, code: ColorConversionCodes.GRAY2BGR);
            return res;
        }

        private static Mat DoCLAHEtoSaturation(Mat source_image)
        {
            Mat res = new Mat();
            CLAHE clahe = Cv2.CreateCLAHE(clipLimit: CLAHEClipLimit, tileGridSize: CLAHETileGridSize);
            Cv2.CvtColor(src: source_image, dst: res, code: ColorConversionCodes.BGR2HSV);
            Mat[] channelSplit = Cv2.Split(res);
            clahe.Apply(channelSplit[1], channelSplit[1]);
            Cv2.Merge(channelSplit, res);
            Cv2.CvtColor(src: res, dst: res, code: ColorConversionCodes.HSV2BGR);
            return res;
        }

        private static Mat ImageProc_B6(Mat source_image)
        {
            Mat res = new Mat();
            Cv2.CvtColor(src: source_image, dst: res, code: ColorConversionCodes.BGR2YUV);
            Mat[] channelSplit = Cv2.Split(res);
            Sp = Asp[ImageProcB6State];
            Ep = Aep[ImageProcB6State];
            float element = 0;
            try
            {
                for (int index = 1; index < Sp; index++)
                {
                    element = Lut[index - 1] + Convert.ToSingle((255 - mappingRange) / (256 - Ep + Sp));
                    Lut[index] = element;
                    //Lut[index] = (int)(Lut[index - 1] + (255.0F - mappingRange) / (256.0F - Ep + Sp));
                }
                System.Diagnostics.Trace.WriteLine(message: "Sp: " + Sp.ToString());
                System.Diagnostics.Trace.WriteLine(message: "Ep: " + Ep.ToString());
                for (int index = Sp; index < Ep; index++)
                {
                    if (index == 0)
                    {
                        element = Lut[255] + Convert.ToSingle(mappingRange / (Ep - Sp));
                    }
                    else
                    {
                        element = Lut[index - 1] + Convert.ToSingle(mappingRange / (Ep - Sp));
                    }
                    if (element > 255)
                    {
                        Lut[index] = 255.0F;
                    }
                    else
                    {
                        Lut[index] = element;
                    }
                    //Lut[index] = (int)(Lut[index - 1] + (mappingRange) / (Ep + Sp));
                }
                for (int index = Ep; index < 256; index++)
                {
                    element = Lut[index - 1] + Convert.ToSingle((255 - mappingRange) / (256 - Ep + Sp));
                    Lut[index] = element;
                }
                DebugLut = new Mat(rows: 256, cols: 1, type: MatType.CV_32F, data: Lut);
                Cv2.LUT(src: channelSplit[0], lut: DebugLut, dst: channelSplit[0]);
                channelSplit[0].ConvertTo(channelSplit[0], rtype: MatType.CV_8UC1);

                Cv2.Merge(channelSplit, res);
                Cv2.CvtColor(src: res, dst: res, code: ColorConversionCodes.YUV2BGR);
                return res;
            }
            catch (OverflowException exception)
            {
                System.Diagnostics.Trace.WriteLine(message: "element: " + element.ToString());
                DateTime currentDate = DateTime.Now;
                Console.WriteLine(value: exception.ToString());
                Mat empty = new Mat();
                return empty;
            }
            catch (OpenCvSharp.OpenCVException exception)
            {
                System.Diagnostics.Trace.WriteLine(message: "element: " + element.ToString());
                DateTime currentDate = DateTime.Now;
                Console.WriteLine(value: exception.ToString());
                Mat empty = new Mat();
                return empty;
            }
        }

        public static void IMG_save_image(in Mat image, in string fileName)
        {
            try
            {
                string directory = IMG_storage_directory + @"\" + fileName.Substring(0, 10) + @"\" + fileName;
                image.SaveImage(fileName: directory);
            }
            catch (Exception exception)
            {
                DateTime current_date = DateTime.Now;
                Console.WriteLine(value: exception.ToString());
            }
        }

        public unsafe static byte[] ConvertMatToByteArray(in Mat input)
        {
            int channels = input.Channels();
            int columns = input.Cols;
            int rows = input.Rows;
            byte[] byteArray = new byte[channels * columns * rows];
            int byteArrayIndex = 0;
            for (int rowIndex = 0; rowIndex < input.Rows; rowIndex++)
            {
                Vec3b* rowPtr = (Vec3b*)input.Ptr(rowIndex).ToPointer();
                for (int columnIndex = 0; columnIndex < input.Cols; columnIndex++)
                {
                    byteArrayIndex = (rowIndex * (input.Cols * channels)) + (columnIndex * channels);
                    for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                    {
                        byteArray[byteArrayIndex + channelIndex] = rowPtr[columnIndex][channelIndex];
                    }
                }
            }
            return byteArray;
        }

        public unsafe static Mat ConvertByteArrayToMat(in byte[] input, int rows, int columns, int channels, MatType type)
        {
            Mat output = new Mat(rows: rows, cols: columns, type: type);
            int byteArrayIndex = 0;
            for (int rowIndex = 0; rowIndex < rows; rowIndex++)
            {
                Vec3b* srcImagePtr = (Vec3b*)output.Ptr(rowIndex).ToPointer();
                for (int columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    byteArrayIndex = (rowIndex * (columns * channels)) + (columnIndex * channels);
                    for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                    {
                        srcImagePtr[columnIndex][channelIndex] = input[byteArrayIndex + channelIndex];
                    }
                }
            }
            return output;
        }

    }
}