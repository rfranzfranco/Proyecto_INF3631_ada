using System;
using System.Collections.Generic;

namespace AdaCompilador
{
    public class Token
    {
        public int Codigo { get; set; }
        public string Lexema { get; set; }
        public int Linea { get; set; }

        public Token(int codigo, string lexema, int linea)
        {
            Codigo = codigo;
            Lexema = lexema;
            Linea = linea;
        }

        public override string ToString()
        {
            return $"[{Codigo,-5}] Lexema: \"{Lexema}\", Línea: {Linea}";
        }
    }

    public static class CodigosToken
    {
        // Palabras Reservadas
        public const int Ada = 10;
        public const int Text = 11;
        public const int IO = 12;
        public const int With = 13;
        public const int Use = 15;
        public const int Text_IO = 18;
        public const int Integer_Text_IO = 14;
        public const int Float_Text_IO = 16;
        public const int Procedure = 100;
        public const int Function = 101;
        public const int Is = 102;
        public const int Begin = 103;
        public const int End = 110;
        public const int Return = 111;
        public const int Constant = 112;
        public const int Type = 113;
        public const int Array = 120;
        public const int Of = 125;
        public const int String = 200;
        public const int Natural = 210;
        public const int Integer = 220;
        public const int Float = 230;
        public const int Boolean = 240;
        public const int True = 300;
        public const int False = 310;
        public const int Fore = 400;
        public const int Aft = 410;
        public const int Exp = 420;
        public const int Image = 430;
        public const int If = 500;
        public const int Then = 510;
        public const int Elsif = 520;
        public const int Else = 530;
        public const int Case = 540;
        public const int When = 580;
        public const int Others = 590;
        public const int For = 600;
        public const int In = 610;
        public const int Out = 611;
        public const int Put = 660;
        public const int New = 661;
        public const int LineKw = 662;
        public const int Get = 663;
        public const int Reverse = 664;
        public const int Put_Line = 665;
        public const int New_Line = 666;
        public const int Get_Line = 667;
        public const int Loop = 680;
        public const int WhileKw = 690;
        public const int Exit = 777;
        public const int And = 900;
        public const int Or = 910;
        public const int Not = 920;
        public const int Xor = 930;
        public const int Mod = 940;

        public const int Identificador = 1000;
        public const int EnteroLiteral = 2000;
        public const int DecimalLiteral = 3000;

        public const int Mas = 4010;             // +
        public const int Menos = 4020;           // -
        public const int Multiplicar = 4030;     // *
        public const int Dividir = 4040;         // /
        public const int DosPuntos = 4090;       // :
        public const int Asignacion = 4100;      // :=
        public const int Punto = 4210;           // .
        public const int Rango = 4220;           // ..
        public const int Coma = 4310;            // ,
        public const int GuionBajo = 4320;       // _
        public const int Igual = 4400;           // =
        public const int Flecha = 4450;          // =>
        public const int MenorQue = 4500;        // <
        public const int MayorQue = 4510;        // >
        public const int MenorIgual = 4501;      // <=
        public const int MayorIgual = 4511;      // >=
        public const int Distinto = 4520;        // /=
        public const int Ampersand = 4610;       // &
        public const int ComillaSimple = 4620;   // '
        public const int CadenaLiteral = 4646;   // "..."
        public const int ParentesisAbre = 4701;  // (
        public const int ParentesisCierra = 4702; // )
        public const int CorcheteAbre = 4801;    // [
        public const int CorcheteCierra = 4802;   // ]
        public const int PuntoYComa = 5000;       // ;

        public const int ErrorLexico = -404;

        public static readonly Dictionary<string, int> PalabrasReservadas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ada", Ada },
            { "Text", Text },
            { "IO", IO },
            { "Text_IO", Text_IO },
            { "Integer_Text_IO", Integer_Text_IO },
            { "Float_Text_IO", Float_Text_IO },
            { "with", With },
            { "use", Use },
            { "procedure", Procedure },
            { "function", Function },
            { "is", Is },
            { "begin", Begin },
            { "end", End },
            { "return", Return },
            { "constant", Constant },
            { "type", Type },
            { "array", Array },
            { "of", Of },
            { "String", String },
            { "Natural", Natural },
            { "Integer", Integer },
            { "Float", Float },
            { "Boolean", Boolean },
            { "True", True },
            { "False", False },
            { "Fore", Fore },
            { "Aft", Aft },
            { "Exp", Exp },
            { "Image", Image },
            { "if", If },
            { "then", Then },
            { "elsif", Elsif },
            { "else", Else },
            { "case", Case },
            { "when", When },
            { "others", Others },
            { "for", For },
            { "in", In },
            { "out", Out },
            { "Put", Put },
            { "Put_Line", Put_Line },
            { "New", New },
            { "New_Line", New_Line },
            { "Line", LineKw },
            { "Get", Get },
            { "Get_Line", Get_Line },
            { "reverse", Reverse },
            { "loop", Loop },
            { "while", WhileKw },
            { "exit", Exit },
            { "and", And },
            { "or", Or },
            { "not", Not },
            { "xor", Xor },
            { "mod", Mod }
        };

        public static readonly Dictionary<string, int> Simbolos = new Dictionary<string, int>
        {
            { "+", Mas },
            { "-", Menos },
            { "*", Multiplicar },
            { "/", Dividir },
            { ":", DosPuntos },
            { ":=", Asignacion },
            { ".", Punto },
            { "..", Rango },
            { ",", Coma },
            { "_", GuionBajo },
            { "=", Igual },
            { "=>", Flecha },
            { "<", MenorQue },
            { ">", MayorQue },
            { "<=", MenorIgual },
            { ">=", MayorIgual },
            { "/=", Distinto },
            { "&", Ampersand },
            { "'", ComillaSimple },
            { "(", ParentesisAbre },
            { ")", ParentesisCierra },
            { "[", CorcheteAbre },
            { "]", CorcheteCierra },
            { ";", PuntoYComa }
        };
    }
}