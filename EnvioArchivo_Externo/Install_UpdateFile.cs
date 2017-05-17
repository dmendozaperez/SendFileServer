using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace EnvioArchivo_Externo
{
    [RunInstaller(true)]
    public partial class Install_UpdateFile : System.Configuration.Install.Installer
    {
        public Install_UpdateFile()
        {
            InitializeComponent();
        }
    }
}
