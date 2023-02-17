#version 330 core
out vec4 FragColor;

in vec2 texCoord;

uniform sampler2D texture0;

void main()
{
	vec4 color = texture(texture0, texCoord);

	if(color.a < 1.0f) discard;

	FragColor = color;
}