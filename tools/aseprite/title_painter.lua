return function(options)
local project = options.root
local outputSource = options.source
local outputTexture = options.texture
local paint = dofile(project .. "/tools/aseprite/pixel_tools.lua")
local sprite = Sprite(640, 360, ColorMode.RGB)
sprite:assignColorSpace(ColorSpace{ sRGB = true })
paint.begin(sprite)
math.randomseed(20260908)
local function random(minimum, maximum)
    return math.random(math.ceil(minimum), math.floor(maximum))
end
local colors = {}
for name, hex in pairs({
    night="242c46", sky="303b55", blue="3b4660", dusk="49516a", mist="646479",
    ridge="444b60", mountain="343f52", distant="30444c", cedar="263e40", cedarLight="3d5650",
    leafDark="172f35", leaf="24453f", leafMid="35574a", leafLight="52705a", moss="687957",
    ground="1d3839", groundMid="29463e", shadow="172b33", stoneDark="3c5050", stone="63736a", stoneLight="93a18a", stoneRim="b4b59a",
    bark="443c42", barkLight="716054", barkShadow="2b3038",
    cherryDark="634355", cherry="89576d", cherryMid="b57b8a", cherryLight="d8a0a2", petal="efc1b0",
    redDark="633d49", red="a45553", redLight="cd7960", redRim="e9a477", roof="26353c", roofLight="566061",
    moonShade="c7c5a0", moon="e8dbae", moonLight="f6ecc7", crater="d6c598",
    goldDark="775940", gold="bd8a50", goldLight="e3b873", flame="ffe6a3", paper="eee1b9"
}) do colors[name] = paint.color(options.palette and options.palette[name] or hex) end

paint.layer("01 - Indigo sky")
paint.rect(0, 0, 640, 360, colors.night)
paint.rect(0, 38, 640, 42, colors.sky)
paint.rect(0, 80, 640, 42, colors.blue)
paint.rect(0, 122, 640, 62, colors.dusk)
paint.rect(0, 184, 640, 78, colors.mist)
for _, band in ipairs({{36,"29324b"},{38,"2c374f"},{78,"354159"},{80,"39445d"},{120,"414b64"},{122,"454e67"},{182,"55596f"},{184,"5d5f74"}}) do
    paint.rect(0,band[1],640,2,paint.color(band[2]))
end
for _, star in ipairs({{18,18},{92,8},{215,17},{316,30},{339,66},{394,19},{429,47},{461,12},{562,29},{611,17},{633,77},{347,102},{407,95},{307,138},{582,91}}) do
    paint.pixel(star[1], star[2], colors.moonShade)
end
for _, star in ipairs({{371,44},{598,55},{451,93}}) do
    paint.rect(star[1] - 1, star[2], 3, 1, colors.moonShade)
    paint.rect(star[1], star[2] - 1, 1, 3, colors.moonShade)
    paint.pixel(star[1], star[2], colors.moonLight)
end

paint.layer("02 - Moon and high cloud")
paint.ellipse(516, 79, 49, 49, paint.color("9291a0", 22))
paint.ellipse(516, 79, 45, 45, paint.color("b9b2a9", 34))
paint.ellipse(516, 79, 41, 41, colors.moonShade)
paint.ellipse(517, 77, 39, 39, colors.moon)
paint.ellipse(519, 73, 34, 34, colors.moonLight)
paint.cluster(533, 66, 10, 7, colors.moon)
paint.cluster(536, 65, 6, 5, colors.crater)
paint.cluster(501, 93, 8, 6, colors.moon)
paint.cluster(498, 93, 4, 3, colors.crater)
paint.cluster(524, 99, 3, 2, colors.moonShade)
paint.rect(490, 76, 2, 2, colors.crater)
paint.rect(510, 57, 3, 1, colors.moon)
paint.rect(534, 89, 3, 2, colors.moon)
paint.poly({{421,113},{443,113},{443,110},{468,110},{468,107},{503,107},{503,110},{526,110},{526,113},{553,113},{553,116},{457,116},{457,118},{430,118}}, paint.color("787c91", 100))
paint.rect(462, 118, 67, 1, paint.color("aaa3a0", 95))
paint.rect(565, 130, 58, 2, paint.color("989098", 100))

