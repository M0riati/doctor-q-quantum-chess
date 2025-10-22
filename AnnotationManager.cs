using Godot;
using System;
using System.Collections.Generic;

public struct CircleAnnotation {
	public Vector2 position;
	public float radius;
	public Color color;
	public bool filled;
}

public partial class AnnotationManager : Node2D {
	public CircleAnnotation[] circles = new CircleAnnotation[0];
	public Transform2D transform;
	private float t = 0;

	
	public override void _Process(double delta) {
		this.t += (float) delta;
		this.QueueRedraw();
	}
	
	public override void _Draw() {
		DrawSetTransformMatrix(this.transform);
		this.ZIndex = 1000;
		foreach (CircleAnnotation circleAnnotation in circles) {
			this.DrawCircle(circleAnnotation.position, circleAnnotation.radius*(1.0f+0.05f*Mathf.Sin(10.0f*this.t)), circleAnnotation.color, circleAnnotation.filled);

		} 
	}


}
