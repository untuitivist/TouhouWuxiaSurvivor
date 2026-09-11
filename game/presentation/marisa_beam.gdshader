shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float flow_time = 0.0;
uniform float beam_length = 760.0;
uniform float cap_length = 100.0;

void fragment() {
    float along = UV.x < 0.5 ? UV.x * 2.0 * cap_length : cap_length + (UV.x * 2.0 - 1.0) * (beam_length - cap_length);
    float phase = along / 280.0 - flow_time * 0.65 + UV.y * 0.2;
    vec3 channels = abs(fract(vec3(phase) + vec3(0.0, 0.6666667, 0.3333333)) * 6.0 - 3.0);
    vec3 rainbow = mix(vec3(0.16), vec3(1.0), clamp(channels - 1.0, 0.0, 1.0));
    float center = 1.0 - abs(UV.y * 2.0 - 1.0);
    float core = center * center * center;
    COLOR.rgb *= mix(rainbow, vec3(1.0), core * core * 0.72);
}
