using System;
using System.IO;
using System.Collections.Generic;

namespace AdaCompilador
{
    class Program
    {
        static void Main(string[] args)
        {
            string rutaDefecto = @"D:\SEM 1-2026\INF 3631A - Diseno de compiladores\proyecto\Proyecto_INF3631_ada\codigo_fuente_ADA\operaciones_matematicas.txt";            
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
                    Console.WriteLine("No se encontraron errores léxicos.");
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

                Console.WriteLine("\n=======================================================================");
                Console.WriteLine("                ANALIZADOR SINTÁCTICO - COMPILADOR ADA");
                Console.WriteLine("=======================================================================");
                
                AnalizadorSintactico analizadorSintactico = new AnalizadorSintactico(analizador.Tokens);
                bool esSintacticoValido = analizadorSintactico.Analizar(); // devuelve true si tanto los errores sintácticos como los semánticos son 0

                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine("                      REPORTE DE ERRORES SINTÁCTICOS");
                Console.WriteLine("-----------------------------------------------------------------------");
                if (analizadorSintactico.Errores.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("VÁLIDO: No se encontraron errores sintácticos.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{"Línea",-6} | {"Descripción",-60}");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    foreach (ErrorSintactico err in analizadorSintactico.Errores)
                    {
                        Console.WriteLine($"{err.Linea,-6} | {err.Descripcion}");
                    }
                    Console.ResetColor();
                }
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"Total de errores sintácticos detectados: {analizadorSintactico.Errores.Count}");
                Console.WriteLine("=======================================================================");

                Console.WriteLine("\n=======================================================================");
                Console.WriteLine("                ANALIZADOR SEMÁNTICO - COMPILADOR ADA");
                Console.WriteLine("=======================================================================");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("                      REPORTE DE ERRORES SEMÁNTICOS");
                Console.WriteLine("-----------------------------------------------------------------------");
                if (analizadorSintactico.ErroresSemanticos.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("VÁLIDO: No se encontraron errores semánticos.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{"Línea",-6} | {"Descripción",-60}");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    foreach (ErrorSemantico err in analizadorSintactico.ErroresSemanticos)
                    {
                        Console.WriteLine($"{err.Linea,-6} | {err.Descripcion}");
                    }
                    Console.ResetColor();
                }
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine($"Total de errores semánticos detectados: {analizadorSintactico.ErroresSemanticos.Count}");
                Console.WriteLine("=======================================================================");

                // Si no hay errores, imprimir el código intermedio
                if (analizadorSintactico.Errores.Count == 0 && analizadorSintactico.ErroresSemanticos.Count == 0)
                {
                    Console.WriteLine("\n=======================================================================");
                    Console.WriteLine("               GENERACIÓN DE CÓDIGO INTERMEDIO");
                    Console.WriteLine("=======================================================================");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    Console.WriteLine("                    EXPRESIONES (PREFIJO / POSFIJO)");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    var expresiones = analizadorSintactico.Generador.ObtenerExpresiones();
                    if (expresiones.Count == 0)
                    {
                        Console.WriteLine("No se procesaron expresiones.");
                    }
                    else
                    {
                        Console.WriteLine($"{"Línea",-6} | {"Infija (Original)",-32} | {"Prefija",-24} | {"Posfija",-24}");
                        Console.WriteLine("-----------------------------------------------------------------------");
                        foreach (var exp in expresiones)
                        {
                            string inf = exp.Infija;
                            string pref = exp.Prefija;
                            string posf = exp.Posfija;
                            Console.WriteLine($"{exp.Linea,-6} | {inf,-32} | {pref,-24} | {posf,-24}");
                        }
                    }
                    Console.WriteLine("-----------------------------------------------------------------------");
                    Console.WriteLine($"Total de expresiones procesadas: {expresiones.Count}");
                    Console.WriteLine("-----------------------------------------------------------------------");

                    Console.WriteLine("\n------------------------------------------------------------------------------------------------------");
                    Console.WriteLine("                                         CÓDIGO EN CUARTETOS");
                    Console.WriteLine("------------------------------------------------------------------------------------------------------");
                    var cuartetos = analizadorSintactico.Generador.ObtenerCuartetos();
                    if (cuartetos.Count == 0)
                    {
                        Console.WriteLine("No se generaron cuartetos.");
                    }
                    else
                    {
                        Console.WriteLine($"{"No.",-5} | {"Operador",-12} | {"Operador1",-30} | {"Operador2",-30} | {"Resultado",-25}");
                        Console.WriteLine("------------------------------------------------------------------------------------------------------");
                        for (int k = 0; k < cuartetos.Count; k++)
                        {
                            var c = cuartetos[k];
                            string op = c.Operador;
                            string op1 = string.IsNullOrEmpty(c.Operando1) ? "null" : c.Operando1;
                            string op2 = string.IsNullOrEmpty(c.Operando2) ? "null" : c.Operando2;
                            string res = string.IsNullOrEmpty(c.Resultado) ? "null" : c.Resultado;

                            // Truncar si los valores son extremadamente largos para mantener la estructura de la tabla
                            if (op.Length > 12) op = op.Substring(0, 9) + "...";
                            if (op1.Length > 30) op1 = op1.Substring(0, 27) + "...";
                            if (op2.Length > 30) op2 = op2.Substring(0, 27) + "...";
                            if (res.Length > 25) res = res.Substring(0, 22) + "...";

                            Console.WriteLine($"{k + 1,-5} | {op,-12} | {op1,-30} | {op2,-30} | {res,-25}");
                        }
                    }
                    Console.WriteLine("------------------------------------------------------------------------------------------------------");
                    Console.WriteLine($"Total de cuartetos generados: {cuartetos.Count}");
                    Console.WriteLine("=======================================================================");
                }
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