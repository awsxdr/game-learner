#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform int frame;

void main()
{
	texCoord = vec2(aTexCoord.x / 5.0 + frame * 1.0 / 5.0, aTexCoord.y);
	gl_Position = vec4(vPos, 1.0) * model * view * projection;
}