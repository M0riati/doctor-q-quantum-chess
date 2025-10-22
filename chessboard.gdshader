shader_type canvas_item;

uniform int num_cells = 8;
uniform vec4 light_square: source_color;
uniform vec4 dark_square: source_color;


void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	int sum = (int(UV.x*float(num_cells)) + int(UV.y*float(num_cells)));
	COLOR = sum % 2 == 0? light_square: dark_square;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
