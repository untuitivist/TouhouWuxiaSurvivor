local root=app.params["root"] or app.fs.currentPath
local painter=dofile(root.."/tools/aseprite/pixel_tools.lua")
local brush={root=root,painter=painter,colors={}}
for name,hex in pairs({ink="3c2b3b",edge="754655",hairDark="422b36",hair="5b333c",hairMid="784249",hairLight="a26456",hairGlow="c18b6b",skin="f5d2bd",skinLight="ffe5ce",skinMid="e9b5a5",skinShade="c88d91",blush="e8a6a0",redDeep="673044",redShade="92384d",red="c23e55",redMid="dc5763",redLight="f07b77",redGlow="f2a09a",creamDeep="a18e9f",creamShade="c4b6bc",creamMid="e4d6ce",cream="f5e7d7",white="fff5de",goldShade="9f6953",gold="d3a264",goldLight="f4d095",blue="97bdc9",blueShade="5d8294",blueLight="d0e3e3",joint="efbd80",ground="334349"}) do brush.colors[name]=painter.color(hex) end
function brush.begin(sprite) painter.begin(sprite) end
function brush.layer(name) painter.layer(name) end
function brush.finish() painter.finish() end
function brush.pixel(horizontal,vertical,color) painter.pixel(horizontal,vertical,brush.colors[color] or color) end
function brush.rect(left,top,width,height,color) painter.rect(left,top,width,height,brush.colors[color] or color) end
function brush.line(left,top,right,bottom,color,width) painter.line(left,top,right,bottom,brush.colors[color] or color,width) end
function brush.ellipse(horizontal,vertical,width,height,color) painter.ellipse(horizontal,vertical,width,height,brush.colors[color] or color) end
function brush.path(commands,fill,stroke,width)
    local points={}
    local current
    for _,command in ipairs(commands) do
        if #command==2 then current={command[1],command[2]};points[#points+1]=current
        else
            local origin=current
            local steps=math.max(8,math.ceil((math.abs(command[5]-origin[1])+math.abs(command[6]-origin[2]))/2))
            for index=1,steps do
                local progress=index/steps
                local inverse=1-progress
                points[#points+1]={inverse^3*origin[1]+3*inverse^2*progress*command[1]+3*inverse*progress^2*command[3]+progress^3*command[5],inverse^3*origin[2]+3*inverse^2*progress*command[2]+3*inverse*progress^2*command[4]+progress^3*command[6]}
            end
            current={command[5],command[6]}
        end
    end
    if fill then painter.poly(points,brush.colors[fill] or fill) end
    if stroke then
        for index=2,#points do painter.line(points[index-1][1],points[index-1][2],points[index][1],points[index][2],brush.colors[stroke] or stroke,width) end
        if fill then painter.line(points[#points][1],points[#points][2],points[1][1],points[1][2],brush.colors[stroke] or stroke,width) end
    end
end
function brush.mark(centerX,centerY,radius,color)
    local points={}
    for index=0,9 do
        local angle=index*math.pi/5-math.pi/2
        local length=index%2==0 and radius or radius*0.4
        points[#points+1]={centerX+math.cos(angle)*length,centerY+math.sin(angle)*length}
    end
    brush.path(points,color)
end
return brush
