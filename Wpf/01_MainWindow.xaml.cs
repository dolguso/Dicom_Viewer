//using ai_model_01_01K.ClassFiles;
using Alturos.Yolo.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using System.Windows.Shapes;
using System.Linq;
using System.Xml.Linq;
using System.Security.Principal;
using OpenCvSharp.Util;
using System.Threading.Tasks;

namespace Wpf
{
    public static class Extensions
    {
        public static IEnumerable<DetectItem> Replace<T>(this IEnumerable<DetectItem> source, string oldValue, string newValue)
        {
            return source.Select(element =>
            {
                DetectItem temp = element;
                if (temp.Type == oldValue)
                    temp.Type = newValue;
                return temp;
            }
            );
        }
    }
    public enum DesignFlagIndices
    {
        bounding_box_activation
    }

    public enum SettingFlagIndex
    {
        roi_start_x,
        roi_start_y,
        roi_end_x,
        roi_end_y,
        center_x,
        center_y,
        scale_x,
        scale_y,
        save_dir_path,
        bandwidth,
        detection_interval,
        ai_result_add,
        screen_width,
        screen_height
    }

    public class AIset
    {
        public string[] class_list;
        public double[] confidence_list;
        public int[,] color_list;
        public int number_of_class;
        public int[] result_converter;
        public string[,] class_name;
    }

    public class box
    {
        public float x, y, w, h;
    }

    public partial class MainWindow : System.Windows.Window
    {
        #region class_name
        public string[,] lang_ai_model_01 = { { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05" }, { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05" } };
        public string[,] lang_ai_model_02 = { { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06", "prod_07", "prod_08", "prod_09", "prod_10", }, { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06", "prod_07", "prod_08", "prod_09", "prod_10", } };
        #endregion

        #region led_dll_load
        [DllImport("Ux64_dllc.dll")]
        public static extern void Usb_Qu_Open();
        [DllImport("Ux64_dllc.dll")]
        public static extern void Usb_Qu_Close();
        [DllImport("Ux64_dllc.dll")]
        public static unsafe extern bool Usb_Qu_write(byte Q_index, byte Q_type, byte* pQ_data);
        [DllImport("Ux64_dllc.dll")]
        public static extern int Usb_Qu_Getstate();
        #endregion

        #region checker
        bool loop_check = false; // while문 실행을 위한 변수
        int distance_calculate_check = 0; // 백그라운드로 돌아가는 pixel 거리 계산 여부 확인
        private int db_save_check = -1; // DB 입력 여부 확인
        int roi_click_check = 0; // configure 창에서 ROI 버튼 클릭할 시 마우스 왼쪽 버튼 다운, 업, ROI 상자 클릭 등 확인
        int image_db_save_check = 4; // IMAGE, RESULT 이미지 저장 및 DB 저장 값 여부 확인
        int box_drawing_check = 0; // AI 검출 후 박스 생성 여부 확인
        #endregion

        #region video
        VideoCapture video_capture; // Video 캡쳐 변수
        WriteableBitmap image_print_bitmap, image_print_bitmap_2; // Image Control 출력을 위한 변수
        Mat previous_frame = new Mat(); // 이미지 거리 측정을 위한 이전 프레임 변수
        const int frame_width = 1920;
        const int frame_height = 1080;
        int image_process_flag = -1;
        Mat current_frame = new Mat();
        Mat print_frame = new Mat();
        List<System.Windows.Shapes.Rectangle> ai_model_01_rectangle_list = new List<System.Windows.Shapes.Rectangle>();
        List<Thickness> ai_model_01_margin_list = new List<Thickness>();
        List<System.Windows.Shapes.Rectangle> ai_model_02_rectangle_list = new List<System.Windows.Shapes.Rectangle>();
        List<Thickness> ai_model_02_margin_list = new List<Thickness>();
        Mat temp_mat_1, temp_mat_2;
        double roi_start_x = 0;
        double roi_start_y = 0;
        double roi_end_x = 1920;
        double roi_end_y = 1080;
        private int ai_model_01_matching_result;
        private int pre_ai_model_01_matching_result;
        private int ai_model_02_matching_result;
        private int pre_ai_model_02_matching_result;
        OpenCvSharp.Rect rect_for_rectangle;
        OpenCvSharp.Size idealSize = new OpenCvSharp.Size(1920.0, 1080.0);
        OpenCvSharp.Size size720p = new OpenCvSharp.Size(1280.0, 664.0);
        OpenCvSharp.Size dsize = new OpenCvSharp.Size(608.0, 315.0);
        OpenCvSharp.Size zeroSize = new OpenCvSharp.Size(0, 0);
        Mat[] mat_list = new Mat[5];
        int mat_index = 0;
        int save_index = 0;
        int pre_dis = 0;
        int sub_max = 300;
        int fast_check = 1;
        double bandwidth = 0;
        double rate = 1.0;
        int frame_index = 0;
        int check_dis = 0;
        Queue<Mat> mat_queue = new Queue<Mat>();
        Queue<Mat> mat_queue_input = new Queue<Mat>();
        Queue<byte[]> byte_queue = new Queue<byte[]>();
        int mem_count = 0;
        #endregion

        #region ui
        //System.Windows.Controls.Slider[] slider_list;
        //List<Slider> slider_list = new List<Slider>();
        //System.Windows.Controls.Label[] label_list;
        //List<Label> label_list = new List<Label>();
        List<SolidColorBrush> brush_list = new List<SolidColorBrush>();
        OpenCvSharp.Rect roi_rect;
        //private System.Windows.Controls.Label[] full_label_list;
        List<Label> full_label_list = new List<Label>();
        List<Image> image_list = new List<Image>();
        Stopwatch stopwatch_fps = new Stopwatch();
        List<int> border_thickness_list = new List<int>();
        int alarm_margin_left = 0;
        bool alarm_label_change = false;
        List<System.Windows.Shapes.Rectangle> ai_model_01_rect_list = new List<System.Windows.Shapes.Rectangle>();
        List<System.Windows.Shapes.Rectangle> ai_model_02_rect_list = new List<System.Windows.Shapes.Rectangle>();
        bool get_check = true;

        int screen_wid = 0;

        int screen_hei = 0;


        #endregion

        #region AI
        //ai_model_01_01K.ai_model_01Manager ai_model_01_manager;
        AI_Vid_Demo.ai_model_02Manager ai_model_02_manager;
        AI_Vid_Demo.ai_model_02Manager2 ai_model_02_manager_2;
        AIset ai_model_02_AIset = new AIset();
        AIset ai_model_01_AIset = new AIset();
        private BackgroundWorker bg_aiworker_ai_model_01, bg_aiworker_ai_model_02, bg_dbworker, bg_distanceworker, bg_saveimage, bg_check_image, bg_empty_mem, bg_getImage_1, bg_getImage_2, bg_getImage_3;
        int detection_interval;
        IEnumerable<DetectItem> ai_model_01_result, ai_model_01_result_temp, ai_model_02_result, ai_model_02_result_temp;
        byte[] alarm = new byte[6];
        byte[] alarm_check = new byte[6];
        List<int> ai_model_01_on = new List<int>();
        List<int> ai_model_02_on = new List<int>();
        int ideal_height = 0;
        int ideal_width = 0;
        int ai_check = 0;
        int image_original_check = 1;
        List<Mat> list_for_calc = new List<Mat>();
        Queue<Mat> list_capture = new Queue<Mat>();
        int ai_result_add = 0;
        int speed_check = 0;
        int dis_ai = 0;
        bool dis_ai_check = true;
        //double resize_width = 1200.0;
        //double resize_height = 933.0;
        double resize_width = 1920.0;
        double resize_height = 1080.0;
        #endregion

        #region setting
        double image_scale_x, image_scale_y;
        double start_size_x, start_size_y;
        DateTime time;
        string dir_path;
        int[] parameter_send;
        private string mac_address = "";
        string save_dir_path = "";
        string[] setting_save = new string[Enum.GetNames(typeof(SettingFlagIndex)).Length];
        DispatcherTimer licence_check_timer = new DispatcherTimer();
        int detector_count = 2;
        string preset_user = "DEFAULT";
        static string log_file_name = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss.ffff") + ".txt";
        string DirPath = Directory.GetCurrentDirectory() + @"\Logs";
        string FilePath = Directory.GetCurrentDirectory() + @"\Logs\" + log_file_name;
        int lang = 0;
        #endregion

        #region DB
        string[] all_count;
        object[][] count_result;
        string[] info_insert = null;
        object[] input_pk;
        List<object[]> db_ai_model_01_input_list = new List<object[]>();
        List<object[]> db_ai_model_02_input_list = new List<object[]>();
        List<object[]> db_input_list = new List<object[]>();
        List<object[]> list_ai_model_01 = new List<object[]>();
        List<object[]> list_ai_model_02 = new List<object[]>();
        int pk_index;
        object[] db_ai_model_01_input;
        object[] db_ai_model_02_input;
        #endregion

        public bool[] detection_flag
        {
            get; set;
        }

        public bool[] box_activation_flag
        {
            get; set;
        }

        public unsafe MainWindow(List<string> object_name, List<int> ai_model_01_index_array, List<int> ai_model_02_index_array, int lang_index)
        {
            loop_check = true;
            for (int i = 0; i < mat_list.Length; i++)
                mat_list[i] = new Mat();
            ai_model_01_on = ai_model_01_index_array.ToList();
            ai_model_02_on = ai_model_02_index_array.ToList();
            lang = lang_index;
            if (!(Directory.Exists(Directory.GetCurrentDirectory() + @"\Logs")))
            {
                Directory.CreateDirectory(DirPath);
            }

            if (System.Windows.SystemParameters.PrimaryScreenWidth < 1600)
            {
                this.Width = 1280;
                this.Height = 720;
            }

            if (!(File.Exists(@"./info.txt")))
                File.Create(@"./info.txt");
            if (!(File.Exists(@"./detect.txt")))
                File.Create(@"./detect.txt");
            InitializeComponent();
            for (int i = 0; i < 6; i++)
                alarm[i] = 0;
            fixed (byte* temp = alarm)
                Usb_Qu_write(0, 0, temp);

            ColumnDefinition[] c1 = new ColumnDefinition[object_name.Count];
            ColumnDefinition[] c2 = new ColumnDefinition[object_name.Count];

            for (int i = 0; i < object_name.Count; i++)
            {
                c1[i] = new ColumnDefinition();
                c2[i] = new ColumnDefinition();
                c1[i].Width = new GridLength(1, GridUnitType.Star);
                c2[i].Width = new GridLength(1, GridUnitType.Star);
                label_grid.ColumnDefinitions.Add(c1[i]);
                if (object_name.Count >= 13)
                    image_grid.ColumnDefinitions.Add(c2[i]);
            }

            ColumnDefinition column_temp = new ColumnDefinition();
            ColumnDefinition column_temp_2 = new ColumnDefinition();
            column_temp.Width = new GridLength(92, GridUnitType.Pixel);
            column_temp_2.Width = new GridLength(92, GridUnitType.Pixel);
            label_grid.ColumnDefinitions.Add(column_temp);
            if (object_name.Count >= 13)
                image_grid.ColumnDefinitions.Add(column_temp_2);

            for (int i = 0; i < object_name.Count; i++)
            {
                string temp = "label_alarm_" + object_name[i];
                for (int j = 0; j < label_grid.Children.Count - 1; j++)
                {
                    if (((Label)label_grid.Children[j]).Name.ToString() == temp)
                    {
                        ((Label)label_grid.Children[j]).IsEnabled = true;
                        ((Label)label_grid.Children[j]).Visibility = System.Windows.Visibility.Visible;
                        ((Label)label_grid.Children[j]).SetValue(Grid.ColumnProperty, i);
                        ((Label)label_grid.Children[j]).FontSize = 35;
                        if (object_name.Count >= 13)
                        {
                            ((Label)label_grid.Children[j]).FontSize = 0.1;
                            ((Image)image_grid.Children[j]).IsEnabled = true;
                            ((Image)image_grid.Children[j]).Visibility = System.Windows.Visibility.Visible;
                            ((Image)image_grid.Children[j]).SetValue(Grid.ColumnProperty, i);
                            ((Image)image_grid.Children[j]).MouseLeftButtonDown += click_AlarmLabel;
                            image_list.Add((Image)image_grid.Children[j]);
                        }
                        break;
                    }
                }
            }

            ((Grid)label_grid.Children[label_grid.Children.Count - 1]).IsEnabled = true;

            ((Grid)label_grid.Children[label_grid.Children.Count - 1]).Visibility = System.Windows.Visibility.Visible;

            ((Grid)label_grid.Children[label_grid.Children.Count - 1]).SetValue(Grid.ColumnProperty, object_name.Count);

            #region get_BasicSetting
            if (File.Exists(@"./setting_dictionary.txt"))
            {
                string[] read_setting_string_lits = File.ReadAllLines(@"./setting_dictionary.txt");
                foreach (string read_setting_string in read_setting_string_lits)
                {
                    string[] read_setting_string_split = new string[2];
                    read_setting_string_split = read_setting_string.Split(' ');
                    int index = (int)Enum.Parse(typeof(SettingFlagIndex), read_setting_string_split[0]);

                    setting_save[index] = read_setting_string_split[1];
                }
            }
            else
            {
                string[] output_setting_string_list = { "0", "0", "1920", "1080", "0", "0", "1", "1", @"D:\DATA\", "1", "500", "0", "1920", "1080" };

                using (StreamWriter outputFile = new StreamWriter(@"./setting_dictionary.txt"))
                {
                    int setting_save_index = 0;
                    foreach (string output_setting_string in output_setting_string_list)
                    {
                        outputFile.WriteLine(typeof(SettingFlagIndex).GetEnumName(setting_save_index) + " " + output_setting_string);
                        setting_save[setting_save_index] = output_setting_string_list[setting_save_index];
                        setting_save_index++;
                    }
                }
            }

            mac_address = NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString().Replace(":", "");

            #endregion

            dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"IMAGES\";
            DirectoryInfo directory_path = new DirectoryInfo(dir_path);
            if (directory_path.Exists == false)
            {
                directory_path.Create();
            }

            dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"RESULTS\";
            directory_path = new DirectoryInfo(dir_path);
            if (directory_path.Exists == false)
            {
                directory_path.Create();
            }

            dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"REPORT\";
            directory_path = new DirectoryInfo(dir_path);
            if (directory_path.Exists == false)
            {
                directory_path.Create();
            }
            setting_save[(int)SettingFlagIndex.screen_width] = Convert.ToString(System.Windows.SystemParameters.PrimaryScreenWidth);
            setting_save[(int)SettingFlagIndex.screen_height] = Convert.ToString(System.Windows.SystemParameters.PrimaryScreenHeight);

            screen_wid = Convert.ToInt32((setting_save[(int)SettingFlagIndex.screen_width]));
            screen_hei = Convert.ToInt32((setting_save[(int)SettingFlagIndex.screen_height]));

            ai_model_01_AIset.number_of_class = 0;
            ai_model_02_AIset.number_of_class = 0;

            stopwatch_fps.Start();
            detection_flag = new bool[detector_count];
            initLatestDetectionFlags();

            #region  collect_UI2AI
            for (int i = 0; i < label_grid.Children.Count; i++)
            {
                DependencyObject canvas_child = label_grid.Children[i];
                if (typeof(FrameworkElement).IsAssignableFrom(canvas_child.GetType()) && ((string)((FrameworkElement)canvas_child).Tag == "main_alarm_label_ai_model_01") && ((FrameworkElement)canvas_child).IsEnabled == true)
                {
                    full_label_list.Add((Label)canvas_child);
                    ai_model_01_AIset.number_of_class += 1;
                }
                if (typeof(FrameworkElement).IsAssignableFrom(canvas_child.GetType()) && ((string)((FrameworkElement)canvas_child).Tag == "main_alarm_label_ai_model_02") && ((FrameworkElement)canvas_child).IsEnabled == true)
                {
                    full_label_list.Add((Label)canvas_child);
                    ai_model_02_AIset.number_of_class += 1;
                }
            }
            #endregion

            #region set_AIComponent
            ai_model_02_AIset.result_converter = new int[]
            {
                0,1,2,3,4,5,6,7,8,9
            };

            ai_model_01_AIset.result_converter = new int[]
            {
                0,1,2,3,4,5
            };

            ai_model_02_AIset.class_name = new string[,]
            {
                { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06", "prod_07", "prod_08", "prod_09", "prod_10", },
                { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06", "prod_07", "prod_08", "prod_09", "prod_10", }

            };

            ai_model_01_AIset.class_name = new string[,]
            {
                { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06" },
                { "prod_01", "prod_02", "prod_03", "prod_04", "prod_05", "prod_06" }
            };

            ai_model_01_AIset.confidence_list = new double[ai_model_01_AIset.result_converter.Length];
            for (int i = 0; i < ai_model_01_AIset.result_converter.Length; i++)
                ai_model_01_AIset.confidence_list[i] = 0.9;

            int ai_model_01_index = 0;

            if (ai_model_01_AIset.number_of_class > 0)
                for (int i = 0; i < ai_model_01_AIset.result_converter.Length; i++)
                {
                    if (ai_model_01_AIset.result_converter[i] == ai_model_01_index)
                    {
                        ai_model_01_index += 1;
                    }
                    if (ai_model_01_index > ai_model_01_AIset.number_of_class)
                        break;
                }
            int ai_model_02_index = 0;

            if (ai_model_02_AIset.number_of_class > 0)
                for (int i = 0; i < ai_model_02_AIset.result_converter.Length; i++)
                {
                    if (ai_model_02_AIset.result_converter[i] == ai_model_02_index)
                    {
                        ai_model_02_index += 1;
                    }
                    if (ai_model_02_index > ai_model_02_AIset.number_of_class)
                        break;
                }

            ai_model_02_AIset.confidence_list = new double[ai_model_02_AIset.result_converter.Length];
            for (int i = 0; i < ai_model_02_AIset.result_converter.Length; i++)
                ai_model_02_AIset.confidence_list[i] = 0.9;
            #endregion

            for (int i = 0; i < lang_ai_model_01.Length / 2 + lang_ai_model_02.Length / 2; i++)
            {
                brush_list.Add(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)));
                border_thickness_list.Add(3);
            }


            int configurationLength = label_grid.Children.Count * 2;
            parameter_send = new int[configurationLength];

            preset_user = "DEFAULT";
            Console.WriteLine(preset_user);
            if (File.Exists(@"./preset/" + preset_user + ".txt"))
            {
                string[] read_setting_string_lits = File.ReadAllLines(@"./preset/" + preset_user + ".txt");
                foreach (string read_setting_string in read_setting_string_lits)
                {
                    string[] read_setting_string_split = new string[8];
                    read_setting_string_split = read_setting_string.Split(' ');
                    int index = Convert.ToInt32(read_setting_string_split[0]);
                    if (index > 0)
                    {
                        if (index <= 13 && index >= 1)
                        {
                            if (index <= 5)
                            {
                                for (int j = 0; j < ai_model_01_AIset.result_converter.Length; j++)
                                {
                                    if (ai_model_01_AIset.result_converter[j] == index - 1)
                                    {
                                        ai_model_01_AIset.confidence_list[j] = Convert.ToInt32(read_setting_string_split[1]) / 100.0;
                                    }
                                }
                                brush_list[index - 1] = new SolidColorBrush(Color.FromArgb(Convert.ToByte(read_setting_string_split[2]), Convert.ToByte(read_setting_string_split[3]), Convert.ToByte(read_setting_string_split[4]), Convert.ToByte(read_setting_string_split[5])));
                            }
                            if (index > 5 && index <= 13)
                            {
                                Console.WriteLine(read_setting_string_split);
                                for (int j = 0; j < ai_model_02_AIset.result_converter.Length; j++)
                                {
                                    if (ai_model_02_AIset.result_converter[j] == index - 6)
                                    {
                                        ai_model_02_AIset.confidence_list[j] = Convert.ToInt32(read_setting_string_split[1]) / 100.0;
                                    }
                                }
                                brush_list[index - 1] = new SolidColorBrush(Color.FromArgb(Convert.ToByte(read_setting_string_split[2]), Convert.ToByte(read_setting_string_split[3]), Convert.ToByte(read_setting_string_split[4]), Convert.ToByte(read_setting_string_split[5])));
                            }
                            //slider_list[index - 1].Value = Convert.ToInt32(read_setting_string_split[1]);
                            parameter_send[index * 2] = Convert.ToInt32(read_setting_string_split[6]);
                            parameter_send[index * 2 + 1] = Convert.ToInt32(read_setting_string_split[1]);
                            border_thickness_list[index - 1] = Convert.ToInt32(read_setting_string_split[7]);
                        }
                    }
                    if (index == -1)
                        parameter_send[0] = Convert.ToInt32(read_setting_string_split[1]);
                    else if (index == -2)
                        parameter_send[1] = Convert.ToInt32(read_setting_string_split[1]);
                }
            }

            time = DateTime.Now;
            all_count = new string[1];
            all_count[0] = "COUNT(*)";
            count_result = AI_Vid_Demo.SqlManager.MSQL_SEL(time.ToString("yyyyMM") + "info", all_count);
            pk_index = Convert.ToInt32(count_result[0][0]) + 1;

            for (int i = 2; i < parameter_send.Length; i += 2)
            {
                if (i < 12)
                {
                    if (ai_model_01_on.Contains(i / 2 - 1))
                    {
                        if (parameter_send[i] == 0)
                        {
                            if (object_name.Count < 13)
                                full_label_list[ai_model_01_on.IndexOf(i / 2 - 1)].Foreground = new SolidColorBrush(Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source.ToString().Replace("_on", "_off"));
                                bt.EndInit();
                                image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source = bt;
                            }
                        }
                    }
                }
                else
                {
                    if (ai_model_02_on.Contains(i / 2 - 6))
                    {
                        if (parameter_send[i] == 0)
                        {
                            if (object_name.Count < 13)
                                full_label_list[ai_model_02_on.IndexOf(i / 2 - 6) + ai_model_01_on.Count].Foreground = new SolidColorBrush(Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_02_on.IndexOf(i / 2 - 6) + ai_model_01_on.Count].Source.ToString().Replace("_on", "_off"));
                                bt.EndInit();
                                image_list[ai_model_02_on.IndexOf(i / 2 - 6) + ai_model_01_on.Count].Source = bt;
                            }
                        }
                    }
                }
            }

