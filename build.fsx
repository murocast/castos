// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.IO
open System.Diagnostics

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"


// Filesets
let appReferences  =
    !! "/**/*.csproj"
      ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

let build () = 
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
        |> Log "AppBuild-Output: "

let rec runWebsite() =
    let codeFolder = FullName "src"
    use watcher = new FileSystemWatcher(codeFolder, "*.fs")
    watcher.EnableRaisingEvents <- true
    watcher.IncludeSubdirectories <- true
    watcher.Changed.Add(handleWatcherEvents)
    watcher.Created.Add(handleWatcherEvents)
    watcher.Renamed.Add(handleWatcherEvents)
    
    build()

    let app = Path.Combine(buildDir, "Castos.Api.exe")
    let ok = 
        execProcess (fun info -> 
            info.FileName <- app
            info.Arguments <- "") TimeSpan.MaxValue
    if not ok then tracefn "Website shut down."
    watcher.Dispose()

and handleWatcherEvents (e:IO.FileSystemEventArgs) =
    tracefn "Rebuilding website...."

    let runningWebsites = 
        Process.GetProcessesByName("Castos.Api")
        |> Seq.iter (fun p -> p.Kill())

    runWebsite()

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    build()
)

Target "Run" (fun _ ->
    async {
        Threading.Thread.Sleep(3000)
        Process.Start(sprintf "http://localhost:%d" 8083) |> ignore }
    |> Async.Start

    runWebsite()
)

"Clean"
  ==> "Build"
  ==> "Watch"

// start build
RunTargetOrDefault "Build"
