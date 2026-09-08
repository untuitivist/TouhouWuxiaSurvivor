return function(art)
    dofile(art.root .. "/tools/aseprite/title_painter.lua")({
        root = art.root,
        source = art.root .. "/art/redraw/scenery/title_shrine.aseprite",
        texture = art.root .. "/assets/aseprite/redraw/scenery/title_shrine.png"
    })
    art.entries[#art.entries+1] = {name="scenery/title_shrine",width=640,height=360}
end
