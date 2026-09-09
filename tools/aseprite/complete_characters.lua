return function(art)
local color=art.palette
local reimuFront=dofile(art.root.."/tools/aseprite/reimu_front_clusters.lua")
local function ofuda(horizontal,vertical)
    art.rect(horizontal-3,vertical-7,7,13,color.ink);art.rect(horizontal-2,vertical-6,5,11,color.paper)
    art.rect(horizontal-1,vertical-5,3,1,color.red);art.line(horizontal,vertical-3,horizontal,vertical+2,color.red)
    art.line(horizontal-1,vertical-1,horizontal+1,vertical-2,color.red);art.pixel(horizontal+1,vertical+3,color.red)
end
local function hakkero(horizontal,vertical)
    art.poly({{horizontal-5,vertical-3},{horizontal-3,vertical-5},{horizontal+3,vertical-5},{horizontal+5,vertical-3},{horizontal+5,vertical+3},{horizontal+3,vertical+5},{horizontal-3,vertical+5},{horizontal-5,vertical+3}},color.ink)
    art.ring(horizontal,vertical,4,1,color.blondDark,1);art.ellipse(horizontal,vertical,2,2,color.goldLight);art.pixel(horizontal,vertical,color.cream)
end
local function frontLegs(phase,back,marisa)
    local left=phase==1 and 3 or phase==3 and -2 or phase==2 and 1 or phase==4 and -1 or 0
    local right=phase==3 and 3 or phase==1 and -2 or phase==4 and 1 or phase==2 and -1 or 0
    local trim=marisa and color.blond or color.red
    for _,leg in ipairs({{27,left},{37,right}}) do
        local horizontal,advance=leg[1],leg[2]
        art.poly({{horizontal-3,45},{horizontal+3,45},{horizontal+4,49},{horizontal+1,53+advance},{horizontal+2,57+advance},{horizontal-3,58+advance},{horizontal-5,55+advance},{horizontal-3,50}},color.ink)
        art.poly({{horizontal-2,46},{horizontal+2,46},{horizontal+2,49},{horizontal,53+advance},{horizontal-3,52+advance}},color.trousersLight)
        art.poly({{horizontal-3,53+advance},{horizontal+1,53+advance},{horizontal+2,56+advance},{horizontal,58+advance},{horizontal-4,57+advance}},color.hat)
        art.line(horizontal-3,54+advance,horizontal,55+advance,trim);art.line(horizontal-3,57+advance,horizontal,57+advance,color.wood)
        if not back then art.pixel(horizontal-2,54+advance,marisa and color.cream or color.redGlint) end
    end
end
local function reimuDown(column)
    local phase=column>=1 and column<=4 and column or 0
    local hop=(phase==2 or phase==4) and 1 or 0
    local casting=column>=5
    local active=true
    local wrapper={}
    function wrapper.layer(name)
        art.layer(name);active=not name:match("^03 ")
        local horizontal,vertical=-20,4+hop
        if name:match("^01 ") then horizontal=horizontal+(phase==1 and 1 or phase==3 and -1 or 0) end
        if name:match("^05 ") then horizontal=horizontal+(casting and -3 or phase==1 and -1 or 0);vertical=vertical+(casting and -4 or phase==3 and 1 or 0) end
        if name:match("^06 ") then vertical=vertical+(phase==1 and 1 or 0) end
        if not active then art.offset();frontLegs(phase,false,false) end
        art.offset(horizontal,vertical)
    end
    for _,name in ipairs({"pixel","rect","line","poly","ellipse"}) do wrapper[name]=function(...) if active then art[name](...) end end end
    reimuFront(wrapper,color);art.offset()
    if casting then art.layer("12 Casting hand and ofuda");ofuda(column==7 and 15 or 13,column==6 and 31 or column==5 and 35 or 38) end
