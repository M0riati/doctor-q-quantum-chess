using Godot;
using System;

public partial class PieceSprite : Sprite2D {
	private Vector2 target;
	private bool enRoute = false;
	private bool isWhite;
	private float certainty = 1.0f;
	
	[Export] public Color colorWhite = new Color(0.816f, 0.91f, 0.847f);
	[Export] public Color colorBlack = new Color(0.606f, 0.945f, 0.891f);
	
	public PieceSprite(bool isWhite) {
		this.isWhite = isWhite;
		this.certainty = 1.0f;
	}
	
	public override void _Ready() {
		base._Ready();
	}

	public void SetTarget(Vector2 target) {
		this.target = target;
		enRoute = true;
	}
	
	public override void _Process(double delta) {
		
		if (isWhite) {
			this.Modulate = colorWhite;
		}
		else {
			this.Modulate = colorBlack;
		}
		this.Modulate = new Color(this.Modulate.R, this.Modulate.G, this.Modulate.B, (float) Random.Shared.NextDouble()*(1.0f-certainty) + certainty);
		if (enRoute) {
			this.Position = this.Position.Lerp(target, (float) delta*10.0f);
			if (this.Position.DistanceTo(target) < 0.001f) {
				this.Position = target;
				enRoute = false;
			}
		}
	}
}
