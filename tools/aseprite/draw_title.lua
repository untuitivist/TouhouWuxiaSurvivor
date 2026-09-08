local project = app.params["root"] or "."
dofile(project .. "/tools/aseprite/title_painter.lua")({
    root = project,
    source = app.params["source"] or project .. "/art/title/moonlit_shrine.aseprite",
    texture = app.params["texture"] or project .. "/assets/ui/title/moonlit_shrine.png"
})
