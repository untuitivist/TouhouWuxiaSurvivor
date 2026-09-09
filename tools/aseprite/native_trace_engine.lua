local engine={}
local pixel=app.pixelColor
local colors={}
local brush=Brush(1)
local function nativeColor(value)
    if not colors[value] then colors[value]=Color{r=pixel.rgbaR(value),g=pixel.rgbaG(value),b=pixel.rgbaB(value),a=pixel.rgbaA(value)} end
    return colors[value]
end
function engine.isBackdrop(value)
    return pixel.rgbaB(value)>pixel.rgbaR(value)+8 and pixel.rgbaG(value)>pixel.rgbaR(value)+3
end
function engine.inside(horizontal,vertical,polygon)
    local contained=false
    local previous=#polygon
    for index=1,#polygon do
        local point,prior=polygon[index],polygon[previous]
        if (point[2]>vertical)~=(prior[2]>vertical) and horizontal<(prior[1]-point[1])*(vertical-point[2])/(prior[2]-point[2])+point[1] then contained=not contained end
        previous=index
    end
    return contained
end
function engine.paint(sprite,reference,rectangle,layerFor,transparent,report)
    local layers={}
    local painted=0
    local strokes=0
    local started=os.clock()
    local previousLayer
    app.transaction("Reference-faithful native pencil retracing",function()
        for vertical=0,rectangle.height-1 do
            local horizontal=0
            while horizontal<rectangle.width do
                local referenceX,referenceY=rectangle.x+horizontal,rectangle.y+vertical
                local value=reference:getPixel(referenceX,referenceY)
                local name=layerFor(referenceX,referenceY,value)
                if name and not (transparent and engine.isBackdrop(value)) then
                    local last=horizontal
                    while last+1<rectangle.width do
                        local nextValue=reference:getPixel(rectangle.x+last+1,referenceY)
                        if nextValue~=value or layerFor(rectangle.x+last+1,referenceY,nextValue)~=name then break end
                        last=last+1
                    end
                    if not layers[name] then
                        local layer=previousLayer and sprite:newLayer() or sprite.layers[1]
                        layer.name=name
                        layers[name]=layer
                        previousLayer=layer
                    end
                    app.useTool{tool="pencil",brush=brush,color=nativeColor(value),points={Point(horizontal,vertical),Point(last,vertical)},layer=layers[name],frame=sprite.frames[1],opacity=255,freehandAlgorithm=0}
                    strokes=strokes+1
                    painted=painted+last-horizontal+1
                    horizontal=last+1
                else horizontal=horizontal+1 end
            end
            if vertical%96==95 and report then report("row="..vertical.." strokes="..strokes.." elapsed="..string.format("%.2f",os.clock()-started)) end
        end
    end)
    return {strokes=strokes,painted_pixels=painted,seconds=os.clock()-started,layers=layers}
end
function engine.verify(sprite,reference,rectangle,layerFor,transparent)
    local flattened=Image(sprite.spec)
    flattened:drawSprite(sprite,1,Point(0,0))
    local mismatches,expectedPixels,unexpectedPixels=0,0,0
    for vertical=0,rectangle.height-1 do
        for horizontal=0,rectangle.width-1 do
            local value=reference:getPixel(rectangle.x+horizontal,rectangle.y+vertical)
            local wanted=layerFor(rectangle.x+horizontal,rectangle.y+vertical,value) and not (transparent and engine.isBackdrop(value))
            local actual=flattened:getPixel(horizontal,vertical)
            if wanted then
                expectedPixels=expectedPixels+1
                if actual~=value then mismatches=mismatches+1 end
            elseif pixel.rgbaA(actual)~=0 then unexpectedPixels=unexpectedPixels+1 end
        end
    end
    return {matching_pixels=expectedPixels-mismatches,expected_pixels=expectedPixels,color_mismatches=mismatches,unexpected_pixels=unexpectedPixels}
end
return engine
