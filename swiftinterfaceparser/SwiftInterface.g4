grammar SwiftInterface ;

// This is an antlr grammar describing the Swift .swiftinterface format
// This is a subset of the Swift grammar itself.
// This grammar was based on https://github.com/antlr/grammars-v4/blob/master/swift/swift3/Swift3.g4
// which is for Swift 3, but I've updated it to match changes in Swift 5
// and fixed the identifier grammar to properly support nosebleed unicode

swiftinterface: statement* ;

statement: 
	import_statement
	| declaration
	| comment
	;


comment : Comment_line ;

declaration: 
	 variable_declaration
	| typealias_declaration
	| function_declaration
	| enum_declaration
	| struct_declaration
	| class_declaration
	| protocol_declaration
	| initializer_declaration
	| deinitializer_declaration
	| extension_declaration
// technically wrong since this can't be at top level
// but the swift compiler will never generate a top-level subscript
// so...
	| subscript_declaration
	| operator_declaration
	| precedence_group_declaration
	;



import_statement: attributes? 'import' import_kind? import_path ;
import_kind : 
	'typealias'
	| 'struct'
	| 'class'
	| 'enum'
	| 'protocol'
	| 'var'
	| 'func'
	;

import_path : import_path_identifier (OpDot import_path_identifier)* ;
import_path_identifier : declaration_identifier ;

variable_declaration: variable_declaration_head variable_name type_annotation getter_setter_keyword_block? ;
variable_declaration_head : attributes? declaration_modifiers? var_clause
	| attributes? declaration_modifiers? let_clause ;
variable_name : declaration_identifier ;

var_clause : 'var';
let_clause : 'let';

getter_setter_keyword_block :
	OpLBrace getter_keyword_clause setter_keyword_clause OpRBrace
	| OpLBrace setter_keyword_clause getter_keyword_clause OpRBrace
	| OpLBrace getter_keyword_clause OpRBrace
	;


getter_keyword_clause : attributes? mutation_modifier? 'get' ;
setter_keyword_clause : attributes? mutation_modifier? 'set' |
	attributes? mutation_modifier? 'set' OpLParen new_value_name OpRParen ;

new_value_name :	declaration_identifier ;

// typealias

typealias_declaration : attributes? access_level_modifier? 'typealias' typealias_name generic_parameter_clause? typealias_assignment ;
typealias_name : declaration_identifier ;
typealias_assignment : OpAssign type ;

// enum

enum_declaration : attributes? access_level_modifier? union_style_enum | attributes? access_level_modifier? raw_value_style_enum  ;

union_style_enum : 'indirect'? 'enum' enum_name generic_parameter_clause? type_inheritance_clause? generic_where_clause? OpLBrace union_style_enum_members? OpRBrace ;

union_style_enum_members : union_style_enum_member union_style_enum_members? ;

union_style_enum_member :
	declaration
	| union_style_enum_case_clause 
	;

union_style_enum_case_clause : attributes? 'indirect'? 'case' union_style_enum_case_list  ;

union_style_enum_case_list :
	union_style_enum_case
	| union_style_enum_case OpComma union_style_enum_case_list
	;

union_style_enum_case : enum_case_name tuple_type? ;

enum_name : declaration_identifier ;

enum_case_name : declaration_identifier ;

raw_value_style_enum : 'enum' enum_name generic_parameter_clause? type_inheritance_clause generic_where_clause? OpLBrace raw_value_style_enum_members OpRBrace ;

raw_value_style_enum_members : raw_value_style_enum_member raw_value_style_enum_members? ;

raw_value_style_enum_member : 
	declaration
	| raw_value_style_enum_case_clause
	;

raw_value_style_enum_case_clause : attributes? 'case' raw_value_style_enum_case_list ;

raw_value_style_enum_case_list : raw_value_style_enum_case | raw_value_style_enum_case OpComma raw_value_style_enum_case_list ;

