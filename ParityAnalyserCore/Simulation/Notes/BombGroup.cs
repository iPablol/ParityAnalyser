
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BombCondition = System.Func<ParityAnalyserCore.Sim.BaseNote, bool>;

namespace ParityAnalyserCore.Sim
{
    public record struct BombGroup(ISimulationObject prevObject, List<Note> bombs, ISimulationObject nextObject) : ISimulationObject
    {
        public BaseNote startNote => prevObject.LastNote();
        public BaseNote endNote => nextObject?.FirstNote() ?? null;
        public float Time() => prevObject.FirstNote().JsonTime;
        public BaseNote FirstNote() => prevObject.FirstNote();
        public BaseNote LastNote() => nextObject.LastNote();

        public bool singleBeat => (from bomb in bombs
                                   group bomb by bomb.Time() into g
                                   select g.Key).Count() <= 1;

        public bool endsInDot => nextObject is not SliderGroup && endNote.IsDot();

        public IEnumerable<BaseNote> Notes() => prevObject.Notes().Concat(nextObject.Notes());
        public float minX => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.PosX ascending select bomb.PosX).First();
        public float maxX => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.PosX descending select bomb.PosX).First();

        public float minY => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.PosY ascending select bomb.PosY).First();
        public float maxY => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.PosY descending select bomb.PosY).First();

        public float minTime => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.JsonTime ascending select bomb.JsonTime).First();
        public float maxTime => (from bomb in bombs.ConvertAll<BaseNote>(bomb => bomb.Value) orderby bomb.JsonTime descending select bomb.JsonTime).First();

        public bool All(BombCondition predicate) => bombs.ConvertAll<BaseNote>(bomb => bomb.Value).All(predicate);
        public bool Any(BombCondition predicate) => bombs.ConvertAll<BaseNote>(bomb => bomb.Value).Any(predicate);


        public IEnumerable<BaseNote> After(float jsonTime) => this.Where(bomb => bomb.JsonTime >= jsonTime);
        public IEnumerable<BaseNote> Where(BombCondition condition) => bombs.ConvertAll<BaseNote>(bomb => bomb.Value).Where(condition);

        public IEnumerable<(BaseNote, BaseNote)> GetPairs() => new OverlappingPairIterator<BaseNote>(bombs.ConvertAll<BaseNote>(note => note.Value).Append(nextObject.FirstNote()).ToList(), false);
        public IEnumerable<(BombCluster, BombCluster)> GetClusterPairs(bool merging = true) => new OverlappingPairIterator<BombCluster>(GetClusters(merging), true, OverlappingPairIterator<BombCluster>.SingleItemBehaviour.PAIR_WITH_LAST);

        // Phantom bomb at pink diamond 236 (fixed with threshold)
        public static readonly float clusterMergeThreshold = 3 / 8f; // MARENOL has 1/2 beat bombs at 208
        public IEnumerable<BombCluster> GetClusters(bool merging = true)
        {   
            if (singleBeat)
            {
                yield return new BombCluster(bombs, bombs.First().Time());
                yield break;
            }
            var singleBeatClusters = (from bomb in bombs
                                      group bomb by bomb.Time() into cluster
                                      orderby cluster.First().Time()
                                      select new BombCluster(cluster.ToList(), cluster.First().Time()));
            if (merging)
            {
                foreach ((BombCluster cluster1, BombCluster cluster2) in new OverlappingPairIterator<BombCluster>(singleBeatClusters, true, OverlappingPairIterator<BombCluster>.SingleItemBehaviour.PAIR_WITH_LAST))
                {
                    // Merge clusters and filter for unique position
                    if (cluster2.time -  cluster1.time <= clusterMergeThreshold)
                    {
                        yield return new BombCluster((from bomb in cluster1.notes.Concat(cluster2.notes)
                                                      group bomb by bomb.FirstNote().Position() into g
                                                      select g.First()).ToList(), cluster1.time);
                    }
                    else
                    {
                        yield return cluster1;
                    }
                }
            }
            else
            {
                foreach (var cluster in singleBeatClusters) yield return cluster;
            }
        }

        public bool Satisfies(List<BombCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                if (!bombs.ConvertAll<BaseNote>(bomb => bomb.Value).Any(condition))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<BombCondition> Satisfy(List<BombCondition> conditions)
        {
            foreach (var condition in conditions) if (Any(condition)) yield return condition;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Bomb group from {prevObject.Time()} ({prevObject.GetType()}) to {nextObject.Time()} ({nextObject.GetType()})");
            foreach (var bomb in bombs)
            {
                sb.AppendLine(bomb.Time().ToString());
            }

            sb.AppendLine();
            return sb.ToString();
        }

        // previous group has startNote, next group has endNote
        public BombGroup Merge(BombGroup nextGroup) => this with
        {
            prevObject = nextGroup.prevObject,
            nextObject = nextGroup.nextObject,
            bombs = bombs.Concat(nextGroup.bombs).ToList()
        };

        public BombGroup MergeToPrevious(BombGroup previousGroup) => this with
        {
            prevObject = previousGroup.prevObject,
            nextObject = previousGroup.nextObject,
            bombs = previousGroup.bombs.Concat(bombs).ToList()
        };

    }


}
