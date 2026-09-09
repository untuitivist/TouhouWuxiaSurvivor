return function(art)
local color=art.palette
local function masonry(width,height,variant)
    local stoneTones={"70766f","747970","6b736d","777b72"}
    art.layer("01 Weathered grout bed");art.rect(0,0,width,height,art.color("555f59"))
    art.layer("02 Broad flagstones with chipped edges")
    local slots=variant%2==0 and {{0,0,32,32},{32,0,32,32},{0,32,64,32}} or {{0,0,64,32},{0,32,32,32},{32,32,32,32}}
    for index,slot in ipairs(slots) do
        local horizontal,vertical,span,depth=slot[1],slot[2],slot[3],slot[4]
        art.poly({{horizontal+2,vertical+1},{horizontal+span-3,vertical+1},{horizontal+span-1,vertical+3},{horizontal+span-2,vertical+depth-2},{horizontal+3,vertical+depth-1},{horizontal+1,vertical+depth-4}},art.color(stoneTones[(variant+index)%4+1]))
        art.line(horizontal+4,vertical+2,horizontal+span-6,vertical+2,art.color("879087"))
        art.line(horizontal+2,vertical+5,horizontal+2,vertical+depth-8,art.color("7b837b"))
        art.line(horizontal+span-2,vertical+6,horizontal+span-3,vertical+depth-4,art.color("626c63"))
        if index==2 then art.line(horizontal+span-3,vertical+13,horizontal+span-9,vertical+18,art.color("626c63"));art.line(horizontal+span-9,vertical+18,horizontal+span-8,vertical+22,art.color("626c63")) end
        art.cluster(horizontal+8,vertical+depth-5,4,1,art.color("707970"))
    end
    art.layer("03 Moss in seams and sparse maple leaves")
    for index=0,variant>=2 and 8 or 2 do
        local horizontal=4+(index*23+variant*7)%55;local vertical=index%2==0 and 31 or 61
        art.cluster(horizontal,vertical,variant>=2 and 5 or 2,1,color.moss)
        if variant>=2 then art.line(horizontal,vertical-1,horizontal+3,vertical-1,color.jade) end
        if variant>=2 and index%4==0 then art.poly({{horizontal,vertical-5},{horizontal+2,vertical-8},{horizontal+3,vertical-5},{horizontal+6,vertical-4},{horizontal+3,vertical-2},{horizontal+2,vertical},{horizontal+1,vertical-3},{horizontal-2,vertical-4}},color.redShade) end
    end
end
art.save("scenery/courtyard_tiles",256,64,function()
    for variant=0,3 do art.offset(variant*64,0);masonry(64,64,variant) end;art.offset()
end,{columns=4,rows=1,frame_width=64,frame_height=64})
art.save("combat/grass",48,48,function()
    art.layer("01 Forest ground");art.rect(0,0,48,48,color.moss)
    art.layer("02 Grass tufts and leaf litter")
    for index=0,49 do local horizontal=index*19%48;local vertical=index*29%48;art.line(horizontal,vertical,horizontal-1,vertical-3,color.jade);art.line(horizontal,vertical,horizontal+2,vertical-2,color.stoneDark);if index%9==0 then art.pixel(horizontal+1,vertical,color.redShade) end end
end)
local function torii()
    art.layer("01 Footing and upright posts")
    art.ellipse(64,138,57,6,art.color("111d2b",150))
    art.rect(24,51,11,83,color.redDark);art.rect(94,51,11,83,color.redDark)
    art.rect(25,53,5,79,color.red);art.rect(95,53,5,79,color.red)
    art.rect(22,128,17,10,color.stoneDark);art.rect(91,128,17,10,color.stoneDark)
    art.line(25,132,35,132,color.stoneLight);art.line(95,132,105,132,color.stoneLight)
    art.layer("02 Curved roof beam and double crossbar")
    art.poly({{4,28},{21,33},{107,33},{124,28},{120,38},{112,43},{16,43},{8,38}},color.ink)
    art.poly({{8,35},{23,37},{106,37},{120,34},{114,41},{15,41}},color.hatLight)
    art.rect(14,43,100,8,color.redDark);art.rect(17,44,96,4,color.red)
    art.rect(15,67,98,7,color.redDark);art.rect(17,67,94,3,color.redShade)
    art.rect(58,47,12,25,color.redDark);art.rect(60,49,7,19,color.red)
    art.layer("03 Rope tassels and sacred papers")
    for horizontal=32,94 do local vertical=77+math.floor(math.sin((horizontal-32)/62*math.pi)*6);art.pixel(horizontal,vertical,color.blondDark);art.pixel(horizontal,vertical-1,color.blond) end
    for horizontal=39,87,16 do art.poly({{horizontal,81},{horizontal+3,83},{horizontal+1,87},{horizontal+4,89},{horizontal+2,94},{horizontal-1,92},{horizontal+1,89},{horizontal-2,86}},color.paper) end
