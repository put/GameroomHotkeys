using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameroomHotkeys
{
    public partial class MainFrm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void BringToFront(Process pTemp)
        {
            SetForegroundWindow(pTemp.MainWindowHandle);
        }
        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public const string FOUND_MSG = "Status: Klaar voor gebruik!";
        public const string PROCESS_NAME = "Telegram";
        public const string ENTER_KEY = "{ENTER}";
        public const string ANSWER_A = "**A**";
        public const string ANSWER_B = "**B**";
        public const string GUESS_A = "**GOK:** A";
        public const string GUESS_B = "**GOK:** B";
        public const string HELP_TITLE = "Gameroom Hotkeys ~ Hulp";
        public const string HELP_MESSAGE = "Wanneer je 1 van de hotkeys gebruikt, zal dit programma jouw Telegram app naar voren halen en automatisch" +
            " het bijbehorende antwoord intypen en versturen.\n\nKan jouw Telegram app niet worden gevonden? Dubbelcheck dan of je de app van de Telegram" +
            " website hebt gedownload. Zo niet? Probeer dat!\n\nLukt het nog steeds niet? Gebruik dan de \"Contact\" optie in dit programma om hulp te krijgen.";
        public const string CREDITS_URL = "https://github.com/put";
        public const string CONTACT_URL = "https://t.me/mikatje";

        public Process Telegram { get; set; }
        public bool HotkeysSet { get; set; }
        public Stopwatch Throttler { get; set; }

        public MainFrm()
        {
            InitializeComponent();
            HotkeysSet = false;
            Throttler = new Stopwatch();
            FindTelegram();
        }

        private async Task FindTelegram()
        {
            bool telegramFound = false;

            while (!telegramFound)
            {
                var processList = Process.GetProcessesByName(PROCESS_NAME);

                if (processList.Length > 0)
                {
                    telegramFound = true;
                    Telegram = processList.First();
                    StatusLbl.Text = FOUND_MSG;
                    StatusLbl.ForeColor = Color.Green;
                    StatusBx.BackColor = Color.Green;
                    Status2Bx.BackColor = Color.Green;

                    RegisterHotKey(Handle, 1, (int)KeyModifier.None, (int)Keys.F1);
                    RegisterHotKey(Handle, 2, (int)KeyModifier.None, (int)Keys.F2);
                    RegisterHotKey(Handle, 3, (int)KeyModifier.Shift, (int)Keys.F1);
                    RegisterHotKey(Handle, 4, (int)KeyModifier.Shift, (int)Keys.F2);

                    HotkeysSet = true;
                }

                await Task.Delay(1000);
            }
        }

        public void GiveAnswer(string message)
        {
            BringToFront(Telegram);
            SendKeys.SendWait(message + ENTER_KEY);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {

                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);
                int id = m.WParam.ToInt32();

                if (!Throttler.IsRunning || Throttler.ElapsedMilliseconds > 3000)
                {
                    switch (id)
                    {
                        case 1:
                            GiveAnswer(ANSWER_A);
                            break;
                        case 2:
                            GiveAnswer(ANSWER_B);
                            break;
                        case 3:
                            GiveAnswer(GUESS_A);
                            break;
                        case 4:
                            GiveAnswer(GUESS_B);
                            break;
                    }

                    Throttler.Restart();
                }
            }
        }

        private void CreditsLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) 
            => Process.Start(CREDITS_URL);

        private void HelpLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => MessageBox.Show(HELP_MESSAGE, HELP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Question);

        private void ContactLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start(CONTACT_URL);

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(Handle, 1);
            UnregisterHotKey(Handle, 2);
            UnregisterHotKey(Handle, 3);
            UnregisterHotKey(Handle, 4);
        }
    }
}
