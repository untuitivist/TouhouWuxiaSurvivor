local root=app.params["root"] or app.fs.currentPath
local edition=app.params["edition"] or "03b"
if not edition:match("^[a-z0-9]+$") then error("ASCII edition required") end
local source=root.."/art/trace/reimu_front_"..edition..".aseprite"
local output=root.."/artifacts/aseprite-tracing/reimu-front-"..edition
if app.fs.isFile(source) then
    print("FRONT_STUDY_SKIPPED existing_source="..source)
    return
end
app.fs.makeAllDirectories(output)
local painter=dofile(root.."/tools/aseprite/pixel_tools.lua")
local sprite=Sprite(112,60,ColorMode.RGB)
painter.begin(sprite)
local palette={}
local paletteHex={
    ink="170d12",inkSoft="29171d",hairDark="48252b",hairShade="663536",
    hair="773f40",hairMid="884b49",hairLight="9b6966",hairGlint="b58073",
    redDark="660e20",redShade="b71429",red="e72737",redLight="ee394d",
    redGlint="ff7979",redEdge="ba5e65",white="fcf6f5",paper="f3e4e3",
    clothMid="d4c3ce",clothShade="ad969e",clothDark="776577",
    skinLight="f9e6d0",skin="fbdec9",skinMid="efb894",skinShade="d98967",
    goldLight="ffe074",gold="f9982b",goldShade="c65e1e",
    trousers="26191b",trousersLight="482c2d",boot="350711",sole="9c302e"
}
for name,hex in pairs(paletteHex) do palette[name]=painter.color(hex) end
local draw=dofile(root.."/tools/aseprite/reimu_front_clusters.lua")
draw(painter,palette)
painter.finish()
sprite:saveCopyAs(output.."/reimu-front.png")
local finalImage=Image{fromFile=output.."/reimu-front.png"}
local preview=Image(finalImage)
preview:resize(896,480)
preview:saveAs(output.."/reimu-front-8x.png")
local drawingLayers={}
for _,layer in ipairs(sprite.layers) do drawingLayers[#drawingLayers+1]=layer.name end
painter.layer("GUIDE - anatomy landmarks - hidden locked")
local guideColor=painter.color("48cbd0",180)
painter.line(52,25,52,38,guideColor)
painter.line(45,38,61,38,guideColor)
painter.line(52,38,53,47,guideColor)
painter.line(48,47,59,47,guideColor)
painter.line(45,38,39,40,guideColor)
painter.line(39,40,34,39,guideColor)
painter.line(61,38,68,40,guideColor)
painter.line(68,40,74,39,guideColor)
painter.line(48,47,47,50,guideColor)
painter.line(47,50,47,54,guideColor)
painter.line(59,47,61,49,guideColor)
painter.line(61,49,58,52,guideColor)
painter.line(42,55,65,55,painter.color("e9b969",150))
painter.finish()
painter.activeLayer.isVisible=false
painter.activeLayer.isEditable=false
local board=Image{fromFile=root.."/art/reference/reimu-gameplay-style-corrected.png"}
local reference=Image(board,Rectangle(704,503,112,60))
local referenceLayer=sprite:newLayer()
referenceLayer.name="REFERENCE - approved front pose - hidden locked"
sprite:newCel(referenceLayer,1,reference,Point(0,0))
referenceLayer.isVisible=false
referenceLayer.isEditable=false
sprite:saveAs(source)
sprite:close()
local comparison=Sprite(236,72,ColorMode.RGB)
painter.begin(comparison)
painter.layer("Comparison background")
painter.rect(0,0,236,72,painter.color("142638"))
painter.rect(117,0,2,72,painter.color("b98861"))
painter.finish()
local referenceSide=comparison:newLayer()
referenceSide.name="LEFT - approved reference, not artwork"
comparison:newCel(referenceSide,1,reference,Point(3,6))
local studySide=comparison:newLayer()
studySide.name="RIGHT - scripted Aseprite drawing study"
comparison:newCel(studySide,1,finalImage,Point(121,6))
comparison:saveCopyAs(output.."/comparison-native.png")
local comparisonImage=Image{fromFile=output.."/comparison-native.png"}
comparisonImage:resize(944,288)
comparisonImage:saveAs(output.."/comparison-4x.png")
comparison:close()
local report={schema=1,source=source:sub(#root+2),editor="Aseprite",
    method="authored_pixel_clusters_applied_by_aseprite_script",
    manual_mouse_painting=false,source_pixel_replay=false,reference_resized=false,
    runtime_eligible=false,visual_approval=false,frames=1,width=112,height=60,
    reference="art/reference/reimu-gameplay-style-corrected.png",
    reference_rectangle={704,503,112,60},palette=paletteHex,drawing_layers=drawingLayers,
    limitations={"Single front-pose study, not an animation","Reference fidelity requires visual review",
        "No runtime integration or game-view change"}}
local file=assert(io.open(output.."/report.json","wb"))
file:write(json.encode(report).."\n")
file:close()
print("ASEPRITE_FRONT_STUDY_CREATED source="..source.." drawing_layers="..#drawingLayers.." visual_approval=false")
