using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Parity = ParityAnalyser.ParityAnalyser.Parity;
using CutDir = ParityAnalyser.NoteDirection;
using Beatmap.Base;
using Beatmap.Enums;
using Unity.Collections;

using Hit = Intersections.IntersectionHit;
using BombCondition = System.Func<Beatmap.Base.BaseNote, bool>;
using static Beatmap.V4.V4CommonData;

namespace ParityAnalyser.Sim
{
    public abstract class Saber
    {
        public List<ISimulationObject> notes { get; private set; } = [];
        public List<BombGroup> bombGroups { get; private set; } = [];
        public Saber(List<BaseNote> relevantNotes, Parity start = Parity.BACKHAND)
        {
            this.transform.rotation = Quaternion.identity;
            this.parity = start;
            
            IEnumerable<ISimulationObject> notes = from note in relevantNotes
                               where !note.IsBomb() select new Note(note);
            IEnumerable<ISimulationObject> bombGroups = ExtractBombGroups(relevantNotes);

            IEnumerable<ISimulationObject> sliderGroups = ExtractSliderGroups(relevantNotes);

            this.bombGroups = bombGroups.Cast<BombGroup>().ToList();

            this.notes = (from simObject in notes.Concat(bombGroups).Concat(sliderGroups)
                    orderby simObject.Time() ascending select simObject).ToList();

            this.wristAngle = 0;
        }
        private GameObject dummyObject = new GameObject("saber dummy object");
        protected Transform transform => dummyObject.transform;
        public Parity parity {  get; protected set; } = Parity.BACKHAND;

        private bool hasReset = false;

        public List<SaberSnapshot> resetSnapshots { get; protected set; } = [];

        public virtual SaberSnapshot? FirstSwing() => this.Swing(null, notes.First());


        public virtual SaberSnapshot? Swing(ISimulationObject previousObject, ISimulationObject nextObject) 
        {

			hasReset = false;

            this.parity = this.parity.Other();


            if (nextObject is BombGroup group)
            {
                HandleBombGroup(group);
            }
            else if (nextObject is SliderGroup slider)
            {

            }
            else
            {
                RotateTowards(previousObject, nextObject);
            }
            return new(nextObject, this.transform.position, this.transform.rotation, this.parity, this.wristAngle, hasReset); 
        }

        public virtual void Reset(BaseNote culprit, Parity parity, string reason) 
        {
            ParityAnalyser.outline.AddToCache(culprit, Color.yellow);
            Debug.Log($"Reset at beat {culprit.JsonTime}. Reason: {reason}");
            hasReset = true;
            this.parity = parity;
            this.transform.rotation = Quaternion.identity;
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 0f : 180f, Vector3.right);
            this.wristAngle = 0;

            this.resetSnapshots.Add(new (new Note(culprit), transform.position, transform.rotation, parity, wristAngle, true));
        }

        protected void MoveTo(Vector2 pos) { this.transform.position = new Vector3(pos.x, pos.y, transform.position.z); }
        protected void MoveTo(Vector3 pos) { this.transform.position = pos; }

        protected virtual void RotateTowards(ISimulationObject previousObject, ISimulationObject nextObject)
        {
            BaseNote previousNote = previousObject?.LastNote() ?? null;
            BaseNote nextNote = nextObject.GetNote();
            float desiredAngle = CutAngle(previousNote, nextNote);
            float roll = desiredAngle - wristAngle;
            if (roll < -270 || roll > 270)
            {
                Reset(nextNote, this.parity.Other(), "Wristroll too large");
                return;
            }
            this.wristAngle = desiredAngle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? -180f : 0f, Vector3.right);

