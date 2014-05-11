open System.IO;
open EmnExtensions;
open System.Text.RegularExpressions;
open System.Threading.Tasks;

type Cleaners = System.Collections.Generic.List<( unit -> unit )>

let initializeCleaners () = new Cleaners ()
let addCleaner (cleaners:Cleaners) cleaner =
    cleaners.Add(cleaner)
   
let tryCall func =
    try 
        func ()
        None
    with 
        | e -> Some e
   
let cleanup cleaners = 
    let exns = cleaners |> List.ofSeq |> List.map tryCall |> List.choose id
    if not <| List.isEmpty exns then
        raise <| new System.AggregateException(exns)

let file_within (dir:DirectoryInfo) name =
    let path = Path.Combine(dir.FullName, name)
    new FileInfo(path)

let file_within_ext (dir:DirectoryInfo) base_name ext =
    let path = Path.Combine(dir.FullName, Path.GetFileNameWithoutExtension(base_name) + ext)
    new FileInfo(path)

let copy_file_to_temp (out_dir:DirectoryInfo) cleaners (src_file:FileInfo) = 
    let new_path = Path.Combine( out_dir.FullName, src_file.Name)
    if File.Exists new_path then
        failwith <| sprintf "File %s exists" new_path
    addCleaner cleaners (fun () -> File.Delete new_path)
    printfn "Copying: %s -> %s" src_file.FullName new_path
    src_file.CopyTo(new_path)


let generate_avs_script out_dir cleaners (tmp_file:FileInfo) = 
    let contents = @"
        FFVideoSource(""" + tmp_file.FullName + @""")
        #Trim(4315,5750)
        ConvertToYV12 #required for MCTD
        MCTD(settings=""medium"",radius=3,sigma=9,limitC=3,bt=5,bwbh=32,owoh=16,enhance=true,dbF=""GradFun3(lsb=true,radius=16,smode=2)"")
        #limitC=2: do less noise reduction on chrome planes
        Dither_out() #required for x264
        "
    let avs_file = file_within_ext out_dir tmp_file.Name ".avs"
    if avs_file.Exists then
        //failwith <| sprintf "File %s exists" avs_file.FullName
        ()
    else
        printfn "Generating avs script: %s" avs_file.FullName
        addCleaner cleaners (fun () -> avs_file.Delete())
        File.WriteAllText(avs_file.FullName, contents)
    avs_file

let recompress_video out_dir cleaners (avs_file:FileInfo) =
    let base_args = "--x264-binary x264-10b.exe --threads 3 --preset placebo --tune film --open-gop --non-deterministic --keyint 5000 --min-keyint 25 --crf 23 --nr 200 --aq-strength 0.8 --input-depth 16"
    let video_file = file_within_ext out_dir avs_file.Name "-video.mkv"
    if video_file.Exists then
//        failwith <| sprintf "File %s already exists" video_file.FullName
        ()
    else
        printfn "Compressing (slow): %s" video_file.FullName

        addCleaner cleaners (fun () -> video_file.Delete())
        let file_args  = sprintf " -o \"%s\" \"%s\"" video_file.FullName avs_file.FullName
        let res = WinProcessUtil.ExecuteProcessSynchronously("avs4x264mod.exe", base_args + file_args, "")
        if res.ExitCode <> 0 then
            System.Console.WriteLine res.StandardOutputContents
            System.Console.WriteLine res.StandardErrorContents
            failwith ("video encode failed to create " + video_file.FullName)
    video_file

let extract_audio out_dir cleaners (tmp_file:FileInfo) =
    let flac_file = file_within_ext out_dir tmp_file.Name "-audio.flac"
    if flac_file.Exists then
        //failwith <| sprintf "File %s already exists" flac_file.FullName
        ()
    else
        addCleaner cleaners (fun () -> flac_file.Delete())
        printfn "Extracting: %s" flac_file.FullName

        let res = WinProcessUtil.ExecuteProcessSynchronously("ffmpeg.exe", "-i \"" + tmp_file.FullName + "\" \"" + flac_file.FullName + "\"", "")
        if res.ExitCode <> 0 then
            System.Console.WriteLine res.StandardOutputContents
            System.Console.WriteLine res.StandardErrorContents
            failwith ("audio encode failed to extract " + flac_file.FullName)
    flac_file

