using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Servidor
{
    internal class Configuracion
    {

        private Dictionary<string, string> configValues;

        public Configuracion(string FilePath)
        {
            configValues = new Dictionary<string, string>();
            CargarConfiguracion(FilePath);
        }

        private void CargarConfiguracion(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException("No se encontró el archivo de configuración.");
            }

            string[] lineas = File.ReadAllLines(FilePath);

            foreach (string linea in lineas)
            {
                if (!string.IsNullOrWhiteSpace(linea) && !linea.TrimStart().StartsWith("#"))
                {
                    string[] partes = linea.Split('=');
                    if (partes.Length == 2)
                    {
                        string clave = partes[0].Trim();
                        string valor = partes[1].Trim();
                        configValues[clave] = valor;
                    }
                }
            }
        }

        public string Get(string clave)
        {
            return configValues.ContainsKey(clave) ? configValues[clave] : null;
        }
    }
}
    

