(*
 * Copyright (C) 2009-2023 Lemoine Automation Technologies
 *
 * SPDX-License-Identifier: Apache-2.0
 *)


// Implementation file for parser generated by fsyacc
module Parser
#nowarn "64";; // turn off warnings that type variables used in production annotations are instantiated to concrete type
open FSharp.Text.Lexing
open FSharp.Text.Parsing.ParseHelpers
# 1 "..\Lemoine.StandardGCodesParser\Parser.fsy"

// fsharplint:disable TypeNames
// fsharplint:disable UnionCasesNames
// fsharplint:disable ParameterNames PublicValuesNames

open System
open Lemoine.StandardGCodesParser
open Lemoine.StandardGCodesParser.NcProgram

let mutable eventManager: ParseEventManager option = None

# 18 "Parser.fs"
// This type is the type of tokens accepted by the parser
type token = 
  | FUNC of (string)
  | OP of (string)
  | LBRACKET
  | RBRACKET
  | EOF of (int)
  | NEWLINE of (int)
  | PERCENT
  | ESCAPE
  | VARPREFIX
  | EQSYMB
  | STARTCOMMENT
  | ENDCOMMENT
  | FILE of (string)
  | COMMENT of (string)
  | EXTRA of (string)
  | XCODE of (char)
  | NUMBER of (float)
// This type is used to give symbolic names to token indexes, useful for error messages
type tokenId = 
    | TOKEN_FUNC
    | TOKEN_OP
    | TOKEN_LBRACKET
    | TOKEN_RBRACKET
    | TOKEN_EOF
    | TOKEN_NEWLINE
    | TOKEN_PERCENT
    | TOKEN_ESCAPE
    | TOKEN_VARPREFIX
    | TOKEN_EQSYMB
    | TOKEN_STARTCOMMENT
    | TOKEN_ENDCOMMENT
    | TOKEN_FILE
    | TOKEN_COMMENT
    | TOKEN_EXTRA
    | TOKEN_XCODE
    | TOKEN_NUMBER
    | TOKEN_end_of_input
    | TOKEN_error
// This type is used to give symbolic names to token indexes, useful for error messages
type nonTerminalId = 
    | NONTERM__startstart
    | NONTERM_start
    | NONTERM_header
    | NONTERM_footer
    | NONTERM_optfooterpercent
    | NONTERM_optblocks
    | NONTERM_blocks
    | NONTERM_blockwithend
    | NONTERM_block
    | NONTERM_instructions
    | NONTERM_instruction
    | NONTERM_code
    | NONTERM_optcomments
    | NONTERM_comments
    | NONTERM_comment
    | NONTERM_extra
    | NONTERM_file
    | NONTERM_setvariable
    | NONTERM_value
    | NONTERM_op

// This function maps tokens to integer indexes
let tagOfToken (t:token) = 
  match t with
  | FUNC _ -> 0 
  | OP _ -> 1 
  | LBRACKET  -> 2 
  | RBRACKET  -> 3 
  | EOF _ -> 4 
  | NEWLINE _ -> 5 
  | PERCENT  -> 6 
  | ESCAPE  -> 7 
  | VARPREFIX  -> 8 
  | EQSYMB  -> 9 
  | STARTCOMMENT  -> 10 
  | ENDCOMMENT  -> 11 
  | FILE _ -> 12 
  | COMMENT _ -> 13 
  | EXTRA _ -> 14 
  | XCODE _ -> 15 
  | NUMBER _ -> 16 

// This function maps integer indexes to symbolic token ids
let tokenTagToTokenId (tokenIdx:int) = 
  match tokenIdx with
  | 0 -> TOKEN_FUNC 
  | 1 -> TOKEN_OP 
  | 2 -> TOKEN_LBRACKET 
  | 3 -> TOKEN_RBRACKET 
  | 4 -> TOKEN_EOF 
  | 5 -> TOKEN_NEWLINE 
  | 6 -> TOKEN_PERCENT 
  | 7 -> TOKEN_ESCAPE 
  | 8 -> TOKEN_VARPREFIX 
  | 9 -> TOKEN_EQSYMB 
  | 10 -> TOKEN_STARTCOMMENT 
  | 11 -> TOKEN_ENDCOMMENT 
  | 12 -> TOKEN_FILE 
  | 13 -> TOKEN_COMMENT 
  | 14 -> TOKEN_EXTRA 
  | 15 -> TOKEN_XCODE 
  | 16 -> TOKEN_NUMBER 
  | 19 -> TOKEN_end_of_input
  | 17 -> TOKEN_error
  | _ -> failwith "tokenTagToTokenId: bad token"

