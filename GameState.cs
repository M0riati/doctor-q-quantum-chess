using System.Collections.Generic;
using System.Xml.Schema;
using Godot;

public struct SpecialMove {
	public Vector2I from;
	public Vector2I to;
	public List<(Vector2I, Vector2I)> difference;
}

internal interface ISpecialMoveRule {
	public static abstract List<SpecialMove> FindMatches(GameState state);
}


class EnPassant : ISpecialMoveRule {
	private static bool CanEnPassant(Pawn pawn1, Pawn pawn2) {
		GD.Print(pawn2.isEnPassantable);
		return pawn2.isEnPassantable && (pawn1.isWhite != pawn2.isWhite);
	}

	public static List<SpecialMove> FindMatches(GameState state) {
		var validSpecialMoves = new List<SpecialMove>();
		for (int x = 0; x < state.size; x++) {
			for (int y = 0; y < state.size; y++) {
				if (state.GetAt(x, y) is Pawn pawn && pawn.isWhite == state.IsWhiteTurn()) {
					var leftPos = new Vector2I(x, y - 1);
					var rightPos = new Vector2I(x, y + 1);
					var pieceL = state.GetAt(leftPos);
					var pieceR = state.GetAt(rightPos);
					if (pieceL is Pawn pawnL && CanEnPassant(pawn, pawnL)) {
						var difference = new List<(Vector2I, Vector2I)>();
						difference.Add((leftPos, new Vector2I(-1, -1)));
						var targetPos = new Vector2I(x + (pawn.isWhite ? 1 : -1), y - 1);
						if (state.GetAt(targetPos) is not null) {
							continue;
						}
						difference.Add((new Vector2I(x, y), targetPos));
						validSpecialMoves.Add(new SpecialMove() {
							difference = difference,
							from = new Vector2I(x, y),
							to = targetPos
						});
						continue;
					}

					if (pieceR is Pawn pawnR && CanEnPassant(pawn, pawnR)) {
						var difference = new List<(Vector2I, Vector2I)>();
						var targetPos = new Vector2I(x + (pawn.isWhite ? 1 : -1), y + 1);
						if (state.GetAt(targetPos) is not null) {
							continue;
						}
						difference.Add((rightPos, new Vector2I(-1, -1)));
						difference.Add((new Vector2I(x, y), targetPos));
						validSpecialMoves.Add(new SpecialMove() {
							difference = difference,
							from = new Vector2I(x, y),
							to = targetPos
						});
					}
				}
			}
		}
		return validSpecialMoves;
	}
}

class Castle : ISpecialMoveRule {
	private static SpecialMove? GetCastle(GameState state, Vector2I kingPos, Vector2I rookPos) {
		if (state.GetAt(kingPos).hasMoved || state.GetAt(rookPos).hasMoved) {
			return null;
		}
		int direction = kingPos.Y < rookPos.Y ? 1 : -1;
		for (int y = kingPos.Y+direction; y != rookPos.Y; y += direction) {
			if (state.GetAt(kingPos.X, y) != null) {
				return null;
			}
		}
		var king = state.GetAt(kingPos);
		var rook = state.GetAt(rookPos);
		var difference = new List<(Vector2I, Vector2I)>();
		difference.Add((kingPos, kingPos + Vector2I.Up*(2 * -direction)));
		difference.Add((rookPos, kingPos + Vector2I.Up*(1 * -direction)));
		return new SpecialMove() {
			from = kingPos,
			to = rookPos,
			difference = difference
		};
	}
	
	public static List<SpecialMove> FindMatches(GameState state) {
		var validCastleMoves = new List<SpecialMove>();
		var rank = state.IsWhiteTurn() ? 0 : 7;
		var kingPos = new Vector2I(rank, 4);
		var rook1Pos = new Vector2I(rank, 0);
		var rook2Pos = new Vector2I(rank, state.size-1);
		var king = state.GetAt(kingPos);
		if (king is not King) {
			return validCastleMoves;
		}
		var potentialRook1 = state.GetAt(rook1Pos);
		var potentialRook2 = state.GetAt(rook2Pos);
		if (potentialRook1 is Rook) {
			var potentialMove = GetCastle(state, kingPos, rook1Pos);
			if (potentialMove is { } castleMove) {
				validCastleMoves.Add(castleMove);
			}
		}	
		if (potentialRook2 is Rook) {
			var potentialMove = GetCastle(state, kingPos, rook2Pos);
			if (potentialMove is { } castleMove) {
				validCastleMoves.Add(castleMove);
			}
		}

		return validCastleMoves;
	}
}

public class GameState {
	public int size { get; }

	private Piece[,] board;
	public int move;
	public bool checkWhite;
	public bool checkBlack;
	private bool noValidMoves;
	public bool checkmate => noValidMoves && (checkWhite || checkBlack);
	public bool stalemate => noValidMoves && !checkWhite && !checkBlack;
	private List<Vector2I>[,] legalMoves;
	public List<SpecialMove> specialMoves;
	
	
	public GameState(int size) {
		this.size = size;
		this.board = new Piece[size, size];
		this.legalMoves = new List<Vector2I>[size, size];
		this.specialMoves = new List<SpecialMove>();
		this.move = 0;
		this.checkWhite = false;
		this.checkBlack = false;
		this.noValidMoves = false;
	}

	public bool IsWhiteTurn() {
		return move % 2 == 0;
	}

	public Piece GetAt(int rank, int file) {
		if (rank < 0 || rank >= size || file < 0 || file >= size) {
			return null;
		}

		return board[rank, file];
	}

