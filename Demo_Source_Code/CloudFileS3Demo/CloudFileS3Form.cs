using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

using CloudFile.CommonObjects;
using CloudFile.FilterControl;

namespace CloudFileS3Demo
{
    public partial class CloudFileS3Form : Form
    {

        public CloudFileS3Form()
        {
            InitializeComponent();

            string lastError = string.Empty;
            Utils.CopyOSPlatformDependentFiles(ref lastError);

            StartPosition = FormStartPosition.CenterScreen;

            DisplayVersion();

        }

        ~CloudFileS3Form()
        {
            FilterWorker.StopService();
        }

        private void DisplayVersion()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            try
            {
                string filterDllPath = Path.Combine(GlobalConfig.AssemblyPath, "FilterAPI.Dll");
                version = FileVersionInfo.GetVersionInfo(filterDllPath).ProductVersion;
            }
            catch (Exception ex)
            {
                EventManager.WriteMessage(43, "LoadFilterAPI Dll", EventLevel.Error, "FilterAPI.dll can't be found." + ex.Message);
            }

            this.Text += "    Version:  " + version;
        }



        private void toolStripButton_StartFilter_Click(object sender, EventArgs e)
        {
            string lastError = string.Empty;

            if (!FilterWorker.StartService(FilterWorker.StartType.GuiApp, listView_Info, out lastError))
            {
                MessageBoxHelper.PrepToCenterMessageBoxOnForm(this);
                MessageBox.Show("Start filter failed." + lastError, "StartFilter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            toolStripButton_StartFilter.Enabled = false;
            toolStripButton_Stop.Enabled = true;

            EventManager.WriteMessage(102, "StartFilter", EventLevel.Information, "Start filter service succeeded.");

        }

        private void toolStripButton_Stop_Click(object sender, EventArgs e)
        {
            FilterWorker.StopService();

            toolStripButton_StartFilter.Enabled = true;
            toolStripButton_Stop.Enabled = false;
        }

        private void toolStripButton_ClearMessage_Click(object sender, EventArgs e)
        {
            listView_Info.Clear();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingForm = new SettingsForm();
            settingForm.StartPosition = FormStartPosition.CenterParent;
            settingForm.ShowDialog();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            EventForm.DisplayEventForm();
        }

       

        private void uninstallDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FilterWorker.StopService();
            FilterAPI.UnInstallDriver();
        }

        private void installDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FilterAPI.InstallDriver();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FilterWorker.StopService();
            Application.Exit();
        }

        private void demoForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            FilterWorker.StopService();
        }
     

        private void toolStripButton_Help_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.easefilter.com/cloud/cloud-file-dr-s3-demo.htm");
        }

        private void toolStripButton_S3Explorer_Click(object sender, EventArgs e)
        {
            string s3explorerPath = GlobalConfig.AssemblyPath + "\\S3Explorer.exe";
            System.Diagnostics.Process.Start(s3explorerPath);
        }
    }
}