            box_activation_flag = new bool[Enum.GetNames(typeof(Wpf.DesignFlagIndices)).Length];
            initDesignFlags();

            #region set_BackgroundWorker
            bg_dbworker = new BackgroundWorker();
            bg_dbworker.DoWork += new DoWorkEventHandler(backgroundDBInput);
            bg_dbworker.RunWorkerAsync();

            detection_interval = 500;
            //if (ai_model_01_AIset.number_of_class > 0)
            //{
            //    Console.WriteLine("ai_model_01 run");
            //    ai_model_01_manager = new ai_model_01_01K.ai_model_01Manager();
            //    bg_aiworker_ai_model_01 = new BackgroundWorker();
            //    bg_aiworker_ai_model_01.DoWork += new DoWorkEventHandler(backgroundai_model_01AIResult);
            //    bg_aiworker_ai_model_01.RunWorkerAsync();
            //}

            //if (ai_model_02_AIset.number_of_class > 0)
            //{
            //    Console.WriteLine("ai_model_02 run");
            //    ai_model_02_manager = new ai_model_01_01K.ai_model_02Manager();
            //    ai_model_02_manager_2 = new ai_model_01_01K.ai_model_02Manager2();
            //    bg_aiworker_ai_model_02 = new BackgroundWorker();
            //    bg_aiworker_ai_model_02.DoWork += new DoWorkEventHandler(backgroundai_model_02AIResult);
            //    bg_aiworker_ai_model_02.RunWorkerAsync();
            //}
            #endregion

            bg_saveimage = new BackgroundWorker();
            bg_saveimage.DoWork += new DoWorkEventHandler(backgroundSaveImage);

            bg_check_image = new BackgroundWorker();
            bg_check_image.DoWork += new DoWorkEventHandler(backgroundCheckImage);

            bg_empty_mem = new BackgroundWorker();
            bg_empty_mem.DoWork += new DoWorkEventHandler(backgroundEmptyMem);

            #region set_LicenseCheck
            licence_check_timer.Interval = TimeSpan.FromHours(12);
            licence_check_timer.Tick += new EventHandler(checkLicense);
            //licence_check_timer.Start();
            #endregion
            
            update_lang(lang);
        }

        private object obj = new object();

        private void getImage_1(object sender, DoWorkEventArgs e)
        {
            Mat temp = new Mat();
            while (get_check)
            {
                if (video_capture.IsOpened())
                {
                    video_capture.Read(temp);
                    if (temp.Width != 0)
                    {
                        list_capture.Enqueue(temp);
                        byte_queue.Enqueue(temp.ToBytes());
                    }
                    //Console.WriteLine("enqueue_1");
                }
            }
            #region old
            //Mat[] temp = new Mat[6];
            //int index = 0;
            //Mat prev_temp = new Mat();
            //for (int i = 0; i < 6; i++)
            //    temp[i] = new Mat();

            //float XResizeCoefficient = 1.0F;
            //float YResizeCoefficient = 0.01F;
            //bool start = false;
            //int save_index = 0;
            //List<Mat> sort_list = new List<Mat>();
            //int num = 1;
            //bool direction = true;
            //int reverse_count = 0;
            //Mat import_padding = new Mat();

            //Stopwatch cycle = new Stopwatch();
            //cycle.Restart();


            //while (true)
            //{
            //    video_capture.Read(temp[index]);

            //    Mat copy = new Mat(new OpenCvSharp.Size(1920, 1080), MatType.CV_8UC3);
            //    temp[index].CopyTo(copy);
            //    mat_queue.Enqueue(copy);

            //    #region input_sort
            //    //mat_queue.Enqueue(temp[index]);
            //    //sort_list.Add(temp[index].Clone());
            //    //if (reverse_count > 5)
            //    //{
            //    //    reverse_count = 0;
            //    //    direction = !direction;
            //    //}
            //    //if (sort_list.Count >= 12)
            //    //{
            //    //    Stopwatch sort_time = new Stopwatch();
            //    //    sort_time.Restart();
            //    //    int result = sort_input_image_list(sort_list[sort_list.Count - 1], sort_list, direction);
            //    //    sort_time.Stop();
            //    //    //Console.WriteLine("sort_time : " + sort_time.ElapsedMilliseconds);
            //    //    switch (result)
            //    //    {
            //    //        case -2:
            //    //            Console.WriteLine("SAME DROP!!!");
            //    //            //import_padding = sort_list[5].Clone();
            //    //            //sort_list[4][0, 996, 0, 1914].CopyTo(import_padding[0, 996, 6, 1920]);
            //    //            sort_list = sort_list.GetRange(0, sort_list.Count - 1);
            //    //            //sort_list.Add(import_padding);
            //    //            reverse_count = 0;
            //    //            break;
            //    //        case -1:
            //    //            Console.WriteLine("DROP!!!");
            //    //            sort_list = sort_list.GetRange(0, sort_list.Count - 1);
            //    //            reverse_count++;
            //    //            break;
            //    //        case 11:
            //    //            Console.WriteLine("KEEP!!!");
            //    //            reverse_count = 0;
            //    //            break;
            //    //        default:
            //    //            Console.WriteLine("PUT IN " + (result + 1));
            //    //            Mat import = sort_list[sort_list.Count - 1].Clone();
            //    //            List<Mat> swap_list = new List<Mat>();
            //    //            swap_list = sort_list.GetRange(0, result);
            //    //            swap_list.Add(import);
            //    //            swap_list.AddRange(sort_list.GetRange(result, sort_list.Count - result - 1));
            //    //            //for (int i = 0; i < sort_list.Count; i++)
            //    //            //{
            //    //            //    sort_list[i].SaveImage(@"./image_check/before_" + num + "_" + i + ".png");
            //    //            //}
            //    //            sort_list = swap_list;
            //    //            //for (int i = 0; i < sort_list.Count; i++)
            //    //            //{
            //    //            //    sort_list[i].SaveImage(@"./image_check/after_" + num + "_" + i + ".png");
            //    //            //}
            //    //            num += 1;
            //    //            reverse_count = 0;
            //    //            break;
            //    //    };
            //    //    mat_queue.Enqueue(sort_list[0]);
            //    //    //mat_queue_input.Enqueue(sort_list[0]);
            //    //    sort_list = sort_list.GetRange(1, sort_list.Count - 1);
            //    //}
            //    #endregion

            //    index += 1;
            //    index = index % 6;
            //}
            #endregion
        }

