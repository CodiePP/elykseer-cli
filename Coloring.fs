namespace LXRcli

module Coloring =

    open System

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

