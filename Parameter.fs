(*
    eLyKseeR or LXR - cryptographic data archiving software
    https://github.com/CodiePP/elykseer-cli
    Copyright (C) 2017 Alexander Diemand

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace LXRcli

type Parameter (name : string, ?nec0 : bool) =

    let nec : bool = defaultArg nec0 false
    let mutable init : bool = false
    let mutable value : string list = []

    member this.parse (args : string array) = 
        let n = args.Length
        Array.iteri (fun i e -> if e = name && i < (n - 1) then value <- args.[i + 1] :: value; init <- true) args

    member this.isInit = init
    member this.isNecessary = nec
    member this.getValue = value
    member this.getName = name
