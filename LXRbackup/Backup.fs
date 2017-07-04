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

    let goblack = "\u001b[30m"
    let gored = "\u001b[31m"
    let gogreen = "\u001b[32m"
    let goyellow = "\u001b[33m"
    let goblue = "\u001b[34m"
    let gomagenta = "\u001b[35m"
    let gocyan = "\u001b[36m"
    let gowhite = "\u001b[37m"
    let gonormal = "\u001b[39m"

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
        match refDbKey with
        | Some dbkey ->
            match refDbFp with
            | Some dbfp ->
                SBCLab.LXR.BackupCtrl.setReference (ctrl ()) dbkey dbfp
            | _ -> ()
        | _ -> ()

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
                Console.WriteLine(gored + "skipping: {0}" + gonormal, fp)
            else
                Console.Write("backing up " + gocyan + fp + gonormal + "  ")
                SBCLab.LXR.BackupCtrl.backup (ctrl ()) fp
                Console.WriteLine(gogreen + "done." + gonormal, fp)
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
        System.Console.WriteLine("backup {0:0,0} bytes (read {1:0,0} bytes); took write={2} ms encrypt={3} ms extract={4} ms",
            bo, bi,
            SBCLab.LXR.BackupCtrl.time_write (ctrl ()), SBCLab.LXR.BackupCtrl.time_encrypt (ctrl ()), SBCLab.LXR.BackupCtrl.time_extract (ctrl ()))
        System.Console.WriteLine("compression rate: " + gocyan + "{0:0.00}" + gonormal + "  time: " + gocyan + "{1}" + gonormal + " ms  throughput: " + gocyan + "{2:0,0}" + gonormal + " kilobytes per second", (double(bi) / double(bo)), td, (double(bi) * 1000.0 / 1024.0 / double(td)))
        ()
