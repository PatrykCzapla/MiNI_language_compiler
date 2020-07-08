using System;
using System.IO;
using System.Collections.Generic;

namespace GardensPoint
{
    public class Compiler
    {
        public enum Var_type {integer, real, boolean}; //possible types of variables

        public static int errors = 0; //error count
        public static List<string> errorList = new List<string>(); // error messages

        public static int lineNo = 1; //line number

        private static StreamWriter sw;
        public static List<string> source; //source code divided in lines

        public static Root root; //root of structure tree
        public static bool doneDeclarations = false; //flag informing if is it possible to declare new variables
        public static Dictionary<string, Var_type> variables = new Dictionary<string, Var_type>(); //types of declared variables

        public static int logicCount = 0; //used to keep unique labels in CIL code for logical_expression
        public static int ifCount = 0; //used to keep unique labels in CIL code for if statements
        public static int whileCount = 0; //used to keep unique labels in CIL code for while statements

        public static int Main(string[] args)
        {
            string file;
            FileStream source;
            Console.WriteLine("\nCIL Code Generator for MiNI language - Gardens Point");
            if (args.Length >= 1) //first argument is source file, others are ingored
                file = args[0];
            else //ask for source file
            {
                Console.Write("\nSource file:  ");
                file = Console.ReadLine();
            }
            try
            {
                StreamReader sr = new StreamReader(file);
                string str = sr.ReadToEnd();
                sr.Close();
                Compiler.source = new System.Collections.Generic.List<string>(str.Split(new string[] { "\r\n" }, System.StringSplitOptions.None)); //division of source file into lines
                source = new FileStream(file, FileMode.Open);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
                return 1;
            }
            Scanner scanner = new Scanner(source);
            Parser parser = new Parser(scanner);
            sw = new StreamWriter(file + ".il");
            GenProlog();
            parser.Parse();
            if(errors == 0 && root.CheckType() != 'e') //syntax and types correct
                    root.CreateCode();
            GenEpilog();
            sw.Close();
            source.Close();
            if (errors == 0)
                Console.WriteLine("\nCompilation successful\n");
            else
            {
                Console.WriteLine($"\n  {errors} errors detected\n");
                foreach(string error in errorList)
                    Console.WriteLine(error);
                Console.WriteLine();
                File.Delete(file + ".il");
            }
            return errors == 0 ? 0 : 2;
        }

        public static void EmitCode(string instr = null)
        {
            sw.WriteLine(instr);
        }

        private static void GenProlog()
        {
            EmitCode(".assembly extern mscorlib { }");
            EmitCode(".assembly mini_lang { }");
            EmitCode(".method static void main()");
            EmitCode("{");
            EmitCode(".entrypoint");
            EmitCode(".try");
            EmitCode("{");
            EmitCode("// prolog");
            EmitCode(".maxstack 128");
        }

        private static void GenEpilog()
        {
            EmitCode("}");
            EmitCode("catch [mscorlib]System.Exception");
            EmitCode("{");
            EmitCode("callvirt instance string [mscorlib]System.Exception::get_Message()");
            EmitCode("call void [mscorlib]System.Console::WriteLine(string)");
            EmitCode("leave EndMain");
            EmitCode("}");
            EmitCode("EndMain: ret");
            EmitCode("}");
        }

        public abstract class SyntaxTree
        {
            public char type; //type of node: 'e' - error, 'o' - ok, 's' - string, 'i' - int, 'd' - double, 'r' - return, 'b' - bool, 'I' - ident
            public int line_number = -1; //number of line in code
            public abstract char CheckType(); //check if children have correct types
            public abstract void CreateCode(); //generate CIL code
        }

        public class Root : SyntaxTree
        {
            public InstructionsTree instructions;

            public Root(SyntaxTree instructions = null)
            {
                this.instructions = (InstructionsTree)instructions;
            }

            public override char CheckType()
            {
                if (instructions == null) return 'o';
                if (instructions.CheckType() == 'o') return 'o';
                return 'e';
            }

