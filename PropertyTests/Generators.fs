﻿(*
 * Copyright (C) 2016-2018 Ian Spratt <ian144@hotmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301, USA.
 *
 *)
 


module Generators



open FsCheck

open Fix44.CompoundItems

open DateTimeGenerators


// strings stored in FIX do not contain field terminators, 
// let genAlphaChar = Gen.choose(32,255) |> Gen.map char 
let genAlphaChar = Gen.choose(65,90) |> Gen.map char 
//let genAlphaCharArray = Gen.arrayOfLength 16 genAlphaChar 
let genAlphaString = 
        gen{
            let! len = Gen.choose(4, 8)
            let! chars = Gen.arrayOfLength len genAlphaChar
            return System.String chars
        }

let genChar = Gen.choose(62,127) |> Gen.map char 


let genByteMain = Gen.choose(0, 255) |> Gen.map byte
let genByteFieldSeperator = Gen.constant 1uy
let genByteTagValueSeperator = Gen.constant 61uy


// used to generate byte arrays that could concievably be ASCII encoded, required for quickfix echo testing
let genNonFieldValueSeperatorByte = Gen.choose(64, 90) |> Gen.map byte

// used to generate byte arrays with lots of tag-value and field seperators
let genRawByte = Gen.frequency[ 4, genNonFieldValueSeperatorByte; 1, genByteFieldSeperator; 1, genByteTagValueSeperator ]



let genNonEmptyByteArray = 
    gen{
        let! len = Gen.choose(1, 64)
        let! bytes = Gen.arrayOfLength len genNonFieldValueSeperatorByte
        return NonEmptyByteArray.Make bytes
    }


// adapted from fsCheck code
let genDecimal15dp =
    gen {
        let! lo = Arb.generate
        let! mid = Arb.generate
        let! hi = Arb.generate
        let! isNegative = Arb.generate
        let! scale = Gen.choose(0, 28) |> Gen.map byte
        let d1 = System.Decimal(lo, mid, hi, isNegative, scale)
        return System.Math.Round(d1, 15)
    }


let genCurrency = 
    gen {
        let! ss = Gen.elements [ "GBP"; "USD"; "EUR"; "SGD" ]
        return Fix44.Fields.Currency ss
    }
    

let genCountry = 
    gen {
       let! ss = Gen.elements [ "AD"; "AE"; "AF"; "AG"; "AI"; "AL"; "AM"; "AO"; "AQ"; "AR"; "AS"; "AT"; "AU"; "AW"; "AX"; "AZ"; "BA"; "BB"; "BD"; "BE"; "BF"; "BG"; "BH"; "BI"; "BJ"; "BL"; "BM"; "BN"; "BO"; "BQ"; "BR"; "BS"; "BT"; "BV"; "BW"; "BY"; "BZ"; "CA"; "CC"; "CD"; "CF"; "CG"; "CH"; "CI"; "CK"; "CL"; "CM"; "CN"; "CO"; "CR"; "CU"; "CV"; "CW"; "CX"; "CY"; "CZ"; "DE"; "DJ"; "DK"; "DM"; "DO"; "DZ"; "EC"; "EE"; "EG"; "EH"; "ER"; "ES"; "ET"; "FI"; "FJ"; "FK"; "FM"; "FO"; "FR"; "GA"; "GB"; "GD"; "GE"; "GF"; "GG"; "GH"; "GI"; "GL"; "GM"; "GN"; "GP"; "GQ"; "GR"; "GS"; "GT"; "GU"; "GW"; "GY"; "HK"; "HM"; "HN"; "HR"; "HT"; "HU"; "ID"; "IE"; "IL"; "IM"; "IN"; "IO"; "IQ"; "IR"; "IS"; "IT"; "JE"; "JM"; "JO"; "JP"; "KE"; "KG"; "KH"; "KI"; "KM"; "KN"; "KP"; "KR"; "KW"; "KY"; "KZ"; "LA"; "LB"; "LC"; "LI"; "LK"; "LR"; "LS"; "LT"; "LU"; "LV"; "LY"; "MA"; "MC"; "MD"; "ME"; "MF"; "MG"; "MH"; "MK"; "ML"; "MM"; "MN"; "MO"; "MP"; "MQ"; "MR"; "MS"; "MT"; "MU"; "MV"; "MW"; "MX"; "MY"; "MZ"; "NA"; "NC"; "NE"; "NF"; "NG"; "NI"; "NL"; "NO"; "NP"; "NR"; "NU"; "NZ"; "OM"; "PA"; "PE"; "PF"; "PG"; "PH"; "PK"; "PL"; "PM"; "PN"; "PR"; "PS"; "PT"; "PW"; "PY"; "QA"; "RE"; "RO"; "RS"; "RU"; "RW"; "SA"; "SB"; "SC"; "SD"; "SE"; "SG"; "SH"; "SI"; "SJ"; "SK"; "SL"; "SM"; "SN"; "SO"; "SR"; "SS"; "ST"; "SV"; "SX"; "SY"; "SZ"; "TC"; "TD"; "TF"; "TG"; "TH"; "TJ"; "TK"; "TL"; "TM"; "TN"; "TO"; "TR"; "TT"; "TV"; "TW"; "TZ"; "UA"; "UG"; "UM"; "US"; "UY"; "UZ"; "VA"; "VC"; "VE"; "VG"; "VI"; "VN"; "VU"; "WF"; "WS"; "YE"; "YT"; "ZA"; "ZM"; "ZW" ]
       return Fix44.Fields.Country ss
    }

let genRawData =
    gen{
        let! len = Gen.choose(1, 64)
        let! bytes = Gen.arrayOfLength len genRawByte
        return NonEmptyByteArray.Make bytes |> Fix44.Fields.RawData
    }
    


// For the purposes of echoing FIX messages to quickfixJ and quickfixN, stop the encoding 
// from being randomly chosen, as fscheck generated Encoded fields contents could concevibly 
// be Utf8 but probably not Iso2022Jp, EucJp or ShiftJis
let genMessageEncoding = gen{ 
    return Fix44.Fields.MessageEncoding.Utf8 
    } // for 'Encoded' fields
//let genMessageEncoding2:Gen<Fix44.Fields.MessageEncoding> = Arb.generate


type ArbOverrides() =
    static member NonEmptyByteArray = Arb.fromGen genNonEmptyByteArray // todo: genNonEmptyByteArray should be shrinkable
    static member RawData()         = Arb.fromGen genRawData
    static member Char()            = Arb.fromGen genChar 
    static member String()          = Arb.fromGen genAlphaString
    static member UTCTimeOnly()     = Arb.fromGen genUTCTimeOnly
    static member UTCDate()         = Arb.fromGen genUTCDate
    static member UTCTimestamp()    = Arb.fromGen genUTCTimestamp
    static member TZTimeonly()      = Arb.fromGen genTZTimeOnly
    static member MonthYear()       = Arb.fromGen genMonthYear
    static member Decimal15dp       = Arb.fromGenShrink (genDecimal15dp, Arb.shrinkNumber)
    static member LocalMktDate()    = Arb.fromGen genLocalMktDate
    static member Currency()        = Arb.fromGen genCurrency
    static member Country()         = Arb.fromGen genCountry
    static member ListT()           = Arb.fromGen (Gen.nonEmptyListOf Arb.generate) // quickfixj considers an option.Some empty list to be option.None, so forcing all lists to be at least 1 long
    static member MessageEncoding   = Arb.fromGen genMessageEncoding
