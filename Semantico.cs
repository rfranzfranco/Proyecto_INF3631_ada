using System;
using System.Collections.Generic;

namespace AdaCompilador
{
    public class Simbolo
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; } // "Integer", "Float", "Boolean", "String", "Natural", etc.
        public string? TipoElemento { get; set; } // Para tipos de arreglos (opcional)
        public bool EsConstante { get; set; }
        public int LineaDeclaracion { get; set; }

        public Simbolo(string nombre, string tipo, bool esConstante, int linea)
        {
            Nombre = nombre;
            Tipo = tipo;
            EsConstante = esConstante;
            LineaDeclaracion = linea;
        }
    }

    public class TablaSimbolos
    {
        private readonly Dictionary<string, Simbolo> _tabla = new Dictionary<string, Simbolo>(StringComparer.OrdinalIgnoreCase);
        public TablaSimbolos? Padre { get; }

        public TablaSimbolos(TablaSimbolos? padre)
        {
            Padre = padre;
        }

        public bool Insertar(Simbolo simb)
        {
            if (_tabla.ContainsKey(simb.Nombre))
            {
                return false; // Error de doble declaración detectado en este ámbito
            }
            _tabla[simb.Nombre] = simb;
            return true;
        }

        public Simbolo? Buscar(string nombre)
        {
            if (_tabla.TryGetValue(nombre, out var simb))
            {
                return simb;
            }
            if (Padre != null)
            {
                return Padre.Buscar(nombre); // Búsqueda recursiva en ámbitos superiores
            }
            return null; // Identificador no declarado
        }
    }

    public class ErrorSemantico
    {
        public int Linea { get; set; }
        public string Descripcion { get; set; }

        public ErrorSemantico(int linea, string descripcion)
        {
            Linea = linea;
            Descripcion = descripcion;
        }

        public override string ToString()
        {
            return $"Error Semántico en Línea {Linea}: {Descripcion}";
        }
    }
}