            public override void CreateCode()
            {
                if (instructions != null) instructions.CreateCode();
                EmitCode("leave EndMain");
            }
        }

        public class Leaf : SyntaxTree
        {
            public string value;

            public Leaf() { }

            public Leaf(char type, string value, int line)
            {
                this.type = type;
                this.value = value;
                line_number = line;
                doneDeclarations = true;
            }

            public override char CheckType()
            {
                if (type == 'I' && !variables.ContainsKey(value))
                {
                    CallAnError(line_number, "Undeclared variable");
                    return 'e';
                }
                return type;
            }

            public override void CreateCode()
            {
                char tmpType = type;
                if (tmpType == 'r') EmitCode("leave EndMain");
                else if (tmpType == 's')
                {
                    EmitCode("ldstr " + value);
                }
                else if (tmpType == 'i') EmitCode("ldc.i4 " + value);
                else if (tmpType == 'd') EmitCode("ldc.r8 " + value);
                else if (tmpType == 'b') EmitCode("ldc.i4." + (value.Equals("true") ? 1 : 0));
                else if (tmpType == 'I')
                {
                    tmpType = FindIndentType(value);
                    if (tmpType == 'i') EmitCode("ldloc " + FindIndexOfVar(value));
                    else if (tmpType == 'd') EmitCode("ldloc " + FindIndexOfVar(value));
                    else if (tmpType == 'b') EmitCode("ldloc " + FindIndexOfVar(value));
                }
            }
        }

        public class Declaration : Leaf
        {
            public char valueType;

            public Declaration(char type, char valueType, string value, int line)
            {
                this.type = type;
                this.valueType = valueType;
                this.value = value;
                line_number = line;
                if (doneDeclarations)
                {
                    CallAnError(line, "Variables must be declared at the beginning");
                }
                else if (variables.ContainsKey(value))
                {
                    CallAnError(line, "Variable already declared");
                }
                else
                {
                    switch(valueType)
                    {
                        case 'i':
                            variables.Add(value, Var_type.integer);
                            break;
                        case 'd':
                            variables.Add(value, Var_type.real);
                            break;
                        case 'b':
                            variables.Add(value, Var_type.boolean);
                            break;
                    }
                }
            }

            public override void CreateCode()
            {
                if (valueType == 'i') EmitCode(".locals init ([" + FindIndexOfVar(value) + "] int32 _" + value + ")");
                else if(valueType == 'd') EmitCode(".locals init ([" + FindIndexOfVar(value) + "] float64 _" + value + ")");
                else EmitCode(".locals init ([" + FindIndexOfVar(value) + "] bool _" + value + ")");
                EmitCode("nop");
            }
        }

        public class InstructionsTree : SyntaxTree
        {
            public List<InstructionTree> instructions = new List<InstructionTree>();

            public InstructionsTree Add(SyntaxTree instruction, int line)
            {
                instructions.Add((InstructionTree)instruction);
                line_number = line;
                return this;
            }

            public override char CheckType()
            {
                bool success = true;
                foreach (InstructionTree tree in instructions)
                {
                    if (tree.CheckType() == 'e') success = false;

                }
                if (success) return 'o';
                else return 'e';
            }

            public override void CreateCode()
            {
                foreach (InstructionTree tree in instructions)
                    tree.CreateCode();
            }
        }

        public class InstructionTree : SyntaxTree
        {
            public SyntaxTree child;

            public InstructionTree(SyntaxTree child, int line)
            {
                this.child = child;
                line_number = line;
            }

            public override char CheckType()
            {
                if (child.CheckType() != 'e') return 'o';
                else return 'e';
            }

            public override void CreateCode()
            {
                child.CreateCode();
                if (child.GetType() == typeof(ExpressionTree)) EmitCode("pop");
            }
        }

        public class WriteTree : SyntaxTree
        {
            public SyntaxTree toWrite;

