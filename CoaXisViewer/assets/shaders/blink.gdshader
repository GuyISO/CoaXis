shader_type spatial;

uniform bool highlight = true;
uniform vec4 highlight_color = vec4(1.0, 1.0, 0.0, 1.0);
uniform float pulse_speed = 4.0;
uniform float pulse_strength = 1.0;

void fragment() {
    vec3 base = ALBEDO;

    if (highlight) {
        float pulse = abs(sin(TIME * pulse_speed));
        vec3 glow = mix(base, highlight_color.rgb, pulse * pulse_strength);
        ALBEDO = glow;
    } else {
        ALBEDO = base;
    }
}
