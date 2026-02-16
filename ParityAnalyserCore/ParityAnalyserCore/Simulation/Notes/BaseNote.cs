

namespace ParityAnalyser.Sim
{
	public class BaseNote(float jsonTime, int type, int cutDirection, int posX, int posY)
	{
		public float JsonTime { get; set; } = jsonTime;
		public NoteType type { get; set; } = (NoteType)type;
		public NoteCutDirection cutDirection { get; set; } = (NoteCutDirection)cutDirection;
		public int PosX { get; set; } = posX;
		public int PosY { get; set; } = posY;

		public bool IsDot() => cutDirection == NoteCutDirection.Any;
		public bool IsBomb() => type == NoteType.Bomb;

		public bool IsInlineWith(BaseNote note2) => this.Position() == note2.Position();

		public bool IsInvert(BaseNote previousNote, float cutAngle)
		{
			Vector2 dir = Utils.DirectionFromDownAngle(cutAngle);
			return this.Position().SignedDistanceToPlane(previousNote.Position(), dir) > 0;
		}

		public bool IsInner() => PosX > 0 && PosX < 3;
		public bool IsMiddle() => IsInner() && PosY == 2;
	}

	public enum NoteType
	{
		Red,
		Blue,
		Bomb = 3
	}
	public enum NoteCutDirection
	{
		Up,
		Down,
		Left,
		Right,
		UpLeft,
		UpRight,
		DownLeft,
		DownRight,
		Any,
		None
	}
}
