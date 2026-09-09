local root=app.params["root"] or app.fs.currentPath
local painter=dofile(root.."/tools/aseprite/pixel_tools.lua")
local draw=dofile(root.."/tools/aseprite/character_study_actors.lua")
local folder=root.."/art/character-study"
app.fs.makeAllDirectories(folder)
local drawings={}
for _,name in ipairs({"reimu","marisa"}) do
    local sprite=Sprite(128,192,ColorMode.RGB)
    painter.begin(sprite)
    local brush={}
    for key,value in pairs(painter) do brush[key]=value end
    draw(brush,name=="marisa")
    painter.finish()
    sprite:saveAs(folder.."/"..name..".aseprite")
    sprite:saveCopyAs(folder.."/"..name..".png")
    local flattened=Image(sprite.spec)
    flattened:drawSprite(sprite,1)
    drawings[name]=flattened
    sprite:close()
end
local sheet=Sprite(960,600,ColorMode.RGB)
painter.begin(sheet)
painter.layer("Moonlit reference palette")
local ink=painter.color("252e42")
local paper=painter.color("f2e3bc")
local muted=painter.color("b7b6ad")
local gold=painter.color("bb8c4e")
painter.rect(0,0,960,600,ink)
painter.rect(24,65,446,496,painter.color("34404b"))
painter.rect(490,65,446,496,painter.color("34404b"))
painter.line(24,65,469,65,gold)
painter.line(490,65,935,65,gold)
painter.line(24,560,469,560,gold)
painter.line(490,560,935,560,gold)
local font={
 A={"01110","10001","10001","11111","10001","10001","10001"},B={"11110","10001","10001","11110","10001","10001","11110"},
 C={"01111","10000","10000","10000","10000","10000","01111"},D={"11110","10001","10001","10001","10001","10001","11110"},
 E={"11111","10000","10000","11110","10000","10000","11111"},F={"11111","10000","10000","11110","10000","10000","10000"},
 G={"01111","10000","10000","10111","10001","10001","01111"},H={"10001","10001","10001","11111","10001","10001","10001"},
 I={"11111","00100","00100","00100","00100","00100","11111"},J={"00111","00010","00010","00010","10010","10010","01100"},
 K={"10001","10010","10100","11000","10100","10010","10001"},L={"10000","10000","10000","10000","10000","10000","11111"},
 M={"10001","11011","10101","10101","10001","10001","10001"},N={"10001","11001","11001","10101","10011","10011","10001"},
 O={"01110","10001","10001","10001","10001","10001","01110"},P={"11110","10001","10001","11110","10000","10000","10000"},
 Q={"01110","10001","10001","10001","10101","10010","01101"},R={"11110","10001","10001","11110","10100","10010","10001"},
 S={"01111","10000","10000","01110","00001","00001","11110"},T={"11111","00100","00100","00100","00100","00100","00100"},
 U={"10001","10001","10001","10001","10001","10001","01110"},V={"10001","10001","10001","10001","10001","01010","00100"},
 W={"10001","10001","10001","10101","10101","10101","01010"},X={"10001","10001","01010","00100","01010","10001","10001"},
 Y={"10001","10001","01010","00100","00100","00100","00100"},Z={"11111","00001","00010","00100","01000","10000","11111"},
 ["0"]={"01110","10001","10011","10101","11001","10001","01110"},["1"]={"00100","01100","00100","00100","00100","00100","01110"},
 ["2"]={"01110","10001","00001","00010","00100","01000","11111"},["3"]={"11110","00001","00001","01110","00001","00001","11110"},
 ["4"]={"00010","00110","01010","10010","11111","00010","00010"},["6"]={"00110","01000","10000","11110","10001","10001","01110"},
 ["8"]={"01110","10001","10001","01110","10001","10001","01110"},["9"]={"01110","10001","10001","01111","00001","00010","01100"},
 ["-"]={"00000","00000","00000","11111","00000","00000","00000"},["/"]={"00001","00001","00010","00100","01000","10000","10000"}}
local function text(value,left,top,scale,color)
    for position=1,#value do
        local glyph=font[value:sub(position,position)]
        if glyph then for row,line in ipairs(glyph) do for column=1,#line do if line:sub(column,column)=="1" then painter.rect(left+(position-1)*6*scale+(column-1)*scale,top+(row-1)*scale,scale,scale,color) end end end end
    end
end
text("CHARACTER STUDY / 01",26,24,3,paper)
text("ASEPRITE - NOT IN GAME",670,34,1,muted)
local function paste(image,left,top,scale,width,height)
    for row=0,(height or image.height)*scale-1 do
        for column=0,(width or image.width)*scale-1 do
            local pixel=image:getPixel(math.floor(column/scale),math.floor(row/scale))
            if app.pixelColor.rgbaA(pixel)>0 then painter.pixel(left+column,top+row,pixel) end
        end
    end
end
for index,name in ipairs({"reimu","marisa"}) do
    local left=(index-1)*466
    painter.layer(name.." original Aseprite design study")
    text(name=="reimu" and "REIMU HAKUREI" or "MARISA KIRISAME",left+46,84,2,paper)
    paste(drawings[name],left+50,119,2)
    text("DETAIL / 2X",left+110,523,1,muted)
    text("BEFORE / 48",left+337,143,1,muted)
    local old=app.open(root.."/assets/aseprite/redraw/players/"..name..".png")
    local oldImage=Image(old.spec);oldImage:drawSprite(old,1)
    paste(oldImage,left+351,168,1,48,48)
    old:close()
    text("STUDY / 48",left+337,248,1,muted)
    paste(drawings[name],left+359,273,0.25)
    text("STUDY / 144",left+337,353,1,muted)
    paste(drawings[name],left+311,373,0.75)
end
painter.layer("Scope label")
text("PROPORTION / COSTUME STUDY - NO RUNTIME REPLACEMENT - NO ANIMATION YET",26,579,1,muted)
painter.finish()
sheet:saveAs(folder.."/comparison.aseprite")
sheet:saveCopyAs(folder.."/comparison.png")
sheet:close()
print("CHARACTER_STUDY_PASS 2 layered characters and comparison sheet")
