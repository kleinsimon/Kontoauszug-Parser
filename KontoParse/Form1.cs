using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        string[] AllowedTypes = { ".tiff", ".jpg", ".png" };
        parser p;
        Color test;

        public Form1()
        {
            InitializeComponent();
            test = Color.White;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string f in files)
            {
                if (File.Exists(f) && AllowedTypes.Contains(Path.GetExtension(f).ToLower()))
                {
                    listBox1.Items.Add(f);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Enabled = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void parseImg(string fn)
        {
            p = new parser(Bitmap.FromFile(fn), this);
            p.DoIt();
        }




        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for ( int i=0; i<listBox1.Items.Count; i++ )
            {
                string fi = (string) listBox1.Items[i];
                parseImg(fi);
                int prc=(int)((float) (i) /(float) listBox1.Items.Count*100);
                
                backgroundWorker1.ReportProgress(prc);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            progressBar1.Update();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listBox1.Enabled = true;
            listBox1.Items.Clear();
            progressBar1.Value = 0;
            progressBar1.Update();
            MessageBox.Show("Parsen abgeschlossen");
            p = null;
        }
    }
}