raw_value_style_enum_case : enum_case_name raw_value_assignment? ;

raw_value_assignment : OpAssign raw_value_literal  ;

raw_value_literal : numeric_literal | Static_string_literal | boolean_literal ;


// struct

struct_declaration : attributes? access_level_modifier? 'struct' struct_name generic_parameter_clause? type_inheritance_clause? generic_where_clause? struct_body  ;
struct_name : declaration_identifier  ;
struct_body : OpLBrace struct_member* OpRBrace  ;

struct_member : declaration ;

// class

class_declaration : 
	attributes? access_level_modifier? final_clause? 'class' class_name generic_parameter_clause? type_inheritance_clause? generic_where_clause? class_body
	| attributes? 'final' access_level_modifier? 'class' class_name generic_parameter_clause? type_inheritance_clause? generic_where_clause? class_body
	;

class_name : declaration_identifier ;
class_body : OpLBrace class_member* OpRBrace ;
class_member : declaration ;
final_clause : 'final' ;

protocol_declaration : attributes? access_level_modifier? 'protocol' protocol_name type_inheritance_clause? protocol_body ;
protocol_name : declaration_identifier ;
protocol_body : OpLBrace protocol_member* OpRBrace ;

protocol_member : 
	protocol_member_declaration
	;

protocol_member_declaration : variable_declaration
	| function_declaration
	| initializer_declaration
	| subscript_declaration
	| protocol_associated_type_declaration
	| typealias_declaration
	;

// operator declaration

operator_declaration : prefix_operator_declaration | postfix_operator_declaration | infix_operator_declaration ;

prefix_operator_declaration : 'prefix' 'operator' operator ;
postfix_operator_declaration : 'postfix' 'operator' operator ;
infix_operator_declaration : 'infix' 'operator' operator infix_operator_group? ;

infix_operator_group : OpColon precedence_group_name ;


// precedence group delcaration

precedence_group_declaration : 'precedencegroup' precedence_group_name OpLBrace precedence_group_attribute* OpRBrace ;

precedence_group_attribute : 
	precedence_group_relation
	| precedence_group_assignment
	| precedence_group_associativity
	;

precedence_group_relation :
	'higherThan' ':' precedence_group_names
	| 'lowerThan' ':' precedence_group_names
	;
 
precedence_group_assignment : 'assignment' ':' boolean_literal ;

precedence_group_associativity : 'associativity' ':' associativity ;
associativity : 'left' | 'right' | 'none' ;

precedence_group_names : precedence_group_name (OpComma precedence_group_name)* ;
precedence_group_name : declaration_identifier ;


// extension

extension_declaration :
	attributes? access_level_modifier? 'extension' type_identifier type_inheritance_clause? extension_body
	| attributes? access_level_modifier? 'extension' type_identifier generic_where_clause extension_body
	;
extension_body : OpLBrace extension_member* OpRBrace ;

extension_member : declaration ;

// subscript

subscript_declaration : 
	subscript_head subscript_result //code_block
	| subscript_head subscript_result getter_setter_keyword_block
	;

subscript_head : attributes? declaration_modifiers? 'subscript' parameter_clause ;
subscript_result : arrow_operator attributes? type ;


// protocol associated type

protocol_associated_type_declaration : attributes? access_level_modifier? 'associatedtype' typealias_name type_inheritance_clause? typealias_assignment? 
	generic_where_clause? ;


function_declaration : function_head function_name generic_parameter_clause? function_signature generic_where_clause? ;

function_head : attributes? declaration_modifiers? 'func' ;
function_name : declaration_identifier | operator_name ;

operator_name : operator ;

function_signature : parameter_clause throws_clause? function_result?
	| parameter_clause rethrows_clause function_result?
	;

throws_clause : 'throws' ;
rethrows_clause : 'rethrows' ;

function_result : arrow_operator attributes? type ;

