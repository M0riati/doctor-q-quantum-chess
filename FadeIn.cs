using Godot;
using System;

public partial class FadeIn : Node2D {
	public bool fadeIn = false;

	public void StartFade() {
		if (!fadeIn) {
			this.fadeIn = true;
			var player1 = new AudioStreamPlayer2D();
			var player2 = new AudioStreamPlayer2D();
			player1.Stream = AudioStreamOggVorbis.LoadFromFile("res://checkmate.ogg");
			player2.Stream = AudioStreamOggVorbis.LoadFromFile("res://lost-Piano.ogg");
			this.AddChild(player1);
			this.AddChild(player2);
			player1.Play();
			player2.Play();
		}
	}

	public override void _Ready() {
		base._Ready();
	}

	public override void _Process(double delta) {
		base._Process(delta);
		if (fadeIn) {
			fadeIn = this.Modulate.A < 1.0f;
			this.Modulate = new Color(1.0f, 1.0f, 1.0f, this.Modulate.A + (float) delta);
		}
	}
}
