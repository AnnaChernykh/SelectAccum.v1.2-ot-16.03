using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Form_1etap
{
    public partial class Form3A : Form
    {
       
        ToolStripLabel infoLabel;
        public Form3A()
        {
            
            InitializeComponent();
            infoLabel = new ToolStripLabel();
            statusStrip1.Items.Add(infoLabel);
            infoLabel.Text = "";
            
        }

        private void Form3A_Load(object sender, EventArgs e)
        {
            Form2A main = this.Owner as Form2A;
            this.Hide();
           
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
