using System;
using System.Collections.Generic;

namespace AdaCompilador
{
    public class ErrorLexico
    {
        public int Linea { get; set; }
        public string Lexema { get; set; }
        public string Descripcion { get; set; }

        public ErrorLexico(int linea, string lexema, string descripcion)
        {
            Linea = linea;
            Lexema = lexema;
            Descripcion = descripcion;
        }

        public override string ToString()
        {
            return $"Error Léxico en Línea {Linea}: {Descripcion} (Lexema: \"{Lexema}\")";
        }
    }

    public class AnalizadorLexico
    {
        private readonly string _codigo;
        private readonly List<Token> _tokens = new List<Token>();
        private readonly List<ErrorLexico> _errores = new List<ErrorLexico>();

        public List<Token> Tokens => _tokens;
        public List<ErrorLexico> Errores => _errores;

        public AnalizadorLexico(string codigo)
        {
            _codigo = codigo;
        }

        public void Analizar()
        {
            _tokens.Clear();
            _errores.Clear();
            string codigoConEspacio = _codigo + " ";
            int i = 0;
            int n = codigoConEspacio.Length;
            int linea = 1;
            int estado = 0;
            
            int lineaInicio = 1;
            string lexema = "";

            while (i < n)
            {
                char c = codigoConEspacio[i];

                switch (estado)
                {
                    case 0:
                        if (c == '\n')
                        {
                            linea++;
                            i++;
                            continue;
                        }
                        if (c == '\r')
                        {
                            i++;
                            continue;
                        }
                        if (char.IsWhiteSpace(c))
                        {
                            i++;
                            continue;
                        }
                        if (c == '-' && i + 1 < n && codigoConEspacio[i + 1] == '-')
                        {
                            i += 2;
                            while (i < n && codigoConEspacio[i] != '\n')
                            {
                                i++;
                            }
                            continue;
                        }
                        lineaInicio = linea;
                        lexema = c.ToString();
                        if (char.IsAsciiLetter(c))
                        {
                            estado = 1;
                            i++;
                        }
                        else if (char.IsAsciiDigit(c))
                        {
                            estado = 2;
                            i++;
                        }
                        else if (c == '+') { estado = 3; i++; }
                        else if (c == '-') { estado = 4; i++; }
                        else if (c == '*') { estado = 5; i++; }
                        else if (c == '/') { estado = 6; i++; }
                        else if (c == '_') { estado = 7; i++; }
                        else if (c == ':') { estado = 8; i++; }
                        else if (c == '.') { estado = 9; i++; }
                        else if (c == ',') { estado = 10; i++; }
                        else if (c == '[') { estado = 11; i++; }
                        else if (c == ']') { estado = 12; i++; }
                        else if (c == '(') { estado = 13; i++; }
                        else if (c == ')') { estado = 14; i++; }
                        else if (c == '>') { estado = 15; i++; }
                        else if (c == '<') { estado = 16; i++; }
                        else if (c == '=') { estado = 19; i++; }
                        else if (c == '&') { estado = 20; i++; }
                        else if (c == ';') { estado = 22; i++; }
                        else if (c == '\'' || c == '‘' || c == '’') { estado = 27; i++; }
                        else if (c == '"' || c == '“' || c == '”') { estado = 23; i++; }
                        else
                        {
                            _errores.Add(new ErrorLexico(linea, c.ToString(), $"Carácter extraño no reconocido: '{c}'"));
                            _tokens.Add(new Token(CodigosToken.ErrorLexico, c.ToString(), linea));
                            i++;
                        }
                        break;

                    case 1:
                        if (char.IsAsciiLetterOrDigit(c) || c == '_')
                        {
                            lexema += c;
                            i++;
                        }
                        else
                        {
                            estado = 28;
                        }
                        break;

                    case 2:
                        if (char.IsAsciiDigit(c))
                        {
                            lexema += c;
                            i++;
                        }
                        else if (c == '.' && i + 1 < n && codigoConEspacio[i + 1] != '.')
                        {
                            lexema += c;
                            i++;
                            estado = 30;
                        }
                        else
                        {
                            estado = 29;
                        }
                        break;

                    case 3:
                        _tokens.Add(new Token(CodigosToken.Mas, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 4:
                        _tokens.Add(new Token(CodigosToken.Menos, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 5:
                        _tokens.Add(new Token(CodigosToken.Multiplicar, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 6:
                        if (c == '=')
                        {
                            lexema += c;
                            i++;
                            estado = 24;
                        }
                        else
                        {
                            estado = 33;
                        }
                        break;

                    case 7:
                        _tokens.Add(new Token(CodigosToken.GuionBajo, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 8:
                        if (c == '=')
                        {
                            lexema += c;
                            i++;
                            estado = 21;
                        }
                        else
                        {
                            estado = 34;
                        }
                        break;

                    case 9:
                        if (c == '.')
                        {
                            lexema += c;
                            i++;
                            estado = 26;
                        }
                        else
                        {
                            estado = 35;
                        }
                        break;

                    case 10:
                        _tokens.Add(new Token(CodigosToken.Coma, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 11:
                        _tokens.Add(new Token(CodigosToken.CorcheteAbre, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 12:
                        _tokens.Add(new Token(CodigosToken.CorcheteCierra, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 13:
                        _tokens.Add(new Token(CodigosToken.ParentesisAbre, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 14:
                        _tokens.Add(new Token(CodigosToken.ParentesisCierra, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 15:
                        if (c == '=')
                        {
                            lexema += c;
                            i++;
                            estado = 17;
                        }
                        else
                        {
                            estado = 36;
                        }
                        break;

                    case 16:
                        if (c == '=')
                        {
                            lexema += c;
                            i++;
                            estado = 18;
                        }
                        else
                        {
                            estado = 37;
                        }
                        break;

                    case 17:
                        _tokens.Add(new Token(CodigosToken.MayorIgual, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 18:
                        _tokens.Add(new Token(CodigosToken.MenorIgual, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 19:
                        if (c == '>')
                        {
                            lexema += c;
                            i++;
                            estado = 25;
                        }
                        else
                        {
                            estado = 38;
                        }
                        break;

                    case 20:
                        _tokens.Add(new Token(CodigosToken.Ampersand, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 21:
                        _tokens.Add(new Token(CodigosToken.Asignacion, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 22:
                        _tokens.Add(new Token(CodigosToken.PuntoYComa, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 23:
                        if (c == '"' || c == '“' || c == '”')
                        {
                            lexema += c;
                            i++;
                            estado = 40;
                        }
                        else if (c == '\n')
                        {
                            _errores.Add(new ErrorLexico(lineaInicio, lexema, "Cadena no finalizada, falta comilla de cierre."));
                            _tokens.Add(new Token(CodigosToken.ErrorLexico, lexema, lineaInicio));
                            estado = 0;
                        }
                        else
                        {
                            lexema += c;
                            i++;
                            estado = 39;
                        }
                        break;

                    case 24:
                        _tokens.Add(new Token(CodigosToken.Distinto, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 25:
                        _tokens.Add(new Token(CodigosToken.Flecha, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 26:
                        _tokens.Add(new Token(CodigosToken.Rango, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 27:
                        _tokens.Add(new Token(CodigosToken.ComillaSimple, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 28:
                        if (CodigosToken.PalabrasReservadas.TryGetValue(lexema, out int codigoPalabra))
                        {
                            _tokens.Add(new Token(codigoPalabra, lexema, lineaInicio));
                        }
                        else
                        {
                            _tokens.Add(new Token(CodigosToken.Identificador, lexema, lineaInicio));
                        }
                        estado = 0;
                        break;

                    case 29:
                        _tokens.Add(new Token(CodigosToken.EnteroLiteral, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 30:
                        if (char.IsAsciiDigit(c))
                        {
                            lexema += c;
                            i++;
                            estado = 31;
                        }
                        else
                        {
                            _errores.Add(new ErrorLexico(lineaInicio, lexema, "Número decimal mal formado."));
                            _tokens.Add(new Token(CodigosToken.ErrorLexico, lexema, lineaInicio));
                            estado = 0;
                        }
                        break;

                    case 31:
                        if (char.IsAsciiDigit(c))
                        {
                            lexema += c;
                            i++;
                        }
                        else
                        {
                            estado = 32;
                        }
                        break;

                    case 32:
                        _tokens.Add(new Token(CodigosToken.DecimalLiteral, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 33:
                        _tokens.Add(new Token(CodigosToken.Dividir, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 34:
                        _tokens.Add(new Token(CodigosToken.DosPuntos, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 35:
                        _tokens.Add(new Token(CodigosToken.Punto, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 36:
                        _tokens.Add(new Token(CodigosToken.MayorQue, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 37:
                        _tokens.Add(new Token(CodigosToken.MenorQue, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 38:
                        _tokens.Add(new Token(CodigosToken.Igual, lexema, lineaInicio));
                        estado = 0;
                        break;

                    case 39:
                        if (c == '"' || c == '“' || c == '”')
                        {
                            lexema += c;
                            i++;
                            estado = 40;
                        }
                        else if (c == '\n')
                        {
                            _errores.Add(new ErrorLexico(lineaInicio, lexema, "Cadena no finalizada, falta comilla de cierre."));
                            _tokens.Add(new Token(CodigosToken.ErrorLexico, lexema, lineaInicio));
                            estado = 0;
                        }
                        else
                        {
                            lexema += c;
                            i++;
                        }
                        break;

                    case 40:
                        _tokens.Add(new Token(CodigosToken.CadenaLiteral, lexema, lineaInicio));
                        estado = 0;
                        break;
                }
            }
        }
    }
}