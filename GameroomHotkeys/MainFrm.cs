using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameroomHotkeys
{
    public partial class MainFrm : Form
    {
        // Stuff I totally copied from a Google search, I don't know any of this off the top of my head
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
        public const string PROCESS_NAME = "Telegram"; // Obviously, the Telegram app's process name
        public const string ENTER_KEY = "{ENTER}"; // This is how SendKeys wants to be told to 'press' enter
        public const string ANSWER_A = "**A**"; // The ** makes the message bold in Telegram
        public const string ANSWER_B = "**B**";
        public const string GUESS_A = "**GOK:** A";
        public const string GUESS_B = "**GOK:** B";
        public const string HELP_TITLE = "Gameroom Hotkeys ~ Hulp";
        public const string HELP_MESSAGE = "Wanneer je 1 van de hotkeys gebruikt, zal dit programma jouw Telegram app naar voren halen en automatisch" +
            " het bijbehorende antwoord intypen en versturen.\n\nKan jouw Telegram app niet worden gevonden? Dubbelcheck dan of je de app van de Telegram" +
            " website hebt gedownload. Zo niet? Probeer dat!\n\nLukt het nog steeds niet? Gebruik dan de \"Contact\" optie in dit programma om hulp te krijgen.";
        public const string CREDITS_URL = "https://github.com/put";
        public const string CONTACT_URL = "https://t.me/mikatje";

        // This will be the Telegram process once we found it
        public Process Telegram { get; set; }
        // This will be the stopwatch we use to make sure people don't spam answers
        public Stopwatch Throttler { get; set; }

        // The form's constructor
        public MainFrm()
        {
            InitializeComponent();
            // Make new stopwatch
            Throttler = new Stopwatch();
            // Call the function that will try to find the Telegram process
            FindTelegram();
        }

        private async Task FindTelegram()
        {
            // We use this to keep our loop going for as long as we need
            bool telegramFound = false;

            // A loop that runs until telegramFound is set to true (which would mean the process is found)
            while (!telegramFound)
            {
                // Get a list of all processes running on the computer that match a certain process name (in our case, 'Telegram')
                var processList = Process.GetProcessesByName(PROCESS_NAME);

                // Check if any processes were found (by checking if the list has at least 1 process in it
                if (processList.Length > 0)
                {
                    // Set this to true, so the loop will stop
                    telegramFound = true;
                    // Save the Telegram process for later use, we simply assume it's the first process, if you run multiple processes called 'Telegram' this might cause trouble for you
                    Telegram = processList.First();
                    // Next 4 lines are a bunch of visual stuff, they change texts and colors
                    StatusLbl.Text = FOUND_MSG;
                    StatusLbl.ForeColor = Color.Green;
                    StatusBx.BackColor = Color.Green;
                    Status2Bx.BackColor = Color.Green;

                    // Register all hotkeys, which ones they are is probably fairly obvious. 1,2,3,4 are their identifiers
                    RegisterHotKey(Handle, 1, (int)KeyModifier.None, (int)Keys.F1);
                    RegisterHotKey(Handle, 2, (int)KeyModifier.None, (int)Keys.F2);
                    RegisterHotKey(Handle, 3, (int)KeyModifier.Shift, (int)Keys.F1);
                    RegisterHotKey(Handle, 4, (int)KeyModifier.Shift, (int)Keys.F2);
                }

                // finally, wait a second, this is because we don't want to check if the Telegram process exists thousands of times per second
                await Task.Delay(1000);
            }
        }

        // This is the function that does everything required to send an answer once a certain hotkey was pressed
        public void GiveAnswer(string message)
        {
            // Bring the Telegram process to the front (focus on it)
            BringToFront(Telegram);
            // Send the correct message + an enter to the currently focused window (which SHOULD be Telegram)
            SendKeys.SendWait(message + ENTER_KEY);
        }

        // This is a function that triggers when a registered hotkey was pressed
        protected override void WndProc(ref Message m)
        {
            // I wish I knew what this was for, this is part of what I copied from that Google search ;-)
            base.WndProc(ref m);

            // Same as above, but I'm slightly more clueless about this one
            if (m.Msg == 0x0312)
            {

                // This is the key that was pressed
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                // This is the key modifier that was pressed (shift, ctrl, etc)
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);
                // This is the identifier we gave to that particular hotkey
                int id = m.WParam.ToInt32();

                // Check if the stopwatch is either not running, or has been running for more than 3 seconds
                // In both cases that means we allow the user to give an answer
                if (!Throttler.IsRunning || Throttler.ElapsedMilliseconds > 3000)
                {
                    // Switch between supported hotkey identifiers
                    // Inside of this, we really just make the correct call to the 'GiveAnswer' function, which does everything required to send a Telegram message
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

                    // Finally, we restart the stopwatch, so it will start at 0 seconds again. This is how we prevent spamming answers
                    Throttler.Restart();
                }
            }
        }

        // This function handles clicks on the 'GitHub' link, it just opens the website, really
        private void CreditsLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) 
            => Process.Start(CREDITS_URL);

        // This function handles clicks on the 'Hulp' link, which displays a little message box containing (hopefully) helpful information
        private void HelpLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => MessageBox.Show(HELP_MESSAGE, HELP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Question);

        // This function handles clicks on the 'Contact' link, it also just opens a website
        private void ContactLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            => Process.Start(CONTACT_URL);

        // This function triggers when the application is closed
        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // When the application closes, we want to unregister all hotkeys again, which is what happens in the next 4 lines
            UnregisterHotKey(Handle, 1);
            UnregisterHotKey(Handle, 2);
            UnregisterHotKey(Handle, 3);
            UnregisterHotKey(Handle, 4);
        }
    }
}
