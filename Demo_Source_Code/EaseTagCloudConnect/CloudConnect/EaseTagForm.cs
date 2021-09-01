using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using EaseFilter.GlobalObjects;

namespace CloudConnect
{
    public partial class EaseTagForm : Form
    {

        public EaseTagForm()
        {
            Utils.CopyOSPlatformDependentFiles();
            InitializeComponent();
        }

        private void button_Ok_Click(object sender, EventArgs e)
        {
            string fileName = textBox_Input.Text;

            IntPtr fileHandle = IntPtr.Zero;
            bool ret = EaseTag.OpenStubFile(fileName, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite, ref fileHandle);
            if (!ret)
            {
                MessageBox.Show("Open stub file error with " + EaseTag.GetLastErrorMessage(), "OpenStubfile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            byte[] returnTagData;
            string lastError = string.Empty;
            ret = EaseTag.GetTagData(fileHandle, out returnTagData, out lastError);
            if (!ret)
            {
                MessageBox.Show("GetTagData from stub file error with " + EaseTag.GetLastErrorMessage(), "GetTagData", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string tagStr = Encoding.UTF8.GetString(returnTagData);
            MessageBox.Show("GetTagData:" + tagStr, "GetTagData", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }


        private void button_Browse_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_Input.Text = openFileDialog1.FileName;
            }
        }
    }
}
