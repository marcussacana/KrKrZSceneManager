using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KrKrSceneManager;

namespace ScnEditorGUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MessageBox.Show("This GUI don't is a stable translation tool, this program is a Demo for my dll, the \"KrKrSceneManager.dll\" it's a opensoruce project to allow you make your program to edit any scn file, with sig PSB or MDF.\n\nHow to use:\n*Rigth Click in the window to open or save the file\n*Select the string in listbox and edit in the text box\n*Press enter to update the string\n\nThis program is unstable!");
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "";
            fd.Filter = "All KiriKiri SCN Files | *.scn";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK)
                OpenFile(fd.FileName);
        }
        //public SCENE SCN = new SCENE(); //Correct usage
        public object SCN = new object(); //I use this for tests 
        private void OpenFile(string fname)
        {
            listBox1.Items.Clear();
            SCENE scn = (new SCENE()).import(System.IO.File.ReadAllBytes(fname));
            SCN = scn;
            foreach (string str in ((SCENE)SCN).Strings)
            {
                listBox1.Items.Add(str);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                this.Text = "id: " + listBox1.SelectedIndex;
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
            catch { }
        }
        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = "";
            save.Filter = "All KiriKiri SCN Files | *.scn";
            DialogResult dr = save.ShowDialog();
            if (dr == DialogResult.OK)
            {
                for (int i = 0; i < ((SCENE)SCN).Strings.Length; i++)
                {
                    ((SCENE)SCN).Strings[i] = listBox1.Items[i].ToString();
                }
                dr = MessageBox.Show("Would you like to compress the script? (Recommended)\n\nDoes not work with old games.", "ScnEditorGUI", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                ((SCENE)SCN).CompressScene = dr == DialogResult.Yes;
                byte[] outfile = ((SCENE)SCN).export();
                System.IO.File.WriteAllBytes(save.FileName, outfile);
                MessageBox.Show("File Saved.");
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' || e.KeyChar == '\n')
            {
                listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
            }
        }
    }
}