paint.layer("03 - Distant mountain ridges")
paint.poly({{0,195},{39,172},{72,187},{110,161},{154,183},{208,148},{249,167},{279,146},{313,173},{354,150},{382,167},{427,144},{462,170},{499,148},{538,167},{581,146},{640,171},{640,360},{0,360}}, colors.ridge)
paint.poly({{0,223},{42,209},{85,215},{128,190},{180,215},{231,199},{279,213},{321,186},{366,204},{411,182},{459,216},{510,192},{568,215},{611,197},{640,201},{640,360},{0,360}}, colors.mountain)
paint.poly({{0,260},{64,228},{113,246},{169,227},{220,239},{266,227},{303,244},{342,221},{381,229},{424,215},{474,247},{526,225},{578,235},{640,219},{640,360},{0,360}}, colors.distant)
for _, pine in ipairs({{320,203,39},{352,190,46},{397,177,63},{584,177,68},{623,199,54},{287,211,31},{54,205,39}}) do
    local centerX, top, height = pine[1], pine[2], pine[3]
    for tier = 0, 5 do
        local half = 5 + tier * 3
        paint.poly({{centerX,top + tier * height / 8},{centerX-half,top + tier * height / 8 + 15},{centerX+half,top + tier * height / 8 + 15}}, colors.cedar)
    end
end

paint.layer("04 - Moonlit cedar boughs")
for _, cedar in ipairs({{393,152},{582,147}}) do
    local centerX, top = cedar[1], cedar[2]
    paint.poly({{centerX-4,top+96},{centerX+5,top+96},{centerX+2,top+22},{centerX-3,top+22}},colors.barkShadow)
    paint.line(centerX,top+70,centerX-22,top+29,colors.bark,3)
    paint.line(centerX,top+61,centerX+24,top+20,colors.bark,3)
    for _, bough in ipairs({{-20,8,26,17},{7,-6,31,21},{29,13,24,18},{-6,27,38,18}}) do
        local posX, posY = centerX+bough[1],top+bough[2]
        paint.cluster(posX,posY,bough[3],bough[4],colors.cedar)
        paint.cluster(posX-3,posY-6,bough[3]*0.8,bough[4]*0.65,colors.cedarLight)
        for detail = 1, 8 do
            local leafX = posX + random(-bough[3]*0.65,bough[3]*0.5)
            local leafY = posY + random(-bough[4]*0.7,bough[4]*0.1)
            paint.line(leafX,leafY,leafX+3,leafY-2,colors.leafLight)
        end
    end
end

paint.layer("05 - Moss garden and stone approach")
paint.poly({{0,290},{60,270},{140,281},{226,264},{291,281},{360,258},{415,258},{451,279},{511,281},{562,270},{640,263},{640,360},{0,360}}, colors.ground)
paint.poly({{0,328},{110,302},{211,315},{288,298},{344,301},{411,289},{443,301},{513,298},{576,287},{640,298},{640,360},{0,360}}, colors.groundMid)
paint.poly({{467,258},{535,258},{587,360},{402,360}}, colors.stoneDark)
for step = 0, 10 do
    local top = 265 + step * 4 + step * step * 0.42
    local width = 68 + step * 8
    local centerX = 501 - step * 0.8
    local depth = 3 + step * 0.48
    paint.poly({{centerX-width/2,top},{centerX+width/2,top},{centerX+width/2+3,top+depth},{centerX-width/2-3,top+depth}}, colors.stone)
    paint.line(centerX-width/2,top,centerX+width/2,top,colors.stoneLight)
    paint.rect(centerX-width/2-3,top+depth,width+6,2,colors.stoneDark)
    local joint = centerX + ((step % 3) - 1) * width / 5
    paint.line(joint,top+1,joint+1,top+depth,colors.stoneDark)
    paint.rect(centerX-width/2+3,top+1,math.max(2,11-step/2),1,colors.moss)
end
for index = 1, 135 do
    local posX, posY = random(0,639), random(284,359)
    if posX < 409 or posX > 588 then
        paint.rect(posX,posY,random(2,6),1,index % 4 == 0 and colors.moss or colors.leafMid)
    end
end

