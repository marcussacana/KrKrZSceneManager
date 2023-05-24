using System;
using System.Configuration;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KrKrSceneManager;

namespace ScnEditorGUI {
    public partial class Form1 : Form {
        /**
         * https://stackoverflow.com/questions/57124243/winforms-dark-title-bar-on-windows-10#62811758
         */
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled) {
            if (Environment.OSVersion.Version.Build >= 9200) {
                var attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, (int)attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        public Form1() {
            InitializeComponent();
            MessageBox.Show("This GUI isn't a stable translation tool, this program is a demo for my DLL, \"KrKrSceneManager.dll.\" It's an open-soruce project to allow you to make your program for editing any .scn files (with sig PSB or MDF) or TJS2 files (with sig TJS2100)\n\nHow to use:\n*Right-click in the window to open or save a file.\n*Select the string in listbox and edit in the text box\n*Press enter to update the string\n\nThis program is unstable!");

            string maximized = ConfigurationSettings.AppSettings["maximized"];
            string fontSize = ConfigurationSettings.AppSettings["font_size"];
            string darkMode = ConfigurationSettings.AppSettings["dark_mode"];
            string textboxSelectAllWhenClick = ConfigurationSettings.AppSettings["textbox_select_all_when_click"];
            string textboxEditItemWhenCtrlV = ConfigurationSettings.AppSettings["textbox_edit_item_when_ctrl_v"];

            if (maximized == "true") {
                this.WindowState = FormWindowState.Maximized;
            }

            Font font = new Font(
                "Microsoft Sans Serif",
                float.Parse(fontSize),
                FontStyle.Regular,
                GraphicsUnit.Point,
                ((byte)(0))
            );
            this.listBox1.Font = font;
            this.textBox1.Font = font;

            if(darkMode == "true") {
                UseImmersiveDarkMode(this.Handle, true);
                this.BackColor = Color.Black;
                this.listBox1.BackColor = Color.Black;
                this.listBox1.ForeColor = Color.White;
                this.textBox1.BackColor = Color.Black;
                this.textBox1.ForeColor = Color.White;
            }

            if (textboxSelectAllWhenClick == "true") {
                this.textBox1.MouseClick += new MouseEventHandler(this.textBox1_MouseClick);
            }

            if (textboxEditItemWhenCtrlV == "true") {
                this.textBox1.KeyUp += new KeyEventHandler(this.textBox1_KeyUp);
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "";
            fd.Filter = "KiriKiri Compiled Files | *.scn; *.psb|Pack of Resources | *.pimg";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK)
                OpenFile(fd.FileName);
        }
        bool ResourceMode = false;
        PSBResManager PRM = new PSBResManager();
        public PSBAnalyzer SCN;
        private void OpenFile(string fname) {
            if (fname.EndsWith(".pimg")) {
                ResourceMode = true;
                FileEntry[] Rst = PRM.Import(System.IO.File.ReadAllBytes(fname));
                for (int i = 0; i < Rst.Length; i++)
                    System.IO.File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + i + ".res", Rst[i].Data);
                MessageBox.Show("Resources Extracted in the Program Directory...", "Resource Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                ResourceMode = false;
                listBox1.Items.Clear();
                SCN = new PSBAnalyzer(System.IO.File.ReadAllBytes(fname));
                foreach (string str in SCN.Import()) {
                    listBox1.Items.Add(str);
                }
                if (SCN.UnkOpCodes) {
                    MessageBox.Show("Maybe the reoder is wrong... try create a issue");
                }
                if (SCN.HaveEmbedded) {
                    MessageBox.Show("Looks this psb contains a Embedded File, try open as .pimg");
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            this.Text = "id: " + listBox1.SelectedIndex;

            if (listBox1.SelectedIndex != -1) {
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e) {
            this.Text = "id: " + listBox1.SelectedIndex;

            if (listBox1.SelectedIndex != -1) {
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e) {
            if ((e.Modifiers == Keys.Control) && (e.KeyCode == Keys.F)) {
                e.SuppressKeyPress = true;
            }
        }

        private void listBox1_KeyUp(object sender, KeyEventArgs e) {
            if ((e.Modifiers == Keys.Control) && (e.KeyCode == Keys.F)) {
                Find modalFind = new Find(this);
                modalFind.ShowDialog(this);
            }
        }

        public void FindMyString(string searchString) {
            bool findOk = false;

            // Ensure we have a proper string to search for.
            if (!string.IsNullOrEmpty(searchString)) {
                // Find the item in the list and store the index to the item.
                for (int index = 0; index < listBox1.Items.Count; index++) {
                    if (listBox1.Items[index].ToString().IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0) {
                        this.Text = "id: " + index;
                        listBox1.SetSelected(index, true);
                        textBox1.Text = listBox1.Items[index].ToString();
                        findOk = true;
                        break;
                    }
                }
            }

            if(!findOk) {
                MessageBox.Show("The search string did not match any items in the list");
            }
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e) {
            if (ResourceMode) {
                SaveFileDialog save = new SaveFileDialog();
                save.FileName = "";
                save.Filter = "Pack of Resources | *.pimg";
                DialogResult dr = save.ShowDialog();
                if (dr == DialogResult.OK) {
                    FileEntry[] Images = new FileEntry[PRM.EntryCount];
                    for (int i = 0; i < Images.Length; i++) {
                        Images[i] = new FileEntry();
                        Images[i].Data = System.IO.File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + i + ".res");
                    }
                    byte[] result = PRM.Export(Images);
                    System.IO.File.WriteAllBytes(save.FileName, result);
                }
            }
            else {
                SaveFileDialog save = new SaveFileDialog();
                save.FileName = "";
                save.Filter = "KiriKiri Compiled Files | *.scn; *.psb|Pack of Resources | *.pimg";
                DialogResult dr = save.ShowDialog();
                if (dr == DialogResult.OK) {
                    string[] Strings = new string[listBox1.Items.Count];
                    for (int i = 0; i < Strings.Length; i++) {
                        Strings[i] = listBox1.Items[i].ToString();
                    }
                    dr = MessageBox.Show("Would you like to compress the script? (Recommended)\n\nDoes not work with old games.", "ScnEditorGUI", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    SCN.CompressPackage = dr == DialogResult.Yes;
                    PSBStrMan.CompressionLevel = CompressionLevel.Z_BEST_COMPRESSION; //opitional
                    byte[] outfile = SCN.Export(Strings);
                    System.IO.File.WriteAllBytes(save.FileName, outfile);

                }
            }
            MessageBox.Show("File Saved.");
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\r' || e.KeyChar == '\n') {
                listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
            }
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e) {
            this.textBox1.SelectAll();
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e) {
            if ((e.Modifiers == Keys.Control) && (e.KeyCode == Keys.V)) {
                this.listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
            }
        }

        private void decompressImageToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "";
            fd.Filter = "All Files | *.*";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK) {
                byte[] input = System.IO.File.ReadAllBytes(fd.FileName);
                byte[] output = HuffmanTool.DecompressBitmap(input);
                string fname = System.IO.Path.GetFileNameWithoutExtension(fd.FileName);
                System.IO.File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + fname + "_decompressed.raw", output);
                MessageBox.Show("This Resouce is Writed to Dracu-Riot, you need discovery resolution and add a bmp header manually and the unknown resolution.\n\n(Probabbly use RGBQuad)", "ScnEditorGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void compressImageToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "";
            fd.Filter = "All Files | *.*";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK) {
                byte[] input = System.IO.File.ReadAllBytes(fd.FileName);
                byte[] output = HuffmanTool.CompressBitmap(input, true);
                string fname = System.IO.Path.GetFileNameWithoutExtension(fd.FileName);
                System.IO.File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + fname + "_compressed.res", output);
                MessageBox.Show("Compressed to Tool Dir.", "ScnEditorGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
        }

        void tryRecoveryToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All PSB Files|*.psb;*.scn;*.pimg";
            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK) {
                try {
                    byte[] data = System.IO.File.ReadAllBytes(ofd.FileName);
                    var PSBManager = new PSBStrMan(data);
                    data = PSBManager.TryRecovery();
                    System.IO.File.WriteAllBytes(ofd.FileName, data);
                    MessageBox.Show("Package Offset's Updated", "ScnEditorGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) {
                    MessageBox.Show("Failed To Recovery:\n" + ex.Message, "ScnEditorGUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }



        TJS2SManager TJSEditor;
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "";
            fd.Filter = "KiriKiri TJS Compiled Files | *.tjs";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK) {
                byte[] Data = System.IO.File.ReadAllBytes(fd.FileName);
                TJSEditor = new TJS2SManager(Data);
                string[] Strings = TJSEditor.Import();
                listBox1.Items.Clear();
                foreach (string str in Strings)
                    listBox1.Items.Add(str);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.FileName = "";
            fd.Filter = "KiriKiri TJS Compiled Files | *.tjs";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK) {
                string[] NewString = new string[listBox1.Items.Count];
                for (int i = 0; i < NewString.Length; i++)
                    NewString[i] = listBox1.Items[i].ToString();
                System.IO.File.WriteAllBytes(fd.FileName, TJSEditor.Export(NewString));
                MessageBox.Show("File Saved.");
            }
        }

        private void ClipboardSeekSample_Click(object sender, EventArgs e) {
            SeekUpdate.Enabled = ClipboardSeekSample.Checked;
        }

        string LastClip = string.Empty;
        private void SeekUpdate_Tick(object sender, EventArgs e) {
            string Clip = Clipboard.GetText();
            if (LastClip != Clip) {
                LastClip = Clip;
                Clip = SimplfyMatch(Clip);
                if (!string.IsNullOrWhiteSpace(Clip)) {
                    for (int i = 0; i < listBox1.Items.Count; i++) {
                        string Line = SimplfyMatch(listBox1.Items[i].ToString());
                        if (Line == Clip) {
                            SaveIfNeeded();
                            listBox1.SelectedIndex = i;
                            return;
                        }
                    }
                    for (int i = 0; i < listBox1.Items.Count; i++) {
                        string Line = SimplfyMatch(listBox1.Items[i].ToString());
                        if (Line.Contains(Clip)) {
                            SaveIfNeeded();
                            listBox1.SelectedIndex = i;
                            return;
                        }
                    }
                }
            }
        }

        private void SaveIfNeeded() {
            int Sel = listBox1.SelectedIndex;
            if (Sel < 0)
                return;
            if (textBox1.Text != listBox1.Items[Sel].ToString())
                listBox1.Items[Sel] = textBox1.Text;
        }

        /// <summary>
        /// Minify a String at the max.
        /// </summary>
        /// <param name="Str">The string to Minify</param>
        /// <returns>The Minified String</returns>
        internal static string SimplfyMatch(string Str) {
            string Output = TrimString(Str);
            for (int i = 0; i < MatchDel.Length; i++)
                Output = Output.Replace(MatchDel[i], "");
            return Output;
        }        

        /// <summary>
        /// Trim a String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimString(string Input) {
            string Result = Input;
            Result = TrimStart(Result);
            Result = TrimEnd(Result);
            return Result;
        }

        /// <summary>
        /// Trim the Begin of the String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimStart(string Txt) {
            string rst = Txt;
            foreach (string str in TrimContent) {
                if (string.IsNullOrEmpty(str))
                    continue;
                while (rst.StartsWith(str)) {
                    rst = rst.Substring(str.Length, rst.Length - str.Length);
                }
            }

            if (rst != Txt)
                rst = TrimStart(rst);

            return rst;
        }

        /// <summary>
        /// Trim the End of the String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimEnd(string Txt) {
            string rst = Txt;
            foreach (string str in TrimContent) {
                if (string.IsNullOrEmpty(str))
                    continue;
                while (rst.EndsWith(str)) {
                    rst = rst.Substring(0, rst.Length - str.Length);
                }
            }            

            if (rst != Txt)
                rst = TrimEnd(rst);

            return rst;
        }
        static string[] MatchDel = new string[] {
            "\r", "\\r", "\n", "\\n", " ", "_r", "―", "-", "*", "♥", "①", "♪"
        };

        static string[] TrimContent = new string[] {
            " ", "'", "\"", "<", "(", "[", "“", "［", "《", "«",
            "「", "『", "【", "]", "”", "］", "》",
            "»", "」", "』", "】", ")", ">", "‘", "’", "〃", "″",
            "～", "~", "―", "-", "%K", "%LC", "♪", "%P", "%f;",
            "%fSourceHanSansCN-M;", "[―]"
        };

        private void decompressScriptToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.FileName = "All PSB Files|*.psb;*.scn;*.pimg";
            if (fd.ShowDialog() != DialogResult.OK)
                return;
            byte[] Content = System.IO.File.ReadAllBytes(fd.FileName);
            System.IO.File.WriteAllBytes(fd.FileName, PSBStrMan.ExtractMDF(Content));
            MessageBox.Show("Finished");
        }
    }
}
