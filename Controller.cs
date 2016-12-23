using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MinesweeperSolver
{
    class Controller
    {
        private int x1;
        private int y1;
        private int x2;
        private int y2;
        private int row;
        private int col;
        private int mine;
        private int width;
        private int height;
        private Bitmap faceScreenshot;
        private bool stopFlag;
        private Thread t;
        private Solver solver;

        /*
             * 0: 192, 192, 192
             * 1: 0, 0, 255
             * 2: 0, 128, 0
             * 3: 255, 0, 0
             * 4: 0, 0, 128
             * 5: 128, 0, 0
             * 6: 0, 128, 128
             * 7: 0, 0, 0
             * 8: 128, 128, 128
             */
        private static Dictionary<int, int> colorMapping = new Dictionary<int, int>()
        {
            { Color.Blue.ToArgb(), 1 }, { Color.Green.ToArgb(), 2 }, { Color.Red.ToArgb(),3 }, { Color.Navy.ToArgb(),4 },
            { Color.Maroon.ToArgb(), 5 }, { Color.Teal.ToArgb(), 6 }, { Color.Black.ToArgb(), 7 }, { Color.Gray.ToArgb(), 8 }
        };

        public Controller(int x1, int y1, int x2, int y2, int row, int col, int mine)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.row = row;
            this.col = col;
            this.mine = mine;
            stopFlag = false;
            width = x2 - x1;
            height = y2 - y1;
            solver = new Solver(row, col, mine);
        }

        public void Start()
        {
            t = new Thread(WorkingMethod);
            t.Start();
        }

        public bool IsFinished()
        {
            return !t.IsAlive;
        }

        Point CenterPosition(int x, int y)
        {
            return new Point((int)(0.5 + (y + 0.5) * width / col), (int)(0.5 + (x + 0.5) * height / row));
        }

        Point LeftEdgePosition(int x, int y)
        {
            return new Point((int)((double)y * width / col), (int)(0.5 + (x + 0.5) * height / row));
        }

        void WorkingMethod()
        {
            LeftMouseClick((x1 + x2) / 2, y1 - 25);
            Thread.Sleep(500);
            faceScreenshot = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            Graphics.FromImage(faceScreenshot).CopyFromScreen((x1 + x2) / 2 - 8, y1 - 30, 0, 0, new Size(16, 16), CopyPixelOperation.SourceCopy);
            while (!solver.isSolved() && (!stopFlag))
            {
                solver.Search();
                foreach (Tuple<int, int> pos in solver.GetFlaggingPoints())
                {
                    RightClickBlock(pos.Item1, pos.Item2);
                }
                foreach (Tuple<int, int> pos in solver.GetDiggingPoints())
                {
                    LeftClickBlock(pos.Item1, pos.Item2);
                }
                UpdateBoard();
            }
            if (solver.isSolved())
            {
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        if (solver.GetBlock(i, j) == -2)
                        {
                            LeftClickBlock(i, j);
                        }
                    }
                }
            }
        }

        // may be slow
        private static bool Equals(Bitmap bmp1, Bitmap bmp2)
        {
            if (!bmp1.Size.Equals(bmp2.Size))
            {
                return false;
            }
            for (int x = 0; x < bmp1.Width; ++x)
            {
                for (int y = 0; y < bmp1.Height; ++y)
                {
                    if (bmp1.GetPixel(x, y) != bmp2.GetPixel(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        void UpdateBoard()
        {
            Thread.Sleep(100);
            var faceScreenshot = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            Graphics.FromImage(faceScreenshot).CopyFromScreen((x1 + x2) / 2 - 8, y1 - 30, 0, 0, new Size(16, 16), CopyPixelOperation.SourceCopy);
            if (this.faceScreenshot != null && !Equals(faceScreenshot, this.faceScreenshot))
            {
                stopFlag = true;
                return;
            }
            var boardScreenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics.FromImage(boardScreenshot).CopyFromScreen(x1, y1, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            for (int x = 0; x < row; x++)
            {
                for (int y = 0; y < col; y++)
                {
                    if (solver.GetBlock(x, y) != -2) continue;
                    Point leftEdge = LeftEdgePosition(x, y);
                    Point rightEdge = LeftEdgePosition(x, y + 1);
                    bool set = false;
                    int xpos = leftEdge.X;
                    for (int i = 0; i < 3; i++, xpos++)
                    {
                        int c = boardScreenshot.GetPixel(xpos, leftEdge.Y).ToArgb();
                        if (c == Color.White.ToArgb())
                        {
                            set = true;
                            break;
                        }
                    }
                    for (; (!set) && xpos < rightEdge.X - 2; xpos++)
                    {
                        int c = boardScreenshot.GetPixel(xpos, leftEdge.Y).ToArgb();
                        if (colorMapping.ContainsKey(c))
                        {
                            solver.SetBlock(x, y, colorMapping[c]);
                            set = true;
                        }
                    }
                    if (!set)
                    {
                        solver.SetBlock(x, y, 0);
                    }
                }
            }
        }

        void LeftClickBlock(int x, int y)
        {
            Point p = CenterPosition(x, y);
            LeftMouseClick(p.X + x1, p.Y + y1);
        }

        void RightClickBlock(int x, int y)
        {
            Point p = CenterPosition(x, y);
            RightMouseClick(p.X + x1, p.Y + y1);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;

        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(200);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public static void RightMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(200);
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, xpos, ypos, 0, 0);
        }
    }
}
