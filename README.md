# Compilador de un Subconjunto de Ada

Este proyecto es un compilador para un subconjunto del lenguaje de programación **Ada**, desarrollado en **C#** utilizando **.NET 10.0**. Este sistema abarca todas las fases principales de la etapa de análisis del front-end (léxico, sintáctico y semántico) y la primera parte de la síntesis del back-end mediante la generación de código intermedio (notación polaca y cuartetos).

El proyecto fue desarrollado para la materia **INF 3631A - Diseño de Compiladores**.

---

## Características y Fases del Compilador

El compilador está estructurado de manera modular y procesa el código fuente de Ada en las siguientes etapas:

### 1. Analizador Léxico (`Lexico.cs`)
* **Modelo formal:** Se basa en un **Autómata Finito Determinista (AFD)** formalizado de **41 estados** ($q_0$ a $q_{40}$) y **27 estados finales/de aceptación**.
* **Reconocimiento:**
  * Palabras reservadas del lenguaje Ada (e.g., `with`, `use`, `procedure`, `is`, `begin`, `end`, `if`, `then`, `else`, `while`, `loop`, `Put_Line`, entre otras).
  * Identificadores, números enteros (`Integer`) y números decimales (`Float`).
  * Operadores aritméticos, lógicos, relacionales y delimitadores de sintaxis.
  * Omisión automática de espacios en blanco, tabulaciones, saltos de línea y comentarios de línea (`--`).
* **Manejo de errores:** Captura los errores léxicos (caracteres no válidos, cadenas mal formadas) reportando con precisión la línea y el lexema en conflicto.

### 2. Analizador Sintáctico (`Sintactico.cs`)
* **Método de análisis:** Implementado mediante **Descenso Recursivo** (Recursive Descent Parser).
* **Especificación:** Las subrutinas del analizador sintáctico modelan fielmente las reglas de la gramática formal del subconjunto de Ada definidas en formato **EBNF** (Extended Backus-Naur Form).
* **Manejo de errores:** Cuenta con sincronización de errores en puntos clave (como delimitadores de sentencias `;` y bloques) para permitir continuar el análisis sintáctico tras encontrar un error.

### 3. Analizador Semántico (`Sintactico.cs` & `Semantico.cs`)
* **Tabla de Símbolos:** Estructura jerárquica con soporte para diferentes ámbitos (ámbitos locales y globales).
* **Validaciones semánticas:**
  * Declaración previa de variables e identificadores.
  * Compatibilidad de tipos en expresiones aritméticas y lógicas (`Integer`, `Float`, `Boolean`, `String`).
  * Validación de definición y uso de constantes (bloquea la reasignación de constantes).
  * Control de firmas y parámetros en bloques.

### 4. Generación de Código Intermedio (`CodigoIntermedio.cs`)
Cuando el código de entrada está libre de errores léxicos, sintácticos y semánticos, el compilador procede a:
* **Conversión de Expresiones:** Transforma expresiones aritméticas de formato infijo a **notación prefija** y **notación posfija (polaca)**.
* **Generación de Cuartetos:** Traduce el flujo de control y las asignaciones en una tabla de código de tres direcciones en formato de **Cuartetos** (estructura: `Operador`, `Operando 1`, `Operando 2`, `Resultado`), ideal para la posterior optimización y generación de código de máquina/ensamblador.

---

## Requisitos Previos

Para compilar y ejecutar este software en tu equipo, debes instalar:

* **SDK de .NET 10.0** (o posterior). Puedes descargarlo desde el sitio oficial de Microsoft: [.NET Downloads](https://dotnet.microsoft.com/download).

---

## Instrucciones de Instalación y Ejecución

Sigue estos pasos para instalar y ejecutar el compilador desde una terminal o consola de comandos:

### 1. Clonar el Repositorio
Clona el repositorio desde GitHub en tu máquina local:
```bash
git clone https://github.com/rfranzfranco/Proyecto_INF3631_ada.git
cd nombre-repositorio
```

### 2. Compilar el Proyecto
Ingresa a la carpeta del proyecto en C# y compílalo usando el CLI de .NET:
```bash
cd Proyecto_INF3631_ada
dotnet build
```

### 3. Ejecutar el Compilador

#### Opción A: Ejecución con el archivo de prueba por defecto
Si ejecutas el proyecto sin argumentos, analizará de forma predeterminada el archivo de pruebas con errores (`prueba_errores.txt`):
```bash
dotnet run
```

#### Opción B: Ejecución especificando un código fuente Ada
Puedes pasar como argumento la ruta de cualquier archivo `.txt` que contenga código escrito en Ada para ser analizado:
```bash
# Ejemplo: Analizar operaciones matemáticas correctas
dotnet run -- ./codigo_fuente_ADA/operaciones_matematicas.txt

# Ejemplo: Analizar la gestión de notas
dotnet run -- ./codigo_fuente_ADA/gestion_notas.txt

# Ejemplo: Analizar el menú del sistema
dotnet run -- ./codigo_fuente_ADA/menu_sistema.txt
```

---

## Formato de Salida del Compilador

Cuando ejecutas el compilador sobre un archivo de código fuente, la consola desplegará un reporte detallado con las siguientes secciones:

1. **Información de Entrada:** Muestra la ruta del archivo Ada cargado.
2. **Tabla de Tokens:** Lista detallada de todos los tokens válidos reconocidos durante la fase de análisis léxico, indicando su código interno, lexema y línea.
3. **Reporte de Errores Léxicos:** Si existieron fallos de formato, se listan por pantalla con su respectiva línea y descripción.
4. **Reporte de Errores Sintácticos:** Reporta cualquier violación en la estructura gramatical (gramática EBNF).
5. **Reporte de Errores Semánticos:** Detalla problemas de incompatibilidad de tipos, variables no declaradas o alteraciones a constantes.
6. **Código Intermedio (Si no hay errores):**
   * **Tabla de Expresiones:** Desglose de cada expresión del programa mostrando su equivalente en notación **infija (original)**, **prefija** y **posfija**.
   * **Tabla de Cuartetos:** Muestra el listado secuencial de los cuartetos generados en formato de tabla formateada (`Operador`, `Operando1`, `Operando2`, `Resultado`).
