using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.IO;

namespace CSVMinify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties
        private static Dispatcher _mainDispatcher;
        private static MainWindow _csvWindow;
        private static TextBox _pathFieldTextBox;
        private static TextBox _numOfLinesTextBox;
        private static TextBox _logBoxTextBox;
        private static CheckBox _allRecordsCheckBox;
        private static int _fileShrinkThreadStatus;
        private Thread _fileShrinkThread;
        private FileShrinker _fileShrinker;

        public Dispatcher MainDispatcher
        {
            get
            {
                return _mainDispatcher;
            }
            private set
            {
                _mainDispatcher = value;
            }
        }

        public static MainWindow CSVWindow
        {
            get
            {
                return _csvWindow;
            }
            private set
            {
                _csvWindow = value;
            }
        }

        public Thread FileShrinkThread
        {
            get
            {
                return _fileShrinkThread;
            }
            set
            {
                _fileShrinkThread = value;
            }
        }

        public int FileShrinkThreadStatus
        {
            get
            {
                return _fileShrinkThreadStatus;
            }
            set
            {
                _fileShrinkThreadStatus = value;
            }
        }
        
        public FileShrinker FileShrinker
        {
            get
            {
                return _fileShrinker;
            }
            set
            {
                _fileShrinker = value;
            }
        }
        #endregion

        #region UI Elements
        public MainWindow()
        {
            InitializeComponent();
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            _csvWindow = this;
            _mainDispatcher = this.Dispatcher;
            _pathFieldTextBox = this.PathField;
            _numOfLinesTextBox = this.NumOfLines;
            _logBoxTextBox = this.LogBox;
            _allRecordsCheckBox = this.AllRecordsCheckBox;
            _fileShrinkThreadStatus = 0;
            _fileShrinkThread = null;
            _fileShrinker = null;
        }

        private void AllRecordsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_fileShrinker == null)
                _fileShrinker = new FileShrinker();

            _fileShrinker.AllRecords = true;
            NumOfLines.IsEnabled = false;
            NumOfLines.Text = null;
            NumOfLines.Background = Brushes.LightGray;
            NumOfLines_Subfiles.IsEnabled = true;
            NumOfLines_Subfiles.Text = "";
            NumOfLines_Subfiles.ClearValue(BackgroundProperty);
        }

        private void AllRecordsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_fileShrinker == null)
                _fileShrinker = new FileShrinker();
            _fileShrinker.AllRecords = false;
            NumOfLines.IsEnabled = true;
            NumOfLines.ClearValue(BackgroundProperty);
            NumOfLines_Subfiles.IsEnabled = false;
            NumOfLines_Subfiles.Text = "";
            NumOfLines_Subfiles.Background = Brushes.LightGray;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV files (*.csv)|*.csv";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                PathField.Text = filename;
            }
        }

        private void SubmitPath_Click(object sender, RoutedEventArgs e)
        {
            if (_fileShrinker == null)
            {
                _fileShrinker = new FileShrinker();
                if (AllRecordsCheckBox.IsChecked == true)
                    _fileShrinker.AllRecords = true;
                else
                    _fileShrinker.AllRecords = false;
            }
            //LogBox.Clear();
            //LogBox.Text = "Large CSV Reader Log:\n";
            
            if (PathField.Text == "")
            {
                UpdateLogBox("Make sure the path has been provided and try again.");
                return;
            }
            else if (NumOfLines.Text == "" && AllRecordsCheckBox.IsChecked == false)
            {
                UpdateLogBox("Make sure number of lines have been provided, or choose to split file.");
                return;
            }
            else
            {
                _fileShrinker.PathToShrink = PathField.Text;
                _fileShrinker.TBox = LogBox;
                try
                {
                    if (NumOfLines.Text != "")
                        _fileShrinker.NumLinesToShow = Convert.ToInt32(NumOfLines.Text);
                    else if (NumOfLines_Subfiles.Text != "")
                        _fileShrinker.NumLinesToShow = Convert.ToInt32(NumOfLines_Subfiles.Text);
                }
                catch
                {
                    UpdateLogBox("Error converting number of lines to an integer. Make sure it's a valid integer and try again.");
                }
            }

            if (_fileShrinkThread == null || !_fileShrinkThread.IsAlive)
                CreateFileShrinkThread();

            if (_fileShrinkThread.IsAlive && _fileShrinkThreadStatus < 0)
            {
                UpdateLogBox("FileShrinker() had an error. Attempting to abort thread.");
                try
                {
                    _fileShrinkThread.Abort();
                    _fileShrinkThread = null;
                    _fileShrinkThreadStatus = 0;
                    UpdateLogBox("Thread has been aborted."); 
                }
                catch (ThreadAbortException ex)
                {
                    UpdateLogBox("Error aborting thread. Please restart program.");
                    UpdateLogBox("Exception caught:\n" + ex.Message);
                    Thread.ResetAbort();
                }
            }
        }
        #endregion

        #region UI Helper Methods
        private void CreateFileShrinkThread()
        {
            _fileShrinkThread = new Thread(new ThreadStart(_fileShrinker.CreateNewFile));
            _fileShrinkThread.Name = "File Shrink Thread";
            _fileShrinkThread.IsBackground = true;
            _fileShrinkThread.Start();
        }

        public void UpdateLogBox(string txt)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate()
            {
                LogBox.Text += txt + Environment.NewLine;
                LogBox.ScrollToEnd();
            }));
        }

        public void UpdateLogBox(string txt, bool inline)
        {
            //Dispatcher disp = CSVMinify.App.Current.Dispatcher;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(delegate()
            {
                if (inline)
                    LogBox.Text += txt;
                else
                    UpdateLogBox(txt);
                LogBox.ScrollToEnd();
            }));
        }
        #endregion
    }
}