initializer_declaration :
	initializer_head generic_parameter_clause? parameter_clause throws_clause? generic_where_clause? 
	| initializer_head generic_parameter_clause? parameter_clause rethrows_clause generic_where_clause?
	;

initializer_head :
	attributes? declaration_modifiers? 'init'
	| attributes? declaration_modifiers? 'init' OpQuestion
	| attributes? declaration_modifiers? 'init' OpBang
	;

deinitializer_declaration : attributes? declaration_modifiers? 'deinit' ;

parameter_clause : OpLParen OpRParen | OpLParen parameter_list OpRParen ;
parameter_list : parameter (OpComma parameter)* ;

parameter : 
	external_parameter_name? local_parameter_name type_annotation
	| external_parameter_name? local_parameter_name type_annotation range_operator
	;
external_parameter_name : label_identifier ;
local_parameter_name : label_identifier ;


declaration_identifier : Identifier | keyword_as_identifier_in_declarations ;

type_inheritance_clause :
	OpColon class_requirement OpComma type_inheritance_list
	| OpColon class_requirement
	| OpColon type_inheritance_list
	;

type_inheritance_list :
	type_identifier
	| type_identifier OpComma type_inheritance_list
	;

class_requirement : 'class' ;


attribute : OpAt attribute_name attribute_argument_clause? ;
attribute_name : declaration_identifier ;
attribute_argument_clause : OpLParen balanced_tokens OpRParen;
attributes : attribute+ ;

balanced_tokens : balanced_token* ;
balanced_token :
	OpLParen balanced_tokens OpRParen
	| OpLBracket balanced_tokens OpRBracket
	| OpLBrace balanced_tokens OpRBrace
	| label_identifier
	| literal
	| operator
	| any_punctuation_for_balanced_token
	;

any_punctuation_for_balanced_token :
	( OpDot | OpComma | OpColon | OpSemi | OpAssign | OpAt | OpPound | OpBackTick | OpQuestion )
	| arrow_operator
	;

declaration_modifier : 'class' | 'convenience' | 'dynamic' | 'final'
	| 'infix' | 'lazy' | 'optional' | 'override'
	| 'postfix' | 'prefix' | 'required' | 'static'
	| 'unowned' | 'unowned' '(' 'safe' ')' | 'unowned' '(' 'unsafe' ')' | 'weak'
	| access_level_modifier | mutation_modifier
	;

declaration_modifiers : declaration_modifier+ ;

access_level_modifier : 'private' | 'private' '(' 'set' ')'
	| 'fileprivate' | 'fileprivate' '(' 'set' ')'
	| 'internal' | 'internal' '(' 'set' ')'
	| 'public' | 'public' '(' 'set' ')'
	| 'open' | 'open' '(' 'set' ')'
	;

mutation_modifier : 'mutating' | 'nonmutating' ;


pattern : wildcard_pattern type_annotation?
	| identifier_pattern type_annotation?
	| 'is' type
	| pattern 'as' type
	;

wildcard_pattern : OpUnder ;
identifier_pattern : declaration_identifier ;

function_type :
	attributes? function_type_argument_clause 'throws'? arrow_operator type
	| attributes? function_type_argument_clause 'rethrows' arrow_operator type
	;

function_type_argument_clause : 
	OpLParen OpRParen
	| OpLParen function_type_argument_list range_operator? OpRParen
	;

function_type_argument_list : 
	function_type_argument
	| function_type_argument OpComma function_type_argument_list
	;

function_type_argument : 
	attributes? 'inout'? type
	| argument_label type_annotation
	;

argument_label : label_identifier ;

type : 
	array_type
	| dictionary_type
	| function_type
	| type_identifier
	| tuple_type
	| type OpQuestion
	| type OpBang
	| protocol_composition_type
	| type OpDot 'Type'
	| type OpDot 'Protocol'
	| 'Any'
	| 'Self'
	;

type_annotation : OpColon attributes? inout_clause? type ;

