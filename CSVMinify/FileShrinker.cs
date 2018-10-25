using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

namespace CSVMinify
{
    public class FileShrinker
    {
        #region Properties
        private TextBox _tbox = null;
        private MainWindow _mw = null;
        private DisplayProgressThread _dpt = null;
        private string _pathToShrink = null;
        private string _fileName = null;
        private string _fileDirectory = null;
        private string _newFilePath = null;
        private int _numLinesToShow = -1;
        private bool _allRecords = false;

        public MainWindow MW
        {
            get
            {
                return _mw;
            }
            set
            {
                _mw = value;
            }
        }

        public TextBox TBox
        {
            get
            {
                return _tbox;
            }
            set
            {
                _tbox = value;
            }
        }

        public string PathToShrink
        {
            get
            {
                return _pathToShrink;    
            }
            set
            {
                _pathToShrink = value;
            }
        }

        public string FileName
        {
            get
            {
                return _fileName;
            }
            private set
            {
                _fileName = value;
            }
        }

        public string FileDirectory
        {
            get
            {
                return _fileDirectory;
            }
            private set
            {
                _fileDirectory = value;
            }
        }

        public string NewFilePath
        {
            get
            {
                return _newFilePath;
            }
            private set
            {
                _newFilePath = value;
            }
        }

        public int NumLinesToShow
        {
            get
            {
                return _numLinesToShow;
            }
            set
            {

                _numLinesToShow = value;
            }
        }

        public bool AllRecords
        {
            get
            {
                return _allRecords;
            }
            set
            {
                _allRecords = value;
            }
        }
        #endregion

        #region Constructors
        public FileShrinker()
        {
            _pathToShrink = "";
            _numLinesToShow = -1;
            _mw = MainWindow.CSVWindow;
            _dpt = new DisplayProgressThread(_mw.LogBox, _mw, _mw.MainDispatcher);
        }
        #endregion

        #region Methods
        public void CreateNewFile()
        {
            // Remove any extra quotes, happens when the path is copy/pasted from file explorer
            _pathToShrink = _pathToShrink.Replace("\"", string.Empty);
            _mw.UpdateLogBox("Path Entered:" + _pathToShrink + "");

            if (!IsFileInfoValid())
                return;

            if (_allRecords == false)
            {
                ShrinkGivenNumOfLines();
            }
            else
            {
                ShrinkAll();
            }

            // Wait for user to close program
            _mw.UpdateLogBox(
                "\n\n--------------------------------------------------------------------------\n" +
                "The program has finished.\nNew Path:\n'" + (_allRecords ? _fileDirectory : _newFilePath)
            );
           _mw.FileShrinker = null;
        }

        private bool IsFileInfoValid()
        {
            
            if (File.Exists(_pathToShrink) == false)
            {
                _mw.UpdateLogBox("Invalid path. Check the path and try again.");
                return false;
            }

            // Get the directory and filename of _pathToShrink
            try
            {
                _fileDirectory = Path.GetDirectoryName(_pathToShrink);
                _fileName = Path.GetFileName(_pathToShrink);
            }
            catch
            {
                _mw.UpdateLogBox("Error in path/filename. Check the path and try again.");
                return false;
            }

            _mw.UpdateLogBox("File info is valid.");

            return true;
        }

