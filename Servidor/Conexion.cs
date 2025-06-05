using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Servidor
{
    internal class Conexion
    {
        private string cadenaConexion;
        private SqlConnection conexion;

        public Conexion()
        {
            try
            {
                Configuracion config = new Configuracion("C:\\Users\\Joés\\source\\repos\\Servidor\\Servidor\\config.txt");

                string servidor = config.Get("Server_name");
                string baseDatos = config.Get("Database");
                bool authIntegrada = config.Get("autenticacion_integrada") == "true";

                if (authIntegrada)
                {
                    cadenaConexion = $"Data Source={servidor};Initial Catalog={baseDatos};Integrated Security=true;Encrypt=false";
                }
                else
                {
                    string usuario = config.Get("usuario_sql");
                    string contrasena = config.Get("contrasena_sql");
                    cadenaConexion = $"Data Source={servidor};Initial Catalog={baseDatos};User ID={usuario};Password={contrasena};Encrypt=false";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al leer la configuración: " + ex.ToString());
            }
        }

        private SqlConnection AbrirConexion()
        {
            try
            {
                conexion = new SqlConnection(cadenaConexion);
                conexion.Open();
                return conexion;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al conectar con la base de datos: " + ex.ToString());
                return null;
            }
        }

        public DataSet query(string query)
        {
            try
            {
                DataSet ds = new DataSet();
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, AbrirConexion());
                sqlDataAdapter.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                return null;
            }
        }

        public bool command(string consulta)
        {
            try
            { 
                SqlCommand command = new SqlCommand(consulta, AbrirConexion());
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al ejecutar comando: " + ex.ToString());
                return false;
                throw;
            }
        }
    }
}
        