inout_clause : 'inout' ;

type_identifier : type_name generic_argument_clause? (OpDot type_identifier)? ;

type_name : declaration_identifier ;

tuple_type : '(' tuple_type_element_list? ')' ;
tuple_type_element_list : tuple_type_element | tuple_type_element OpComma tuple_type_element_list ;
tuple_type_element : element_name type_annotation | type ;
element_name : label_identifier ;

array_type : OpLBracket type OpRBracket ;

dictionary_type : OpLBracket type OpColon type OpRBracket ;

protocol_composition_type : protocol_identifier (OpAmp protocol_identifier )+ ;
protocol_identifier : type_identifier ;


literal : numeric_literal | string_literal | boolean_literal | nil_literal;

nil_literal : 'nil' ;
boolean_literal : 'true' | 'false' ;
numeric_literal : (OpPlus | OpMinus)? integer_literal ;

integer_literal : Binary_literal
	| Octal_literal
	| Decimal_literal
	| Pure_decimal_digits
	| Hexadecimal_literal ;

Binary_literal : '0b' Binary_digit Binary_literal_characters? ;
fragment Binary_digit : [01] ;
fragment Binary_literal_character : Binary_digit | OpUnder ;
fragment Binary_literal_characters : Binary_literal_character+ ;

Octal_literal : '0o' Octal_digit Octal_literal_characters? ;
fragment Octal_digit : [0-7] ;
fragment Octal_literal_character : Octal_digit | OpUnder ;
fragment Octal_literal_characters : Octal_literal_character + ;

Decimal_literal		: [0-9] [0-9_]* ;
Pure_decimal_digits : [0-9]+ ;

Hexadecimal_literal : '0x' Hexadecimal_digit Hexadecimal_literal_characters? ;
fragment Hexadecimal_digit : [0-7] ;
fragment Hexadecimal_literal_character : Hexadecimal_digit | OpUnder ;
fragment Hexadecimal_literal_characters : Hexadecimal_literal_character + ;


string_literal : Static_string_literal ;