let compress_audio out_dir cleaners (flac_file:FileInfo) =
    let opus_file = file_within_ext out_dir flac_file.Name ".opus"
    if opus_file.Exists then
        //failwith <| sprintf "File %s already exists" opus_file.FullName
        ()
    else
        addCleaner cleaners (fun () -> opus_file.Delete())
        printfn "Compressing audio: %s" opus_file.FullName

        let res = WinProcessUtil.ExecuteProcessSynchronously("opusenc.exe", "--framesize 60 --bitrate 64 \"" + flac_file.FullName + "\" \"" + opus_file.FullName + "\"", "")
        if res.ExitCode <> 0 then
            System.Console.WriteLine res.StandardOutputContents
            System.Console.WriteLine res.StandardErrorContents
            failwith ("audio encode failed to extract " + opus_file.FullName)
    opus_file

let extract_chapters out_dir cleaners (tmp_file:FileInfo) =
    let chapters_file = file_within_ext out_dir tmp_file.Name "-chapters.xml"
    if chapters_file.Exists then
        failwith <| sprintf "File %s already exists" chapters_file.FullName
        Some chapters_file
    else
        printfn "Extracting chapters: %s" chapters_file.FullName
        let res = WinProcessUtil.ExecuteProcessSynchronously("mkvextract.exe", "chapters \"" + tmp_file.FullName + "\"", "")

        try
            if res.ExitCode <> 0 then
                System.Console.WriteLine res.StandardOutputContents
                System.Console.WriteLine res.StandardErrorContents
                failwith ("chapters extraction failed to extract " + chapters_file.FullName)

            let hopefullyXml = Regex.Match(res.StandardOutputContents, @"<.*>").Value

            System.Xml.Linq.XDocument.Parse(hopefullyXml) |> ignore
            addCleaner cleaners (fun () -> chapters_file.Delete())
            File.WriteAllText(chapters_file.FullName, hopefullyXml)
            Some chapters_file
        with
            | _ -> None

let identify_output_file out_dir (src_file:FileInfo) =
    let name = Regex.Replace(Path.GetFileNameWithoutExtension(src_file.Name), @"(bluray|x264|-rovers)", "")
    let spaced_name = Regex.Replace(name, @"[ .]+", " ")
    file_within_ext out_dir spaced_name "-output.mkv"
    

let recombine_video_audio_chapters out_dir cleaners (video_file:FileInfo) (opus_file:FileInfo) (maybe_chapters_file: FileInfo option) (output_file:FileInfo) =
    
    let chapter_options = 
        match maybe_chapters_file with
            | None -> ""
            | Some(chapters_file) -> " --chapters \"" + chapters_file.FullName + "\""
    printfn "Merging output: %s" output_file.FullName
 
    let res = WinProcessUtil.ExecuteProcessSynchronously("mkvmerge.exe", "\"" + video_file.FullName + "\" \"" + opus_file.FullName + "\" -o \"" + output_file.FullName + "\" " + chapter_options, "")

    if res.ExitCode <> 0 then
        System.Console.WriteLine res.StandardOutputContents
        System.Console.WriteLine res.StandardErrorContents
        failwith ("Failed to merge " + output_file.FullName)
    

let processFile (out_dir:DirectoryInfo) (src_file:FileInfo) =
    printfn "Processing %s" src_file.FullName
    let output_file = identify_output_file out_dir src_file
    if output_file.Exists then
        printfn "Already exists: %s, skipping." output_file.FullName
    else
        let cleaners = initializeCleaners()
        try
            let avs_file = generate_avs_script out_dir cleaners src_file
            let video_file = recompress_video out_dir cleaners avs_file
            let flac_file = extract_audio out_dir cleaners src_file
            let opus_file = compress_audio out_dir cleaners flac_file
            let maybe_chapters_file = extract_chapters out_dir cleaners src_file
            recombine_video_audio_chapters out_dir cleaners video_file opus_file maybe_chapters_file output_file
            cleanup cleaners
            ()
        finally
            printfn "Finished: %s" output_file.FullName
            ()

let acceptable_extensions = Set.ofList [ ".mkv"; ".mp4"; ".avi" ]
    

let processFiles (in_dir:DirectoryInfo) (out_dir:DirectoryInfo) = 
    let files = in_dir.GetFiles() |> Array.filter (fun f -> f.Extension |> acceptable_extensions.Contains)
    Parallel.ForEach(files, new ParallelOptions(MaxDegreeOfParallelism = 2), processFile out_dir) |> ignore
    ()


[<EntryPoint>]
let main argv = 
    System.Diagnostics.Process.GetCurrentProcess().PriorityClass <- System.Diagnostics.ProcessPriorityClass.Idle
    processFiles (new DirectoryInfo(argv.[0]))  (new DirectoryInfo @"E:\TmpVideoOut\")

    0 // return an integer exit code