            public WriteTree(SyntaxTree toWrite, int line)
            {
                this.toWrite = toWrite;
                line_number = line;
            }

            public override char CheckType()
            {
                if(toWrite.CheckType() == 'e') return 'e';
                return 'o';
            }

            public override void CreateCode()
            {
                char tmpType = toWrite.type;
                if (toWrite.type == 'I')
                {
                    tmpType = FindIndentType(((Leaf)((ExpressionTree)((ExpressionTree)toWrite).assign).assign).value);
                }
                if (tmpType == 'd')
                {
                    EmitCode("call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                    EmitCode("ldstr \"{0:0.000000}\"");
                    toWrite.CreateCode();
                    EmitCode("box [mscorlib]System.Double");
                    EmitCode("call string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)");
                    EmitCode("call void [mscorlib]System.Console::Write(string)");
                    return;
                }
                toWrite.CreateCode();
                if (tmpType == 's') EmitCode("call void [mscorlib]System.Console::Write(string)");
                else if (tmpType == 'b') EmitCode("call void [mscorlib]System.Console::Write(bool)");
                else EmitCode("call void [mscorlib]System.Console::Write(int32)");
            }
        }

        public class ReadTree : SyntaxTree
        {
            public Leaf ident;

            public ReadTree(Leaf ident, int line)
            {
                this.ident = ident;
                line_number = line;
            }

            public override char CheckType()
            {
                char tmpType = ident.CheckType();
                if (tmpType == 'e') return 'e';
                if (tmpType != 'I')
                {
                    CallAnError(line_number, "Incorrect type. Must be an identifier");
                    return 'e';
                }
                return 's';
            }

            public override void CreateCode()
            {
                int index = FindIndexOfVar(ident.value);
                EmitCode("call string [mscorlib]System.Console::ReadLine()");
                char tmpType = FindIndentType(ident.value);
                if (tmpType == 'i') EmitCode("call int32 [mscorlib]System.Int32::Parse(string)");
                else if (tmpType == 'd')
                {
                    EmitCode("call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                    EmitCode("call float64 [mscorlib]System.Double::Parse(string, class [mscorlib]System.IFormatProvider)");
                }
                else EmitCode("call bool [mscorlib]System.Boolean::Parse(string)");
                EmitCode("stloc " + index);
            }
        }

        public class BlockTree : SyntaxTree
        {
            public InstructionsTree child;

            public BlockTree(SyntaxTree child, int line)
            {
                this.child = (InstructionsTree)child;
                line_number = line;
            }

            public override char CheckType()
            {
                if (child == null) return 'o';
                if (child.CheckType() == 'e') return 'e';
                return 'o';
            }

            public override void CreateCode()
            {
                if (child != null) child.CreateCode();
            }
        }

        public class IfElseTree : SyntaxTree
        {
            public ExpressionTree expression;
            public InstructionTree ifInstruction;
            public InstructionTree elseInstruction;

            public IfElseTree(int line, SyntaxTree expression, SyntaxTree ifInstruction, SyntaxTree elseInstruction=null)
            {
                line_number = line;
                this.expression = (ExpressionTree)expression;
                this.ifInstruction = (InstructionTree)ifInstruction;
                this.elseInstruction = (InstructionTree)elseInstruction;
            }

            public override char CheckType()
            {
                char tmpType = expression.CheckType();
                if (tmpType == 'I') tmpType = FindIndentType(((Leaf)expression.assign).value);
                if (tmpType == 'e' || ifInstruction.CheckType() == 'e') return 'e';
                if (elseInstruction != null && elseInstruction.CheckType() == 'e') return 'e';
                if (tmpType != 'b')
                {
                    CallAnError(line_number, "Incorrect type of expression. Must be bool");
                    return 'e';
                }
                return 'o';
            }

