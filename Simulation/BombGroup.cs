using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BombCondition = System.Func<Beatmap.Base.BaseNote, bool>;

namespace ParityAnalyser
{
    public record struct BombGroup(BaseNote startNote, List<BaseNote> bombs, BaseNote endNote) : ISimulationObject
    {

        public float Time() => startNote.JsonTime;
        public BaseNote GetNote() => bombs.First();
        public BaseNote LastNote() => endNote;

        public IEnumerable<BaseNote> Notes() => [startNote, endNote];
        public float minX => (from bomb in bombs orderby bomb.PosX ascending select bomb.PosX).First();
        public float maxX => (from bomb in bombs orderby bomb.PosX descending select bomb.PosX).First();

        public float minY => (from bomb in bombs orderby bomb.PosY ascending select bomb.PosY).First();
        public float maxY => (from bomb in bombs orderby bomb.PosY descending select bomb.PosY).First();

        public float minTime => (from bomb in bombs orderby bomb.JsonTime ascending select bomb.JsonTime).First();
        public float maxTime => (from bomb in bombs orderby bomb.JsonTime descending select bomb.JsonTime).First();

        public bool All(BombCondition predicate) => bombs.All(predicate);
        public bool Any(BombCondition predicate) => bombs.Any(predicate);
        

        public IEnumerable<BaseNote> After(float jsonTime) => this.Where(bomb => bomb.JsonTime >= jsonTime);
        public IEnumerable<BaseNote> Where(BombCondition condition) => bombs.Where(condition);

        public IEnumerable<(BaseNote, BaseNote)> GetPairs() => new OverlappingPairIterator<BaseNote>(bombs.Append(endNote).ToList(), false);

        public bool AllConditions(List<BombCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                if (!bombs.Any(condition))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Bomb group from {startNote.JsonTime} to {endNote.JsonTime}");
            foreach (var bomb in bombs)
            {
                sb.AppendLine(bomb.JsonTime.ToString());
            }

            sb.AppendLine();
            return sb.ToString();
        }
    }


}
