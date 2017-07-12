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
open System.Text.RegularExpressions

type Restore () =

    let mutable n = 256
    let mutable r = 0
    let mutable pX = "/tmp/"
    let mutable pOut = "/tmp/"
    let mutable ctrl = SBCLab.LXR.RestoreCtrl.create ()
    let mutable selexpr = []
    let mutable xclexpr = []

    let goblack = "\u001b[30m"
    let gored = "\u001b[31m"
    let gogreen = "\u001b[32m"
    let goyellow = "\u001b[33m"
    let goblue = "\u001b[34m"
    let gomagenta = "\u001b[35m"
    let gocyan = "\u001b[36m"
    let gowhite = "\u001b[37m"
    let gonormal = "\u001b[39m"

    let init () =
        let o = new SBCLab.LXR.Options()
        o.setNchunks n
        o.setRedundancy r
        o.setFpathDb ""
        o.setFpathChunks pX
        SBCLab.LXR.RestoreCtrl.setOptions ctrl o
         
    let readfpdb fp =
        try
            (* read db file *)
            use str = new IO.FileStream(fp, IO.FileMode.Open)
            let db = SBCLab.LXR.RestoreCtrl.getDbFp ctrl
            db.inStream (new StreamReader(str))
            Console.WriteLine("know of {0} filepaths in db", db.idb.count)
            with _ -> ()
        ()
    let readkeydb fp =
        try
            (* read db file *)
            use str = new IO.FileStream(fp, IO.FileMode.Open)
            let db = SBCLab.LXR.RestoreCtrl.getDbKeys ctrl
            db.inStream (new StreamReader(str))
            Console.WriteLine("know of {0} keys in db", db.idb.count)
            with _ -> ()
        ()

    let dorestore (fp : string) =
        System.Console.Write("restoring " + gocyan + fp + gonormal + "  ")
        try SBCLab.LXR.RestoreCtrl.restore ctrl pOut fp
            let dbfp = SBCLab.LXR.RestoreCtrl.getDbFp ctrl
            match dbfp.idb.get fp with
            | None -> System.Console.WriteLine(gored + "no data" + gonormal)
            | Some db ->
                let chk = SBCLab.LXR.Sha256.hash_file (pOut + "/" + fp) |> SBCLab.LXR.Key256.toHex
                if chk = SBCLab.LXR.Key256.toHex db.checksum then
                    System.Console.WriteLine(gogreen + "success." + gonormal)
                else
                    System.Console.WriteLine(gored + "failure " + chk + "/=" + (SBCLab.LXR.Key256.toHex db.checksum) + gonormal)
        with
        | e -> System.Console.WriteLine(gored + "failed" + gonormal + " with")
               System.Console.WriteLine("{0}", e.ToString())
               reraise ()

    let checkRestore (fp : string) =
        // check if in any selection regexp
        if List.exists (fun (pat : Regex) -> pat.IsMatch(fp)) selexpr then 

            // check if none in exclude regexp matches
            if List.forall (fun (pat : Regex) -> pat.IsMatch(fp) |> not) xclexpr then

                dorestore fp

        ()

    member this.setChunkPath p =
        pX <- p
        init ()

    member this.setOutputPath p =
        pOut <- p
        if not <| SBCLab.LXR.FileCtrl.dirExists p then
            Directory.CreateDirectory(p) |> ignore

    member this.setSelectExpr (x : string) =
        selexpr <- new Regex(x,RegexOptions.CultureInvariant) :: selexpr
        ()

    member this.setExcludeExpr (x : string) =
        xclexpr <- new Regex(x,RegexOptions.CultureInvariant) :: xclexpr
        ()


    member this.addDb fp =
        if SBCLab.LXR.FileCtrl.fileExists fp then
            readfpdb fp
            readkeydb fp
        ()

    member this.restore fp =
        dorestore fp
(*        try SBCLab.LXR.RestoreCtrl.restore ctrl pOut fp with
        | e -> System.Console.WriteLine(gored + "restore failed with {0}" + gonormal, e.ToString())
               reraise () *)

    member this.restoreSelection () =
        let db = SBCLab.LXR.RestoreCtrl.getDbFp ctrl
        // all paths are checked against regexps and eventually restored
        db.idb.appKeys (fun e -> checkRestore e)

    member this.summarize =
        let td = SBCLab.LXR.RestoreCtrl.time_read ctrl +
                 SBCLab.LXR.RestoreCtrl.time_decrypt ctrl +
                 SBCLab.LXR.RestoreCtrl.time_extract ctrl
        let bi = SBCLab.LXR.RestoreCtrl.bytes_in ctrl
        let bo = SBCLab.LXR.RestoreCtrl.bytes_out ctrl
        System.Console.WriteLine("restored {0:0,0} bytes (read {1:0,0} bytes); took read={2} ms decrypt={3} ms extract={4} ms",
            bo, bi,
            SBCLab.LXR.RestoreCtrl.time_read ctrl, SBCLab.LXR.RestoreCtrl.time_decrypt ctrl, SBCLab.LXR.RestoreCtrl.time_extract ctrl)
        System.Console.WriteLine("compression rate: " + gocyan + "{0:0.00}" + gonormal + "  time: " + gocyan + "{1}" + gonormal + " ms  throughput: " + gocyan + "{2:0,0}" + gonormal + " kilobytes per second", (double(bo) / double(bi)), td, (double(bo) * 1000.0 / 1024.0 / double(td)))
        ()
