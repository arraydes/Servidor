// See https://aka.ms/new-console-template for more information
using Servidor;

Console.WriteLine("Hello, World!");
try
{
    ServidorWebSocket servidor = new ServidorWebSocket();
    servidor.Iniciar();
    
    Console.WriteLine("Presiona ENTER para salir...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
