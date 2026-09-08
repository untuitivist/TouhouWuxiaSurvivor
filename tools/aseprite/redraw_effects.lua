return function(art)
local palette=art.palette
local function talisman(centerX,centerY,scale)
    art.poly({{centerX-4*scale,centerY-7*scale},{centerX+4*scale,centerY-6*scale},{centerX+3*scale,centerY+7*scale},{centerX-4*scale,centerY+6*scale}},palette.ink)
    art.rect(centerX-3*scale,centerY-6*scale,6*scale,12*scale,palette.gold)
    art.rect(centerX-2*scale,centerY-6*scale,4*scale,10*scale,palette.white)
    art.line(centerX,centerY-4*scale,centerX,centerY+3*scale,palette.red,scale)
    art.line(centerX-scale,centerY-2*scale,centerX+scale,centerY-scale,palette.red,scale)
    art.line(centerX-scale,centerY+scale,centerX+scale,centerY+2*scale,palette.redShade,scale)
end
art.talisman=talisman
for _,name in ipairs({"ofuda","red_pellet","violet_pellet","star","stardust","experience","healing","dream","orb","grass"}) do
    local size=(name=="dream" or name=="orb") and 32 or name=="grass" and 48 or 16
    art.save("combat/"..name,size,size,function()
        art.layer("Pixel silhouette")
        if name=="grass" then
            art.rect(0,0,48,48,palette.jadeShade)
            for row=0,47 do for column=0,47 do
                local hash=(column*37+row*19+column*row*3)%101
                if hash==0 and column%7==0 and row%5==0 then art.rect(column,row,3,2,art.color("2d514f")) end
                if hash==20 and row%9==0 then art.pixel(column,row,palette.jade) end
            end end
            for _,point in ipairs({{7,8},{26,17},{39,38},{15,31}}) do art.line(point[1],point[2],point[1]-1,point[2]-3,palette.jade);art.line(point[1],point[2],point[1]+2,point[2]-2,palette.jade) end
        elseif name=="ofuda" then talisman(8,8,1)
        elseif name=="dream" or name=="orb" then art.orb(16,16,14,0)
        elseif name=="healing" then
            art.poly({{5,2},{10,2},{10,5},{13,8},{13,13},{10,15},{5,15},{2,12},{2,8},{5,5}},palette.ink)
            art.rect(6,2,4,3,palette.gold);art.ellipse(8,10,4,4,palette.redShade);art.ellipse(7,9,3,3,palette.red)
            art.rect(6,8,4,3,palette.paper);art.rect(7,7,2,5,palette.paper);art.pixel(5,8,palette.white)
        elseif name=="experience" then
            art.poly({{8,1},{14,6},{12,12},{7,15},{2,9},{3,4}},palette.ink)
            art.poly({{8,3},{12,6},{10,11},{7,13},{4,9},{5,5}},palette.jade)
            art.poly({{8,3},{9,7},{7,11},{5,8},{5,5}},palette.mint);art.line(6,5,8,4,palette.white)
        elseif name=="star" or name=="stardust" then
            art.star(8,8,8,palette.ink);art.star(8,8,6.8,name=="star" and palette.gold or palette.violet)
            art.star(8,7,4.5,name=="star" and palette.yellow or palette.lavender);art.rect(7,6,2,3,palette.white)
        else
            art.ellipse(8,8,7,7,palette.ink)
            art.ellipse(8,8,6,6,name=="red_pellet" and palette.redShade or palette.violet)
            art.ellipse(7,7,4,4,name=="red_pellet" and palette.red or palette.lavender)
            art.ellipse(7,6,2,2,palette.white);art.pixel(10,11,palette.gold)
        end
        art.layer("Separated highlight layer")
        if name~="grass" and name~="ofuda" then art.pixel(size/2-2,size/2-3,palette.white) end
    end)
end
art.save("effects/reimu_talisman",32,32,function() art.layer("Paper and vermilion ink");talisman(16,16,2);art.layer("Fold");art.line(10,5,16,6,palette.paper) end)
art.save("effects/master_spark",256,128,function()
    art.layer("Stepped luminous silhouette")
    art.poly({{2,64},{19,45},{46,27},{82,14},{128,8},{255,8},{255,120},{128,120},{82,113},{46,101},{19,83}},art.color("8072aa",95))
    art.poly({{8,64},{30,43},{65,28},{126,19},{255,19},{255,110},{126,110},{65,101},{30,84}},palette.violet)
    art.poly({{17,64},{43,44},{87,32},{128,29},{255,29},{255,99},{128,99},{87,97},{43,84}},palette.lavender)
    art.poly({{28,64},{54,49},{104,39},{255,38},{255,90},{104,89},{54,79}},palette.paper)
    art.poly({{36,64},{77,52},{132,48},{255,48},{255,80},{132,80},{77,76}},palette.white)
    art.layer("Starlight and longitudinal rays")
    for _,entry in ipairs({{31,51,106,37},{36,79,124,96},{91,25,255,25},{134,16,255,16},{93,104,255,104},{160,93,255,93}}) do art.line(entry[1],entry[2],entry[3],entry[4],palette.yellow,2) end
    art.star(55,64,19,palette.white);art.star(94,35,7,palette.yellow);art.star(117,93,5,palette.paper)
end)
art.save("effects/marisa_cast",64,64,function()
    art.layer("Star halo");art.ring(32,32,27,1,art.color("8072aa",100),2);art.ring(32,32,23,1,palette.gold)
    art.star(32,32,24,palette.violet);art.star(32,32,20,palette.gold);art.star(32,31,15,palette.yellow)
    art.layer("White starlight");art.star(32,29,8,palette.white);art.star(10,13,4,palette.white);art.star(53,46,3,palette.paper)
end)
for _,entry in ipairs({{"ritual_array",256},{"reimu_seal_ink",128},{"reimu_seal",128},{"reimu_aura",96}}) do
    local name,size=entry[1],entry[2]
    art.save("effects/"..name,size,size,function()
        local center=size/2;local radius=size*0.42
        art.layer("Nested seal geometry")
        local ink=name=="reimu_seal_ink" and palette.paper or palette.red
        art.ring(center,center,radius,1,ink,2);art.ring(center,center,radius-5,1,palette.gold);art.ring(center,center,radius*0.66,1,ink)
        for index=0,7 do
            local angle=index*math.pi/4
            local horizontal,vertical=center+math.cos(angle)*radius*0.82,center+math.sin(angle)*radius*0.82
            art.line(horizontal-3,vertical,horizontal+3,vertical,ink,2);art.line(horizontal,vertical-3,horizontal,vertical+3,ink)
            local nextAngle=angle+math.pi/2
            art.line(center+math.cos(angle)*radius*0.63,center+math.sin(angle)*radius*0.63,center+math.cos(nextAngle)*radius*0.63,center+math.sin(nextAngle)*radius*0.63,ink)
        end
        art.layer("Inner balance and sparks")
        art.orb(center,center,math.floor(radius*0.34),0)
        for index=0,3 do local angle=index*math.pi/2;art.star(center+math.cos(angle)*(radius+4),center+math.sin(angle)*(radius+4),3,palette.white) end
    end)
end
end
