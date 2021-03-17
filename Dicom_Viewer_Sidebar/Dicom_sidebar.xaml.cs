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
using Dicom;
using Dicom.Imaging;
using System.Drawing;

namespace Sidebar
{
    /// <summary>
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Dicom_sidebar : UserControl
    {
        private string dicom_path = "";
        string[] dir_list;
        List<Grid> thumbnail_list = new List<Grid>();
        public Dicom_sidebar(string path)
        {
            InitializeComponent();
            dicom_path = path;

            dir_list = Directory.GetDirectories(dicom_path);

            for (int i = 0; i < dir_list.Length; i++)
            {
                Grid select_base_grid = new Grid();
                select_base_grid.Height = (this.Content as Grid).ActualHeight * 0.25;
                select_base_grid.VerticalAlignment = VerticalAlignment.Top;
                select_base_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                select_base_grid.Margin = new Thickness(0, (this.Content as Grid).ActualHeight * 0.25 * (i * 2) >= 0 ? (this.Content as Grid).ActualHeight * 0.25 * (i * 2) : 0, 0, 0);
                select_base_grid.Tag = i * 2;
                grid_show_thumbnails.Children.Add(select_base_grid);
                thumbnail_list.Add(select_base_grid);

                select_base_grid = new Grid();
                select_base_grid.Height = (this.Content as Grid).ActualHeight * 0.25;
                select_base_grid.VerticalAlignment = VerticalAlignment.Top;
                select_base_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                select_base_grid.Margin = new Thickness(0, (this.Content as Grid).ActualHeight * 0.25 * (i * 2 + 1) >= 0 ? (this.Content as Grid).ActualHeight * 0.25 * (i * 2 + 1) : 0, 0, 0);
                select_base_grid.Tag = i * 2 + 1;
                grid_show_thumbnails.Children.Add(select_base_grid);
                thumbnail_list.Add(select_base_grid);
            }
        }

        private void add_select_areas(int index)
        {
            Grid select_base_grid = thumbnail_list[index];
            select_base_grid.Height = (this.Content as Grid).ActualHeight * 0.25;
            select_base_grid.VerticalAlignment = VerticalAlignment.Top;
            select_base_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            select_base_grid.Margin = new Thickness(0, (this.Content as Grid).ActualHeight * 0.25 * (index) >= 0 ? (this.Content as Grid).ActualHeight * 0.25 * (index) : 0, 0, 0);
            select_base_grid.Tag = index;

            RowDefinition col_7 = new RowDefinition();
            RowDefinition col_3 = new RowDefinition();
            RowDefinition col_1 = new RowDefinition();
            col_7.Height = new GridLength(7, GridUnitType.Star);
            col_3.Height = new GridLength(3, GridUnitType.Star);
            col_1.Height = new GridLength(1, GridUnitType.Star);

            select_base_grid.RowDefinitions.Add(col_7);
            select_base_grid.RowDefinitions.Add(col_3);
            select_base_grid.RowDefinitions.Add(col_1);

            System.Windows.Controls.Image show_image = new System.Windows.Controls.Image();
            string ori_path = dir_list[index / 2] + "/dicom";
            string res_path = dir_list[index / 2] + "/heat_dicom";

            string[] file_list = Directory.GetFiles(ori_path, "*.dcm");
            DicomImage ori_image = new DicomImage(file_list[0]);
            var img_for_show = ori_image.RenderImage().As<Bitmap>();
            show_image.Source = modules.modules.Convert(img_for_show); // Bitmap을 WPF Image의 Source를 지원하는 형식으로 변형
            show_image.Margin = new Thickness(3);

            Border image_border = new Border();
            image_border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 60, 93, 128));
            image_border.BorderThickness = new Thickness(1.5);
            image_border.CornerRadius = new CornerRadius(10);
            image_border.Child = show_image;
            image_border.VerticalAlignment = VerticalAlignment.Stretch;
            image_border.HorizontalAlignment = HorizontalAlignment.Stretch;
            image_border.SetValue(Grid.RowProperty, 0);
            select_base_grid.Children.Add(image_border);
            string study_description = ori_image.Dataset.GetString(DicomTag.StudyDescription).ToString();
            string series_description = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            string series_num = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            string sop_instance_uid = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            string count = (Directory.GetFiles(res_path).Length + 1).ToString();

            Grid label_grid = new Grid();
            label_grid.SetValue(Grid.RowProperty, 1);
            label_grid.SetValue(Grid.RowSpanProperty, 1);

            label_grid.RowDefinitions.Add(new RowDefinition());
            label_grid.RowDefinitions.Add(new RowDefinition());

            Label descript_label = new Label();
            if (study_description != "")
            {
                series_description.Replace(study_description, "");
                descript_label.Content = study_description + series_description;
            }
            else
                descript_label.Content = "UNKNOWN";
            descript_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            descript_label.FontSize = 15;
            descript_label.SetValue(Grid.RowProperty, 0);
            label_grid.Children.Add(descript_label);