/// This function maps production indexes returned in syntax errors to strings representing the non terminal that would be produced by that production
let prodIdxToNonTerminal (prodIdx:int) = 
  match prodIdx with
    | 0 -> NONTERM__startstart 
    | 1 -> NONTERM_start 
    | 2 -> NONTERM_header 
    | 3 -> NONTERM_header 
    | 4 -> NONTERM_header 
    | 5 -> NONTERM_footer 
    | 6 -> NONTERM_footer 
    | 7 -> NONTERM_footer 
    | 8 -> NONTERM_optfooterpercent 
    | 9 -> NONTERM_optfooterpercent 
    | 10 -> NONTERM_optfooterpercent 
    | 11 -> NONTERM_optblocks 
    | 12 -> NONTERM_optblocks 
    | 13 -> NONTERM_blocks 
    | 14 -> NONTERM_blocks 
    | 15 -> NONTERM_blockwithend 
    | 16 -> NONTERM_blockwithend 
    | 17 -> NONTERM_block 
    | 18 -> NONTERM_block 
    | 19 -> NONTERM_instructions 
    | 20 -> NONTERM_instructions 
    | 21 -> NONTERM_instruction 
    | 22 -> NONTERM_instruction 
    | 23 -> NONTERM_instruction 
    | 24 -> NONTERM_instruction 
    | 25 -> NONTERM_instruction 
    | 26 -> NONTERM_code 
    | 27 -> NONTERM_optcomments 
    | 28 -> NONTERM_optcomments 
    | 29 -> NONTERM_comments 
    | 30 -> NONTERM_comments 
    | 31 -> NONTERM_comment 
    | 32 -> NONTERM_extra 
    | 33 -> NONTERM_file 
    | 34 -> NONTERM_setvariable 
    | 35 -> NONTERM_value 
    | 36 -> NONTERM_value 
    | 37 -> NONTERM_value 
    | 38 -> NONTERM_op 
    | 39 -> NONTERM_op 
    | 40 -> NONTERM_op 
    | 41 -> NONTERM_op 
    | _ -> failwith "prodIdxToNonTerminal: bad production index"

let _fsyacc_endOfInputTag = 19 
let _fsyacc_tagOfErrorTerminal = 17

// This function gets the name of a token as a string
let token_to_string (t:token) = 
  match t with 
  | FUNC _ -> "FUNC" 
  | OP _ -> "OP" 
  | LBRACKET  -> "LBRACKET" 
  | RBRACKET  -> "RBRACKET" 
  | EOF _ -> "EOF" 
  | NEWLINE _ -> "NEWLINE" 
  | PERCENT  -> "PERCENT" 
  | ESCAPE  -> "ESCAPE" 
  | VARPREFIX  -> "VARPREFIX" 
  | EQSYMB  -> "EQSYMB" 
  | STARTCOMMENT  -> "STARTCOMMENT" 
  | ENDCOMMENT  -> "ENDCOMMENT" 
  | FILE _ -> "FILE" 
  | COMMENT _ -> "COMMENT" 
  | EXTRA _ -> "EXTRA" 
  | XCODE _ -> "XCODE" 
  | NUMBER _ -> "NUMBER" 

// This function gets the data carried by a token as an object
let _fsyacc_dataOfToken (t:token) = 
  match t with 
  | FUNC _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | OP _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | LBRACKET  -> (null : System.Object) 
  | RBRACKET  -> (null : System.Object) 
  | EOF _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | NEWLINE _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | PERCENT  -> (null : System.Object) 
  | ESCAPE  -> (null : System.Object) 
  | VARPREFIX  -> (null : System.Object) 
  | EQSYMB  -> (null : System.Object) 
  | STARTCOMMENT  -> (null : System.Object) 
  | ENDCOMMENT  -> (null : System.Object) 
  | FILE _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | COMMENT _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | EXTRA _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | XCODE _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
  | NUMBER _fsyacc_x -> Microsoft.FSharp.Core.Operators.box _fsyacc_x 
