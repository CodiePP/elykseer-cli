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

module Main =

    open System
    open System.Reflection

    open LXRcli.Coloring

    let showHeader () =
        let refl = Reflection.Assembly.GetAssembly(typeof<LXRcli.Restore>)
        let n = refl.GetName()
        gocyan()
        Console.WriteLine(n.Name + " " + n.Version.ToString())
        gonormal()
        gored()
        Console.WriteLine(SBCLab.LXR.Liz.version)
        gonormal()
        Console.WriteLine()

    let showHelp () =
        let showparam (a:string) (b:string) =
                                gogreen()
                                Console.Write(a)
                                gonormal()
                                Console.WriteLine(b)
                                ()

        Console.WriteLine("Parameters: ")
        showparam "  -o    " "output directory (created if non-existing)"
        showparam "  -pX   " "path to encrypted chunks"
        showparam "  -d    " "(*)load XML with filepaths or keys"
        showparam "  -r    " "(*)restore a single file"
        showparam "  -s    " "(*)regular expression to select files to restore"
        showparam "  -x    " "(*)regular expression to exclude files"
        Console.WriteLine("(*)marked parameters may occur several times.")
        Console.WriteLine("")
        showparam "--help      " "shows this help"
        showparam "--version   " "displays version information"
        showparam "--license   " "displays license text"
        showparam "--copyright " "displays copyright information"
        gonormal()
        Console.WriteLine("")


    [<EntryPoint>]
    let main argv =

        showHeader ()

        if Array.length argv = 0
         || Array.contains "-h" argv 
         || Array.contains "--help" argv then
            showHelp ()
            exit(0)

        if Array.contains "--version" argv then
            Console.WriteLine(SBCLab.LXR.Liz.version)
            exit(0)

        if Array.contains "--license" argv then
            Console.WriteLine(SBCLab.LXR.Liz.license)
            exit(0)

        if Array.contains "--copyright" argv then
            Console.WriteLine(SBCLab.LXR.Liz.copyright)
            exit(0)

        let ps = List.collect (fun (name,req) -> [new Parameter(name,req)]) 
                    [ ("-o",true); ("-d",true); ("-pX",true); ("-r",false); ("-s",false); ("-x",false) ]
                 (*: Parameter list*)
        List.map (fun (p : Parameter) -> p.parse argv) ps |> ignore
        let nmissed = List.fold (fun c (p : Parameter) -> if p.isNecessary && not p.isInit then c + 1 else c) 0 ps
        if nmissed > 0 then
            showHelp ()
            exit(1)

        let restore = new Restore()
        List.iter 
            (fun (n, f) ->
                match List.tryFind (fun (p : Parameter) -> p.getName = n) ps with
                | Some p -> if p.isInit then f (p.getValue |> List.head)
                | _ -> () )
            [("-o",restore.setOutputPath);("-pX",restore.setChunkPath)]


        // read db files (XML)
        match List.tryFind (fun (p : Parameter) -> p.getName = "-d") ps with
        | Some p -> List.iter (fun fp -> restore.addDb fp) p.getValue
        | _ -> ()

        match List.tryFind (fun (p : Parameter) -> p.getName = "-s") ps with
        | Some p -> List.iter (fun x -> restore.setSelectExpr x) p.getValue
        | _ -> ()

        match List.tryFind (fun (p : Parameter) -> p.getName = "-x") ps with
        | Some p -> List.iter (fun x-> restore.setExcludeExpr x) p.getValue
        | _ -> ()

        // restore files (single)
        match List.tryFind (fun (p : Parameter) -> p.getName = "-r") ps with
        | Some p -> List.iter (fun fp -> restore.restore fp) p.getValue
        | _ -> ()

        // restore files matching selection regexp
        // but not matching exclude regexp
        restore.restoreSelection ()

        // report statistics
        restore.summarize

        0 // return an integer exit code