            public override void CreateCode()
            {
                ifCount++;
                int index = ifCount;
                expression.CreateCode();
                EmitCode("brfalse ELSE" + index);
                ifInstruction.CreateCode();
                EmitCode("br END_IF" + index);
                EmitCode("ELSE" + index + ": nop");
                if (elseInstruction != null) elseInstruction.CreateCode();
                EmitCode("END_IF" + index + ": nop");
            }
        }

        public class WhileTree : SyntaxTree
        {
            public ExpressionTree expression;
            public InstructionTree instruction;

            public WhileTree(SyntaxTree expression, SyntaxTree instruction, int line)
            {
                this.expression = (ExpressionTree)expression;
                this.instruction = (InstructionTree)instruction;
                line_number = line;
            }
            public override char CheckType()
            {
                char tmpType = expression.CheckType();
                if (tmpType == 'I') tmpType = FindIndentType(((Leaf)expression.assign).value);
                if (tmpType == 'e' || instruction.CheckType() == 'e') return 'e';
                if (tmpType != 'b')
                {
                    CallAnError(line_number, "Incorrect type of expression. Must be bool");
                    return 'e';
                }
                return 'o';
            }

            public override void CreateCode()
            {
                whileCount++;
                int index = whileCount;
                EmitCode("WHILE_START" + index + ": nop");
                expression.CreateCode();
                EmitCode("brfalse WHILE_END" + index);
                instruction.CreateCode();
                EmitCode("br WHILE_START" + index);
                EmitCode("WHILE_END" + index + ": nop");

            }
        }

        public class ExpressionTree : SyntaxTree 
        {
            public SyntaxTree assign;

            public ExpressionTree(SyntaxTree assign, int line)
            {
                this.assign = assign;
                line_number = line;
            }

            public override char CheckType()
            {
                type = assign.CheckType();
                return type;
            }

            public override void CreateCode()
            {
                assign.CreateCode();
            }
        }

        public class AssignExpressionTree : SyntaxTree 
        {
            public string ident;
            public SyntaxTree assignExpression;

            public AssignExpressionTree(string ident, SyntaxTree assignExpression, int line)
            {
                this.ident = ident;
                this.assignExpression = assignExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                if (!variables.ContainsKey(ident))
                {
                    CallAnError(line_number, "Undeclared variable");
                    return 'e';
                }
                Var_type variable_type = variables[ident];
                type = assignExpression.CheckType();
                if (type == 'I') type = FindIndentType(((Leaf)assignExpression).value);
                switch (variable_type)
                {
                    case Var_type.real:
                        if (type == 'b')
                        {
                            CallAnError(line_number, "Incorrect type. Cannot be bool");
                            return 'e';
                        }
                        if (type == 'i')
                        {
                            assignExpression = new IntToDouble(assignExpression);
                            type = assignExpression.CheckType();
                            if (type != 'd')
                            {
                                CallAnError(line_number, "Incorrect type. Must be double");
                                return 'e';
                            }
                        }
                        break;
                    case Var_type.integer:
                        if (type != 'i')
                        {
                            CallAnError(line_number, "Incorrect type. Must be int");
                            return 'e';
                        }
                        break;
                    case Var_type.boolean:
                        if (type != 'b')
                        {
                            CallAnError(line_number, "Incorrect type. Must be bool");
                            return 'e';
                        }
                        break;
                }
                return type;
            }

            public override void CreateCode()
            {
                assignExpression.CreateCode();
                int index = FindIndexOfVar(ident);
                EmitCode("stloc " + index);
                EmitCode("ldloc " + index);
            }
        }

        public class LogicalExpressionTree : SyntaxTree
        {
            public SyntaxTree logicalExpression;
            public string op;
            public SyntaxTree relationalExpression;

            public LogicalExpressionTree(SyntaxTree logicalExpression, string op, SyntaxTree relationalExpression, int line)
            {
                this.logicalExpression = logicalExpression;
                this.op = op;
                this.relationalExpression = relationalExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                type = relationalExpression.CheckType();
                if (type == 'I') type = FindIndentType(((Leaf)relationalExpression).value);
                char type2 = logicalExpression.CheckType();
                if (type2 == 'I') type2 = FindIndentType(((Leaf)logicalExpression).value);
                if (type == 'e' || type2 == 'e') return 'e';
                if (type != 'b' || type2 != 'b')
                {
                    CallAnError(line_number, "Incorrect type. Must be bool");
                    return 'e';
                }
                return type;
            }

