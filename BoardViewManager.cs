using Godot;
using System;
using System.Collections.Generic;

public partial class BoardViewManager : Node2D {

	public GameState gameState;
	
	public int boardSize = 8;
	public Sprite2D chessBoardSprite;
	public FadeIn gameOverScreen;
	public AnnotationManager annotationManager;
	public Dictionary<Vector2I, PieceSprite> positionToSprite = new();
	public Vector2I selectedSquare = new Vector2I(-1, -1);
	public List<Vector2I> legalSquares;
	
	public void BaseSetup() {
		gameState.SetAt(0, 0, new Rook(true));
		gameState.SetAt(0, 1, new Knight(true));
		gameState.SetAt(0, 2, new Bishop(true));
		gameState.SetAt(0, 3, new Queen(true));
		gameState.SetAt(0, 4, new King(true));
		gameState.SetAt(0, 5, new Bishop(true));
		gameState.SetAt(0, 6, new Knight(true));
		gameState.SetAt(0, 7, new Rook(true));

		for (int f = 0; f < boardSize; f++) {
			gameState.SetAt(1, f, new Pawn(true));
			gameState.SetAt(6, f, new Pawn(false));
		}
		
		gameState.SetAt(7, 0, new Rook(false));
		gameState.SetAt(7, 1, new Knight(false));
		gameState.SetAt(7, 2, new Bishop(false));
		gameState.SetAt(7, 3, new Queen(false));
		gameState.SetAt(7, 4, new King(false));
		gameState.SetAt(7, 5, new Bishop(false));
		gameState.SetAt(7, 6, new Knight(false));
		gameState.SetAt(7, 7, new Rook(false));
	}

	private void ResetSelection() {
		this.selectedSquare = new Vector2I(-1, -1);
		this.legalSquares = new();
		NotifyAnnotationManager();
	}
	
	private void NotifyAnnotationManager() {
		var circles = new CircleAnnotation[legalSquares.Count];
		var i = 0;
		foreach (var move in legalSquares ) {
			circles[i++] = new CircleAnnotation() {
				color = Color.Color8(125, 3, 9, 200),
				filled = true,
				position = (new Vector2( move.Y+0.5f, move.X+0.5f)/boardSize)-Vector2.One*0.5f,
				radius = 0.03f
			};
		}

		annotationManager.circles = circles;
		annotationManager.QueueRedraw();
	}
	
	private void Select(int x, int y) {
		var piece = gameState.GetAt(x, y);
		
		var square = new Vector2I(x, y);

		foreach (var specialMove in gameState.specialMoves) {
			if (specialMove.from == this.selectedSquare && specialMove.to == square) {
				gameState.RunSpecialMove(specialMove);
				foreach (var (from, to) in specialMove.difference) {
					if (to == -Vector2I.One) {
						positionToSprite[from].QueueFree();
						positionToSprite.Remove(from);
					}
					else {
						this.Move(from, to);
					}
				}

				this.ResetSelection();
				if (gameState.checkmate) {
					this.GameOver();
				}
				return;
			}
		}

		// only move your own pieces
		if (piece?.isWhite == gameState.IsWhiteTurn()) {
			this.selectedSquare = square;
			this.legalSquares = new List<Vector2I>(gameState.GetLegalMoves(this.selectedSquare));

			foreach (var specialMove in gameState.specialMoves) {
				if (specialMove.from == this.selectedSquare) {
					this.legalSquares.Add(specialMove.to);
				}
			}
			
			GD.Print($"Selected {this.gameState.GetAt(x, y).GetType().Name.ToLower()} {ToAlgebraic(y, x)}");
			NotifyAnnotationManager();
			return;
		}
		else if (legalSquares.Contains(square)) {
			gameState.RunMove(this.selectedSquare, square);
			if (gameState.checkmate) {
				this.GameOver();
			}
			this.Move(this.selectedSquare, square);
		}
		this.ResetSelection();
		GD.Print($"Clicked on {ToAlgebraic(y, x)}");
	}

	public void GameOver() {
		this.gameOverScreen.StartFade();
	}

	private void Move(Vector2I from, Vector2I to) {
		if (positionToSprite.ContainsKey(to)) {
			positionToSprite[to].QueueFree();
		} 
		var sprite = positionToSprite[from];
		sprite.SetTarget(this.GetViewportBoardPosition(to));
		positionToSprite.Remove(from);
		positionToSprite[to] = sprite;
	}
	
	private Vector2 GetViewportBoardPosition(Vector2I position) {
		return (new Vector2(position.Y, position.X) + Vector2.One * 0.5f) / boardSize - new Vector2(0.5f, 0.5f);
	}

	private Vector2 GetViewportBoardPosition(int x, int y) {
		return GetViewportBoardPosition(new Vector2I(x, y));
	}
	
	private static string ToAlgebraic(int x, int y) {
		return $"{Convert.ToChar(97 + x)}{(y + 1)}";
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent) {
			if (!mouseEvent.Pressed) return;
			var boardPosition = chessBoardSprite.GlobalTransform.AffineInverse().BasisXform(mouseEvent.Position-chessBoardSprite.Position);
			boardPosition += Vector2.One*0.5f;
			if (boardPosition.X < 1.0f && boardPosition.X > 0.0f && boardPosition.Y < 1.0f && boardPosition.Y > 0.0f) {
				Select((int)(boardPosition.Y * boardSize), (int)(boardPosition.X * boardSize));
			}
		}
	}

	public void InitSprites() {
		for (int x = 0; x < boardSize; x++) {
			for (int y = 0; y < boardSize; y++) {
				var piece = gameState.GetAt(x, y);
				if (piece != null) {
					var sprite = new PieceSprite(piece.isWhite);
					var texture = piece.GetTexture();
					sprite.Texture = texture;
					sprite.Scale = new Vector2(1.0f/boardSize, 1.0f/boardSize)* 1 / (texture.GetSize());
					sprite.Position = this.GetViewportBoardPosition(x, y);
					positionToSprite[new Vector2I(x, y)] = sprite;
					chessBoardSprite.AddChild(sprite);
				}
			}
		}
	}
	
	
	public override void _Ready() {
		base._Ready();
		this.legalSquares = new();
		chessBoardSprite = GetNode<Sprite2D>("/root/ChessBoard/ChessBoardSprite");
		annotationManager = GetNode<AnnotationManager>("/root/ChessBoard/AnnotationManager");
		gameOverScreen = GetNode<FadeIn>("/root/ChessBoard/CheckmateBlind");
		annotationManager.transform = chessBoardSprite.GlobalTransform;
		gameState = new GameState(boardSize);
		this.BaseSetup();
		InitSprites();
	}

}
