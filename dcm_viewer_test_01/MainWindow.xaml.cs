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
using Dicom;
using Dicom.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows.Media.Animation;

namespace dcm_viewer_test_01
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        string base_path = @"C:\DVIEWER\dview\data"; //FolderBrowserDialog 기본 최상위 경로
        string ori_path = ""; // FolderBrowserDialog 결과, Dicom file 탐색을 위한 StudyID 기반 경로
        string[] dir_list = new string[0]; // ori_path 내의 결과 folder 존재 여부 확인
        string[] file_list = new string[0]; // ori_path 내의 Dicom 폴더 속 *.dcm 파일들의 full path 저장
        bool mouse_right_button_down = false; // 우클릭 체크
        bool mouse_left_button_down = false; // 좌클릭 체크
        double load_ww = 0.0; // 이미지 로딩 시 기본 WindowWidth 값 저장
        double load_wl = 0.0; // 이미지 로딩 시 기본 WindowCenter 값 저장
        double load_width = 0.0; // 이미지 로딩 시 기본 Width 값 저장
        double load_height = 0.0; // 이미지 로딩 시 기본 Height 값 저장
        double load_pixel_space = 0.0; // 이미지 로딩 시 기본 pixel space 값 저장
        double last_x = -1.0; // 마우스 move event의 직전 좌표 X값 저장
        double last_y = -1.0; // 마우스 move event의 직전 좌표 Y값 저장
        double scroll_center = 0.0; // 마우스 move event의 직전 좌표 Y값 저장
        int max_col = 2; // UI 최대 column 수
        int max_row = 2; // UI 최대 row 수 
        int selected_col = 0; // UI Viewer의 선택된 grid column 값 
        int selected_row = 0; // UI Viewer의 선택된 grid row 값
        bool[] inv_index = new bool[0];
        int[] rotate_index = new int[0];
        int[] flip_index = new int[0];
        int[][] flip_flag;
        bool select_mode = false;
        bool label_on_off = true;
        int angle_step = 0;
        int dist_step = 0;
        double pin_zoom_size = 300.0;
        Border drawing_border;
        Grid drawing_grid;
        System.Windows.Controls.Image drawing_zoom;
        Line[] drawing_line;
        Label[] drawing_label;
        int drawing_count = 0;
        DicomImage[] dcm_img;
        System.Windows.Controls.Image[] viewer_image_list;
        Vector[] calc_angle = new Vector[2];
        Label[] viewer_label_list;
        List<int> overed_line_list = new List<int>();
        List<Border> clicked_reset_list = new List<Border>();
        List<Tuple<string, bool>> clicked_reset_bool_list_str = new List<Tuple<string, bool>>();
        Dictionary<string, bool> reset_list_to_dict = new Dictionary<string, bool>();
        Tuple<double, double>[] viewer_margin_list = new Tuple<double, double>[0];
        List<Tuple<double, double>> angle_line_list = new List<Tuple<double, double>>();
        List<Tuple<double, double>> dist_line_list = new List<Tuple<double, double>>();
        RotateFlipType[,] rotate_flip_list = {
            { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipX, RotateFlipType.RotateNoneFlipY, RotateFlipType.RotateNoneFlipXY },
            { RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate90FlipX, RotateFlipType.Rotate90FlipY, RotateFlipType.Rotate90FlipXY },
            { RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate180FlipY, RotateFlipType.Rotate180FlipXY},
            { RotateFlipType.Rotate270FlipNone, RotateFlipType.Rotate270FlipX, RotateFlipType.Rotate270FlipY, RotateFlipType.Rotate270FlipXY}
        };

        public MainWindow()
        {
            InitializeComponent();

            clicked_reset_list.Add(btn_06);
            clicked_reset_list.Add(btn_07);
            clicked_reset_list.Add(btn_08);
            clicked_reset_list.Add(btn_13);
            clicked_reset_list.Add(btn_15);
            clicked_reset_list.Add(btn_16);
            clicked_reset_list.Add(btn_18);


            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("pan_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("pin_zoom_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("zoom_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("wwwl_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("angle_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("dist_mode", false));
            clicked_reset_bool_list_str.Add(new Tuple<string, bool>("erase_mode", false));
            reset_list_to_dict = clicked_reset_bool_list_str.ToDictionary(l => l.Item1, l => l.Item2);
        }

        //function #001
        private void show_img(int row, int col)
        {
            /*
             * Description : Show viewer fit in Grid Layout
             * Input parameter : row(Row count), col(Column count)
             * Output : void
             * 추가정보 : Dicom Viewer 구성
                    # Border - {Grid 1 - {Grid 2 - {ScrollViewer - {Image}}}, Label_Grid, Drawing_Grid}
                        > Border - 테두리
                        > Grid 1 - 이미지 표시 제한 영역
                        > Grid 2 - 실제 이미지 보관 영역
                        > ScrollViewer - Zoom 기능 구현 영역
                        > Image - 이미지 뷰어 영역
                        > Label_Grid - 정보 시각화 영역
                        > Drawing_Grid - 각도, 길이 시각화 영역
            */
            int now_index = row * max_col + col; // 선택한 grid index 계산

            //----Get Dicom Info----//
            gdcm.ImageReader reader = new gdcm.ImageReader();
            reader.SetFileName(file_list[0]);
            var get_image = reader.GetImage();
            var file = DicomFile.Open(file_list[0]);
            var pixel_space = file.Dataset.Get<string>(DicomTag.ImagerPixelSpacing);

            dcm_img[now_index] = new DicomImage(file_list[0]); // Dicom image load
            load_ww = dcm_img[now_index].WindowWidth; // Get default WW
            load_wl = dcm_img[now_index].WindowCenter; // Get default WL
            load_width = dcm_img[now_index].Width; // Get default WL
            load_height = dcm_img[now_index].Height; // Get default WL
            load_pixel_space = double.Parse(pixel_space); // Get default Pixel Spacing
            //////////////////////////

            //----Viewer 최상위 Border----//
            Border input_border = new Border();
            input_border.BorderThickness = new Thickness(1);
            input_border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
            input_border.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#20a5d6"));
            if (col == selected_col && row == selected_row)
                input_border.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f7dd44"));
            input_border.MouseLeftButtonDown += dcm_viewer_MouseLeftButtonDown;
            input_border.MouseRightButtonDown += dcm_viewer_MouseRightButtonDown;
            input_border.SetValue(Grid.ColumnProperty, col); // Set column in grid 
            input_border.SetValue(Grid.RowProperty, row); // Set row in grid
            ////////////////////////////////

            //----Image Packing Grid 1----//
            Grid take_all_grid = new Grid(); // 표시 제한 영역
            take_all_grid.ClipToBounds = true;
            ////////////////////////////////
            ///
            //----Image Packing Grid 2----//
            Grid grid_take_image = new Grid(); // Viewer image를 담고있을 grid
            grid_take_image.Margin = new Thickness(3); // 테두리 침범 제한
            grid_take_image.ClipToBounds = true;
            grid_take_image.Loaded += Grid_take_image_Loaded;
            ////////////////////////////////


            //----Viewer----//
            System.Windows.Controls.Image input_image = new System.Windows.Controls.Image(); // Dicom image를 render 할 image
            input_image.HorizontalAlignment = HorizontalAlignment.Center;
            viewer_margin_list[now_index] = new Tuple<double, double>(0, 0);
            TransformGroup myTransformGroup = new TransformGroup(); // Image 이동, 회전, 대칭 등을 지원하는 transformgroup
            TranslateTransform myTranslate = new TranslateTransform(); // 상하좌우 moving
            myTranslate.X = viewer_margin_list[now_index].Item1;
            myTranslate.Y = viewer_margin_list[now_index].Item2;
            myTransformGroup.Children.Add(myTranslate);
            input_image.RenderTransform = myTransformGroup;
            viewer_image_list[now_index] = input_image;
            //////////////////

            //----Viewer Zoom----//
            ScrollViewer scroll_for_image = new ScrollViewer();
            scroll_for_image.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll_for_image.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll_for_image.HorizontalAlignment = HorizontalAlignment.Center;
            scroll_for_image.Content = input_image;
            scroll_for_image.IsEnabled = false;
            scroll_for_image.Background = null;
            TransformGroup scroll_tfGroup = new TransformGroup();
            ScaleTransform scroll_scale = new ScaleTransform();
            scroll_scale.CenterX = 0.5;
            scroll_scale.CenterY = 0.5;
            scroll_tfGroup.Children.Add(scroll_scale);
            scroll_for_image.RenderTransform = scroll_tfGroup;
            ///////////////////////

            //----Component Add----//
            grid_take_image.Children.Add(scroll_for_image); // grid 2에 image 추가
            take_all_grid.Children.Add(grid_take_image); // grid 1에 grid 2 추가
            input_border.Child = take_all_grid; // border에 grid 1 추가
            grid_viewer.Children.Add(input_border); // grid_viewer에 viewer(border) 추가
            /////////////////////////

            //----Get & Show Image----//
            var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();
            img_for_show.RotateFlip(rotate_flip_list[rotate_index[now_index], flip_index[now_index]]); // 회전 및 대칭 설정
            input_image.Source = modules.modules.Convert(img_for_show); // Bitmap을 WPF Image의 Source를 지원하는 형식으로 변형
            ////////////////////////////

            //----Show WWWL Info----//
            Label input_label = new Label(); // WW, WL 정보 송출용 Label
            input_label.FontSize = 20;
            input_label.Content = "WW : " + Math.Round(dcm_img[now_index].WindowWidth).ToString() + ", WL : " + Math.Round(dcm_img[now_index].WindowCenter).ToString();
            input_label.SetValue(Grid.ColumnProperty, col);
            input_label.SetValue(Grid.RowProperty, row);
            input_label.HorizontalContentAlignment = HorizontalAlignment.Left;
            input_label.VerticalAlignment = VerticalAlignment.Bottom;
            input_label.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9ccef9"));
            viewer_label_list[now_index] = input_label;
            //////////////////////////

            //----Info Label Grid----//
            Grid label_grid = new Grid();
            label_grid.SetValue(Grid.ColumnProperty, col); // Set column in grid 
            label_grid.SetValue(Grid.RowProperty, row); // Set row in grid
            label_grid.VerticalAlignment = VerticalAlignment.Stretch;
            label_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            label_grid.Children.Add(input_label); // label_grid에 input_label 추가
            grid_viewer.Children.Add(label_grid); // grid_viewer에 label_grid 추가
            ///////////////////////////

            //----Angle & Dist Draw Grid----//
            Grid drawing_grid = new Grid();
            drawing_grid.SetValue(Grid.ColumnProperty, col); // Set column in grid 
            drawing_grid.SetValue(Grid.RowProperty, row); // Set row in grid
            drawing_grid.VerticalAlignment = VerticalAlignment.Stretch;
            drawing_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid_viewer.Children.Add(drawing_grid); // grid_viewer에 drawing_grid 추가
            //////////////////////////////////
        }

        //function #002
        private void make_grid(int col, int row)
        {
            /*
             * Description : Make layout to Base viewer grid
             * Input parameter : col(Column count), row(Row count)
             * Output : void
             * FYI : https://insurang.tistory.com/240 ColumnDefinition, RowDefinition의 의미 및 역할
            */
            side_btn_off();

            //----initlize----//
            grid_viewer.ColumnDefinitions.Clear();
            grid_viewer.RowDefinitions.Clear();
            grid_viewer.Children.Clear();
            selected_col = 0;
            selected_row = 0;
            dcm_img = new DicomImage[col * row];
            max_col = col;
            max_row = row;
            viewer_image_list = new System.Windows.Controls.Image[col * row];
            viewer_label_list = new Label[col * row];
            viewer_margin_list = new Tuple<double, double>[col * row];
            inv_index = new bool[col * row];
            rotate_index = new int[col * row];
            flip_index = new int[col * row];
            flip_flag = new int[col * row][];
            drawing_line = new Line[col * row];
            drawing_label = new Label[col * row];
            for (int i = 0; i < col * row; i++)
            {
                flip_flag[i] = new int[2];
            }
            ////////////////////

            //----Column,Row Definition Add----//
            for (int i = 0; i < col; i++)
            {
                grid_viewer.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < row; i++)
            {
                grid_viewer.RowDefinitions.Add(new RowDefinition());
            }
            /////////////////////////////////////

            //----call show img----//
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    show_img(i, j);
                }
            }
            /////////////////////////
        }

        //function #003
        private void big_window_Loaded(object sender, RoutedEventArgs e)
        {
            /*
             * Description : Using value (fixed after render or load) 
             * Input parameter : default event parameter
             * Output : void
            */
            get_list(); // get file list
            make_grid(max_col, max_row); // make grid

            //----side btn 갯수에 맞춘 전체 viewer 높이 조절----//
            //big_grid.Height = grid_btn.ActualWidth * ((double)grid_btn.RowDefinitions.Count() / 2.0) * 12.0 / 11.0;
            grid_select.Height = grid_btn.ActualWidth * ((double)grid_btn.RowDefinitions.Count() / 2.0) * 11.1 / 11.0;
            grid_viewer.Height = grid_btn.ActualWidth * ((double)grid_btn.RowDefinitions.Count() / 2.0) * 11.1 / 11.0;
            grid_sidebar.Height = grid_btn.ActualWidth * ((double)grid_btn.RowDefinitions.Count() / 2.0) * 11.1 / 11.0;
            grid_btn.Height = grid_btn.ActualWidth * ((double)grid_btn.RowDefinitions.Count() / 2.0);
            btn_21.Height = btn_21.ActualWidth;
            btn_22.Height = btn_22.ActualWidth;
            //////////////////////////////////////////////////////

            //----side btn add event----//
            for (int i = 0; i < grid_btn.Children.Count; i++)
            {
                if (grid_btn.Children[i] is Border)
                {
                    var btn_border = grid_btn.Children[i] as Border;
                    btn_border.MouseEnter += side_btn_mouse_enter;
                    btn_border.MouseLeave += side_btn_mouse_leave;
                    btn_border.MouseLeftButtonDown += side_btn_click;
                }
            }
            //////////////////////////////

            Dicom_side.Dicom_sidebar draw_sidebar = new Dicom_side.Dicom_sidebar(ori_path);
            grid_select.Children.Add(draw_sidebar);
        }

        //function #004
        private void get_list()
        {
            /*
             * Description : Get path of dcm files 
             * Input parameter : null
             * Output : void
            */
            try
            {
                //----get path where to find----//
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.SelectedPath = base_path;
                    dialog.ShowDialog();
                    ori_path = dialog.SelectedPath.ToString();
                }
                dir_list = Directory.GetDirectories(ori_path);
                //////////////////////////////////

                //----get path of dicom files----//
                if (dir_list.Length > 0)
                {
                    file_list = Directory.GetFiles(dir_list[0] + @"\dicom", "*.dcm");
                }
                ///////////////////////////////////
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                big_window.Close();
            }

            //if file not exists 
            if (file_list.Length == 0)
                big_window.Close();
        }

        //function #005
        private void reset_image(int col, int row)
        {
            /*
             * Description : Reset image post processing 
             * Input parameter : int col, int row
             * Output : void
            */
            int now_index = row * max_col + col; // calc index of selected viewer

            //----get component of selected viewer----//
            var selected_border = grid_viewer.Children[now_index * 3] as Border;
            var selected_grid = selected_border.Child as Grid;
            var selected_image = viewer_image_list[now_index] as System.Windows.Controls.Image;
            ////////////////////////////////////////////

            //----get default WindowWidth, WindowCenter and show to label----//
            dcm_img[now_index].WindowWidth = load_ww;
            dcm_img[now_index].WindowCenter = load_wl;
            viewer_label_list[now_index].Content = "WW : " + Math.Round(load_ww).ToString() + ", WL : " + Math.Round(load_wl).ToString();
            ///////////////////////////////////////////////////////////////////


            //----get dicom image and show via wpf image component----//
            var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();
            var selected_transformGroup = viewer_image_list[now_index].RenderTransform as TransformGroup;
            var selected_translate = selected_transformGroup.Children[0] as TranslateTransform;
            selected_translate.X = 0;
            selected_translate.Y = 0;
            viewer_margin_list[now_index] = new Tuple<double, double>(0, 0);
            selected_image.Source = modules.modules.Convert(img_for_show);
            ////////////////////////////////////////////////////////////

            //----set default flip, rotate variable----//
            flip_flag[now_index][0] = 0;
            flip_flag[now_index][1] = 0;
            flip_index[now_index] = 0;
            rotate_index[now_index] = 0;
            /////////////////////////////////////////////

            //----set default scroll scale----//
            ScrollViewer now_scroll_viewer = selected_image.Parent as ScrollViewer;
            TransformGroup scroll_tfGroup = now_scroll_viewer.RenderTransform as TransformGroup;
            ScaleTransform scroll_scale = scroll_tfGroup.Children[0] as ScaleTransform;

            scroll_scale.ScaleX = 1;
            scroll_scale.ScaleY = 1;
            scroll_scale.CenterX = now_scroll_viewer.ActualWidth / 2.0;
            scroll_scale.CenterY = now_scroll_viewer.ActualHeight / 2.0;
            ////////////////////////////////////

            side_btn_update(); // set side btn on to off
        }

        //function #006
        private void side_btn_update()
        {
            /*
             * Description : Set side btn on/off by selected viewer's option
             * Input parameter : null
             * Output : void
            */
            int now_index = selected_row * max_col + selected_col; // calc index of selected viewer

            //----set on/off by options----//
            var side_btn_change = btn_11.Child as System.Windows.Controls.Image;
            BitmapImage bt = new BitmapImage();
            bt.BeginInit();
            bt.UriSource = flip_flag[now_index][0] == 0 ? new Uri(side_btn_change.Source.ToString().Replace("_02.png", "_01.png")) : new Uri(side_btn_change.Source.ToString().Replace("_01.png", "_02.png"));
            bt.EndInit();
            side_btn_change.Source = bt;

            side_btn_change = btn_12.Child as System.Windows.Controls.Image;
            bt = new BitmapImage();
            bt.BeginInit();
            bt.UriSource = flip_flag[now_index][1] == 0 ? new Uri(side_btn_change.Source.ToString().Replace("_02.png", "_01.png")) : new Uri(side_btn_change.Source.ToString().Replace("_01.png", "_02.png"));
            bt.EndInit();
            side_btn_change.Source = bt;

            side_btn_change = btn_14.Child as System.Windows.Controls.Image;
            bt = new BitmapImage();
            bt.BeginInit();
            bt.UriSource = inv_index[now_index] == false ? new Uri(side_btn_change.Source.ToString().Replace("_02.png", "_01.png")) : new Uri(side_btn_change.Source.ToString().Replace("_01.png", "_02.png"));
            bt.EndInit();
            side_btn_change.Source = bt;
            /////////////////////////////////
        }

        //function #007
        private bool side_btn_off(string input)
        {
            /*
             * Description : Set side btn off when change tools that cannot be used simultaneously without now using
             * Input parameter : bool input
             * Output : void
            */
            bool return_bool = false;
            //----turn off btn without now using----//
            for (int i = 0; i < clicked_reset_bool_list_str.Count; i++)
            {
                if (clicked_reset_bool_list_str[i].Item1 != input)
                {
                    clicked_reset_bool_list_str[i] = new Tuple<string, bool>(clicked_reset_bool_list_str[i].Item1, false);
                    System.Windows.Controls.Image clicked_img = clicked_reset_list[i].Child as System.Windows.Controls.Image;

                    BitmapImage bt;
                    bt = new BitmapImage();
                    bt.BeginInit();
                    bt.UriSource = new Uri(clicked_img.Source.ToString().Replace("_02.png", "_01.png"));
                    bt.EndInit();
                    clicked_img.Source = bt;
                }
                else
                {
                    return_bool = !clicked_reset_bool_list_str[i].Item2;
                    clicked_reset_bool_list_str[i] = new Tuple<string, bool>(clicked_reset_bool_list_str[i].Item1, !clicked_reset_bool_list_str[i].Item2);
                    System.Windows.Controls.Image clicked_img = clicked_reset_list[i].Child as System.Windows.Controls.Image;

                    BitmapImage bt;
                    bt = new BitmapImage();
                    bt.BeginInit();
                    bt.UriSource = return_bool ? new Uri(clicked_img.Source.ToString().Replace("_01.png", "_02.png")) : new Uri(clicked_img.Source.ToString().Replace("_02.png", "_01.png"));
                    bt.EndInit();
                    clicked_img.Source = bt;
                }
            }
            reset_list_to_dict = clicked_reset_bool_list_str.ToDictionary(l => l.Item1, l => l.Item2);
            return return_bool;
            //////////////////////////////////////////
        }

        //function #007
        private void side_btn_off()
        {
            /*
             * Description : Set side btn off tools that cannot be used simultaneously
             * Input parameter : bool input
             * Output : void
            */

            //----turn off every btn----//
            for (int i = 0; i < clicked_reset_bool_list_str.Count; i++)
            {
                clicked_reset_bool_list_str[i] = new Tuple<string, bool>(clicked_reset_bool_list_str[i].Item1, false);
                System.Windows.Controls.Image clicked_img = clicked_reset_list[i].Child as System.Windows.Controls.Image;

                BitmapImage bt;
                bt = new BitmapImage();
                bt.BeginInit();
                bt.UriSource = new Uri(clicked_img.Source.ToString().Replace("_02.png", "_01.png"));
                bt.EndInit();
                clicked_img.Source = bt;
            }
            reset_list_to_dict = clicked_reset_bool_list_str.ToDictionary(l => l.Item1, l => l.Item2);
            ////////////////////////
        }

        //function #008
        private void Grid_take_image_Loaded(object sender, RoutedEventArgs e)
        {
            /*
             * Description : When grid load set grid size to fit with image
             * Input parameter : default event parameter
             * Output : void
            */
            var loaded = sender as Grid;
            var par_grid = loaded.Parent as Grid;
            var child_scroll = loaded.Children[0] as ScrollViewer;
            var child_img = child_scroll.Content as System.Windows.Controls.Image;
            par_grid.Width = child_img.ActualWidth;
            par_grid.Height = child_img.ActualHeight;
        }

        //function #009
        private void side_btn_click(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : Click side btn event
             * Input parameter : default event parameter
             * Output : void
            */

            //----Get Clicked Button and Get infos----//
            var clicked = sender as Border;
            int now_index = selected_row * max_col + selected_col;
            ////////////////////////////////////////////

            //----Initialize----//
            System.Windows.Controls.Image overed_img;
            BitmapImage bt;
            var selected_image = viewer_image_list[now_index];
            var selected_image_source = dcm_img[now_index].RenderImage().As<Bitmap>();
            bool result;
            Grid drawing_grid;
            TransformGroup temp_tfGroup;
            RotateTransform temp_rotate;

            last_x = -1;
            last_y = -1;
            angle_step = 0;
            dist_step = 0;
            angle_step = 0;
            dist_step = 0;
            //////////////////////

            switch (clicked.Name) // check what button clicked
            {
                case "btn_01": // 1*1
                    make_grid(1, 1);
                    break;
                case "btn_02": // 1*2
                    make_grid(2, 1);
                    break;
                case "btn_03": // 2*1
                    make_grid(1, 2);
                    break;
                case "btn_04": // 2*2
                    make_grid(2, 2);
                    break;
                case "btn_05": // reset image
                    reset_image(selected_col, selected_row);
                    break;
                case "btn_06": // pan mode btn
                    result = side_btn_off("pan_mode");
                    break;
                case "btn_07": // pin zoom mode btn
                    result = side_btn_off("pin_zoom_mode");
                    reset_image(selected_col, selected_row);
                    //(((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).Parent as Grid).ClipToBounds = !result;
                    break;
                case "btn_08": // drag zoom mode btn
                    result = side_btn_off("zoom_mode");
                    break;
                case "btn_09": // rotate anticlockwise btn
                    rotate_index[now_index] = (rotate_index[now_index] + 3) % 4;
                    rotate_flip_viewer(now_index);
                    break;
                case "btn_10": // rotate clockwise btn
                    rotate_index[now_index] = (rotate_index[now_index] + 1) % 4;
                    rotate_flip_viewer(now_index);
                    break;
                case "btn_11": // horizontal symmetry btn
                    flip_flag[now_index][0] = (flip_flag[now_index][0] + 1) % 2;
                    rotate_flip_viewer(now_index);
                    break;
                case "btn_12": // vertical symmetry btn
                    flip_flag[now_index][1] = (flip_flag[now_index][1] + 1) % 2;
                    rotate_flip_viewer(now_index);
                    break;
                case "btn_13": // Window width & Window center mode left click activate btn
                    result = side_btn_off("wwwl_mode");
                    break;
                case "btn_14": // invert image btn
                    inv_index[now_index] = !inv_index[now_index];
                    dcm_img[now_index] = inv_index[now_index] == true ? new DicomImage(file_list[1]) : new DicomImage(file_list[0]); // index 0 : normal image, index 1 : invert image
                    var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();
                    viewer_image_list[now_index].Source = modules.modules.Convert(img_for_show);
                    viewer_label_list[now_index].Content = "WW : " + Math.Round(dcm_img[now_index].WindowWidth).ToString() + ", WL : " + Math.Round(dcm_img[now_index].WindowCenter).ToString(); // reset Window width & Window center overlay text

                    overed_img = clicked.Child as System.Windows.Controls.Image;

                    //----switch btn light----//
                    bt = new BitmapImage();
                    bt.BeginInit();
                    if (inv_index[now_index] == false)
                        bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_02.png", "_01.png"));
                    else if (inv_index[now_index] == true)
                        bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_01.png", "_02.png"));
                    bt.EndInit();
                    overed_img.Source = bt;
                    ////////////////////////////
                    break;
                case "btn_15": // angle calc mode btn
                    result = side_btn_off("angle_mode");
                    break;
                case "btn_16": // dist calc mode btn
                    result = side_btn_off("dist_mode");
                    break;
                case "btn_17": // dicom info overlay on/off btn
                    label_on_off = !label_on_off;

                    //----on/off label grid visibility----//
                    if (label_on_off == true)
                    {
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            var label_grid = grid_viewer.Children[i * 3 + 1];
                            label_grid.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            var label_grid = grid_viewer.Children[i * 3 + 1];
                            label_grid.Visibility = Visibility.Hidden;
                        }
                    }
                    ////////////////////////////////////////

                    //----switch btn light----//
                    overed_img = clicked.Child as System.Windows.Controls.Image;
                    bt = new BitmapImage();
                    bt.BeginInit();
                    if (label_on_off == false)
                        bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_02.png", "_01.png"));
                    else if (label_on_off == true)
                        bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_01.png", "_02.png"));
                    bt.EndInit();
                    overed_img.Source = bt;
                    ////////////////////////////
                    break;
                case "btn_18": // angle & calc overlay remove mode btn
                    result = side_btn_off("erase_mode");

                    //----turn on/off mouse interaction----//
                    if (result == true)
                    {
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            var search_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            for (int j = 0; j < search_grid.Children.Count; j++)
                            {
                                if (search_grid.Children[j] is Label || search_grid.Children[j] is Ellipse || search_grid.Children[j] is Line)
                                    (search_grid.Children[j] as FrameworkElement).IsHitTestVisible = true;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            var search_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            for (int j = 0; j < search_grid.Children.Count; j++)
                            {
                                if (search_grid.Children[j] is Label || search_grid.Children[j] is Ellipse || search_grid.Children[j] is Line)
                                    (search_grid.Children[j] as FrameworkElement).IsHitTestVisible = false;
                            }
                        }
                    }
                    /////////////////////////////////////////
                    break;
                case "btn_19": // heatmap index prev btn

                    break;
                case "btn_20": // heatmap index next btn 

                    break;
                case "btn_21": // pacs send btn

                    break;
                case "btn_22": // close btn
                    this.Close();
                    break;
            }
        }

        //function #010
        private void side_btn_mouse_enter(object sender, MouseEventArgs e)
        {
            /*
             * Description : Enter side btn event
             * Input parameter : default event parameter
             * Output : void
            */

            Border overed = sender as Border; // get what button send event
            if (overed.Child is System.Windows.Controls.Image)
            {
                System.Windows.Controls.Image overed_img = overed.Child as System.Windows.Controls.Image;

                BitmapImage bt = new BitmapImage();
                bt.BeginInit();
                bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_01.png", "_02.png"));
                bt.EndInit();
                overed_img.Source = bt;
            }
            else if (overed.Child is Label)
            {
                Label overed_lbl = overed.Child as Label;
                overed_lbl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ffffff"));
            }
        }

        //function #010
        private void side_btn_mouse_leave(object sender, MouseEventArgs e)
        {
            /*
             * Description : Leave side btn event
             * Input parameter : default event parameter
             * Output : void
            */

            Border overed = sender as Border; // get what button send event
            int now_index = selected_row * max_col + selected_col;


            if (overed.Child is System.Windows.Controls.Image)
            {
                if ((overed.Name == "btn_06" && reset_list_to_dict["pan_mode"] == true)
                    || (overed.Name == "btn_07" && reset_list_to_dict["pin_zoom_mode"] == true)
                    || (overed.Name == "btn_08" && reset_list_to_dict["zoom_mode"] == true)
                    || (overed.Name == "btn_11" && flip_flag[now_index][0] == 1)
                    || (overed.Name == "btn_12" && flip_flag[now_index][1] == 1)
                    || (overed.Name == "btn_13" && reset_list_to_dict["wwwl_mode"] == true)
                    || (overed.Name == "btn_14" && inv_index[now_index] == true)
                    || (overed.Name == "btn_15" && reset_list_to_dict["angle_mode"] == true)
                    || (overed.Name == "btn_16" && reset_list_to_dict["dist_mode"] == true)
                    || (overed.Name == "btn_17" && label_on_off == true)
                    || (overed.Name == "btn_18" && reset_list_to_dict["erase_mode"] == true)
                    ) // disable if btn option not activate
                {
                    return;
                }
                System.Windows.Controls.Image overed_img = overed.Child as System.Windows.Controls.Image;

                BitmapImage bt = new BitmapImage();
                bt.BeginInit();
                bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_02.png", "_01.png"));
                bt.EndInit();
                overed_img.Source = bt;
            }
            else if (overed.Child is Label)
            {
                Label overed_lbl = overed.Child as Label;
                overed_lbl.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#aba49c"));
            }
        }

        //function #011
        private void drawing_lines_mouse_enter(object sender, MouseEventArgs e)
        {
            /*
             * Description : Enter drawing line event
             * Input parameter : default event parameter
             * Output : void
            */

            var overed = sender as FrameworkElement; // get what line mouse overed

            //----add overed line's tag to list----//
            int add = Int32.Parse(overed.Tag.ToString());
            if (!overed_line_list.Contains(add))
                overed_line_list.Add(add);
            /////////////////////////////////////////

            //----change color----//
            for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
            {
                var search_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                for (int j = 0; j < search_grid.Children.Count; j++)
                {
                    if ((search_grid.Children[j] as FrameworkElement).Tag == null)
                        continue;
                    if (overed_line_list.Contains(Int32.Parse((search_grid.Children[j] as FrameworkElement).Tag.ToString())))
                    {
                        if (search_grid.Children[j] is Label)
                        {
                            (search_grid.Children[j] as Label).Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
                        }
                        else if (search_grid.Children[j] is Ellipse)
                        {
                            (search_grid.Children[j] as Ellipse).Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
                        }
                        else if (search_grid.Children[j] is Line)
                        {
                            (search_grid.Children[j] as Line).Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
                        }
                    }
                }
            }
            ////////////////////////
        }

        //function #011
        private void drawing_lines_mouse_leave(object sender, MouseEventArgs e)
        {
            /*
             * Description : Leave drawing line event
             * Input parameter : default event parameter
             * Output : void
            */

            //----remove overed line's tag to list----//
            var overed = sender as FrameworkElement;
            int add = Int32.Parse(overed.Tag.ToString());
            if (overed_line_list.Contains(add))
                overed_line_list.Remove(add);
            /////////////////////////////////////////

            //----restore and change color----//
            for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
            {
                var search_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                for (int j = 0; j < search_grid.Children.Count; j++)
                {
                    if ((search_grid.Children[j] as FrameworkElement).Tag == null)
                        continue;
                    if (overed_line_list.Contains(Int32.Parse((search_grid.Children[j] as FrameworkElement).Tag.ToString())))
                    {
                        SolidColorBrush over_color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 0));
                        if (search_grid.Children[j] is Label)
                        {
                            (search_grid.Children[j] as Label).Foreground = over_color;
                        }
                        else if (search_grid.Children[j] is Ellipse)
                        {
                            (search_grid.Children[j] as Ellipse).Stroke = over_color;
                        }
                        else if (search_grid.Children[j] is Line)
                        {
                            (search_grid.Children[j] as Line).Stroke = over_color;
                        }
                    }
                    else
                    {
                        SolidColorBrush non_over_color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                        if (search_grid.Children[j] is Label)
                        {
                            (search_grid.Children[j] as Label).Foreground = non_over_color;
                        }
                        else if (search_grid.Children[j] is Ellipse)
                        {
                            (search_grid.Children[j] as Ellipse).Stroke = non_over_color;
                        }
                        else if (search_grid.Children[j] is Line)
                        {
                            (search_grid.Children[j] as Line).Stroke = non_over_color;
                        }
                    }
                }
            }
            ////////////////////////////////////

        }

        //function #012
        private void drawing_lines_mouse_left_button_down(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : drawing line mouse left button down event
             * Input parameter : default event parameter
             * Output : void
                 * 추가정보
                    > tag를 기준으로 탐색하여 삭제
            */

            //----erase overlay lines find by tag----//
            for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
            {
                var search_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                for (int j = search_grid.Children.Count - 1; j >= 0; j--)
                {
                    if ((search_grid.Children[j] as FrameworkElement).Tag == null)
                        continue;
                    if (overed_line_list.Contains(Int32.Parse((search_grid.Children[j] as FrameworkElement).Tag.ToString())))
                    {
                        search_grid.Children.RemoveAt(j);
                    }
                }
            }
            ///////////////////////////////////////////
        }

        //function #013
        private void dcm_viewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : Dicom viewer mouse click event
             * Input parameter : default event parameter
             * Output : void
            */

            //----normal click -> select viewer change----//
            for (int i = 0; i < grid_viewer.Children.Count; i++)
            {
                if (grid_viewer.Children[i] is Border)
                {
                    var check_border = grid_viewer.Children[i] as Border;
                    check_border.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#20a5d6"));
                }
            }
            ////////////////////////////////////////////////

            //----initialize parameter----//
            var clicked_border = sender as Border;
            clicked_border.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#f7dd44"));
            selected_col = (int)clicked_border.GetValue(Grid.ColumnProperty);
            selected_row = (int)clicked_border.GetValue(Grid.RowProperty);
            side_btn_update();
            last_x = -1;
            last_y = -1;
            mouse_left_button_down = true;
            int now_index = selected_row * max_col + selected_col;
            ////////////////////////////////


            if (reset_list_to_dict["angle_mode"] == true && ((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).IsMouseOver == true)
            {
                if (angle_step == 0)
                {
                    //----set selected ui to variable----//
                    drawing_border = grid_viewer.Children[(now_index) * 3] as Border;
                    drawing_grid = drawing_border.Child as Grid;
                    ///////////////////////////////////////

                    Tuple<double, double> now_position = new Tuple<double, double>(e.GetPosition(drawing_grid).X - 5, e.GetPosition(drawing_grid).Y - 5); // get mouse pointer's position
                    if (!(now_position.Item1 > drawing_grid.ActualWidth || now_position.Item2 > drawing_grid.ActualHeight))
                    {
                        angle_line_list.Add(now_position);
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            Ellipse temp_ellipse = new Ellipse();
                            temp_ellipse.Width = 10;
                            temp_ellipse.Height = 10;
                            temp_ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_ellipse.Tag = drawing_count;
                            temp_ellipse.VerticalAlignment = VerticalAlignment.Top;
                            temp_ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                            temp_ellipse.Margin = new Thickness(now_position.Item1, now_position.Item2, 0, 0);
                            temp_ellipse.IsHitTestVisible = false;
                            temp_ellipse.MouseEnter += drawing_lines_mouse_enter;
                            temp_ellipse.MouseLeave += drawing_lines_mouse_leave;
                            temp_ellipse.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            Grid drawing_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            drawing_grid.Children.Add(temp_ellipse);

                            Line temp_line = new Line();
                            temp_line.X1 = now_position.Item1 + 5;
                            temp_line.Y1 = now_position.Item2 + 5;
                            temp_line.X2 = now_position.Item1 + 5;
                            temp_line.Y2 = now_position.Item2 + 5;
                            temp_line.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_line.Tag = drawing_count;
                            temp_line.StrokeThickness = 2;
                            temp_line.IsHitTestVisible = false;
                            temp_line.MouseEnter += drawing_lines_mouse_enter;
                            temp_line.MouseLeave += drawing_lines_mouse_leave;
                            temp_line.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            drawing_grid.Children.Add(temp_line);
                            drawing_line[i] = temp_line;
                        }
                        angle_step = 1;
                    }
                }
                else if (angle_step == 1)
                {
                    Tuple<double, double> now_position = new Tuple<double, double>(e.GetPosition(drawing_grid).X - 5, e.GetPosition(drawing_grid).Y - 5);
                    if (!(now_position.Item1 > drawing_grid.ActualWidth || now_position.Item2 > drawing_grid.ActualHeight))
                    {
                        angle_line_list.Add(now_position);
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            Ellipse temp_ellipse = new Ellipse();
                            temp_ellipse.Width = 10;
                            temp_ellipse.Height = 10;
                            temp_ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_ellipse.StrokeThickness = 2;
                            temp_ellipse.Tag = drawing_count;
                            temp_ellipse.VerticalAlignment = VerticalAlignment.Top;
                            temp_ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                            temp_ellipse.Margin = new Thickness(now_position.Item1, now_position.Item2, 0, 0);
                            temp_ellipse.IsHitTestVisible = false;
                            temp_ellipse.MouseEnter += drawing_lines_mouse_enter;
                            temp_ellipse.MouseLeave += drawing_lines_mouse_leave;
                            temp_ellipse.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            Grid drawing_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            drawing_grid.Children.Add(temp_ellipse);

                            Line temp_line = new Line();
                            temp_line.X1 = now_position.Item1 + 5;
                            temp_line.Y1 = now_position.Item2 + 5;
                            temp_line.X2 = now_position.Item1 + 5;
                            temp_line.Y2 = now_position.Item2 + 5;
                            temp_line.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_line.Tag = drawing_count;
                            temp_line.StrokeThickness = 2;
                            temp_line.IsHitTestVisible = false;
                            temp_line.MouseEnter += drawing_lines_mouse_enter;
                            temp_line.MouseLeave += drawing_lines_mouse_leave;
                            temp_line.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            drawing_grid.Children.Add(temp_line);
                            drawing_line[i] = temp_line;

                            Label temp_angle = new Label();
                            temp_angle.VerticalAlignment = VerticalAlignment.Top;
                            temp_angle.HorizontalContentAlignment = HorizontalAlignment.Left;
                            temp_angle.Margin = new Thickness(now_position.Item1 + 70 > viewer_image_list[0].ActualWidth ? viewer_image_list[0].ActualWidth - 50 : now_position.Item1 + 20, now_position.Item2 + 50 > viewer_image_list[0].ActualHeight ? viewer_image_list[0].ActualWidth - 30 : now_position.Item2 + 20, 0, 0);
                            temp_angle.Tag = drawing_count;
                            temp_angle.FontSize = 20;
                            temp_angle.IsHitTestVisible = false;
                            temp_angle.MouseEnter += drawing_lines_mouse_enter;
                            temp_angle.MouseLeave += drawing_lines_mouse_leave;
                            temp_angle.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            temp_angle.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            drawing_label[i] = temp_angle;

                            drawing_grid.Children.Add(temp_angle);
                        }
                        angle_step = 2;
                    }
                }
                else if (angle_step == 2)
                {
                    Tuple<double, double> now_position = new Tuple<double, double>(e.GetPosition(drawing_grid).X - 5, e.GetPosition(drawing_grid).Y - 5);
                    if (!(now_position.Item1 > drawing_grid.ActualWidth || now_position.Item2 > drawing_grid.ActualHeight))
                    {
                        angle_line_list.Add(now_position);
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            Ellipse temp_ellipse = new Ellipse();
                            temp_ellipse.Width = 10;
                            temp_ellipse.Height = 10;
                            temp_ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_ellipse.Tag = drawing_count;
                            temp_ellipse.VerticalAlignment = VerticalAlignment.Top;
                            temp_ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                            temp_ellipse.Margin = new Thickness(now_position.Item1, now_position.Item2, 0, 0);
                            temp_ellipse.IsHitTestVisible = false;
                            temp_ellipse.MouseEnter += drawing_lines_mouse_enter;
                            temp_ellipse.MouseLeave += drawing_lines_mouse_leave;
                            temp_ellipse.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            Grid drawing_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            drawing_grid.Children.Add(temp_ellipse);
                        }
                        drawing_line = new Line[max_col * max_row];
                        angle_step = 0;
                        drawing_count++;
                    }
                }
            }
            else if (reset_list_to_dict["dist_mode"] == true && ((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).IsMouseOver == true)
            {
                if (dist_step == 0)
                {
                    //----set selected ui to variable----//
                    drawing_border = grid_viewer.Children[(now_index) * 3] as Border;
                    drawing_grid = drawing_border.Child as Grid;
                    ///////////////////////////////////////

                    Tuple<double, double> now_position = new Tuple<double, double>(e.GetPosition(drawing_grid).X - 5, e.GetPosition(drawing_grid).Y - 5);
                    if (!(now_position.Item1 > drawing_grid.ActualWidth || now_position.Item2 > drawing_grid.ActualHeight))
                    {
                        dist_line_list.Add(now_position);
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            Ellipse temp_ellipse = new Ellipse();
                            temp_ellipse.Width = 10;
                            temp_ellipse.Height = 10;
                            temp_ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_ellipse.Tag = drawing_count;
                            temp_ellipse.VerticalAlignment = VerticalAlignment.Top;
                            temp_ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                            temp_ellipse.Margin = new Thickness(now_position.Item1, now_position.Item2, 0, 0);
                            temp_ellipse.IsHitTestVisible = false;
                            temp_ellipse.MouseEnter += drawing_lines_mouse_enter;
                            temp_ellipse.MouseLeave += drawing_lines_mouse_leave;
                            temp_ellipse.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            //Grid drawing_grid = grid_viewer.Children[i * 3 + 2] as Grid;
                            Grid drawing_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            drawing_grid.Children.Add(temp_ellipse);

                            Line temp_line = new Line();
                            temp_line.X1 = now_position.Item1 + 5;
                            temp_line.Y1 = now_position.Item2 + 5;
                            temp_line.X2 = now_position.Item1 + 5;
                            temp_line.Y2 = now_position.Item2 + 5;
                            temp_line.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_line.Tag = drawing_count;
                            temp_line.StrokeThickness = 2;
                            temp_line.IsHitTestVisible = false;
                            temp_line.MouseEnter += drawing_lines_mouse_enter;
                            temp_line.MouseLeave += drawing_lines_mouse_leave;
                            temp_line.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            drawing_grid.Children.Add(temp_line);
                            drawing_line[i] = temp_line;

                            Label temp_dist = new Label();
                            temp_dist.VerticalAlignment = VerticalAlignment.Top;
                            temp_dist.HorizontalContentAlignment = HorizontalAlignment.Left;
                            temp_dist.Margin = new Thickness(now_position.Item1 + 20, now_position.Item2, 0, 0);
                            temp_dist.Tag = drawing_count;
                            temp_dist.FontSize = 20;
                            temp_dist.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_dist.IsHitTestVisible = false;
                            temp_dist.MouseEnter += drawing_lines_mouse_enter;
                            temp_dist.MouseLeave += drawing_lines_mouse_leave;
                            temp_dist.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            drawing_label[i] = temp_dist;

                            drawing_grid.Children.Add(temp_dist);
                        }
                        dist_step = 1;
                    }
                }
                else if (dist_step == 1)
                {
                    Tuple<double, double> now_position = new Tuple<double, double>(e.GetPosition(drawing_grid).X - 5, e.GetPosition(drawing_grid).Y - 5);
                    if (!(now_position.Item1 > drawing_grid.ActualWidth || now_position.Item2 > drawing_grid.ActualHeight))
                    {
                        dist_line_list.Add(now_position);
                        for (int i = 0; i < grid_viewer.Children.Count / 3; i++)
                        {
                            Ellipse temp_ellipse = new Ellipse();
                            temp_ellipse.Width = 10;
                            temp_ellipse.Height = 10;
                            temp_ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF7F00"));
                            temp_ellipse.StrokeThickness = 2;
                            temp_ellipse.Tag = drawing_count;
                            temp_ellipse.VerticalAlignment = VerticalAlignment.Top;
                            temp_ellipse.HorizontalAlignment = HorizontalAlignment.Left;
                            temp_ellipse.Margin = new Thickness(now_position.Item1, now_position.Item2, 0, 0);
                            temp_ellipse.IsHitTestVisible = false;
                            temp_ellipse.MouseEnter += drawing_lines_mouse_enter;
                            temp_ellipse.MouseLeave += drawing_lines_mouse_leave;
                            temp_ellipse.MouseLeftButtonDown += drawing_lines_mouse_left_button_down;
                            //Grid drawing_grid = grid_viewer.Children[i * 3 + 2] as Grid;
                            Grid drawing_grid = ((viewer_image_list[i].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
                            drawing_grid.Children.Add(temp_ellipse);
                        }
                        drawing_count++;
                        dist_step = 0;
                    }
                }
            }
            else if (reset_list_to_dict["pin_zoom_mode"] == true && ((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).IsMouseOver == true)
            {
                var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();
                pin_scroll.Visibility = Visibility.Visible; // pin_zoom_area visible
                pin_zoom_area.Source = modules.modules.Convert(img_for_show);

                //----fit pin_zoom_area into viewer area----//
                double left = e.GetPosition(big_window).X - pin_zoom_size / 2.0 < 0 ? 0 : e.GetPosition(big_window).X - pin_zoom_size / 2.0;
                double top = e.GetPosition(big_window).Y - pin_zoom_size / 2.0 < 0 ? 0 : e.GetPosition(big_window).Y - pin_zoom_size / 2.0;
                if (e.GetPosition(big_window).X + pin_zoom_size / 2.0 > big_window.ActualWidth)
                    left = big_window.ActualWidth - pin_zoom_size;
                if (e.GetPosition(big_window).Y + pin_zoom_size / 2.0 > big_window.ActualHeight)
                    top = big_window.ActualHeight - pin_zoom_size;

                var ttv = (grid_viewer.Children[now_index * 3] as Border).TransformToVisual(big_window).Transform(new System.Windows.Point(0, 0));
                left = left < ttv.X ? ttv.X : left + pin_zoom_size > ttv.X + (grid_viewer.Children[now_index * 3] as Border).ActualWidth ? ttv.X + (grid_viewer.Children[now_index * 3] as Border).ActualWidth - pin_zoom_size : left;
                top = top < ttv.Y ? ttv.Y : top + pin_zoom_size > ttv.Y + (grid_viewer.Children[now_index * 3] as Border).ActualHeight ? ttv.Y + (grid_viewer.Children[now_index * 3] as Border).ActualHeight - pin_zoom_size : top;

                pin_grid.Margin = new Thickness(left, top, 0, 0);
                pin_grid.Width = pin_zoom_size;
                pin_grid.Height = pin_zoom_size;
                //////////////////////////////////////////////

                //----set zoom setting----//
                double now_x = (e.GetPosition(viewer_image_list[now_index]).X / viewer_image_list[now_index].ActualWidth) * pin_zoom_size;
                double now_y = (e.GetPosition(viewer_image_list[now_index]).Y / viewer_image_list[now_index].ActualHeight) * pin_zoom_size;
                now_x = now_x < 0 ? 0 : now_x > pin_zoom_size ? pin_zoom_size : now_x;
                now_y = now_y < 0 ? 0 : now_y > pin_zoom_size ? pin_zoom_size : now_y;
                pin_scale_trans.CenterX = now_x;
                pin_scale_trans.CenterY = now_y;
                pin_scale_trans.ScaleX = 4;
                pin_scale_trans.ScaleY = 4;
                ////////////////////////////
            }
            else if (reset_list_to_dict["zoom_mode"] == true && ((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).IsMouseOver == true)
            {
                //----set drag zoom default setting----//
                ScrollViewer now_scroll_viewer = viewer_image_list[now_index].Parent as ScrollViewer;
                TransformGroup scroll_tfGroup = now_scroll_viewer.RenderTransform as TransformGroup;
                ScaleTransform scroll_scale = scroll_tfGroup.Children[0] as ScaleTransform;

                double now_x = e.GetPosition(viewer_image_list[now_index]).X;
                double now_y = e.GetPosition(viewer_image_list[now_index]).Y;
                scroll_scale.CenterX = now_x;
                scroll_scale.CenterY = now_y;
                scroll_center = e.GetPosition(big_window).Y;
                /////////////////////////////////////////
            }
        }

        //function #013
        private void dcm_viewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : Dicom viewer mouse click event
             * Input parameter : default event parameter
             * Output : void
            */
            int now_index = selected_row * max_col + selected_col;
            if (mouse_left_button_down == true)
            {
                mouse_left_button_down = false;

                if (reset_list_to_dict["pin_zoom_mode"] == true)
                {

                    pin_scroll.Visibility = Visibility.Hidden;
                }
            }
        }

        //function #014
        private void dcm_viewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : Dicom viewer mouse right button click event
             * Input parameter : default event parameter
             * Output : void
            */
            var clicked_border = sender as Border;
            if (mouse_right_button_down == false)
            {
                selected_col = (int)clicked_border.GetValue(Grid.ColumnProperty);
                selected_row = (int)clicked_border.GetValue(Grid.RowProperty);
                last_x = e.GetPosition(big_window).X;
                last_y = e.GetPosition(big_window).Y;
                mouse_right_button_down = true;
            }
        }

        //function #014
        private void dcm_viewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            /*
             * Description : Dicom viewer mouse right button click event
             * Input parameter : default event parameter
             * Output : void
            */
            if (this.mouse_right_button_down == true)
            {
                this.mouse_right_button_down = false;
            }
        }

        //function #015
        private void dcm_viewer_MouseMove(object sender, MouseEventArgs e)
        {
            /*
             * Description : Dicom viewer mouse move event
             * Input parameter : default event parameter
             * Output : void
            */
            try
            {
                //----set now selected index----//
                int col = selected_col;
                int row = selected_row;
                int now_index = row * max_col + col;
                //////////////////////////////////


                //----default Window width & Window center control event----//
                if (mouse_right_button_down == true)
                {
                    if (last_x == -1.0)
                    {
                        last_x = e.GetPosition(big_window).X;
                        last_y = e.GetPosition(big_window).Y;
                        return;
                    }
                    dcm_img[now_index].WindowWidth += (((e.GetPosition(big_window).X) - last_x) / grid_viewer.ActualWidth) * dcm_img[now_index].WindowWidth;
                    dcm_img[now_index].WindowCenter -= (((e.GetPosition(big_window).Y) - last_y) / grid_viewer.ActualHeight) * dcm_img[now_index].WindowCenter;
                    last_x = e.GetPosition(big_window).X;
                    last_y = e.GetPosition(big_window).Y;
                    var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();

                    viewer_image_list[now_index].Source = modules.modules.Convert(img_for_show);
                    viewer_label_list[now_index].Content = "WW : " + Math.Round(dcm_img[now_index].WindowWidth).ToString() + ", WL : " + Math.Round(dcm_img[now_index].WindowCenter).ToString();
                }
                //////////////////////////////////////////////////////////////

                else if (mouse_left_button_down == true)
                {
                    // pan_mode move event
                    if (reset_list_to_dict["pan_mode"] == true)
                    {
                        if (last_x == -1.0)
                        {
                            last_x = e.GetPosition(big_window).X;
                            last_y = e.GetPosition(big_window).Y;
                            return;
                        }

                        double move_x = (e.GetPosition(big_window).X) - last_x;
                        double move_y = (e.GetPosition(big_window).Y) - last_y;
                        last_x = e.GetPosition(big_window).X;
                        last_y = e.GetPosition(big_window).Y;
                        var selected_transformGroup = viewer_image_list[now_index].RenderTransform as TransformGroup;
                        var selected_translate = selected_transformGroup.Children[0] as TranslateTransform;
                        selected_translate.X = viewer_margin_list[now_index].Item1 + move_x;
                        selected_translate.Y = (viewer_margin_list[now_index].Item2 + move_y);
                        viewer_margin_list[now_index] = new Tuple<double, double>(viewer_margin_list[now_index].Item1 + move_x, viewer_margin_list[now_index].Item2 + move_y);
                    }

                    // Window width & Window center move event when mode on 
                    else if (reset_list_to_dict["wwwl_mode"] == true)
                    {
                        if (last_x == -1.0)
                        {
                            last_x = e.GetPosition(big_window).X;
                            last_y = e.GetPosition(big_window).Y;
                            return;
                        }
                        dcm_img[now_index].WindowWidth += (((e.GetPosition(big_window).X) - last_x) / grid_viewer.ActualWidth) * dcm_img[now_index].WindowWidth;
                        dcm_img[now_index].WindowCenter -= (((e.GetPosition(big_window).Y) - last_y) / grid_viewer.ActualHeight) * dcm_img[now_index].WindowCenter;
                        last_x = e.GetPosition(big_window).X;
                        last_y = e.GetPosition(big_window).Y;
                        var img_for_show = dcm_img[now_index].RenderImage().As<Bitmap>();

                        viewer_image_list[now_index].Source = modules.modules.Convert(img_for_show);
                        viewer_label_list[now_index].Content = "WW : " + Math.Round(dcm_img[now_index].WindowWidth).ToString() + ", WL : " + Math.Round(dcm_img[now_index].WindowCenter).ToString();
                    }

                    // drag zoom move event
                    else if (reset_list_to_dict["zoom_mode"] == true)
                    {
                        ScrollViewer now_scroll_viewer = viewer_image_list[now_index].Parent as ScrollViewer;
                        TransformGroup scroll_tfGroup = now_scroll_viewer.RenderTransform as TransformGroup;
                        ScaleTransform scroll_scale = scroll_tfGroup.Children[0] as ScaleTransform;

                        double movement = e.GetPosition(big_window).Y - scroll_center;


                        scroll_scale.ScaleX *= (1 + movement / big_window.ActualHeight);
                        scroll_scale.ScaleY *= (1 + movement / big_window.ActualHeight);
                        if (scroll_scale.ScaleX <= 0.5)
                        {
                            scroll_scale.ScaleX = 0.5;
                        }
                        else if (scroll_scale.ScaleX >= 5)
                        {
                            scroll_scale.ScaleX = 5;
                        }
                        if (scroll_scale.ScaleY <= 0.5)
                        {
                            scroll_scale.ScaleY = 0.5;
                        }
                        else if (scroll_scale.ScaleY >= 5)
                        {
                            scroll_scale.ScaleY = 5;
                        }

                        Console.WriteLine(scroll_scale.ScaleX + ", " + scroll_scale.ScaleY);
                    }
                }

                //angle mode move event
                if (reset_list_to_dict["angle_mode"] == true && angle_step != 0)
                {
                    for (int i = 0; i < drawing_line.Length; i++)
                    {
                        drawing_line[i].X2 = e.GetPosition(drawing_grid).X;
                        drawing_line[i].Y2 = e.GetPosition(drawing_grid).Y;
                        if (angle_step == 2)
                        {
                            //----calc angle----//
                            double len_12, len_23, len_13;
                            double len_12_2, len_23_2, len_13_2;
                            len_12 = Math.Pow(angle_line_list[angle_line_list.Count - 1].Item1 - angle_line_list[angle_line_list.Count - 2].Item1, 2) +
                                Math.Pow(angle_line_list[angle_line_list.Count - 1].Item2 - angle_line_list[angle_line_list.Count - 2].Item2, 2);
                            len_23 = Math.Pow(angle_line_list[angle_line_list.Count - 1].Item1 - drawing_line[i].X2, 2) +
                                Math.Pow(angle_line_list[angle_line_list.Count - 1].Item2 - drawing_line[i].Y2, 2);
                            len_13 = Math.Pow(drawing_line[i].X2 - angle_line_list[angle_line_list.Count - 2].Item1, 2) +
                                Math.Pow(drawing_line[i].Y2 - angle_line_list[angle_line_list.Count - 2].Item2, 2);
                            len_12_2 = Math.Pow(len_12, 0.5);
                            len_23_2 = Math.Pow(len_23, 0.5);
                            len_13_2 = Math.Pow(len_13, 0.5);

                            double angle = ((len_12 + len_23 - len_13) / (2 * len_12_2 * len_23_2));
                            //////////////////////

                            drawing_label[i].Content = (Math.Round((1 - angle) * 90, 1)).ToString() + "˚";
                        }
                    }
                }

                //dist mode move event
                if (reset_list_to_dict["dist_mode"] == true && dist_step != 0)
                {
                    for (int i = 0; i < drawing_line.Length; i++)
                    {
                        drawing_line[i].X2 = e.GetPosition(drawing_grid).X;
                        drawing_line[i].Y2 = e.GetPosition(drawing_grid).Y;
                        drawing_label[i].Margin = new Thickness(drawing_line[i].X2 + 100 > viewer_image_list[0].ActualWidth ? viewer_image_list[0].ActualWidth - 100 : drawing_line[i].X2 < 10 ? 10 : drawing_line[i].X2, drawing_line[i].Y2 + 30 > viewer_image_list[0].ActualHeight ? viewer_image_list[0].ActualHeight - 30 : drawing_line[i].Y2, 0, 0);
                        //----calc dist----//
                        if (dist_step == 1)
                        {
                            double pixel_x = (drawing_line[i].X2 - drawing_line[i].X1) / viewer_image_list[i].ActualWidth * load_width;
                            double pixel_Y = (drawing_line[i].Y2 - drawing_line[i].Y1) / viewer_image_list[i].ActualHeight * load_height;

                            double dist = Math.Pow(Math.Pow(pixel_x, 2) + Math.Pow(pixel_Y, 2), 0.5) * load_pixel_space;

                            drawing_label[i].Content = Math.Round(dist, 2).ToString() + "mm";
                        }
                        /////////////////////
                    }
                }

                //pin zoom mode move event
                if (reset_list_to_dict["pin_zoom_mode"] == true && mouse_left_button_down == true)
                {
                    double left = e.GetPosition(big_window).X - pin_zoom_size / 2.0 < 0 ? 0 : e.GetPosition(big_window).X - pin_zoom_size / 2.0;
                    double top = e.GetPosition(big_window).Y - pin_zoom_size / 2.0 < 0 ? 0 : e.GetPosition(big_window).Y - pin_zoom_size / 2.0;
                    if (e.GetPosition(big_window).X + pin_zoom_size / 2.0 > big_window.ActualWidth)
                        left = big_window.ActualWidth - pin_zoom_size;
                    if (e.GetPosition(big_window).Y + pin_zoom_size / 2.0 > big_window.ActualHeight)
                        top = big_window.ActualHeight - pin_zoom_size;

                    //----calc absolute position----//
                    var ttv = (grid_viewer.Children[now_index * 3] as Border).TransformToVisual(big_window).Transform(new System.Windows.Point(0, 0));
                    left = left < ttv.X ? ttv.X : left + pin_zoom_size > ttv.X + (grid_viewer.Children[now_index * 3] as Border).ActualWidth ? ttv.X + (grid_viewer.Children[now_index * 3] as Border).ActualWidth - pin_zoom_size : left;
                    top = top < ttv.Y ? ttv.Y : top + pin_zoom_size > ttv.Y + (grid_viewer.Children[now_index * 3] as Border).ActualHeight ? ttv.Y + (grid_viewer.Children[now_index * 3] as Border).ActualHeight - pin_zoom_size : top;
                    //////////////////////////////////

                    pin_grid.Margin = new Thickness(left, top, 0, 0);

                    double now_x = (e.GetPosition(viewer_image_list[now_index]).X / viewer_image_list[now_index].ActualWidth) * pin_zoom_size;
                    double now_y = (e.GetPosition(viewer_image_list[now_index]).Y / viewer_image_list[now_index].ActualHeight) * pin_zoom_size;
                    now_x = now_x < 0 ? 0 : now_x > pin_zoom_size ? pin_zoom_size : now_x;
                    now_y = now_y < 0 ? 0 : now_y > pin_zoom_size ? pin_zoom_size : now_y;
                    pin_scale_trans.CenterX = now_x;
                    pin_scale_trans.CenterY = now_y;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }

        //function #016
        private void rotate_flip_viewer(int now_index)
        {
            /*
             * Description : Rotate and flip viewer image
             * Input parameter : int now_index
             * Output : void
            */

            //----set default variable of transforms----//
            RotateTransform temp_rotate;
            TransformGroup temp_tfGroup;
            ScaleTransform temp_scale;
            temp_rotate = new RotateTransform();
            temp_scale = new ScaleTransform();
            //////////////////////////////////////////////

            temp_rotate.Angle = 90.0 * rotate_index[now_index]; // set rotate

            //----set flip----//
            temp_scale.ScaleX = flip_flag[now_index][0] == 0 ? 1 : -1;
            temp_scale.ScaleY = flip_flag[now_index][1] == 0 ? 1 : -1;
            ////////////////////

            //----set transform to viewer----//
            temp_tfGroup = new TransformGroup();
            temp_tfGroup.Children.Add(temp_rotate);
            temp_tfGroup.Children.Add(temp_scale);
            var viewer_whole_grid = ((viewer_image_list[now_index].Parent as ScrollViewer).Parent as Grid).Parent as Grid;
            viewer_whole_grid.LayoutTransform = temp_tfGroup;
            ///////////////////////////////////

            //----set rotate and flip to overlay text label----//
            RotateTransform label_rotate = new RotateTransform();
            TransformGroup label_tfGroup = new TransformGroup();
            ScaleTransform label_scale = new ScaleTransform();
            label_rotate.Angle = 360.0 - temp_rotate.Angle;
            label_rotate.CenterX = 0.5;
            label_rotate.CenterY = 0.5;
            label_scale.ScaleX = 1 / temp_scale.ScaleX;
            label_scale.ScaleY = 1 / temp_scale.ScaleY;
            label_scale.CenterX = 0.5;
            label_scale.CenterY = 0.5;
            label_tfGroup.Children.Add(label_rotate);
            label_tfGroup.Children.Add(label_scale);

            for (int i = 0; i < viewer_whole_grid.Children.Count; i++)
            {
                if (viewer_whole_grid.Children[i] is Label)
                {
                    Label temp = viewer_whole_grid.Children[i] as Label;
                    temp.RenderTransform = label_tfGroup;
                }
            }
            /////////////////////////////////////////////////////
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            var overed_border = sender as Border;
            ColorAnimation animation;
            if (select_mode == false)
            {
                animation = new ColorAnimation();
                animation.From = (overed_border.Background as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 62, 89, 117);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                overed_border.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            var overed_border = sender as Border;
            ColorAnimation animation;
            if (select_mode == false)
            {
                animation = new ColorAnimation();
                animation.From = (overed_border.BorderBrush as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 175, 175, 175);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                overed_border.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                animation = new ColorAnimation();
                animation.From = (overed_border.Background as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 55, 58, 58);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                overed_border.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var overed_border = sender as Border;
            select_mode = !select_mode;
            System.Windows.Controls.Image overed_img;
            BitmapImage bt;

            ColorAnimation animation;
            Int32Animation int_animation;

            if (select_mode == true)
            {
                animation = new ColorAnimation();
                animation.From = (overed_border.BorderBrush as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 247, 221, 68);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                overed_border.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                animation = new ColorAnimation();
                animation.From = (overed_border.Background as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 22, 32, 43);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                //animation.Completed += Animation_Completed;
                overed_border.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                overed_img = overed_border.Child as System.Windows.Controls.Image;

                bt = new BitmapImage();
                bt.BeginInit();
                bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_01.png", "_02.png"));
                bt.EndInit();
                overed_img.Source = bt;
                grid_select.SetValue(Grid.ZIndexProperty, 100);

                //grid_viewer.SetValue(Grid.ColumnSpanProperty, 1);
                //grid_viewer.SetValue(Grid.ColumnProperty, 1);
            }
            else
            {
                animation = new ColorAnimation();
                animation.From = (overed_border.BorderBrush as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 175, 175, 175);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                overed_border.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                animation = new ColorAnimation();
                animation.From = (overed_border.Background as SolidColorBrush).Color;
                animation.To = System.Windows.Media.Color.FromArgb(255, 62, 89, 117);
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                //animation.Completed += Animation_Completed;
                overed_border.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                overed_img = overed_border.Child as System.Windows.Controls.Image;

                bt = new BitmapImage();
                bt.BeginInit();
                bt.UriSource = new Uri(overed_img.Source.ToString().Replace("_02.png", "_01.png"));
                bt.EndInit();
                overed_img.Source = bt;

                grid_select.SetValue(Grid.ZIndexProperty, 0);

                //grid_viewer.SetValue(Grid.ColumnSpanProperty, 2);
                //grid_viewer.SetValue(Grid.ColumnProperty, 0);
            }
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            for (int i = 0; i < viewer_image_list.Length; i++)
            {
                var image = viewer_image_list[i];
                var scroll_viewer = image.Parent as ScrollViewer;
                var grid_2 = scroll_viewer.Parent as Grid;
                var grid_1 = grid_2.Parent as Grid;

                Border base_border = grid_viewer.Children[0] as Border;

                if (base_border.ActualWidth > base_border.ActualHeight)
                {
                    image.Height = base_border.ActualHeight;
                    image.Width = base_border.ActualHeight * load_width / load_height;
                }
                else
                {
                    image.Width = base_border.ActualWidth;
                    image.Height = base_border.ActualWidth * load_height / load_width;
                }
                grid_1.Margin = new Thickness(0, 0, 0, 0);

            }
        }
    }
}
