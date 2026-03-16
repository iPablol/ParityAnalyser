
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
namespace ParityAnalyserCore.Sim
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

		public bool IsInlineWith(BaseNote note2) => this.Position == note2.Position;

		public bool IsInvert(BaseNote previousNote, float cutAngle)
		{
			Vector2 dir = Utils.DirectionFromDownAngle(cutAngle);
			return this.Position.SignedDistanceToPlane(previousNote.Position, dir) > 0;
		}

		public bool IsInner => PosX > 0 && PosX < 3;
		public bool IsMiddle => IsInner && MiddleRow;

		public Vector2 Position => new (PosX, PosY);

		public bool BottomRow => PosY == 0;
		public bool MiddleRow => PosY == 1;
		public bool TopRow => PosY == 2;

		public bool LeftOuterLane => PosX == 0;
		public bool LeftInnerLane => PosX == 1;
		public bool RightInnerLane => PosX == 2;
		public bool RightOuterLane => PosX == 3;

		public Vector2 BombDodgeCenter(BombGroup group)
		{
			Vector2 offset = default;

			int dodgeOutward = group.All(bomb => bomb.IsInner) ? -1 : 1;

			switch (PosY)
			{
				case 0:
					offset = PosX > 1 ?
						// Bottom right
						new(0.5f, -0.5f) :
						// Bottom left
						new(-0.5f, -0.5f);
					break;
				case 1:
					if (RightOuterLane)
					{
						// Middle right
						offset = new(0.5f, 0f);
					}
					else if (LeftOuterLane)
					{
						// Middle left
						offset = new(-0.5f, 0f);
					}
					break;
				case 2:
					offset = PosX > 1 ?
						// Top right
						new(0.5f, 0.5f) :
						// Top left
						new(-0.5f, 0.5f);
					break;

				default: break;
			}
			offset.X *= dodgeOutward;
			return Position + offset;
		}
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