let _fsyacc_gotos = [| 0us;65535us;1us;65535us;0us;1us;3us;65535us;0us;2us;6us;7us;9us;10us;2us;65535us;3us;4us;17us;18us;2us;65535us;3us;15us;17us;15us;1us;65535us;2us;3us;1us;65535us;2us;22us;2us;65535us;2us;23us;22us;24us;2us;65535us;2us;25us;22us;25us;3us;65535us;2us;28us;22us;28us;29us;30us;2us;65535us;28us;31us;30us;31us;2us;65535us;28us;32us;30us;32us;1us;65535us;12us;13us;5us;65535us;0us;5us;6us;5us;9us;5us;12us;39us;40us;41us;7us;65535us;0us;40us;6us;40us;9us;40us;12us;40us;28us;34us;30us;34us;40us;40us;2us;65535us;28us;35us;30us;35us;2us;65535us;28us;36us;30us;36us;2us;65535us;28us;33us;30us;33us;8us;65535us;37us;38us;45us;46us;47us;48us;50us;51us;52us;55us;59us;55us;60us;55us;62us;55us;4us;65535us;52us;53us;59us;56us;60us;57us;62us;58us;|]
let _fsyacc_sparseGotoTableRowOffsets = [|0us;1us;3us;7us;10us;13us;15us;17us;20us;23us;27us;30us;33us;35us;41us;49us;52us;55us;58us;67us;|]
let _fsyacc_stateToProdIdxsTableElements = [| 1us;0us;1us;0us;1us;1us;1us;1us;2us;1us;7us;1us;2us;1us;2us;1us;2us;1us;3us;1us;3us;1us;3us;1us;4us;1us;4us;1us;4us;1us;4us;2us;5us;10us;1us;5us;1us;6us;2us;6us;7us;1us;7us;1us;9us;1us;10us;2us;12us;14us;1us;13us;1us;14us;2us;15us;16us;1us;15us;1us;16us;2us;17us;20us;1us;18us;2us;18us;20us;1us;20us;1us;21us;1us;22us;1us;23us;1us;24us;1us;25us;1us;26us;1us;26us;1us;28us;2us;29us;30us;1us;30us;1us;31us;1us;32us;1us;33us;1us;34us;1us;34us;1us;34us;1us;34us;1us;35us;1us;36us;1us;36us;1us;37us;3us;37us;39us;40us;1us;37us;1us;38us;3us;39us;39us;40us;3us;39us;40us;40us;3us;39us;40us;41us;1us;39us;1us;40us;1us;41us;1us;41us;1us;41us;|]
let _fsyacc_stateToProdIdxsTableRowOffsets = [|0us;2us;4us;6us;8us;11us;13us;15us;17us;19us;21us;23us;25us;27us;29us;31us;34us;36us;38us;41us;43us;45us;47us;50us;52us;54us;57us;59us;61us;64us;66us;69us;71us;73us;75us;77us;79us;81us;83us;85us;87us;90us;92us;94us;96us;98us;100us;102us;104us;106us;108us;110us;112us;114us;118us;120us;122us;126us;130us;134us;136us;138us;140us;142us;|]
let _fsyacc_action_rows = 64
let _fsyacc_actionTableElements = [|3us;32768us;6us;8us;13us;42us;15us;11us;0us;49152us;6us;16395us;7us;29us;8us;16403us;12us;16403us;13us;16403us;14us;16403us;15us;16403us;2us;16392us;5us;17us;6us;20us;1us;16385us;5us;19us;1us;32768us;5us;6us;3us;32768us;6us;8us;13us;42us;15us;11us;0us;16386us;1us;32768us;5us;9us;3us;32768us;6us;8us;13us;42us;15us;11us;0us;16387us;1us;32768us;16us;12us;1us;16411us;13us;42us;1us;32768us;5us;14us;0us;16388us;2us;32768us;4us;16us;5us;21us;0us;16389us;2us;16392us;5us;17us;6us;20us;1us;16390us;5us;19us;0us;16391us;0us;16393us;0us;16394us;6us;16396us;7us;29us;8us;16403us;12us;16403us;13us;16403us;14us;16403us;15us;16403us;0us;16397us;0us;16398us;2us;32768us;4us;27us;5us;26us;0us;16399us;0us;16400us;5us;16401us;8us;45us;12us;44us;13us;42us;14us;43us;15us;37us;0us;16403us;5us;16402us;8us;45us;12us;44us;13us;42us;14us;43us;15us;37us;0us;16404us;0us;16405us;0us;16406us;0us;16407us;0us;16408us;0us;16409us;3us;32768us;2us;52us;8us;50us;16us;49us;0us;16410us;0us;16412us;1us;16413us;13us;42us;0us;16414us;0us;16415us;0us;16416us;0us;16417us;3us;32768us;2us;52us;8us;50us;16us;49us;1us;32768us;9us;47us;3us;32768us;2us;52us;8us;50us;16us;49us;0us;16418us;0us;16419us;3us;32768us;2us;52us;8us;50us;16us;49us;0us;16420us;4us;32768us;0us;61us;2us;52us;8us;50us;16us;49us;3us;32768us;1us;59us;3us;54us;7us;60us;0us;16421us;0us;16422us;2us;16423us;1us;59us;7us;60us;2us;16424us;1us;59us;7us;60us;3us;32768us;1us;59us;3us;63us;7us;60us;4us;32768us;0us;61us;2us;52us;8us;50us;16us;49us;4us;32768us;0us;61us;2us;52us;8us;50us;16us;49us;1us;32768us;2us;62us;4us;32768us;0us;61us;2us;52us;8us;50us;16us;49us;0us;16425us;|]
let _fsyacc_actionTableRowOffsets = [|0us;4us;5us;12us;15us;17us;19us;23us;24us;26us;30us;31us;33us;35us;37us;38us;41us;42us;45us;47us;48us;49us;50us;57us;58us;59us;62us;63us;64us;70us;71us;77us;78us;79us;80us;81us;82us;83us;87us;88us;89us;91us;92us;93us;94us;95us;99us;101us;105us;106us;107us;111us;112us;117us;121us;122us;123us;126us;129us;133us;138us;143us;145us;150us;|]
let _fsyacc_reductionSymbolCounts = [|1us;3us;3us;3us;4us;2us;2us;2us;0us;1us;2us;0us;1us;1us;2us;2us;2us;1us;2us;0us;2us;1us;1us;1us;1us;1us;2us;0us;1us;1us;2us;1us;1us;1us;4us;1us;2us;3us;1us;3us;3us;4us;|]
let _fsyacc_productionToNonTerminalTable = [|0us;1us;2us;2us;2us;3us;3us;3us;4us;4us;4us;5us;5us;6us;6us;7us;7us;8us;8us;9us;9us;10us;10us;10us;10us;10us;11us;12us;12us;13us;13us;14us;15us;16us;17us;18us;18us;18us;19us;19us;19us;19us;|]
let _fsyacc_immediateActions = [|65535us;49152us;65535us;65535us;65535us;65535us;65535us;16386us;65535us;65535us;16387us;65535us;65535us;65535us;16388us;65535us;16389us;65535us;65535us;16391us;16393us;16394us;65535us;16397us;16398us;65535us;16399us;16400us;65535us;65535us;65535us;16404us;16405us;16406us;16407us;16408us;16409us;65535us;16410us;16412us;65535us;16414us;16415us;16416us;16417us;65535us;65535us;65535us;16418us;16419us;65535us;16420us;65535us;65535us;16421us;16422us;65535us;65535us;65535us;65535us;65535us;65535us;65535us;16425us;|]
let _fsyacc_reductions ()  =    [| 
# 229 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> unit in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
                      raise (FSharp.Text.Parsing.Accept(Microsoft.FSharp.Core.Operators.box _1))
                   )
                 : 'gentype__startstart));
