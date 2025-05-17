using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoFlagTrainer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        const int KEYEVENTF_KEYUP = 0x0002;

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private MenuItem toggleItem;
        private DateTime lastEnergyUse;
        private Random rand = new Random();
        private bool isRunning = false;

        public MainForm()
        {
            trayMenu = new ContextMenu();

            toggleItem = new MenuItem("▶ 시작", (s, e) => ToggleRunning());
            trayMenu.MenuItems.Add(toggleItem);
            trayMenu.MenuItems.Add("종료", OnExit);

            trayIcon = new NotifyIcon()
            {
                Text = "AutoFlagTrainer",
                Icon = SystemIcons.Application,
                ContextMenu = trayMenu,
                Visible = true
            };

            lastEnergyUse = DateTime.Now;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            StartLoop();
        }

        private async void StartLoop()
        {
            while (true)
            {
                if (isRunning)
                {
                    if (IsWhiteFlag())
                    {
                        PressRandomKey(0x33, 0x34); // 3 or 4
                        await Task.Delay(2000);
                        PressRandomKey(0x31, 0x32); // 1 or 2
                    }

                    if ((DateTime.Now - lastEnergyUse).TotalSeconds > 240)
                    {
                        PressRandomKey(0x37, 0x38); // 7 or 8
                        lastEnergyUse = DateTime.Now;
                    }
                }

                await Task.Delay(500);
            }
        }

        private void ToggleRunning()
        {
            isRunning = !isRunning;
            toggleItem.Text = isRunning ? "■ 정지" : "▶ 시작";
            trayIcon.Text = isRunning ? "AutoFlagTrainer - 실행 중" : "AutoFlagTrainer - 정지됨";
        }

        private void PressRandomKey(byte key1, byte key2)
        {
            byte key = rand.Next(2) == 0 ? key1 : key2;
            keybd_event(key, 0, 0, 0);
            Task.Delay(50).Wait();
            keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
        }

        private bool IsWhiteFlag()
        {
            Rectangle region = new Rectangle(940, 480, 40, 40);
            using (Bitmap screen = new Bitmap(region.Width, region.Height))
            using (Graphics g = Graphics.FromImage(screen))
            {
                g.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
                return IsWhiteFlagFromImage(screen);
            }
        }

        private bool IsWhiteFlagFromImage(Bitmap bmp)
        {
            List<Color> pixels = new List<Color>();
            int cx = bmp.Width / 2, cy = bmp.Height / 2;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    pixels.Add(bmp.GetPixel(cx + dx, cy + dy));
                }
            }

            double avgR = pixels.Average(c => c.R);
            double avgG = pixels.Average(c => c.G);
            double avgB = pixels.Average(c => c.B);

            double distToWhite = ColorDistance(avgR, avgG, avgB, 240, 240, 240);
            double distToBlue = ColorDistance(avgR, avgG, avgB, 60, 90, 180);

            return distToWhite < distToBlue && distToWhite < 60;
        }

        private double ColorDistance(double r1, double g1, double b1, double r2, double g2, double b2)
        {
            return Math.Sqrt(Math.Pow(r1 - r2, 2) + Math.Pow(g1 - g2, 2) + Math.Pow(b1 - b2, 2));
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
