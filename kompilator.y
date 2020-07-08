%namespace GardensPoint

%union
{
public string val;
public char type;
public Compiler.SyntaxTree tree;
}

%token Program If Else While Read Write Return Int Double Bool True False Assign Plus Minus Multiplies Divides 
OpenPar ClosePar OpenBracket CloseBracket Semicolon Or And 
Equal NotEqual Greater Lesser GreaterEqual LesserEqual BitOr BitAnd Neg Not Eof Error
%token <val> Ident IntNumber RealNumber String

%type <val> unary_operator bit_operator multiplicative_operator additive_operator relation_operator logic_operator 

%type <type> type

%type <tree> primary_expression  unary_expression bit_expression multiplicative_expression additive_expression relational_expression logical_expression assign_expression expression
if_else_instruction while_instruction declaration block write read instruction instructions

%%
start : Program OpenBracket instructions CloseBracket Eof {Compiler.root = new Compiler.Root($3); YYACCEPT; } 
| Program OpenBracket CloseBracket Eof {Compiler.root = new Compiler.Root(); YYACCEPT; }
| error {Compiler.CallAnError(Compiler.lineNo, "Syntax error"); yyerrok(); YYABORT; } 
| error Eof {Compiler.CallAnError(Compiler.lineNo, "Syntax error"); yyerrok(); YYABORT; };


instructions : instructions instruction {$$ = ((Compiler.InstructionsTree)$1).Add($2, Compiler.lineNo); } | instruction {$$ = new Compiler.InstructionsTree().Add($1, Compiler.lineNo); };

instruction	: Return Semicolon {$$ = new Compiler.InstructionTree(new Compiler.Leaf('r', "return", Compiler.lineNo), Compiler.lineNo); } | declaration {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } 
| write {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } 
| read {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } | expression Semicolon {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } 
| if_else_instruction {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } | while_instruction {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); } | block {$$ = new Compiler.InstructionTree($1, Compiler.lineNo); }
| error {Compiler.CallAnError(Compiler.lineNo, "Syntax error"); yyerrok(); YYABORT; } 
| error Eof {Compiler.CallAnError(Compiler.lineNo, "Syntax error"); yyerrok(); YYABORT; };


write : Write expression Semicolon {$$ = new Compiler.WriteTree(new Compiler.ExpressionTree($2, Compiler.lineNo), Compiler.lineNo); }
| Write String Semicolon {$$ = new Compiler.WriteTree(new Compiler.Leaf('s', $2, Compiler.lineNo), Compiler.lineNo); } ;

read : Read Ident Semicolon {$$ = new Compiler.ReadTree(new Compiler.Leaf('I', $2, Compiler.lineNo), Compiler.lineNo); } ;

declaration : type Ident Semicolon {$$ = new Compiler.Declaration('I', $1, $2, Compiler.lineNo); } ;

if_else_instruction : If OpenPar expression ClosePar instruction {$$ = new Compiler.IfElseTree(Compiler.lineNo, $3, $5); } 
| If OpenPar expression ClosePar instruction Else instruction {$$ = new Compiler.IfElseTree(Compiler.lineNo, $3, $5, $7); } ;

while_instruction : While OpenPar expression ClosePar instruction {$$ = new Compiler.WhileTree($3, $5, Compiler.lineNo); } ; 

block : OpenBracket instructions CloseBracket {$$ = new Compiler.BlockTree($2, Compiler.lineNo); } | OpenBracket CloseBracket {$$ = new Compiler.BlockTree(null, Compiler.lineNo); } ;


type : Int {$$ = 'i'; } | Double {$$ = 'd'; } | Bool {$$ = 'b'; } ;


unary_operator : Minus {$$ = "-"; } | Neg {$$ = "~"; } | Not {$$ = "!"; } | OpenPar Int ClosePar {$$ = "toI"; }| OpenPar Double ClosePar {$$ = "toD"; } ;

bit_operator : BitOr {$$ = "|"; } | BitAnd {$$ = "&"; } ;

multiplicative_operator : Multiplies {$$ = "*"; } | Divides {$$ = "/"; } ;

additive_operator : Plus {$$ = "+"; } | Minus {$$ = "-"; };

relation_operator : Equal {$$ = "=="; } | NotEqual {$$ = "!="; } | Greater {$$ = ">"; } | Lesser {$$ = "<"; } | GreaterEqual {$$ = ">="; } | LesserEqual {$$ = "<="; } ;

logic_operator : Or {$$ = "||"; } | And {$$ = "&&"; } ;


primary_expression : Ident {$$ = new Compiler.Leaf('I', $1, Compiler.lineNo); } | IntNumber {$$ = new Compiler.Leaf('i', $1, Compiler.lineNo); } | RealNumber {$$ = new Compiler.Leaf('d', $1, Compiler.lineNo); } 
| OpenPar expression ClosePar {$$ = $2; }
| True {$$ = new Compiler.Leaf('b', "true", Compiler.lineNo); } | False {$$ = new Compiler.Leaf('b', "false", Compiler.lineNo); } ;

unary_expression : unary_operator unary_expression {$$ = new Compiler.UnaryExpressionTree($2, $1, Compiler.lineNo); }
| primary_expression {$$ = $1; } ; 

bit_expression : unary_expression {$$ = $1; }
| bit_expression bit_operator unary_expression {$$ = new Compiler.BitExpressionTree($1, $2, $3, Compiler.lineNo); } ;

multiplicative_expression : bit_expression {$$ = $1; }
| multiplicative_expression multiplicative_operator bit_expression {$$ = new Compiler.MultiplicativeExpressionTree($1, $2, $3, Compiler.lineNo); } ;

additive_expression : multiplicative_expression {$$ = $1; }
| additive_expression additive_operator multiplicative_expression {$$ = new Compiler.AdditiveExpressionTree($1, $2, $3, Compiler.lineNo); } ;

relational_expression : relational_expression relation_operator additive_expression {$$ = new Compiler.RelationalExpressionTree($1, $2, $3, Compiler.lineNo); }
| additive_expression {$$ = $1; } ;

logical_expression : logical_expression logic_operator relational_expression {$$ = new Compiler.LogicalExpressionTree($1, $2, $3, Compiler.lineNo); } 
| relational_expression {$$ = $1; } ;

assign_expression : Ident Assign assign_expression {$$ = new Compiler.AssignExpressionTree($1, $3, Compiler.lineNo); } | logical_expression {$$ = $1; } ;

expression : assign_expression {$$ = new Compiler.ExpressionTree($1, Compiler.lineNo); } ;	

%%
public Parser(Scanner scanner) : base(scanner) { }