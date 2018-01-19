// include libs
#r "./packages/build-gr/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper

// Directories    
let buildDir      = "./build/"
let deployDir     = "./deploy/"
let debugBuildDir = "./src/debug-prj/bin/Debug/netcoreapp2.0"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "aggregator"

// Default target configuration
let configuration = "Release"

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

// File system information
let solutionFile  = "aggregator.sln"

// Debug project name
let debugPrjName = "debug-prj"

// Path to debug project
let debugPrj  = "./src" </> debugPrjName </> debugPrjName + ".fsproj"

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title         projectName
          Attribute.Product       project
          Attribute.Version       release.AssemblyVersion
          Attribute.FileVersion   release.AssemblyVersion
          Attribute.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.filter (fun (_, prjName, _, _) -> not <| prjName.StartsWith debugPrjName)
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs 
        [ buildDir 
          buildDir </> project
          deployDir
          debugBuildDir ]
)

// --------------------------------------------------------------------------------------
// Build library

Target "Build" (fun _ ->

    //build debug just for sure    
    if fileExists debugPrj then
        DotNetCli.Build (fun p ->
            { p with 
                Project       = debugPrj
                Configuration = "Debug" })

    !! solutionFile
    |> Seq.iter (fun slnPath ->
        DotNetCli.Build ( fun p -> 
            { p with
                WorkingDir    = "./"
                Configuration = configuration
                Output        = !! (buildDir </> project) |> Seq.head
                Project       = slnPath}))
    CopyFile  (buildDir </> "host.json") "./src/host.json"
    )

// --------------------------------------------------------------------------------------
// Creates zip from buildDir

Target "ZipAndCopy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir </> "drop.zip")
)

Target "All" DoNothing

// Build order
"AssemblyInfo"
  ==> "Clean"
  ==> "Build"
  ==> "ZipAndCopy"
  ==> "All"

// Start build
RunTargetOrDefault "All"