            public override void CreateCode()
            {
                logicCount++;
                int index = logicCount;
                logicalExpression.CreateCode();
                if (op == "||")
                {
                    EmitCode("ldc.i4 1");
                    EmitCode("beq LOG_EX_T" + index);
                    relationalExpression.CreateCode();
                    EmitCode("ldc.i4 1");
                    EmitCode("beq LOG_EX_T" + index);
                    EmitCode("ldc.i4 0");
                    EmitCode("br LOG_EX_noOp" + index);
                    EmitCode("LOG_EX_T" + index + ": ldc.i4 1");
                    EmitCode("LOG_EX_noOp" + index + ": nop");
                }
                else
                {
                    EmitCode("ldc.i4 0");
                    EmitCode("beq LOG_EX_F" + index);
                    relationalExpression.CreateCode();
                    EmitCode("ldc.i4 0");
                    EmitCode("beq LOG_EX_F" + index);
                    EmitCode("ldc.i4 1");
                    EmitCode("br LOG_EX_noOp" + index);
                    EmitCode("LOG_EX_F" + index + ": ldc.i4 0");
                    EmitCode("LOG_EX_noOp" + index + ": nop");
                }
                
            }
        }

        public class RelationalExpressionTree : SyntaxTree
        {
            public SyntaxTree relationalExpression;
            public string op;
            public SyntaxTree additiveExpression;

            public RelationalExpressionTree(SyntaxTree relationalExpression, string op, SyntaxTree additiveExpression, int line)
            {
                this.relationalExpression = relationalExpression;
                this.op = op;
                this.additiveExpression = additiveExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                char type1 = additiveExpression.CheckType();
                if (type1 == 'I') type1 = FindIndentType(((Leaf)additiveExpression).value);
                char type2 = relationalExpression.CheckType();
                if (type2 == 'I') type2 = FindIndentType(((Leaf)relationalExpression).value);
                if (type1 == 'e' || type2 == 'e') return 'e';
                if(op.Equals("==") || op.Equals("!="))
                {
                    if((type1 == 'b' && type2 != type1) || (type2 == 'b' && type1 != type2))
                    {
                        CallAnError(line_number, "Incorrect types");
                        return 'e';
                    }
                }
                else
                {
                    if(type1 == 'b' || type2 == 'b')
                    {
                        CallAnError(line_number, "Incorrect types");
                        return 'e';
                    }
                }
                if (type1 != type2)
                {
                    if (type1 == 'd')
                    {
                        relationalExpression = new IntToDouble(relationalExpression);
                        type2 = relationalExpression.CheckType();
                    }
                    else
                    {
                        additiveExpression = new IntToDouble(additiveExpression);
                        type1 = additiveExpression.CheckType();
                    }
                    if (type1 != type2)
                    {
                        CallAnError(line_number, "Incorrect type. Both arguments of operand must be of the same type.");
                        return 'e';
                    }
                }
                type = 'b';
                return 'b';
            }

            public override void CreateCode()
            {
                relationalExpression.CreateCode();
                additiveExpression.CreateCode();
                switch(op)
                {
                    case "<":
                        EmitCode("clt");
                        break;
                    case "<=":
                        EmitCode("cgt");
                        EmitCode("ldc.i4 0");
                        EmitCode("ceq");
                        break;
                    case ">":
                        EmitCode("cgt");
                        break;
                    case ">=":
                        EmitCode("clt");
                        EmitCode("ldc.i4 0");
                        EmitCode("ceq");
                        break;
                    case "==":
                        EmitCode("ceq");
                        break;
                    case "!=":
                        EmitCode("ceq");
                        EmitCode("ldc.i4 0");
                        EmitCode("ceq");
                        break;
                }
            }
        }

