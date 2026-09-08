return function(art)
local palette=art.palette
local function girl(marisa,phase)
    local step=({0,1,0,-1})[phase+1]
    art.rect(18,37+step,5,7,palette.ink);art.rect(26,37-step,5,7,palette.ink)
    art.rect(19,37+step,3,4,palette.paper);art.rect(27,37-step,3,4,palette.paper)
    art.rect(17,43+step,6,2,palette.redShade);art.rect(26,43-step,7,2,palette.redShade)
    art.poly({{17,22},{30,22},{37,36},{33,40},{15,40},{10,36}},palette.ink)
    art.poly({{18,23},{29,23},{34,36},{30,39},{16,38},{13,35}},marisa and palette.deep or palette.redShade)
    art.poly({{20,24},{28,24},{31,36},{24,38},{15,35}},marisa and palette.blue or palette.red)
    art.line(17,35,30,38,palette.paper,2);art.line(14,34,17,36,palette.white)
    if marisa then
        art.poly({{21,25},{27,25},{31,36},{25,38},{17,35}},palette.paper)
        art.line(22,27,20,34,palette.white,2);art.line(28,32,30,35,palette.gold)
        art.line(8,35,39,21,palette.goldShade,2);art.line(8,34,38,21,palette.gold)
        art.poly({{3,34},{10,31},{13,38},{3,41}},palette.goldShade)
        art.line(4,35,10,35,palette.yellow);art.line(3,38,10,37,palette.gold,2)
    else
        art.poly({{16,22},{10,25},{7,32},{13,35},{17,30}},palette.ink)
        art.poly({{15,23},{11,26},{9,31},{13,33},{16,29}},palette.paper)
        art.line(10,30,13,31,palette.red,2)
        art.poly({{31,22},{36,24},{41,30},{37,34},{30,29}},palette.ink)
        art.poly({{31,24},{35,25},{39,30},{36,32},{31,28}},palette.white)
        art.line(35,29,38,30,palette.red,2)
        art.rect(37,19,2,10,palette.goldShade)
        art.poly({{36,17},{40,18},{39,23},{42,24},{39,27},{37,24}},palette.white)
        art.line(18,25,29,25,palette.gold,2)
    end
    art.poly({{15,9},{20,6},{29,7},{34,12},{33,23},{28,27},{15,24},{13,17}},palette.ink)
    art.poly({{17,10},{23,8},{29,9},{31,13},{31,23},{27,25},{16,22}},marisa and palette.goldShade or palette.hair)
    art.rect(18,13,12,9,palette.skinShade);art.rect(18,13,11,7,palette.skin)
    art.rect(19,13,8,4,palette.paper)
    art.rect(19,16,2,3,palette.ink);art.rect(27,16,2,3,palette.ink)
    art.pixel(19,16,palette.white);art.pixel(27,16,palette.white)
    art.pixel(18,20,palette.rose);art.pixel(29,20,palette.rose);art.rect(23,21,2,1,palette.skinShade)
    art.poly({{16,11},{21,9},{29,10},{31,14},{27,14},{25,12},{24,15},{21,13},{18,15}},marisa and palette.yellow or palette.hair)
    art.line(17,11,21,10,marisa and palette.white or palette.hairLight)
    if marisa then
        art.rect(15,16,3,8,palette.gold);art.rect(30,15,3,10,palette.gold)
        art.rect(15,20,2,3,palette.yellow);art.rect(31,21,2,3,palette.yellow)
        art.poly({{9,12},{16,8},{20,1},{27,2},{31,8},{38,11},{35,15},{12,15}},palette.ink)
        art.poly({{12,12},{18,9},{22,3},{26,4},{29,9},{35,12},{31,14},{16,14}},palette.blue)
        art.poly({{18,9},{23,3},{26,4},{24,9}},palette.slate)
        art.line(19,9,29,10,palette.paper,2)
        art.poly({{29,8},{33,7},{33,10},{36,12},{31,12}},palette.white)
        art.rect(30,10,2,2,palette.gold)
    else
        art.poly({{14,11},{11,2},{20,4},{24,6},{29,3},{35,2},{34,12},{26,9},{22,9}},palette.ink)
        art.poly({{15,9},{13,4},{20,5},{23,7},{19,9}},palette.red)
        art.poly({{25,7},{30,5},{33,4},{32,10},{28,9}},palette.red)
        art.line(14,4,16,8,palette.white);art.line(32,4,32,8,palette.paper)
        art.rect(22,6,4,4,palette.redShade);art.rect(22,6,3,2,palette.rose)
        art.rect(14,17,3,7,palette.hair);art.rect(31,17,3,7,palette.hair)
        art.rect(13,20,4,2,palette.paper);art.rect(31,20,4,2,palette.paper)
        art.poly({{21,23},{25,23},{26,28},{23,30},{21,27}},palette.gold)
    end
end
for _,name in ipairs({"reimu","marisa"}) do
    art.save("players/"..name,192,48,function()
        art.layer("Character silhouette and costume")
        for frame=0,3 do art.offset(frame*48,frame%2);girl(name=="marisa",frame) end
        art.offset();art.layer("Moonlit edge accents")
        for frame=0,3 do art.offset(frame*48,frame%2);art.pixel(17,13,palette.white);art.pixel(29,12,palette.paper) end
    end)
