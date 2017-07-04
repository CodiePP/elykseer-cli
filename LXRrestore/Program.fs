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

    open System.Reflection

    let showHelp () =
        let refl = System.Reflection.Assembly.GetAssembly(typeof<LXRcli.Restore>)
        let n = refl.GetName()
        let copyright = refl.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright
        System.Console.WriteLine(n.Name + " " + n.Version.ToString())
        System.Console.WriteLine(copyright)
        System.Console.WriteLine()
        System.Console.WriteLine("Parameters: ")
        System.Console.WriteLine("  -o    output directory (created if non-existing)")
        System.Console.WriteLine("  -pX   path to encrypted chunks")
        System.Console.WriteLine("  -d    (*)load XML with filepaths or keys")
        System.Console.WriteLine("  -r    (*)restore a single file")
        System.Console.WriteLine("  -s    (*)regular expression to select files to restore")
        System.Console.WriteLine("  -x    (*)regular expression to exclude files")
        System.Console.WriteLine("(*)marked parameters may occur several times.")


    [<EntryPoint>]
    let main argv =

        if Array.length argv = 0 || Array.contains "--help" argv then
            showHelp ()
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