end
art.save("scenery/torii",128,144,torii,{foot_anchor=138})
local function tree(variant)
    art.layer("01 Shadow roots and branching trunk")
    art.ellipse(67,149,46,7,art.color("0b1826",115))
    art.poly({{56,65},{74,63},{78,95},{74,125},{84,148},{76,152},{65,145},{49,154},{40,151},{55,134},{60,112},{54,93}},color.ink)
    art.poly({{61,68},{68,68},{72,96},{68,125},{77,148},{68,142},{58,145},{62,128},{65,108},{59,90}},color.bark)
    art.line(65,86,66,125,color.wood,3);art.line(65,113,49,100,color.wood,3);art.line(68,99,83,85,color.wood,3)
    art.layer("02 Layered maple canopy silhouette")
    local dark=variant==0 and color.redDark or color.deep
    local middle=variant==0 and color.redShade or color.moss
    local light=variant==0 and color.red or color.jade
    art.cluster(64,61,61,49,color.ink);art.cluster(61,56,58,44,dark)
    for index=0,14 do
        local horizontal=19+(index*29)%89;local vertical=22+(index*17)%54
        local radius=12+index%5
        art.cluster(horizontal,vertical,radius,radius*0.65,middle)
        art.cluster(horizontal-2,vertical-4,radius*0.72,radius*0.35,light)
        art.line(horizontal-radius/2,vertical+radius/3,horizontal+radius/2,vertical+radius/3,dark)
    end
    art.layer("03 Individual leaf silhouettes and lit edges")
    for index=0,43 do
        local horizontal=13+(index*31)%104;local vertical=17+(index*23)%73
        art.poly({{horizontal-3,vertical},{horizontal-2,vertical-2},{horizontal,vertical-1},{horizontal+1,vertical-4},{horizontal+2,vertical-1},{horizontal+5,vertical},{horizontal+2,vertical+2},{horizontal+1,vertical+4},{horizontal-1,vertical+2}},index%4==0 and (variant==0 and color.redGlint or color.mint) or light)
    end