	public Piece GetAt(Vector2I square) {
		if (square.X < 0 || square.X >= size || square.Y < 0 || square.Y >= size) {
			return null;
		}

		return board[square.X, square.Y];
	}

	public bool IsEmpty(int rank, int file) {
		return board[rank, file] == null;
	}

	public bool IsEmpty(Vector2I square) {
		return board[square.X, square.Y] == null;
	}

	public void SetAt(int rank, int file, Piece piece) {
		board[rank, file] = piece;
	}

	public void SetAt(Vector2I square, Piece piece) {
		board[square.X, square.Y] = piece;
	}

	public List<Vector2I> GetLegalMoves(int rank, int file) {
		// This is lazy, so legal moves are only computed at the end of the turn or if they don't exist
		if (legalMoves[rank, file] == null) {
			legalMoves[rank, file] = ComputeLegalMoves(new Vector2I(rank, file));
		}
		return legalMoves[rank, file];
	}
	
	public List<Vector2I> GetLegalMoves(Vector2I square) {
		return GetLegalMoves(square.X, square.Y);
	}

	public List<Vector2I> ComputeLegalMoves(Vector2I square) {
		var piece = GetAt(square);
		if (piece == null) {
			return new();
		}

		var moveCandidates = piece.GetObstructingMoveCandidates(square, size);
		var computedLegalMoves = new List<Vector2I>();

		// remove obstructed moves
		foreach (var baseMoveCandidate in moveCandidates) {
			var moveCandidate = baseMoveCandidate.obstructs;
			while (moveCandidate != null) {
				var otherPiece = GetAt(moveCandidate.square);
				if (otherPiece != null) {
					if (moveCandidate.onCapture && (otherPiece.isWhite != piece.isWhite)) {
						computedLegalMoves.Add(moveCandidate.square);
					}

					if (moveCandidate.obstructable) {
						break;
					}
				}
				else if (moveCandidate.onNonCapture) {
					computedLegalMoves.Add(moveCandidate.square);
				}

				moveCandidate = moveCandidate.obstructs;
			}
		}

		return computedLegalMoves;
	}

	private List<SpecialMove> ComputeSpecialMoves() {
		var moves = new List<SpecialMove>();
		moves.AddRange(EnPassant.FindMatches(this));
		moves.AddRange(Castle.FindMatches(this));
		return moves;
	}

	private GameState GetStateAfterMove(Vector2I from, Vector2I to) {
		var newState = new GameState(size);
		for (int x = 0; x < this.size; x++) {
			for (int y = 0; y < this.size; y++) {
				newState.SetAt(x, y, this.GetAt(x, y)?.Clone<Piece>());
			}
		}

		newState.RunMove(from, to, false);
		return newState;
	}

	private void PostMove(bool computeUncheck = true) {
		move++;
		checkBlack = checkWhite = false;
		for (int x = 0; x < this.size; x++) {
			for (int y = 0; y < this.size; y++) {
				this.legalMoves[x, y] = ComputeLegalMoves(new Vector2I(x, y));
				if (checkBlack && checkWhite) continue;
				foreach (var legalMove in this.legalMoves[x, y]) {
					if (this.GetAt(legalMove)?.GetType()?.Name == "King") {
						this.checkBlack |= !this.GetAt(legalMove).isWhite;
						
						this.checkWhite |= this.GetAt(legalMove).isWhite;
						GD.Print("check");
						break;
					}
				}
			}
		}

		if (computeUncheck) {
			noValidMoves = true;
			specialMoves = ComputeSpecialMoves();
			for (int x = 0; x < this.size; x++) {
				for (int y = 0; y < this.size; y++) {
					CheckPromotion(new Vector2I(x, y));
					if (this.GetAt(x, y)?.isWhite != this.IsWhiteTurn()) continue;
					var newLegalMoves = new List<Vector2I>();
					var legalMovesXy = this.legalMoves[x, y];
					for (int i = 0; i < legalMovesXy.Count; i++) {
						var stateAfterMove = GetStateAfterMove(new Vector2I(x, y), legalMovesXy[i]);
						if (!(this.IsWhiteTurn() && stateAfterMove.checkWhite) && !(!this.IsWhiteTurn() && stateAfterMove.checkBlack)) {
							newLegalMoves.Add(legalMovesXy[i]);
							GD.Print(x, y, legalMovesXy[i]);
							noValidMoves = false;
						}
					}

					this.legalMoves[x, y] = newLegalMoves;
				}
			}
			GD.Print($"No valid moves: {noValidMoves}");
			
		}
	}

	private void Move(Vector2I from, Vector2I to) {
		var movingPiece = GetAt(from);
		movingPiece.OnMove(from, to);
		SetAt(to, movingPiece);
		SetAt(from, null);
	}

	private void CheckPromotion(Vector2I pos) {
		if (GetAt(pos) is Pawn pawn) {
			if (!pawn.isWhite && pos.X > 0) {
				return;
			}
			if (pawn.isWhite && pos.X < this.size - 1) {
				return;
			}
			GD.Print("Promotion");
			SetAt(pos, new Queen(pawn.isWhite));
		}
	}
	
	public void RunSpecialMove(SpecialMove specialMove, bool computeUncheck = true) {
		foreach (var (from, to) in specialMove.difference) {
			if (to == -Vector2I.One) {
				this.SetAt(from, null);
			}
			else {
				Move(from, to);
			}
		}
		PostMove(computeUncheck);
	}

	public void RunMove(Vector2I from, Vector2I to, bool computeUncheck = true) {
		foreach (var piece in board) {
			if (piece == null) continue;
			piece.OnTurn();
		}
		Move(from, to);
		PostMove(computeUncheck);
	}
}
