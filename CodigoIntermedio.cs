using System;
using System.Collections.Generic;
using System.Text;

namespace AdaCompilador
{
    public class Cuarteto
    {
        public string Operador { get; set; }
        public string? Operando1 { get; set; }
        public string? Operando2 { get; set; }
        public string? Resultado { get; set; }

        public Cuarteto(string op, string? arg1, string? arg2, string? res)
        {
            Operador = op;
            Operando1 = arg1;
            Operando2 = arg2;
            Resultado = res;
        }

        public override string ToString()
        {
            string op1 = string.IsNullOrEmpty(Operando1) ? "null" : Operando1;
            string op2 = string.IsNullOrEmpty(Operando2) ? "null" : Operando2;
            string res = string.IsNullOrEmpty(Resultado) ? "null" : Resultado;
            return $"({Operador}, {op1}, {op2}, {res})";
        }
    }

    public class ExpresionRegistrada
    {
        public int Linea { get; set; }
        public string Infija { get; set; }
        public string Prefija { get; set; }
        public string Posfija { get; set; }

        public ExpresionRegistrada(int linea, string infija, string prefija, string posfija)
        {
            Linea = linea;
            Infija = infija;
            Prefija = prefija;
            Posfija = posfija;
        }
    }

    public class GeneradorCodigoIntermedio
    {
        private readonly List<Cuarteto> _cuartetos = new List<Cuarteto>();
        private readonly List<ExpresionRegistrada> _expresiones = new List<ExpresionRegistrada>();
        private int _contadorTemporales = 1;
        private int _contadorEtiquetas = 1;

        public string NuevaVariableTemporal()
        {
            return $"R{_contadorTemporales++}";
        }

        public string NuevaEtiqueta()
        {
            return $"L{_contadorEtiquetas++}";
        }

        public void Emitir(string op, string? arg1, string? arg2, string? res)
        {
            _cuartetos.Add(new Cuarteto(op, arg1, arg2, res));
        }

        public void RegistrarExpresion(int linea, string infija, string prefija, string posfija)
        {
            // Evitar duplicados en la misma línea si son idénticos
            if (_expresiones.Exists(e => e.Linea == linea && e.Infija == infija))
                return;
            _expresiones.Add(new ExpresionRegistrada(linea, infija, prefija, posfija));
        }

        public List<Cuarteto> ObtenerCuartetos() => _cuartetos;
        public List<ExpresionRegistrada> ObtenerExpresiones() => _expresiones;

        public void Clear()
        {
            _cuartetos.Clear();
            _expresiones.Clear();
            _contadorTemporales = 1;
            _contadorEtiquetas = 1;
        }
    }

    public class NodoExp
    {
        public string Valor { get; set; }
        public NodoExp? Izq { get; set; }
        public NodoExp? Der { get; set; }
        public bool EsOperador { get; set; }

        public NodoExp(string valor, bool esOperador = false)
        {
            Valor = valor;
            EsOperador = esOperador;
        }

        public string ToPrefijo()
        {
            if (!EsOperador) return Valor;
            string op = TraducirOperador(Valor);
            string izqStr = Izq != null ? Izq.ToPrefijo() : "";
            string derStr = Der != null ? Der.ToPrefijo() : "";
            
            if (string.IsNullOrEmpty(derStr))
                return $"{op} {izqStr}".Trim();
            return $"{op} {izqStr} {derStr}".Trim();
        }

        public string ToPosfijo()
        {
            if (!EsOperador) return Valor;
            string op = TraducirOperador(Valor);
            string izqStr = Izq != null ? Izq.ToPosfijo() : "";
            string derStr = Der != null ? Der.ToPosfijo() : "";
            
            if (string.IsNullOrEmpty(derStr))
                return $"{izqStr} {op}".Trim();
            return $"{izqStr} {derStr} {op}".Trim().Replace("  ", " ");
        }

        private string TraducirOperador(string op)
        {
            // Normalizar los nombres de operadores internos para la salida matemática estándar
            if (op == "unary_minus") return "-";
            if (op == "unary_plus") return "+";
            return op;
        }
    }

    public static class ExpressionParser
    {
        private static int ObtenerPrecedencia(string op, bool esUnario)
        {
            if (esUnario) return 5;
            switch (op.ToLower())
            {
                case "*":
                case "/":
                case "mod":
                    return 4;
                case "+":
                case "-":
                    return 3;
                case "=":
                case "/=":
                case "<":
                case ">":
                case "<=":
                case ">=":
                    return 2;
                case "and":
                case "and then":
                case "or":
                case "or else":
                case "xor":
                    return 1;
                default:
                    return 0; // Funciones / conversiones de tipo
            }
        }

        private static bool EsAsociativoIzquierda(string op)
        {
            return true;
        }

        public static NodoExp? Parse(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0) return null;

            var stackOutput = new Stack<NodoExp>();
            var stackOperators = new Stack<Token>();
            var esUltimoTokenOperador = true;

