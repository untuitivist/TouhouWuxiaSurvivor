local root = app.params["root"] or "."
local output = app.params["output"] or root .. "/art/portraits/ai-preview"
local textures = app.params["textures"] or root .. "/assets/ui/portraits/ai-preview"
local pixel = app.pixelColor
local specifications = {
    reimu = {
        {208,32},{232,31},{246,48},{277,50},{310,91},{337,76},{402,65},{400,111},{391,162},{414,171},{449,170},{451,191},{428,231},{405,235},{395,228},{424,255},{467,276},{502,301},{504,326},{478,344},{474,365},{449,381},{430,377},{452,399},{477,427},{501,454},{500,482},{479,491},{460,487},{465,529},{453,551},{433,534},{411,559},{388,548},{385,527},{375,533},{384,554},{367,575},{335,577},{321,604},{292,605},{282,626},{282,653},{275,662},{273,680},{277,689},{278,703},{284,718},{287,732},{288,740},{295,747},{295,768},{287,775},{284,779},{238,780},{234,775},{234,746},{239,725},{237,699},{236,684},{231,683},{233,710},{226,719},{215,729},{201,729},{197,724},{191,718},{191,696},{196,685},{197,677},{202,666},{205,660},{195,654},{189,651},{170,655},{152,642},{146,661},{129,654},{114,616},{121,576},{104,582},{91,559},{93,533},{79,518},{73,496},{83,478},{106,445},{132,415},{104,434},{85,425},{71,446},{54,442},{42,413},{43,378},{18,397},{0,402},{0,376},{15,351},{38,331},{53,316},{44,300},{46,276},{57,251},{88,229},{94,222},{92,204},{87,191},{94,174},{103,168},{109,145},{109,126},{115,117},{113,99},{134,97},{143,106},{161,103},{162,125},{151,180},{139,187},{141,207},{137,220},{145,236},{166,242},{173,238},{163,225},{163,201},{173,176},{184,155},{193,139},{186,130},{186,108},{195,80},{200,58}
    },
    marisa = {
        {241,2},{262,0},{288,18},{310,35},{337,51},{339,82},{386,75},{404,76},{402,98},{389,139},{405,145},{427,151},{440,174},{447,192},{478,206},{484,219},{470,241},{450,233},{431,216},{416,233},{383,239},{402,253},{422,252},{433,262},{449,251},{467,261},{479,282},{497,302},{499,327},{486,337},{495,353},{486,375},{470,391},{445,387},{459,402},{483,432},{497,458},{503,482},{497,507},{483,518},{469,497},{449,486},{442,526},{451,539},{458,568},{443,571},{420,553},{410,584},{398,589},{376,567},{365,598},{350,619},{329,608},{326,585},{312,573},{300,601},{291,626},{284,641},{282,651},{276,659},{280,673},{280,682},{285,689},{286,708},{291,723},{292,739},{300,744},{303,753},{303,773},{293,779},{235,781},{233,772},{233,751},{241,737},{242,727},{241,707},{239,696},{236,688},{232,685},{233,716},{222,723},{216,729},{197,729},{191,722},{186,718},{184,709},{183,691},{187,687},{191,683},{191,669},{194,661},{179,653},{170,648},{153,648},{143,669},{128,659},{118,633},{107,622},{100,594},{102,568},{105,550},{83,543},{75,565},{62,584},{42,584},{23,575},{5,560},{0,550},{0,491},{19,472},{49,457},{65,445},{78,420},{62,431},{45,435},{26,459},{19,441},{20,416},{29,391},{39,375},{53,357},{42,341},{35,335},{36,312},{47,300},{62,285},{72,278},{67,257},{79,247},{76,228},{83,213},{93,201},{105,192},{97,182},{78,169},{68,154},{70,143},{96,130},{110,122},{114,102},{130,93},{138,82},{143,57},{163,47},{180,48},{195,33},{212,19}
    }
}
local function inside(horizontal, vertical, polygon)
    local contained = false
    local previous = polygon[#polygon]
    for _, current in ipairs(polygon) do
        if (current[2] > vertical) ~= (previous[2] > vertical) and horizontal < (previous[1] - current[1]) * (vertical - current[2]) / (previous[2] - current[2]) + current[1] then
            contained = not contained
        end
        previous = current
    end
    return contained
end
local function removeLoosePixels(image)
    local visited = {}
    for iterator in image:pixels() do
        local origin = iterator.y * image.width + iterator.x
        if pixel.rgbaA(iterator()) > 0 and not visited[origin] then
            local component = {origin}
            local cursor = 1
            visited[origin] = true
            while cursor <= #component do
                local position = component[cursor]
                local horizontal, vertical = position % image.width, math.floor(position / image.width)
                for _, offset in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
                    local neighborX, neighborY = horizontal + offset[1], vertical + offset[2]
                    local neighbor = neighborY * image.width + neighborX
                    if neighborX >= 0 and neighborX < image.width and neighborY >= 0 and neighborY < image.height and not visited[neighbor] and pixel.rgbaA(image:getPixel(neighborX, neighborY)) > 0 then
                        visited[neighbor] = true
                        component[#component + 1] = neighbor
                    end
                end
                cursor = cursor + 1
            end
            if #component < 512 then
                for _, position in ipairs(component) do image:drawPixel(position % image.width, math.floor(position / image.width), 0) end
            end
        end
    end
end
app.fs.makeAllDirectories(output)
app.fs.makeAllDirectories(textures)
for _, character in ipairs({"reimu", "marisa"}) do
    if app.fs.isFile(output .. "/" .. character .. ".aseprite") or app.fs.isFile(textures .. "/" .. character .. ".png") then error("Refusing to overwrite edited portraits; choose new output and textures directories") end
    local board = Image{fromFile=root .. "/ai-assets/" .. character .. "_pixel_concept.png"}
    local original = Image(board, Rectangle(16, 140, 520, 800))
    local extracted = Image(520, 800, ColorMode.RGB)
    for iterator in original:pixels() do
        local value = iterator()
        local red, green, blue = pixel.rgbaR(value), pixel.rgbaG(value), pixel.rgbaB(value)
        if inside(iterator.x, iterator.y, specifications[character]) and not (green > red + 3 and blue > red + 8) then
            extracted:drawPixel(iterator.x, iterator.y, value)
        end
    end
    removeLoosePixels(extracted)
    local sprite = Sprite(520, 800, ColorMode.RGB)
    sprite:assignColorSpace(ColorSpace{sRGB=true})
    local reference = sprite.layers[1]
    reference.name = "00 Original AI portrait crop - reference only"
    sprite:newCel(reference, 1, original, Point(0,0))
    reference.isVisible = false
    reference.isEditable = false
    local portrait = sprite:newLayer()
    portrait.name = "01 AI portrait extract - editable, not a redraw"
    sprite:newCel(portrait, 1, extracted, Point(0,0))
    sprite:saveAs(output .. "/" .. character .. ".aseprite")
    sprite:saveCopyAs(textures .. "/" .. character .. ".png")
    sprite:close()
    print("AI_PORTRAIT_EXTRACT " .. character .. " 520x800")
end
