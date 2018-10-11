using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinPrueba
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string varchivov = "c://valida_hash.txt";
            Cursor.Current = Cursors.WaitCursor;
            string _error = "";
            //Capa_Envio.Basico._envia_xml(ref _error);
            Capa_Envio.Basico._ejecutar_proceso_ws(ref _error);

            //TextWriter tw = new StreamWriter(varchivov, true);
            //tw.WriteLine(DateTime.Now.ToString() + _error);
            //tw.Close();

            MessageBox.Show("ok");
            Cursor.Current = null;

            /*c*/
        }

        private void btnuploadf_Click(object sender, EventArgs e)
        {
            string _error = "";
            Capa_Envio.Basico.ejecuta_proceso_foto(ref _error);
        }
    }
}
