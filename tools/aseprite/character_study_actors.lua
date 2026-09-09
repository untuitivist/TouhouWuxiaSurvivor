return function(art, witch)
local colors={ink="302d3a",edge="534351",skin="efd0b3",skinShade="c4988a",skinLight="f8e0c4",blush="d9a09a",paper="eee3c9",light="fff3d8",paperShade="bcb9ad",paperMid="d5ceba",red="b64550",redShade="763b4b",redDeep="543347",redLight="d46963",hair="44343f",hairMid="62434b",hairLight="896052",gold="cba16b",goldLight="f0d498",goldShade="8f6e50",night="343b4c",nightMid="4d5467",nightLight="727789",nightDeep="272c3c",blonde="d8b578",blondeShade="a57b58",blondeLight="f4d799",blue="9daeb3"}
local palette={}
for name,hex in pairs(colors) do palette[name]=art.color(hex) end
local function polygon(points,fill,outline)
    art.poly(points,palette[fill])
    if outline then
        local previous=points[#points]
        for _,point in ipairs(points) do art.line(previous[1],previous[2],point[1],point[2],palette[outline]);previous=point end
    end
end
local function line(left,top,right,bottom,color,width) art.line(left,top,right,bottom,palette[color],width) end
local function rect(left,top,width,height,color) art.rect(left,top,width,height,palette[color]) end
local function dot(horizontal,vertical,color) art.pixel(horizontal,vertical,palette[color]) end
local function ellipse(horizontal,vertical,width,height,color) art.ellipse(horizontal,vertical,width,height,palette[color]) end
local cloth=witch and "night" or "red"
local shade=witch and "nightDeep" or "redShade"
local mid=witch and "nightMid" or "redLight"
local hair=witch and "blonde" or "hair"
local hairShade=witch and "blondeShade" or "hairMid"
local hairLight=witch and "blondeLight" or "hairLight"
art.layer("01 Back hair and theme silhouette")
if witch then
    polygon({{53,33},{70,31},{78,41},{80,63},{87,75},{81,74},{84,82},{75,78},{69,68},{51,75},{45,69},{48,60},{44,51}},hairShade,"ink")
    polygon({{49,43},{52,48},{50,61},{47,68},{53,66},{51,73},{57,68},{58,48}},hair)
    polygon({{74,43},{76,56},{73,65},{78,74},{76,65},{80,71},{77,54}},hairLight)
    line(16,163,109,80,"ink",4);line(17,162,110,80,"goldShade",2);line(19,161,110,80,"gold")
    polygon({{14,158},{25,155},{30,163},{20,175},{6,184},{3,179},{7,170}},"goldShade","ink")
    polygon({{14,162},{23,159},{25,163},{17,171},{6,179},{9,170}},"gold")
    for index=0,3 do line(9+index*3,173-index*3,6+index*3,180-index,"goldLight") end
    line(21,158,27,164,"night",2)
else
    polygon({{50,29},{70,28},{78,37},{79,57},{83,77},{77,72},{79,91},{73,85},{70,66},{49,72},{43,82},{44,70},{39,76},{43,53},{42,40}},hair,"ink")
    polygon({{45,44},{49,35},{49,58},{45,72},{45,65},{42,70}},hairShade)
    line(74,44,75,69,hairShade,2);line(75,69,77,79,hairShade)
end
art.layer("02 Footwork and cloth tails")
polygon({{49,137},{60,137},{59,154},{55,173},{47,173},{49,156}},"skinShade","ink")
polygon({{65,136},{75,134},{75,153},{83,170},{76,175},{67,156}},"skinShade","ink")
polygon({{51,143},{57,142},{56,157},{52,168},{49,168}},"skin")
polygon({{69,142},{73,141},{72,154},{79,168},{76,170},{69,155}},"skin")
polygon({{48,160},{55,161},{53,174},{47,175}},witch and "night" or "paper","ink")
polygon({{75,160},{79,160},{85,173},{78,177},{76,168}},witch and "night" or "paper","ink")
line(49,162,53,162,witch and "nightLight" or "light")
line(77,162,79,166,witch and "nightLight" or "light")
polygon({{47,171},{53,171},{54,177},{52,181},{38,181},{38,178},{45,175}},witch and "goldShade" or "redDeep","ink")
polygon({{78,172},{83,170},{88,175},{93,176},{94,180},{86,183},{80,180}},witch and "goldShade" or "redDeep","ink")
line(40,179,50,179,witch and "gold" or "redShade");line(84,179,91,179,witch and "gold" or "redShade")
line(41,182,51,182,"ink");line(86,184,93,181,"ink")
if not witch then
    polygon({{68,91},{79,95},{83,110},{93,122},{88,122},{94,129},{84,125},{76,113},{74,101}},"paperShade","ink")
    polygon({{71,94},{77,96},{81,110},{89,121},{86,119},{78,109}},"paper")
    line(86,123,90,125,"red")
end
art.layer("03 Bodice and layered skirt")
polygon({{52,65},{61,62},{69,65},{77,71},{73,87},{75,97},{82,111},{90,132},{85,138},{74,145},{61,149},{46,146},{30,137},{35,127},{43,110},{48,96},{46,83},{44,73}},shade,"ink")
polygon({{54,66},{61,65},{68,68},{72,75},{68,87},{70,99},{64,104},{49,100},{51,85},{48,75}},cloth)
polygon({{53,68},{58,67},{55,79},{56,88},{52,94},{50,88},{51,78}},mid)
polygon({{51,100},{61,103},{66,100},{69,110},{75,132},{74,143},{63,146},{48,143},{35,135},{43,119}},cloth)
polygon({{48,111},{52,103},{53,115},{45,133},{48,141},{38,135}},mid)
polygon({{62,106},{66,107},{64,119},{69,139},{63,144},{59,128}},mid)
polygon({{73,108},{79,121},{85,133},{78,139},{79,130}},cloth)
line(53,112,48,133,shade);line(70,113,76,136,shade,2)
line(56,123,54,140,shade);line(35,137,47,143,"paperShade",3)
line(47,144,61,147,"paper",2);line(62,147,75,143,"light",2)
line(76,143,86,136,"paper",2)
for index=0,5 do dot(40+index*7,140+math.floor(math.sin(index*0.6)*5),"light") end
if witch then
    polygon({{52,67},{56,66},{58,80},{66,82},{70,69},{73,72},{70,90},{70,98},{74,113},{82,131},{73,140},{60,143},{47,137},{44,133},{51,115},{55,96},{53,85}},"paperShade","edge")
    polygon({{56,72},{58,78},{65,83},{69,77},{66,93},{68,101},{64,105},{56,101},{58,94}},"paper")
    polygon({{56,102},{66,103},{68,116},{77,131},{71,137},{60,139},{48,133},{53,119}},"paper")
    polygon({{57,109},{60,107},{58,121},{54,131},{61,135},{69,134},{72,136},{61,138},{49,132}},"light")
    line(65,115,69,126,"paperShade");line(59,124,58,132,"paperMid")
    polygon({{69,87},{81,82},{80,90},{87,95},{76,98},{70,93}},"paper","edge")
    line(75,87,73,91,"light");line(74,94,82,94,"paperShade")
    polygon({{52,92},{68,92},{69,96},{53,97}},"night","edge")
    rect(59,93,5,3,"gold");rect(60,94,3,1,"nightDeep")
    line(56,98,54,107,"goldShade");dot(54,108,"goldLight")
else
    polygon({{52,65},{60,67},{64,65},{69,69},{65,80},{61,77},{57,81},{49,70}},"paper","edge")
    line(52,68,58,76,"light",2);line(67,68,63,75,"light")
    polygon({{60,72},{64,74},{63,78},{65,88},{61,91},{58,87},{60,78},{58,75}},"gold","goldShade")
    line(61,78,60,86,"goldLight");dot(60,75,"light")
    polygon({{49,92},{70,92},{72,97},{66,101},{48,98}},"paperShade","edge")
    polygon({{50,93},{69,93},{70,96},{64,98},{49,96}},"paper")
    line(51,95,68,96,"redShade")
    polygon({{67,95},{73,94},{78,98},{74,101},{69,98},{65,101},{63,98}},"red","edge")
    dot(69,97,"goldLight")
    line(69,99,72,112,"goldShade");line(71,101,74,112,"gold")
    rect(71,112,4,2,"goldShade");line(72,114,72,118,"gold")
end
art.layer("04 Gesture sleeves and identity props")
if witch then
    polygon({{48,68},{42,69},{39,77},{42,85},{47,87},{52,82},{53,73}},cloth,"ink")
    polygon({{43,71},{46,70},{45,80},{42,82},{41,77}},mid)
    polygon({{43,82},{49,83},{47,94},{42,103},{37,109},{33,106},{38,99},{40,91}},"skinShade","ink")
    polygon({{43,85},{47,85},{45,94},{40,101},{38,101},{42,92}},"skin")
    polygon({{35,103},{39,104},{40,108},{36,113},{31,112},{30,109},{33,108}},"skin","edge")
    line(34,109,36,106,"skinLight");dot(33,111,"skinShade")
    polygon({{71,69},{77,69},{82,74},{81,83},{77,87},{70,83},{68,77}},cloth,"ink")
    line(77,72,79,78,mid,2)
    polygon({{75,83},{80,82},{83,90},{91,96},{89,101},{80,96},{76,89}},"skinShade","ink")
    polygon({{79,85},{81,92},{89,97},{88,99},{81,94},{78,87}},"skin")
    polygon({{88,96},{94,93},{97,95},{97,99},{92,102},{88,101}},"skin","edge")
    line(91,97,94,95,"skinLight");line(92,100,96,97,"skinShade")
    polygon({{27,73},{30,79},{37,80},{32,85},{33,92},{27,88},{21,91},{22,84},{17,80},{24,79}},"gold","goldShade")
    polygon({{27,76},{29,81},{34,81},{30,84},{31,88},{27,86},{24,88},{25,83},{21,81},{26,81}},"goldLight")
    line(17,69,17,73,"paper");line(15,71,19,71,"paper");dot(37,72,"gold")
else
    polygon({{45,68},{41,72},{36,81},{29,87},{31,92},{40,87},{49,77}},"skinShade","ink")
    line(43,73,36,82,"skin",3)
    polygon({{37,77},{41,80},{35,90},{31,110},{21,115},{12,108},{18,91},{28,83}},"paperShade","ink")
    polygon({{35,80},{38,81},{31,94},{28,109},{21,111},{15,107},{21,93}},"paper")
    polygon({{33,82},{34,84},{25,99},{24,108},{18,108},{22,96}},"light")
    line(15,107,22,112,"red",3);line(23,112,29,109,"red",2)
    line(28,92,23,105,"paperShade");line(32,94,30,103,"paperMid")
    for index=0,3 do line(29+index*2,83-index*2,31+index*2,85-index*2,"red") end
    polygon({{20,111},{25,113},{25,117},{22,121},{18,120},{17,116}},"skin","edge")
    line(19,116,19,119,"skinShade")
    polygon({{72,69},{78,73},{83,83},{91,76},{95,79},{85,91},{80,92},{73,82}},"skinShade","ink")
    line(76,74,81,83,"skin",3)
    polygon({{86,79},{90,77},{99,82},{97,99},{92,108},{84,104},{81,95}},"paperShade","ink")
    polygon({{88,81},{91,80},{96,84},{93,97},{91,104},{86,101},{84,95}},"paper")
    polygon({{91,82},{93,83},{90,96},{91,102},{87,100},{86,95}},"light")
    line(85,102,92,106,"red",2);line(93,104,96,99,"red",2)
    polygon({{90,77},{92,70},{96,68},{100,70},{99,73},{96,74},{95,81}},"skin","edge")
    line(94,70,96,70,"skinLight");line(94,74,97,73,"skinShade")
    line(99,47,96,82,"goldShade",2);line(100,47,97,82,"gold")
    polygon({{99,47},{108,49},{104,55},{109,57},{104,65},{110,66},{103,74},{100,69},{103,65},{98,62},{103,56},{99,53}},"paper","edge")
    line(102,49,104,50,"light",2);line(102,58,105,58,"light",2);line(101,63,103,64,"paperShade")
    polygon({{17,113},{13,121},{18,124},{23,115}},"paper","goldShade")
    line(18,117,16,120,"red");dot(18,119,"red")
end
art.layer("05 Face and individually grouped hair locks")
polygon({{54,53},{66,54},{65,64},{69,67},{63,71},{55,65}},"skinShade","edge")
polygon({{57,56},{63,57},{62,64},{65,66},{61,68},{56,64}},"skin")
polygon({{48,34},{57,28},{68,29},{75,37},{75,48},{72,57},{65,63},{58,62},{51,57},{48,49}},hair,"ink")
ellipse(50,48,3,4,"skinShade");ellipse(74,48,2,4,"skinShade")
polygon({{53,36},{64,33},{71,38},{73,46},{71,55},{65,60},{59,60},{53,55},{51,47}},"skinShade")
polygon({{54,37},{64,35},{69,39},{71,46},{69,55},{64,58},{59,57},{54,53},{52,47}},"skin")
polygon({{55,39},{62,36},{66,38},{65,43},{56,45},{53,45}},"skinLight")
line(54,46,59,45,"edge");line(65,45,70,44,"edge")
rect(55,47,5,3,"light");rect(65,46,5,3,"light")
rect(57,46,2,4,witch and "goldShade" or "redShade");rect(67,45,2,4,witch and "goldShade" or "redShade")
dot(57,46,"ink");dot(67,45,"ink");dot(58,47,"light");dot(68,46,"light")
line(55,50,59,50,"skinShade");line(66,49,70,49,"skinShade")
line(54,43,58,42,hairShade);line(66,41,69,42,hairShade)
line(62,49,61,52,"skinShade");dot(62,52,"skinLight")
line(62,55,64,55,"skinShade");dot(63,56,"skinLight")
line(54,52,57,53,"blush");line(68,51,70,51,"blush")
polygon({{47,38},{51,30},{61,27},{71,30},{76,36},{76,44},{73,47},{70,43},{68,37},{67,44},{63,41},{61,35},{59,44},{55,41},{54,36},{51,44},{49,50},{46,48}},hair,"ink")
polygon({{49,36},{53,31},{60,29},{67,31},{61,31},{56,34},{53,40},{50,44}},hairShade)
polygon({{57,32},{60,30},{59,37},{58,40},{56,38}},hairLight)
polygon({{64,32},{67,33},{66,39},{65,37}},hairShade)
line(71,35,73,42,hairShade);line(49,41,48,46,hairLight)
if witch then
    polygon({{49,44},{51,46},{49,54},{54,59},{51,63},{54,68},{50,72},{46,68},{49,63},{46,58},{47,51}},hair,"edge")
    line(49,49,48,54,hairLight);line(49,58,51,60,hairLight)
    line(49,64,51,67,hairLight);rect(49,71,3,2,"paper")
    polygon({{50,73},{46,78},{50,77},{52,80},{54,74}},hair,"edge")
    polygon({{73,44},{76,43},{76,54},{74,59},{78,64},{76,67},{72,62},{71,55}},hair,"edge")
    line(74,47,74,53,hairLight);line(74,59,76,63,hairLight)
else
    polygon({{48,44},{51,46},{50,58},{48,67},{44,69},{47,61},{46,56}},hair,"ink")
    line(49,49,48,59,hairShade);line(47,58,46,64,hairLight)
    polygon({{73,44},{75,43},{77,53},{76,62},{79,69},{74,67},{71,57}},hair,"ink")
    line(74,48,75,58,hairShade);line(74,59,75,64,hairLight)
    polygon({{46,58},{50,59},{49,64},{45,63}},"paper","edge")
    line(46,60,49,61,"red");polygon({{73,59},{77,58},{78,63},{74,64}},"paper","edge")
    line(75,61,77,60,"red")
end
art.layer("06 Signature hat or shrine ribbon")
if witch then
    polygon({{34,36},{43,29},{50,17},{60,4},{68,7},{67,15},{76,27},{91,34},{92,39},{81,42},{66,40},{53,40},{40,42},{32,40}},"nightDeep","ink")
    polygon({{43,33},{52,18},{61,6},{65,8},{63,16},{70,26},{76,32},{64,35},{51,35}},"night")
    polygon({{50,26},{55,16},{61,8},{64,9},{60,19},{61,27},{56,31}},"nightMid")
    line(58,13,60,11,"nightLight");line(48,27,53,20,"nightMid")
    polygon({{46,29},{54,30},{65,29},{71,27},{75,31},{67,34},{55,35},{44,33}},"paperShade","edge")
    line(48,30,56,32,"paper",2);line(57,32,68,31,"light",2)
    polygon({{35,37},{43,34},{56,37},{67,36},{78,36},{89,36},{85,39},{77,40},{66,38},{54,39},{40,40}},"nightMid")
    line(36,38,45,37,"nightLight");line(80,38,88,37,"nightLight")
    polygon({{72,30},{75,23},{80,24},{80,31},{89,29},{89,36},{83,39},{76,35}},"paperShade","ink")
    polygon({{74,29},{76,25},{78,26},{77,32}},"light")
    polygon({{80,33},{87,31},{86,35},{82,37}},"paper")
    rect(76,31,4,4,"goldShade");rect(77,31,2,2,"goldLight")
else
    polygon({{48,29},{38,17},{41,11},{55,15},{62,23},{71,15},{81,13},{82,20},{76,33},{66,28},{61,31},{55,28}},"redShade","ink")
    polygon({{48,26},{41,17},{43,14},{53,18},{58,25},{53,27}},"red")
    polygon({{65,24},{73,18},{79,16},{78,22},{73,29},{67,27}},"red")
    line(43,15,50,18,"redLight",2);line(71,21,77,18,"redLight")
    line(40,17,48,27,"paper",2);line(79,17,74,28,"paper",2)
    line(42,19,45,21,"light");line(77,20,76,23,"light")
    polygon({{57,23},{63,22},{67,25},{65,30},{59,30},{56,27}},"redDeep","ink")
    polygon({{58,24},{62,24},{64,26},{62,29},{59,28}},"red")
    line(59,24,61,24,"redLight")
end
end