        private void getImage_2(object sender, DoWorkEventArgs e)
        {
            Mat temp = new Mat();
            while (true)
            {
                try
                {
                    if (video_capture.IsOpened())
                    {
                        video_capture.Read(temp);
                        if (temp.Width != 0)
                            list_capture.Enqueue(temp);
                        //Console.WriteLine("enqueue_2");
                        Thread.Sleep(10);
                    }
                }
                catch (System.Reflection.TargetInvocationException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (OpenCvSharpException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (NullReferenceException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (Exception f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
            }
        }

        private void getImage_3(object sender, DoWorkEventArgs e)
        {
            Mat temp = new Mat();
            while (true)
            {
                try
                {
                    video_capture.Read(temp);
                    if (temp.Width != 0)
                        list_capture.Enqueue(temp);
                    Console.WriteLine("enqueue_3");
                    Thread.Sleep(10);
                }
                catch (System.Reflection.TargetInvocationException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (OpenCvSharpException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (NullReferenceException f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
                catch (Exception f)
                {
                    Console.WriteLine(f.ToString());
                    Thread.Sleep(10);
                }
            }
        }

        private int sort_input_image_list(in Mat prior_image, in List<Mat> current_image, bool direction)
        {
            float XResizeCoefficient = 0.5f;
            float YResizeCoefficient = 0.01f;
            int j = 0;

            Mat resizedPriorImage = new Mat();
            Cv2.Resize(src: prior_image, dst: resizedPriorImage,
                dsize: new OpenCvSharp.Size(prior_image.Width * XResizeCoefficient,
                prior_image.Height * YResizeCoefficient), 0, 0, InterpolationFlags.Nearest);

            Mat[] resizedPriorImageComponent = resizedPriorImage.Split();
            Mat priorMax = new Mat(resizedPriorImage.Size(), resizedPriorImage.Type());
            Mat priorMin = new Mat(resizedPriorImage.Size(), resizedPriorImage.Type());

            Cv2.Max(resizedPriorImageComponent[0], resizedPriorImageComponent[1], priorMax);
            Cv2.Max(resizedPriorImageComponent[2], priorMax, priorMax);
            Cv2.Min(resizedPriorImageComponent[0], resizedPriorImageComponent[1], priorMin);
            Cv2.Min(resizedPriorImageComponent[2], priorMin, priorMin);
            Mat priordiff = priorMax - priorMin;
            Cv2.Threshold(priordiff, priordiff, 20, 255, ThresholdTypes.Binary);
            OpenCvSharp.Rect rect_1 = new OpenCvSharp.Rect(10, 0, priordiff.Width - 20, priordiff.Height);


            for (j = current_image.Count - 2; j > 1; j--)
            {
                Mat resizedCurrentImage = new Mat();
                int mini = 10;
                try
                {
                    Cv2.Resize(src: current_image[j], dst: resizedCurrentImage,
                        dsize: new OpenCvSharp.Size(current_image[j].Width * XResizeCoefficient,
                        current_image[j].Height * YResizeCoefficient), 0, 0, InterpolationFlags.Nearest);
                    Mat[] resizedCurrentImageComponent = resizedCurrentImage.Split();
                    Mat currentMax = new Mat(resizedCurrentImage.Size(), resizedCurrentImage.Type());
                    Mat currentMin = new Mat(resizedCurrentImage.Size(), resizedCurrentImage.Type());
                    Cv2.Max(resizedCurrentImageComponent[0], resizedCurrentImageComponent[1], currentMax);
                    Cv2.Max(resizedCurrentImageComponent[2], currentMax, currentMax);
                    Cv2.Min(resizedCurrentImageComponent[0], resizedCurrentImageComponent[1], currentMin);
                    Cv2.Min(resizedCurrentImageComponent[2], currentMin, currentMin);
                    Mat currentdiff = currentMax - currentMin;

                    Cv2.Threshold(currentdiff, currentdiff, 20, 255, ThresholdTypes.Binary);

                    long minerror = 9000000000;
                    Mat subimg_1, subimg_2, subimg_3;
                    for (int i = 0; i < 21; i++)
                    {
                        OpenCvSharp.Rect rect_2 = new OpenCvSharp.Rect(i, 0, priordiff.Width - 20, priordiff.Height);
                        subimg_1 = new Mat(currentdiff, rect_1);
                        subimg_2 = new Mat(priordiff, rect_2);
                        subimg_3 = new Mat(subimg_1.Size(), subimg_1.Type());
                        Cv2.Subtract(subimg_1, subimg_2, subimg_3);

                        Scalar error = Cv2.Sum(subimg_3);
                        long error1 = (long)(error.Val0 / 255);

                        if (error1 < minerror)
                        {
                            minerror = error1;
                            mini = i;
                        }
                    }
                }
                catch (Exception exception)
                {
                    DateTime current_date = DateTime.Now;
                    Console.WriteLine(value: exception.ToString());
                    //return distance;
                }
                if ((mini - 10 > 0) == direction)
                {
                    break;
                }
                else if (mini == 10)
                {
                    return -2;
                }
                else if (j == 2)
                {
                    return -1;
                }
            }
            return j + 1;
        }

        private void update_lang(int lang_index)
        {
            switch (lang_index)
            {
                case 0:
                    break;
                case 1:
                    for (int i = 0; i < ai_model_01_on.Count; i++)
                        full_label_list[i].Content = lang_ai_model_01[lang_index, ai_model_01_on[i]];
                    for (int i = ai_model_01_on.Count; i < ai_model_01_on.Count + ai_model_02_on.Count; i++)
                        full_label_list[i].Content = lang_ai_model_02[lang_index, ai_model_02_on[i - ai_model_01_on.Count]];
                    label_Preset.Content = "DEFAULT";
                    bt_Configure.Content = "CONFIGURE";
                    bt_Archive.Content = "ARCHIVE";
                    bt_Close.Content = "CLOSE";
                    break;
                default:
                    break;
            }
        }

        private void checkLicense(object sender, EventArgs e)
        {
            string command = @".\basic_client.exe";
            Console.WriteLine(mac_address);
            Process cmd_process = new Process();
            cmd_process.StartInfo.FileName = command;
            cmd_process.StartInfo.Arguments = @".\license.bin " + mac_address;
            cmd_process.StartInfo.RedirectStandardInput = true;
            cmd_process.StartInfo.RedirectStandardOutput = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.UseShellExecute = false;
            cmd_process.Start();
            cmd_process.StandardInput.Flush();
            cmd_process.StandardInput.Close();
            cmd_process.WaitForExit();
            int cmd_process_result = cmd_process.ExitCode;

            if (cmd_process_result != 0)
            {
                System.Windows.Forms.DialogResult rst = System.Windows.Forms.MessageBox.Show("LICENSE EXPIRED!", "License", System.Windows.Forms.MessageBoxButtons.OK);
                if (rst == System.Windows.Forms.DialogResult.OK)
                    this.Close();
            }
            Console.WriteLine("license checked");
        }

        private void initDesignFlags()
        {
            box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = true;
        }

        private void initLatestDetectionFlags()
        {
            for (int idx = 0; idx < detection_flag.Length; idx++)
            {
                detection_flag[idx] = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (initVideo())
            {
            }
            else
            {
                MessageBox.Show("Video source unavailable.");
            }
        }

        private bool initVideo()
        {
            try
            {

                video_capture = new VideoCapture(0);

                video_capture.Set(CaptureProperty.FrameWidth, frame_width);
                video_capture.Set(CaptureProperty.FrameHeight, frame_height);
                //video_capture.Set(CaptureProperty.Brightness, -10);
                //video_capture.Set(CaptureProperty.Contrast, 110);



                //video_capture.Set(CaptureProperty., );

                image_print_bitmap = new WriteableBitmap(frame_width, frame_height, 96, 96, PixelFormats.Bgr24, null);
                video_show_image.Source = image_print_bitmap;
                //bg_getImage_1.RunWorkerAsync();
                //bg_getImage_2.RunWorkerAsync();
                //bg_getImage_3.RunWorkerAsync();

                //bg_getImage.RunWorkerAsync();
                return true;
            }
            catch (Exception f)
            {
                Console.WriteLine(f.ToString());
                return false;
            }
        }

        private BitmapImage byteArrayToImage(byte[] byteArrayIn)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                stream.Write(byteArrayIn, 0, byteArrayIn.Length);
                stream.Position = 0;
                System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
                BitmapImage returnImage = new BitmapImage();
                returnImage.BeginInit();
                MemoryStream ms = new MemoryStream();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                returnImage.StreamSource = ms;
                returnImage.EndInit();

                return returnImage;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public class ByteImageConverter
        {
            public static ImageSource ByteToImage(byte[] imageData)
            {
                BitmapImage biImg = new BitmapImage();
                MemoryStream ms = new MemoryStream(imageData);
                biImg.BeginInit();
                biImg.StreamSource = ms;
                biImg.EndInit();

                ImageSource imgSrc = biImg as ImageSource;

                return imgSrc;
            }
        }
        private void Dispose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loop_check = false;
            if (video_capture.IsOpened())
            {
                video_capture.Dispose();
            }
        }

        private unsafe void clickVideoStreamButton(object sender, RoutedEventArgs e)
        {
            ai_result_add = Convert.ToInt32(setting_save[(int)SettingFlagIndex.ai_result_add]);
            Mat one = new Mat(new OpenCvSharp.Size(1920, 1080), MatType.CV_8UC3, new Scalar(1, 1, 1));

            #region set_BasicVisual
            int fps_print_interval = 0;
            image_scale.ScaleX = image_scroll.ActualWidth / video_show_image.ActualWidth;
            image_scale.ScaleY = image_scroll.ActualHeight / video_show_image.ActualHeight;
            start_size_x = screen_wid;
            start_size_y = screen_hei;
            image_scale_x = image_scroll.ActualWidth / video_show_image.ActualWidth;
            image_scale_y = image_scroll.ActualHeight / video_show_image.ActualHeight;

            int[] roi_int = new int[4];

            roi_int[0] = Convert.ToInt32(Convert.ToDouble(setting_save[(int)SettingFlagIndex.roi_start_x]));
            roi_int[1] = Convert.ToInt32(Convert.ToDouble(setting_save[(int)SettingFlagIndex.roi_start_y]));
            roi_int[2] = Convert.ToInt32(setting_save[(int)SettingFlagIndex.roi_end_x]) > Convert.ToInt32(setting_save[(int)SettingFlagIndex.screen_width]) ? Convert.ToInt32(setting_save[(int)SettingFlagIndex.screen_width]) : Convert.ToInt32(setting_save[(int)SettingFlagIndex.roi_end_x]);
            roi_int[3] = Convert.ToInt32(setting_save[(int)SettingFlagIndex.roi_end_y]) > Convert.ToInt32(setting_save[(int)SettingFlagIndex.screen_height]) ? Convert.ToInt32(setting_save[(int)SettingFlagIndex.screen_height]) : Convert.ToInt32(setting_save[(int)SettingFlagIndex.roi_end_y]);
            //roi_int[2] = Convert.ToInt32(Convert.ToDouble(setting_save[(int)SettingFlagIndex.roi_end_x]));
            //roi_int[3] = Convert.ToInt32(Convert.ToDouble(setting_save[(int)SettingFlagIndex.roi_end_y]));

            //roi_int[0] = roi_int[0] < 0 ? 0 : roi_int[0];
            //roi_int[1] = roi_int[1] < 0 ? 0 : roi_int[1];
            //roi_int[2] = roi_int[2] > frame_width ? frame_width : roi_int[2];
            //roi_int[3] = roi_int[3] > frame_height ? frame_height : roi_int[3];

            roi_start_x = roi_int[0];
            roi_end_x = roi_int[2];
            roi_start_y = roi_int[1];
            roi_end_y = roi_int[3];

            //roi_int[0] = roi_int[0] * frame_width / 1920;
            //roi_int[1] = roi_int[1] * frame_height / 1080;
            //roi_int[2] = roi_int[2] * frame_width / 1920;
            //roi_int[3] = roi_int[3] * frame_height / 1080;
            roi_int[0] = roi_int[0] * frame_width / screen_wid;
            roi_int[1] = roi_int[1] * frame_height / screen_hei;
            roi_int[2] = roi_int[2] * frame_width / screen_wid;
            roi_int[3] = roi_int[3] * frame_height / screen_hei;


            roi_scale.CenterX = Convert.ToDouble(setting_save[(int)SettingFlagIndex.center_x]);
            roi_scale.CenterY = Convert.ToDouble(setting_save[(int)SettingFlagIndex.center_y]);
            roi_scale.ScaleX = Convert.ToDouble(setting_save[(int)SettingFlagIndex.scale_x]);
            roi_scale.ScaleY = Convert.ToDouble(setting_save[(int)SettingFlagIndex.scale_y]);
            roi_rect = new OpenCvSharp.Rect(roi_int[0], roi_int[1], roi_int[2] - roi_int[0], roi_int[3] - roi_int[1]);

            #endregion
            Stopwatch one_cycle = new Stopwatch();
            one_cycle.Restart();
            save_dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]);
            bandwidth = Convert.ToDouble(setting_save[(int)SettingFlagIndex.bandwidth]);
            detection_interval = Convert.ToInt32(setting_save[(int)SettingFlagIndex.detection_interval]);

            #region set_SaveDirectory
            dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"IMAGES\" + time.ToString("yyyy-MM-dd");
            #endregion

            int check = 0;

            Stopwatch tempoasdf = new Stopwatch();
            Stopwatch wait_time = new Stopwatch();

            int ai_model_01_count = 0;
            int ai_model_02_count = 0;

            Mat abc = new Mat();
            video_capture.Read(abc);

            //_05_Showing show = new _05_Showing(roi_rect, roi_scale.CenterX, roi_scale.CenterY, roi_scale.ScaleX, roi_scale.ScaleY);
            //show.Tag = "mdi_child";
            //show.Show();

            byte[] temp_byte = new byte[0];

            Stopwatch matching = new Stopwatch();
            Stopwatch box = new Stopwatch();
            Stopwatch db = new Stopwatch();

            List<Mat> matching_error_before = new List<Mat>();
            List<Mat> matching_error_after = new List<Mat>();
            List<int> matching_error_mini = new List<int>();
            int result = 0;
            int minus_check = 0;
            //Cv2.NamedWindow("show", WindowMode.OpenGL);

            while (loop_check)
            //for (int k = 0; k < 500; k++)
            {
                //Console.WriteLine(screen_wid + ", " + screen_hei);
                int matching_re = 0;


                #region save_ResultImage
                if (image_db_save_check == 3)
                {
                    image_db_save_check = 4;
                    info_insert = null;
                }

                #endregion


                try
                {
                    video_capture.Read(mat_list[mat_index % 5]);

                    #region queue_read
                    //mat_queue_input.Enqueue(mat_list[mat_index % 5].Clone());
                    //try
                    //{
                    //    if (list_capture.Count > 0/* && byte_queue.Count > 0*/)
                    //    {
                    //        mat_list[mat_index % 5] = list_capture.Dequeue();
                    //        //temp_byte = byte_queue.Dequeue();
                    //        //Console.WriteLine("dequeue image");
                    //    }
                    //    else
                    //    {
                    //        int d = Cv2.WaitKey(1);
                    //        while (d != -1)
                    //            break;
                    //        //Console.WriteLine("wait");
                    //        continue;
                    //    }
                    //}
                    //catch (System.Reflection.TargetInvocationException f)
                    //{
                    //    Console.WriteLine(f.ToString());
                    //}
                    //catch (OpenCvSharpException f)
                    //{
                    //    Console.WriteLine(f.ToString());
                    //}
                    //catch (NullReferenceException f)
                    //{
                    //    Console.WriteLine(f.ToString());
                    //}
                    //catch (Exception f)
                    //{
                    //    Console.WriteLine(f.ToString());
                    //}
                    #endregion

                    matching.Restart();
                    if (mat_index != 0)
                        result = AI_Vid_Demo.ImgManager.IMG_calculate_movement_distance(prior_image: mat_list[mat_index % 5][roi_rect][Convert.ToInt32(roi_rect.Height * (0.5 - 0.5 * bandwidth)), Convert.ToInt32(roi_rect.Height * (0.5 + 0.5 * bandwidth)), 0, roi_rect.Width], current_image: mat_list[(mat_index - 1) % 5][roi_rect][Convert.ToInt32(roi_rect.Height * (0.5 - 0.5 * bandwidth)), Convert.ToInt32(roi_rect.Height * (0.5 + 0.5 * bandwidth)), 0, roi_rect.Width]);
                    matching.Stop();
                }
                catch (Exception df)
                {
                    Console.WriteLine(df);
                    continue;
                }
                //dis_ai += result;

                //if (Math.Abs(dis_ai) > 640 && dis_ai_check == false)
                //{
                //    dis_ai_check = true;
                //    dis_ai = 0;
                //}

                //Console.WriteLine(dis_ai);

                //Console.WriteLine(result);
                //if (result == 10 || result == -10)
                //{
                //    speed_check++;
                //    minus_check = 0;
                //}
                //else
                //{
                //    speed_check--;
                //    minus_check++;
                //}
                //if (minus_check >= 10)
                //{
                //    minus_check = 0;
                //    speed_check = 0;
                //    dis_ai_check = true;
                //}
                //if (matching.ElapsedMilliseconds >= 10)
                //{
                //    //matching_error_before.Add(mat_list[(mat_index - 1) % 5][roi_rect].Clone()); ;
                //    //matching_error_after.Add(mat_list[(mat_index) % 5][roi_rect].Clone());
                //    //matching_error_mini.Add(result);
                //}
                //Console.WriteLine("dis calc");

                check = 1;
                distance_calculate_check = 1;

                if (mat_index > 1005)
                {
                    mat_index = 5 + mat_index % 5;
                    mem_count++;
                }

                if (mat_list[mat_index % 5].Width == 0 || mat_list[mat_index % 5].Height == 0)
                    continue;

                box.Restart();
                #region box_drawing
                ai_model_01_matching_result = Convert.ToInt32(Convert.ToDouble(AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01)) * Convert.ToDouble(screen_wid) / 1920.0);
                ai_model_02_matching_result = Convert.ToInt32(Convert.ToDouble(AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02)) * Convert.ToDouble(screen_wid) / 1920.0);

                if (box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] == true && speed_check <= 30)
                {
                    if (detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_01] == false && speed_check <= 30)
                    {
                        for (int i = 0; i < ai_model_01_margin_list.Count; i++)
                            ai_model_01_rect_list[i].Margin = new Thickness(ai_model_01_margin_list[i].Left + Convert.ToDouble(ai_model_01_matching_result) * roi_rect.Width / mat_list[mat_index % 5].Width, ai_model_01_margin_list[i].Top, 0, 0);
                    }

                    if (detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_02] == false && speed_check <= 30)
                    {
                        for (int i = 0; i < ai_model_02_margin_list.Count; i++)
                            ai_model_02_rect_list[i].Margin = new Thickness(ai_model_02_margin_list[i].Left + Convert.ToDouble(ai_model_02_matching_result) * roi_rect.Width / mat_list[mat_index % 5].Width, ai_model_02_margin_list[i].Top, 0, 0);
                    }

                    if (ai_model_01_result == null && ai_model_02_result == null)
                    {
                        for (int xp = 0; xp < full_label_list.Count; xp++)
                        {
                            if (xp < ai_model_01_on.Count)
                            {
                                full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                if (parameter_send[2 + 2 * ai_model_01_on[xp]] == 1)
                                    full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                                else
                                    full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                                if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                {
                                    if (parameter_send[2 + ai_model_01_on[xp] * 2] == 1)
                                    {
                                        BitmapImage bt = new BitmapImage();
                                        bt.BeginInit();
                                        bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                        bt.EndInit();
                                        image_list[xp].Source = bt;
                                    }
                                }
                            }
                            else
                            {
                                full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                if (parameter_send[2 + 2 * (ai_model_02_on[xp - ai_model_01_on.Count] + 5)] == 1)
                                    full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                                else
                                    full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                                if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                {
                                    if (parameter_send[2 + (ai_model_02_on[xp - ai_model_01_AIset.number_of_class] + 5) * 2] == 1)
                                    {
                                        BitmapImage bt = new BitmapImage();
                                        bt.BeginInit();
                                        bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                        bt.EndInit();
                                        image_list[xp].Source = bt;
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < 30; i++)
                            ai_model_01_rect_list[i].Visibility = System.Windows.Visibility.Hidden;
                        for (int i = 0; i < 30; i++)
                            ai_model_02_rect_list[i].Visibility = System.Windows.Visibility.Hidden;

                        ai_model_01_count = 0;
                        ai_model_02_count = 0;

                        ai_model_01_margin_list.Clear();
                        ai_model_02_margin_list.Clear();
                        init_alarm();
                    }
                    else if (speed_check <= 30)
                    {
                        ai_model_01_result_temp = ai_model_01_result;
                        ai_model_02_result_temp = ai_model_02_result;
                        if (ai_model_01_AIset.number_of_class > 0 && detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_01] == true)
                        {
                            pre_ai_model_01_matching_result = AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01);
                            AI_Vid_Demo.ImgManager.UpdateMatchingResult_2(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01);
                            ai_model_01_matching_result = AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01) * screen_wid / 1920;
                            detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_01] = false;

                            for (int xp = 0; xp < ai_model_01_AIset.number_of_class; xp++)
                            {
                                full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                if (parameter_send[2 + ai_model_01_on[xp] * 2] == 1)
                                    full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                                else
                                    full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                                if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                {
                                    if (parameter_send[2 + ai_model_01_on[xp] * 2] == 1)
                                    {
                                        BitmapImage bt = new BitmapImage();
                                        bt.BeginInit();
                                        bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                        bt.EndInit();
                                        image_list[xp].Source = bt;
                                    }
                                }
                            }

                            if (ai_model_01_count > 0)
                            {
                                ai_model_01_count = 0;
                                ai_model_01_margin_list.Clear();
                            }
                            for (int i = 0; i < 30; i++)
                                ai_model_01_rect_list[i].Visibility = System.Windows.Visibility.Hidden;

                            init_alarm_ai_model_01();
                            time = DateTime.Now;

                            if (((pre_ai_model_01_matching_result > 0 || (pre_ai_model_01_matching_result == 0 && ai_model_01_matching_result == 0)) || (pre_ai_model_01_matching_result < 0 || (pre_ai_model_01_matching_result == 0 && ai_model_01_matching_result == 0)))/* && fast_check == 1*/)
                            {
                                int temp_index = 0;
                                foreach (DetectItem detResult in ai_model_01_result)
                                {
                                    double det_val = (1 - detResult.Confidence);
                                    if (det_val <= 0.99)
                                    {
                                        int det_class = Convert.ToInt32(detResult.Type);
                                        if (ai_model_01_on.Contains(ai_model_01_AIset.result_converter[det_class]))
                                            if (det_val < ai_model_01_AIset.confidence_list[det_class] && parameter_send[ai_model_01_AIset.result_converter[det_class] * 2 + 2] == 1)
                                            {
                                                rect_for_rectangle.X = Convert.ToInt32(Convert.ToDouble(detResult.X));
                                                rect_for_rectangle.Y = Convert.ToInt32(Convert.ToDouble(detResult.Y));
                                                rect_for_rectangle.Width = Convert.ToInt32(Convert.ToDouble(detResult.Width));
                                                rect_for_rectangle.Height = Convert.ToInt32(Convert.ToDouble(detResult.Height));
                                                //rect_for_rectangle.X = detResult.X;
                                                //rect_for_rectangle.Y = detResult.Y;
                                                //rect_for_rectangle.Width = detResult.Width;
                                                //rect_for_rectangle.Height = detResult.Height;

                                                //int[] listColor = returnai_model_01Color(det_class);
                                                SolidColorBrush brush = returnai_model_01Color_brush(det_class);
                                                //System.Windows.Shapes.Rectangle temp_rect = new System.Windows.Shapes.Rectangle();

                                                if (brush != null)
                                                {
                                                    full_label_list[ai_model_01_on.IndexOf(ai_model_01_AIset.result_converter[det_class])].Background = brush;
                                                    full_label_list[ai_model_01_on.IndexOf(ai_model_01_AIset.result_converter[det_class])].Foreground = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                                    add_alarm(ai_model_01_AIset.result_converter[det_class]);
                                                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                                    {
                                                        BitmapImage bt = new BitmapImage();
                                                        bt.BeginInit();
                                                        bt.UriSource = new Uri(image_list[ai_model_01_on.IndexOf(ai_model_01_AIset.result_converter[det_class])].Source.ToString().Replace("_on", "_off"));
                                                        bt.EndInit();
                                                        image_list[ai_model_01_on.IndexOf(ai_model_01_AIset.result_converter[det_class])].Source = bt;
                                                    }
                                                }

                                                if (det_class == 5)
                                                    rate = 1.5;
                                                else
                                                    rate = 1.0;

                                                ai_model_01_rect_list[temp_index].Tag = "ai_model_01_AIset";
                                                ai_model_01_rect_list[temp_index].Name = "ai_model_01_rect";
                                                ai_model_01_rect_list[temp_index].Height = Convert.ToDouble(rect_for_rectangle.Height) / mat_list[mat_index % 5].Height * roi_rect.Height * rate * screen_hei / 1080.0;
                                                ai_model_01_rect_list[temp_index].Width = Convert.ToDouble(rect_for_rectangle.Width) / mat_list[mat_index % 5].Width * roi_rect.Width * rate * screen_wid / 1920.0;
                                                ai_model_01_rect_list[temp_index].Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, brush.Color.R, brush.Color.G, brush.Color.B));
                                                ai_model_01_rect_list[temp_index].Stroke = brush;
                                                ai_model_01_rect_list[temp_index].StrokeThickness = border_thickness_list[ai_model_01_AIset.result_converter[det_class]];
                                                ai_model_01_rect_list[temp_index].RadiusX = 10;
                                                ai_model_01_rect_list[temp_index].RadiusY = 10;
                                                ai_model_01_rect_list[temp_index].Visibility = System.Windows.Visibility.Visible;

                                                //temp_rect.Tag = "ai_model_01_AIset";
                                                //temp_rect.Name = "ai_model_01_rect";
                                                //temp_rect.Height = Convert.ToDouble(detResult.Height) / mat_list[mat_index % 5].Height * roi_rect.Height  * rate;
                                                //temp_rect.Width = Convert.ToDouble(detResult.Width) / mat_list[mat_index % 5].Width * roi_rect.Width * rate;
                                                //temp_rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, brush.Color.R, brush.Color.G, brush.Color.B));
                                                //temp_rect.Stroke = brush;
                                                //temp_rect.StrokeThickness = border_thickness_list[ai_model_01_AIset.result_converter[det_class]];
                                                //temp_rect.RadiusX = 10;
                                                //temp_rect.RadiusY = 10;
                                                //Panel.SetZIndex(temp_rect, 10);

                                                //temp_rect.Margin =
                                                //    new Thickness(
                                                //        ((Convert.ToDouble(rect_for_rectangle.X) + Convert.ToDouble(ai_model_01_matching_result) * (Convert.ToDouble(temp_mat_1.Width) / Convert.ToDouble(roi_rect.Width)) * 1.0)) / mat_list[mat_index % 5].Width * roi_rect.Width + roi_start_x + ideal_width - (rate - 1.0) / 2 * temp_rect.Width / rate,
                                                //        (Convert.ToDouble(rect_for_rectangle.Y) ) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y + ideal_height - (rate - 1.0) / 2 * temp_rect.Height / rate,
                                                //        0,
                                                //        0);
                                                //Thickness input_margin =
                                                //    new Thickness(
                                                //        (Convert.ToDouble(rect_for_rectangle.X)) / mat_list[mat_index % 5].Width * roi_rect.Width + ideal_width + roi_start_x - (rate - 1.0) / 2 * temp_rect.Width / rate,
                                                //        (Convert.ToDouble(rect_for_rectangle.Y) ) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y + ideal_height - (rate - 1.0) / 2 * temp_rect.Height / rate,
                                                //        0,
                                                //        0);

                                                ai_model_01_rect_list[temp_index].Margin =
                                                    new Thickness(
                                                        (Convert.ToDouble(rect_for_rectangle.X) * screen_wid / 1920.0) / mat_list[mat_index % 5].Width * roi_rect.Width + roi_start_x /*+ ideal_width - (rate - 1.0) / 2 * ai_model_01_rect_list[temp_index].Width / rate */+ Convert.ToDouble(ai_model_01_matching_result) * roi_rect.Width / mat_list[mat_index % 5].Width,
                                                        (Convert.ToDouble(rect_for_rectangle.Y) * screen_hei / 1080.0) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y /*+ ideal_height - (rate - 1.0) / 2 * ai_model_01_rect_list[temp_index].Height / rate*/,
                                                        0,
                                                        0);
                                                Thickness input_margin =
                                                    new Thickness(
                                                        (Convert.ToDouble(rect_for_rectangle.X) * screen_wid / 1920.0) / mat_list[mat_index % 5].Width * roi_rect.Width + roi_start_x /*+ ideal_width - (rate - 1.0) / 2 * ai_model_01_rect_list[temp_index].Width / rate*/,
                                                        (Convert.ToDouble(rect_for_rectangle.Y) * screen_hei / 1080.0) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y /*+ ideal_height - (rate - 1.0) / 2 * ai_model_01_rect_list[temp_index].Height / rate*/,
                                                        0,
                                                        0);

                                                ai_model_01_rect_list[temp_index].HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                                                ai_model_01_rect_list[temp_index].VerticalAlignment = System.Windows.VerticalAlignment.Top;

                                                ai_model_01_margin_list.Add(input_margin);
                                                temp_index++;
                                                if (temp_index >= 30)
                                                    break;
                                            }
                                    }
                                }
                                ai_model_01_count = temp_index;
                            }
                        }
                        if (ai_model_02_AIset.number_of_class > 0 && detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_02] == true)
                        {
                            pre_ai_model_02_matching_result = AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02);
                            AI_Vid_Demo.ImgManager.UpdateMatchingResult_2(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02);
                            ai_model_02_matching_result = AI_Vid_Demo.ImgManager.GetMatchingResult(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02);
                            detection_flag[(int)AI_Vid_Demo.DetectorIndices.ai_model_02] = false;

