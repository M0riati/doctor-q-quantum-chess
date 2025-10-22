using Godot;
using System;
using System.Collections.Generic;

public class MoveCandidate : IEquatable<MoveCandidate> {
	public bool onCapture;
	public bool onNonCapture;
	public bool obstructable;
	public int turn;
	public Vector2I square;
	public MoveCandidate obstructs;
	public MoveCandidate obstructedBy;
	
	public MoveCandidate(bool onCapture, bool onNonCapture, bool obstructable, Vector2I square) {
		this.onCapture = onCapture;
		this.onNonCapture = onNonCapture;
		this.obstructable = obstructable;
		this.square = square;
		obstructs = null;
	}

	public bool Equals(MoveCandidate other) {
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		// Equality is based on move characteristics and destination square.
		return onCapture == other.onCapture
		       && onNonCapture == other.onNonCapture
		       && obstructable == other.obstructable
		       && turn == other.turn
		       && square.Equals(other.square);
	}

	public override bool Equals(object obj) {
		return obj is MoveCandidate other && Equals(other);
	}

	public override int GetHashCode() {
		unchecked {
			int hash = 17;
			hash = hash * 31 + onCapture.GetHashCode();
			hash = hash * 31 + onNonCapture.GetHashCode();
			hash = hash * 31 + obstructable.GetHashCode();
			hash = hash * 31 + turn.GetHashCode();
			hash = hash * 31 + square.GetHashCode();
			return hash;
		}
	}

	public static bool operator ==(MoveCandidate left, MoveCandidate right) {
		return Equals(left, right);
	}

	public static bool operator !=(MoveCandidate left, MoveCandidate right) {
		return !Equals(left, right);
	}
}

class MoveRules {
	public static MoveCandidate Line(int xIncrement, int yIncrement, Vector2I square, int boardSize, int maxIterations = -1, bool onCapture = true,
		bool onNonCapture = true, bool obstructable = true) {
		List<MoveCandidate> candidates = new();
		Vector2I movePointer = new Vector2I(square.X, square.Y);
		int i = 0;
		while ((i < maxIterations) || maxIterations == -1) {
			movePointer += new Vector2I(xIncrement, yIncrement);
			if (movePointer.X < 0 || movePointer.X >= boardSize || movePointer.Y < 0 || movePointer.Y >= boardSize) {
				break;
			}
			MoveCandidate moveCandidate = new(onCapture, onNonCapture, obstructable, movePointer);
			if (obstructable && i > 0) {
				candidates[i - 1].obstructs = moveCandidate;
				moveCandidate.obstructedBy = candidates[i - 1];
			}
			candidates.Add(moveCandidate);

			i++;
		}

		var baseMove = new MoveCandidate(false, false, obstructable, square);
		if (candidates.Count > 0) {
			baseMove.obstructs = candidates[0];
			candidates[0].obstructedBy = baseMove;
		}
		return baseMove;
	}
}


public abstract class Piece {
	
	public bool hasMoved;
	public bool isWhite;

	protected Piece(bool isWhite) {
		this.isWhite = isWhite;
		this.hasMoved = false;
	}
	
	public virtual void OnMove(Vector2I from, Vector2I to) {
		this.hasMoved = true;
	}

	public virtual void OnTurn() {
		
	}
	
	public abstract CompressedTexture2D GetTexture();

	public abstract List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize);

	public T Clone<T>() {
		return (T) this.MemberwiseClone();
	}
}