end
local function bow(horizontal,vertical,back)
    art.poly({{horizontal-14,vertical-6},{horizontal-8,vertical-5},{horizontal-2,vertical},{horizontal+3,vertical},{horizontal+10,vertical-6},{horizontal+15,vertical-6},{horizontal+15,vertical+8},{horizontal+10,vertical+9},{horizontal+3,vertical+4},{horizontal-3,vertical+4},{horizontal-11,vertical+9},{horizontal-15,vertical+7}},color.ink)
    art.poly({{horizontal-13,vertical-4},{horizontal-9,vertical-3},{horizontal-3,vertical+1},{horizontal-10,vertical+7},{horizontal-13,vertical+6}},color.red)
    art.poly({{horizontal+5,vertical+1},{horizontal+12,vertical-4},{horizontal+14,vertical-4},{horizontal+13,vertical+7},{horizontal+10,vertical+7}},color.redShade)
    art.line(horizontal-13,vertical-3,horizontal-13,vertical,color.paper,2);art.line(horizontal+12,vertical-4,horizontal+12,vertical-1,color.paper,2)
    art.pixel(horizontal-13,vertical+4,color.paper);art.line(horizontal+12,vertical+4,horizontal+13,vertical+6,color.paper)
    art.rect(horizontal-2,vertical,5,4,back and color.redLight or color.redDark)
end
local function witchHat(horizontal,vertical,back,side)
    art.poly({{horizontal-19,vertical+6},{horizontal-11,vertical+2},{horizontal-5,vertical-9},{horizontal+1,vertical-12},{horizontal+5,vertical-7},{horizontal+11,vertical+2},{horizontal+20,vertical+6},{horizontal+17,vertical+10},{horizontal+4,vertical+12},{horizontal-11,vertical+11}},color.ink)
    art.poly({{horizontal-17,vertical+6},{horizontal-8,vertical+3},{horizontal-4,vertical-9},{horizontal+1,vertical-10},{horizontal+3,vertical-6},{horizontal+9,vertical+3},{horizontal+17,vertical+7},{horizontal+9,vertical+9},{horizontal-9,vertical+9}},color.hat)
    art.poly({{horizontal-5,vertical-6},{horizontal,vertical-12},{horizontal,vertical-6},{horizontal+5,vertical+2},{horizontal-8,vertical+4}},color.hatLight)
    art.line(horizontal-12,vertical+6,horizontal+12,vertical+7,color.clothDark)
    local shift=side and 6 or back and -6 or 5
    art.poly({{horizontal+shift-8,vertical-1},{horizontal+shift-4,vertical-4},{horizontal+shift,vertical+1},{horizontal+shift+7,vertical-3},{horizontal+shift+9,vertical},{horizontal+shift+5,vertical+6},{horizontal+shift,vertical+4},{horizontal+shift-5,vertical+6}},color.paper)
    art.line(horizontal+shift-5,vertical,horizontal+shift-1,vertical+3,color.clothShade);art.line(horizontal+shift+5,vertical,horizontal+shift+1,vertical+3,color.clothMid)
    art.rect(horizontal+shift-1,vertical+1,3,4,color.white)
end
local function face(horizontal,vertical,marisa)
    art.poly({{horizontal-11,vertical-7},{horizontal+9,vertical-8},{horizontal+12,vertical-2},{horizontal+10,vertical+5},{horizontal+6,vertical+8},{horizontal-4,vertical+9},{horizontal-10,vertical+5},{horizontal-12,vertical}},color.ink)
    art.poly({{horizontal-9,vertical-5},{horizontal+8,vertical-6},{horizontal+10,vertical-1},{horizontal+8,vertical+5},{horizontal+3,vertical+7},{horizontal-5,vertical+6},{horizontal-10,vertical+1}},color.skinShade)
    art.poly({{horizontal-8,vertical-4},{horizontal+7,vertical-4},{horizontal+8,vertical+1},{horizontal+5,vertical+5},{horizontal-4,vertical+5},{horizontal-8,vertical+2}},color.skin)
    art.rect(horizontal-5,vertical-1,2,4,color.ink);art.rect(horizontal+4,vertical-1,2,4,color.ink)
    art.pixel(horizontal-4,vertical+2,marisa and color.blondDark or color.redDark);art.pixel(horizontal+4,vertical+2,color.redDark)
