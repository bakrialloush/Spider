using System;
using System.Linq;
using System.Windows.Forms;

namespace Kosynka
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            foreach (var tb in Controls.OfType<RadioButton>())
            {
                if (tb.Checked)
                    Data.numShirt = Convert.ToInt32(tb.Name.Substring(11));
            }
            Close();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
