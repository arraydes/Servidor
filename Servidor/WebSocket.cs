using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;

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
                Configuracion config = new Configuracion("config.txt");

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

                        var resultado = datos.query(mensaje);

                        if (resultado != null)
                        {
                            var tabla = resultado.Tables[0];
                            string respuesta = "";

                            foreach (DataRow fila in tabla.Rows)
                            {
                                foreach (var item in fila.ItemArray)
                                {
                                    respuesta += item.ToString() + "\t";
                                }
                                respuesta += "\n";
                            }

                            socket.Send(respuesta);
                        }
                        else
                        {
                            socket.Send("Error al ejecutar la consulta.");
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar el servidor: " + ex.Message);
            }
        }
    }


}
