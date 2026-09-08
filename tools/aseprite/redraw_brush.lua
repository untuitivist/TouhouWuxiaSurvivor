local root = app.params["root"] or app.fs.currentPath
local painter = dofile(root .. "/tools/aseprite/pixel_tools.lua")
local brush = { root = root, entries = {}, offsetX = 0, offsetY = 0, palette = {} }
for name, hex in pairs({ink="172631",deep="223643",blue="344e60",slate="597786",mist="91aaa6",paper="efe3c3",white="fff5da",gold="d7ab65",goldShade="9d7046",red="bd5357",redShade="783e4f",rose="e59b8e",jade="618e80",jadeShade="365d59",mint="aed0ab",violet="8072aa",lavender="b7a5d0",skin="efbf9c",skinShade="be8676",hair="473746",hairLight="79535b",yellow="f2d48a"}) do brush.palette[name] = painter.color(hex) end
function brush.offset(horizontal, vertical) brush.offsetX=horizontal or 0; brush.offsetY=vertical or 0 end
function brush.pixel(horizontal, vertical, color) painter.pixel(horizontal+brush.offsetX,vertical+brush.offsetY,color) end
function brush.rect(left,top,width,height,color) painter.rect(left+brush.offsetX,top+brush.offsetY,width,height,color) end
function brush.line(left,top,right,bottom,color,thickness) painter.line(left+brush.offsetX,top+brush.offsetY,right+brush.offsetX,bottom+brush.offsetY,color,thickness) end
function brush.ellipse(horizontal,vertical,width,height,color) painter.ellipse(horizontal+brush.offsetX,vertical+brush.offsetY,width,height,color) end
function brush.poly(points,color)
    local translated={}
    for _,point in ipairs(points) do translated[#translated+1]={point[1]+brush.offsetX,point[2]+brush.offsetY} end
    painter.poly(translated,color)
end
function brush.cluster(horizontal,vertical,width,height,color) painter.cluster(horizontal+brush.offsetX,vertical+brush.offsetY,width,height,color) end
function brush.layer(name) painter.layer(name) end
function brush.color(hex,alpha) return painter.color(hex,alpha) end
function brush.ring(horizontal,vertical,radius,aspect,color,thickness)
    local previousX,previousY=horizontal+radius,vertical
    for step=1,96 do
        local angle=step*math.pi*2/96
        local nextX,nextY=horizontal+math.cos(angle)*radius,vertical+math.sin(angle)*radius*aspect
        brush.line(previousX,previousY,nextX,nextY,color,thickness)
        previousX,previousY=nextX,nextY
    end
end
function brush.star(horizontal,vertical,radius,color,phase)
    local points={}
    for index=0,9 do
        local angle=-math.pi/2+index*math.pi/5+(phase or 0)
        local length=index%2==0 and radius or radius*0.44
        points[#points+1]={horizontal+math.cos(angle)*length,vertical+math.sin(angle)*length}
    end
    brush.poly(points,color)
end
function brush.save(name,width,height,draw)
    local sprite=Sprite(width,height,ColorMode.RGB)
    brush.offset()
    painter.begin(sprite)
    draw()
    painter.finish()
    local source=root.."/art/redraw/"..name..".aseprite"
    local texture=root.."/assets/aseprite/redraw/"..name..".png"
    app.fs.makeAllDirectories(app.fs.filePath(source))
    app.fs.makeAllDirectories(app.fs.filePath(texture))
    sprite:saveAs(source)
    sprite:saveCopyAs(texture)
    sprite:close()
    brush.entries[#brush.entries+1]={name=name,width=width,height=height}
    print("ASEPRITE_REDRAW "..name.." "..width.."x"..height)
end
return brush
