using System;
using System.Collections.Generic;

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

        public List<ErrorSintactico> Errores => _listaErrores;

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
            _posicionActual = 0;

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

            return _listaErrores.Count == 0;
        }

        #region Métodos de Utilidad y Recuperación

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
                
                // Uso de mayúsculas y minúsculas coincida con la palabra reservada
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
            // Códigos correspondientes a palabras reservadas de 10 a 777, y 900 a 940
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

        #endregion

        #region Reglas de Producción de la Gramática (EBNF)

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
            Identificador();
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
            Identificador();
            FuncEntrada();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Funciones();
            Cuerpo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");
        }

        // coleccion = "type", identificador, "is array", "(", longitud, ")", "of", tipo, ";", variables
        private void Coleccion()
        {
            Emparejar(CodigosToken.Type, "Se esperaba 'type'.");
            Identificador();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Emparejar(CodigosToken.Array, "Se esperaba 'array'.");
            Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
            Longitud();
            Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            Emparejar(CodigosToken.Of, "Se esperaba 'of'.");
            Tipo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");
            Variables();
        }

        // variables = var, {var} (opcional en la práctica)
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
            Identificador();
            if (!Emparejar(CodigosToken.DosPuntos, "Se esperaba ':'. ")) { SincronizarVar(); return; }
            TipoVar();
            if (!Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'. ")) { SincronizarVar(); return; }
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
        private void TipoVar()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.String || codigo == CodigosToken.Natural)
            {
                CadenaVar();
            }
            else if (codigo == CodigosToken.Constant)
            {
                PalConst();
                Tipo();
                ValorIni();
            }
            else if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
            {
                PalConst();
                Tipo();
                ValorIni();
            }
            else if (codigo == CodigosToken.Identificador)
            {
                Identificador();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Tipo de variable inválido: '{ObtenerTokenActual().Lexema}'.");
                Avanzar();
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
        private void TipoSal()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean)
            {
                Tipo();
            }
            else if (codigo == CodigosToken.String || codigo == CodigosToken.Natural)
            {
                CadenaVar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba un tipo de retorno válido, se encontró '{ObtenerTokenActual().Lexema}'.");
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

        // valorIni = [":=", expre]  (Nota: en EBNF dice [expre], pero requiere el operador de asignación)
        private void ValorIni()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Asignacion)
            {
                Avanzar();
                Expre();
            }
        }

        // funcion = "function", identificador, "(", varEntradas, ")", "return", tipoSal, "is", variables, cuerpo, ";"
        private void Funcion()
        {
            Emparejar(CodigosToken.Function, "Se esperaba 'function'.");
            Identificador();
            Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
            VarEntradas();
            Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            Emparejar(CodigosToken.Return, "Se esperaba 'return'.");
            TipoSal();
            Emparejar(CodigosToken.Is, "Se esperaba 'is'.");
            Variables();
            Cuerpo();
            Emparejar(CodigosToken.PuntoYComa, "Se esperaba ';'.");
        }

        // varEntradas = varEnt, ";", {varEnt, ";"}  (Nota: flexibilizado para soportar sin ';' al final)
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
            Identificador();
            Emparejar(CodigosToken.DosPuntos, "Se esperaba ':'.");
            InAux();
            TipoVar();
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
                Expre();
            }
            else if (codigo == CodigosToken.Put_Line)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                DatoPut();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            else if (codigo == CodigosToken.Put)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                DatoPut();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            else if (codigo == CodigosToken.New_Line)
            {
                Avanzar();
            }
            else if (codigo == CodigosToken.Get_Line)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                ListaVar();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            else if (codigo == CodigosToken.Get)
            {
                Avanzar();
                Emparejar(CodigosToken.ParentesisAbre, "Se esperaba '('.");
                VarUso();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
            else if (codigo == CodigosToken.Exit)
            {
                Avanzar();
                Emparejar(CodigosToken.When, "Se esperaba 'when'.");
                Condicion();
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
                // asignacion = var_uso, ":=", expre  |  var_uso (llamada de procedimiento)
                VarUso();
                if (ObtenerTokenActual().Codigo == CodigosToken.Asignacion)
                {
                    Avanzar();
                    Expre();
                }
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Acción sintácticamente no reconocida: '{ObtenerTokenActual().Lexema}'.");
                Avanzar();
            }
        }

        // identificador = letra, resto_id
        private void Identificador()
        {
            if (ObtenerTokenActual().Codigo == CodigosToken.Identificador)
            {
                Avanzar();
            }
            else
            {
                RegistrarError(ObtenerTokenActual().Linea, $"Se esperaba un identificador, se encontró '{ObtenerTokenActual().Lexema}'.");
            }
        }

        // var_uso = identificador, ["(", lista_var, ")"]
        private void VarUso()
        {
            Identificador();
            if (ObtenerTokenActual().Codigo == CodigosToken.ParentesisAbre)
            {
                Avanzar();
                ListaVar();
                Emparejar(CodigosToken.ParentesisCierra, "Se esperaba ')'.");
            }
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
        private void Expre()
        {
            int codigo = ObtenerTokenActual().Codigo;
            if (codigo == CodigosToken.True || codigo == CodigosToken.False)
            {
                Avanzar(); // datoBool
            }
            else
            {
                // termino = palnum | tipo, "(", palnum, ")" | "(", expre, ")"
                // var_uso = identificador, ["(", lista_var, ")"]
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
                    Avanzar(); // palnum -> numero
                }
                else if (codigo == CodigosToken.Identificador)
                {
                    VarUso();
                }
                else if (codigo == CodigosToken.CadenaLiteral)
                {
                    Avanzar(); // Permitir constantes string en expresiones
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
            Identificador();
            Emparejar(CodigosToken.In, "Se esperaba 'in'.");
            Reverso();
            Longitud();
            CicloLoop();
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
            Condicion();
            Emparejar(CodigosToken.Then, "Se esperaba 'then'.");
            Acciones();
            EntoncesSi();
            Emparejar(CodigosToken.End, "Se esperaba 'end'.");
            Emparejar(CodigosToken.If, "Se esperaba 'if'.");
        }

        // entoncesSi = {"elsif", condicion, "then", acciones}, ["else", acciones]
        private void EntoncesSi()
        {
            while (ObtenerTokenActual().Codigo == CodigosToken.Elsif)
            {
                Avanzar();
                Condicion();
                Emparejar(CodigosToken.Then, "Se esperaba 'then'.");
                Acciones();
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
            Emparejar(CodigosToken.WhileKw, "Se esperaba 'while'.");
            Condicion();
            CicloLoop();
        }

        // condicion ::= {not, casoComp, [opLogico]}
        private void Condicion()
        {
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
            Avanzar(); // and / or / xor
            if (esCompuesto)
            {
                Avanzar(); // then / else
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

        #endregion
    }
}