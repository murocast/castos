#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System

open Fake.Core
open Fake.DotNet
open Fake.IO

Target.initEnvironment ()

let sharedPath = Path.getFullName "./src/Shared"
let serverPath = Path.getFullName "./src/Server"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let deployDir = Path.getFullName "./deploy"
let sharedTestsPath = Path.getFullName "./tests/Shared"
let serverTestsPath = Path.getFullName "./tests/Smapi.Tests"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let npm args workingDir =
    let npmPath =
        match ProcessUtils.tryFindFileOnPath "npm" with
        | Some path -> path
        | None ->
            "npm was not found in path. Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            |> failwith

    let arguments = args |> String.split ' ' |> Arguments.OfArgs

    Command.RawCommand (npmPath, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let dotnet cmd workingDir =
    let result = DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir


let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

// let runDotNet cmd workingDir =
//     let result =
//         DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
//     if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

// let openBrowser url =
//     //https://github.com/dotnet/corefx/issues/10361
//     Command.ShellCommand url
//     |> CreateProcess.fromCommand
//     |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
//     |> Proc.run
//     |> ignore


Target.create "Clean" (fun _ ->
    [ deployDir
      clientDeployPath ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ -> npm "install" ".")

Target.create "Bundle" (fun _ ->
    dotnet (sprintf "publish -c Release -o \"%s\"" deployDir) serverPath
    npm "run build" "."
)

// Target.create "Build" (fun _ ->
//     runDotNet "build" serverPath
//     Shell.regexReplaceInFileWithEncoding
//         "let app = \".+\""
//        ("let app = \"" + release.NugetVersion + "\"")
//         System.Text.Encoding.UTF8
//         (Path.combine clientPath "Version.fs")
//     runTool yarnTool "webpack-cli -p" __SOURCE_DIRECTORY__
// )

// Target.create "Run" (fun _ ->
//     let server = async {
//         runDotNet "watch run" serverPath
//     }
//     let client = async {
//         runTool yarnTool "webpack-dev-server" __SOURCE_DIRECTORY__
//     }
//     let browser = async {
//         do! Async.Sleep 5000
//         openBrowser "http://localhost:8080"
//     }

//     let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
//     let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

//     let tasks =
//         [ if not safeClientOnly then yield server
//           yield client
//           if not vsCodeSession then yield browser ]

//     tasks
//     |> Async.Parallel
//     |> Async.RunSynchronously
//     |> ignore
// )

Target.create "Run" (fun _ ->
    dotnet "build" sharedPath
    [ async { dotnet "watch run" serverPath }
      async { npm "run start" "." } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "RunTests" (fun _ ->
    //dotnet "build" sharedTestsPath
    [ async { dotnet "watch run" serverTestsPath }
      async { npm "run test:live" "." } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

let buildDocker tag =
    let args = sprintf "build -t %s ." tag
    runTool "docker" args __SOURCE_DIRECTORY__

let dockerUser = "murocast"
let dockerImageName = "murocast"
let dockerFullName = sprintf "%s/%s" dockerUser dockerImageName

Target.create "Docker" (fun _ ->
    buildDocker dockerFullName
)

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Bundle"
    ==> "Docker"

"Clean"
    ==> "InstallClient"
    ==> "Run"

"Clean"
    ==> "InstallClient"
    ==> "RunTests"

Target.runOrDefaultWithArguments "Build"
