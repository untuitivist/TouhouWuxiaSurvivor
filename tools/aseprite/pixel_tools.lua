local painter = {}
local surface
local artwork
local initialized = false
local canvasWidth = 640
local canvasHeight = 360

function painter.color(hex, alpha)
    return app.pixelColor.rgba(tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16), alpha or 255)
end

function painter.begin(sprite)
    artwork = sprite
    canvasWidth = sprite.width
    canvasHeight = sprite.height
end

function painter.layer(name)
    if surface then painter.finish() end
    local layer = artwork.layers[1]
    if initialized then layer = artwork:newLayer() end
    initialized = true
    layer.name = name
    surface = Image(canvasWidth, canvasHeight, ColorMode.RGB)
    surface:clear()
    painter.activeLayer = layer
end

function painter.finish()
    if not surface then return end
    artwork:newCel(painter.activeLayer, 1, surface, Point(0, 0))
    surface = nil
end

function painter.pixel(posX, posY, color)
    posX, posY = math.floor(posX), math.floor(posY)
    if posX >= 0 and posX < canvasWidth and posY >= 0 and posY < canvasHeight then surface:drawPixel(posX, posY, color) end
end

function painter.rect(left, top, width, height, color)
    for row = math.max(0, math.floor(top)), math.min(canvasHeight - 1, math.floor(top + height - 1)) do
        for column = math.max(0, math.floor(left)), math.min(canvasWidth - 1, math.floor(left + width - 1)) do
            surface:drawPixel(column, row, color)
        end
    end
end

function painter.ellipse(centerX, centerY, radiusX, radiusY, color)
    for row = -math.ceil(radiusY), math.ceil(radiusY) do
        local fraction = 1 - (row * row) / (radiusY * radiusY)
        if fraction >= 0 then
            local span = math.floor(radiusX * math.sqrt(fraction))
            painter.rect(centerX - span, centerY + row, span * 2 + 1, 1, color)
        end
    end
end

function painter.line(startX, startY, endX, endY, color, thickness)
    local distance = math.max(math.abs(endX - startX), math.abs(endY - startY), 1)
    for step = 0, distance do
        painter.rect(math.floor(startX + (endX - startX) * step / distance), math.floor(startY + (endY - startY) * step / distance), thickness or 1, thickness or 1, color)
    end
end

function painter.poly(points, color)
    local minimum, maximum = canvasHeight, 0
    for _, point in ipairs(points) do minimum = math.min(minimum, point[2]); maximum = math.max(maximum, point[2]) end
    for row = math.floor(minimum), math.ceil(maximum) do
        local crossings = {}
        local previous = points[#points]
        for _, current in ipairs(points) do
            if (current[2] <= row and previous[2] > row) or (previous[2] <= row and current[2] > row) then
                crossings[#crossings + 1] = current[1] + (row - current[2]) * (previous[1] - current[1]) / (previous[2] - current[2])
            end
            previous = current
        end
        table.sort(crossings)
        for index = 1, #crossings - 1, 2 do
            local left = math.ceil(crossings[index])
            painter.rect(left, row, math.floor(crossings[index + 1]) - left + 1, 1, color)
        end
    end
end

function painter.cluster(centerX, centerY, radiusX, radiusY, color)
    local points = {
        {centerX - radiusX, centerY - radiusY * 0.25}, {centerX - radiusX * 0.72, centerY - radiusY * 0.25},
        {centerX - radiusX * 0.72, centerY - radiusY * 0.73}, {centerX - radiusX * 0.3, centerY - radiusY * 0.73},
        {centerX - radiusX * 0.3, centerY - radiusY}, {centerX + radiusX * 0.34, centerY - radiusY},
        {centerX + radiusX * 0.34, centerY - radiusY * 0.7}, {centerX + radiusX * 0.78, centerY - radiusY * 0.7},
        {centerX + radiusX * 0.78, centerY - radiusY * 0.28}, {centerX + radiusX, centerY - radiusY * 0.28},
        {centerX + radiusX, centerY + radiusY * 0.35}, {centerX + radiusX * 0.7, centerY + radiusY * 0.35},
        {centerX + radiusX * 0.7, centerY + radiusY * 0.74}, {centerX + radiusX * 0.3, centerY + radiusY * 0.74},
        {centerX + radiusX * 0.3, centerY + radiusY}, {centerX - radiusX * 0.4, centerY + radiusY},
        {centerX - radiusX * 0.4, centerY + radiusY * 0.7}, {centerX - radiusX * 0.8, centerY + radiusY * 0.7},
        {centerX - radiusX * 0.8, centerY + radiusY * 0.3}, {centerX - radiusX, centerY + radiusY * 0.3}
    }
    painter.poly(points, color)
end

return painter