# 238 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_header in
            let _2 = parseState.GetInput(2) :?> 'gentype_optblocks in
            let _3 = parseState.GetInput(3) :?> 'gentype_footer in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 35 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                   
                   )
# 35 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : unit));
# 251 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_comments in
            let _2 = parseState.GetInput(2) :?> int in
            let _3 = parseState.GetInput(3) :?> 'gentype_header in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 39 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                   eventManager.Value.AddHeaderComments _1 
                   )
# 39 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_header));
# 264 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _2 = parseState.GetInput(2) :?> int in
            let _3 = parseState.GetInput(3) :?> 'gentype_header in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 40 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                  
                   )
# 40 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_header));
# 276 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> char in
            let _2 = parseState.GetInput(2) :?> float in
            let _3 = parseState.GetInput(3) :?> 'gentype_optcomments in
            let _4 = parseState.GetInput(4) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 41 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                           
                           begin
                             match _1 with
                             | 'O' ->
                               let block = eventManager.Value.AddHeader _2 _3 in
                               eventManager.Value.NotifyNewBlock block _4
                             | _ ->
                               let instructions = (XCode(_1, Number(_2)))::(_3) in
                               let block = eventManager.Value.AddBlock instructions;
                               eventManager.Value.NotifyNewBlock block _4
                           end
                         
                   )
