using System;
using System.Collections.Generic;
using System.Text;

namespace AdaCompilador
{
    public class ErrorSintactico
    {
        public int Linea { get; set; }
        public string Descripcion { get; set; }

        public ErrorSintactico(int linea, string descripcion)
        {
            Linea = linea;
            Descripcion = descripcion;
        }

        public override string ToString()
        {
            return $"Error Sintáctico en Línea {Linea}: {Descripcion}";
        }
    }

    public class AnalizadorSintactico
    {
        private readonly List<Token> _listaTokens;
        private int _posicionActual;
        private readonly List<ErrorSintactico> _listaErrores;
        
        // Campos de análisis semántico y código intermedio
        private TablaSimbolos? _ambitoActual;
        private readonly List<ErrorSemantico> _listaErroresSemanticos = new List<ErrorSemantico>();
        private readonly GeneradorCodigoIntermedio _generadorCodigoIntermedio = new GeneradorCodigoIntermedio();
        private List<Token>? _tokensExpActual = null;

        public List<ErrorSintactico> Errores => _listaErrores;
        public List<ErrorSemantico> ErroresSemanticos => _listaErroresSemanticos;
        public GeneradorCodigoIntermedio Generador => _generadorCodigoIntermedio;

        public AnalizadorSintactico(List<Token> tokens)
        {
            _listaTokens = new List<Token>();
            foreach (var t in tokens)
            {
                if (t.Codigo != CodigosToken.ErrorLexico)
                {
                    _listaTokens.Add(t);
                }
            }
            _posicionActual = 0;
            _listaErrores = new List<ErrorSintactico>();
        }

        public bool Analizar()
        {
            _listaErrores.Clear();
            _listaErroresSemanticos.Clear();
            _generadorCodigoIntermedio.Clear();
            _posicionActual = 0;
            _tokensExpActual = null;
            
            // Inicializar el ámbito global
            _ambitoActual = new TablaSimbolos(null);

            if (_listaTokens.Count == 0)
            {
                RegistrarError(1, "El archivo no contiene tokens válidos para analizar.");
                return false;
            }

            try
            {
                Programa();
            }
            catch (Exception ex)
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Fallo crítico en el analizador sintáctico (EBNF): {ex.Message}");
            }

            if (_posicionActual < _listaTokens.Count)
            {
                Token tokenInesperado = ObtenerTokenActual();
                RegistrarError(tokenInesperado.Linea, $"Símbolo inesperado '{tokenInesperado.Lexema}' después del final del programa.");
            }

            return _listaErrores.Count == 0 && _listaErroresSemanticos.Count == 0;
        }

        private Token ObtenerTokenActual()
        {
            if (_posicionActual >= _listaTokens.Count)
            {
                int ultimaLinea = _listaTokens.Count > 0 ? _listaTokens[_listaTokens.Count - 1].Linea : 1;
                return new Token(-1, "EOF", ultimaLinea);
            }
            return _listaTokens[_posicionActual];
        }

        private void Avanzar()
        {
            if (_posicionActual < _listaTokens.Count)
            {
                Token tokenActual = _listaTokens[_posicionActual];
                
                // Registrar el token si estamos recolectando para una expresión
                if (_tokensExpActual != null)
                {
                    _tokensExpActual.Add(tokenActual);
                }

                // Asegurar que el uso de mayúsculas y minúsculas coincida con la palabra reservada
                if (EsCodigoPalabraReservada(tokenActual.Codigo))
                {
                    if (EsPalabraReservadaCasingIncorrecto(tokenActual.Lexema))
                    {
                        RegistrarError(tokenActual.Linea, $"La palabra reservada o tipo '{tokenActual.Lexema}' tiene un uso incorrecto de mayúsculas/minúsculas.");
                    }
                }

                _posicionActual++;
            }
        }

        private bool EsCodigoPalabraReservada(int codigo)
        {
            return (codigo >= 10 && codigo <= 777) || (codigo >= 900 && codigo <= 940);
        }

        private bool EsPalabraReservadaCasingIncorrecto(string lexema)
        {
            foreach (var kv in CodigosToken.PalabrasReservadas)
            {
                if (string.Equals(kv.Key, lexema, StringComparison.OrdinalIgnoreCase))
                {                    
                    return !string.Equals(kv.Key, lexema, StringComparison.Ordinal);
                }
            }
            return false;
        }

        private bool Emparejar(int codigoEsperado, string mensajeError)
        {
            Token tokenActual = ObtenerTokenActual();
            if (tokenActual.Codigo == codigoEsperado)
            {
                Avanzar();
                return true;
            }
            else
            {
                RegistrarError(tokenActual.Linea, mensajeError + $" Se encontró '{tokenActual.Lexema}'.");
                return false;
            }
        }

        private void RegistrarError(int linea, string descripcion)
        {
            if (_listaErrores.Count > 0 && 
                _listaErrores[_listaErrores.Count - 1].Linea == linea && 
                _listaErrores[_listaErrores.Count - 1].Descripcion == descripcion)
            {
                return;
            }
            _listaErrores.Add(new ErrorSintactico(linea, descripcion));
        }

        private void RegistrarErrorSemantico(int linea, string descripcion)
        {
            if (_listaErroresSemanticos.Count > 0 &&
                _listaErroresSemanticos[_listaErroresSemanticos.Count - 1].Linea == linea &&
                _listaErroresSemanticos[_listaErroresSemanticos.Count - 1].Descripcion == descripcion)
            {
                return;
            }
            _listaErroresSemanticos.Add(new ErrorSemantico(linea, descripcion));
        }