        public class AdditiveExpressionTree : SyntaxTree
        {
            public SyntaxTree additiveExpression;
            public string op;
            public SyntaxTree multiplicativeExpression;

            public AdditiveExpressionTree(SyntaxTree additiveExpression, string op, SyntaxTree multiplicativeExpression, int line)
            {
                this.additiveExpression = additiveExpression;
                this.op = op;
                this.multiplicativeExpression = multiplicativeExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                char type1 = additiveExpression.CheckType();
                if (type1 == 'I') type1 = FindIndentType(((Leaf)additiveExpression).value);
                char type2 = multiplicativeExpression.CheckType();
                if (type2 == 'I') type2 = FindIndentType(((Leaf)multiplicativeExpression).value);
                if (type1 == 'b' || type2 == 'b')
                {
                    CallAnError(line_number, "Incorrect type. Cannot be bool");
                    return 'e';
                }
                if (type1 == type2) type = type1;
                else
                {
                    if (type1 == 'd')
                    {
                        multiplicativeExpression = new IntToDouble(multiplicativeExpression);
                        type2 = multiplicativeExpression.CheckType();
                    }
                    else
                    {
                        additiveExpression = new IntToDouble(additiveExpression);
                        type1 = additiveExpression.CheckType();
                    }
                    if (type1 != type2)
                    {
                        CallAnError(line_number, "Incorrect type. Both arguments of operand must be of the same type.");
                        return 'e';
                    }
                    type = 'd';
                }
                return type;
            }

            public override void CreateCode()
            {
                additiveExpression.CreateCode();
                multiplicativeExpression.CreateCode();
                if (op == "+") EmitCode("add");
                else EmitCode("sub");
            }
        }

        public class MultiplicativeExpressionTree : SyntaxTree
        {
            public SyntaxTree multiplicativeExpression;
            public string op;
            public SyntaxTree bitExpression;

            public MultiplicativeExpressionTree(SyntaxTree multiplicativeExpression, string op, SyntaxTree bitExpression, int line)
            {
                this.multiplicativeExpression = multiplicativeExpression;
                this.op = op;
                this.bitExpression = bitExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                char type1 = multiplicativeExpression.CheckType();
                if (type1 == 'I') type1 = FindIndentType(((Leaf)multiplicativeExpression).value);
                char type2 = bitExpression.CheckType();
                if (type2 == 'I') type2 = FindIndentType(((Leaf)bitExpression).value);
                if (type1 == 'b' || type2 == 'b')
                {
                    CallAnError(line_number, "Incorrect type. Cannot be bool");
                    return 'e';
                }
                if (type1 == type2) type = type1;
                else
                {
                    if (type1 == 'd')
                    {
                        bitExpression = new IntToDouble(bitExpression);
                        type2 = bitExpression.CheckType();
                    }
                    else
                    {
                        multiplicativeExpression = new IntToDouble(multiplicativeExpression);
                        type1 = multiplicativeExpression.CheckType();
                    }
                    if (type1 != type2)
                    {
                        CallAnError(line_number, "Incorrect type. Both arguments of operand must be of the same type.");
                        return 'e';
                    }
                    type = 'd';
                }
                return type;
            }

            public override void CreateCode()
            {
                multiplicativeExpression.CreateCode();
                bitExpression.CreateCode();
                if (op == "*") EmitCode("mul");
                else EmitCode("div");
            }
        }

        public class BitExpressionTree : SyntaxTree
        {
            public SyntaxTree bitExpression;
            public string op;
            public SyntaxTree unaryExpression;


            public BitExpressionTree(SyntaxTree bitExpression, string op, SyntaxTree unaryExpression, int line)
            {
                this.bitExpression = bitExpression;
                this.op = op;
                this.unaryExpression = unaryExpression;
                line_number = line;
            }

