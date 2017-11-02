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

    open LXRcli.Coloring

    let showHeader () =
        let refl = Reflection.Assembly.GetAssembly(typeof<LXRcli.Backup>)
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
        showparam "  -j        " "backup job description in XML"
        Console.WriteLine("")
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

        if Array.contains "-j" argv then
            let p = new Parameter("-j",true)
            p.parse argv
            if not p.isInit then
                showHelp ()
                exit(0)
            else
                let fp = p.getValue |> List.head
                let jdesc = new SBCLab.LXR.DbBackupJob ()
                (
                    use fstr = File.OpenRead(fp)
                    use instr = new StreamReader(fstr)
                    jdesc.inStream instr
                )
                let backup = new Backup()
                //backup.runJob //name job
                jdesc.idb.appValues (fun name (job: SBCLab.LXR.DbJobDat) ->
                    if job.options.isDeduplicated > 0 then
                        let fpath = new DirectoryInfo(job.options.fpath_db) in
                        SBCLab.LXR.Logging.log () <| Printf.sprintf "reading dir %A" fpath 
                        let newest = fpath.EnumerateFiles("lxr*_dbfp.xml", SearchOption.TopDirectoryOnly)
                        let selected = newest |> Seq.map (fun fi -> (fi.LastWriteTime.Ticks, fi.FullName))
                                       |> Seq.sortByDescending (fun (dt, _) -> dt) |> Seq.take 1 |> Seq.toList
                        match selected with
                        | [(_, el)] -> let db = new SBCLab.LXR.DbFp() in
                                       use str = File.OpenText(el)
                                       db.inStream str
                                       backup.setRefDbFp db

                        | _ -> ()
                    backup.runJob name job
                    )
        else
            let ps = List.collect (fun (name,req) -> [new Parameter(name,req)]) 
                        [ ("-n",true); ("-r",false); ("-c",false); ("-d",false); ("-ref",false);
                          ("-pD",true); ("-pX",true); ("-f",false); ("-d1",false); ("-dr",false) ]
                     (*: Parameter list*)
            List.map (fun (p : Parameter) -> p.parse argv) ps |> ignore
            let nmissed = List.fold (fun c (p : Parameter) -> if p.isNecessary && not p.isInit then c + 1 else c) 0 ps
            if nmissed > 0 then
                showHelp ()
                exit(1)

            let backup = new Backup()

            List.iter 
                (fun (n, f) ->
                    match List.tryFind (fun (p : Parameter) -> p.getName = n) ps with
                    | Some p -> if p.isInit then f (p.getValue.Head)
                    | _ -> () )
                [("-n",backup.setN);("-r",backup.setR);("-c",backup.setC);("-d",backup.setD);("-pD",backup.setDataPath);("-pX",backup.setChunkPath)]

            let mutable refdbfp : SBCLab.LXR.DbFp option = None
            let mutable refdbkey : SBCLab.LXR.DbKey option = None

            match List.tryFind (fun (p : Parameter) -> p.getName = "-ref" && p.isInit) ps with
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

            let job = backup.mkJob
            backup.runJob "backup" job

        0 // return an integer exit code