local function cherry(centerX, centerY, scale)
    paint.poly({{centerX-6*scale,centerY+90*scale},{centerX+5*scale,centerY+90*scale},{centerX+3*scale,centerY+30*scale},{centerX+16*scale,centerY+7*scale},{centerX+11*scale,centerY+4*scale},{centerX-1*scale,centerY+27*scale},{centerX-17*scale,centerY+9*scale},{centerX-21*scale,centerY+12*scale},{centerX-6*scale,centerY+38*scale}},colors.barkShadow)
    paint.line(centerX-2*scale,centerY+82*scale,centerX-1*scale,centerY+29*scale,colors.bark,3*scale)
    paint.line(centerX-3*scale,centerY+54*scale,centerX-27*scale,centerY+27*scale,colors.barkLight,2*scale)
    paint.line(centerX+1*scale,centerY+40*scale,centerX+29*scale,centerY+12*scale,colors.bark,3*scale)
    for _, patch in ipairs({{-31,7,26,20},{-10,-13,28,25},{20,-10,30,23},{39,13,23,21},{12,16,35,23},{-14,22,29,22}}) do
        local posX, posY = centerX+patch[1]*scale,centerY+patch[2]*scale
        paint.cluster(posX,posY,patch[3]*scale,patch[4]*scale,colors.cherryDark)
        paint.cluster(posX-1*scale,posY-6*scale,patch[3]*scale*0.88,patch[4]*scale*0.69,colors.cherry)
        paint.cluster(posX-6*scale,posY-10*scale,patch[3]*scale*0.55,patch[4]*scale*0.4,colors.cherryMid)
        for detail = 1, 7 do
            local leafX = posX + random(-patch[3]*0.7,patch[3]*0.5)*scale
            local leafY = posY + random(-patch[4]*0.6,patch[4]*0.25)*scale
            paint.rect(leafX,leafY,3*scale,1*scale,colors.cherryLight)
            if detail % 2 == 0 then paint.pixel(leafX+1,leafY-1,colors.petal) end
        end
    end
end

paint.layer("06 - Cherry silhouettes and petals")
cherry(365,207,0.95)
cherry(619,192,1.15)
for _, petal in ipairs({{328,253},{346,271},{374,250},{390,237},{597,249},{609,281},{574,260},{588,300},{346,306},{416,319},{592,326},{608,342},{407,287}}) do
    paint.rect(petal[1],petal[2],2,1,colors.cherryLight)
    paint.pixel(petal[1]+1,petal[2]-1,colors.cherryMid)
end

paint.layer("07 - Vermilion torii")
paint.poly({{432,184},{445,184},{438,291},{421,291}},colors.redDark)
paint.poly({{434,185},{442,185},{435,288},{426,288}},colors.red)
paint.line(434,190,427,284,colors.redLight,2)
paint.poly({{560,184},{573,184},{586,291},{568,291}},colors.redDark)
paint.poly({{562,185},{569,185},{579,288},{570,288}},colors.red)
paint.line(563,190,571,284,colors.redLight,2)
paint.rect(419,287,24,8,colors.roof)
paint.rect(420,287,21,2,colors.roofLight)
paint.rect(566,287,24,8,colors.roof)
paint.rect(567,287,21,2,colors.roofLight)
paint.rect(422,202,163,10,colors.redDark)
paint.rect(422,202,163,5,colors.red)
paint.rect(423,201,161,2,colors.redLight)
paint.poly({{409,161},{432,168},{572,168},{599,159},{596,172},{578,180},{429,180},{412,173}},colors.roof)
paint.poly({{412,162},{434,169},{572,169},{597,161},{594,164},{573,172},{431,172},{413,166}},colors.roofLight)
paint.poly({{420,177},{585,177},{581,187},{425,187}},colors.redDark)
paint.rect(424,177,158,5,colors.redLight)
paint.rect(427,182,152,5,colors.red)
paint.line(430,177,576,177,colors.redRim)
paint.rect(493,181,18,27,colors.goldDark)
paint.rect(495,181,14,25,colors.gold)
paint.rect(497,183,10,21,colors.roof)
paint.line(500,188,504,188,colors.goldLight)
paint.line(502,185,502,201,colors.goldLight)
paint.line(499,194,505,194,colors.goldLight)
paint.line(499,198,505,198,colors.goldLight)
for _, nail in ipairs({{434,205},{573,205},{437,180},{568,180}}) do paint.rect(nail[1],nail[2],2,2,colors.goldDark) end

