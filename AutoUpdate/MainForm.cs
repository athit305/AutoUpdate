using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace AutoUpdate
{
    public partial class MainForm : Form
    {
        private string ClientDir;
        private string ServerDir;
        private bool RunbyUser = true;
        private List<string> FileUpdateList = new List<string>();
        private string AppFileName = "OIS.exe";
        private string ConfigFileName = "config.ini";

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (ReadConfig())
            {
            }
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            try
            {
                if (!backgroundWorker1.IsBusy)
                {
                    Thread.Sleep(2000);
                    if (OpenConection())
                    {
                        GetUpdateFileList();
                    }

                    progressBar1.Maximum = FileUpdateList.Count;
                    progressBar1.Step = 1;
                    progressBar1.Value = 0;

                    backgroundWorker1.RunWorkerAsync();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            UpdateFile();
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            //tsTotal.Text = string.Format("{0}/{1}", e.ProgressPercentage, FileUpdateList.Count);
            //tsFileName.Text = FileUpdateList[e.ProgressPercentage - 1];
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RunApp();
        }

        private bool ReadConfig()
        {
            try
            {
                ClientDir = Path.Combine("C:\\", ConfigurationManager.AppSettings["ClientFilePath"]);
                ServerDir = ConfigurationManager.AppSettings["ServerFilePath"];
                if(Environment.MachineName == "ATHIT0U0N15")
                {
                    RunbyUser = false;
                    ServerDir = ConfigurationManager.AppSettings["ServerFilePath2"];
                }

                if(!Directory.Exists(ClientDir))
                {
                    Directory.CreateDirectory(ClientDir);
                }
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return false;
            }
        }
        private bool OpenConection()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return false;
            }
        }
        private bool GetUpdateFileList()
        {
            try
            {
                if(ClientDir == null || ServerDir == null)
                {
                    return false;
                }

                NetworkCredential theNetworkCredential = new NetworkCredential(@"ERTC\Administrator", ConfigurationManager.AppSettings["CredentialPass"].ToString());
                //CredentialCache theNetCache = new CredentialCache();
                //theNetCache.Add(new Uri(@"\\SERVER\\OIS\\AutoUpdate"), "Basic", theNetworkCredential);
                //theNetCache.Add(new Uri(@"\\SERVER\\OIS\\Deploy\OIS"), "Basic", theNetworkCredential);
                //theNetCache.Add(new Uri(ServerDir), "Basic", theNetworkCredential);
                string[] serverFileList;
                if (!RunbyUser)
                {
                    using (new NetworkConnection(ServerDir, theNetworkCredential))
                    {
                        serverFileList = Directory.GetFiles(ServerDir, "*.*", SearchOption.AllDirectories);
                    }
                }
                else
                {
                    serverFileList = Directory.GetFiles(ServerDir, "*.*", SearchOption.AllDirectories);
                }
                var clientFileList = Directory.GetFiles(ClientDir, "*.*", SearchOption.AllDirectories);

                foreach (string serverFile in serverFileList)
                {
                    string sFile = serverFile.Substring(ServerDir.Length + 1);
                    int index = sFile.LastIndexOf("\\");
                    string sFolder = "";
                    if(index >= 0)
                    {
                        sFolder = sFile.Substring(0, index);
                    }

                    string cFile = Path.Combine(ClientDir, sFile);
                    string cFolder = Path.Combine(ClientDir, sFolder);

                    if(sFolder != "" && !Directory.Exists(cFolder))
                    {
                        Directory.CreateDirectory(cFolder);
                    }
                    if (File.Exists(cFile))
                    {
                        FileInfo cFi = new FileInfo(cFile);
                        FileInfo sFi = new FileInfo(serverFile);
                        if(cFi.Length != sFi.Length || cFi.LastWriteTime != sFi.LastWriteTime)
                        {
                            if (sFi.Name != ConfigFileName)
                            {
                                FileUpdateList.Add(sFile);
                            }
                        }
                    }
                    else
                    {
                        FileUpdateList.Add(sFile);
                    }
                }

                foreach (string clientFile in clientFileList)
                {
                    string cFile = clientFile.Substring(ClientDir.Length + 1);
                    bool isFoundOnServer = false;
                    foreach (string serverFile in serverFileList)
                    {
                        string sFile = serverFile.Substring(ServerDir.Length + 1);
                        if(cFile == sFile)
                        {
                            isFoundOnServer = true;
                        }
                    }
                    if(!isFoundOnServer && cFile.Substring(0,10) != "AutoUpdate")
                    {
                        File.Delete(clientFile);
                    }
                }

                tsFileName.Text = string.Empty;
                tsTotal.Text = string.Format("0/{0}", FileUpdateList.Count);

                return true;
            }
             catch (Exception ex)
            {
                throw ex;
            }
        }
        private void UpdateFile()
        {
            try
            {
                if(FileUpdateList.Count > 0)
                {
                    int i = 0;
                    foreach(string FilePath in FileUpdateList)
                    {
                        tsFileName.Text = FileUpdateList[i];
                        string ClientFilePath = Path.Combine(ClientDir, FilePath);
                        string ServerFilePath = Path.Combine(ServerDir, FilePath);
                        File.Copy(ServerFilePath, ClientFilePath, true);
                        backgroundWorker1.ReportProgress(i + 1);
                        tsTotal.Text = string.Format("{0}/{1}", i + 1, FileUpdateList.Count);
                        i++;
                    }
                }

                DeleteDirectory(ClientDir);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }
        private void RunApp()
        {
            try
            {
                string FilePath = Path.Combine(ClientDir, AppFileName);
                if(File.Exists(FilePath))
                {
                    Process.Start(FilePath);
                    this.Close();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }
        private void DeleteDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