            if ((CutDir)nextNote.CutDirection != CutDir.ANY)
            {
                MoveTo((Vector3)nextNote.Position() - offset);
            }
            else
            {
                MoveTo(nextNote.Position());
            }

        }

        protected virtual float CutAngle(BaseNote prevNote, BaseNote nextNote)
        {
            float desiredAngle = DesiredAngle((NoteDirection)nextNote.CutDirection);
            if (prevNote != null)
            {
                if (nextNote.CutDirection == (int)CutDir.ANY /*&& previousNote.CutDirection != (int)CutDir.ANY*/)
                {
                    // TODO: maybe check inlines (example: also abstruse dilemma)
                    // also try different directions if the swing collides with bombs   
                    MoveTo(prevNote.Position());
                    Vector2 dir = (nextNote.Position() - (Vector2)transform.position).normalized;
                    if (!parity.Bool()) dir = -dir;
                    float signedAngle = Vector2.SignedAngle(Vector2.down, dir);
                    desiredAngle = Mathf.DeltaAngle(0f, signedAngle);
                    Debug.Log($"Beat: {nextNote.JsonTime}, Angle: {desiredAngle}, Signed: {signedAngle}");
                    //desiredAngle = Utils.ClosestToZero(desiredAngle, desiredAngle - 180);

                }
                //else if (note.CutDirection == (int)CutDir.ANY && previousNote.CutDirection == (int)CutDir.ANY)
                //{
                //     if (Utils.IsStackOrSlider(previousNote, note)) { return; }
                //}
            }
            return desiredAngle;
        }

        protected virtual float WristRoll(BaseNote prevNote, BaseNote nextNote) => CutAngle(prevNote, nextNote) - wristAngle;

        private void HandleBombGroup(BombGroup group)
        {

            if (group.endNote == null)
            {
                // Map ends with bombs
                return;
            }

            float roll = WristRoll(group.startNote, group.endNote);

            bool saberCollides = group.Any((note) => Collision.SegmentIntersectsCircle(hilt, tip, note.Position(), Simulation.bombRadius));
            
           
            //if (!saberCollides)
            //{
            //    return;
            //}
            List<BombCondition> bottomRowReset = [
                (note) => note.LeftOuterLane() && note.BottomRow(),
                (note) => note.LeftInnerLane() && note.BottomRow(),
                (note) => note.RightInnerLane() && note.BottomRow(),
                (note) => note.RightOuterLane() && note.BottomRow(),
                ];


            bool shouldResetDueToAngle = Mathf.Abs(wristAngle + roll) > 90f;
            bool shouldResetDueToInline = group.endNote.BottomRow();
            bool shouldResetDueToRoll = roll >= 180f;

            
            //Debug.Log($"Beat: {group.startNote.JsonTime}, angle: {wristAngle}, roll: {roll}, condition: {Mathf.Abs(wristAngle + roll)}, desired: {DesiredAngle((CutDir)group.endNote.CutDirection)}");
            if (parity == Parity.BACKHAND && Mathf.Abs(wristAngle) <= 45f && group.AllConditions(bottomRowReset) && (shouldResetDueToAngle || shouldResetDueToInline || shouldResetDueToRoll))
            {
                //TODO: try to move out of the way
                Reset(group.bombs[0], Parity.FOREHAND, "Bottom row reset");
                return;
            }

            List<BombCondition> topRowReset = [
                (note) => note.LeftOuterLane() && note.TopRow(),
                (note) => note.LeftInnerLane() && note.TopRow(),
                (note) => note.RightInnerLane() && note.TopRow(),
                (note) => note.RightOuterLane() && note.TopRow(),
                ];

            shouldResetDueToInline = group.endNote.TopRow();
            if (parity == Parity.FOREHAND && Mathf.Abs(wristAngle) <= 45f && group.AllConditions(topRowReset) && (shouldResetDueToAngle || shouldResetDueToInline || shouldResetDueToRoll))
            {
                //TODO: try to move out of the way
                Reset(group.bombs[0], Parity.BACKHAND, "Top row reset");
                return;
            }

            // Spiral reset
            // TODO: Distinguish from bomb spam (example: MARENOL), detect spirals with missing bombs (example: meowter space
            if (group.AllConditions(topRowReset) && group.AllConditions(bottomRowReset))
            {
                bool isBottomRow = (from b in @group.Where(note => note.TopRow() || note.BottomRow())
                                    orderby b.JsonTime descending
                                    select b).Last().BottomRow();
                if (isBottomRow)
                {
                    Reset(group.bombs[0], Parity.FOREHAND, "Bottom row spiral reset");
                    return;
                }
                else
                {
                    Reset(group.bombs[0], Parity.BACKHAND, "Top row spiral reset");
                    return;
                }

            }

            // Quick bomb reset
            float quickBombThreshold = 1 / 4f;
            IEnumerable<BaseNote> inlineBombs = group.Where(bomb => bomb.PosX == group.startNote.PosX && bomb.PosY == group.startNote.PosY);
            if (inlineBombs.Count() > 0)
            {
                bool quickInlineBomb = inlineBombs.OrderByDescending(bomb => bomb.JsonTime).Last().JsonTime - group.startNote.JsonTime < quickBombThreshold;

                if (quickInlineBomb)
                {
                    Reset(group.bombs[0], parity, "Inline bomb");
                    return;
                }
            }

            
            //if (ExploreBombGroup(group, group.startNote.JsonTime, group.endNote.JsonTime) is BaseNote bomb)
            //{
            //    Reset(bomb, parity.Other(), "Swing path");
            //    return;
            //}

            // TODO: reset when the only resting position available is covered by bombs, all other slots are walls

        }

        protected virtual BaseNote ExploreBombGroup(BombGroup group, float startTime, float endTime, bool recursive = false)
        {
            float roll = WristRoll(group.startNote, group.endNote);
            Vector3 originalPos = transform.position;
            foreach ((BaseNote bomb1, BaseNote bomb2) in group.GetPairs())
            {
                float swingOffset = parity.Bool() ? 180f : 0f;
                float startAngle = wristAngle + swingOffset;
                float endAngle = wristAngle + roll + swingOffset;
                float lerpFactor = (endTime - startTime);

                float bomb1LerpAmount = (bomb1.JsonTime - startTime) / lerpFactor;
                float bomb2LerpAmount = (bomb2.JsonTime - startTime) / lerpFactor;

                bool isCenter = bomb1.MiddleRow() && (bomb1.LeftInnerLane() || bomb1.RightInnerLane());
                float distanceToBomb = (new Vector2(hilt.x, hilt.y) - bomb1.Position()).magnitude;

                Vector2 hiltPos = new Vector2(hilt.x, hilt.y);

                Vector3 zOff = new Vector3(0, 0, bomb1.zPos());
                Vector2 center = new Vector2(1.5f, 1f);
                //Utils.RenderLine((Vector3)hiltPos + zOff, (Vector3)center + zOff, Color.magenta, Color.magenta, 0.3f);
                Vector2 directionToCenter = (center - hiltPos).normalized * (isCenter ? -1f : 1f);

                float movementScale = 10f;
                float timeScaleFactor = (bomb2.JsonTime - bomb1.JsonTime);
                //float distanceScaleFactor = (1 / Mathf.Pow(Vector2.Distance(hiltPos, bomb1.Position()), 2));
                float distanceScaleFactor = Vector2.Distance(hiltPos, center);
                this.transform.position += (Vector3)(movementScale * directionToCenter * timeScaleFactor * distanceScaleFactor);


                Utils.RenderLine((Vector3)hiltPos + zOff, transform.position + zOff, Color.yellow, Color.black);

                bool swingPathCollides = Collision.SwingPathIntersects(hilt, Mathf.Lerp(startAngle, endAngle, bomb1LerpAmount), Mathf.Lerp(startAngle, endAngle, bomb2LerpAmount), bomb1.Position(), true, bomb1.zPos());

                if (swingPathCollides)
                {
                    // Try to do the full roll from start until this bomb?
                    if (recursive)
                    {
                        Reset(bomb1, parity.Other(), "Swing path (recursive resolution)");
                        return bomb1;
                    }
                    else if (ExploreBombGroup(group, startTime, bomb1.JsonTime, false) is BaseNote note)
                    {
                        // Then try resetting and roll from the reset position
                        Reset(note, parity.Other(), "Swing path (possible spiral)");
                        return ExploreBombGroup(group, bomb1.JsonTime, endTime, true);
                    }
                    else
                    {
                        return null;
                    }

                    //Reset(bomb1, parity.Other(), "Swing path");
                    return bomb1;
                }
            }
            return null;
        }

        public virtual IEnumerable<ISimulationObject> ExtractBombGroups(List<BaseNote> allNotes)
        {
            bool firstNoteHappened = false;
            for (int i = 0; i < allNotes.Count; i++)
            {
                if (allNotes[i].Type != (int)NoteType.Bomb) firstNoteHappened = true;
                if (firstNoteHappened && allNotes[i].Type == (int)NoteType.Bomb)
                {
                    BaseNote start = allNotes[i - 1];
                    List<BaseNote> bombs = [];
                    while (allNotes[i].Type == (int)(NoteType.Bomb))
                    {
                        bombs.Add(allNotes[i]);
                        if (i == allNotes.Count - 1)
                        {
                            yield return new BombGroup(start, bombs, null);
                            yield break;
                        }
                        i++;
                    }
                    yield return new BombGroup(start, bombs, allNotes[i]);
                }
            }

        }

        public virtual IEnumerable<ISimulationObject> ExtractSliderGroups(List<BaseNote> allNotes)
        {
            for (int i = 1; i < allNotes.Count; i++)
            {
                if (Utils.IsStackOrSlider(allNotes[i - 1], allNotes[i]))
                {
                    BaseNote previousNote = i <= 1 ? null : allNotes[i - 2];
                    List<BaseNote> slider = [allNotes[i - 1]];
                    while (Utils.IsStackOrSlider(allNotes[i - 1], allNotes[i]))
                    {
                        slider.Add(allNotes[i]);
                        i++;
                        if (i >= allNotes.Count) break;
                    }
                    yield return new SliderGroup(previousNote, slider);
                }
            }
        }

        public void RenderBombGroups(bool renderStart = false, bool renderEnd = false)
        {
            foreach (BombGroup group in bombGroups)
            {
                Color groupColor = Utils.RandomColor();
                if (renderStart) ParityAnalyser.outline.AddToCache(group.startNote, groupColor);
                foreach (BaseNote bomb in group.bombs)
                {
                    ParityAnalyser.outline.AddToCache(bomb, groupColor);
                }
                if (renderEnd) ParityAnalyser.outline.AddToCache(group.endNote, groupColor);
            }
        }

        public List<SliderGroup> sliderGroups { get; private set; } = [];

        private Vector3 offset => transform.up * CutDistance;
        public static readonly float CutDistance = 1f;

        protected abstract float maxClockwiseAngle { get; }
        protected abstract float maxCCAngle { get; }

        public float wristAngle { get; protected set; }

        public static readonly float length = 2.1f;
        public Vector3 hilt => this.transform.position;
        public Vector3 tip => this.transform.position + (length * transform.up);

        public IEnumerator<ISimulationObject> GetEnumerator() => this.notes.GetEnumerator();
        public IEnumerable<(ISimulationObject, ISimulationObject)> GetPairs() => new OverlappingPairIterator<ISimulationObject>(this.notes, false);

        public static implicit operator List<ISimulationObject>(Saber saber) => saber.notes;

        protected abstract float DesiredAngle(CutDir dir);
    }
}
