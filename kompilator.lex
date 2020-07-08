%using QUT.Gppg;
%namespace GardensPoint

IntNumber   ([1-9][0-9]*|0)
RealNumber  ([1-9][0-9]*|0)\.[0-9]+
Ident       ([a-z]|[A-Z])([a-z]*[A-Z]*[0-9]*)*
String		\"([^(\n|\"|\\)]*|(\\n|\\t|\\r|\\\\|\\\"|\\\/)*)*\" 
Comment		"//"[^\n]*

%%

"program"     { return (int)Tokens.Program; }
"if"          { return (int)Tokens.If; }
"else"        { return (int)Tokens.Else; }
"while"       { return (int)Tokens.While; }
"read"        { return (int)Tokens.Read; }
"write"       { return (int)Tokens.Write; }
"return"      { return (int)Tokens.Return; }
"int"         { return (int)Tokens.Int; }
"double"      { return (int)Tokens.Double; }
"bool"        { return (int)Tokens.Bool; }
"true"        { return (int)Tokens.True; }
"false"       { return (int)Tokens.False; }
{IntNumber}   { yylval.val=yytext; return (int)Tokens.IntNumber; }
{RealNumber}  { yylval.val=yytext; return (int)Tokens.RealNumber; }
{Ident}       { yylval.val=yytext; return (int)Tokens.Ident; }
{String}      { yylval.val=yytext; return (int)Tokens.String; }
{Comment}     { }
"="           { return (int)Tokens.Assign; }
"+"           { return (int)Tokens.Plus; }
"-"           { return (int)Tokens.Minus; }
"*"           { return (int)Tokens.Multiplies; }
"/"           { return (int)Tokens.Divides; }
"("           { return (int)Tokens.OpenPar; }
")"           { return (int)Tokens.ClosePar; }
"{"           { return (int)Tokens.OpenBracket; }
"}"           { return (int)Tokens.CloseBracket; }
";"           { return (int)Tokens.Semicolon; }
"||"          { return (int)Tokens.Or; }
"&&"          { return (int)Tokens.And; }
"=="          { return (int)Tokens.Equal; }
"!="          { return (int)Tokens.NotEqual; }
"<"           { return (int)Tokens.Lesser; }
">"           { return (int)Tokens.Greater; }
"<="          { return (int)Tokens.LesserEqual; }
">="          { return (int)Tokens.GreaterEqual; }
"|"           { return (int)Tokens.BitOr; }
"&"           { return (int)Tokens.BitAnd; }
"~"           { return (int)Tokens.Neg; }
"!"           { return (int)Tokens.Not; }
"\r"          { }
"\n"          { Compiler.lineNo++; }
<<EOF>>       { return (int)Tokens.Eof; }
" "           { }
"\t"          { }
.             { return (int)Tokens.Error; }