        // Auxiliares para comprobación de tipos y generación de cuartetos
        private string ObtenerTipoExp(NodoExp? node)
        {
            if (node == null) return "Unknown";
            if (!node.EsOperador)
            {
                if (double.TryParse(node.Valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double _))
                {
                    return node.Valor.Contains(".") ? "Float" : "Integer";
                }
                if (node.Valor.Equals("true", StringComparison.OrdinalIgnoreCase) || node.Valor.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return "Boolean";
                }
                if (node.Valor.StartsWith("\"") && node.Valor.EndsWith("\""))
                {
                    return "String";
                }
                
                string nombreBase = node.Valor;
                int idxParentesis = nombreBase.IndexOf('(');
                bool esAccesoElemento = (idxParentesis > 0);
                if (esAccesoElemento)
                {
                    nombreBase = nombreBase.Substring(0, idxParentesis);
                }

                if (_ambitoActual == null) return "Unknown";
                Simbolo? simb = _ambitoActual.Buscar(nombreBase);
                if (simb != null)
                {
                    if (esAccesoElemento)
                    {
                        // Buscar la definición del tipo de arreglo
                        Simbolo? typeSimb = _ambitoActual.Buscar(simb.Tipo);
                        if (typeSimb != null && typeSimb.Tipo == "ArrayType" && !string.IsNullOrEmpty(typeSimb.TipoElemento))
                        {
                            return typeSimb.TipoElemento;
                        }
                        // De lo contrario, es una llamada a función (o conversión) -> devolver su tipo directamente
                        return simb.Tipo;
                    }
                    return simb.Tipo;
                }
                return "Unknown";
            }

            if (node.Der == null)
            {
                if (node.Valor.Equals("not", StringComparison.OrdinalIgnoreCase))
                {
                    return "Boolean";
                }
                if (node.Valor.Equals("unary_minus", StringComparison.OrdinalIgnoreCase) || node.Valor.Equals("unary_plus", StringComparison.OrdinalIgnoreCase))
                {
                    return ObtenerTipoExp(node.Izq);
                }
                if (node.Valor.Equals("Integer", StringComparison.OrdinalIgnoreCase) ||
                    node.Valor.Equals("Float", StringComparison.OrdinalIgnoreCase) ||
                    node.Valor.Equals("Boolean", StringComparison.OrdinalIgnoreCase))
                {
                    return node.Valor;
                }
                return "Unknown";
            }

            string tIzq = ObtenerTipoExp(node.Izq);
            string tDer = ObtenerTipoExp(node.Der);

            if (tIzq == "Unknown" || tDer == "Unknown") return "Unknown";

            if (node.Valor.Equals("and", StringComparison.OrdinalIgnoreCase) ||
                node.Valor.Equals("and then", StringComparison.OrdinalIgnoreCase) ||
                node.Valor.Equals("or", StringComparison.OrdinalIgnoreCase) ||
                node.Valor.Equals("or else", StringComparison.OrdinalIgnoreCase) ||
                node.Valor.Equals("xor", StringComparison.OrdinalIgnoreCase))
            {
                if (tIzq != "Boolean" || tDer != "Boolean")
                {
                    RegistrarErrorSemantico(ObtenerTokenActual().Linea, "Error Semántico: Incompatibilidad de tipos.");
                    return "Unknown";
                }
                return "Boolean";
            }

            if (node.Valor == "=" || node.Valor == "/=" || node.Valor == "<" || node.Valor == ">" || node.Valor == "<=" || node.Valor == ">=")
            {
                if (tIzq != tDer)
                {
                    RegistrarErrorSemantico(ObtenerTokenActual().Linea, "Error Semántico: Incompatibilidad de tipos.");
                    return "Unknown";
                }
                return "Boolean";
            }

            if (tIzq != tDer)
            {
                RegistrarErrorSemantico(ObtenerTokenActual().Linea, "Error Semántico: Incompatibilidad de tipos.");
                return "Unknown";
            }
            return tIzq;
        }

        private string? GenerarCuartetosExpre(NodoExp? node)
        {
            if (node == null) return null;
            if (!node.EsOperador) return node.Valor;

            string? arg1 = GenerarCuartetosExpre(node.Izq);
            string? arg2 = GenerarCuartetosExpre(node.Der);

            string temp = _generadorCodigoIntermedio.NuevaVariableTemporal();
            string op = node.Valor;
            if (op == "unary_minus") op = "-";
            else if (op == "unary_plus") op = "+";

            _generadorCodigoIntermedio.Emitir(op, arg1, arg2, temp);
            return temp;
        }

        // programa = librerias, cuerpoPrinc
        private void Programa()
        {
            Librerias();
            CuerpoPrinc();
        }

        // librerias = lib, {librerias}
        private void Librerias()
        {
            Lib();
            while (ObtenerTokenActual().Codigo == CodigosToken.With || ObtenerTokenActual().Codigo == CodigosToken.Use)
            {
                Lib();
            }
        }

