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
    open System.IO
    open System.Reflection

#if compile_for_windows
    let goblack () = Console.ForegroundColor <- ConsoleColor.Black
    let gored () = Console.ForegroundColor <- ConsoleColor.Red
    let gogreen () = Console.ForegroundColor <- ConsoleColor.Green
    let goyellow () = Console.ForegroundColor <- ConsoleColor.Yellow
    let goblue () = Console.ForegroundColor <- ConsoleColor.Blue
    let gomagenta () = Console.ForegroundColor <- ConsoleColor.Magenta
    let gocyan () = Console.ForegroundColor <- ConsoleColor.Cyan
    let gowhite () = Console.ForegroundColor <- ConsoleColor.White
    let normal0 = Console.ForegroundColor
    let gonormal () = Console.ForegroundColor <- normal0
#else
    let goblack () = Console.Write("\u001b[30m")
    let gored () = Console.Write("\u001b[31m")
    let gogreen () = Console.Write("\u001b[32m")
    let goyellow () = Console.Write("\u001b[33m")
    let goblue () = Console.Write("\u001b[34m")
    let gomagenta () = Console.Write("\u001b[35m")
    let gocyan () = Console.Write("\u001b[36m")
    let gowhite () = Console.Write("\u001b[37m")
    let gonormal () = Console.Write("\u001b[39m")
#endif

    let showHeader () =
        let refl = Reflection.Assembly.GetAssembly(typeof<LXRcli.Backup>)
        let n = refl.GetName()
        let copyright = refl.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright
        gocyan()
        Console.WriteLine(n.Name + " " + n.Version.ToString())
        gonormal()
        gored()
        Console.WriteLine("pre-release version TR1 - do not use for production")
        gonormal()
        Console.WriteLine(copyright)
        Console.WriteLine()

    let showHelp () =
        let showparam (a:string) (b:string) =
                                gogreen()
                                Console.Write(a)
                                gonormal()
                                Console.WriteLine(b)
                                ()
        Console.WriteLine("Parameters: ")
        showparam "  -n        " "number of chunks (à 256 kB) per assembly"
        //showparam "  -r       " "redundant chunks per assembly (default = 0)"
        showparam "  -c        " "compression [0|1] (default = 0)"
        showparam "  -d        " "deduplication [0|1|2] (default = 0)"
        showparam "  -pX       " "output path to encrypted chunks"
        showparam "  -pD       " "output path to data files (XML)"
        showparam "  -ref      " "reference DbFp (XML)"
        showparam "  -f        " "(*)backup single file"
        showparam "  -d1       " "(*)backup all files in a directory"
        showparam "  -dr       " "(*)recursively backup all files in a directory"
        Console.WriteLine("(*)marked parameters may occur several times.")


    [<EntryPoint>]
    let main argv =

        showHeader ()

        if Array.length argv = 0 || Array.contains "--help" argv then
            showHelp ()
            exit(0)

        let ps = List.collect (fun (name,req) -> [new Parameter(name,req)]) 
                    [ ("-n",true); ("-r",false); ("-c",false); ("-d",false); ("-ref",false);
                      ("-pD",true); ("-pX",true); ("-f",false); ("-d1",false); ("-dr",false) ]
                 (*: Parameter list*)
        List.map (fun (p : Parameter) -> p.parse argv) ps |> ignore
        let nmissed = List.fold (fun c (p : Parameter) -> if p.isNecessary && not p.isInit then c + 1 else c) 0 ps
        if nmissed > 0 then
            showHelp ()
            exit(1)

        (* prepare *)
        let backup = new Backup()
        List.iter 
            (fun (n, f) ->
                match List.tryFind (fun (p : Parameter) -> p.getName = n) ps with
                | Some p -> if p.isInit then f (p.getValue |> List.head)
                | _ -> () )
            [("-n",backup.setN);("-r",backup.setR);("-c",backup.setC);("-d",backup.setD);("-pD",backup.setDataPath);("-pX",backup.setChunkPath)]

        let mutable refdbfp : SBCLab.LXR.DbFp option = None
        let mutable refdbkey : SBCLab.LXR.DbKey option = None

        match List.tryFind (fun (p : Parameter) -> p.getName = "-ref") ps with
        | Some p -> try let db = new SBCLab.LXR.DbFp() in
                        //System.Console.WriteLine("reading paths as reference from {0}", p.getValue.Head)
                        use str = File.OpenText(p.getValue.Head)
                        db.inStream str
                        backup.setRefDbFp db
                    with _ -> ()
        | _ -> ()

        (* run parameters *)
        match List.tryFind (fun (p : Parameter) -> p.getName = "-f") ps with
        | Some p -> List.iter (fun fp -> backup.backupFile fp) p.getValue
        | _ -> ()

        match List.tryFind (fun (p : Parameter) -> p.getName = "-d1") ps with
        | Some p -> List.iter (fun fp -> backup.backupDirectory fp) p.getValue
        | _ -> ()

        match List.tryFind (fun (p : Parameter) -> p.getName = "-dr") ps with
        | Some p -> List.iter (fun fp -> backup.backupRecursive fp) p.getValue
        | _ -> ()

        // finalize the current assembly
        // and write data files to disk
        backup.finalize

        // report statistics
        backup.summarize

        0 // return an integer exit code

