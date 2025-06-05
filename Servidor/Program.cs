// See https://aka.ms/new-console-template for more information
using Servidor;

Console.WriteLine("Hello, World!");
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