        private void ShrinkGivenNumOfLines()
        {
            // Copy the user defined # of lines from old file to array
            _mw.UpdateLogBox("Copying the number of lines specified to be written to new file.");
            StreamReader sr = new StreamReader(_pathToShrink);
            int count = 0;
            string[] lines = new string[_numLinesToShow];
            _dpt.StartProgressThread();
            while (!sr.EndOfStream && count < _numLinesToShow)
            {
                try
                {
                    lines[count] = sr.ReadLine();
                    count++;
                }
                catch
                {
                    _dpt.KillProgressThread();
                    _mw.UpdateLogBox(
                        "An error occured while saving data from old path.\n" +
                        "Try a smaller number of lines to copy.\n" +
                        "Line number it failed on: " + count + "\n"
                    );
                    sr.Close();
                    _mw.FileShrinkThreadStatus = -1;
                    return;
                }
            }
            sr.Close();
            _dpt.KillProgressThread();

            // Copy lines from old file (saved in lines[]) to the new file
            _newFilePath = _fileDirectory + "\\SHRUNK(" + _numLinesToShow + "_Lines)__" + _fileName;
            FileInfo fi = new FileInfo(_newFilePath);
            if (!fi.Exists) // file doesn't exist, create new one
            {
                FileStream newFile = File.Create(_newFilePath);
                newFile.Close();

                // Attempt to write the number of lines to file. May fail if number too high
                _mw.UpdateLogBox("Writing lines to new file.");
                _dpt.StartProgressThread();
                try
                {
                    File.WriteAllLines(_newFilePath, lines);
                    _dpt.KillProgressThread();
                    _mw.UpdateLogBox("Success. File created.");
                }
                catch
                {
                    _dpt.KillProgressThread();
                    _mw.UpdateLogBox(
                        "An error occured while saving data from old path.\n" +
                        "Try a smaller number of lines to copy.\n" +
                        "Line number it failed on: " + count + "\n"
                    );
                    sr.Close();
                    _mw.FileShrinkThreadStatus = -1;
                    return;
                }

            }
            else if (!IsFileLocked(fi)) // file exists, not in use
            {
                // Attempt to write the number of lines to file. May fail if number too high
                _mw.UpdateLogBox("Writing lines to new file.");
                _dpt.StartProgressThread();
                try
                {
                    File.WriteAllLines(_newFilePath, lines);
                    _dpt.KillProgressThread();
                    _mw.UpdateLogBox("Success. File created.");
                }
                catch
                {
                    _dpt.KillProgressThread();
                    _mw.UpdateLogBox(
                        "An error occured while saving data from old path.\n" +
                        "Try a smaller number of lines to copy.\n" +
                        "Line number it failed on: " + count + "\n"
                    );
                    sr.Close();
                    _mw.FileShrinkThreadStatus = -1;
                    return;
                }
            }
            else // file exists, in use or other issue
            {
                _mw.UpdateLogBox("Error writing file. Is the file open by another process?");
            }
        }

        private void ShrinkAll()
        {
            _dpt.StartProgressThread();
            string[] lines = File.ReadAllLines(_pathToShrink);
            _dpt.KillProgressThread();
            string columnHeaders = "line#," + lines[0];
            if(_numLinesToShow < 0)
                _numLinesToShow = 10000;
            int lastIndexForCurrentFile = _numLinesToShow;
            int totalLines = lines.Count() - 1;
            double numOfFiles = Math.Ceiling(Convert.ToDouble(totalLines)/_numLinesToShow);
            int currentLineNumber = 1;
            _mw.UpdateLogBox("Number of lines in original file: " + totalLines);
            _mw.UpdateLogBox("Number of files that will be created: " + numOfFiles);

            _fileDirectory += "\\SHRUNK";
            int folderNum = 0;
            while (Directory.Exists(_fileDirectory + folderNum))
            {
                folderNum++;
            }

            Directory.CreateDirectory(_fileDirectory += folderNum);
            int linecounter = 1;
            for (int i = 1; i <= numOfFiles; i++)
            {
                _newFilePath = _fileDirectory + "\\SHRUNK" + i + "__" + _fileName;
                _mw.UpdateLogBox("Writing file number " + i + "...");
                _dpt.StartProgressThread();
                if (!File.Exists(_newFilePath))
                {
                    using (StreamWriter sw = File.CreateText(_newFilePath))
                    {
                        sw.WriteLine(columnHeaders);
                    }
                }

                using (StreamWriter sw = File.AppendText(_newFilePath))
                {
                    for (int j = currentLineNumber; j < lastIndexForCurrentFile; j++)
                    {
                        if (j >= totalLines) break;
                        lines[j] = linecounter.ToString() + "," + lines[j];
                        sw.WriteLine(lines[j]);
                        currentLineNumber++;
                        linecounter++;
                    }
                    lastIndexForCurrentFile = currentLineNumber + _numLinesToShow;
                }
                _dpt.KillProgressThread();
                _mw.UpdateLogBox(
                    "File number " + i + " written to disk.\n" +
                    "Path name:\n " + _newFilePath
                );
            }
        }

        private void Progress()
        {
            _mw.UpdateLogBox("Working . . . ", true);
            while (true)
            {
                _mw.UpdateLogBox(". ", true);
                Thread.Sleep(1000);
            }
        }

        // Method to check if file is used by another process
        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        #endregion
    }
}
