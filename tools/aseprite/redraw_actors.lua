return function(art)
local palette=art.palette
local function girl(marisa,phase)
    local step=({0,1,0,-1})[phase+1]
    local ink=art.color("302c37")
    local hair=marisa and art.color("af864d") or art.color("44313c")
    local hairLight=marisa and art.color("e3c17f") or art.color("71505a")
    local skin=art.color("ebc3a4")
    local skinShade=art.color("ba897c")
    local cloth=marisa and art.color("393f4f") or art.color("b73e48")
    local shade=marisa and art.color("262d3a") or art.color("753c4c")
    local lit=marisa and art.color("666b76") or art.color("dc675a")
    local cream=art.color("eee4cf")
    local white=art.color("fff1dd")
    art.rect(21,37+step,3,8,skinShade);art.rect(26,37-step,3,8,skinShade)
    art.rect(22,38+step,2,5,cream);art.rect(26,38-step,2,5,cream)
    art.rect(20,44+step,4,2,shade);art.rect(26,44-step,4,2,shade)
    art.poly({{20,21},{27,21},{29,26},{34,36},{31,40},{20,41},{14,37},{18,27}},ink)
    art.poly({{21,22},{26,22},{28,27},{32,35},{30,39},{20,39},{16,36},{19,27}},shade)
    art.poly({{22,23},{26,23},{27,28},{30,35},{27,38},{19,37},{18,35},{21,27}},cloth)
    art.line(20,29,18,35,lit);art.line(24,28,23,36,lit)
    art.line(28,31,29,36,shade);art.line(17,37,22,39,cream)
    art.line(22,39,29,38,white);art.line(29,38,31,36,cream)
    if marisa then
        art.poly({{22,22},{26,22},{26,28},{30,35},{26,38},{20,37},{18,35},{22,28}},cream)
        art.poly({{24,23},{26,23},{24,29},{27,35},{23,37},{20,35},{23,28}},white)
        art.line(26,31,28,35,art.color("c4bca7"))
        art.poly({{20,23},{17,25},{15,30},{18,32},{21,27}},shade)
        art.line(17,28,16,30,lit);art.rect(17,31,2,2,skin)
        art.poly({{28,23},{31,26},{33,30},{30,33},{27,28}},shade)
        art.line(30,27,32,30,lit);art.rect(29,31,2,2,skin)
        art.line(8,38,38,24,art.color("775940"),2)
        art.line(9,38,38,24,art.color("bd8a50"))
        art.poly({{4,38},{10,34},{14,39},{7,43},{3,43}},art.color("775940"))
        art.line(5,39,10,37,art.color("e3b873"));art.line(5,41,11,38,art.color("bd8a50"))
    else
        art.poly({{20,23},{17,24},{13,30},{14,33},{19,32},{21,27}},ink)
        art.poly({{19,24},{17,26},{14,30},{15,32},{18,31},{20,26}},cream)
        art.line(14,30,18,31,cloth);art.pixel(16,27,white)
        art.poly({{28,23},{31,25},{34,30},{33,33},{28,30},{27,26}},ink)
        art.poly({{28,24},{30,26},{32,30},{31,32},{29,29}},white)
        art.line(30,29,32,30,cloth)
        art.rect(20,26,8,2,cream);art.rect(21,27,6,1,art.color("d8bb86"))
        art.poly({{22,21},{25,22},{24,27},{22,26}},art.color("e3b873"))
        art.line(33,20,33,31,art.color("bd8a50"))
        art.poly({{32,19},{36,20},{35,23},{37,24},{34,27},{33,24},{34,22}},white)
    end
    art.poly({{20,9},{25,8},{29,11},{30,18},{29,24},{26,24},{27,18},{20,20},{18,23},{17,20},{18,13}},ink)
    art.poly({{20,10},{25,9},{28,12},{29,19},{27,23},{26,19},{19,19},{18,21},{19,13}},hair)
    art.rect(21,12,6,7,skinShade);art.rect(21,12,5,6,skin)
    art.rect(22,12,3,2,cream)
    art.pixel(21,15,ink);art.pixel(25,15,ink);art.pixel(26,17,skinShade)
    art.pixel(23,18,skinShade)
    art.poly({{20,11},{24,10},{28,12},{27,15},{25,12},{23,14},{21,13},{20,16}},hair)
    art.line(20,12,23,11,hairLight);art.rect(19,15,1,6,hairLight)
    art.line(27,16,28,21,hairLight)
    if marisa then
        art.poly({{12,14},{18,11},{22,3},{26,5},{28,10},{34,13},{35,16},{28,17},{18,17}},ink)
        art.poly({{15,14},{20,11},{23,5},{25,6},{27,12},{32,14},{28,16},{18,16}},shade)
        art.poly({{20,11},{23,5},{25,6},{23,11}},lit)
        art.line(20,11,27,12,cream,2)
        art.line(15,14,20,13,lit);art.line(28,15,32,14,lit)
        art.poly({{27,11},{30,10},{30,12},{33,13},{31,15},{28,14}},white)
        art.rect(28,12,2,2,art.color("d8bb86"))
    else
        art.poly({{18,10},{16,5},{22,6},{24,8},{27,5},{31,5},{30,11},{26,10},{22,10}},ink)
        art.poly({{18,8},{18,6},{22,7},{23,9},{20,9}},cloth)
        art.poly({{25,8},{28,6},{30,6},{29,10},{26,9}},cloth)
        art.line(18,6,19,8,cream);art.line(29,6,29,9,cream)
        art.rect(23,7,3,3,shade);art.rect(23,7,2,1,lit)
        art.rect(18,18,2,2,cream);art.rect(28,19,2,2,cream)
    end
end
for _,name in ipairs({"reimu","marisa"}) do
    art.save("players/"..name,192,48,function()
        art.layer("Character silhouette and costume")
        for frame=0,3 do art.offset(frame*48,frame%2);girl(name=="marisa",frame) end
        art.offset();art.layer("Moonlit edge accents")
        for frame=0,3 do art.offset(frame*48,frame%2);art.pixel(22,12,art.color("eee4cf")) end
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
