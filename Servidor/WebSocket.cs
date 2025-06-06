using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Servidor
{
    internal class ServidorWebSocket
    {
        private List<IWebSocketConnection> clientes = new List<IWebSocketConnection>();
        private Conexion datos;

        public void Iniciar()
        {
            try
            {
                //Se abre el archivo de configuración para obtener la ip
                Configuracion config = new Configuracion("C:\\Users\\Joés\\source\\repos\\Servidor\\Servidor\\config.txt");

                string ip = config.Get("websocket_ip");
                string puerto = config.Get("websocket_puerto");

                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(puerto))
                {
                    Console.WriteLine("IP o puerto de WebSocket no especificado");
                    return;
                }

                string direccionWS = $"ws://{ip}:{puerto}";
                Console.WriteLine("Iniciando servidor WebSocket en: " + direccionWS);

                FleckLog.Level = LogLevel.Debug;

                var servidor = new WebSocketServer(direccionWS);
                datos = new Conexion();

                servidor.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        Console.WriteLine("Cliente conectado: " + socket.ConnectionInfo.ClientIpAddress);
                        clientes.Add(socket);
                    };

                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Cliente desconectado.");
                        clientes.Remove(socket);
                    };

                    socket.OnMessage = mensaje =>
                    {
                        Console.WriteLine("Mensaje recibido: " + mensaje);

                        // Deserializamos el mensaje recibido en un objeto JSON
                        var clienteMsg = JsonConvert.DeserializeObject<Dictionary<string, object>>(mensaje);

                        string respuesta = "";

                        // Verificamos si el mensaje contiene el campo "evento"
                        if (clienteMsg.ContainsKey("evento"))
                        {
                            string evento = clienteMsg["evento"].ToString();

                            switch (evento)
                            {
                                case "GET_INSTRUMENTOS":
                                    // Procesamos la solicitud de obtener los instrumentos
                                    respuesta = ProcesarInstrumentos();
                                    break;

                                case "INSERTAR":
                                    // Procesamos la operación INSERT
                                    //respuesta = ProcesarInsert(clienteMsg, socket, clientes);
                                    //socket.Send(respuesta);
                                    respuesta = ProcesarInsert(clienteMsg, socket);
                                    break;

                                case "ACTUALIZAR":
                                    // Procesamos la operación UPDATE
                                    respuesta = ProcesarUpdate(clienteMsg);
                                    break;

                                case "ELIMINAR":
                                    // Procesamos la operación DELETE
                                    respuesta = ProcesarDelete(clienteMsg);
                                    break;

                                case "AUTENTICAR":
                                    respuesta = ProcesarLogin(clienteMsg);
                                    break;

                                default:
                                    // Si el evento no está en los permitidos, respondemos con un error
                                    respuesta = CrearRespuesta("error", "Operación no válida.");
                                    break;
                            }
                        }
                        else
                        {
                            respuesta = CrearRespuesta("error", "Falta el campo 'evento'.");
                        }

                        // Enviamos la respuesta al cliente
                        socket.Send(respuesta);
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar el servidor: " + ex.Message);
            }
        }

        
        private string ProcesarInstrumentos()
        {
            try
            {
                // Consulta SQL para obtener los instrumentos desde la base de datos
                string query = "SELECT * FROM Instrumentos";  

                // Ejecutamos la consulta
                DataSet ds = datos.query(query);

                if (ds != null && ds.Tables.Count > 0)
                {
                    // Convertimos la tabla a JSON usando Newtonsoft.Json
                    string json = Utilidades.ConvertirDataTableAJson(ds.Tables[0]);

                    // Creamos el objeto con el evento "DATOS" y el contenido con la tabla en JSON
                    var respuesta = new
                    {
                        evento = "DATOS",
                        contenido = json
                    };

                    // Convertimos el objeto a JSON
                    return JsonConvert.SerializeObject(respuesta);
                }
                else
                {
                    // Si no hay datos, devolvemos un error
                    return CrearRespuesta("error", "No se encontraron instrumentos.");
                }
            }
            catch (Exception ex)
            {
                // En caso de error en la consulta, enviamos el mensaje de error
                return CrearRespuesta("error", "Error al obtener los instrumentos: " + ex.Message);
            }
        }


        //Método para procesar el insert que el cliente envía
        private string ProcesarInsert(Dictionary<string, object> clienteMsg, IWebSocketConnection origenCliente)
        {
            try
            {
                if (!clienteMsg.ContainsKey("tabla") || !clienteMsg.ContainsKey("valores"))
                    return CrearRespuesta("error", "Faltan campos obligatorios: 'tabla' o 'valores'");

                string tabla = clienteMsg["tabla"].ToString();

                // Convertimos 'valores' a JObject de manera segura
                var jObject = clienteMsg["valores"] as JObject;
                if (jObject == null)
                    return CrearRespuesta("error", "'valores' no es un objeto válido");

                // Construimos columnas y valores
                string columnas = string.Join(", ", jObject.Properties().Select(p => p.Name));
                string valoresSQL = string.Join(", ", jObject.Properties().Select(p => $"'{p.Value.ToString().Replace("'", "''")}'"));

                string query = $"INSERT INTO {tabla} ({columnas}) VALUES ({valoresSQL})";

                string tablaActualizada = ProcesarInstrumentos(); //La tabla se obtiene con el mismo método para procesar el evento GET_INSTRUMENTOS

                Console.WriteLine("Consulta generada: " + query);

                bool exito = this.datos.command(query); // Asegúrate de que 'datos' es tu objeto de conexión

                if (exito)
                {
                    string respuesta = CrearRespuesta("ok", "Registro insertado correctamente.");
                    foreach (var cliente in clientes)
                    {
                        if (cliente != origenCliente) // No reenviamos al cliente que insertó
                        {
                            cliente.Send(tablaActualizada);
                        }
                    }
                    return respuesta;
                }
                else
                {
                    return CrearRespuesta("error", "Error al insertar registro.");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al insertar: " + ex.Message);
                return CrearRespuesta("error", "Error en la operación INSERT: " + ex.Message);
            }
        }

        //private string ProcesarInsert(Dictionary<string, object> clienteMsg, IWebSocketConnection origenCliente, List<IWebSocketConnection> clientes)
        //{
        //    try
        //    {
        //        if (!clienteMsg.ContainsKey("tabla") || !clienteMsg.ContainsKey("valores"))
        //            return CrearRespuesta("error", "Faltan campos obligatorios: 'tabla' o 'valores'");

        //        string tabla = clienteMsg["tabla"].ToString();

        //        var jObject = clienteMsg["valores"] as JObject;
        //        if (jObject == null)
        //            return CrearRespuesta("error", "'valores' no es un objeto válido");

        //        string columnas = string.Join(", ", jObject.Properties().Select(p => p.Name));
        //        string valoresSQL = string.Join(", ", jObject.Properties().Select(p => $"'{p.Value.ToString().Replace("'", "''")}'"));

        //        string query = $"INSERT INTO {tabla} ({columnas}) VALUES ({valoresSQL})";

        //        Console.WriteLine("Consulta generada: " + query);

        //        bool exito = this.datos.command(query);

        //        if (exito)
        //        {
        //            // Enviamos la tabla actualizada a todos los demás clientes
        //            string tablaActualizada = ProcesarInstrumentos(); //La tabla se obtiene con el mismo método para procesar el evento GET_INSTRUMENTOS

        //            foreach (var cliente in clientes)
        //            {
        //                if (cliente != origenCliente) // No reenviamos al cliente que insertó
        //                {
        //                    cliente.Send(tablaActualizada);
        //                }
        //            }

        //            //return CrearRespuesta("ok", "Registro insertado correctamente.");
        //            return "";
        //        }
        //        else
        //        {
        //            return null;
        //            //return CrearRespuesta("error", "Error al insertar registro.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error al insertar: " + ex.Message);
        //        return CrearRespuesta("error", "Error en la operación INSERT: " + ex.Message);
        //    }
        //}

        // Método para procesar un UPDATE 
        private string ProcesarUpdate(Dictionary<string, object> clienteMsg)
        {
            try
            {
                // Validar que los campos necesarios existen
                if (!clienteMsg.ContainsKey("tabla") ||
                    !clienteMsg.ContainsKey("condiciones") ||
                    !clienteMsg.ContainsKey("valores"))
                {
                    return CrearRespuesta("error", "Faltan campos obligatorios: 'tabla', 'condiciones' o 'valores'");
                }

                string tabla = clienteMsg["tabla"].ToString();
                string condiciones = clienteMsg["condiciones"].ToString();

                // Convertir 'nuevosValores' a JObject para poder recorrer sus propiedades
                var valores = clienteMsg["valores"] as JObject;
                if (valores == null)
                    return CrearRespuesta("error", "'nuevosValores' no es un objeto válido");

                // Construir el conjunto de asignaciones tipo columna = 'valor'
                string asignaciones = string.Join(", ", valores.Properties().Select(p =>
                    $"{p.Name} = '{p.Value.ToString().Replace("'", "''")}'"));

                // Construir consulta SQL
                string query = $"UPDATE {tabla} SET {asignaciones} WHERE {condiciones}";

                Console.WriteLine("Consulta UPDATE generada: " + query);

                bool exito = datos.command(query);

                return exito
                    ? CrearRespuesta("ok", "Registro actualizado correctamente.")
                    : CrearRespuesta("error", "Error al actualizar registro.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en UPDATE: " + ex.Message);
                return CrearRespuesta("error", "Error en la operación UPDATE: " + ex.Message);
            }
        }

        private string ProcesarDelete(Dictionary<string, object> clienteMsg)
        {
            try
            {
                // Validar que los campos necesarios existen
                if (!clienteMsg.ContainsKey("tabla") || !clienteMsg.ContainsKey("condiciones"))
                {
                    return CrearRespuesta("error", "Faltan campos obligatorios: 'tabla' o 'condiciones'");
                }

                string tabla = clienteMsg["tabla"].ToString();
                string condiciones = clienteMsg["condiciones"].ToString();

                // Construir consulta DELETE
                string query = $"DELETE FROM {tabla} WHERE {condiciones}";

                Console.WriteLine("Consulta DELETE generada: " + query);

                bool exito = datos.command(query);

                return exito
                    ? CrearRespuesta("ok", "Registro eliminado correctamente.")
                    : CrearRespuesta("error", "Error al eliminar registro.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en DELETE: " + ex.Message);
                return CrearRespuesta("error", "Error en la operación DELETE: " + ex.Message);
            }
        }


        private string ProcesarLogin(Dictionary<string, object> clienteMsg)
        {
            try
            {
                // Extraemos las credenciales del mensaje
                string usuario = clienteMsg.ContainsKey("usuario") ? clienteMsg["usuario"].ToString() : null;
                string contrasena = clienteMsg.ContainsKey("contrasena") ? clienteMsg["contrasena"].ToString() : null;

                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
                    return CrearRespuesta("error", "Faltan credenciales.");

                // Consulta SQL para verificar usuario y contraseña
                string query = $"SELECT COUNT(*) FROM Usuarios WHERE Usuario = '{usuario}' AND Contrasena = '{contrasena}'";

                var ds = datos.query(query);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int count = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                    if (count > 0)
                        return CrearRespuesta("ok", "Autenticación exitosa.");
                    else
                        return CrearRespuesta("error", "Credenciales incorrectas.");
                }

                return CrearRespuesta("error", "Error al verificar credenciales.");
            }
            catch (Exception ex)
            {
                return CrearRespuesta("error", "Error en autenticación: " + ex.Message);
            }
        }


        // Método para crear respuestas en formato JSON (exito/error), esto con el propósito de avisar de los cambios
        private string CrearRespuesta(string estado, object resultado = null)
        {
            var objeto = new
            {
                evento = "NOTIFICACION",
                contenido = resultado
            };

            return JsonConvert.SerializeObject(objeto);
        }
    }


}
