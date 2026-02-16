

namespace ParityAnalyser.Sim
{
    public class RightSaber : Saber
    {
        public RightSaber(List<BaseNote> relevantNotes, Parity start = Parity.FOREHAND) : base(relevantNotes, start)
        {
            this.transform.position = restPoint.ToVector3();
        }

        protected override float maxClockwiseAngle => -180f;

        // Howl in the night sky has upside down hits
        protected override float maxCCAngle => 180f;

        protected override float preferredRollDirection => Math.Sign(maxClockwiseAngle);

        protected override Vector2 restPoint { get; } = new Vector2(2f, 1f);

        protected override float DesiredAngle(NoteCutDirection dir) => this.parity switch
        {
            Parity.FOREHAND => dir switch
            {
                NoteCutDirection.Up               => -180f,
                NoteCutDirection.Down             => 0f,
                NoteCutDirection.Left             => -90f,
                NoteCutDirection.Right            => 90f,
                NoteCutDirection.UpLeft           => -135f,
                NoteCutDirection.UpRight          => 135f,
                NoteCutDirection.DownRight        => 45f,
                NoteCutDirection.DownLeft         => -45f,
                NoteCutDirection.Any              => wristAngle,
                _ => 0f
            },
            Parity.BACKHAND => dir switch
            {
                NoteCutDirection.Up             => 0f,
                NoteCutDirection.Down           => -180f,
                NoteCutDirection.Left           => 90f,
                NoteCutDirection.Right          => -90f,
                NoteCutDirection.UpLeft         => 45f,
                NoteCutDirection.UpRight        => -45f,
                NoteCutDirection.DownRight      => -135f,
                NoteCutDirection.DownLeft       => 135f,
                NoteCutDirection.Any            => wristAngle,
                _ => 0f
            },
            _ => 0
        };
	}
}