                            for (int xp = ai_model_01_AIset.number_of_class; xp < ai_model_01_AIset.number_of_class + ai_model_02_AIset.number_of_class; xp++)
                            {
                                full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                if (parameter_send[2 + 2 * (ai_model_02_on[xp - ai_model_01_AIset.number_of_class] + 5)] == 1)
                                    full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                                else
                                    full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                                if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                {
                                    if (parameter_send[2 + (ai_model_02_on[xp - ai_model_01_AIset.number_of_class] + 5) * 2] == 1)
                                    {
                                        BitmapImage bt = new BitmapImage();
                                        bt.BeginInit();
                                        bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                        bt.EndInit();
                                        image_list[xp].Source = bt;
                                    }
                                }
                            }
                            if (ai_model_02_count > 0)
                            {
                                ai_model_02_count = 0;
                                ai_model_02_margin_list.Clear();
                            }
                            for (int i = 0; i < 30; i++)
                                ai_model_02_rect_list[i].Visibility = System.Windows.Visibility.Hidden;

                            init_alarm_ai_model_02();
                            if (((pre_ai_model_02_matching_result > 0 || (pre_ai_model_02_matching_result == 0 && ai_model_02_matching_result == 0)) || (pre_ai_model_02_matching_result < 0 || (pre_ai_model_02_matching_result == 0 && ai_model_02_matching_result == 0))) /*&& fast_check == 1*/)
                            {
                                int temp_index = 0;
                                foreach (DetectItem detResult in ai_model_02_result)
                                {
                                    double det_val = (1 - detResult.Confidence);
                                    if (det_val <= 0.99)
                                    {
                                        int det_class = Convert.ToInt32(detResult.Type);
                                        if (ai_model_02_on.Contains(ai_model_02_AIset.result_converter[det_class]))
                                            if (det_val < ai_model_02_AIset.confidence_list[det_class] && parameter_send[(ai_model_02_AIset.result_converter[det_class] + 5) * 2 + 2] == 1)
                                            {
                                                rect_for_rectangle.X = Convert.ToInt32(Convert.ToDouble(detResult.X) * 1920.0 / resize_width);
                                                rect_for_rectangle.Y = Convert.ToInt32(Convert.ToDouble(detResult.Y) * 996.0 / resize_height);
                                                rect_for_rectangle.Width = Convert.ToInt32(Convert.ToDouble(detResult.Width) * 1920.0 / resize_width);
                                                rect_for_rectangle.Height = Convert.ToInt32(Convert.ToDouble(detResult.Height) * 996.0 / resize_height);

                                                SolidColorBrush brush = returnai_model_02Color_brush(det_class);
                                                System.Windows.Shapes.Rectangle temp_rect = new System.Windows.Shapes.Rectangle();

                                                if (brush != null)
                                                {
                                                    full_label_list[ai_model_02_on.IndexOf(ai_model_02_AIset.result_converter[det_class]) + ai_model_01_AIset.number_of_class].Background = brush;
                                                    full_label_list[ai_model_02_on.IndexOf(ai_model_02_AIset.result_converter[det_class]) + ai_model_01_AIset.number_of_class].Foreground = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                                                    //add_alarm(ai_model_01_AIset.number_of_class + ai_model_02_AIset.result_converter[det_class]);
                                                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                                                    {
                                                        BitmapImage bt = new BitmapImage();
                                                        bt.BeginInit();
                                                        bt.UriSource = new Uri(image_list[ai_model_02_on.IndexOf(ai_model_02_AIset.result_converter[det_class]) + ai_model_01_AIset.number_of_class].Source.ToString().Replace("_on", "_off"));
                                                        bt.EndInit();
                                                        image_list[ai_model_02_on.IndexOf(ai_model_02_AIset.result_converter[det_class]) + ai_model_01_AIset.number_of_class].Source = bt;
                                                    }
                                                }

                                                ai_model_02_rect_list[temp_index].Tag = "ai_model_02_AIset";
                                                ai_model_02_rect_list[temp_index].Name = "ai_model_02_rect";
                                                ai_model_02_rect_list[temp_index].Height = Convert.ToDouble(rect_for_rectangle.Height) / mat_list[mat_index % 5].Height * roi_rect.Height * rate * screen_hei / 1080.0;
                                                ai_model_02_rect_list[temp_index].Width = Convert.ToDouble(rect_for_rectangle.Width) / mat_list[mat_index % 5].Width * roi_rect.Width * rate * screen_wid / 1920.0;
                                                ai_model_02_rect_list[temp_index].Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, brush.Color.R, brush.Color.G, brush.Color.B));
                                                ai_model_02_rect_list[temp_index].Stroke = brush;
                                                ai_model_02_rect_list[temp_index].StrokeThickness = border_thickness_list[ai_model_01_AIset.number_of_class + ai_model_02_AIset.result_converter[det_class]];
                                                ai_model_02_rect_list[temp_index].RadiusX = 10;
                                                ai_model_02_rect_list[temp_index].RadiusY = 10;
                                                ai_model_02_rect_list[temp_index].Visibility = System.Windows.Visibility.Visible;

                                                ai_model_02_rect_list[temp_index].Margin =
                                                    new Thickness(
                                                        (Convert.ToDouble(rect_for_rectangle.X) * screen_wid / 1920.0) / mat_list[mat_index % 5].Width * roi_rect.Width + roi_start_x + /*ideal_width - (rate - 1.0) / 2 * ai_model_02_rect_list[temp_index].Width / rate +*/ Convert.ToDouble(ai_model_02_matching_result) * roi_rect.Width / mat_list[mat_index % 5].Width,
                                                        (Convert.ToDouble(rect_for_rectangle.Y) * screen_hei / 1080.0) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y /*+ ideal_height - (rate - 1.0) / 2 * ai_model_02_rect_list[temp_index].Height / rate*/,
                                                        0,
                                                        0);
                                                Thickness input_margin =
                                                    new Thickness(
                                                        (Convert.ToDouble(rect_for_rectangle.X) * screen_wid / 1920.0) / mat_list[mat_index % 5].Width * roi_rect.Width + roi_start_x /*+ ideal_width - (rate - 1.0) / 2 * ai_model_02_rect_list[temp_index].Width / rate*/,
                                                        (Convert.ToDouble(rect_for_rectangle.Y) * screen_hei / 1080.0) / mat_list[mat_index % 5].Height * roi_rect.Height + roi_start_y /*+ ideal_height - (rate - 1.0) / 2 * ai_model_02_rect_list[temp_index].Height / rate*/,
                                                        0,
                                                        0);

                                                ai_model_02_rect_list[temp_index].HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                                                ai_model_02_rect_list[temp_index].VerticalAlignment = System.Windows.VerticalAlignment.Top;

                                                ai_model_02_margin_list.Add(input_margin);
                                                temp_index++;
                                                if (temp_index >= 30)
                                                    break;
                                            }
                                    }
                                }
                                ai_model_02_count = temp_index;
                            }
                        }
                    }
                    box_drawing_check = 0;
                }
                else
                {
                    for (int xp = 0; xp < full_label_list.Count; xp++)
                    {
                        if (xp < ai_model_01_on.Count)
                        {
                            full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 118, 131, 142));
                            if (parameter_send[2 + 2 * ai_model_01_on[xp]] == 1)
                                full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                            else
                                full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                            {
                                if (parameter_send[2 + ai_model_01_on[xp] * 2] == 1)
                                {
                                    BitmapImage bt = new BitmapImage();
                                    bt.BeginInit();
                                    bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                    bt.EndInit();
                                    image_list[xp].Source = bt;
                                }
                            }
                        }
                        else
                        {
                            full_label_list[xp].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                            if (parameter_send[2 + 2 * (ai_model_02_on[xp - ai_model_01_on.Count] + 5)] == 1)
                                full_label_list[xp].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                            else
                                full_label_list[xp].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                            {
                                if (parameter_send[2 + (ai_model_02_on[xp - ai_model_01_AIset.number_of_class] + 5) * 2] == 1)
                                {
                                    BitmapImage bt = new BitmapImage();
                                    bt.BeginInit();
                                    bt.UriSource = new Uri(image_list[xp].Source.ToString().Replace("_off", "_on"));
                                    bt.EndInit();
                                    image_list[xp].Source = bt;
                                }
                            }
                        }
                    }
                    for (int i = 0; i < 30; i++)
                    {
                        ai_model_01_rect_list[i].Visibility = System.Windows.Visibility.Hidden;
                        ai_model_02_rect_list[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    ai_model_01_count = 0;
                    ai_model_01_margin_list.Clear();
                    ai_model_02_count = 0;
                    ai_model_02_margin_list.Clear();
                    init_alarm();
                }
                #endregion
                box.Stop();


                #region show_bright_Image
                //Mat print = mat_list[mat_index % 5].Mul(one, 1.2);
                //WriteableBitmapConverter.ToWriteableBitmap(print, image_print_bitmap);
                #endregion

                #region show_Image
                //video_show_image.Source = ByteImageConverter.ByteToImage(temp_byte);
                //video_show_image.Source = byteArrayToImage(temp_byte);
                //IntPtr ptr = mat_list[mat_index % 5].Data;
                //Cv2.ImShow("show", mat_list[mat_index % 5]);
                WriteableBitmapConverter.ToWriteableBitmap(mat_list[mat_index % 5], image_print_bitmap);
                //Cv2.ImShow("temp", mat_list[mat_index % 5][roi_rect]);
                #endregion

                db.Restart();
                #region db
                int dis_check = 0;
                int wholeDis = 0;

                if (ai_model_01_AIset.number_of_class > 0)
                {
                    dis_check = ai_model_01_matching_result;
                    wholeDis = AI_Vid_Demo.ImgManager.GetSaveDistance(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01);
                }
                else if (ai_model_02_AIset.number_of_class > 0)
                {
                    dis_check = ai_model_02_matching_result;
                    wholeDis = AI_Vid_Demo.ImgManager.GetSaveDistance(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02);
                }

                sub_max = sub_max > (Math.Abs(wholeDis) - Math.Abs(pre_dis)) ? sub_max : (wholeDis - pre_dis);
                pre_dis = wholeDis;
                //if (sub_max > 300)
                //    fast_check = 0;

                if (Math.Abs(dis_check) > 30 && image_db_save_check == 4)
                    image_db_save_check = 1;

                if ((/*Math.Abs(dis_check) < 30 || */Math.Abs(wholeDis) > 640)/* && ai_check == 1 && image_db_save_check == 1 && db_save_check == -1 && ai_model_01_matching_result < 300*/)
                {
                    sub_max = 300;
                    pre_dis = 0;
                    fast_check = 1;
                    box_drawing_check = 0;
                    time = DateTime.Now;
                    save_index = mat_index;
                    AI_Vid_Demo.ImgManager.ResetSaveDistance(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_01);
                    AI_Vid_Demo.ImgManager.ResetSaveDistance(index: (int)AI_Vid_Demo.DetectorIndices.ai_model_02);
                    db_save_check = 0;
                    list_ai_model_01 = new List<object[]>();
                    list_ai_model_02 = new List<object[]>();

                    db_ai_model_01_input_list = new List<object[]>();
                    db_ai_model_02_input_list = new List<object[]>();

                    #region save_DB
                    image_db_save_check = 3;
                    info_insert = new string[7];
                    input_pk = new object[7];
                    time.ToString("yyyy-MM-dd_HH:mm:ss.ffffff");
                    input_pk[1] = "A CON";
                    input_pk[2] = "B FAC";
                    input_pk[3] = "C LINE";
                    input_pk[4] = "D DEV";
                    input_pk[5] = time.ToString("yyyy-MM-dd HH:mm:ss"); ;
                    input_pk[6] = time.ToString("yyyy-MM-dd_HH_mm_ss.ffffff") + ".jpg";

                    info_insert[0] = "PK";
                    info_insert[1] = "COUNTRY";
                    info_insert[2] = "FACNAME";
                    info_insert[3] = "FACNUMB";
                    info_insert[4] = "DEVNUMB";
                    info_insert[5] = "DATE";
                    info_insert[6] = "FILEPATH";

                    input_pk[0] = pk_index;
                    if (ai_model_01_AIset.number_of_class > 0 && ai_model_01_result_temp != null)
                        foreach (DetectItem detResult in ai_model_01_result_temp)
                        {
                            double det_val = (1 - detResult.Confidence);
                            if (det_val <= 0.99)
                            {
                                int det_class = Convert.ToInt32(detResult.Type);
                                if (!(ai_model_01_on.Contains(ai_model_01_AIset.result_converter[det_class])))
                                    continue;
                                int sens = 0;
                                if (det_val < ai_model_01_AIset.confidence_list[det_class] && parameter_send[ai_model_01_AIset.result_converter[det_class] * 2 + 2] == 1)
                                {
                                    sens = 1;
                                }
                                int class_d = 0;
                                class_d = ai_model_01_AIset.result_converter[det_class];
                                class_d += 1;
                                db_ai_model_01_input = new object[8];
                                db_ai_model_01_input[0] = pk_index;
                                db_ai_model_01_input[1] = class_d;
                                db_ai_model_01_input[4] = (Convert.ToDouble(detResult.Width) / 1920.0);
                                db_ai_model_01_input[5] = (Convert.ToDouble(detResult.Height) / 996.0);
                                db_ai_model_01_input[2] = (Convert.ToDouble(detResult.X) / 1920.0 + (ai_model_01_matching_result * roi_rect.Width / mat_list[mat_index % 5].Width) / 1920.0);
                                db_ai_model_01_input[3] = (Convert.ToDouble(detResult.Y + ideal_height) / 996.0);
                                db_ai_model_01_input[6] = detResult.Confidence;
                                db_ai_model_01_input[7] = sens;
                                db_input_list.Add(db_ai_model_01_input);
                                list_ai_model_01.Add(db_ai_model_01_input);
                            }
                        }

                    if (ai_model_02_AIset.number_of_class > 0 && ai_model_02_result_temp != null)
                        foreach (DetectItem detResult in ai_model_02_result_temp)
                        {
                            double det_val = (1 - detResult.Confidence);
                            if (det_val <= 0.99)
                            {
                                int det_class = Convert.ToInt32(detResult.Type);
                                if (!(ai_model_02_on.Contains(ai_model_02_AIset.result_converter[det_class])))
                                    continue;
                                int sens = 0;
                                int class_d = 0;
                                if (det_val < ai_model_02_AIset.confidence_list[det_class] && parameter_send[(ai_model_02_AIset.result_converter[det_class] + 5) * 2 + 2] == 1)
                                {
                                    sens = 1;
                                }
                                class_d = ai_model_02_AIset.result_converter[det_class];
                                class_d += 6;
                                db_ai_model_02_input = new object[8];
                                db_ai_model_02_input[0] = pk_index;
                                db_ai_model_02_input[1] = class_d;
                                db_ai_model_02_input[4] = (Convert.ToDouble(detResult.Width) / resize_width);
                                db_ai_model_02_input[5] = (Convert.ToDouble(detResult.Height) / resize_height);
                                db_ai_model_02_input[2] = (Convert.ToDouble(detResult.X) / resize_width + (ai_model_02_matching_result * roi_rect.Width / mat_list[mat_index % 5].Width) / 1920.0);
                                db_ai_model_02_input[3] = (Convert.ToDouble(detResult.Y) / resize_height);
                                db_ai_model_02_input[6] = detResult.Confidence;
                                db_ai_model_02_input[7] = sens;
                                db_input_list.Add(db_ai_model_02_input);
                                list_ai_model_02.Add(db_ai_model_02_input);
                            }
                        }
                    bg_saveimage.RunWorkerAsync();
                    #endregion
                    db_save_check = 1;

                    dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"IMAGES\" + time.ToString("yyyy-MM-dd");
                    DirectoryInfo directory_path = new DirectoryInfo(dir_path);
                    if (directory_path.Exists == false)
                    {
                        directory_path.Create();
                    }
                    dir_path = (setting_save[(int)SettingFlagIndex.save_dir_path]) + @"RESULTS\" + time.ToString("yyyy-MM-dd");
                    directory_path = new DirectoryInfo(dir_path);
                    if (directory_path.Exists == false)
                    {
                        directory_path.Create();
                    }

                    ai_check = 0;
                    image_db_save_check = 4;

                }

                //#region led_select
                ////if (alarm_check != alarm && parameter_send[1] == 1)
                ////{
                ////    fixed (byte* temp = alarm)
                ////        Usb_Qu_write(0, 0, temp);
                ////}
                ////else if (parameter_send[1] == 0)
                ////{
                ////    for (int i = 0; i < 6; i++)
                ////        alarm[i] = 0;
                ////    fixed (byte* temp = alarm)
                ////        Usb_Qu_write(0, 0, temp);
                ////}
                //#endregion
                #endregion
                db.Stop();
                //if (matching.ElapsedMilliseconds >= 7)
                //    Console.WriteLine("MATCHING, " + matching.ElapsedMilliseconds + ", BOX , " + box.ElapsedMilliseconds + ", DB , " + db.ElapsedMilliseconds);

                mat_index += 1;
                /* int c = */
                Cv2.WaitKey(1);
                //while (c != -1)
                //    break;
            }
            //for (int i = 0; i < matching_error_after.Count; i++)
            //{
            //    matching_error_before[i].SaveImage(@"./before/" + i.ToString() + "_0_" + matching_error_mini[i] + ".jpg");
            //    matching_error_after[i].SaveImage(@"./before/" + i.ToString() + "_1_" + matching_error_mini[i] + ".jpg");
            //}
        }

        private SolidColorBrush returnai_model_01Color_brush(int type)
        {
            if (type < ai_model_01_AIset.confidence_list.Length)
            {
                return brush_list[ai_model_01_AIset.result_converter[type]];
            }
            else
            {
                return null;
            }
        }

        private SolidColorBrush returnai_model_02Color_brush(int type)
        {
            if (type < ai_model_02_AIset.confidence_list.Length)
            {
                return brush_list[ai_model_02_AIset.result_converter[type] + ai_model_01_AIset.number_of_class];
            }
            else
            {
                return null;
            }
        }

        private void sendSensitivityToMainConfiguration(int[] PARAMETERS, List<SolidColorBrush> brush, List<int> border)
        {
            if (grid_alarm_sensitivity.Visibility != System.Windows.Visibility.Hidden)
                grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;

            for (int i = 0; i < parameter_send.Length; i++)
                Console.WriteLine(PARAMETERS[i]);
            for (int i = 0; i < ai_model_01_AIset.confidence_list.Length; i++)
            {
                if (ai_model_01_AIset.result_converter[i] < 100)
                    ai_model_01_AIset.confidence_list[i] = Convert.ToDouble(PARAMETERS[ai_model_01_AIset.result_converter[i] * 2 + 3]) / 100.0;
            }
            for (int i = 0; i < ai_model_02_AIset.confidence_list.Length; i++)
            {
                if (ai_model_02_AIset.result_converter[i] < 100)
                    ai_model_02_AIset.confidence_list[i] = Convert.ToDouble(PARAMETERS[(5 + ai_model_02_AIset.result_converter[i]) * 2 + 3]) / 100.0;
            }

            for (int i = 2; i < PARAMETERS.Length; i += 2)
            {
                if (i < 12)
                {
                    if (ai_model_01_on.Contains(i / 2 - 1))
                    {
                        if (PARAMETERS[i] == 0)
                        {
                            if (ai_model_01_on.Count + ai_model_02_on.Count < 13)
                                full_label_list[ai_model_01_on.IndexOf(i / 2 - 1)].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source.ToString().Replace("_on", "_off"));
                                bt.EndInit();
                                image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source = bt;
                            }
                        }
                        else
                        {
                            if (ai_model_01_on.Count + ai_model_02_on.Count < 13)
                                full_label_list[ai_model_01_on.IndexOf(i / 2 - 1)].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 235, 235, 235));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source.ToString().Replace("_off", "_on"));
                                bt.EndInit();
                                image_list[ai_model_01_on.IndexOf(i / 2 - 1)].Source = bt;
                            }
                        }
                    }
                }
                else
                {
                    if (ai_model_02_on.Contains(i / 2 - 6))
                    {
                        if (PARAMETERS[i] == 0)
                        {
                            if (ai_model_01_on.Count + ai_model_02_on.Count < 13)
                                full_label_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Source.ToString().Replace("_on", "_off"));
                                bt.EndInit();
                                image_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Source = bt;
                            }
                        }
                        else
                        {
                            if (ai_model_01_on.Count + ai_model_02_on.Count < 13)
                                full_label_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 235, 235, 235));
                            else
                            {
                                BitmapImage bt = new BitmapImage();
                                bt.BeginInit();
                                bt.UriSource = new Uri(image_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Source.ToString().Replace("_off", "_on"));
                                bt.EndInit();
                                image_list[ai_model_01_on.Count + ai_model_02_on.IndexOf(i / 2 - 6)].Source = bt;
                            }
                        }
                    }
                }
            }

            //for (int i = 1; i < 1 + slider_list.Count; i++)
            //{
            //    if (PARAMETERS[i * 2] == 1)
            //        slider_list[i - 1].Value = PARAMETERS[i * 2 + 1];
            //    else
            //        slider_list[i - 1].Value = 0;
            //    label_list[i - 1].Content = Convert.ToInt32(slider_list[i - 1].Value) + "%";
            //}

            parameter_send = PARAMETERS;
            brush_list = brush;
            border_thickness_list = border;
        }

        private void handlerForPopupClose()
        {
            #region start_RoiSetting

            //video_show_image.Visibility = System.Windows.Visibility.Visible;

            roi_click_check = 1;
            roi_scale.ScaleX = 1;
            roi_scale.ScaleY = 1;
            roi_scale.CenterX = roi_scroll.ActualWidth / 2;
            roi_scale.CenterY = roi_scroll.ActualHeight / 2;
            image_scale.ScaleX = 1;
            image_scale.ScaleY = 1;
            image_scale.CenterX = image_scroll.ActualWidth / 2;
            image_scale.CenterY = image_scroll.ActualHeight / 2;
            roi_start_x = 0;
            roi_start_y = 0;
            roi_end_x = start_size_x;
            roi_end_y = start_size_y;
            roi_rect = new OpenCvSharp.Rect(0, 0, frame_width, frame_height);
            click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
            button_close.Visibility = System.Windows.Visibility.Hidden;
            button_menu.Visibility = System.Windows.Visibility.Visible;
            grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            #endregion
        }

        private void sendPresetToMain(string preset_name)
        {
            if (preset_user != "DEFAULT")
            {
                string[] preset_save = new string[15];
                int i = 0;
                for (i = 0; i < 13; i++)
                {
                    preset_save[i] = Convert.ToString(i + 1) + " " + parameter_send[3 + i * 2] + " " + brush_list[i].Color.A + " " + brush_list[i].Color.R + " " + brush_list[i].Color.G + " " + brush_list[i].Color.B + " " + parameter_send[2 + i * 2] + " " + border_thickness_list[i];
                }
                preset_save[i] = "-1 " + parameter_send[0];
                preset_save[i + 1] = "-2 " + parameter_send[1];

                using (StreamWriter output_file_name = new StreamWriter(@"./preset/" + preset_user + ".txt"))
                {
                    for (i = 0; i < preset_save.Length; i++)
                    {
                        output_file_name.WriteLine(preset_save[i]);
                    }
                }
            }
            preset_user = preset_name;
            Console.WriteLine(preset_user);
            if (File.Exists(@"./preset/" + preset_user + ".txt"))
            {
                string[] read_setting_string_lits = File.ReadAllLines(@"./preset/" + preset_user + ".txt");
                foreach (string read_setting_string in read_setting_string_lits)
                {
                    string[] read_setting_string_split = new string[8];
                    read_setting_string_split = read_setting_string.Split(' ');
                    int index = Convert.ToInt32(read_setting_string_split[0]);
                    if (index < 15)
                    {
                        if (index > 0)
                        {
                            if (index <= 5)
                            {
                                for (int j = 0; j < ai_model_01_AIset.result_converter.Length; j++)
                                {
                                    if (ai_model_01_AIset.result_converter[j] == index - 1)
                                    {
                                        ai_model_01_AIset.confidence_list[j] = Convert.ToInt32(read_setting_string_split[1]) / 100.0;
                                    }
                                }
                                brush_list[index - 1] = new SolidColorBrush(Color.FromArgb(Convert.ToByte(read_setting_string_split[2]), Convert.ToByte(read_setting_string_split[3]), Convert.ToByte(read_setting_string_split[4]), Convert.ToByte(read_setting_string_split[5])));
                            }
                            else
                            {
                                for (int j = 0; j < ai_model_02_AIset.result_converter.Length; j++)
                                {
                                    if (ai_model_02_AIset.result_converter[j] + 5 == index - 1)
                                    {
                                        ai_model_02_AIset.confidence_list[j] = Convert.ToInt32(read_setting_string_split[1]) / 100.0;
                                    }
                                }
                                brush_list[index - 1] = new SolidColorBrush(Color.FromArgb(Convert.ToByte(read_setting_string_split[2]), Convert.ToByte(read_setting_string_split[3]), Convert.ToByte(read_setting_string_split[4]), Convert.ToByte(read_setting_string_split[5])));
                            }
                            parameter_send[index * 2] = Convert.ToInt32(read_setting_string_split[6]);
                            if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                            {
                                if (parameter_send[index * 2] == 1)
                                {
                                    if (index < 6)
                                    {
                                        if (ai_model_01_on.Contains(index - 1))
                                        {
                                            int temp_index = ai_model_01_on.IndexOf(index - 1);
                                            BitmapImage bt = new BitmapImage();
                                            bt.BeginInit();
                                            bt.UriSource = new Uri(image_list[temp_index].Source.ToString().Replace("_off", "_on"));
                                            bt.EndInit();
                                            image_list[temp_index].Source = bt;
                                        }
                                    }
                                    else
                                    {
                                        if (ai_model_02_on.Contains(index - 6))
                                        {
                                            int temp_index = ai_model_02_on.IndexOf(index - 6);
                                            BitmapImage bt = new BitmapImage();
                                            bt.BeginInit();
                                            bt.UriSource = new Uri(image_list[temp_index + ai_model_01_on.Count].Source.ToString().Replace("_off", "_on"));
                                            bt.EndInit();
                                            image_list[temp_index + ai_model_01_on.Count].Source = bt;
                                        }
                                    }
                                }
                                else if (parameter_send[index * 2] == 0)
                                {
                                    if (index < 6)
                                    {
                                        if (ai_model_01_on.Contains(index - 1))
                                        {
                                            int temp_index = ai_model_01_on.IndexOf(index - 1);
                                            BitmapImage bt = new BitmapImage();
                                            bt.BeginInit();
                                            bt.UriSource = new Uri(image_list[temp_index].Source.ToString().Replace("_on", "_off"));
                                            bt.EndInit();
                                            image_list[temp_index].Source = bt;
                                        }
                                    }
                                    else
                                    {
                                        if (ai_model_02_on.Contains(index - 6))
                                        {
                                            int temp_index = ai_model_02_on.IndexOf(index - 6);
                                            BitmapImage bt = new BitmapImage();
                                            bt.BeginInit();
                                            bt.UriSource = new Uri(image_list[temp_index + ai_model_01_on.Count].Source.ToString().Replace("_on", "_off"));
                                            bt.EndInit();
                                            image_list[temp_index + ai_model_01_on.Count].Source = bt;
                                        }
                                    }
                                }
                            }
                            parameter_send[1 + 2 * index] = Convert.ToInt32(read_setting_string_split[1]);
                            border_thickness_list[index - 1] = Convert.ToInt32(read_setting_string_split[7]);
                        }
                    }
                    if (index == -1)
                        parameter_send[0] = Convert.ToInt32(read_setting_string_split[1]);
                    else if (index == -2)
                        parameter_send[1] = Convert.ToInt32(read_setting_string_split[1]);

                }
                if (lang == 0 && preset_user == "default")
                    label_Preset.Content = "기본값";
                else
                    label_Preset.Content = preset_user.ToUpper();
            }
        }

        private void click_GridButton(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (grid_sidebar.Visibility != Visibility.Hidden)
            {
                grid_sidebar.Visibility = Visibility.Hidden;
                roi_scroll.SetValue(Grid.ColumnSpanProperty, 2);
                grid_alarm.SetValue(Grid.ColumnSpanProperty, 2);
                image_setting.SetValue(Grid.ColumnSpanProperty, 2);
            }
            else
            {
                grid_sidebar.Visibility = Visibility.Visible;
                image_setting.SetValue(Grid.ColumnSpanProperty, 1);
                roi_scroll.SetValue(Grid.ColumnSpanProperty, 1);
                grid_alarm.SetValue(Grid.ColumnSpanProperty, 1);
            }
        }

        private void click_GridCloseButton(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (grid_sidebar.Visibility == System.Windows.Visibility.Hidden)
            {
                for (int i = 0; i < full_label_list.Count; i++)
                    full_label_list[i].FontWeight = FontWeights.Normal;
                grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 35, 44, 57));
                button_close.Visibility = System.Windows.Visibility.Visible;
                button_menu.Visibility = System.Windows.Visibility.Hidden;
                grid_sidebar.Visibility = System.Windows.Visibility.Visible;
            }
            else if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
            {
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                button_close.Visibility = System.Windows.Visibility.Hidden;
                button_menu.Visibility = System.Windows.Visibility.Visible;
                grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void click_AlarmLabel(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Label)
            {
                if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
                {
                    click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                    button_close.Visibility = System.Windows.Visibility.Hidden;
                    button_menu.Visibility = System.Windows.Visibility.Visible;
                    grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
                }
                int index = full_label_list.IndexOf((Label)sender);

                if (index < ai_model_01_on.Count)
                {
                    if (parameter_send[2 + 2 * ai_model_01_on[index]] == 0)
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                        slider_value.IsEnabled = false;
                    }
                    else
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                        slider_value.IsEnabled = true;
                    }
                }
                else
                {
                    if (parameter_send[2 + 2 * (ai_model_02_on[index - ai_model_01_on.Count] + 5)] == 0)
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                        slider_value.IsEnabled = false;
                    }
                    else
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                        slider_value.IsEnabled = true;
                    }
                }

                if (grid_alarm_sensitivity.Visibility == System.Windows.Visibility.Hidden)
                {
                    grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Visible;
                    grid_alarm_sensitivity.Margin = new System.Windows.Thickness(alarm_margin_left * index, 0, 0, 0);
                    full_label_list[index].FontWeight = FontWeights.Bold;
                }
                else if (grid_alarm_sensitivity.Margin.Left / alarm_margin_left != index)
                {
                    grid_alarm_sensitivity.Margin = new System.Windows.Thickness(alarm_margin_left * index, 0, 0, 0);
                    for (int i = 0; i < full_label_list.Count; i++)
                        full_label_list[i].FontWeight = FontWeights.Normal;
                    full_label_list[index].FontWeight = FontWeights.Bold;
                }
                else
                {
                    grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                    full_label_list[index].FontWeight = FontWeights.Normal;
                }
                if (((Label)sender).Tag.ToString() == "main_alarm_label_ai_model_01")
                {
                    for (int j = 0; j < ai_model_01_AIset.result_converter.Length; j++)
                    {
                        if (ai_model_01_on[index] == ai_model_01_AIset.result_converter[j])
                        {
                            ((Slider)grid_alarm_sensitivity.Children[0]).Value = ai_model_01_AIset.confidence_list[j] * 100;
                            break;
                        }
                    }
                    if (((Label)grid_alarm_sensitivity.Children[1]).Content.ToString() != "X")
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = Convert.ToInt32(((Slider)grid_alarm_sensitivity.Children[0]).Value).ToString() + "%";
                }
                else if (((Label)sender).Tag.ToString() == "main_alarm_label_ai_model_02")
                {
                    for (int j = 0; j < ai_model_02_AIset.result_converter.Length; j++)
                    {
                        if (ai_model_02_on[(index - ai_model_01_on.Count)] == ai_model_02_AIset.result_converter[j])
                        {
                            ((Slider)grid_alarm_sensitivity.Children[0]).Value = ai_model_02_AIset.confidence_list[j] * 100;
                            break;
                        }
                    }
                    if (((Label)grid_alarm_sensitivity.Children[1]).Content.ToString() != "X")
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = ((Slider)grid_alarm_sensitivity.Children[0]).Value.ToString() + "%";
                }
                if (grid_labelname.ColumnDefinitions.Count > 0)
                    grid_labelname.ColumnDefinitions.Clear();

                int class_num = 0;
                grid_labelname.Children.RemoveRange(0, grid_labelname.Children.Count);

                if (index < ai_model_01_on.Count)
                {
                    for (int i = 0; i < ai_model_01_AIset.result_converter.Length; i++)
                    {
                        if (ai_model_01_AIset.result_converter[i] == ai_model_01_on[index])
                        {

                            Label temp_label = new Label();
                            temp_label.Content = ai_model_01_AIset.class_name[lang, i];
                            grid_labelname.Children.Add(temp_label);
                            temp_label.VerticalContentAlignment = VerticalAlignment.Center;
                            temp_label.HorizontalContentAlignment = HorizontalAlignment.Center;
                            temp_label.SetValue(Grid.ColumnProperty, class_num);
                            class_num += 1;
                            temp_label.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                            temp_label.FontSize = 13;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < ai_model_02_AIset.result_converter.Length; i++)
                    {
                        if (ai_model_02_AIset.result_converter[i] == ai_model_02_on[index - ai_model_01_on.Count])
                        {

                            Label temp_label = new Label();
                            temp_label.Content = ai_model_02_AIset.class_name[lang, i];
                            grid_labelname.Children.Add(temp_label);
                            temp_label.VerticalContentAlignment = VerticalAlignment.Center;
                            temp_label.HorizontalContentAlignment = HorizontalAlignment.Center;
                            temp_label.SetValue(Grid.ColumnProperty, class_num);
                            class_num += 1;
                            temp_label.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                            temp_label.FontSize = 15;
                        }
                    }
                }
                ColumnDefinition[] temp_col = new ColumnDefinition[class_num];
                for (int i = 0; i < class_num; i++)
                {
                    temp_col[i] = new ColumnDefinition();
                    temp_col[i].Width = new GridLength(1, GridUnitType.Star);
                    grid_labelname.ColumnDefinitions.Add(temp_col[i]);
                }
            }
            else if (sender is Image)
            {
                if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
                {
                    click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                    button_close.Visibility = System.Windows.Visibility.Hidden;
                    button_menu.Visibility = System.Windows.Visibility.Visible;
                    grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
                }
                int index = image_list.IndexOf((Image)sender);

                if (index < ai_model_01_on.Count)
                {
                    if (parameter_send[2 + 2 * ai_model_01_on[index]] == 0)
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                        slider_value.IsEnabled = false;
                    }
                    else
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                        slider_value.IsEnabled = true;
                    }
                }
                else
                {
                    if (parameter_send[2 + 2 * (ai_model_02_on[index - ai_model_01_on.Count] + 5)] == 0)
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                        slider_value.IsEnabled = false;
                    }
                    else
                    {
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                        slider_value.IsEnabled = true;
                    }
                }

                if (grid_alarm_sensitivity.Visibility == System.Windows.Visibility.Hidden)
                {
                    grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Visible;
                    grid_alarm_sensitivity.Margin = new System.Windows.Thickness(alarm_margin_left * index, 0, 0, 0);
                    full_label_list[index].FontWeight = FontWeights.Bold;
                }
                else if (grid_alarm_sensitivity.Margin.Left / alarm_margin_left != index)
                {
                    grid_alarm_sensitivity.Margin = new System.Windows.Thickness(alarm_margin_left * index, 0, 0, 0);
                    for (int i = 0; i < full_label_list.Count; i++)
                        full_label_list[i].FontWeight = FontWeights.Normal;
                    full_label_list[index].FontWeight = FontWeights.Bold;
                }
                else
                {
                    grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = "%";
                    full_label_list[index].FontWeight = FontWeights.Normal;
                }
                if ((full_label_list[index]).Tag.ToString() == "main_alarm_label_ai_model_01")
                {
                    for (int j = 0; j < ai_model_01_AIset.result_converter.Length; j++)
                    {
                        if (ai_model_01_on[index] == ai_model_01_AIset.result_converter[j])
                        {
                            ((Slider)grid_alarm_sensitivity.Children[0]).Value = ai_model_01_AIset.confidence_list[j] * 100;
                            break;
                        }
                    }
                    if (((Label)grid_alarm_sensitivity.Children[1]).Content.ToString() != "X")
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = Convert.ToInt32(((Slider)grid_alarm_sensitivity.Children[0]).Value).ToString() + "%";
                }
                else if ((full_label_list[index]).Tag.ToString() == "main_alarm_label_ai_model_02")
                {
                    for (int j = 0; j < ai_model_02_AIset.result_converter.Length; j++)
                    {
                        if (ai_model_02_on[(index - ai_model_01_on.Count)] == ai_model_02_AIset.result_converter[j])
                        {
                            ((Slider)grid_alarm_sensitivity.Children[0]).Value = ai_model_02_AIset.confidence_list[j] * 100;
                            break;
                        }
                    }
                    if (((Label)grid_alarm_sensitivity.Children[1]).Content.ToString() != "X")
                        ((Label)grid_alarm_sensitivity.Children[1]).Content = ((Slider)grid_alarm_sensitivity.Children[0]).Value.ToString() + "%";
                }

                if (grid_labelname.ColumnDefinitions.Count > 0)
                    grid_labelname.ColumnDefinitions.Clear();
                int class_num = 0;
                grid_labelname.Children.RemoveRange(0, grid_labelname.Children.Count);

                if (index < ai_model_01_on.Count)
                {
                    for (int i = 0; i < ai_model_01_AIset.result_converter.Length; i++)
                    {
                        if (ai_model_01_AIset.result_converter[i] == ai_model_01_on[index])
                        {

                            Label temp_label = new Label();
                            temp_label.Content = ai_model_01_AIset.class_name[lang, i];
                            grid_labelname.Children.Add(temp_label);
                            temp_label.VerticalContentAlignment = VerticalAlignment.Center;
                            temp_label.HorizontalContentAlignment = HorizontalAlignment.Center;
                            temp_label.SetValue(Grid.ColumnProperty, class_num);
                            class_num += 1;
                            temp_label.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                            temp_label.FontSize = 15;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < ai_model_02_AIset.result_converter.Length; i++)
                    {
                        if (ai_model_02_AIset.result_converter[i] == ai_model_02_on[index - ai_model_01_on.Count])
                        {

                            Label temp_label = new Label();
                            temp_label.Content = ai_model_02_AIset.class_name[lang, i];
                            grid_labelname.Children.Add(temp_label);
                            temp_label.VerticalContentAlignment = VerticalAlignment.Center;
                            temp_label.HorizontalContentAlignment = HorizontalAlignment.Center;
                            temp_label.SetValue(Grid.ColumnProperty, class_num);
                            class_num += 1;
                            temp_label.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                            temp_label.FontSize = 15;
                        }
                    }
                }
                ColumnDefinition[] temp_col = new ColumnDefinition[class_num];
                for (int i = 0; i < class_num; i++)
                {
                    temp_col[i] = new ColumnDefinition();
                    temp_col[i].Width = new GridLength(1, GridUnitType.Star);
                    grid_labelname.ColumnDefinitions.Add(temp_col[i]);
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Grid parent = ((Grid)((Slider)sender).Parent);
            if (((Label)grid_alarm_sensitivity.Children[1]).Content.ToString() != "X")
                ((Label)parent.Children[1]).Content = ((Slider)sender).Value.ToString() + "%";

            int index = Convert.ToInt32(parent.Margin.Left / alarm_margin_left);

            if (index < ai_model_01_on.Count)
            {
                for (int j = 0; j < ai_model_01_AIset.result_converter.Length; j++)
                {
                    if (ai_model_01_on[index] == ai_model_01_AIset.result_converter[j])
                    {
                        ai_model_01_AIset.confidence_list[j] = (((Slider)sender).Value) / 100;
                        Console.WriteLine("ai_model_01 " + j + " : " + ai_model_01_AIset.confidence_list[j]);
                    }
                }
                parameter_send[3 + ai_model_01_on[index] * 2] = Convert.ToInt32(((Slider)sender).Value);
            }
            else
            {
                for (int j = 0; j < ai_model_02_AIset.result_converter.Length; j++)
                {
                    if (ai_model_02_on[index - ai_model_01_on.Count] == ai_model_02_AIset.result_converter[j])
                    {
                        ai_model_02_AIset.confidence_list[j] = (((Slider)sender).Value) / 100;
                        Console.WriteLine("ai_model_02 " + j + " : " + ai_model_02_AIset.confidence_list[j]);
                    }
                }
                parameter_send[3 + (ai_model_02_on[index - ai_model_01_on.Count] + 5) * 2] = Convert.ToInt32(((Slider)sender).Value);
            }
        }

        private void click_SensitivityOnOffButton(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Grid parent = ((Grid)((Label)sender).Parent);

            int index = Convert.ToInt32(parent.Margin.Left) / alarm_margin_left;
            if (index < ai_model_01_on.Count)
            {
                if (parameter_send[2 + ai_model_01_on[index] * 2] == 1)
                {
                    parameter_send[2 + ai_model_01_on[index] * 2] = 0;
                    full_label_list[index].Foreground = new SolidColorBrush(Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                    slider_value.IsEnabled = false;
                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                    {
                        BitmapImage bt = new BitmapImage();
                        bt.BeginInit();
                        bt.UriSource = new Uri(image_list[index].Source.ToString().Replace("_on", "_off"));
                        bt.EndInit();
                        image_list[index].Source = bt;
                    }

                }
                else
                {
                    parameter_send[2 + ai_model_01_on[index] * 2] = 1;
                    full_label_list[index].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = ((Slider)grid_alarm_sensitivity.Children[0]).Value + "%";
                    slider_value.IsEnabled = true;
                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                    {
                        BitmapImage bt = new BitmapImage();
                        bt.BeginInit();
                        bt.UriSource = new Uri(image_list[index].Source.ToString().Replace("_off", "_on"));
                        bt.EndInit();
                        image_list[index].Source = bt;
                    }
                }
            }
            else
            {
                if (parameter_send[2 + (ai_model_02_on[index - ai_model_01_on.Count] + 5) * 2] == 1)
                {
                    parameter_send[2 + (ai_model_02_on[index - ai_model_01_on.Count] + 5) * 2] = 0;
                    full_label_list[index].Foreground = new SolidColorBrush(Color.FromArgb(System.Drawing.Color.Gray.A, System.Drawing.Color.Gray.R, System.Drawing.Color.Gray.G, System.Drawing.Color.Gray.B));
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = "X";
                    slider_value.IsEnabled = false;
                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                    {
                        BitmapImage bt = new BitmapImage();
                        bt.BeginInit();
                        bt.UriSource = new Uri(image_list[index].Source.ToString().Replace("_on", "_off"));
                        bt.EndInit();
                        image_list[index].Source = bt;
                    }
                }
                else
                {
                    parameter_send[2 + (ai_model_02_on[index - ai_model_01_on.Count] + 5) * 2] = 1;
                    full_label_list[index].Foreground = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                    ((Label)grid_alarm_sensitivity.Children[1]).Content = ((Slider)grid_alarm_sensitivity.Children[0]).Value + "%";
                    slider_value.IsEnabled = true;
                    if (ai_model_01_on.Count + ai_model_02_on.Count >= 13)
                    {
                        BitmapImage bt = new BitmapImage();
                        bt.BeginInit();
                        bt.UriSource = new Uri(image_list[index].Source.ToString().Replace("_off", "_on"));
                        bt.EndInit();
                        image_list[index].Source = bt;
                    }
                }
            }
        }

        private void mouseEnter_Preset(object sender, System.Windows.Input.MouseEventArgs e)
        {
            preset_Background.Fill = new SolidColorBrush(Color.FromArgb(255, 22, 32, 49));
            preset_Ellipse.Fill = new SolidColorBrush(Color.FromArgb(255, 35, 44, 57));
        }

        private void mouseLeave_Preset(object sender, System.Windows.Input.MouseEventArgs e)
        {
            preset_Ellipse.Fill = new SolidColorBrush(Color.FromArgb(255, 22, 32, 49));
            preset_Background.Fill = new SolidColorBrush(Color.FromArgb(255, 35, 44, 57));
        }

        private void mouseEnter_Grid(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Grid)
            {
                Grid now = (Grid)sender;
                ((System.Windows.Shapes.Rectangle)now.Children[0]).Fill = new SolidColorBrush(Color.FromArgb(255, 22, 33, 49));
            }
            else if (sender is System.Windows.Shapes.Ellipse)
            {
                System.Windows.Shapes.Ellipse now = (System.Windows.Shapes.Ellipse)sender;
                now.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
            }
            else if (sender is Image)
            {
                Image now = (Image)sender; ((System.Windows.Shapes.Ellipse)((Grid)now.Parent).Children[3]).Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
            }
        }

        private void mouseLeave_Grid(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Grid)
            {
                Grid now = (Grid)sender;
                ((System.Windows.Shapes.Rectangle)now.Children[0]).Fill = new SolidColorBrush(Color.FromArgb(255, 35, 44, 57));
            }
            else if (sender is System.Windows.Shapes.Ellipse)
            {
                System.Windows.Shapes.Ellipse now = (System.Windows.Shapes.Ellipse)sender;
                now.Fill = new SolidColorBrush(Color.FromArgb(255, 22, 33, 49));
            }
            else if (sender is Image)
            {
                Image now = (Image)sender;
                ((System.Windows.Shapes.Ellipse)((Grid)now.Parent).Children[3]).Fill = new SolidColorBrush(Color.FromArgb(255, 22, 33, 49));
            }
        }

        private void click_Preset(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            while (db_save_check != -1)
            {
                Thread.Sleep(10);
            }
            if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
            {
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                button_close.Visibility = System.Windows.Visibility.Hidden;
                button_menu.Visibility = System.Windows.Visibility.Visible;
                grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            }
            System.Drawing.Color _black = System.Drawing.Color.Black;
            System.Windows.Media.Brush black = new SolidColorBrush(System.Windows.Media.Color.FromArgb(_black.A, _black.R, _black.G, _black.B));

            for (int i = 0; i < full_label_list.Count; i++)
                full_label_list[i].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));

            Preset preset_popup = new Preset(lang);
            preset_popup.preset_send += new Preset.preset_config(sendPresetToMain);
            this.Opacity = 0.3;
            preset_popup.ShowDialog();
            this.Opacity = 1.2;
            mat_queue.Clear();
        }

        private void click_MainConfigure(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            while (db_save_check != -1)
            {
                Thread.Sleep(10);
            }
            if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
            {
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                button_close.Visibility = System.Windows.Visibility.Hidden;
                button_menu.Visibility = System.Windows.Visibility.Visible;
                grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            }
            if (grid_alarm_sensitivity.Visibility != System.Windows.Visibility.Hidden)
                grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;
            System.Drawing.Color _black = System.Drawing.Color.Black;
            System.Windows.Media.Brush black = new SolidColorBrush(System.Windows.Media.Color.FromArgb(_black.A, _black.R, _black.G, _black.B));

            for (int i = 0; i < full_label_list.Count; i++)
                full_label_list[i].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));

            List<string> object_name = new List<string>();
            for (int i = 0; i < full_label_list.Count; i++)
                object_name.Add(full_label_list[i].Name.Replace("label_alarm_", ""));
            List<string> print_object_name = new List<string>();
            for (int i = 0; i < full_label_list.Count; i++)
                print_object_name.Add(full_label_list[i].Content.ToString());
            main_configure main_configure_popup = new main_configure(parameter_send, brush_list, border_thickness_list, object_name, print_object_name, save_dir_path, ai_model_01_on, ai_model_02_on, lang);
            main_configure_popup.sensitivity_send += new main_configure.main_config(sendSensitivityToMainConfiguration);
            main_configure_popup.close_handler += new main_configure.config_close(handlerForPopupClose);
            main_configure_popup.reset += new main_configure.main_config_2(reset_object);
            this.Opacity = 0.3;
            main_configure_popup.ShowDialog();
            this.Opacity = 1.0;
            mat_queue.Clear();
        }

        private void reset_object()
        {
            _00_selection reset = new _00_selection();
            this.Close();
            reset.ShowDialog();
        }

        private void click_Archive(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            while (db_save_check != -1)
            {
                Thread.Sleep(10);
            }
            if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
            {
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                button_close.Visibility = System.Windows.Visibility.Hidden;
                button_menu.Visibility = System.Windows.Visibility.Visible;
                grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            }
            if (grid_alarm_sensitivity.Visibility != System.Windows.Visibility.Hidden)
                grid_alarm_sensitivity.Visibility = System.Windows.Visibility.Hidden;
            for (int i = 0; i < full_label_list.Count; i++)
                full_label_list[i].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));

            List<string> object_name = new List<string>();
            for (int i = 0; i < full_label_list.Count; i++)
                object_name.Add(full_label_list[i].Name.Replace("label_alarm_", ""));
            archive archive_popup = new archive(parameter_send, brush_list, border_thickness_list, object_name, save_dir_path, ai_model_01_on, ai_model_02_on, lang);
            this.Opacity = 0.3;
            archive_popup.ShowDialog();
            this.Opacity = 1.0;
            mat_queue.Clear();
        }

        private unsafe void click_Close(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            get_check = false;
            if (grid_sidebar.Visibility == System.Windows.Visibility.Visible)
            {
                click_background.Fill = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                button_close.Visibility = System.Windows.Visibility.Hidden;
                button_menu.Visibility = System.Windows.Visibility.Visible;
                grid_sidebar.Visibility = System.Windows.Visibility.Hidden;
            }
            for (int i = 0; i < 6; i++)
                alarm[i] = 0;
            fixed (byte* temp = alarm)
                Usb_Qu_write(0, 0, temp);
            #region save_BasicSetting
            setting_save = new string[] {
                Convert.ToString(Convert.ToInt32(Convert.ToDouble(roi_rect.X)/Convert.ToDouble(frame_width)*Convert.ToDouble(screen_wid))),
                Convert.ToString(Convert.ToInt32(Convert.ToDouble(roi_rect.Y)/Convert.ToDouble(frame_height)*Convert.ToDouble(screen_hei))),
                Convert.ToString(Convert.ToInt32(Convert.ToDouble(roi_rect.X+roi_rect.Width)/Convert.ToDouble(frame_width)*Convert.ToDouble(screen_wid))),
                Convert.ToString(Convert.ToInt32(Convert.ToDouble(roi_rect.Y+roi_rect.Height)/Convert.ToDouble(frame_height)*Convert.ToDouble(screen_hei))),
                Convert.ToString(roi_scale.CenterX),
                Convert.ToString(roi_scale.CenterY),
                Convert.ToString(roi_scale.ScaleX),
                Convert.ToString(roi_scale.ScaleY),
                save_dir_path,
                Convert.ToString(bandwidth),
                Convert.ToString(detection_interval),
                Convert.ToString(ai_result_add),
                Convert.ToString(screen_wid),
                Convert.ToString(screen_hei)

            };

            using (StreamWriter output_file_name = new StreamWriter(@"./setting_dictionary.txt"))
            {
                int index = 0;
                foreach (string line in setting_save)
                {
                    output_file_name.WriteLine(typeof(SettingFlagIndex).GetEnumName(index) + " " + line);
                    index++;
                }
            }
            #endregion

            #region save_PresetSetting
            if (preset_user != "DEFAULT")
            {
                string[] preset_save = new string[15];
                int i = 0;
                for (i = 0; i < 13; i++)
                {
                    preset_save[i] = Convert.ToString(i + 1) + " " + parameter_send[3 + i * 2] + " " + brush_list[i].Color.A + " " + brush_list[i].Color.R + " " + brush_list[i].Color.G + " " + brush_list[i].Color.B + " " + parameter_send[2 + i * 2] + " " + border_thickness_list[i];
                }
                preset_save[i] = "-1 " + parameter_send[0];
                preset_save[i + 1] = "-2 " + parameter_send[1];

                using (StreamWriter output_file_name = new StreamWriter(@"./preset/" + preset_user + ".txt"))
                {
                    for (i = 0; i < preset_save.Length; i++)
                    {
                        output_file_name.WriteLine(preset_save[i]);
                    }
                }
            }
            #endregion

            #region save_input_Image
            //this.Close();
            //Mat[] result_input = mat_queue_input.ToArray();
            ////Mat[] result = list_for_calc.ToArray();
            //float XResizeCoefficient = 1.0f;
            //float YResizeCoefficient = 0.01f;

            //for (int j = 0; j < result_input.Length - 1; j++)
            //{
            //    //    Mat prior_image = result_input[j + 1];
            //    //    Mat current_image = result_input[j];
            //    //    Mat resizedPriorImage = new Mat();
            //    //    Mat resizedCurrentImage = new Mat();
            //    //    try
            //    //    {
            //    //        Cv2.Resize(src: prior_image, dst: resizedPriorImage,
            //    //            dsize: new OpenCvSharp.Size(prior_image.Width * XResizeCoefficient,
            //    //            prior_image.Height * YResizeCoefficient));
            //    //        Cv2.Resize(src: current_image, dst: resizedCurrentImage,
            //    //            dsize: new OpenCvSharp.Size(current_image.Width * XResizeCoefficient,
            //    //            current_image.Height * YResizeCoefficient));

            //    //        Mat[] resizedPriorImageComponent = resizedPriorImage.Split();
            //    //        Mat priorMax = new Mat(resizedPriorImage.Size(), resizedPriorImage.Type());
            //    //        Mat priorMin = new Mat(resizedPriorImage.Size(), resizedPriorImage.Type());
            //    //        Cv2.Max(resizedPriorImageComponent[0], resizedPriorImageComponent[1], priorMax);
            //    //        //Cv2.Max(resizedPriorImageComponent[2], priorMax, priorMax);
            //    //        Cv2.Min(resizedPriorImageComponent[0], resizedPriorImageComponent[1], priorMin);
            //    //        //Cv2.Min(resizedPriorImageComponent[2], priorMin, priorMin);
            //    //        Mat priordiff = priorMax - priorMin;

            //    //        Mat[] resizedCurrentImageComponent = resizedCurrentImage.Split();
            //    //        Mat currentMax = new Mat(resizedCurrentImage.Size(), resizedCurrentImage.Type());
            //    //        Mat currentMin = new Mat(resizedCurrentImage.Size(), resizedCurrentImage.Type());
            //    //        Cv2.Max(resizedCurrentImageComponent[0], resizedCurrentImageComponent[1], currentMax);
            //    //        //Cv2.Max(resizedCurrentImageComponent[2], currentMax, currentMax);
            //    //        Cv2.Min(resizedCurrentImageComponent[0], resizedCurrentImageComponent[1], currentMin);
            //    //        //Cv2.Min(resizedCurrentImageComponent[2], currentMin, currentMin);
            //    //        Mat currentdiff = currentMax - currentMin;

            //    //        Cv2.Threshold(priordiff, priordiff, 20, 255, ThresholdTypes.Binary);
            //    //        Cv2.Threshold(currentdiff, currentdiff, 20, 255, ThresholdTypes.Binary);

            //    //        long minerror = 9000000000;
            //    //        int mini = 10;
            //    //        Mat subimg_1, subimg_2, subimg_3;
            //    //        OpenCvSharp.Rect rect_1 = new OpenCvSharp.Rect(10, 0, priordiff.Width - 20, priordiff.Height);
            //    //        for (int i = 0; i < 21; i++)
            //    //        {
            //    //            OpenCvSharp.Rect rect_2 = new OpenCvSharp.Rect(i, 0, priordiff.Width - 20, priordiff.Height);
            //    //            subimg_1 = new Mat(currentdiff, rect_1);
            //    //            subimg_2 = new Mat(priordiff, rect_2);
            //    //            subimg_3 = new Mat(subimg_1.Size(), subimg_1.Type());
            //    //            Cv2.Subtract(subimg_1, subimg_2, subimg_3);

            //    //            Scalar error = Cv2.Sum(subimg_3);
            //    //            long error1 = (long)(error.Val0 / 255);

            //    //            if (error1 < minerror)
            //    //            {
            //    //                minerror = error1;
            //    //                mini = i;
            //    //            }
            //    //        }
            //    //        //result[j].SaveImage(@"./image/" + j.ToString() + ".png");
            //result_input[j].SaveImage(@"./get_image/" + j.ToString() + ".png");
            //    //        Console.WriteLine("diff : " + (mini - 10));

            //    //        #region
            //    //        /* 190607-R04
            //    //         * author: swkim
            //    //         * type: update
            //    //         * object: entire region block
            //    //         */
            //    //        //Console.WriteLine(mini);
            //    //        #endregion
            //    //    }
            //    //    catch (Exception exception)
            //    //    {
            //    //        DateTime current_date = DateTime.Now;
            //    //        Console.WriteLine(value: exception.ToString());
            //    //        //return distance;
            //    //    }
            //}
            #endregion

            init_alarm();

            video_capture.Dispose();
            System.GC.Collect();

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void clickMouseLeftForRoiMake(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (roi_click_check == 1)
            {
                //video_show_image.Visibility = System.Windows.Visibility.Visible;
                box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = false;
                roi_start_x = e.GetPosition(video_show_image).X;
                roi_start_y = e.GetPosition(video_show_image).Y;
                roi_start_x = (roi_start_x % 4 == 0) ? roi_start_x : roi_start_x - roi_start_x % 4;
                roi_start_y = (roi_start_y % 4 == 0) ? roi_start_y : roi_start_y - roi_start_y % 4;
                roi_click_check = 2;

                //foreach (System.Windows.Window win in App.Current.Windows)
                //{
                //    if (!win.IsFocused && win.Tag.ToString() == "mdi_child")
                //    {
                //        win.Close();
                //    }
                //}

                return;
            }
            else if (roi_click_check == 3)
            {
                //video_show_image.Visibility = System.Windows.Visibility.Hidden;
                for (int i = big_grid.Children.Count - 1; i > 0; i--)
                    if (big_grid.Children[i].GetType().Name == "Rectangle")
                        big_grid.Children.RemoveAt(i);
                roi_click_check = 0;
                box_drawing_check = 0;

                roi_scale.CenterX = roi_start_x + (roi_end_x - roi_start_x) * (roi_start_x) / (roi_start_x + roi_scroll.ActualWidth - roi_end_x);
                roi_scale.CenterY = roi_start_y + (roi_end_y - roi_start_y) * (roi_start_y) / (roi_start_y + roi_scroll.ActualHeight - roi_end_y);

                roi_scale.ScaleX = image_scroll.ActualWidth / (roi_end_x - roi_start_x);
                roi_scale.ScaleY = image_scroll.ActualHeight / (roi_end_y - roi_start_y);

                roi_rect = new OpenCvSharp.Rect(Convert.ToInt32(roi_start_x / video_show_image.ActualWidth * frame_width) / 4 * 4, Convert.ToInt32(roi_start_y / video_show_image.ActualHeight * frame_height) / 4 * 4, Convert.ToInt32((roi_end_x - roi_start_x) / video_show_image.ActualWidth * frame_width) / 4 * 4, Convert.ToInt32((roi_end_y - roi_start_y) / video_show_image.ActualHeight * frame_height) / 4 * 4);

                //_05_Showing show = new _05_Showing(roi_rect, roi_scale.CenterX, roi_scale.CenterY, roi_scale.ScaleX, roi_scale.ScaleY);
                //show.Tag = "mdi_child";
                //show.Show();

                box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = true;
                //video_show_image.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            if (roi_click_check == 0)
            {
                //    if (box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] == true)
                //    {
                //        box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = false;
                //        if (ai_model_01_AIset.number_of_class + ai_model_02_AIset.number_of_class > 10)
                //        {
                //            image_grid.Background = new SolidColorBrush(Color.FromArgb(255, 118, 131, 142));
                //        }
                //        else
                //        {

                //            label_grid.Background = new SolidColorBrush(Color.FromArgb(255, 118, 131, 142));
                //            for (int i = 0; i < full_label_list.Count; i++)
                //                full_label_list[i].Background = new SolidColorBrush(Color.FromArgb(255, 118, 131, 142));
                //        }
                //    }
                //    else if (box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] == false)
                //    {
                //        box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = true;
                //        if (ai_model_01_AIset.number_of_class + ai_model_02_AIset.number_of_class > 10)

                //            image_grid.Background = null;
                //        else
                //        {
                //            label_grid.Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                //            for (int i = 0; i < full_label_list.Count; i++)
                //                full_label_list[i].Background = new SolidColorBrush(Color.FromArgb(255, 29, 32, 39));
                //        }
                //    }
            }
        }

        private void clickMouseLeftForRoiHold(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (roi_click_check == 2)
            {
                System.Windows.Shapes.Rectangle temp_rect = new System.Windows.Shapes.Rectangle();
                temp_rect.Margin = new System.Windows.Thickness(roi_start_x, roi_start_y, 0, 0);
                roi_end_x = e.GetPosition(video_show_image).X;
                roi_end_y = e.GetPosition(video_show_image).Y;
                roi_end_x = (roi_end_x % 4 == 0) ? roi_end_x : roi_end_x + 4 - roi_end_x % 4;
                roi_end_y = (roi_end_y % 4 == 0) ? roi_end_y : roi_end_y + 4 - roi_end_y % 4;
                temp_rect.Width = roi_end_x - roi_start_x;
                temp_rect.Height = roi_end_y - roi_start_y;
                temp_rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                temp_rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                temp_rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 160, 160, 160));
                temp_rect.MouseLeftButtonDown += clickMouseLeftForRoiMake;
                temp_rect.SetValue(Grid.ColumnSpanProperty, 2);
                temp_rect.SetValue(Grid.RowSpanProperty, 2);
                big_grid.Children.Add(temp_rect);
                roi_click_check = 3;
            }
        }

        private void wheelForImageScale(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            image_scale.CenterX = e.GetPosition(canvas).X;
            image_scale.CenterY = e.GetPosition(canvas).Y;
            int mouseWheelValue = e.Delta;
            if (mouseWheelValue < 0)
            {
                image_scale.ScaleX /= 1.1;
                image_scale.ScaleY /= 1.1;

                if (image_scale.ScaleX < image_scale_x)
                    image_scale.ScaleX = image_scale_x;
                if (image_scale.ScaleY < image_scale_y)
                    image_scale.ScaleY = image_scale_y;
            }
            else if (mouseWheelValue > 0)
            {
                image_scale.ScaleX *= 1.1;
                image_scale.ScaleY *= 1.1;
            }
        }

        private void Canvas_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (roi_click_check == 1)
                roi_click_check = 0;
            //video_show_image.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Grid_alarm_sensitivity_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Grid temp = (Grid)sender;
            grid_labelname.Visibility = temp.Visibility;
        }

        private void Grid_alarm_sensitivity_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Grid temp = (Grid)sender;
            grid_labelname.Margin = new System.Windows.Thickness(temp.Margin.Left, 0, 0, temp.ActualHeight);
        }

        private void backgroundEmptyMem(object sender, DoWorkEventArgs e)
        {
            Process empty_mem = new Process();
            empty_mem.StartInfo.FileName = @".\memory.vbs";
            empty_mem.StartInfo.CreateNoWindow = true;
            empty_mem.Start();

            Thread.Sleep(20000);

            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("cmd"))
                    process.Kill();
            }
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("EmptyStandby"))
                    process.Kill();
            }
        }

        private void backgroundCheckImage(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Scalar mean_temp = mat_list[mat_index % 5][roi_rect].Mean();
                if ((Math.Abs(mean_temp.Val0 - mean_temp.Val1) < 1.0 && Math.Abs(mean_temp.Val0 - mean_temp.Val2) < 1.0 && Math.Abs(mean_temp.Val2 - mean_temp.Val1) < 1.0) || (mean_temp.Val0 < 100 && mean_temp.Val1 < 100 && mean_temp.Val2 < 100))
                {
                    image_original_check = 0;
                    ai_model_01_result = null;
                    ai_model_02_result = null;
                }
                else
                    image_original_check = 1;
                Console.WriteLine("IMAGE ORIGINAL CHECK : " + image_original_check);

                Thread.Sleep(1000);
            }
        }

        private void backgroundSaveImage(object sender, DoWorkEventArgs e)
        {
            //Mat save_mat = new Mat();
            //Cv2.Resize(mat_list[save_index % 5][roi_rect], save_mat, new OpenCvSharp.Size(1920, 996));
            //save_mat.SaveImage((setting_save[(int)SettingFlagIndex.save_dir_path]) + @"IMAGES\" + time.ToString("yyyy-MM-dd") + @"\" + time.ToString("yyyy-MM-dd_HH_mm_ss.ffffff") + ".jpg");
            //save_mat.Dispose();

            //using (StreamWriter outputFile = new StreamWriter((setting_save[(int)SettingFlagIndex.save_dir_path]) + @"IMAGES\" + time.ToString("yyyy-MM-dd") + @"\" + time.ToString("yyyy-MM-dd_HH_mm_ss.ffffff") + ".csv"))
            //{
            //    outputFile.WriteLine("ai_model_01");
            //    outputFile.WriteLine("class_id, left, top, width, height, confidence, sens");
            //    for (int i = 0; i < list_ai_model_01.Count; i++)
            //    {
            //        for (int j = 1; j < list_ai_model_01[i].Length; j++)
            //        {
            //            outputFile.Write(list_ai_model_01[i][j] + ", ");
            //        }
            //        outputFile.Write("\n");
            //    }
            //    outputFile.WriteLine("ai_model_02");
            //    outputFile.WriteLine("class_id, left, top, width, height, confidence, sens");
            //    for (int i = 0; i < list_ai_model_02.Count; i++)
            //    {
            //        for (int j = 1; j < list_ai_model_02[i].Length; j++)
            //        {
            //            outputFile.Write(list_ai_model_02[i][j] + ", ");
            //        }
            //        outputFile.Write("\n");
            //    }
            //}
        }

        //private void backgroundai_model_01AIResult(object sender, DoWorkEventArgs e)
        //{
        //    #region detection_bak
        //    //ai_model_01_manager = new ai_model_01_01K.ai_model_01Manager();
        //    //while (true)
        //    //{
        //    //    if (mat_list[mat_index % 5].Empty() == false && box_drawing_check == 0)
        //    //    {
        //    //        ideal_width = 0;
        //    //        ideal_height = 0;
        //    //        temp_mat_1 = mat_list[mat_index % 5][roi_rect];
        //    //        double origin_factor = Convert.ToDouble(canvas.ActualHeight) / Convert.ToDouble(canvas.ActualWidth);
        //    //        double mod_factor = Convert.ToDouble(temp_mat_1.Height) / Convert.ToDouble(temp_mat_1.Width);
        //    //        if (origin_factor != mod_factor)
        //    //        {
        //    //            if (mod_factor > origin_factor)
        //    //            {
        //    //                ideal_height = (temp_mat_1.Height - Convert.ToInt32(origin_factor * temp_mat_1.Width)) / 2;
        //    //                temp_mat_1 = temp_mat_1[ideal_height, temp_mat_1.Height - ideal_height, 0, temp_mat_1.Width];
        //    //            }
        //    //            else if (mod_factor < origin_factor)
        //    //            {
        //    //                ideal_width = (temp_mat_1.Width - Convert.ToInt32(temp_mat_1.Height / origin_factor)) / 2;
        //    //                temp_mat_1 = temp_mat_1[0, temp_mat_1.Height, ideal_width, temp_mat_1.Width - ideal_width];
        //    //            }
        //    //        }

        //    //        Mat padding_mat = new Mat(new OpenCvSharp.Size(temp_mat_1.Width, temp_mat_1.Height), temp_mat_1.Type());
        //    //        padding_mat[0, temp_mat_1.Height, 50, temp_mat_1.Width] = temp_mat_1[0, temp_mat_1.Height, 0, temp_mat_1.Width - 50];

        //    //        if (!Directory.Exists(@"./before"))
        //    //        {
        //    //            Directory.CreateDirectory(@"./before");
        //    //        }
        //    //        if (!Directory.Exists(@"./after"))
        //    //        {
        //    //            Directory.CreateDirectory(@"./after");
        //    //        }
        //    //        string name = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss.ffffff") + ".jpg";
        //    //        temp_mat_1.SaveImage(@"./before/" + name);
        //    //        padding_mat.SaveImage(@"./after/" + name);

        //    //        ai_model_01_result = ai_model_01_manager.AI_detect_objects(temp_mat_1.ToBytes());
        //    //        IEnumerable<YoloItem> ai_model_01_result_padding = ai_model_01_manager.AI_detect_objects_padding(padding_mat.ToBytes());
        //    //        List<YoloItem> ai_model_01_list = ai_model_01_result.ToList();
        //    //        foreach (YoloItem temp in ai_model_01_result_padding)
        //    //        {
        //    //            YoloItem sub = temp;
        //    //            sub.X = sub.X - 50;
        //    //            for (int i = 0; i < ai_model_01_list.Count; i++)
        //    //            {
        //    //                YoloItem temp_2 = ai_model_01_list[i];
        //    //                if (Math.Abs(sub.X - temp_2.X) < 15 && Math.Abs(sub.Y - temp_2.Y) < 15 && sub.Type == temp_2.Type)
        //    //                    break;
        //    //                if (i == ai_model_01_list.Count - 1)
        //    //                    ai_model_01_list.Add(sub);
        //    //            }
        //    //        }
        //    //        ai_model_01_result = (IEnumerable<YoloItem>)ai_model_01_list;
        //    //        detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_01] = true;
        //    //        box_drawing_check = 1;
        //    //    }
        //    //}
        //    #endregion

        //    temp_mat_1 = new Mat();
        //    Mat one = new Mat(new OpenCvSharp.Size(frame_width, frame_height), MatType.CV_8UC3, new Scalar(1, 1, 1));
        //    List<YoloItem> old_result = new List<YoloItem>();
        //    while (true)
        //    {
        //        #region temp_detection_off_invORgray
        //        //Scalar mean_temp = mat_list[mat_index % 5][roi_rect].Mean();
        //        //if ((Math.Abs(mean_temp.Val0 - mean_temp.Val1) < 1.0 && Math.Abs(mean_temp.Val0 - mean_temp.Val2) < 1.0 && Math.Abs(mean_temp.Val2 - mean_temp.Val1) < 1.0) || (mean_temp.Val0 < 100 && mean_temp.Val1 < 100 && mean_temp.Val2 < 100))
        //        //{
        //        //    image_original_check = 0;
        //        //    ai_model_01_result = null;
        //        //    ai_model_02_result = null;
        //        //    box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = false;
        //        //    Thread.Sleep(detection_interval);
        //        //    continue;
        //        //}
        //        //else
        //        //{
        //        //    image_original_check = 1;
        //        //    box_activation_flag[(int)Wpf.DesignFlagIndices.bounding_box_activation] = true;
        //        //}
        //        #endregion
        //        try
        //        {
        //            if (speed_check >= 30)
        //            {
        //                Thread.Sleep(detection_interval);
        //                continue;
        //            }
        //            if (mat_list[mat_index % 5].Empty() == false && box_drawing_check == 0 && image_original_check == 1)
        //            {
        //                ideal_width = 0;
        //                ideal_height = 0;
        //                //ai_model_01_01K.ImgManager.ResetAddMatching();
        //                ai_model_01_01K.ImgManager.UpdateMatchingResult(index: (int)ai_model_01_01K.DetectorIndices.ai_model_01);
        //                //int gap = ai_model_01_01K.ImgManager.GetMatchingResult(index: (int)ai_model_01_01K.DetectorIndices.ai_model_01);
        //                //int move = ai_model_01_01K.ImgManager.GetAddMatching();
        //                int move = ai_model_01_matching_result;

        //                Mat print = mat_list[mat_index % 5].Mul(one, 0.92);
        //                Cv2.Resize(print[roi_rect], temp_mat_1, new OpenCvSharp.Size(1920, 996), 0, 0, InterpolationFlags.Linear);
        //                //Cv2.Resize(mat_list[mat_index % 5][roi_rect], temp_mat_1, new OpenCvSharp.Size(1920, 996), 0, 0, InterpolationFlags.Linear);

        //                Stopwatch a = new Stopwatch();
        //                ai_model_01_result = ai_model_01_manager.AI_detect_objects(temp_mat_1.ToBytes());
        //                Console.WriteLine("result : " + ai_model_01_result);

        //                DateTime end = DateTime.Now;

        //                detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_01] = true;
        //                box_drawing_check = 1;
        //                ai_check = 1;
        //                dis_ai_check = false;
        //            }
        //            Thread.Sleep(detection_interval);
        //        }
        //        catch (OpenCvSharpException f)
        //        {
        //            Console.WriteLine(f.ToString());
        //        }
        //        catch (Exception f)
        //        {
        //            Console.WriteLine(f.ToString());
        //        }
        //    }
        //}

        public IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items, int maxItems)
        {
            return items.Select((item, index) => new { item, index })
                        .GroupBy(x => x.index / maxItems)
                        .Select(g => g.Select(x => x.item));
        }

        private void XInspector_Deactivated(object sender, EventArgs e)
        {
            System.Windows.Window window = (System.Windows.Window)sender;
            window.Topmost = true;
        }

        private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine("size change enter");
            alarm_label_change = true;
            //video_show_image.Visibility = System.Windows.Visibility.Visible;
            this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        private void Rectangle_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (alarm_label_change == true)
            {
                double height = e.GetPosition(big_grid).Y;
                Console.WriteLine("height : " + height);
                RowDefinition row_temp = new RowDefinition();
                row_temp.Height = new GridLength(1080 - Convert.ToInt32(height), GridUnitType.Pixel);
                big_grid.RowDefinitions[2] = row_temp;
                RowDefinition row_temp_2 = new RowDefinition();
                row_temp_2.Height = new GridLength(912 - 1080 + Convert.ToInt32(height), GridUnitType.Pixel);
                big_grid.RowDefinitions[0] = row_temp_2;
            }
        }

        private void Rectangle_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.WriteLine("size change leave");
            alarm_label_change = false;
            //video_show_image.Visibility = System.Windows.Visibility.Hidden;
            this.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        }

        List<YoloItem> do_nms_sort(List<YoloItem> dets, int total, int classes, double[] thresh)
        {
            List<YoloItem> result_dets = new List<YoloItem>();
            result_dets.AddRange(dets);


            int i, j, k;
            k = total - 1;

            for (i = 0; i <= k; ++i)
            {
                if (dets[i].Confidence == 0)
                {
                    YoloItem swap = dets[i];
                    dets[i] = dets[k];
                    dets[k] = swap;
                    --k;
                    --i;
                }
            }
            total = k + 1;
            int check = 0;
            for (i = 0; i < total; ++i)
            {
                if (dets[i].Confidence == 0) continue;
                box a = new box();
                a.x = dets[i].X;
                a.y = dets[i].Y;
                a.w = dets[i].Width;
                a.h = dets[i].Height;
                for (j = i + 1; j < total; ++j)
                {
                    if (dets[i].Type == dets[j].Type)
                    {
                        check = 1;
                        box b = new box();
                        b.x = dets[j].X;
                        b.y = dets[j].Y;
                        b.w = dets[j].Width;
                        b.h = dets[j].Height;
                        if (box_iou(a, b) > thresh[ai_model_01_AIset.result_converter[Convert.ToInt32(dets[i].Type)]])
                        {
                            dets[j].Confidence = 0;
                        }
                    }
                }
            }
            return result_dets;
        }

        float box_iou(box a, box b)
        {
            float I = box_intersection(a, b);
            float U = box_union(a, b);
            if (I == 0 || U == 0)
            {
                return 0;
            }
            return I / U;
        }

        float box_intersection(box a, box b)
        {
            float w = overlap(a.x, a.w, b.x, b.w);
            float h = overlap(a.y, a.h, b.y, b.h);
            if (w < 0 || h < 0) return 0;
            float area = w * h;
            return area;
        }

        float overlap(float x1, float w1, float x2, float w2)
        {
            float l1 = x1 - w1 / 2;
            float l2 = x2 - w2 / 2;
            float left = l1 > l2 ? l1 : l2;
            float r1 = x1 + w1 / 2;
            float r2 = x2 + w2 / 2;
            float right = r1 < r2 ? r1 : r2;
            return right - left;
        }

        float box_union(box a, box b)
        {
            float i = box_intersection(a, b);
            float u = a.w * a.h + b.w * b.h - i;
            return u;
        }
        //private void backgroundai_model_01AIResult(object sender, DoWorkEventArgs e)
        //{
        //    temp_mat_1 = new Mat();
        //    while (true)
        //    {
        //        if (mat_list[mat_index % 5].Empty() == false && (box_drawing_check == 0 || detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_01] == true))
        //        {
        //            ideal_width = 0;
        //            ideal_height = 0;
        //            Cv2.Resize(mat_list[mat_index % 5][roi_rect], temp_mat_1, new OpenCvSharp.Size(1920, 996), 0, 0, InterpolationFlags.Linear);
        //            ai_model_01_01K.ImgManager.UpdateMatchingResult(index: (int)ai_model_01_01K.DetectorIndices.ai_model_01);
        //            //Cv2.ImShow("ori", mat_list[mat_index % 5][roi_rect]);
        //            //Cv2.ImShow("resize", temp_mat_1);
        //            ai_model_01_result = ai_model_01_manager.AI_detect_objects(temp_mat_1.ToBytes());
        //            ai_model_01_result = ai_model_01_result.Where((j) => !(j.Type == "6" || j.Type == "7"));
        //            detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_01] = true;
        //            box_drawing_check = 1;
        //            ai_check = 1;
        //            //Cv2.WaitKey(1);
        //        }
        //        Thread.Sleep(detection_interval);
        //    }
        //}

        private void backgroundai_model_02AIResult(object sender, DoWorkEventArgs e)
        {
            //temp_mat_2 = new Mat();
            //while (true)
            //{
            //    if (mat_list[mat_index % 5].Empty() == false && (box_drawing_check == 0 || detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_01] == true))
            //    {
            //        ideal_width = 0;
            //        ideal_height = 0;
            //        Cv2.Resize(mat_list[mat_index % 5][roi_rect], temp_mat_2, new OpenCvSharp.Size(resize_width, resize_height), 0, 0, InterpolationFlags.Linear);
            //        ai_model_01_01K.ImgManager.UpdateMatchingResult(index: (int)ai_model_01_01K.DetectorIndices.ai_model_02);
            //        ai_model_02_result = ai_model_02_manager.AI_detect_objects(temp_mat_2.ToBytes());
            //        ai_model_02_result = ai_model_02_result.Where(a => ((Convert.ToInt32(a.Type) >= 0 && (Convert.ToInt32(a.Type) < 14))));

            //        ai_model_02_result = ai_model_02_result.Select(element =>
            //        {
            //            DetectItem temp = element;
            //            if (temp.Type == "9")
            //                temp.Type = "15";
            //            return temp;
            //        });


            //        ai_model_02_result = ai_model_02_result.Select(element =>
            //        {
            //            DetectItem temp = element;
            //            if (temp.Type == "10")
            //                temp.Type = "16";
            //            return temp;
            //        });

            //        ai_model_02_result = ai_model_02_result.Select(element =>
            //        {
            //            DetectItem temp = element;
            //            if (temp.Type == "6")
            //                temp.Type = "9";
            //            return temp;
            //        });
            //        ai_model_02_result = ai_model_02_result.Select(element =>
            //        {
            //            DetectItem temp = element;
            //            if (temp.Type == "7")
            //                temp.Type = "10";
            //            return temp;
            //        });
            //        ai_model_02_result = ai_model_02_result.Select(element =>
            //        {
            //            DetectItem temp = element;
            //            if (temp.Type == "8")
            //                temp.Type = "11";
            //            return temp;
            //        });

            //        var result = ai_model_02_manager_2.AI_detect_objects(temp_mat_2.ToBytes());
            //        result = result.Where(a => ((Convert.ToInt32(a.Type) < 15 && Convert.ToInt32(a.Type) >= 12) || Convert.ToInt32(a.Type) < 9 && Convert.ToInt32(a.Type) >= 6));

            //        ai_model_02_result = ai_model_02_result.Concat(result);
            //        detection_flag[(int)ai_model_01_01K.DetectorIndices.ai_model_02] = true;
            //        box_drawing_check = 1;
            //        ai_check = 1;
            //    }
            //    Thread.Sleep(detection_interval);
            //}
        }

        private void backgroundDBInput(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (db_save_check == 1)
                {
                    AI_Vid_Demo.SqlManager.MSQL_INSERT_NEW(DateTime.Now.ToString("yyyyMM") + "info", info_insert, input_pk);
                    AI_Vid_Demo.SqlManager.MSQL_INSERT_RESULT(DateTime.Now.ToString("yyyyMM") + "detect", db_input_list.ToArray());

                    db_save_check = 2;

                    pk_index++;
                    input_pk = null;
                    db_input_list.Clear();
                    db_save_check = -1;
                }
                Thread.Sleep(10);
            }
        }

        private unsafe void init_alarm()
        {
            //for (int i = 0; i < 6; i++)
            //    alarm[i] = 0;
        }

        private unsafe void init_alarm_ai_model_01()
        {
            //for (int i = 0; i < ai_model_01_AIset.number_of_class; i++)
            //    alarm[i] = 0;
        }

        private unsafe void init_alarm_ai_model_02()
        {
            //for (int i = ai_model_01_AIset.number_of_class; i < ai_model_01_AIset.number_of_class + ai_model_02_AIset.number_of_class; i++)
            //    if (i < 5)
            //        alarm[i] = 0;
        }

        private unsafe void add_alarm(int i)
        {
            //if (i < 5)
            //{
            //    alarm[i] = 1;
            //}
        }

        private void Window_ContentRendered(object sender, System.EventArgs e)
        {
            alarm_margin_left = Convert.ToInt32(full_label_list[0].ActualWidth);

            grid_sidebar.Width = grid_sidebar.ActualHeight * 4;

            if (System.Windows.SystemParameters.PrimaryScreenWidth < 1600)
            {
                for (int i = 0; i < full_label_list.Count; i++)
                    full_label_list[i].FontSize = 25;
            }

            for (int i = 0; i < 30; i++)
            {
                System.Windows.Shapes.Rectangle temp_ai_model_01 = new System.Windows.Shapes.Rectangle();
                System.Windows.Shapes.Rectangle temp_ai_model_02 = new System.Windows.Shapes.Rectangle();

                temp_ai_model_01.Visibility = System.Windows.Visibility.Hidden;
                temp_ai_model_02.Visibility = System.Windows.Visibility.Hidden;

                temp_ai_model_01.Fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
                temp_ai_model_02.Fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

                temp_ai_model_01.Width = 1;
                temp_ai_model_01.Height = 1;

                temp_ai_model_02.Width = 1;
                temp_ai_model_02.Height = 1;

                Panel.SetZIndex(temp_ai_model_01, 10);
                Panel.SetZIndex(temp_ai_model_02, 10);

                canvas_ai_model_01.Children.Add(temp_ai_model_01);
                canvas_ai_model_02.Children.Add(temp_ai_model_02);

                ai_model_01_rect_list.Add(temp_ai_model_01);
                ai_model_02_rect_list.Add(temp_ai_model_02);
            }

            button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        }

        private void XInspector_DpiChanged(object sender, DpiChangedEventArgs e)
        {

        }

        private void big_grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            alarm_margin_left = Convert.ToInt32(((Label)label_grid.Children[0]).ActualWidth);
            grid_sidebar.Width = grid_sidebar.ActualHeight * 4;
        }
    }
}