# 41 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_header));
# 301 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_optfooterpercent in
            let _2 = parseState.GetInput(2) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 56 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                eventManager.Value.NotifyEndOfFile _2 
                   )
# 56 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_footer));
# 313 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> int in
            let _2 = parseState.GetInput(2) :?> 'gentype_footer in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 57 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                          
                   )
# 57 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_footer));
# 325 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_footer in
            let _2 = parseState.GetInput(2) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 58 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                          
                   )
# 58 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_footer));
# 337 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 62 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                           
                   )
# 62 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optfooterpercent));
# 347 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 63 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                   
                   )
# 63 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optfooterpercent));
# 357 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_optfooterpercent in
            let _2 = parseState.GetInput(2) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 64 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                    
                   )
# 64 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optfooterpercent));
# 369 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 68 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                           
                   )
# 68 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optblocks));
# 379 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_blocks in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 69 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                  
                   )
# 69 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optblocks));
# 390 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_blockwithend in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 73 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                        
                   )
# 73 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_blocks));
# 401 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_blocks in
            let _2 = parseState.GetInput(2) :?> 'gentype_blockwithend in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 74 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                               
                   )
# 74 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_blocks));
# 413 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_block in
            let _2 = parseState.GetInput(2) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 78 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                         eventManager.Value.NotifyNewBlock _1 _2 
                   )
# 78 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_blockwithend));
# 425 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_block in
            let _2 = parseState.GetInput(2) :?> int in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 79 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                     eventManager.Value.NotifyNewBlock _1 _2; eventManager.Value.NotifyEndOfFile _2 
                   )
# 79 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_blockwithend));
# 437 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_instructions in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 83 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                        eventManager.Value.AddBlock (List.rev _1) 
                   )
# 83 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_block));
# 448 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _2 = parseState.GetInput(2) :?> 'gentype_instructions in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 84 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                               eventManager.Value.AddEscapedBlock (List.rev _2) 
                   )
# 84 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_block));
# 459 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 88 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                           [] 
                   )
# 88 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instructions));
# 469 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_instructions in
            let _2 = parseState.GetInput(2) :?> 'gentype_instruction in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 89 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                    _2 :: _1 
                   )
# 89 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instructions));
# 481 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_code in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 93 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                _1 
                   )
# 93 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instruction));
# 492 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_setvariable in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 94 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                       _1 
                   )
# 94 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instruction));
# 503 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_comment in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 95 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                   _1 
                   )
# 95 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instruction));
# 514 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_extra in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 96 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                 _1 
                   )
# 96 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instruction));
# 525 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_file in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 97 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                _1 
                   )
