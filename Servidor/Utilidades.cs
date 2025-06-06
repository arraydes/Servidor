using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Utilidades // Clase para convertir un DataSet dato a un JSON
    {
        // Método para convertir un DataTable en JSON utilizando Newtonsoft.Json
        public static string ConvertirDataTableAJson(DataTable tabla)
        {
            try
            {
                // Convertir DataTable a JSON usando JsonConvert
                return JsonConvert.SerializeObject(tabla);
            }
            catch (Exception ex)
            {
                // Manejo de errores en caso de que la conversión falle
                Console.WriteLine("Error al convertir DataTable a JSON: " + ex.Message);
                return null;
            }
        }

    }
}
