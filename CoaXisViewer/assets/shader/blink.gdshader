shader_type spatial;
render_mode cull_disabled;

uniform bool highlight = true;
uniform vec4 highlight_color = vec4(0.799, 0.44, 0.922, 1.0);
uniform float pulse_speed = 4.0;
uniform float pulse_strength = 1.0;
uniform float emission_energy = 2.86;

void fragment() {
    vec3 base = ALBEDO;

    if (highlight) {
        float pulse = abs(sin(TIME * pulse_speed));
        ALBEDO = highlight_color.rgb;
        EMISSION = highlight_color.rgb * pulse * pulse_strength * emission_energy;
    } else {
        ALBEDO = base;
        EMISSION = vec3(0.0);
    }
}
