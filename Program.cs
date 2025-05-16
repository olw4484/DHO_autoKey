// 트레이 아이콘 기반 키 자동 입력 프로그램 (WinForms 기반, keybd_event 기반 입력 방식 + 디버깅 로그 출력)

using System;
using System.Windows.Forms;
using System.Timers;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

class Program
{
    static bool isRunning = false; // 자동 입력 실행 여부 상태 변수

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        NotifyIcon trayIcon = new NotifyIcon();
        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem toggleItem = new ToolStripMenuItem("▶ 시작 (클릭 시 토글)");
        toggleItem.Click += (s, e) =>
        {
            isRunning = !isRunning;
            toggleItem.Text = isRunning ? "⏸ 일시정지 (클릭 시 토글)" : "▶ 시작 (클릭 시 토글)";
            trayIcon.Text = isRunning ? "자동 입력 실행 중" : "자동 입력 일시정지";

            ShowLog(isRunning ? "자동 입력 시작됨" : "자동 입력 정지됨");
        };
        menu.Items.Add(toggleItem);

        ToolStripMenuItem exitItem = new ToolStripMenuItem("종료");
        exitItem.Click += (s, e) => Application.Exit();
        menu.Items.Add(exitItem);

        trayIcon.Icon = SystemIcons.Information;
        trayIcon.Text = "자동 입력 일시정지";
        trayIcon.Visible = true;
        trayIcon.ContextMenuStrip = menu;

        // 타이머 설정
        System.Timers.Timer timer1 = new System.Timers.Timer(1 * 60 * 1000); // 1분
        timer1.Elapsed += (s, e) => { if (isRunning) SendKeyLowLevel(0x34, 8); }; // '4' 키
        timer1.Start();

        System.Timers.Timer timer20 = new System.Timers.Timer(20 * 60 * 1000); // 25분
        timer20.Elapsed += (s, e) => { if (isRunning) SendKeyLowLevel(0x38, 2); }; // '8' 키
        timer20.Start();

        ShowLog("프로그램이 시작되었습니다. 트레이 아이콘을 확인하세요.");
        Application.Run();

        // 종료 시 리소스 정리
        trayIcon.Visible = false;
        timer1.Stop();
        timer20.Stop();
    }

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const uint KEYEVENTF_KEYUP = 0x0002;

    static void SendKeyLowLevel(byte keyCode, int repeat)
    {
        ShowLog($"[SendKeyLowLevel] VK: 0x{keyCode:X2}, 반복: {repeat}회");

        for (int i = 0; i < repeat; i++)
        {
            keybd_event(keyCode, 0, 0, UIntPtr.Zero); // key down
            Thread.Sleep(250);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // key up
            Thread.Sleep(400);
        }
    }

    static void ShowLog(string message)
    {
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
