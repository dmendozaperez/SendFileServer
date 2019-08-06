using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Capa_Envio
{
    public class Basico
    {
        #region
        #endregion
        private static string conexion
        {
            get { return "Server=POSPERUBD.BGR.PE;Database=BdWSBata;User ID=pos_oracle;Password=Bata2018**;Trusted_Connection=False;"; }
        }
        private static string conexion_remoto
        {
            get { return "Server=POSPERUBD.BGR.PE;Database=BdWebService;User ID=pos_oracle;Password=Bata2018**;Trusted_Connection=False;"; }
        }
        #region<REGION DE ENVIO XML AL SERVIDOR>
        private static DataSet _retorna_tabla_ruta(ref string _error)
        {
            string sqlquery = "[USP_Consulta_RutaXml_Externo]";
            SqlConnection cn = null;
            SqlCommand cmd = null;
            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {
                cn = new SqlConnection(conexion);
                cmd = new SqlCommand(sqlquery, cn);
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;                
                da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds);

            }
            catch(Exception exc)
            {
                _error = exc.Message;
                ds = null;
            }
            if (cn.State == ConnectionState.Open) cn.Close();
            return ds;
        }


        private static Boolean exportar_archivo_xml(byte[] _archivo_xml, string _destino_path,string _name_archivo,string _user,string _pass,ref string _error)
        {
            Boolean _valida = false;       
            try
            {                                                                                     
                       //        //creando la carpeta de la tienda                    

                NetworkShare.ConnectToShare(@_destino_path, _user, _pass);

                string _archivo_ruta = _destino_path + "\\" + _name_archivo + ".xml";

                File.WriteAllBytes(@_archivo_ruta, _archivo_xml);

                _valida = true;                                                            

            }
            catch(Exception exc) 
            {
                _error = exc.Message;
                _valida = false;                                
            }
            return _valida;
        }


       
        public static void _envia_xml(ref string _error)
        {
            //string _ruta_origen_xml = "";
            DataSet ds = null;
            DataTable dtruta = null;
            DataTable dtuser = null;
            try
            {
                ds = _retorna_tabla_ruta(ref _error);

                if (ds!=null)
                {
                    if (ds.Tables.Count>0)
                    {
                        /**/
                        dtruta = ds.Tables[0];
                        dtuser = ds.Tables[1];

                        string _server_usu = dtuser.Rows[0]["usuario"].ToString();
                        string _server_pas= dtuser.Rows[0]["password"].ToString();

                        for (Int32 i=0;i<dtruta.Rows.Count;++i)
                        {
                            //String _codigo = dtruta.Rows[i]["codigo"].ToString();
                            string _path_xml_origen = "";string _path_xml_destino = "";
                          
                            _path_xml_origen = dtruta.Rows[i]["ruta_xml"].ToString();
                            _path_xml_destino = dtruta.Rows[i]["ruta_xml_externo"].ToString();
                            if (Directory.Exists(@_path_xml_origen))
                            {
                                string[] _archivos_origen_xml_array = Directory.
                                    GetFiles(@_path_xml_origen, "*.xml").OrderBy(d=>new FileInfo(d).CreationTime).ToArray();
                                if (_archivos_origen_xml_array.Length>0)
                                {
                                    for (Int32 a=0;a< _archivos_origen_xml_array.Length;++a)
                                    {
                                        string _archivo = _archivos_origen_xml_array[a].ToString();
                                        string _nombrearchivo_xml = System.IO.Path.GetFileNameWithoutExtension(@_archivo);
                                        byte[] _archivo_bytes = File.ReadAllBytes(@_archivo);

                                        Boolean _copia_server = exportar_archivo_xml(_archivo_bytes, _path_xml_destino, _nombrearchivo_xml, _server_usu, _server_pas,ref _error);
                                        /*si copia ok entonces se borra el archivo*/
                                        if (_copia_server)
                                        {
                                            File.Delete(@_archivo);
                                        }

                                    }
                                }
                            }
                                 
                        }                       

                    }
                }
               
            }
            catch (Exception exc)
            {
                _error = exc.Message;
            }
        }

        public static void _ejecutar_procesos_xml(ref string _error)
        {
            BataWS.Autenticacion conexion = null;
            BataWS.bata_transaccionSoapClient trans = null;
            DataSet ds = null;
            DataTable dtruta = null;
            DataTable dtuser = null;
            try
            {
                ds = _retorna_tabla_ruta(ref _error);
                string _server_usu = "";
                string _server_pas = "";
                if (ds != null)
                {
                    if (ds.Tables.Count > 0)
                    {
                        /**/
                        dtruta = ds.Tables[0];
                        dtuser = ds.Tables[1];

                        _server_usu = dtuser.Rows[0]["usuario"].ToString();
                        _server_pas = dtuser.Rows[0]["password"].ToString();

                        foreach(DataRow fila in dtruta.Rows)
                        {
                            NetworkShare.ConnectToShare(fila["ruta_xml_des"].ToString(), _server_usu, _server_pas);
                        }
                    }
                }
                conexion = new BataWS.Autenticacion();
                conexion.user_name = "emcomer";
                conexion.user_password = "Bata2013";
                trans = new BataWS.bata_transaccionSoapClient();

                var files = trans.ws_get_filexml_ws_bytes(conexion);

                if (files != null)
                {
                    if (files.Count() > 0)
                    {
                        foreach (var item in files)
                        {
                            string error = "";
                            if (enviar_xml(item.file_destino, item.file_bytes,ref error))
                            { 
                                string valor= trans.ws_delete_xml_ws(conexion, item.files_origen);
                            }

                            if (error.Length>0)
                            {
                                TextWriter tw = new StreamWriter(@"D:\ERROR.txt", true);
                                tw.WriteLine(_error);
                                tw.Flush();
                                tw.Close();
                                tw.Dispose();
                            }
                        }
                    }
                }

            }
            catch (Exception exc)
            {
                _error = exc.Message;
            }
        }

        #endregion

        #region<REGION DE LA TRASMISION ARCHIVOS DE TIEDAS>
        private static DataSet _retorna_tabla_ruta_remoto(ref string _error)
        {
            string sqlquery = "[USP_Leer_Ruta_Carpeta_Remoto]";
            SqlConnection cn = null;
            SqlCommand cmd = null;
            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {
                cn = new SqlConnection(conexion_remoto);
                cmd = new SqlCommand(sqlquery, cn);
                cmd.CommandTimeout = 0;
                cmd.CommandType = CommandType.StoredProcedure;
                da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds);

            }
            catch (Exception exc)
            {
                _error = exc.Message;
                ds = null;
            }
            if (cn.State == ConnectionState.Open) cn.Close();
            return ds;
        }
        #region<REGION DE UPDATE ARCHIVOS CEN(TX) RECEPCION AL LOCAL>
        public static void _ejecutar_proceso(ref string _error)
        {
            DataSet dsprincipal = null;
            DataTable dtruta = null;
            DataTable dtwx = null;
            DataTable dttx = null;
            try
            {
                dsprincipal = _retorna_tabla_ruta_remoto(ref _error);
                if (dsprincipal.Tables.Count>0)
                {
                    dtruta = dsprincipal.Tables[0];
                    dtwx = dsprincipal.Tables[1];
                    dttx = dsprincipal.Tables[2];

                    string _ruta_server_remoto = dtruta.Rows[0]["ruta_server_remoto"].ToString();
                    string _ruta_serer_local= dtruta.Rows[0]["ruta_server"].ToString();

                    NetworkShare.ConnectToShare(@_ruta_server_remoto, "interfa", "interfa");

                    _ejecutar_proceso_wx(_ruta_serer_local, _ruta_server_remoto, ref _error);

                    string[] _carpeta_remoto = Directory.GetDirectories(@_ruta_server_remoto);

                    /*en este caso verificamos que la carpeta local existe*/
                    if (!Directory.Exists(@_ruta_serer_local)) Directory.CreateDirectory(@_ruta_serer_local);
                    /**/
                    string[] _carpeta_local = Directory.GetDirectories(@_ruta_serer_local);                    

                    for (Int32 i=0;i<_carpeta_remoto.Length;++i)
                    {
                        //string lastFolderName = System.IO.Path.GetDirectoryName(_carpeta_remoto[i].ToString());

                        var dir_server = new DirectoryInfo(_carpeta_remoto[i].ToString()).Name;
                        //var dirName = dir.Name;
                        Boolean _existe_folder=existe_folder_local(_carpeta_local, dir_server);
                        string _ruta_local = _ruta_serer_local + "\\" + dir_server.ToString();
                        string _ruta_local_tx = _ruta_local + "\\TX";
                        string _ruta_local_wx = _ruta_local + "\\WX";

                        if (!_existe_folder)
                        {
                            
                            Directory.CreateDirectory(@_ruta_local);

                           

                            if (!Directory.Exists(@_ruta_local_tx)) Directory.CreateDirectory(@_ruta_local_tx);
                            if (!Directory.Exists(@_ruta_local_wx)) Directory.CreateDirectory(@_ruta_local_wx);
                        }
                        if (!Directory.Exists(@_ruta_local_tx)) Directory.CreateDirectory(@_ruta_local_tx);
                        if (!Directory.Exists(@_ruta_local_wx)) Directory.CreateDirectory(@_ruta_local_wx);
                        /*ESTE PASO COPIAMOS LOS ARCHIV0S CEN DEL TX SERVER  DESTINO LOCAL TX*/
                        string _path_remoto_server_tx = _carpeta_remoto[i].ToString() + "\\TX";
                        /*VERIFICAMOS SI EL ARCHIVO EXISTE*/
                        if (Directory.Exists(@_path_remoto_server_tx))
                        {
                            /*ahora vamos a extraer los archivos CEN*/
                            string[] _CEN= Directory.GetFiles(@_path_remoto_server_tx, "*.CEN");

                            if (_CEN.Length>0)
                            {
                                for (Int32 a=0;a<_CEN.Length;++a)
                                {
                                    string _archivo = _CEN[a].ToString();
                                    string _nombrearchivo_cen = Path.GetFileNameWithoutExtension(@_archivo);
                                    byte[] _archivo_bytes = File.ReadAllBytes(@_archivo);
                                    Boolean _copia_tx = copiar_fie_tx(_archivo_bytes, @_ruta_local_tx, _nombrearchivo_cen, ref _error);

                                    if (_copia_tx)
                                    {
                                        File.Delete(@_archivo);
                                    }

                                }
                            }

                        }

                    }                    

                }
            }
            catch(Exception exc)
            {                
                _error = exc.Message;
            }
        }

        public static void _ejecutar_proceso_ws(ref string _error)
        {
            DataSet dsprincipal = null;
            DataTable dtruta = null;
            DataTable dtwx = null;
            DataTable dttx = null;
            try
            {
                dsprincipal = _retorna_tabla_ruta_remoto(ref _error);
                if (dsprincipal.Tables.Count > 0)
                {
                    dtruta = dsprincipal.Tables[0];
                    dtwx = dsprincipal.Tables[1];
                    dttx = dsprincipal.Tables[2];

                    string _ruta_server_remoto = dtruta.Rows[0]["ruta_server_remoto"].ToString();
                    string _ruta_serer_local = dtruta.Rows[0]["ruta_server"].ToString();

                    NetworkShare.ConnectToShare(@_ruta_server_remoto, "interfa", "interfa");

                    _ejecutar_proceso_wx_ws(ref _error);

                    string[] _carpeta_remoto = Directory.GetDirectories(@_ruta_server_remoto);

               

                    for (Int32 i = 0; i < _carpeta_remoto.Length; ++i)
                    {
                        //string lastFolderName = System.IO.Path.GetDirectoryName(_carpeta_remoto[i].ToString());

                        var dir_server = new DirectoryInfo(_carpeta_remoto[i].ToString()).Name;
                        //var dirName = dir.Name;
                        Boolean _existe_folder = false; //existe_folder_local(_carpeta_local, dir_server);
                        string _ruta_local = _ruta_serer_local + "\\" + dir_server.ToString();
                        string _ruta_local_tx = _ruta_local + "\\TX";
                        string _ruta_local_wx = _ruta_local + "\\WX";
                      
                        /*ESTE PASO COPIAMOS LOS ARCHIV0S CEN DEL TX SERVER  DESTINO LOCAL TX*/
                        string _path_remoto_server_tx = _carpeta_remoto[i].ToString() + "\\TX";
                        /*VERIFICAMOS SI EL ARCHIVO EXISTE*/
                        if (Directory.Exists(@_path_remoto_server_tx))
                        {
                            /*ahora vamos a extraer los archivos CEN*/
                            string[] _CEN = Directory.GetFiles(@_path_remoto_server_tx, "*.CEN");

                            if (_CEN.Length > 0)
                            {
                                for (Int32 a = 0; a < _CEN.Length; ++a)
                                {
                                    string _archivo = _CEN[a].ToString();
                                    string _nombrearchivo_cen = Path.GetFileNameWithoutExtension(@_archivo);
                                    byte[] _archivo_bytes = File.ReadAllBytes(@_archivo);

                                    Boolean _copia_tx = copiar_file_wx_tx(_archivo_bytes, @_ruta_local_tx, _nombrearchivo_cen, ref _error);

                                    if (_copia_tx)
                                    {
                                        File.Delete(@_archivo);
                                    }

                                }
                            }

                        }

                    }

                }
            }
            catch (Exception exc)
            {
                _error = exc.Message;
            }
        }

        private static Boolean copiar_file_wx_tx(byte[] _archivo_xml, string _destino_path, string _name_archivo, ref string _error)
        {
            BataWS.Autenticacion conexion = null;
            BataWS.bata_transaccionSoapClient trans = null;
            Boolean _valida = false;
            try
            {
                string _archivo_ruta = _destino_path + "\\" + _name_archivo + ".cen";

               // File.WriteAllBytes(@_archivo_ruta, _archivo_xml);

                conexion = new BataWS.Autenticacion();
                conexion.user_name = "emcomer";
                conexion.user_password = "Bata2013";
                trans = new BataWS.bata_transaccionSoapClient();

                _valida = trans.ws_send_filepaq_ws_tx(conexion,_archivo_xml,_destino_path,_name_archivo);

                
            }
            catch (Exception exc)
            {
                _error = exc.Message;
                _valida = false;
            }
            return _valida;
        }

        private static Boolean copiar_fie_tx(byte[] _archivo_xml, string _destino_path, string _name_archivo,ref string _error)
        {
            Boolean _valida = false;
            try
            {
                string _archivo_ruta = _destino_path + "\\" + _name_archivo + ".cen";

                File.WriteAllBytes(@_archivo_ruta, _archivo_xml);

                _valida = true;
            }
            catch(Exception exc)
            {
                _error = exc.Message;
                _valida = false;
            }
            return _valida;
        }
        private static Boolean existe_folder_local(string[] path_local,string _folder_server)
        {
            Boolean _valida = false;
            try
            {
                if (path_local.Length>0)
                {
                    for(Int32 i=0;i<path_local.Length;++i)
                    {                        
                        var dir_local = new DirectoryInfo(path_local[i].ToString()).Name;

                        if (dir_local == _folder_server)
                        {
                            _valida = true; break;
                        }                            
                    }
                }
            }
            catch
            {
                _valida = false;
            }
            return _valida;
        }
        #endregion
        #region<REGION DE UPDATE ARCHIVOS WX ENVIA AL REMOTO>

        private static void _ejecutar_proceso_wx_ws(ref string _error)
        {
            BataWS.Autenticacion conexion = null;
            BataWS.bata_transaccionSoapClient trans = null;
            try
            {
                conexion = new BataWS.Autenticacion();
                conexion.user_name = "emcomer";
                conexion.user_password = "Bata2013";
                trans = new BataWS.bata_transaccionSoapClient();

                var files = trans.ws_get_filepaq_ws_bytes(conexion);

                if (files!=null)
                {
                    if (files.Count() > 0)
                    {
                        foreach (var item in files)
                        {
                            if (enviar_paquete_wx(item.file_destino,item.file_bytes))
                                trans.ws_delete_paq_ws(conexion, item.files_origen);
                        }
                    }
                }

            }
            catch (Exception exc)
            {
                _error = exc.Message;
            }
        }
        private static Boolean enviar_paquete_wx(string file_destino,Byte[] file_bytes)
        {
            Boolean valida = false;
            try
            {
                File.WriteAllBytes(@file_destino, file_bytes);
                valida = true;
            }
            catch(Exception exc) 
            {
                valida = false;
            }
            return valida;
        }

        private static Boolean enviar_xml(string file_destino, Byte[] file_bytes,ref string error)
        {
            Boolean valida = false;
            try
            {
                File.WriteAllBytes(@file_destino, file_bytes);
                valida = true;
            }
            catch(Exception exc)
            {
                error = exc.Message;
                valida = false;
            }
            return valida;
        }

        private static void _ejecutar_proceso_wx(string _ruta_local, string _ruta_remoto, ref string _error)
        {
            try
            {
                if (Directory.Exists(@_ruta_local))
                {
                    //var dir_local = new DirectoryInfo(@_ruta_local).Name;

                    string[] _carpeta_local = Directory.GetDirectories(@_ruta_local);

                    if (_carpeta_local.Length>0)
                    {
                        for(Int32 i=0;i< _carpeta_local.Length;++i)
                        {
                            var dir_server_tda = new DirectoryInfo(_carpeta_local[i].ToString()).Name;
                            string ruta_wx = _carpeta_local[i].ToString() + "\\WX";
                            if (Directory.Exists(@ruta_wx))
                            {
                                string[] filespaq=Directory.GetFiles(@ruta_wx,"*.*");
                                for(Int32 a=0;a<filespaq.Length;++a)
                                {
                                    string _archivo_borrar = filespaq[a].ToString();
                                    if (File.Exists(@_archivo_borrar))
                                    {
                                        FileInfo infofile = new FileInfo(_archivo_borrar);
                                        string _archivo_copiar = infofile.Name;
                                        string _ruta_copiar_error = _ruta_remoto + "\\" + dir_server_tda + "\\WX\\" + _archivo_copiar;
                                        _copiar_valida(_archivo_borrar, _ruta_copiar_error);

                                    }
                                }
                            }
                            
                        }
                    }


                }
            }            
            catch(Exception exc)
            {
                _error = exc.Message;
            }
        }
        private static void _copiar_valida(string _ruta_desde, string _ruta_hasta)
        {            
            try
            {
                File.Copy(@_ruta_desde, @_ruta_hasta, true);
                File.Delete(@_ruta_desde);         
            }
            catch
            {                
            }
            
        }

        #endregion
        #endregion

        #region<ENVIO DE FOTOS AL SERVER POS POR WS>

        public static void  ejecuta_proceso_foto(ref string _error)
        {
            BataUpload.ValidateAcceso ws_header_user = null;
            BataUpload.Bata_TransactionSoapClient ws_get_metodo_ws = null;
            try
            {
                /*credenciales para acceso de la ws pos*/
                ws_header_user = new BataUpload.ValidateAcceso();
                ws_header_user.Username = "3D4F4673-98EB-4EB5-A468-4B7FAEC0C721";
                ws_header_user.Password = "566FDFF1-5311-4FE2-B3FC-0346923FE4B4";
                /**/
                /*instancia de la web services*/
                ws_get_metodo_ws = new BataUpload.Bata_TransactionSoapClient();

                /*captura la ruta desde(origen) de los archivos que se van copiar*/
                BataUpload.Ent_File_Ruta ws_ruta_origen = ws_get_metodo_ws.ws_get_file_path(ws_header_user, "01");/*este codigo 01 es cuando se van a cargar las fotos*/
                
                /*validando que no me devuelva vacio*/
                if (ws_ruta_origen != null)
                {
                    /*capturamos la ruta de origen*/
                    string ruta_origen_fotos = ws_ruta_origen.file_origen;
                    NetworkShare.ConnectToShare(@ruta_origen_fotos, "interfa", "interfa");
                    if (Directory.Exists(@ruta_origen_fotos))
                    {
                        string[] _fotos_path = Directory.GetFiles(@ruta_origen_fotos, "*.jpg");/*filtramos solo las fotos jpg ruta especifica*/
                        //var fotos_nom = _fotos_path.Select(Path.GetFileName).ToArray();/*capturamos solo el nombre del archivo foto*/

                        /*como capturamos todos los nombre de los archivos necesitams enviar a la lista de la web service*/
                        //List<BataUpload.Ent_File> result_files_nom= (from element in fotos_nom
                        //                                             select new BataUpload.Ent_File
                        //                                             {
                        //                                                 file_name = element,

                        //                                             }).ToList();
                        DirectoryInfo d1 = new DirectoryInfo(@ruta_origen_fotos);
                        FileSystemInfo[] files = d1.GetFileSystemInfos();

                        List<BataUpload.Ent_File> result_files_nom = (from element in files.Where(e=>e.Extension.ToUpper()== ".JPG")
                                                                       select new BataUpload.Ent_File
                                                                       {
                                                                           file_name = element.Name,
                                                                           file_creacion = element.CreationTime.ToString("dd/MM/yyyy hh:mm:ss"),
                                                                           file_update = element.LastWriteTime.ToString("dd/MM/yyyy hh:mm:ss"),

                                                                       }).ToList();

                        var array = new BataUpload.Ent_Lista_File();
                        array.lista_file_name = result_files_nom.ToArray();/*convertimos array para la variable de ws*/
                        /*la web serivce solo me a traer los archivos que no estan en la base server*/
                        var get_file_nexiste = ws_get_metodo_ws.ws_get_file_upload(ws_header_user,"01",array);

                        if (get_file_nexiste!=null)
                        {
                            foreach(var item_name_ws in get_file_nexiste)
                            {
                                /*solo en este caso vamos a filtrar el array */
                                var file_rut = _fotos_path.Where(f => f.Contains(item_name_ws.file_name.ToString())).ToList();

                                /*verificamos que no este null*/
                                if (file_rut != null)
                                {
                                    if (file_rut.Count > 0)
                                    {
                                        string ruta_file_local = file_rut[0].ToString();
                                        byte[] file_bytes_local = File.ReadAllBytes(@ruta_file_local);
                                        string nom_file_local = Path.GetFileName(@ruta_file_local);

                                        string msg=ws_get_metodo_ws.ws_download_file(ws_header_user, file_bytes_local, nom_file_local, "01", item_name_ws.file_creacion,item_name_ws.file_update);
                                    }
                                }

                            }
                        }
                    }
                }

            }
            catch (Exception exc)
            {

                _error=exc.Message;
            }

        }

        #endregion

        #region<ENVIO DE COMUNICADO DE TIENDA>
        private static string _conexion_fvdes_oledb(string _path)
        {
            return "Provider=VFPOLEDB;Data Source=" + _path + ";Exclusive=No";
            //return "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + _path_default + ";Extended Properties=dBASE IV;";
        }
        private static BataUpload.Ent_Comunicado get_obj_com(string _path, string tienda,string texto)
        {
            BataUpload.Ent_Comunicado obj = null;
            try
            {
                string sqlquery = "SELECT * FROM FOLDER01  WHERE TIENDA='" + tienda + "' AND UPPER(TEXTO2)='" + texto +"'";
                using (OleDbConnection cn = new OleDbConnection(_conexion_fvdes_oledb(_path)))
                {
                    try
                    {
                        if (cn.State == 0) cn.Open();
                        using (OleDbCommand cmd = new OleDbCommand(sqlquery, cn))
                        {
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.Text;
                            OleDbDataReader dr = cmd.ExecuteReader();
                            if (dr.HasRows)
                            {
                                obj = new BataUpload.Ent_Comunicado();
                                while(dr.Read())
                                {
                                    obj.file_cod_tda = dr["tienda"].ToString();
                                    obj.file_nombre = dr["texto2"].ToString();
                                    obj.file_descripcion = dr["detalle"].ToString();
                                    obj.file_user_nov = dr["f_user"].ToString();                                    
                                }
                            }
                        }

                    }
                    catch (Exception exc)
                    {                        
                        if (cn != null)
                            if (cn.State == ConnectionState.Open) cn.Close();
                        obj = null;
                    }
                    if (cn != null)
                        if (cn.State == ConnectionState.Open) cn.Close();
                }

            }
            catch (Exception exc)
            {
                obj = null;
            }
            return obj;
        }
        private static string del_obj_com(string _path, string tienda, string texto)
        {
            string error ="";
            try
            {
                string sqlquery = "DELETE FROM FOLDER01  WHERE TIENDA='" + tienda + "' AND UPPER(TEXTO2)='" + texto + "'";
                using (OleDbConnection cn = new OleDbConnection(_conexion_fvdes_oledb(_path)))
                {
                    try
                    {
                        if (cn.State == 0) cn.Open();
                        using (OleDbCommand cmd = new OleDbCommand(sqlquery, cn))
                        {
                            cmd.CommandTimeout = 0;
                            cmd.CommandType = CommandType.Text;
                            cmd.ExecuteNonQuery();    
                        }

                    }
                    catch (Exception exc)
                    {
                        if (cn != null)
                            if (cn.State == ConnectionState.Open) cn.Close();
                        error = exc.Message;
                    }
                    if (cn != null)
                        if (cn.State == ConnectionState.Open) cn.Close();
                }

            }
            catch (Exception exc)
            {
                error = exc.Message;
            }
            return error;
        }
        private static void _espera_ejecuta(Int32 _segundos)
        {
            try
            {
                _segundos = _segundos * 1000;
                System.Threading.Thread.Sleep(_segundos);
            }
            catch
            {

            }
        }
        public static void ejecuta_proceso_comunicado(ref string _error)
        {
            BataUpload.ValidateAcceso ws_header_user = null;
            BataUpload.Bata_TransactionSoapClient ws_get_metodo_ws = null;
            try
            {
                /*credenciales para acceso de la ws pos*/
                ws_header_user = new BataUpload.ValidateAcceso();
                ws_header_user.Username = "3D4F4673-98EB-4EB5-A468-4B7FAEC0C721";
                ws_header_user.Password = "566FDFF1-5311-4FE2-B3FC-0346923FE4B4";
                /**/
                /*instancia de la web services*/
                ws_get_metodo_ws = new BataUpload.Bata_TransactionSoapClient();
                

                /*captura la ruta desde(origen) de los archivos que se van copiar*/
                BataUpload.Ent_File_Ruta ws_ruta_origen = ws_get_metodo_ws.ws_get_file_path(ws_header_user, "02");/*este codigo 01 es cuando se van a cargar las fotos*/

                /*validando que no me devuelva vacio*/
                if (ws_ruta_origen != null)
                {
                    /*capturamos la ruta de origen*/
                    string ruta_origen_comunicado = ws_ruta_origen.file_origen;
                    string ruta_destino_comunicado = ws_ruta_origen.file_destino;
                    NetworkShare.ConnectToShare(@ruta_origen_comunicado, "dmendoza", "Bata2013");
                    if (Directory.Exists(@ruta_origen_comunicado))
                    {
                        string[] _carpeta_remoto = Directory.GetDirectories(@ruta_origen_comunicado);

                        for (Int32 i = 0; i < _carpeta_remoto.Length; ++i)
                        {
                            //string lastFolderName = System.IO.Path.GetDirectoryName(_carpeta_remoto[i].ToString());

                            var dir_server = new DirectoryInfo(_carpeta_remoto[i].ToString()).Name;
                            /*SOLO CARPETAS TD*/
                            if (dir_server.Substring(0,2).ToUpper()=="TD")
                            {
                                string ruta_tda_server = ruta_destino_comunicado + "\\50" + dir_server.Substring(2, 3).ToUpper();
                                string _ruta_textos = _carpeta_remoto[i].ToString().ToString() + "\\TEXTOS\\WEB";

                                string cod_tienda= "50" + dir_server.Substring(2, 3).ToUpper();

                                string _ruta_textos_dbf = _carpeta_remoto[i].ToString().ToString() + "\\TEXTOS\\DBF";

                                string _file_ruta_textos_dbf= _carpeta_remoto[i].ToString().ToString() + "\\TEXTOS\\DBF\\FOLDER01.DBF";

                                if (Directory.Exists(_ruta_textos))
                                {
                                    string[] _COM = Directory.GetFiles(@_ruta_textos, "*.*");

                                    if (_COM.Length > 0)
                                    {
                                        for (Int32 a = 0; a < _COM.Length; ++a)
                                        {
                                            string _archivo = _COM[a].ToString();
                                            //string _nombrearchivo_com = Path.GetFileNameWithoutExtension(@_archivo);
                                            byte[] _archivo_bytes = File.ReadAllBytes(@_archivo);
                                            string _nombrearchivo_com = Path.GetFileName(@_archivo).ToUpper();                                            


                                            if (_archivo_bytes!=null)
                                            {
                                                FileInfo inf = new FileInfo(@_archivo);
                                                string file_fec_cre = String.Format("{0:dd/MM/yyyy HH:mm:ss}", inf.CreationTime);
                                                string file_fec_mod = String.Format("{0:dd/MM/yyyy HH:mm:ss}", inf.LastWriteTime);

                                                /*validamos si existe el archivo pq de no existir el objeto es null*/

                                                int c_rpta = 0;
                                                bool procesado = false;
                                                int max_intentos = 40;
                                                int sleep = 5;
                                                Boolean existe_dbf = false;
                                                while (procesado == false && c_rpta <= max_intentos)
                                                {
                                                    existe_dbf = File.Exists(_file_ruta_textos_dbf);
                                                    if (existe_dbf)
                                                    {
                                                        procesado = true;
                                                    }
                                                    else
                                                    {
                                                        procesado = false;
                                                        c_rpta++;
                                                        _espera_ejecuta(sleep);
                                                    }
                                                }

                                                BataUpload.Ent_Comunicado obj = null;

                                                c_rpta = 0;
                                                procesado = false;
                                                max_intentos = 40;
                                                sleep = 5;
                                                while (procesado == false && c_rpta <= max_intentos)
                                                {
                                                    obj = (existe_dbf) ? get_obj_com(_ruta_textos_dbf, cod_tienda, _nombrearchivo_com) : null;
                                                    if (obj!=null)
                                                    {
                                                        procesado = true;
                                                    }
                                                    else
                                                    {
                                                        procesado = false;
                                                        c_rpta++;
                                                        _espera_ejecuta(sleep);
                                                    }
                                                }


                                                

                                                if (obj!=null)
                                                {
                                                    obj.file_fecha_hora_cre = file_fec_cre;
                                                    obj.file_fecha_hora_mod = file_fec_mod;
                                                }

                                                string msg = ws_get_metodo_ws.ws_download_file_comunicado(ws_header_user, _archivo_bytes, _nombrearchivo_com, ruta_tda_server,obj);

                                                if (msg.Length == 0)
                                                {
                                                    /*si no hay errores entonces delete a la tabla el registro y borramos el archivo*/
                                                    string error = (obj==null)?"": del_obj_com(_ruta_textos_dbf, cod_tienda, _nombrearchivo_com);
                                                    if (error.Length==0)
                                                        File.Delete(@_archivo);
                                                }
                                                    
                                            }

                                            

                                        }
                                    }

                                }
                            }

                          
                        }                                                                                 
                    }
                }

            }
            catch (Exception exc)
            {

                _error = exc.Message;
            }

        }
        #endregion
    }
}