# 97 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_instruction));
# 536 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> char in
            let _2 = parseState.GetInput(2) :?> 'gentype_value in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 101 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                       XCode((System.Char.ToUpper _1), _2) 
                   )
# 101 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_code));
# 548 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 105 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                           [] 
                   )
# 105 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optcomments));
# 558 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_comments in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 106 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                    _1 
                   )
# 106 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_optcomments));
# 569 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_comment in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 110 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                   [_1] 
                   )
# 110 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_comments));
# 580 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_comment in
            let _2 = parseState.GetInput(2) :?> 'gentype_comments in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 111 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                            _1 :: _2 
                   )
# 111 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_comments));
# 592 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> string in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 115 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                   Comment(_1) 
                   )
# 115 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_comment));
# 603 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> string in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 119 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                 Extra(_1) 
                   )
# 119 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_extra));
# 614 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> string in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 123 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                File(_1) 
                   )
# 123 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_file));
# 625 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _2 = parseState.GetInput(2) :?> 'gentype_value in
            let _4 = parseState.GetInput(4) :?> 'gentype_value in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 127 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                        SetVariable (_2, _4) 
                   )
# 127 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_setvariable));
# 637 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> float in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 131 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                  Number(_1) 
                   )
# 131 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_value));
# 648 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _2 = parseState.GetInput(2) :?> 'gentype_value in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 132 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                           eventManager.Value.ResolveVariable _2 
                   )
# 132 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_value));
# 659 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _2 = parseState.GetInput(2) :?> 'gentype_op in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 133 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                _2 
                   )
# 133 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_value));
# 670 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_value in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 137 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                 _1 
                   )
# 137 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_op));
# 681 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_op in
            let _2 = parseState.GetInput(2) :?> string in
            let _3 = parseState.GetInput(3) :?> 'gentype_op in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 138 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                    eventManager.Value.ApplyOperator _1 _2 _3 
                   )
# 138 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_op));
# 694 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> 'gentype_op in
            let _3 = parseState.GetInput(3) :?> 'gentype_op in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 139 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                        eventManager.Value.ApplyOperator _1 "/" _3 
                   )
# 139 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_op));
# 706 "Parser.fs"
        (fun (parseState : FSharp.Text.Parsing.IParseState) ->
            let _1 = parseState.GetInput(1) :?> string in
            let _3 = parseState.GetInput(3) :?> 'gentype_op in
            Microsoft.FSharp.Core.Operators.box
                (
                   (
# 140 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                                                     eventManager.Value.ApplyFunction _1 _3 
                   )
# 140 "..\Lemoine.StandardGCodesParser\Parser.fsy"
                 : 'gentype_op));
|]
# 719 "Parser.fs"
let tables : FSharp.Text.Parsing.Tables<_> = 
  { reductions= _fsyacc_reductions ();
    endOfInputTag = _fsyacc_endOfInputTag;
    tagOfToken = tagOfToken;
    dataOfToken = _fsyacc_dataOfToken; 
    actionTableElements = _fsyacc_actionTableElements;
    actionTableRowOffsets = _fsyacc_actionTableRowOffsets;
    stateToProdIdxsTableElements = _fsyacc_stateToProdIdxsTableElements;
    stateToProdIdxsTableRowOffsets = _fsyacc_stateToProdIdxsTableRowOffsets;
    reductionSymbolCounts = _fsyacc_reductionSymbolCounts;
    immediateActions = _fsyacc_immediateActions;
    gotos = _fsyacc_gotos;
    sparseGotoTableRowOffsets = _fsyacc_sparseGotoTableRowOffsets;
    tagOfErrorTerminal = _fsyacc_tagOfErrorTerminal;
    parseError = (fun (ctxt:FSharp.Text.Parsing.ParseErrorContext<_>) -> 
                              match parse_error_rich with 
                              | Some f -> f ctxt
                              | None -> parse_error ctxt.Message);
    numTerminals = 20;
    productionToNonTerminalTable = _fsyacc_productionToNonTerminalTable  }
let engine lexer lexbuf startState = tables.Interpret(lexer, lexbuf, startState)
let start lexer lexbuf : unit =
    engine lexer lexbuf 0 :?> _