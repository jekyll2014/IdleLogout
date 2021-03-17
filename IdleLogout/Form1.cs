using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IdleLogout
{
    public partial class Form1 : Form
    {
        private const string ConfigFileName = "config.ini";
        private const string EnableConfigItem = "Enable";
        private const string ForcedLogOffConfigItem = "ForcedLogOff";
        private const string IdleDelayConfigItem = "IdleDelay";

        private int _idleTime = 60;
        private uint _lastIdleTime;
        private bool _forcedLogOff;
        private readonly Timer _systemTimer = new Timer();
        private int _cursorMoveDelay;
        private readonly Random _rnd = new Random();

        public Form1()
        {
            InitializeComponent();
            MaximizeBox = false;
            var autoEnable = false;
            try
            {
                var configLines = File.ReadAllLines(ConfigFileName);
                foreach (var line in configLines)
                {
                    var l = line.Split('=');
                    if (l.Length < 2) continue;

                    l[0] = l[0].Trim().ToUpper();
                    if (l[0] == EnableConfigItem.ToUpper())
                    {
                        int.TryParse(l[1], out var value);
                        autoEnable = value > 0;
                    }
                    else if (l[0] == ForcedLogOffConfigItem.ToUpper())
                    {
                        int.TryParse(l[1], out var value);
                        _forcedLogOff = value > 0;
                    }
                    else if (l[0] == IdleDelayConfigItem.ToUpper())
                    {
                        int.TryParse(l[1], out _idleTime);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Can't load settings: " + exception);
            }

            textBox_time.Text = _idleTime.ToString();
            _cursorMoveDelay = _rnd.Next(0, _idleTime);
            _systemTimer.Enabled = false;
            _systemTimer.Interval = 1000;
            _systemTimer.Tick += TimerEvent;

            checkBox_forcedLogoff.Checked = _forcedLogOff;
            checkBox_enable.Checked = autoEnable;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (checkBox_enable.Checked)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var configText = "";
            configText += EnableConfigItem + "=" + (_systemTimer.Enabled == true ? 1 : 0) + Environment.NewLine;
            configText += ForcedLogOffConfigItem + "=" + (_forcedLogOff == true ? 1 : 0) + Environment.NewLine;
            configText += IdleDelayConfigItem + "=" + _idleTime + Environment.NewLine;
            try
            {
                File.WriteAllText(ConfigFileName, configText);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Can't save settings: " + exception);
            }
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void CheckBox_enable_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_enable.Checked)
            {
                // start TimerEvent
                _systemTimer.Enabled = true;

                // open form window
                textBox_time.Enabled = false;
            }
            else
            {
                // stop timer
                _systemTimer.Enabled = false;

                // minimize form
                textBox_time.Enabled = true;
                notifyIcon1.Text = "Inactive";
            }
        }

        private void CheckBox_forcedLogoff_CheckedChanged(object sender, EventArgs e)
        {
            _forcedLogOff = checkBox_forcedLogoff.Checked;
        }

        private void TextBox_time_Leave(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox_time.Text, out _idleTime)) textBox_time.Text = _idleTime.ToString();

            _cursorMoveDelay = _idleTime;
        }

        private void TimerEvent(object sender, EventArgs args)
        {
            var currentIdleTime = GetIdleTime();

            if (currentIdleTime < _lastIdleTime)
            {
                _cursorMoveDelay = _idleTime;
                _lastIdleTime = currentIdleTime;
            }

            if (currentIdleTime - _lastIdleTime > _cursorMoveDelay * 1000)
            {
                WindowsLogOff();
            }
        }

        private static uint GetIdleTime()
        {
            var lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return (uint)Environment.TickCount - lastInPut.dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)] public uint cbSize;
            [MarshalAs(UnmanagedType.U4)] public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        public bool WindowsLogOff()
        {
            if (_forcedLogOff) return ExitWindowsEx(0 | 0x00000004, 0); // forced log-off
            else return ExitWindowsEx(0, 0);
        }
    }
}
