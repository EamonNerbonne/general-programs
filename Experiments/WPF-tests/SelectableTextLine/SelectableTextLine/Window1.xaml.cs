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

namespace SelectableTextLine
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        const string textline = "This is a simple, sample text line you can click on.";
        public Window1() {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            AddRuns();
            clickableTextBlock.MouseLeftButtonDown += new MouseButtonEventHandler(clickableTextBlock_MouseLeftButtonDown);
        }
        Run selectedRun;

        void clickableTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            Run newTarget = e.OriginalSource as Run;
            if (newTarget != null && newTarget.Text !=" ") {
                if (selectedRun != null) {
                    selectedRun.Background = Brushes.Transparent;
                }
                selectedRun = newTarget;
                selectedRun.Background = Brushes.Yellow;

            }
        }

        private void AddRuns() {
                        string[] words = textline.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var q = from word in textline.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    from str in new[] { word, " " }
                    select new Run(str);

            var runs = q.ToArray();


            var inlines = clickableTextBlock.Inlines;
            inlines.Clear();
            foreach (Run run in runs.Take(runs.Length-1)) {
                inlines.Add(run);
            }


        }
    }
}
