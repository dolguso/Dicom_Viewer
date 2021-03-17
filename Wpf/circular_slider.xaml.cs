using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Wpf
{
    /// <summary>
    /// Window1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class circular_slider : Window
    {
        public circular_slider()
        {
            InitializeComponent();
        }

        private void roundSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is Canvas)
            {
                Canvas now_clicked = (Canvas)sender;
                Canvas parent = (Canvas)now_clicked.Parent;
                DependencyObject temp = now_clicked.Children[3];
                ((Label)temp).Width = e.GetPosition(parent).X;
            }
            else if(sender is Label)
            {
                Label now = (Label)sender;
                Canvas now_clicked = ((Canvas)((Canvas)now.Parent).Children[3]);
                Canvas parent = (Canvas)now_clicked.Parent;
                DependencyObject temp = now_clicked.Children[3];
                ((Label)temp).Width = e.GetPosition(parent).X;
            }
        }

        private void roundSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(sender is Canvas)
            {
                Canvas now_clicked = (Canvas)sender;
                DependencyObject temp = now_clicked.Children[3];
                ((Label)temp).Width = 0;
            }
            else if(sender is Label)
            {
                Label now = (Label)sender;
                Canvas now_clicked = ((Canvas)((Canvas)now.Parent).Children[3]);
                DependencyObject temp = now_clicked.Children[3];
                ((Label)temp).Width = 0;
            }
        }

        private void roundSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Canvas now_clicked = null;
                if (sender is Canvas)
                {
                    now_clicked = (Canvas)sender;
                }
                else if(sender is Label)
                {
                    Label now = (Label)sender;
                    now_clicked = ((Canvas)((Canvas)now.Parent).Children[3]);
                }
                Canvas parent = (Canvas)now_clicked.Parent;
                double start_x = 0;
                double start_rotate = 0;
                DependencyObject temp = null;
                temp = now_clicked.Children[3];
                start_x = ((Label)temp).Width;
                start_rotate = Convert.ToDouble(((Label)temp).Content);

                double add = 0;
                add = (e.GetPosition(parent).X - start_x) / 75 * 240;
                double result = start_rotate + add;
                if (result > 240)
                    result = 240;
                else if (result < 0)
                    result = 0;
                Console.WriteLine(result);
                now_clicked.RenderTransform = new RotateTransform(result);
                ((Label)temp).Content = result;
                ((Label)temp).Width = e.GetPosition(parent).X;
                ((Label)parent.Children[2]).Content = Convert.ToInt32(result / 240 * 100) + "%";
                //((Label)parent.Children[2]).RenderTransform = new RotateTransform(-1 * result);
            }
        }
    }
}
