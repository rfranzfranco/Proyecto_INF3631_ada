using System;
using System.IO;
using System.Collections.Generic;

namespace AdaCompilador
{
    class Program
    {
        static void Main(string[] args)
        {
            string rutaDefecto = @"d:\SEM 1-2026\INF 3631A - Diseno de compiladores\proyecto\codigo ada\estadisticas.txt";            
            string rutaArchivo = args.Length > 0 ? args[0] : rutaDefecto;
            Console.WriteLine("=======================================================================");
            Console.WriteLine("                ANALIZADOR LÉXICO - COMPILADOR ADA");
            Console.WriteLine("=======================================================================");
            Console.WriteLine($"Archivo de entrada: {rutaArchivo}");

            if (!File.Exists(rutaArchivo))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: El archivo '{rutaArchivo}' no existe.");
                Console.ResetColor();
                return;
            }

            try
            {
                string codigoFuente = File.ReadAllText(rutaArchivo);                
                AnalizadorLexico analizador = new AnalizadorLexico(codigoFuente);
                analizador.Analizar();

                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine("                        TABLA DE TOKENS");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"{"Código",-8} | {"Lexema",-30} | {"Línea",-6}");
                Console.WriteLine("-----------------------------------------------------------------------");

                int totalTokensValidos = 0;
                foreach (Token token in analizador.Tokens)
                {
                    if (token.Codigo != CodigosToken.ErrorLexico)
                    {
                        totalTokensValidos++;
                        Console.WriteLine($"{token.Codigo,-8} | {token.Lexema,-30} | {token.Linea,-6}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{token.Codigo,-8} | {token.Lexema,-30} | {token.Linea,-6} (ERROR LÉXICO)");
                        Console.ResetColor();
                    }
                }
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"Total de tokens válidos reconocidos: {totalTokensValidos}");

                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine("                      REPORTE DE ERRORES LÉXICOS");
                Console.WriteLine("-----------------------------------------------------------------------");
                if (analizador.Errores.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("¡No se encontraron errores léxicos! El código fuente es léxicamente correcto.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{"Línea",-6} | {"Lexema",-10} | {"Descripción",-40}");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    foreach (ErrorLexico err in analizador.Errores)
                    {
                        Console.WriteLine($"{err.Linea,-6} | {err.Lexema,-10} | {err.Descripcion}");
                    }
                    Console.ResetColor();
                }
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"Total de errores léxicos detectados: {analizador.Errores.Count}");
                Console.WriteLine("=======================================================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ocurrió un error al leer o analizar el archivo: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}