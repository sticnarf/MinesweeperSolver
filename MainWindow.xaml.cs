using System;
using System.Threading;
using System.Windows;

namespace MinesweeperSolver
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread workingThread;
        Thread cursorThread;

        public MainWindow()
        {
            InitializeComponent();
            cursorThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                for (;;)
                {
                    var cur_pos = System.Windows.Forms.Cursor.Position;
                    labelCursorX.Dispatcher.Invoke(new Action(delegate
                    {
                        labelCursorX.Content = cur_pos.X.ToString();
                    }));
                    labelCursorY.Dispatcher.Invoke(new Action(delegate
                    {
                        labelCursorY.Content = cur_pos.Y.ToString();
                    }));
                    Thread.Sleep(15);
                }
            });
            cursorThread.Start();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Violent stop
            workingThread?.Abort();
            cursorThread?.Abort();
            Thread.Sleep(100);
            base.OnClosing(e);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            int x1 = int.Parse(textBox_X1.Text);
            int y1 = int.Parse(textBox_Y1.Text);
            int x2 = int.Parse(textBox_X2.Text);
            int y2 = int.Parse(textBox_Y2.Text);
            int row = int.Parse(textBox_Row.Text);
            int col = int.Parse(textBox_Col.Text);
            int mine = int.Parse(textBox_Mine.Text);
            workingThread = new Thread(() =>
            {
                for (;;)
                {
                    var controller = new Controller(x1, y1, x2, y2, row, col, mine);
                    controller.Start();
                    while (!controller.IsFinished())
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
            workingThread.Start();
        }
    }
}