            for (int i = 0; i < tokens.Count; i++)
            {
                Token tok = tokens[i];

                if (tok.Codigo == CodigosToken.ParentesisAbre)
                {
                    stackOperators.Push(tok);
                    esUltimoTokenOperador = true;
                }
                else if (tok.Codigo == CodigosToken.ParentesisCierra)
                {
                    while (stackOperators.Count > 0 && stackOperators.Peek().Codigo != CodigosToken.ParentesisAbre)
                    {
                        Token opTok = stackOperators.Pop();
                        bool unario = opTok.Lexema == "not" || opTok.Lexema == "unary_plus" || opTok.Lexema == "unary_minus";

                        if (stackOutput.Count == 0) break;

                        if (unario)
                        {
                            var node = new NodoExp(opTok.Lexema, true) { Izq = stackOutput.Pop() };
                            stackOutput.Push(node);
                        }
                        else
                        {
                            if (stackOutput.Count < 2) break;
                            var der = stackOutput.Pop();
                            var izq = stackOutput.Pop();
                            var node = new NodoExp(opTok.Lexema, true) { Izq = izq, Der = der };
                            stackOutput.Push(node);
                        }
                    }

                    if (stackOperators.Count > 0 && stackOperators.Peek().Codigo == CodigosToken.ParentesisAbre)
                    {
                        stackOperators.Pop();
                    }

                    // Comprobar si la parte superior de la pila es una conversión de tipo
                    if (stackOperators.Count > 0 && EsTipoConversion(stackOperators.Peek().Codigo))
                    {
                        Token funcTok = stackOperators.Pop();
                        if (stackOutput.Count > 0)
                        {
                            var node = new NodoExp(funcTok.Lexema, true) { Izq = stackOutput.Pop() };
                            stackOutput.Push(node);
                        }
                    }

                    esUltimoTokenOperador = false;
                }
                else if (EsOperador(tok.Codigo) || tok.Lexema.Equals("mod", StringComparison.OrdinalIgnoreCase))
                {
                    bool esUnario = false;
                    string opLex = tok.Lexema;

                    if (esUltimoTokenOperador)
                    {
                        esUnario = true;
                        if (opLex == "-") opLex = "unary_minus";
                        else if (opLex == "+") opLex = "unary_plus";
                        else if (opLex.Equals("not", StringComparison.OrdinalIgnoreCase)) opLex = "not";
                    }

                    while (stackOperators.Count > 0 && stackOperators.Peek().Codigo != CodigosToken.ParentesisAbre)
                    {
                        Token topTok = stackOperators.Peek();
                        bool topUnario = topTok.Lexema == "not" || topTok.Lexema == "unary_minus" || topTok.Lexema == "unary_plus";
                        int pTop = ObtenerPrecedencia(topTok.Lexema, topUnario);
                        int pCurr = ObtenerPrecedencia(opLex, esUnario);

                        if (pTop > pCurr || (pTop == pCurr && EsAsociativoIzquierda(opLex) && !esUnario))
                        {
                            stackOperators.Pop();
                            if (stackOutput.Count == 0) break;

                            if (topUnario)
                            {
                                var node = new NodoExp(topTok.Lexema, true) { Izq = stackOutput.Pop() };
                                stackOutput.Push(node);
                            }
                            else
                            {
                                if (stackOutput.Count < 2) break;
                                var der = stackOutput.Pop();
                                var izq = stackOutput.Pop();
                                var node = new NodoExp(topTok.Lexema, true) { Izq = izq, Der = der };
                                stackOutput.Push(node);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    stackOperators.Push(new Token(tok.Codigo, opLex, tok.Linea));
                    esUltimoTokenOperador = true;
                }
                else if (EsTipoConversion(tok.Codigo))
                {
                    stackOperators.Push(tok);
                    esUltimoTokenOperador = true;
                }
                else
                {
                    stackOutput.Push(new NodoExp(tok.Lexema));
                    esUltimoTokenOperador = false;
                }
            }

            while (stackOperators.Count > 0)
            {
                Token opTok = stackOperators.Pop();
                if (opTok.Codigo == CodigosToken.ParentesisAbre) continue;

                bool unario = opTok.Lexema == "not" || opTok.Lexema == "unary_minus" || opTok.Lexema == "unary_plus";
                if (stackOutput.Count == 0) break;

                if (unario)
                {
                    var node = new NodoExp(opTok.Lexema, true) { Izq = stackOutput.Pop() };
                    stackOutput.Push(node);
                }
                else
                {
                    if (stackOutput.Count < 2) break;
                    var der = stackOutput.Pop();
                    var izq = stackOutput.Pop();
                    var node = new NodoExp(opTok.Lexema, true) { Izq = izq, Der = der };
                    stackOutput.Push(node);
                }
            }

            return stackOutput.Count > 0 ? stackOutput.Pop() : null;
        }

        private static bool EsOperador(int codigo)
        {
            return codigo == CodigosToken.Mas || codigo == CodigosToken.Menos ||
                   codigo == CodigosToken.Multiplicar || codigo == CodigosToken.Dividir ||
                   codigo == CodigosToken.Igual || codigo == CodigosToken.Distinto ||
                   codigo == CodigosToken.MenorQue || codigo == CodigosToken.MayorQue ||
                   codigo == CodigosToken.MenorIgual || codigo == CodigosToken.MayorIgual ||
                   codigo == CodigosToken.And || codigo == CodigosToken.Or ||
                   codigo == CodigosToken.Xor || codigo == CodigosToken.Not;
        }

        private static bool EsTipoConversion(int codigo)
        {
            return codigo == CodigosToken.Integer || codigo == CodigosToken.Float || codigo == CodigosToken.Boolean;
        }
    }
}