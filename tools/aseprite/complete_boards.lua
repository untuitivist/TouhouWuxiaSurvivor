return function(art)
local painter=dofile(art.root.."/tools/aseprite/pixel_tools.lua")
local text=dofile(art.root.."/tools/aseprite/study_text.lua")(painter)
local color=art.palette
for _,hero in ipairs({"reimu","marisa"}) do
    local sprite=Sprite(1024,768,ColorMode.RGB)
    painter.begin(sprite);painter.layer("01 Ink board and vermilion panel frames")
    painter.rect(0,0,1024,768,color.night);painter.rect(12,12,1000,744,color.redDark);painter.rect(14,14,996,740,color.night)
    painter.rect(26,84,278,654,color.wood);painter.rect(28,86,274,650,color.deep)
    painter.rect(318,84,680,338,color.wood);painter.rect(320,86,676,334,color.deep)
    painter.rect(318,438,680,300,color.wood);painter.rect(320,440,676,296,color.deep)
    painter.layer("02 Design labels")
    text(hero=="reimu" and "REIMU HAKUREI" or "MARISA KIRISAME",30,30,3,color.paper)
    text("TOUHOU / WUXIA / SURVIVOR",537,39,2,color.woodLight)
    text("FULL BODY",53,98,2,color.paper);text("FOUR DIRECTIONS / IDLE WALK CAST",337,99,2,color.paper)
    text("INDEPENDENT EFFECTS",337,452,2,color.paper)
    for row,label in ipairs({"FRONT","RIGHT","BACK","LEFT"}) do text(label,337,135+(row-1)*69,1,color.mist) end
    text("LAYERED ASEPRITE SOURCES",44,690,1,color.paper);text("SCRIPT DRAWING / NOT MOUSE PAINTED",336,711,1,color.mist)
    painter.finish()
    local portrait=sprite:newLayer();portrait.name="03 Full body portrait from editable drawing";sprite:newCel(portrait,1,art.images["portraits/"..hero],Point(35,171))
    local movement=sprite:newLayer();movement.name="04 Complete directional animation sheet";sprite:newCel(movement,1,art.images["players/"..hero],Point(453,126))
    local names=hero=="reimu" and {"effects/reimu_talisman","actors/yin_yang_orb","effects/reimu_seal_ink"} or {"combat/star","effects/marisa_cast","effects/master_spark"}
    for index,name in ipairs(names) do
        local image=art.images[name]
        if name=="actors/yin_yang_orb" then image=Image(image,Rectangle(0,0,32,32)) end
        local layer=sprite:newLayer();layer.name="05 Effect - "..name;sprite:newCel(layer,1,image,Point(369+(index-1)*185,505))
    end
    local labelLayer=sprite:newLayer();labelLayer.name="06 Palette and source scope"
    local paletteImage=Image(240,22,ColorMode.RGB)
    for index,name in ipairs({"ink","hair","red","paper","gold","skin","hat","blond"}) do
        for horizontal=(index-1)*30,(index-1)*30+25 do for vertical=0,19 do paletteImage:drawPixel(horizontal,vertical,color[name]) end end
    end
    sprite:newCel(labelLayer,1,paletteImage,Point(48,642))
    art.finish("boards/"..hero,sprite,art.flatten(sprite),{method="design_board_from_new_editable_sources",runtime_art=false})
end
app.fs.makeAllDirectories(art.previewRoot)
for _,name in ipairs({"players/reimu","players/marisa","portraits/reimu","portraits/marisa","boards/reimu","boards/marisa","scenery/title_shrine"}) do
    local filename=name:gsub("/","-")
    art.images[name]:saveAs(art.previewRoot.."/"..filename..".png")
end
end
