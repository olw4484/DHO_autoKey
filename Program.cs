using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

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
        private DateTime lastExtraUse;
        private Random rand = new Random();
        private bool isRunning = false;
        private readonly string logPath = "debug_log.txt";

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
            lastExtraUse = DateTime.Now;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;

            Log("프로그램 시작됨.");
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
                        Log("[감지] 흰 깃발 인식됨");
                        PressRandomKey(0x33, 0x34); // 3 or 4
                        await Task.Delay(2000);
                        PressRandomKey(0x31, 0x32); // 1 or 2
                    }
                    else
                    {
                        Log("[감지] 흰 깃발 없음");
                    }

                    if ((DateTime.Now - lastEnergyUse).TotalSeconds > 240)
                    {
                        Log("[행동력] 7 또는 8 입력됨");
                        PressRandomKey(0x37, 0x38); // 7 or 8
                        lastEnergyUse = DateTime.Now;
                    }

                    if ((DateTime.Now - lastExtraUse).TotalMinutes > 10)
                    {
                        Log("[보조키] 5 또는 6 입력됨");
                        PressRandomKey(0x35, 0x36); // 5 or 6
                        lastExtraUse = DateTime.Now;
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
            Log(isRunning ? "[상태] 자동 실행 시작됨" : "[상태] 자동 실행 중지됨");
        }

        private void PressRandomKey(byte key1, byte key2)
        {
            byte key = rand.Next(2) == 0 ? key1 : key2;
            Log($"[입력] 키 입력: {key} (VK: 0x{key:X2})");
            keybd_event(key, 0, 0, 0);
            Task.Delay(50).Wait();
            keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
        }

        private bool IsWhiteFlag()
        {
            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;

            int left = (int)(screenW * 0.47);  // 약 900
            int top = (int)(screenH * 0.20);   // 약 240
            int width = (int)(screenW * 0.06); // 약 110
            int height = (int)(screenH * 0.35); // 약 420

            Rectangle region = new Rectangle(left, top, width, height);

            using (Bitmap screen = new Bitmap(region.Width, region.Height))
            using (Graphics g = Graphics.FromImage(screen))
            {
                g.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
                return IsWhiteFlagFromImage(screen);
            }
        }

        private bool IsWhiteFlagFromImage(Bitmap bmp)
        {
            try
            {
                Mat screenMat = BitmapConverter.ToMat(bmp);
                if (screenMat.Channels() == 4)
                    Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.BGRA2BGR);
                else if (screenMat.Channels() == 1)
                    Cv2.CvtColor(screenMat, screenMat, ColorConversionCodes.GRAY2BGR);
                string[] templateFiles = {
            "image/white_flag1.png",
            "image/white_flag2.png",
            "image/white_flag3.png",
            "image/white_flag4.png"
        };

                foreach (string path in templateFiles)
                {
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    Mat raw = Cv2.ImRead(fullPath, ImreadModes.Unchanged);
                    if (raw.Empty())
                    {
                        Log($"[오류] 템플릿 로드 실패: {path}");
                        continue;
                    }

                    Mat template = new Mat();
                    if (raw.Channels() == 4)
                        Cv2.CvtColor(raw, template, ColorConversionCodes.BGRA2BGR);
                    else if (raw.Channels() == 1)
                        Cv2.CvtColor(raw, template, ColorConversionCodes.GRAY2BGR);
                    else
                        template = raw.Clone();

                    Mat result = new Mat();
                    Cv2.MatchTemplate(screenMat, template, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
                    Log($"[감지] {path} 유사도 = {maxVal:F4}");

                    if (maxVal > 0.45)
                        return true; // 하나라도 통과하면 감지 성공
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("[예외] 템플릿 감지 오류: " + ex.Message);
                return false;
            }
        }


        private void Log(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(logPath, logLine + Environment.NewLine);
        }

        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Log("프로그램 종료됨.");
            Application.Exit();
        }
    }
}