public class Pawn: Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wP.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bP.png");
	public bool isEnPassantable = false;
	
	public Pawn(bool isWhite) : base(isWhite) {
	}

	
	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}

	public override void OnMove(Vector2I from, Vector2I to) {
		base.OnMove(from, to);
		this.isEnPassantable = Mathf.Abs(to.X - from.X) == 2;
	}

	public override void OnTurn() {
		base.OnTurn();
		this.isEnPassantable = false;
	}

	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();
		if (isWhite) {
			if (!hasMoved) {
				candidates.Add(MoveRules.Line(1, 0, position, boardSize, onCapture:false, obstructable:true, maxIterations:2));
			}
			else {
				candidates.Add(MoveRules.Line(1, 0, position, boardSize, onCapture:false, obstructable:false, maxIterations:1));
			}
			candidates.Add(MoveRules.Line(1, 1, position, boardSize, onNonCapture:false,  obstructable:false, maxIterations:1));
			candidates.Add(MoveRules.Line(1, -1, position, boardSize,onNonCapture:false,  obstructable:false, maxIterations:1));
		}
		else {
			if (!hasMoved) {
				candidates.Add(MoveRules.Line(-1, 0, position, boardSize, onCapture:false, obstructable:true, maxIterations:2));
			}
			else {
				candidates.Add(MoveRules.Line(-1, 0, position, boardSize, onCapture:false,  obstructable:false, maxIterations:1));
			}
			candidates.Add(MoveRules.Line(-1, 1, position, boardSize, onNonCapture:false, obstructable:false, maxIterations:1));
			candidates.Add(MoveRules.Line(-1, -1, position, boardSize,onNonCapture:false,  obstructable:false, maxIterations:1));
		}
		return candidates;
	}
}

public class Knight: Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wN.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bN.png");
	
	public Knight(bool isWhite) : base(isWhite) {
	}

	
	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}
	
	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();

		candidates.Add(MoveRules.Line(2, 1, position, boardSize, obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(2, -1, position, boardSize, obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(-2, 1, position, boardSize,obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(-2, -1, position, boardSize,obstructable:false, maxIterations:1));

		candidates.Add(MoveRules.Line(1, 2, position, boardSize,obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(1, -2, position, boardSize,obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(-1, 2, position, boardSize,obstructable:false, maxIterations:1));
		candidates.Add(MoveRules.Line(-1, -2, position, boardSize,obstructable:false, maxIterations:1));
		return candidates;
	}
}

public class Bishop: Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wB.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bB.png");
	
	public Bishop(bool isWhite) : base(isWhite) {
	}

	
	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}
	
	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();
		candidates.Add(MoveRules.Line(1, 1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, 1, position, boardSize));
		candidates.Add(MoveRules.Line(1, -1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, -1, position, boardSize));
		return candidates;
	}
}

public class Rook : Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wR.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bR.png");
	

	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}
	
	public Rook(bool isWhite) : base(isWhite) {
	}

	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();
		candidates.Add(MoveRules.Line(0, 1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, 0, position, boardSize));
		candidates.Add(MoveRules.Line(1, 0, position, boardSize));
		candidates.Add(MoveRules.Line(0, -1, position, boardSize));
		return candidates;
	}
}

public class King : Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wK.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bK.png");

	
	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}

	public King(bool isWhite) : base(isWhite) {
	}

	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();
		candidates.Add(MoveRules.Line(0, 1, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(0, -1, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(1, 0, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(-1, 0, position, boardSize, obstructable: false, maxIterations: 1));

		candidates.Add(MoveRules.Line(1, 1, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(1, -1, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(-1, 1, position, boardSize, obstructable: false, maxIterations: 1));
		candidates.Add(MoveRules.Line(-1, -1, position, boardSize, obstructable: false, maxIterations: 1));
		return candidates;
	}
}

public class Queen : Piece {
	public static CompressedTexture2D whiteTexture = GD.Load<CompressedTexture2D>("res://pieces/png/wQ.png");
	public static CompressedTexture2D blackTexture = GD.Load<CompressedTexture2D>("res://pieces/png/bQ.png");

	
	public override CompressedTexture2D GetTexture() {
		return isWhite? whiteTexture : blackTexture;
	}
	
	public Queen(bool isWhite) : base(isWhite) {
	}

	public override List<MoveCandidate> GetObstructingMoveCandidates(Vector2I position, int boardSize) {
		List<MoveCandidate> candidates = new();
		candidates.Add(MoveRules.Line(1, 1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, 1, position, boardSize));
		candidates.Add(MoveRules.Line(1, -1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, -1, position, boardSize));
		candidates.Add(MoveRules.Line(0, 1, position, boardSize));
		candidates.Add(MoveRules.Line(-1, 0, position, boardSize));
		candidates.Add(MoveRules.Line(1, 0, position, boardSize));
		candidates.Add(MoveRules.Line(0, -1, position, boardSize));
		return candidates;
	}
}