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

    open System.IO
    open System.Reflection

    let showHelp () =
        let refl = System.Reflection.Assembly.GetAssembly(typeof<LXRcli.Backup>)
        let n = refl.GetName()
        let copyright = refl.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright
        System.Console.WriteLine(n.Name + " " + n.Version.ToString())
        System.Console.WriteLine(copyright)
        System.Console.WriteLine()
        System.Console.WriteLine("Parameters: ")
        System.Console.WriteLine("  -n        number of chunks (à 256 kB) per assembly")
        //System.Console.WriteLine("  -r       redundant chunks per assembly (default = 0)")
        System.Console.WriteLine("  -c        compression [0|1] (default = 0)")
        System.Console.WriteLine("  -d        deduplication [0|1|2] (default = 0)")
        System.Console.WriteLine("  -pX       output path to encrypted chunks")
        System.Console.WriteLine("  -pD       output path to data files (XML)")
        System.Console.WriteLine("  -ref      reference DbFp (XML)")
        System.Console.WriteLine("  -f        (*)backup single file")
        System.Console.WriteLine("  -d1       (*)backup all files in a directory")
        System.Console.WriteLine("  -dr       (*)recursively backup all files in a directory")
        System.Console.WriteLine("(*)marked parameters may occur several times.")


    [<EntryPoint>]
    let main argv =

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
                        //str.Close()
                        //refdbfp <- Some db
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

