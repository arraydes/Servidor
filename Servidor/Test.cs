using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servidor
{
    internal class Test
    {
        static void Main(string[] args)
        {
            try
            {
                ServidorWebSocket servidor = new ServidorWebSocket();
                servidor.Iniciar();
                Console.WriteLine("Servidor iniciado :D");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
