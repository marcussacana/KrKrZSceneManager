using System;
using System.Configuration;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScnEditorGUI
{
    public partial class Find : Form
    {
        private Form1 _parentForm = null;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (Environment.OSVersion.Version.Build >= 9200)
            {
                var attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, (int)attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        public Find(Form1 form1)
        {
            InitializeComponent();

            this._parentForm = form1;

            string fontSize = ConfigurationSettings.AppSettings["font_size"];
            string darkMode = ConfigurationSettings.AppSettings["dark_mode"];

            this.textBox1.Font = new Font(
                "Microsoft Sans Serif",
                float.Parse(fontSize),
                FontStyle.Regular,
                GraphicsUnit.Point,
                ((byte)(0))
            );

            if (darkMode == "true") {
                UseImmersiveDarkMode(this.Handle, true);
                this.BackColor = Color.Gray;
                this.textBox1.BackColor = Color.Black;
                this.textBox1.ForeColor = Color.White;
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            this._parentForm.FindMyString(textBox1.Text);
            this.Close();
        }
    }
}
