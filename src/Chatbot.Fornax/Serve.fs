module Serve

open Microsoft.AspNetCore.Builder

let serve () =
    let builder =
        WebApplication.CreateBuilder(new WebApplicationOptions(WebRootPath = Config.OutputFolderName))

    let app = builder.Build()

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles(new StaticFileOptions()) |> ignore
    app.UseHttpsRedirection() |> ignore

    app.Run()
