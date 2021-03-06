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
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let serverPath = Path.getFullName "./src/Yobo.API"
let clientPath = Path.getFullName "./src/Yobo.Client"
let corePath = Path.getFullName "./src/Yobo.Core"
let sharedPath = Path.getFullName "./src/Yobo.Shared"
let clientOutputDir = clientPath + "/output"
let deployDir = Path.getFullName "./deploy"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

Target.create "Clean" (fun _ ->
    !! "obj"
    ++ "src/*/bin"
    ++ "src/*/obj"
    ++ deployDir
    ++ clientOutputDir
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    runDotNet "restore Yobo.Client.fsproj" clientPath
)

Target.create "Build" (fun _ ->
    runDotNet "build" serverPath
    runTool yarnTool "webpack-cli --config src/Yobo.Client/webpack.config.js -p" clientPath
)

Target.create "Run" (fun _ ->
    runDotNet "build" sharedPath
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool yarnTool "webpack-dev-server --config src/Yobo.Client/webpack.config.js" clientPath
    }
    [ client; server ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "Publish" (fun _ ->
    let publicDir = Path.combine deployDir "wwwroot"
    let publishArgs = sprintf "publish -c Release -o \"%s\"" deployDir
    runDotNet publishArgs serverPath
    Shell.copyDir publicDir "src/Yobo.Client/output" FileFilter.allFiles
    Path.combine deployDir "config.development.json" |> File.delete
)

Target.create "RefreshSchema" (fun _ -> 
    let srcFile = "..\Yobo.Private\ReadDb.fs"
    let original = corePath + "\ReadDb.fs"
    let schemaFile = ".\database\yobo.schema"

    schemaFile |> File.delete
    let backup = original |> File.readAsString
    srcFile |> File.readAsString |> File.replaceContent original
    runDotNet (sprintf "build %s" corePath) "."
    backup |> File.replaceContent original
)

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Publish"

"Clean"
    ==> "InstallClient"
    ==> "Run"



Target.runOrDefaultWithArguments "Build"