        // lib = uso, "Ada.", (Text_IO | Integer_Text_IO | Float_Text_IO), ";"
        private void Lib()
        {
            Uso();
            if (!Emparejar(CodigosToken.Ada, "Se esperaba 'Ada'.")) { SincronizarLib(); return; }
            if (!Emparejar(CodigosToken.Punto, "Se esperaba '.'.")) { SincronizarLib(); return; }

            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Text_IO || codigo == CodigosToken.Integer_Text_IO || codigo == CodigosToken.Float_Text_IO)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba 'Text_IO', 'Integer_Text_IO' o 'Float_Text_IO'. Se encontró '{ObtenerTokenActual().Lexema}'.");
                SincronizarLib();
                return;
            }

            if (!Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.")) { SincronizarLib(); return; }
        }

        private void SincronizarLib()
        {
            while (_posicionActual < _listaTokens.Count)
            {
                int codigo = ObtenerTokenActual().Codigo;
                if (codigo == CodigosToken.With || codigo == CodigosToken.Use || codigo == CodigosToken.Procedure || codigo == CodigosToken.PuntoYComa)
                {
                    if (codigo == CodigosToken.PuntoYComa)
                    {
                        Avanzar();
                    }
                    break;
                }
                Avanzar();
            }
        }

        // uso = "with" | "use"
        private void Uso()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.With || codigo == CodigosToken.Use)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, "Se esperaba 'with' o 'use'.");
            }
        }

        // cuerpoPrinc = "procedure", identificador, funcEntrada, "is", funciones, cuerpo, ";"
        private void CuerpoPrinc()
        {
            Emparejar(CodigosToken.Procedure, "Se esperaba 'procedure'.");
            string? procNombre = Identificador();
            
            if (_ambitoActual != null && procNombre != null)
            {
                _ambitoActual.Insertar(new Simbolo(procNombre, "Procedure", false, ObtenerTokenActual().Linea));
            }

            FuncEntrada();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Funciones();
            Cuerpo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");
        }

        // funcEntrada = ["(", varEntradas, ")"]
        private void FuncEntrada()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre)
            {
                Avanzar();
                VarEntradas();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
        }

        // cuerpo = "begin", acciones, "end", identificador
        private void Cuerpo()
        {
            Emparejar(CodigosToken.Begin, "Se esperaba 'begin'.");
            Acciones();
            Emparejar(CodigosToken.End, "Se esperaba 'end'.");
            Identificador();
        }

        // funciones = bloqueCod, {bloqueCod}
        private void Funciones()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Begin)
            {
                return;
            }

            BloqueCod();
            while (ObtenerTokenActual().Codigo != CodigosToken.Begin && ObtenerTokenActual().Codigo != -1)
            {
                int posAntes = _posicionActual;
                BloqueCod();
                if (_posicionActual == posAntes)
                {
                    RegistrarError(ObtenerTokenActual().Linea, $"Símbolo inesperado en zona de declaraciones: '{ObtenerTokenActual().Lexema}'.");
                    Avanzar();
                }
            }
        }

        // bloqueCod = variables | funcion | coleccion | procedimiento
        private void BloqueCod()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Type)
            {
                Coleccion();
            }
            else if (codigo == CodigosToken.Function)
            {
                Funcion();
            }
            else if (codigo == CodigosToken.Procedure)
            {
                Procedimiento();
            }
            else if (codigo == CodigosToken.Identificador)
            {
                Variables();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Declaración inválida: '{ObtenerTokenActual().Lexema}'.");
                Avanzar();
            }
        }

        // procedimiento = "procedure", identificador, funcEntrada, "is", funciones, cuerpo, ";"
        private void Procedimiento()
        {
            Emparejar(CodigosToken.Procedure, "Se esperaba 'procedure'.");
            string? procNombre = Identificador();

            if (_ambitoActual != null && procNombre != null)
            {
                _ambitoActual.Insertar(new Simbolo(procNombre, "Procedure", false, ObtenerTokenActual().Linea));
            }

            // Crear un nuevo ámbito para el procedimiento
            _ambitoActual = new TablaSimbolos(_ambitoActual);

            FuncEntrada();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Funciones();
            Cuerpo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");

            // Restaurar el ámbito anterior
            if (_ambitoActual != null)
            {
                _ambitoActual = _ambitoActual.Padre;
            }
        }

        // coleccion = "type", identificador, "is array", "(", longitud, ")", "of", tipo, ";", variables
        private void Coleccion()
        {
            Emparejar(CodigosToken.Type, "Se esperaba 'type'.");
            string? nombre = Identificador();

            string elTipo = "Integer";

            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Emparejar(CodigosToken.Array, "Se esperaba 'array'.");
            Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
            Longitud();
            Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            Emparejar(CodigosToken.Of, "Se esperaba 'of'.");
            
            // Capturar el nombre del tipo
            Token typeTok = ObtenerTokenActual();
            if (typeTok.Codigo == CodigosToken.Integer || typeTok.Codigo == CodigosToken.Float || typeTok.Codigo == CodigosToken.Boolean)
            {
                elTipo = typeTok.Lexema;
                Tipo();
            }

            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");

            if (nombre != null && _ambitoActual != null)
            {
                // Registrar el tipo de arreglo personalizado con su tipo de elemento
                var simbType = new Simbolo(nombre, "ArrayType", false, ObtenerTokenActual().Linea);
                simbType.TipoElemento = elTipo;
                _ambitoActual.Insertar(simbType);
            }

            Variables();
        }

        // variables = var, {var}
        private void Variables()
        {
            while (ObtenerTokenActual().Codigo == CodigosToken.Identificador)
            {
                Var();
            }
        }

        // var = identificador, ":", tipoVar, ";"
        private void Var()
        {
            int linea = ObtenerTokenActual().Linea;
            string? nombre = Identificador();
            if (!Emparejar(CodigosToken.DosPuntos, "Se esperaba ':'. ")) { SincronizarVar(); return; }

            bool esConstante;
            NodoExp? initExpr;
            string tipo = TipoVar(out esConstante, out initExpr) ?? "Unknown";

            if (!Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'. ")) { SincronizarVar(); return; }

            if (nombre != null && _ambitoActual != null)
            {
                Simbolo simb = new Simbolo(nombre, tipo, esConstante, linea);
                if (!_ambitoActual.Insertar(simb))
                {
                    RegistrarErrorSemantico(linea, $"Error Semántico: El identificador '{nombre}' ya ha sido declarado en este ámbito.");
                }

                // Comprobar compatibilidad de tipos del inicializador
                if (initExpr != null)
                {
                    string tipoInit = ObtenerTipoExp(initExpr);
                    if (tipoInit != "Unknown" && tipo != "Unknown" && tipo != tipoInit)
                    {
                        RegistrarErrorSemantico(linea, "Error Semántico: Incompatibilidad de tipos en asignación.");
                    }

                    // Emitir cuarteto de inicialización
                    string? resTemp = GenerarCuartetosExpre(initExpr);
                    _generadorCodigoIntermedio.Emitir(":=", nombre, resTemp, null);
                }
                else if (esConstante)
                {
                    RegistrarErrorSemantico(linea, $"Error Semántico: La constante '{nombre}' debe ser inicializada.");
                }
            }
        }

        private void SincronizarVar()
        {
            while (_posicionActual < _listaTokens.Count)
            {
                int codigo = ObtenerTokenActual().Codigo;
                if (codigo == CodigosToken.PuntoYComa)
                {
                    Avanzar();
                    break;
                }
                if (codigo == CodigosToken.Begin || 
                    codigo == CodigosToken.Procedure || 
                    codigo == CodigosToken.Function || 
                    codigo == CodigosToken.Type)
                {
                    break;
                }
                Avanzar();
            }
        }

        // tipoVar = identificador | palConst, tipo, valorIni | cadenaVar
        private string? TipoVar(out bool esConstante, out NodoExp? initExpr)
        {
            esConstante = false;
            initExpr = null;
            int codigo = ObtenerTokenActual().Codigo;

            if (codigo == CodigosToken.String || codigo == CodigosToken.Natural)
            {
                string t = ObtenerTokenActual().Lexema;
                CadenaVar();
                return t;
            }
            else if (codigo == CodigosToken.Constant)
            {
                esConstante = true;
                PalConst(); // Consumir "constant"
                string t = ObtenerTokenActual().Lexema;
                Tipo();
                initExpr = ValorIni();
                return t;
            }
            else if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
            {
                string t = ObtenerTokenActual().Lexema;
                Tipo();
                initExpr = ValorIni();
                return t;
            }
            else if (codigo == CodigosToken.Identificador)
            {
                string? t = Identificador();
                return t;
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Tipo de variable inválido: '{ObtenerTokenActual().Lexema}'.");
                Avanzar();
                return "Unknown";
            }
        }

        // palConst = ["constant"]
        private void PalConst()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Constant)
            {
                Avanzar();
            }
        }

        // tipoSal = tipo | cadenaVar
        private string TipoSal()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
            {
                string t = ObtenerTokenActual().Lexema;
                Tipo();
                return t;
            }
            else if (codigo == CodigosToken.String || codigo == CodigosToken.Natural)
            {
                string t = ObtenerTokenActual().Lexema;
                CadenaVar();
                return t;
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba un tipo de retorno válido, se encontró '{ObtenerTokenActual().Lexema}'.");
                return "Unknown";
            }
        }

        // cadenaVar = "String", ["(", longitud, ")"] | "Natural"
        private void CadenaVar()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.String)
            {
                Avanzar();
                if (ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre)
                {
                    Avanzar();
                    Longitud();
                    Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                }
            }
            else if (codigo == CodigosToken.Natural)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, "Se esperaba 'String' o 'Natural'.");
            }
        }

        // valorIni = [":=", expre]
        private NodoExp? ValorIni()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Asignacion)
            {
                Avanzar();
                return Expre();
            }
            return null;
        }

        // funcion = "function", identificador, "(", varEntradas, ")", "return", tipoSal, "is", variables, cuerpo, ";"
        private void Funcion()
        {
            Emparejar(CodigosToken.Function, "Se esperaba 'function'.");
            string? funcNombre = Identificador();

            // Crear ámbito para los parámetros y el cuerpo
            var funcScope = new TablaSimbolos(_ambitoActual);
            var parentScope = _ambitoActual;
            _ambitoActual = funcScope;

            Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
            VarEntradas();
            Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            Emparejar(CodigosToken.Return, "Se esperaba 'return'.");
            
            string retTipo = TipoSal();

            // Registrar la función en el ámbito del padre con su tipo de retorno
            if (parentScope != null && funcNombre != null)
            {
                parentScope.Insertar(new Simbolo(funcNombre, retTipo, false, ObtenerTokenActual().Linea));
            }

            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Variables();
            Cuerpo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");

            // Restaurar ámbito
            _ambitoActual = parentScope;
        }

        // varEntradas = varEnt, ";", {varEnt, ";"}
        private void VarEntradas()
        {
            VarEnt();
            if (ObtenerTokenActual().Codigo == CodigosToken.PuntoYComa)
            {
                Avanzar();
                while (ObtenerTokenActual().Codigo != CodigosToken.ParentesisCierra && ObtenerTokenActual().Codigo != -1)
                {
                    VarEnt();
                    if (ObtenerTokenActual().Codigo == CodigosToken.PuntoYComa)
                    {
                        Avanzar();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // varEnt = identificador, ":", inAux, tipoVar
        private void VarEnt()
        {
            int linea = ObtenerTokenActual().Linea;
            string? nombre = Identificador();
            Emparejar(CodigosToken.DosPuntos, "Se esperaba ':'.");
            InAux();
            
            bool esConst;
            NodoExp? initExpr;
            string tipo = TipoVar(out esConst, out initExpr) ?? "Unknown";

            if (nombre != null && _ambitoActual != null)
            {
                Simbolo simb = new Simbolo(nombre, tipo, esConst, linea);
                if (!_ambitoActual.Insertar(simb))
                {
                    RegistrarErrorSemantico(linea, $"Error Semántico: El identificador '{nombre}' ya ha sido declarado en este ámbito.");
                }
            }
        }

        // inAux = [("in" | "out" | "in out")]
        private void InAux()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.In)
            {
                Avanzar();
                if (ObtenerTokenActual().Codigo == CodigosToken.Out)
                {
                    Avanzar();
                }
            }
            else if (codigo == CodigosToken.Out)
            {
                Avanzar();
            }
        }

        // acciones = accion, ";", {accion, ";"}
        private void Acciones()
        {
            Accion();
            if (!Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'. "))
            {
                SincronizarAccion();
            }

            while (ObtenerTokenActual().Codigo != CodigosToken.End &&
                   ObtenerTokenActual().Codigo != CodigosToken.Elsif &&
                   ObtenerTokenActual().Codigo != CodigosToken.Else &&
                   ObtenerTokenActual().Codigo != CodigosToken.When &&
                   ObtenerTokenActual().Codigo != -1)
            {
                int posAntes = _posicionActual;
                Accion();
                if (!Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'. "))
                {
                    SincronizarAccion();
                }
                if (_posicionActual == posAntes)
                {
                    RegistrarError(ObtenerTokenActual().Linea, $"Acción inválida en secuencia: '{ObtenerTokenActual().Lexema}'.");
                    Avanzar();
                }
            }
        }

        private void SincronizarAccion()
        {
            while (_posicionActual < _listaTokens.Count)
            {
                int codigo = ObtenerTokenActual().Codigo;
                if (codigo == CodigosToken.PuntoYComa)
                {
                    Avanzar();
                    break;
                }
                if (codigo == CodigosToken.End || 
                    codigo == CodigosToken.Elsif || 
                    codigo == CodigosToken.Else || 
                    codigo == CodigosToken.When || 
                    codigo == CodigosToken.If || 
                    codigo == CodigosToken.For || 
                    codigo == CodigosToken.WhileKw ||
                    codigo == CodigosToken.Loop || 
                    codigo == CodigosToken.Case ||
                    codigo == CodigosToken.Put ||
                    codigo == CodigosToken.Get ||
                    codigo == CodigosToken.Exit)
                {
                    break;
                }
                Avanzar();
            }
        }

        // accion = "return", expre | "Put_Line", "(", datoPut, ")" | "Put", "(", datoPut, ")" | "New_Line" | ...
        private void Accion()
        {
            int codigo = ObtenerTokenActual().Codigo;

            if (codigo == CodigosToken.Return)
            {
                Avanzar();
                NodoExp? expTree = Expre();
                if (expTree != null)
                {
                    string? resTemp = GenerarCuartetosExpre(expTree);
                    _generadorCodigoIntermedio.Emitir("return", resTemp, null, null);
                }
            }
            else if (codigo == CodigosToken.Put_Line)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                DatoPut();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                _generadorCodigoIntermedio.Emitir("Put_Line", null, null, null);
            }
            else if (codigo == CodigosToken.Put)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                DatoPut();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                _generadorCodigoIntermedio.Emitir("Put", null, null, null);
            }
            else if (codigo == CodigosToken.New_Line)
            {
                Avanzar();
                _generadorCodigoIntermedio.Emitir("New_Line", null, null, null);
            }
            else if (codigo == CodigosToken.Get_Line)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                ListaVar();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                _generadorCodigoIntermedio.Emitir("Get_Line", null, null, null);
            }
            else if (codigo == CodigosToken.Get)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                VarUso();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                _generadorCodigoIntermedio.Emitir("Get", null, null, null);
            }
            else if (codigo == CodigosToken.Exit)
            {
                Avanzar();
                Emparejar(CodigosToken.When, "Se esperaba 'when'.");
                NodoExp? condTree = Condicion();
                string? condTemp = GenerarCuartetosExpre(condTree);
                _generadorCodigoIntermedio.Emitir("exit_when", condTemp, null, null);
            }
            else if (codigo == CodigosToken.For)
            {
                CicloPara();
            }
            else if (codigo == CodigosToken.If)
            {
                CicloSi();
            }
            else if (codigo == CodigosToken.WhileKw)
            {
                CicloMientras();
            }
            else if (codigo == CodigosToken.Loop)
            {
                CicloLoop();
            }
            else if (codigo == CodigosToken.Case)
            {
                CicloCase();
            }
            else if (codigo == CodigosToken.Identificador)
            {
                Token varTok = ObtenerTokenActual();
                string? nombre = VarUso();
                if (ObtenerTokenActual().Codigo == CodigosToken.Asignacion)
                {
                    Avanzar();
                    NodoExp? expTree = Expre();
                    
                    if (nombre != null && _ambitoActual != null)
                    {
                        Simbolo? simb = _ambitoActual.Buscar(nombre);
                        if (simb != null)
                        {
                            if (simb.EsConstante)
                            {
                                RegistrarErrorSemantico(varTok.Linea, $"Error Semántico: No se puede asignar un valor a la constante '{nombre}'.");
                            }
                            if (expTree != null)
                            {
                                string tipoExpr = ObtenerTipoExp(expTree);
                                if (tipoExpr != "Unknown" && simb.Tipo != "Unknown" && simb.Tipo != tipoExpr)
                                {
                                    RegistrarErrorSemantico(varTok.Linea, "Error Semántico: Incompatibilidad de tipos en asignación.");
                                }
                            }
                        }

                        // Emitir cuarteto de asignación
                        if (expTree != null)
                        {
                            string? resTemp = GenerarCuartetosExpre(expTree);
                            _generadorCodigoIntermedio.Emitir(":=", nombre, resTemp, null);
                        }
                    }
                }
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Acción sintácticamente no reconocida: '{ObtenerTokenActual().Lexema}'.");
                Avanzar();
            }
        }

        // identificador = letra, resto_id
        private string? Identificador()
        {
            Token tok = ObtenerTokenActual();
            if (tok.Codigo == CodigosToken.Identificador)
            {
                Avanzar();
                return tok.Lexema;
            }
            else
            {
                RegistrarError(tok.Linea, $"Se esperaba un identificador, se encontró '{tok.Lexema}'.");
                return null;
            }
        }

        // var_uso = identificador, ["(", lista_var, ")"]
        private string? VarUso()
        {
            var prevTokensExp = _tokensExpActual;
            _tokensExpActual = null; // Deshabilitar la grabación automática para fusionar los tokens de VarUso
            
            int startPos = _posicionActual;
            Token tok = ObtenerTokenActual();
            string? nombre = Identificador();

            if (nombre != null && _ambitoActual != null)
            {
                Simbolo? simb = _ambitoActual.Buscar(nombre);
                if (simb == null)
                {
                    RegistrarErrorSemantico(tok.Linea, $"Error Semántico: La variable '{nombre}' no existe en el ámbito actual.");
                }
            }

            if (ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre)
            {
                Avanzar();
                ListaVar();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            int endPos = _posicionActual;

            // Combinar tokens consumidos dentro de VarUso en un único operando de cadena
            string lexeme = "";
            for (int k = startPos; k < endPos; k++)
            {
                lexeme += _listaTokens[k].Lexema;
            }

            _tokensExpActual = prevTokensExp;
            if (_tokensExpActual != null && !string.IsNullOrEmpty(lexeme))
            {
                _tokensExpActual.Add(new Token(CodigosToken.Identificador, lexeme, tok.Linea));
            }

            return nombre;
        }

        // lista_var = expre {",", expre}
        private void ListaVar()
        {
            Expre();
            while (ObtenerTokenActual().Codigo == CodigosToken.Coma)
            {
                Avanzar();
                Expre();
            }
        }

        // expre = (termino | var_uso), [op, expre] | datoBool
        private NodoExp? Expre()
        {
            bool esTopLevel = (_tokensExpActual == null);
            if (esTopLevel)
            {
                _tokensExpActual = new List<Token>();
            }

            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.True || codigo == CodigosToken.False)
            {
                Avanzar(); // datoBool
            }
            else
            {
                if (codigo == CodigosToken.ParentesisAbre)
                {
                    Avanzar();
                    Expre();
                    Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                }
                else if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
                {
                    Avanzar();
                    Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                    Palnum();
                    Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                }
                else if (codigo == CodigosToken.EnteroLiteral || codigo == CodigosToken.DecimalLiteral)
                {
                    Avanzar();
                }
                else if (codigo == CodigosToken.Identificador)
                {
                    VarUso();
                }
                else if (codigo == CodigosToken.CadenaLiteral)
                {
                    Avanzar();
                }
                else
                {
                    RegistrarError(ObtenerTokenActual().Linea, $"Símbolo inesperado en expresión: '{ObtenerTokenActual().Lexema}'.");
                    Avanzar();
                }

                // [op, expre]
                int codigoSig = ObtenerTokenActual().Codigo;
                if (codigoSig == CodigosToken.Mas || codigoSig == CodigosToken.Menos || 
                    codigoSig == CodigosToken.Multiplicar || codigoSig == CodigosToken.Dividir || 
                    codigoSig == CodigosToken.Mod)
                {
                    Avanzar(); // op
                    Expre();
                }
            }

            if (esTopLevel)
            {
                var tokens = _tokensExpActual;
                _tokensExpActual = null;

                if (tokens != null)
                {
                    NodoExp? tree = ExpressionParser.Parse(tokens);
                    if (tree != null)
                    {
                        StringBuilder infixSb = new StringBuilder();
                        foreach (var t in tokens)
                        {
                            infixSb.Append(t.Lexema).Append(" ");
                        }
                        string infix = infixSb.ToString().Trim();
                        int line = tokens.Count > 0 ? tokens[0].Linea : ObtenerTokenActual().Linea;

                        string prefijo = tree.ToPrefijo();
                        string posfijo = tree.ToPosfijo();

                        _generadorCodigoIntermedio.RegistrarExpresion(line, infix, prefijo, posfijo);
                    }
                    return tree;
                }
            }

            return null;
        }

        // palnum = identificador | numero
        private void Palnum()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Identificador)
            {
                Identificador();
            }
            else if (codigo == CodigosToken.EnteroLiteral || codigo == CodigosToken.DecimalLiteral)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba un número o identificador, se encontró '{ObtenerTokenActual().Lexema}'.");
            }
        }

        // datoPut = """, textoLib, """, concat | var_uso, [",", "Fore =>", ... ]
        private void DatoPut()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.CadenaLiteral)
            {
                Avanzar();
                Concat();
            }
            else
            {
                Expre();
                if (ObtenerTokenActual().Codigo == CodigosToken.Coma)
                {
                    Avanzar();
                    Emparejar(CodigosToken.Fore, "Se esperaba 'Fore'.");
                    Emparejar(CodigosToken.Flecha, "Se esperaba '=>'.");
                    Numero();

                    Emparejar(CodigosToken.Coma, "Se esperaba ','.");
                    Emparejar(CodigosToken.Aft, "Se esperaba 'Aft'.");
                    Emparejar(CodigosToken.Flecha, "Se esperaba '=>'.");
                    Numero();

                    Emparejar(CodigosToken.Coma, "Se esperaba ','.");
                    Emparejar(CodigosToken.Exp, "Se esperaba 'Exp'.");
                    Emparejar(CodigosToken.Flecha, "Se esperaba '=>'.");
                    Numero();
                }
            }
        }

        // numero = entero | decimal
        private void Numero()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.EnteroLiteral || codigo == CodigosToken.DecimalLiteral)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba un número, se encontró '{ObtenerTokenActual().Lexema}'.");
            }
        }

        // concat = {("&", tipo, "' Image", "(", var_uso, ")" | ... )}
        private void Concat()
        {
            while (ObtenerTokenActual().Codigo == CodigosToken.Ampersand)
            {
                Avanzar();
                int codigo = ObtenerTokenActual().Codigo;
                if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
                {
                    Avanzar();
                    Emparejar(CodigosToken.ComillaSimple, "Se esperaba '''.");
                    Emparejar(CodigosToken.Image, "Se esperaba 'Image'.");
                    Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                    Expre();
                    Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                }
                else if (codigo == CodigosToken.CadenaLiteral)
                {
                    Avanzar();
                }
                else
                {
                    if (ObtenerTokenActual().Codigo == CodigosToken.Identificador && EsRangoSiguiente())
                    {
                        Identificador();
                        Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                        Longitud();
                        Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
                    }
                    else
                    {
                        VarUso();
                    }
                }
            }
        }

        private bool EsRangoSiguiente()
        {
            int idx = _posicionActual;
            while (idx < _listaTokens.Count)
            {
                int codigo = _listaTokens[idx].Codigo;
                if (codigo == CodigosToken.PuntoYComa || codigo == CodigosToken.ParentesisCierra)
                {
                    break;
                }
                if (codigo == CodigosToken.Rango)
                {
                    return true;
                }
                idx++;
            }
            return false;
        }

        // tipo = [("Integer" | "Float" | "Boolean")]
        private void Tipo()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
            {
                Avanzar();
            }
        }

        // cicloPara = "for", identificador, "in", reverso, longitud, cicloLoop
        private void CicloPara()
        {
            Emparejar(CodigosToken.For, "Se esperaba 'for'.");

            // Crear un ámbito temporal para la variable de control del ciclo
            _ambitoActual = new TablaSimbolos(_ambitoActual);

            string? varName = Identificador();
            if (varName != null)
            {
                _ambitoActual.Insertar(new Simbolo(varName, "Integer", true, ObtenerTokenActual().Linea));
            }

            Emparejar(CodigosToken.In, "Se esperaba 'in'.");
            bool reverse = (ObtenerTokenActual().Codigo == CodigosToken.Reverse);
            Reverso();

            // Longitud
            Token lowTok = ObtenerTokenActual();
            Palnum();
            Emparejar(CodigosToken.Rango, "Se esperaba '..'.");
            Token highTok = ObtenerTokenActual();
            Palnum();

            string low = lowTok.Lexema;
            string high = highTok.Lexema;

            string labelStart = _generadorCodigoIntermedio.NuevaEtiqueta();
            string labelExit = _generadorCodigoIntermedio.NuevaEtiqueta();

            string startVal = reverse ? high : low;
            _generadorCodigoIntermedio.Emitir(":=", varName, startVal, null);

            _generadorCodigoIntermedio.Emitir("LABEL", labelStart, null, null);

            string condTemp = _generadorCodigoIntermedio.NuevaVariableTemporal();
            if (reverse)
            {
                _generadorCodigoIntermedio.Emitir(">=", varName, low, condTemp);
            }
            else
            {
                _generadorCodigoIntermedio.Emitir("<=", varName, high, condTemp);
            }

            _generadorCodigoIntermedio.Emitir("JUMP_FALSE", condTemp, labelExit, null);

            // Cuerpo del ciclo
            CicloLoop();

            string nextTemp = _generadorCodigoIntermedio.NuevaVariableTemporal();
            string stepOp = reverse ? "-" : "+";
            _generadorCodigoIntermedio.Emitir(stepOp, varName, "1", nextTemp);
            _generadorCodigoIntermedio.Emitir(":=", varName, nextTemp, null);

            _generadorCodigoIntermedio.Emitir("JUMP", labelStart, null, null);
            _generadorCodigoIntermedio.Emitir("LABEL", labelExit, null, null);

            // Salir del ámbito del ciclo
            if (_ambitoActual != null)
            {
                _ambitoActual = _ambitoActual.Padre;
            }
        }

        // reverso = ["reverse"]
        private void Reverso()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Reverse)
            {
                Avanzar();
            }
        }

        // longitud = palnum, "..", palnum
        private void Longitud()
        {
            Palnum();
            Emparejar(CodigosToken.Rango, "Se esperaba '..'.");
            Palnum();
        }

        // cicloSi = "if", condicion, "then", acciones, entoncesSi, "end if"
        private void CicloSi()
        {
            Emparejar(CodigosToken.If, "Se esperaba 'if'.");
            NodoExp? condTree = Condicion();

            string? condTemp = GenerarCuartetosExpre(condTree);
            string labelFalse = _generadorCodigoIntermedio.NuevaEtiqueta();
            string labelExit = _generadorCodigoIntermedio.NuevaEtiqueta();

            _generadorCodigoIntermedio.Emitir("JUMP_FALSE", condTemp, labelFalse, null);

            Emparejar(CodigosToken.Then, "Se esperaba 'then'.");
            Acciones();

            bool hasElseOrElsif = (ObtenerTokenActual().Codigo == CodigosToken.Elsif || ObtenerTokenActual().Codigo == CodigosToken.Else);
            if (hasElseOrElsif)
            {
                _generadorCodigoIntermedio.Emitir("JUMP", labelExit, null, null);
            }

            _generadorCodigoIntermedio.Emitir("LABEL", labelFalse, null, null);

            EntoncesSi(labelExit);

            if (hasElseOrElsif)
            {
                _generadorCodigoIntermedio.Emitir("LABEL", labelExit, null, null);
            }

            Emparejar(CodigosToken.End, "Se esperaba 'end'.");
            Emparejar(CodigosToken.If, "Se esperaba 'if'.");
        }

        // entoncesSi = {"elsif", condicion, "then", acciones}, ["else", acciones]
        private void EntoncesSi(string labelExit)
        {
            while (ObtenerTokenActual().Codigo == CodigosToken.Elsif)
            {
                Avanzar();
                NodoExp? condTree = Condicion();
                string? condTemp = GenerarCuartetosExpre(condTree);
                string labelNextBranch = _generadorCodigoIntermedio.NuevaEtiqueta();

                _generadorCodigoIntermedio.Emitir("JUMP_FALSE", condTemp, labelNextBranch, null);

                Emparejar(CodigosToken.Then, "Se esperaba 'then'.");
                Acciones();

                _generadorCodigoIntermedio.Emitir("JUMP", labelExit, null, null);
                _generadorCodigoIntermedio.Emitir("LABEL", labelNextBranch, null, null);
            }
            if (ObtenerTokenActual().Codigo == CodigosToken.Else)
            {
                Avanzar();
                Acciones();
            }
        }

        // cicloMientras = "while", condicion, cicloLoop
        private void CicloMientras()
        {
            string labelStart = _generadorCodigoIntermedio.NuevaEtiqueta();
            string labelExit = _generadorCodigoIntermedio.NuevaEtiqueta();

            _generadorCodigoIntermedio.Emitir("LABEL", labelStart, null, null);

            Emparejar(CodigosToken.WhileKw, "Se esperaba 'while'.");
            NodoExp? condTree = Condicion();
            string? condTemp = GenerarCuartetosExpre(condTree);

            _generadorCodigoIntermedio.Emitir("JUMP_FALSE", condTemp, labelExit, null);

            CicloLoop();

            _generadorCodigoIntermedio.Emitir("JUMP", labelStart, null, null);
            _generadorCodigoIntermedio.Emitir("LABEL", labelExit, null, null);
        }

        // condicion ::= {not, casoComp, [opLogico]}
        private NodoExp? Condicion()
        {
            bool esTopLevel = (_tokensExpActual == null);
            if (esTopLevel)
            {
                _tokensExpActual = new List<Token>();
            }

            while (ObtenerTokenActual().Codigo == CodigosToken.Not ||
                   ObtenerTokenActual().Codigo == CodigosToken.True ||
                   ObtenerTokenActual().Codigo == CodigosToken.False ||
                   ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre ||
                   ObtenerTokenActual().Codigo == CodigosToken.Integer ||
                   ObtenerTokenActual().Codigo == CodigosToken.Float ||
                   ObtenerTokenActual().Codigo == CodigosToken.Boolean ||
                   ObtenerTokenActual().Codigo == CodigosToken.EnteroLiteral ||
                   ObtenerTokenActual().Codigo == CodigosToken.DecimalLiteral ||
                   ObtenerTokenActual().Codigo == CodigosToken.Identificador)
            {
                int posAntes = _posicionActual;
                Not();
                CasoComp();
                if (EsOpLogico(out bool esCompuesto))
                {
                    AvanzarOpLogico(esCompuesto);
                }
                if (_posicionActual == posAntes)
                {
                    break;
                }
            }

            if (esTopLevel)
            {
                var tokens = _tokensExpActual;
                _tokensExpActual = null;

                if (tokens != null)
                {
                    NodoExp? tree = ExpressionParser.Parse(tokens);
                    if (tree != null)
                    {
                        StringBuilder infixSb = new StringBuilder();
                        foreach (var t in tokens)
                        {
                            infixSb.Append(t.Lexema).Append(" ");
                        }
                        string infix = infixSb.ToString().Trim();
                        int line = tokens.Count > 0 ? tokens[0].Linea : ObtenerTokenActual().Linea;

                        string prefijo = tree.ToPrefijo();
                        string posfijo = tree.ToPosfijo();

                        _generadorCodigoIntermedio.RegistrarExpresion(line, infix, prefijo, posfijo);
                    }
                    return tree;
                }
            }

            return null;
        }

        private bool EsOpLogico(out bool esCompuesto)
        {
            esCompuesto = false;
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.And)
            {
                if (_posicionActual + 1 < _listaTokens.Count && _listaTokens[_posicionActual + 1].Codigo == CodigosToken.Then)
                {
                    esCompuesto = true;
                    return true;
                }
                return true;
            }
            if (codigo == CodigosToken.Or)
            {
                if (_posicionActual + 1 < _listaTokens.Count && _listaTokens[_posicionActual + 1].Codigo == CodigosToken.Else)
                {
                    esCompuesto = true;
                    return true;
                }
                return true;
            }
            return codigo == CodigosToken.Xor;
        }

        private void AvanzarOpLogico(bool esCompuesto)
        {
            if (esCompuesto)
            {
                Token t1 = ObtenerTokenActual();
                int linea = t1.Linea;
                int codigo = t1.Codigo; // CodigosToken.And o CodigosToken.Or
                string lex = (codigo == CodigosToken.And) ? "and then" : "or else";

                var prevTokens = _tokensExpActual;
                _tokensExpActual = null; // Desactivar la grabación temporalmente
                
                Avanzar(); // consume t1 (and / or)
                Avanzar(); // consume t2 (then / else)
                
                _tokensExpActual = prevTokens;
                if (_tokensExpActual != null)
                {
                    _tokensExpActual.Add(new Token(codigo, lex, linea));
                }
            }
            else
            {
                Avanzar();
            }
        }

        // not = ["not"]
        private void Not()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Not)
            {
                Avanzar();
            }
        }

        // casoComp = expre, op_rela, expre | var_uso | "(", condicion, ")"
        private void CasoComp()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre && EsCondicionParentesis())
            {
                Avanzar(); // (
                Condicion();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            else
            {
                Expre();
                int codigo = ObtenerTokenActual().Codigo;
                if (codigo == CodigosToken.Igual || codigo == CodigosToken.Distinto ||
                    codigo == CodigosToken.MenorQue || codigo == CodigosToken.MayorQue ||
                    codigo == CodigosToken.MenorIgual || codigo == CodigosToken.MayorIgual)
                {
                    Avanzar();
                    Expre();
                }
            }
        }

        private bool EsCondicionParentesis()
        {
            int nivelParentesis = 0;
            int idx = _posicionActual;
            if (_listaTokens[idx].Codigo != CodigosToken.ParentesisAbre) return false;

            while (idx < _listaTokens.Count)
            {
                int codigo = _listaTokens[idx].Codigo;
                if (codigo == CodigosToken.ParentesisAbre)
                {
                    nivelParentesis++;
                }
                else if (codigo == CodigosToken.ParentesisCierra)
                {
                    nivelParentesis--;
                    if (nivelParentesis == 0)
                    {
                        break;
                    }
                }
                else if (nivelParentesis == 1)
                {
                    if (codigo == CodigosToken.Igual || codigo == CodigosToken.Distinto ||
                        codigo == CodigosToken.MenorQue || codigo == CodigosToken.MayorQue ||
                        codigo == CodigosToken.MenorIgual || codigo == CodigosToken.MayorIgual ||
                        codigo == CodigosToken.And || codigo == CodigosToken.Or ||
                        codigo == CodigosToken.Xor)
                    {
                        return true;
                    }
                }
                idx++;
            }
            return false;
        }

        // cicloLoop = "loop", acciones, "end loop"
        private void CicloLoop()
        {
            Emparejar(CodigosToken.Loop, "Se esperaba 'loop'.");
            if (ObtenerTokenActual().Codigo == CodigosToken.PuntoYComa)
            {
                RegistrarError(ObtenerTokenActual().Linea, "No se esperaba ';' inmediatamente después de la palabra reservada 'loop'.");
                Avanzar();
            }
            Acciones();
            Emparejar(CodigosToken.End, "Se esperaba 'end'.");
            Emparejar(CodigosToken.Loop, "Se esperaba 'loop'.");
        }

        // cicloCase = "case", var_uso, "is", ramas, "end case"
        private void CicloCase()
        {
            Emparejar(CodigosToken.Case, "Se esperaba 'case'.");
            VarUso();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Ramas();
            Emparejar(CodigosToken.End, "Se esperaba 'end'.");
            Emparejar(CodigosToken.Case, "Se esperaba 'case'.");
        }

        // ramas = [({rama} | ramaOtros)]
        private void Ramas()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.When)
            {
                if (_posicionActual + 1 < _listaTokens.Count && _listaTokens[_posicionActual + 1].Codigo == CodigosToken.Others)
                {
                    RamaOtros();
                }
                else
                {
                    while (ObtenerTokenActual().Codigo == CodigosToken.When && 
                           !(_posicionActual + 1 < _listaTokens.Count && _listaTokens[_posicionActual + 1].Codigo == CodigosToken.Others))
                    {
                        Rama();
                    }
                    if (ObtenerTokenActual().Codigo == CodigosToken.When)
                    {
                        RamaOtros();
                    }
                }
            }
        }

        // rama = "when", palnum, "=>", acciones
        private void Rama()
        {
            Emparejar(CodigosToken.When, "Se esperaba 'when'.");
            Palnum();
            Emparejar(CodigosToken.Flecha, "Se esperaba '=>'.");
            Acciones();
        }

        // ramaOtros = "when others =>", acciones
        private void RamaOtros()
        {
            Emparejar(CodigosToken.When, "Se esperaba 'when'.");
            Emparejar(CodigosToken.Others, "Se esperaba 'others'.");
            Emparejar(CodigosToken.Flecha, "Se esperaba '=>'.");
            Acciones();
        }        
    }
}