            Grid semi_label_grid = new Grid();
            semi_label_grid.SetValue(Grid.RowProperty, 1);
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());

            Label series_label = new Label();
            series_label.Content = "S: " + series_num;
            series_label.SetValue(Grid.ColumnProperty, 0);
            series_label.FontSize = 12;
            series_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            Label uid_label = new Label();
            uid_label.Content = "I: " + sop_instance_uid;
            uid_label.SetValue(Grid.ColumnProperty, 1);
            uid_label.FontSize = 12;
            uid_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            Label count_label = new Label();
            count_label.Content = count;
            count_label.SetValue(Grid.ColumnProperty, 2);
            count_label.FontSize = 12;
            count_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            semi_label_grid.Children.Add(series_label);
            semi_label_grid.Children.Add(uid_label);
            semi_label_grid.Children.Add(count_label);
            label_grid.Children.Add(semi_label_grid);
            label_grid.Margin = new Thickness(10, 0, 0, 0);
            select_base_grid.Children.Add(label_grid);
            
            select_base_grid = thumbnail_list[index + 1];
            select_base_grid.Height = (this.Content as Grid).ActualHeight * 0.25;
            select_base_grid.VerticalAlignment = VerticalAlignment.Top;
            select_base_grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            select_base_grid.Margin = new Thickness(0, (this.Content as Grid).ActualHeight * 0.25 * (index + 1) >= 0 ? (this.Content as Grid).ActualHeight * 0.25 * (index + 1) : 0, 0, 0);
            select_base_grid.Tag = index + 1;

            col_7 = new RowDefinition();
            col_3 = new RowDefinition();
            col_1 = new RowDefinition();
            col_7.Height = new GridLength(7, GridUnitType.Star);
            col_3.Height = new GridLength(3, GridUnitType.Star);
            col_1.Height = new GridLength(1, GridUnitType.Star);

            select_base_grid.RowDefinitions.Add(col_7);
            select_base_grid.RowDefinitions.Add(col_3);
            select_base_grid.RowDefinitions.Add(col_1);



            show_image = new System.Windows.Controls.Image();
            ori_path = dir_list[index / 2] + "/heat_dicom";
            res_path = dir_list[index / 2] + "/heatmap";

            ori_image = new DicomImage(file_list[file_list.Length - 1]);
            if (Directory.Exists(ori_path + "/99.dcm"))
                ori_image = new DicomImage(ori_path + "/99.dcm");
            img_for_show = ori_image.RenderImage().As<Bitmap>();
            show_image.Source = modules.modules.Convert(img_for_show); // Bitmap을 WPF Image의 Source를 지원하는 형식으로 변형
            show_image.Margin = new Thickness(3);

            image_border = new Border();
            image_border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 60, 93, 128));
            image_border.BorderThickness = new Thickness(1.5);
            image_border.CornerRadius = new CornerRadius(10);
            image_border.Child = show_image;
            image_border.VerticalAlignment = VerticalAlignment.Stretch;
            image_border.HorizontalAlignment = HorizontalAlignment.Stretch;
            image_border.SetValue(Grid.RowProperty, 0);
            select_base_grid.Children.Add(image_border);
            study_description = ori_image.Dataset.GetString(DicomTag.StudyDescription).ToString();
            series_description = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            series_num = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            sop_instance_uid = ori_image.Dataset.GetString(DicomTag.SeriesDescription).ToString();
            count = (Directory.GetFiles(res_path).Length + 1).ToString();

            label_grid = new Grid();
            label_grid.SetValue(Grid.RowProperty, 1);
            label_grid.SetValue(Grid.RowSpanProperty, 1);

            label_grid.RowDefinitions.Add(new RowDefinition());
            label_grid.RowDefinitions.Add(new RowDefinition());

            descript_label = new Label();
            if (study_description != "")
            {
                series_description.Replace(study_description, "");
                descript_label.Content = study_description + series_description;
            }
            else
                descript_label.Content = "UNKNOWN";
            descript_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            descript_label.FontSize = 15;
            descript_label.SetValue(Grid.RowProperty, 0);
            label_grid.Children.Add(descript_label);

            semi_label_grid = new Grid();
            semi_label_grid.SetValue(Grid.RowProperty, 1);
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());
            semi_label_grid.ColumnDefinitions.Add(new ColumnDefinition());

            series_label = new Label();
            series_label.Content = "S: " + series_num;
            series_label.SetValue(Grid.ColumnProperty, 0);
            series_label.FontSize = 12;
            series_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            uid_label = new Label();
            uid_label.Content = "I: " + sop_instance_uid;
            uid_label.SetValue(Grid.ColumnProperty, 1);
            uid_label.FontSize = 12;
            uid_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            count_label = new Label();
            count_label.Content = count;
            count_label.SetValue(Grid.ColumnProperty, 2);
            count_label.FontSize = 12;
            count_label.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));

            semi_label_grid.Children.Add(series_label);
            semi_label_grid.Children.Add(uid_label);
            semi_label_grid.Children.Add(count_label);
            label_grid.Children.Add(semi_label_grid);
            label_grid.Margin = new Thickness(10, 0, 0, 0);
            select_base_grid.Children.Add(label_grid);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dir_list.Length; i++)
            {
                add_select_areas(i * 2);
            }
        }

        public List<Grid> get_thumbnails()
        {
            return thumbnail_list;
        }
    }
}
