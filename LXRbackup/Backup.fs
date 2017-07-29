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

open System
open System.IO

type Backup () =

    let mutable n = 16
    let mutable r = 0
    let mutable pOut = "/tmp/"
    let mutable pDb = "/tmp/"
    let mutable backup = None
    let mutable pCompress = false
    let mutable pDedup = 0
    let mutable refDbFp = None
    let mutable refDbKey = None

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

    let ctrl () =
        match backup with
        | Some c -> c
        | None -> 
            let o = new SBCLab.LXR.Options()
            o.setNchunks n
            o.setRedundancy r
            o.setFpathDb pDb
            o.setFpathChunks pOut
            o.setCompression pCompress
            o.setDeduplication pDedup
            //Console.WriteLine ("initializing BackupCtrl...")
            let c = SBCLab.LXR.BackupCtrl.create o
            backup <- Some c
            c
    
    let trySetRefDb _ =
        SBCLab.LXR.BackupCtrl.setReference (ctrl ()) refDbKey refDbFp

    member this.setN (x : string) =
        //Console.WriteLine("setting n = {0}", x)
        let k = try Int32.Parse x with
                | _ -> 16
        if k >= 16 && k <= 256 then
            n <- k

    member this.setR (x : string) =
        let k = try Int32.Parse x with
                | _ -> 0
        if k >= 0 && k <= 3 then
            r <- k

    member this.setC (x : string) =
        let k = try Int32.Parse x with
                | _ -> 0
        if k > 0 then
            pCompress <- true
        else
            pCompress <- false

    member this.setD (x : string) =
        let k = try Int32.Parse x with
                | _ -> 0
        if k >= 0 && k <= 2 then
            pDedup <- k

    member this.setChunkPath p = pOut <- p
    member this.setDataPath p = pDb <- p

    member this.setRefDbKey db =
        refDbKey <- Some db
        trySetRefDb ()

    member this.setRefDbFp db =
        refDbFp <- Some db
        trySetRefDb ()

    member this.finalize =
        SBCLab.LXR.BackupCtrl.finalize <| ctrl ()

    member this.backupFile fp =
        if SBCLab.LXR.FileCtrl.fileExists fp then
            (* backup file *)
            let fi = FileInfo(fp)
            if fi.Attributes.HasFlag(FileAttributes.ReparsePoint)
               || fi.Attributes.HasFlag(FileAttributes.System) then
                gored ()
                Console.WriteLine("skipping: {0}", fp)
                gonormal ()
            else
                Console.Write("backing up ")
                gocyan ()
                Console.Write(fp)
                gonormal ()
                try SBCLab.LXR.BackupCtrl.backup (ctrl ()) fp
                    gogreen(); Console.WriteLine(" done."); gonormal()
                with
                | _ -> gored(); Console.WriteLine(" failed."); gonormal()
        ()

    member this.backupDirectory fp =
        if SBCLab.LXR.FileCtrl.dirExists fp then
            (* backup directory *)
            let di = DirectoryInfo(fp)
            for fps in di.EnumerateFiles() do
                this.backupFile fps.FullName
        ()

    member this.backupRecursive fp =
        if SBCLab.LXR.FileCtrl.dirExists fp then
            (* backup directory *)
            let di = DirectoryInfo(fp)
            for fps in di.EnumerateFiles() do
                this.backupFile fps.FullName
            for dp in di.EnumerateDirectories() do
                this.backupRecursive dp.FullName
        ()

    member this.summarize =
        let td = SBCLab.LXR.BackupCtrl.time_encrypt (ctrl ()) +
                 SBCLab.LXR.BackupCtrl.time_extract (ctrl ()) +
                 SBCLab.LXR.BackupCtrl.time_write (ctrl ())
        let bi = SBCLab.LXR.BackupCtrl.bytes_in (ctrl ())
        let bo = SBCLab.LXR.BackupCtrl.bytes_out (ctrl ())
        Console.WriteLine("backup {0:0,0} bytes (read {1:0,0} bytes); took write={2} ms encrypt={3} ms extract={4} ms",
            bo, bi,
            SBCLab.LXR.BackupCtrl.time_write (ctrl ()), SBCLab.LXR.BackupCtrl.time_encrypt (ctrl ()), SBCLab.LXR.BackupCtrl.time_extract (ctrl ()))
        Console.Write("compression rate: ")
        gocyan ()
        Console.Write("{0:0.00}", (double(bi) / double(bo)))
        gonormal ()
        Console.Write("  time: ")
        gocyan ()
        Console.Write("{0}", td)
        gonormal ()
        Console.Write(" ms  throughput: ")
        gocyan ()
        Console.Write("{0:0,0}", (double(bi) * 1000.0 / 1024.0 / double(td)))
        gonormal ()
        Console.WriteLine(" kilobytes per second")
        ()