paint.layer("08 - Sacred rope and paper")
for segment = 0, 110 do
    local posX = 448 + segment
    local posY = 215 + 6 * math.sin(segment / 110 * math.pi)
    paint.rect(posX,posY,1,2,colors.goldDark)
    paint.pixel(posX,posY,segment % 3 == 0 and colors.paper or colors.goldLight)
end
for _, posX in ipairs({461,484,523,546}) do
    local top = 217 + (posX > 470 and posX < 540 and 4 or 1)
    paint.poly({{posX,top},{posX+5,top},{posX+3,top+5},{posX+7,top+6},{posX+3,top+12},{posX-1,top+10},{posX+2,top+6},{posX-2,top+5}},colors.paper)
    paint.line(posX+1,top+1,posX+1,top+4,colors.moonShade)
end

local function lantern(centerX, top)
    paint.ellipse(centerX,top+17,28,32,paint.color("e9ad62",13))
    paint.ellipse(centerX,top+17,20,25,paint.color("e9ad62",20))
    paint.rect(centerX-3,top+30,6,30,colors.stoneDark)
    paint.rect(centerX-2,top+30,2,29,colors.stone)
    paint.rect(centerX-14,top+58,28,4,colors.stoneDark)
    paint.rect(centerX-12,top+57,24,2,colors.stoneLight)
    paint.poly({{centerX-16,top+3},{centerX,top-7},{centerX+16,top+3}},colors.roof)
    paint.line(centerX-15,top+2,centerX,top-6,colors.roofLight)
    paint.rect(centerX-15,top+3,30,3,colors.roofLight)
    paint.rect(centerX-11,top+6,22,27,colors.barkShadow)
    paint.rect(centerX-8,top+7,16,22,colors.gold)
    paint.rect(centerX-6,top+8,12,19,colors.goldLight)
    paint.rect(centerX-4,top+9,8,17,colors.flame)
    paint.rect(centerX-1,top+8,2,21,colors.gold)
    paint.rect(centerX-8,top+17,16,1,colors.gold)
    paint.rect(centerX-13,top+31,26,4,colors.roof)
    paint.line(centerX-11,top+31,centerX+11,top+31,colors.goldDark)
    paint.ellipse(centerX,top+63,22,3,paint.color("dfb274",45))
end

paint.layer("09 - Warm shrine lanterns")
lantern(399,233)
lantern(597,245)

paint.layer("10 - Foreground shrubs and grass")
for _, shrub in ipairs({{311,332,32},{346,346,38},{617,348,33},{22,347,40},{301,359,39},{638,323,24}}) do
    paint.cluster(shrub[1],shrub[2],shrub[3],shrub[3]*0.58,colors.leafDark)
    paint.cluster(shrub[1]-3,shrub[2]-5,shrub[3]*0.8,shrub[3]*0.38,colors.leaf)
    for detail = 1, 13 do
        local posX = shrub[1]+random(-shrub[3]*0.65,shrub[3]*0.65)
        local posY = shrub[2]+random(-shrub[3]*0.3,shrub[3]*0.12)
        paint.line(posX,posY,posX+3,posY-2,colors.leafMid)
        if detail % 4 == 0 then paint.pixel(posX+3,posY-2,colors.leafLight) end
    end
end
for _, tuft in ipairs({{377,330},{409,303},{580,323},{591,305},{321,301},{439,354}}) do
    paint.line(tuft[1],tuft[2],tuft[1]-3,tuft[2]-5,colors.leafLight)
    paint.line(tuft[1],tuft[2],tuft[1]+1,tuft[2]-7,colors.leafMid)
    paint.line(tuft[1],tuft[2],tuft[1]+5,tuft[2]-4,colors.leafMid)
end
paint.finish()
if options.finish then options.finish(sprite);return end
sprite:saveAs(outputSource)
sprite:saveCopyAs(outputTexture)
print("ASEPRITE_TITLE_SAVED " .. outputSource)
print("ASEPRITE_TEXTURE_SAVED " .. outputTexture)
print("ASEPRITE_TITLE_LAYERS " .. #sprite.layers)

sprite:close()
end
