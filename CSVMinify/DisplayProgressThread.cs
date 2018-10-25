using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;

namespace CSVMinify
{
    class DisplayProgressThread
    {
        private Thread ProgressThread { get; set; }
        private TextBox LogTextBox { get; set; }
        private MainWindow MW { get; set; }
        private Dispatcher CurrentDispatcher { get; set; }


        public DisplayProgressThread()
        {
            ProgressThread = null;
            LogTextBox = null;
            CurrentDispatcher = null;
            MW = null;
        }

        public DisplayProgressThread(TextBox logbox, MainWindow mw, Dispatcher disp)
        {
            LogTextBox = logbox;
            MW = mw;
            CurrentDispatcher = disp;
            ProgressThread = null;
        }
        
        public void StartProgressThread()
        {
            if (ProgressThread == null)
            {
                ProgressThread = new Thread(new ThreadStart(Progress));
                ProgressThread.Name = "Progress Thread";
                ProgressThread.IsBackground = true;
                ProgressThread.Start();
            }
            else
            {
                KillProgressThread();
                StartProgressThread();
            }
        }

        public void KillProgressThread()
        {
            try
            {
                ProgressThread.Abort();
                ProgressThread = null;
            }
            catch (ThreadAbortException ex)
            {
                Thread.ResetAbort();
                UpdateLogBox("Progress thread abort exception thrown:\n" + ex.Message);
            }

            UpdateLogBox(""); ;
        }

        private void Progress()
        {
            UpdateLogBox("Working . . . ", true);
            while (true)
            {
                UpdateLogBox(". ", true);
                Thread.Sleep(1000);
            }
        }

        private void UpdateLogBox(string txt)
        {
            CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate()
            {
                LogTextBox.Text += txt + Environment.NewLine;
                LogTextBox.ScrollToEnd();
            }));
        }

        private void UpdateLogBox(string txt, bool inline)
        {
            CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate()
            {
                if (inline)
                    LogTextBox.Text += txt;
                else
                    UpdateLogBox(txt);
                LogTextBox.ScrollToEnd();
            }));
        }
    }
}
