// 트레이 아이콘 기반 키 자동 입력 프로그램 (WinForms 기반, 수동 트리거 & 일시정지 기능 추가)

using System;
using System.Windows.Forms;
using System.Timers;
using System.Runtime.InteropServices;
using System.Drawing;
using WindowsInput;
using WindowsInput.Native;

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
        System.Timers.Timer timer4 = new System.Timers.Timer(1 * 60 * 1000); // 1분
        timer4.Elapsed += (s, e) => { if (isRunning) SendKey(VirtualKeyCode.VK_4, 8); };
        timer4.Start();

        System.Timers.Timer timer30 = new System.Timers.Timer(25 * 60 * 1000); // 25분
        timer30.Elapsed += (s, e) => { if (isRunning) SendKey(VirtualKeyCode.VK_8, 2); };
        timer30.Start();

        Application.Run();

        // 종료 시 리소스 정리
        trayIcon.Visible = false;
        timer4.Stop();
        timer30.Stop();
    }

    static void SendKey(VirtualKeyCode key, int repeat)
    {
        var sim = new InputSimulator();
        for (int i = 0; i < repeat; i++)
        {
            sim.Keyboard.KeyPress(key);
            System.Threading.Thread.Sleep(200);
        }
    }
}
