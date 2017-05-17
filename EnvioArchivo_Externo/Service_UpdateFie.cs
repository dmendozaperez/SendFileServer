using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Capa_Envio;
namespace EnvioArchivo_Externo
{
    public partial class Service_UpdateFie : ServiceBase
    {
        /*envio xml al server de la FE*/
        Timer tmservicio_xml = null;
        private Int32 _valida_service_xml = 0;
        /*************************/
        Timer tmservicio_update = null;
        private Int32 _valida_service_update = 0;
        public Service_UpdateFie()
        {
            InitializeComponent();
            /*envio xml*/
            tmservicio_xml = new Timer(1000);
            tmservicio_xml.Elapsed += new ElapsedEventHandler(tmservicio_xml_Elapsed);

            /*envio de archivos update*/
            tmservicio_update = new Timer(1000);
            tmservicio_update.Elapsed += new ElapsedEventHandler(tmservicio_update_Elapsed);
        }

        void tmservicio_update_Elapsed(object sender, ElapsedEventArgs e)
        {
            Int32 _valor = 0;
            try
            {
                if (_valida_service_update == 0)
                {

                    _valor = 1;
                    _valida_service_update = 1;
                    string _error = "";
                    Basico._ejecutar_proceso(ref _error);
                    _valida_service_update = 0;
                }
                //****************************************************************************
            }
            catch
            {
                _valida_service_update = 0;
            }

            if (_valor == 1)
            {
                _valida_service_update = 0;
            }

        }

        void tmservicio_xml_Elapsed(object sender, ElapsedEventArgs e)
        {            
            Int32 _valor = 0;
            try
            {                
                if (_valida_service_xml == 0)
                {

                    _valor = 1;
                    _valida_service_xml = 1;             
                    string _error = "";
                    Basico._envia_xml(ref _error);                    
                    _valida_service_xml = 0;                    
                }
                //****************************************************************************
            }
            catch
            {                
                _valida_service_xml = 0;             
            }

            if (_valor == 1)
            {                
                _valida_service_xml = 0;             
            }

        }
        protected override void OnStart(string[] args)
        {
            tmservicio_xml.Start();
            tmservicio_update.Start();
        }

        protected override void OnStop()
        {
            tmservicio_xml.Stop();
            tmservicio_update.Stop();
        }
    }
}