            public override char CheckType()
            {
                char type1 = unaryExpression.CheckType();
                if (type1 == 'I') type1 = FindIndentType(((Leaf)unaryExpression).value);
                char type2 = bitExpression.CheckType();
                if (type2 == 'I') type2 = FindIndentType(((Leaf)bitExpression).value);
                if (type1 != 'i' || type2 != 'i')
                {
                    CallAnError(line_number, "Incorrect type. Must be int");
                    return 'e';
                }
                type = 'i';
                return type;
            }

            public override void CreateCode()
            {
                bitExpression.CreateCode();
                unaryExpression.CreateCode();
                if (op == "|") EmitCode("or");
                else EmitCode("and");
            }
        }

        public class UnaryExpressionTree : SyntaxTree
        {
            public SyntaxTree expression;
            public string op;
            
            public UnaryExpressionTree(SyntaxTree expression, string op, int line)
            {
                this.expression = expression;
                this.op = op;
                line_number = line;
            }

            public override char CheckType()
            {
                type = expression.CheckType();
                if (type == 'e') return 'e';
                if (type == 'I') type = FindIndentType(((Leaf)expression).value);
                switch (op)
                {
                    case "-":
                        if(type == 'b')
                        {
                            CallAnError(line_number, "Incorrect type. Cannot be bool");
                            return 'e';
                        }
                        break;
                    case "~":
                        if (type != 'i')
                        {
                            CallAnError(line_number, "Incorrect type. Must be int");
                            return 'e';
                        }
                        break;
                    case "!":
                        if (type != 'b')
                        {
                            CallAnError(line_number, "Incorrect type. Must be bool");
                            return 'e';
                        }
                        break;
                    case "toI":
                        type = 'i';
                        break;
                    case "toD":
                        type = 'd';
                        break;
                }
                return type;
            }

            public override void CreateCode()
            {
                expression.CreateCode();
                switch (op)
                {
                    case "-":
                        EmitCode("neg");
                        break;
                    case "~":
                        EmitCode("not");
                        break;
                    case "!":
                        EmitCode("ldc.i4 0");
                        EmitCode("ceq");
                        break;
                    case "toI":
                        EmitCode("conv.i4");
                        break;
                    case "toD":
                        EmitCode("conv.r8");
                        break;
                }
            }
        }

        public class IntToDouble : SyntaxTree
        {
            public SyntaxTree child;

            public IntToDouble(SyntaxTree child)
            {
                type = 'd';
                this.child = child;
            }

            public override char CheckType()
            {
                char childType = child.CheckType();
                if (childType == 'I') childType = FindIndentType(((Leaf)child).value);
                if (childType != 'i')
                {
                    CallAnError(line_number, "Cannot cast to double");
                    return 'e';
                }
                return type;
            }

            public override void CreateCode()
            {
                child.CreateCode();
                EmitCode("conv.r8");
            }
        }

        /// <summary>
        /// Writes error message to console and increments error count
        /// </summary>
        /// <param name="line"></param>
        /// <param name="message"></param>
        public static void CallAnError(int line, string message)
        {
            errors++;
            errorList.Add("Line " + line + ": " + message + ".");
        }

        /// <summary>
        /// Find type of variables with ident name
        /// </summary>
        /// <param name="ident"></param>
        /// <returns>type of ident</returns>
        private static char FindIndentType(string ident)
        {
            Var_type type = variables[ident];
            switch(type)
            {
                case Var_type.integer:
                    return 'i';
                case Var_type.real:
                    return 'd';
                case Var_type.boolean:
                    return 'b';
                default:
                    return ' ';
            }
        }

        /// <summary>
        /// Finds index of variable ident in list of variables
        /// </summary>
        /// <param name="ident"></param>
        /// <returns>index of ident</returns>
        private static int FindIndexOfVar(string ident)
        {
            int index = 0;
            foreach(string var in variables.Keys)
            {
                if (var == ident) return index;
                index++;
            }
            return -1;
        }
    }
}