end
local function marisaDown(column)
    local phase=column>=1 and column<=4 and column or 0
    local hop=(phase==2 or phase==4) and 1 or 0
    local cast=column>=5
    art.layer("01 Flowing golden hair");art.offset(0,hop)
    art.poly({{20,22},{43,20},{47,26},{45,31},{50,35},{49,39},{44,37},{44,42},{39,40},{32,42},{26,40},{19,41},{20,37},{15,38},{13,35},{18,30}},color.ink)
    art.poly({{20,24},{41,22},{44,27},{42,32},{47,35},{44,35},{42,38},{39,37},{35,40},{29,38},{23,39},{23,34},{18,36},{18,33}},color.blondDark)
    art.poly({{20,25},{24,25},{23,30},{21,33},{24,36},{21,37},{20,34},{17,35},{20,30}},color.blondLight)
    art.poly({{39,24},{42,26},{40,31},{44,34},{43,36},{39,34},{38,38},{35,37},{37,31}},color.blond)
    art.layer("02 Legs and boot lacing");art.offset();frontLegs(phase,false,true)
    art.layer("03 Black tunic white lapels and coat tails");art.offset(0,hop)
    art.poly({{24,34},{40,33},{44,37},{43,43},{46,49},{42,52},{35,49},{28,51},{22,50},{23,44},{21,39}},color.ink)
    art.poly({{25,35},{39,35},{41,39},{39,44},{43,48},{41,50},{35,47},{28,49},{24,49},{25,43}},color.hat)
    art.poly({{25,35},{28,35},{32,39},{30,42},{25,46},{24,48},{29,46},{33,42},{37,45},{41,49},{43,48},{39,42},{38,35},{35,36},{34,39}},color.paper)
    art.line(26,39,28,40,color.clothShade);art.line(36,41,40,46,color.clothShade)
    art.rect(28,44,11,2,color.blondDark);art.pixel(34,44,color.goldLight)
    art.layer("04 Detached sleeves and hands")
    art.poly({{23,35},{25,39},{23,44},{20,48},{16,46},{13,40},{15,37},{19,37}},color.ink)
    art.poly({{17,37},{22,36},{23,40},{21,44},{18,46},{15,40}},color.paper)
    art.line(17,40,20,44,color.clothShade);art.line(15,40,18,45,color.hatLight)
    art.poly({{42,35},{46,37},{49,37},{52,40},{49,45},{44,48},{41,44},{40,39}},color.ink)
    art.poly({{43,37},{47,38},{50,39},{48,43},{44,46},{42,42}},color.paper)
    art.line(44,41,46,43,color.clothShade);art.line(48,40,46,44,color.hatLight)
    art.layer("05 Braid and white hair tie")
    for step=0,5 do art.ellipse(24+(step%2),31+step*2,2,2,color.blondDark);art.pixel(24+(step%2),30+step*2,color.blondLight) end
    art.rect(22,40,5,2,color.paper);art.line(24,42,23,44,color.blond)
    art.layer("06 Face and layered bangs");face(32,28,true)
    art.poly({{20,22},{24,19},{37,19},{43,23},{42,28},{40,31},{38,27},{36,30},{33,25},{30,28},{27,25},{24,29},{22,30}},color.blondDark)
    art.poly({{22,23},{26,21},{32,21},{35,22},{39,22},{41,24},{39,28},{36,24},{34,27},{31,23},{29,26},{27,23},{24,27}},color.blondLight)
    art.line(27,22,29,24,color.blond);art.line(36,23,38,26,color.blond)
    art.layer("07 Witch hat and white bow");witchHat(32,15,false,false)
    art.layer("08 Hakkero held in the same hand");art.offset(0,cast and -2 or hop);hakkero(cast and 13 or 18,cast and (column==6 and 33 or column==5 and 36 or 38) or 39)
    art.offset()