Static_string_literal : '"' Quoted_text? '"' ;
fragment Quoted_text : Quoted_text_item+ ;
fragment Quoted_text_item : Escaped_character | ~["\n\r\\] ;
fragment Escaped_character : '\\' [0\\tnr"']
	| '\\x' Hexadecimal_digit Hexadecimal_digit
	| '\\u' OpLBrace Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit OpRBrace
	| '\\u' OpLBrace Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit Hexadecimal_digit OpRBrace
	;

label_identifier : Identifier | keyword_as_identifier_in_labels ;

Identifier : 
	Identifier_head Identifier_characters?
	| OpBackTick Identifier_head Identifier_characters? OpBackTick
	| Implicit_parameter_name ;

fragment Identifier_head : [a-zA-Z]
	| '_'
	| '\u00A8' | '\u00AA' | '\u00AD' | '\u00AF' | [\u00B2-\u00B5] | [\u00B7-\u00BA]
	| [\u00BC-\u00BE] | [\u00C0-\u00D6] | [\u00D8-\u00F6] | [\u00F8-\u00FF]
	| [\u0100-\u02FF] | [\u0370-\u167F] | [\u1681-\u180D] | [\u180F-\u1DBF]
	| [\u1E00-\u1FFF]
	| [\u200B-\u200D] | [\u202A-\u202E] | [\u203F-\u2040] | '\u2054' | [\u2060-\u206F]
	| [\u2070-\u20CF] | [\u2100-\u218F] | [\u2460-\u24FF] | [\u2776-\u2793]
	| [\u2C00-\u2DFF] | [\u2E80-\u2FFF]
	| [\u3004-\u3007] | [\u3021-\u302F] | [\u3031-\u303F] | [\u3040-\uD7FF]
	| [\uF900-\uFD3D] | [\uFD40-\uFDCF] | [\uFDF0-\uFE1F] | [\uFE30-\uFE44]
	| [\uFE47-\uFFFD]
	| [\u{10000}-\u{1FFFD}] | [\u{20000}-\u{2FFFD}]
	| [\u{30000}-\u{3FFFD}] | [\u{40000}-\u{4FFFD}]
	| [\u{50000}-\u{5FFFD}] | [\u{60000}-\u{6FFFD}]
	| [\u{70000}-\u{7FFFD}] | [\u{80000}-\u{8FFFD}]
	| [\u{90000}-\u{9FFFD}] | [\u{A0000}-\u{AFFFD}]
	| [\u{B0000}-\u{BFFFD}] | [\u{C0000}-\u{CFFFD}]
	| [\u{D0000}-\u{DFFFD}] | [\u{E0000}-\u{EFFFD}]
	;
	
fragment Identifier_character : [0-9]
	| [\u0300-\u036F] | [\u1DC0-\u1DFF] | [\u20D0-\u20FF] | [\uFE20-\uFE2F]
	| Identifier_head
	;

fragment Identifier_characters : Identifier_character+ ;

Implicit_parameter_name : '$' Decimal_digits ;

generic_parameter_clause : OpLess generic_parameter_list OpGreater  ;
generic_parameter_list : generic_parameter (OpComma generic_parameter)*  ;
generic_parameter : type_name
	| type_name OpColon type_identifier
	| type_name OpColon protocol_composition_type
	;

generic_where_clause : 'where' requirement_list ;
requirement_list : requirement (OpComma requirement)*  ;
requirement : conformance_requirement | same_type_requirement  ;

conformance_requirement : type_identifier ':' type_identifier | type_identifier ':' protocol_composition_type  ;
same_type_requirement : type_identifier OpEqEq type  ;

generic_argument_clause : OpLess generic_argument_list OpGreater ;
generic_argument_list : generic_argument (',' generic_argument)* ;
generic_argument : type ;

arrow_operator : '->' ;
range_operator : '...' ;


WS : [ \n\r\t\u000B\u000C\u0000]+ -> skip ;

OpPlus : '+' ;
OpMinus : '-' ;
OpAssign : '=' ;
OpAmp : '&' ;
OpQuestion : '?' ;
OpLess : '<' ;
OpGreater : '>' ;
OpBang : '!' ;
OpDot : '.' ;
OpComma : ',' ;
OpTilde : '~' ;
OpColon : ':' ;
OpSemi : ';' ;
OpAt : '@' ;
OpPound : '#' ;
OpBackTick : '`' ;
OpUnder : '_' ;
OpLParen : '(' ;
OpRParen : ')' ;
OpLBracket : '[' ;
OpRBracket : ']' ;
OpLBrace : '{' ;
OpRBrace : '}' ;
Decimal_digits : [0-9]+ ;

keyword_as_identifier_in_declarations : 'Protocol'
	| 'Type' | 'alpha' | 'arch' | 'arm'
	| 'arm64' | 'assignment' | 'associativity' | 'blue'
	| 'convenience' | 'didSet' | 'dynamic' | 'file'
	| 'final' | 'get' | 'green' | 'higherThan'
	| 'i386' | 'iOS' | 'iOSApplicationExtension' | 'indirect'
	| 'infix' | 'lazy' | 'left' | 'line'
	| 'lowerThan' | 'macOS' | 'macOSApplicationExtension' | 'mutating'
	| 'none' | 'nonmutating' | 'of' | 'open'
	| 'optional' | 'os' | 'override' | 'postfix'
	| 'precedence' | 'prefix' | 'red' | 'required'
	| 'resourceName' | 'right' | 'safe' | 'set'
	| 'swift' | 'tvOS' | 'type' | 'unowned'
	| 'unsafe' | 'watchOS' | 'weak' | 'willSet'
	| 'x86_64'
	;

keyword_as_identifier_in_labels : 'Any' | 'Protocol' | 'Self' | 'Type'
	| 'alpha' | 'arch' | 'arm' | 'arm64'
	| 'as' | 'assignment' | 'associatedtype' | 'associativity'
	| 'blue' | 'break' | 'case' | 'catch'
	| 'class' | 'continue' | 'convenience' | 'default'
	| 'defer' | 'deinit' | 'didSet' | 'do'
	| 'dynamic' | 'else' | 'enum' | 'extension'
	| 'fallthrough' | 'false' | 'file' | 'fileprivate'
	| 'final' | 'for' | 'func' | 'get'
	| 'green' | 'guard' | 'higherThan' | 'i386'
	| 'iOS' | 'iOSApplicationExtension' | 'if' | 'import'
	| 'in' | 'indirect' | 'infix' | 'init'
	| 'internal' | 'is' | 'lazy' | 'left'
	| 'line' | 'lowerThan' | 'macOS' | 'macOSApplicationExtension'
	| 'mutating' | 'nil' | 'none' | 'nonmutating'
	| 'of' | 'open' | 'operator' | 'optional'
	| 'os' | 'override' | 'postfix' | 'precedence'
	| 'precedencegroup' | 'prefix' | 'private' | 'protocol'
	| 'public' | 'red' | 'repeat' | 'required'
	| 'resourceName' | 'rethrows' | 'return' | 'right'
	| 'safe' | 'self' | 'set' | 'static'
	| 'struct' | 'subscript' | 'super' | 'swift'
	| 'switch' | 'throw' | 'throws' | 'true'
	| 'try' | 'tvOS' | 'type' | 'typealias'
	| 'unowned' | 'unsafe' | 'watchOS' | 'weak'
	| 'where' | 'while' | 'willSet' | 'x86_64'
	;
 
operator : Operator ;


Operator :
	OperatorHead OperatorCharacters?
	| DotOperatorHead DotOperatorFollow+;

OperatorHead :
	('/' | '=' | '-' | '+' | '!' | '*' | '%' | '&' | '|' | '<' | '>' | '^' | '~' | '?')
	| [\u00A1-\u00A7]
	| [\u00A9\u00AB] | [\u00AC\u00AE]
	| [\u00B0-\u00B1\u00B6\u00BB\u00BF\u00D7\u00F7]
	| [\u2016-\u2017\u2020-\u2027] | [\u2030-\u203E]
	| [\u2041-\u2053] | [\u2055-\u205E]
	| [\u2190-\u23FF] | [\u2500-\u2775]
	| [\u2794-\u2BFF] | [\u2E00-\u2E7F]
	| [\u3001-\u3003] | [\u3008-\u3020] | [\u3030]
	;

OperatorCharacters : OperatorFollow+;

fragment OperatorFollow :
	('/' | '=' | '-' | '+' | '!' | '*' | '%' | '&' | '|' | '<' | '>' | '^' | '~' | '?')
	| [\u00A1-\u00A7]
	| [\u00A9\u00AB] | [\u00AC\u00AE]
	| [\u00B0-\u00B1\u00B6\u00BB\u00BF\u00D7\u00F7]
	| [\u0300–\u036F]
	| [\u1DC0-\u1DFF] | [\u20D0-\u20FF]
	| [\u2016-\u2017\u2020-\u2027] | [\u2030-\u203E]
	| [\u2041-\u2053] | [\u2055-\u205E]
	| [\u2190-\u23FF] | [\u2500-\u2775]
	| [\u2794-\u2BFF] | [\u2E00-\u2E7F]
	| [\u3001-\u3003] | [\u3008-\u3020] | [\u3030]
	| [\ufe00-\ufe0f] | [\ufe20-\ufe2f]
	| [\u{e0100}-\u{e01ef}]
	;


DotOperatorHead : OpDot ;
DotOperatorFollow : OperatorHead | OperatorFollow ;

OpEqEq : '==' ;


Comment_line: '//' ~[\r\n]* ;

