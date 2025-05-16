// 트레이 아이콘 기반 키 자동 입력 프로그램 (WinForms 기반, SendInput 직접 호출 + 수동 토글 기능)

using System;
using System.Windows.Forms;
using System.Timers;
using System.Runtime.InteropServices;
using System.Drawing;

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
        timer4.Elapsed += (s, e) => { if (isRunning) SendKey(0x34, 8); }; // '4' 키
        timer4.Start();

        System.Timers.Timer timer30 = new System.Timers.Timer(25 * 60 * 1000); // 25분
        timer30.Elapsed += (s, e) => { if (isRunning) SendKey(0x38, 2); }; // '8' 키
        timer30.Start();

        Application.Run();

        // 종료 시 리소스 정리
        trayIcon.Visible = false;
        timer4.Stop();
        timer30.Stop();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    const int INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    static void SendKey(ushort keyCode, int repeat)
    {
        for (int i = 0; i < repeat; i++)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki = new KEYBDINPUT
            {
                wVk = keyCode,
                wScan = 0,
                dwFlags = 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki = new KEYBDINPUT
            {
                wVk = keyCode,
                wScan = 0,
                dwFlags = KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            System.Threading.Thread.Sleep(200);
        }
    }
}