end
for variant=0,1 do art.save("scenery/tree_canopy_"..(variant==0 and "a" or "b"),128,160,function()tree(variant)end,{foot_anchor=151}) end
art.save("scenery/lantern",48,80,function()
    art.layer("01 Carved stone plinth and shaft")
    art.ellipse(24,74,20,4,art.color("0f1a26",110));art.rect(18,42,12,26,color.stoneDark)
    art.rect(20,43,5,24,color.stoneLight);art.poly({{14,66},{34,66},{39,74},{9,74}},color.stone)
    art.line(13,68,33,68,color.stoneLight)
    art.layer("02 Warm lantern window")
    art.rect(11,23,26,25,color.ink);art.rect(15,27,18,16,color.goldShade);art.rect(19,28,10,13,color.goldLight);art.rect(22,28,4,9,color.cream)
    art.line(15,34,33,34,color.wood,2);art.line(24,27,24,43,color.wood,2)
    art.layer("03 Pagoda cap and weathering")
    art.poly({{24,9},{34,18},{44,22},{42,26},{7,26},{4,22},{15,18}},color.ink)
    art.poly({{24,12},{33,21},{40,23},{10,23},{17,20}},color.stone)
    art.line(14,22,34,22,color.stoneLight);art.rect(21,6,6,6,color.stoneDark);art.pixel(23,6,color.stoneLight)
end,{foot_anchor=75})
local function shrine(width,height,variant)
    local middle=width/2;local base=height-12;local eave=height*0.42
    art.layer("01 Raised stone foundation and steps")
    art.ellipse(middle,base,width*0.46,6,art.color("10202e",120))
    art.rect(width*0.12,base-24,width*0.76,22,color.stoneDark)
    for step=0,3 do art.rect(middle-35-step*6,base-18+step*5,70+step*12,5,step%2==0 and color.stone or color.stoneLight) end
    art.layer("02 Timber frame screens and veranda")
    art.rect(width*0.15,eave,width*0.7,base-eave-24,color.bark)
    for index=0,5 do
        local horizontal=width*0.2+index*width*0.12
        art.rect(horizontal,eave+8,4,base-eave-35,color.wood)
        art.rect(horizontal+5,eave+12,width*0.085,base-eave-44,variant==1 and color.paper or color.redDark)
        for row=eave+16,base-37,7 do art.line(horizontal+5,row,horizontal+width*0.1,row,color.woodLight) end
    end
    art.rect(width*0.12,base-33,width*0.76,7,color.wood);art.line(width*0.12,base-33,width*0.88,base-33,color.woodLight,2)
    for index=0,7 do art.rect(width*0.14+index*width*0.1,base-51,3,17,color.redShade) end
    art.line(width*0.12,base-48,width*0.88,base-48,color.red,2)
    art.layer("03 Sweeping tiled roof and ridge")
    art.poly({{middle,9},{width*0.72,24},{width*0.9,eave-9},{width-3,eave-3},{width-8,eave+8},{width*0.85,eave+13},{width*0.14,eave+13},{5,eave+5},{2,eave-4},{width*0.14,eave-10},{width*0.28,26}},color.ink)
    art.poly({{middle,15},{width*0.69,29},{width*0.85,eave-6},{width-11,eave},{width-15,eave+5},{width*0.85,eave+8},{width*0.15,eave+8},{11,eave+1},{width*0.18,eave-7},{width*0.32,29}},color.blue)
    for row=0,5 do
        local vertical=25+row*(eave-25)/6;local half=width*0.17+row*width*0.055
        art.line(middle-half,vertical,middle+half,vertical,color.slate,2)
        for horizontal=middle-half+3,middle+half-3,9 do art.line(horizontal,vertical+2,horizontal+3,vertical+5,color.deep) end
    end
    art.line(width*0.13,eave+6,width*0.86,eave+6,color.mist,2)
    art.line(width*0.32,25,middle,13,color.mist,2);art.line(middle,13,width*0.7,28,color.slate,2)
    art.layer("04 Shrine sign rope and warm interior")
    art.rect(middle-12,eave+12,24,19,color.ink);art.rect(middle-9,eave+14,18,15,color.wood)
    art.line(middle,eave+17,middle,eave+25,color.paper,2);art.line(middle-4,eave+20,middle+4,eave+20,color.paper)
    art.rect(middle-11,base-71,22,32,color.goldShade);art.rect(middle-7,base-68,14,23,color.goldLight)
    art.line(middle,base-71,middle,base-40,color.wood,2)
    for horizontal=middle-35,middle+35,14 do art.poly({{horizontal,eave+36},{horizontal+3,eave+40},{horizontal+1,eave+44},{horizontal+3,eave+48},{horizontal,eave+52},{horizontal-2,eave+47},{horizontal,eave+43},{horizontal-2,eave+40}},color.paper) end
end
art.save("scenery/shrine_main",256,192,function()shrine(256,192,0)end,{foot_anchor=182})
art.save("scenery/storehouse",160,144,function()shrine(160,144,1)end,{foot_anchor=134})
art.save("scenery/subsidiary_shrine",128,128,function()shrine(128,128,2)end,{foot_anchor=118})
dofile(art.root.."/tools/aseprite/title_painter.lua")({
    root=art.root,
    palette={cherryDark="532c3d",cherry="893747",cherryMid="b95051",cherryLight="d77463",petal="f4a47a",night="18283a",sky="27394d",blue="34475b",dusk="4d5869"},
    finish=function(sprite) art.finish("scenery/title_shrine",sprite,art.flatten(sprite),{method="authored_aseprite_title_brushes_revised_palette"}) end
})
end