end
local function back(column,marisa)
    local phase=column>=1 and column<=4 and column or 0
    local hop=(phase==2 or phase==4) and 1 or 0
    art.layer("01 Rear boots and step");frontLegs(phase,true,marisa)
    art.layer("02 Rear coat and moving sleeves");art.offset(0,hop)
    art.poly({{22,34},{41,34},{46,40},{47,47},{42,49},{39,47},{36,51},{28,51},{24,48},{20,49},{15,44},{17,38}},color.ink)
    art.poly({{23,35},{39,35},{41,43},{43,47},{38,45},{35,49},{29,49},{26,45},{22,46}},marisa and color.hat or color.redDark)
    art.poly({{19,37},{24,39},{22,44},{20,46},{17,43}},color.paper)
    art.poly({{42,37},{46,40},{45,44},{42,46},{40,41}},color.paper)
    art.line(18,43,21,46,marisa and color.hatLight or color.red);art.line(42,45,45,43,marisa and color.hatLight or color.red)
    art.rect(26,44,13,2,marisa and color.blondDark or color.trousers)
    art.layer("03 Hair silhouette with lagging tips")
    local hair=marisa and color.blond or color.hair
    local light=marisa and color.blondLight or color.hairLight
    art.poly({{21,19},{27,15},{37,15},{44,20},{45,27},{43,32},{48,36},{46,41},{42,38},{40,43},{35,40},{31,44},{27,41},{23,43},{21,39},{17,40},{15,36},{20,31}},color.ink)
    art.poly({{22,21},{28,17},{36,17},{42,22},{43,28},{41,33},{45,36},{43,38},{40,36},{38,41},{35,38},{31,41},{27,38},{24,40},{23,36},{19,38},{18,36},{22,30}},hair)
    art.poly({{24,22},{28,19},{29,25},{27,30},{27,35},{25,37},{25,31}},light)
    art.poly({{35,19},{39,22},{40,27},{37,31},{39,35},{37,38},{35,34},{36,28}},light)
    art.layer("04 Rear headwear")
if marisa then witchHat(32,17,true,false) else bow(32,18,true) end
if column>=5 then art.layer("05 Casting prop");local height=column==6 and 29 or column==5 and 32 or 35;if marisa then hakkero(46,height) else ofuda(46,height) end end
    art.offset()
