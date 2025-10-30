using System;
using Godot;

namespace doctorqquantumchess;

public partial class PromotionMenu() : Control {
	public Type[] types;
	public bool isWhite;
	
	public event Action<Type> OnSubmit = delegate { };

	public static Action CancelPromotion = delegate { };

	public void Init(Type[] types, bool isWhite) {
		CancelPromotion += OnCancel;
		var panel = new PanelContainer();
		var grid = new GridContainer();
		grid.Columns = types.Length;
		panel.AddChild(grid);
		this.AddChild(panel);
		foreach (var type in types) {
			TextureButton button = new();
			button.TextureNormal = type.GetField(isWhite? "whiteTexture": "blackTexture").GetValue(null) as Texture2D;
			button.Pressed += () => { this.OnSubmit(type); this.QueueFree(); };
			grid.AddChild(button);
		}
	}
	
	private void OnCancel() => QueueFree();

	public override void _ExitTree() {
		CancelPromotion -= OnCancel;
	}
}