end
local function orb(centerX,centerY,radius,phase)
    art.ellipse(centerX,centerY,radius,radius,palette.ink)
    art.ellipse(centerX,centerY,radius-1,radius-1,palette.goldShade)
    art.ellipse(centerX,centerY,radius-2,radius-2,palette.paper)
    for row=-radius+3,radius-3 do
        local outer=math.floor(math.sqrt(math.max(0,(radius-3)^2-row^2)))
        local boundary=math.floor(math.sin(row/(radius-3)*math.pi)*radius*0.32)
        art.rect(centerX-outer,centerY+row,math.max(0,outer+boundary+1),1,palette.redShade)
    end
    art.ellipse(centerX-1,centerY-radius*0.4,2,2,palette.paper)
    art.ellipse(centerX+1,centerY+radius*0.4,2,2,palette.redShade)
    art.line(centerX-radius+4,centerY-5,centerX-radius+7,centerY-9,palette.white)
end
art.orb=orb
for _,name in ipairs({"kedama","wild_fairy","mountain_spirit","great_youkai","yin_yang_orb"}) do
    art.save("actors/"..name,192,48,function()
        art.layer("Enemy identity and four-frame motion")
        for frame=0,3 do
            art.offset(frame*48,frame%2)
            if name=="yin_yang_orb" then orb(24,23,16,frame)
            elseif name=="kedama" then
                art.cluster(24,24,16,13,palette.ink);art.cluster(24,23,14,11,palette.slate)
                art.cluster(24,21,12,9,palette.mist);art.cluster(21,19,8,6,palette.paper)
                for _,point in ipairs({{11,21},{15,13},{24,10},{34,14},{38,22},{33,33},{19,36}}) do art.line(point[1],point[2],point[1]-2,point[2]-3,palette.mist,2) end
                art.rect(18,22,3,4,palette.ink);art.rect(28,22,3,4,palette.ink)
                art.pixel(18,22,palette.white);art.pixel(28,22,palette.white);art.rect(23,28,3,1,palette.goldShade)
            elseif name=="wild_fairy" then
                local wing=frame%2*3
                art.poly({{20,21},{4,11+wing},{6,25},{16,30},{8,35},{21,31}},palette.ink)
                art.poly({{28,21},{44,11+wing},{42,25},{32,30},{40,35},{27,31}},palette.ink)
                art.poly({{18,22},{7,15+wing},{9,25},{18,28}},palette.mint)
                art.poly({{30,22},{41,15+wing},{39,25},{30,28}},palette.mint)
                art.line(9,20+wing,18,25,palette.white);art.line(39,20+wing,30,25,palette.white)
                art.rect(20,35,3,7,palette.skinShade);art.rect(26,35,3,7,palette.skinShade)
                art.poly({{19,21},{29,21},{34,36},{24,39},{14,35}},palette.ink)
                art.poly({{20,22},{28,22},{31,34},{24,37},{17,34}},palette.violet)
                art.poly({{23,24},{27,24},{29,33},{21,35}},palette.lavender)
                art.ellipse(24,15,8,8,palette.ink);art.ellipse(24,16,6,6,palette.skin)
                art.poly({{16,14},{19,8},{27,7},{32,13},{29,17},{26,12},{22,15},{18,13}},palette.jade)
                art.line(19,10,25,9,palette.mint,2);art.rect(20,16,2,2,palette.ink);art.rect(27,16,2,2,palette.ink)
                art.star(31,9,4,palette.yellow);art.rect(22,23,5,2,palette.gold)
            else
                local boss=name=="great_youkai"
                art.poly({{17,15},{28,13},{34,22},{39,38},{29,42},{13,39},{9,30}},palette.ink)
                art.poly({{18,16},{27,15},{31,23},{35,37},{28,40},{14,37},{13,29}},boss and palette.violet or palette.jadeShade)
                art.poly({{18,24},{26,21},{29,33},{22,38},{15,34}},boss and palette.redShade or palette.jade)
                art.line(20,23,25,35,palette.goldShade,2);art.line(15,37,29,39,palette.gold)
                art.poly({{16,13},{17,5},{23,9},{30,4},{32,16},{27,24},{19,23}},palette.ink)
                art.poly({{18,12},{18,8},{23,11},{29,7},{30,16},{26,22},{20,20}},palette.paper)
                art.line(19,15,22,16,palette.redShade,2);art.line(25,16,28,14,palette.redShade,2)
                art.poly({{22,17},{25,17},{24,21}},palette.goldShade)
                art.line(18,11,19,13,palette.red);art.line(28,10,27,12,palette.red)
                art.poly({{12,24},{7,28},{5,33},{13,30}},palette.paper)
                art.poly({{32,22},{39,26},{41,31},{33,28}},palette.paper)
                if boss then
                    art.poly({{16,24},{4,19},{9,28},{3,30},{15,34}},palette.deep)
                    art.poly({{31,24},{45,19},{40,28},{46,30},{32,34}},palette.deep)
                    art.line(8,23,13,28,palette.lavender);art.line(40,23,35,28,palette.lavender)
                    art.star(24,5,3,palette.gold)
                end
            end
        end
        art.offset();art.layer("Shared warm rim light")
        for frame=0,3 do art.pixel(frame*48+21,10+frame%2,palette.white) end
    end)
end
end