end
local function side(column,left,marisa)
    local phase=column>=1 and column<=4 and column or 0
    local hop=(phase==2 or phase==4) and 1 or 0
    local stride=phase==1 and 3 or phase==3 and -3 or phase==2 and 1 or phase==4 and -1 or 0
    local cast=column>=5
    local reach=column==6 and 5 or column==5 and 2 or column==7 and 3 or 0
    local function point(horizontal) return left and 64-horizontal or horizontal end
    local function polygon(points,fill)
        local transformed={}
        for _,position in ipairs(points) do transformed[#transformed+1]={point(position[1]),position[2]} end
        art.poly(transformed,fill)
    end
    art.layer("01 Profile far trouser and planted heel")
    polygon({{26,45},{34,45},{34+stride,50},{30+stride,56},{32+stride,59},{25+stride,60},{23+stride,56},{26+stride,51}},color.ink)
    art.line(point(28),48,point(27+stride),54,color.trousersLight,4)
    art.line(point(26+stride),57,point(30+stride),57,marisa and color.blond or color.red,2)
    art.layer("02 Profile near knee and advancing boot")
    polygon({{33,45},{40,45},{41-stride,50},{38-stride,55},{42-stride,58},{41-stride,61},{34-stride,61},{31-stride,57},{33-stride,52}},color.ink)
    art.line(point(36),48,point(36-stride),54,color.hatLight,4)
    art.line(point(34-stride),57,point(38-stride),58,marisa and color.blond or color.red,2)
    art.line(point(34-stride),60,point(40-stride),60,color.wood)
    art.layer("03 Profile robe shoulder volume and split hem");art.offset(0,hop)
    polygon({{23,35},{36,33},{43,38},{45,43},{45,48},{40,53},{33,49},{26,53},{20,50},{23,43},{20,39}},color.ink)
    polygon({{25,36},{36,35},{40,39},{42,45},{43,48},{39,50},{33,47},{26,50},{23,49},{26,43},{23,40}},marisa and color.hat or color.redShade)
    polygon({{27,37},{30,37},{31,42},{27,47},{25,48},{27,43}},marisa and color.clothShade or color.redLight)
    art.line(point(37),37,point(36),45,color.paper,2)
    art.line(point(36),46,point(41),49,color.paper,2)
    art.line(point(26),44,point(39),44,color.trousers,2)
    art.pixel(point(35),44,color.goldLight)
    art.layer("04 Profile sleeve cuff and visible fingers")
    polygon({{34,35},{41,35},{43+reach,38},{48+reach,41},{46+reach,46},{41,50},{35,46},{32,40}},color.ink)
    polygon({{35,37},{40,37},{43+reach,40},{46+reach,42},{44+reach,45},{40,48},{36,44},{34,40}},color.paper)
    polygon({{35,40},{39,43},{43+reach,43},{41,47},{37,44}},color.clothShade)
    art.line(point(40),46,point(44+reach),43,marisa and color.hatLight or color.red,2)
    art.line(point(44+reach),39,point(47+reach),40,color.skin,2)
    art.layer("05 Profile long hair with overlapping locks")
    polygon({{21,18},{33,15},{42,19},{45,25},{42,32},{34,37},{28,38},{24,43},{19,40},{14,44},{10,41},{15,36},{13,31},{17,26}},color.ink)
    polygon({{22,20},{33,17},{40,20},{42,26},{39,31},{32,35},{27,36},{24,40},{20,37},{14,41},{13,40},{19,34},{16,31},{20,26}},marisa and color.blondDark or color.hairDark)
    polygon({{22,22},{27,21},{28,25},{24,30},{25,33},{22,36},{20,33},{17,37},{17,34},{20,29}},marisa and color.blond or color.hair)
    art.line(point(23),23,point(21),28,marisa and color.blondLight or color.hairLight,2)
    art.layer("06 Profile cheek jaw eyes and parted fringe")
    polygon({{29,22},{40,21},{44,25},{45,29},{48,31},{45,33},{44,36},{39,39},{32,38},{28,33},{26,27}},color.ink)
    polygon({{30,24},{40,23},{42,26},{43,30},{46,31},{43,32},{42,35},{38,37},{32,36},{29,32},{28,28}},color.skinShade)
    polygon({{32,25},{40,25},{41,28},{42,31},{44,31},{41,33},{41,34},{37,36},{33,35},{30,31}},color.skin)
    art.line(point(39),28,point(43),28,color.ink)
    art.line(point(41),29,point(41),33,color.ink,2)
    art.pixel(point(41),32,marisa and color.goldShade or color.redDark)
    art.pixel(point(40),29,color.paper)
    polygon({{25,21},{31,18},{40,19},{44,23},{44,28},{41,27},{38,24},{37,30},{34,32},{32,28},{29,34},{26,31},{25,26}},marisa and color.blond or color.hair)
    polygon({{28,22},{32,20},{37,21},{39,24},{37,23},{34,26},{33,24},{30,27},{28,30},{27,28}},marisa and color.blondLight or color.hairLight)
    art.layer("07 Profile hat or bow with folded trim")
    if marisa then witchHat(32,18,false,true) else bow(31,18,false) end
    art.layer("08 Profile identity prop and asymmetric ornaments")
    if marisa then
        for step=0,3 do art.ellipse(point(31),33+step*3,2,2,color.blond);art.pixel(point(31),32+step*3,color.blondLight) end
        art.line(point(29),44,point(33),44,color.paper)
        hakkero(left and (cast and 15 or 22) or (cast and 50 or 44),cast and (column==6 and 33 or 36) or 41)
    elseif cast then ofuda(left and 12 or 52,column==6 and 32 or column==5 and 35 or 37)
    else art.rect(left and 26 or 35,43,2,3,color.gold) end
    art.offset()
end
for _,hero in ipairs({"reimu","marisa"}) do
    art.animation("players/"..hero,64,64,8,4,function(column,row)
        if row==0 then if hero=="reimu" then reimuDown(column) else marisaDown(column) end
        elseif row==2 then back(column,hero=="marisa") else side(column,row==3,hero=="marisa") end
    end,{foot_anchor=60,reference="art/reference/"..(hero=="reimu" and "reimu-gameplay-style-corrected.png" or "marisa-gameplay-style-approved.png"),directions={"front","right","back","left"},idle_column=0,walk_columns={1,2,3,4},cast_columns={5,6,7}})
end
end
