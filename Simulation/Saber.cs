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
        public List<BaseNote> notes { get; private set; }
        public Saber(List<BaseNote> relevantNotes, Parity start = Parity.BACKHAND)
        {
            this.transform.rotation = Quaternion.identity;
            this.parity = start;
            this.notes = relevantNotes;
            this.wristAngle = 0;
        }
        private GameObject dummyObject = new GameObject("saber dummy object");
        protected Transform transform => dummyObject.transform;
        public Parity parity {  get; protected set; } = Parity.BACKHAND;

        private bool hasReset = false;

        public virtual SaberSnapshot? FirstSwing() => this.Swing(null, notes.First());

        private int bombGroupIndex = 0;

        public virtual SaberSnapshot? Swing(BaseNote previousNote, BaseNote nextNote) 
        {

			hasReset = false;
            //if (nextNote.Type == (int)NoteType.Bomb)
            //{
            //    if (Collision.SegmentIntersectsCircle(hilt, tip, new Vector2(nextNote.PosX, nextNote.PosY), Simulation.bombRadius))
            //    { 
            //        Reset(nextNote, this.parity.Other());
            //    }
            //}
            if (previousNote == null || !Utils.IsStackOrSlider(previousNote, nextNote))
            {
                this.parity = this.parity.Other();
            }
            
            if (bombGroupIndex < bombGroups.Count && previousNote == bombGroups[bombGroupIndex].startNote)
            {
                // Reached next bomb group
                HandleBombGroup();
            }
            if (hasReset)
            {
                RotateTowards(previousNote, nextNote);
            }
            else
            {
                RotateTowards(previousNote, nextNote);
            }
            return new(nextNote, this.transform.position, this.transform.rotation, this.parity, this.wristAngle, hasReset); 
        }

        public virtual void Reset(BaseNote culprit, Parity parity, string reason) 
        { 
            Debug.Log($"Reset at beat {culprit.JsonTime}. Reason: {reason}");
            hasReset = true;
            this.parity = parity;
            this.transform.rotation = Quaternion.identity;
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 180f : 0f, Vector3.right);
            this.wristAngle = 0;
        }

        protected virtual void RotateTowards(BaseNote previousNote, BaseNote note)
        {
            float desiredAngle = CutAngle(previousNote, note);
            float roll = desiredAngle - wristAngle;
            if (roll < -270 || roll > 270)
            {
                Reset(note, this.parity.Other(), "Wristroll too large");
                return;
            }
            this.wristAngle = desiredAngle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? -180f : 0f, Vector3.right);

            transform.position = new Vector3(note.PosX, note.PosY, transform.position.z) - offset;
            
        }

        protected virtual float CutAngle(BaseNote prevNote, BaseNote nextNote)
        {
            float desiredAngle = DesiredAngle((NoteDirection)nextNote.CutDirection);
            if (prevNote != null)
            {
                if (nextNote.CutDirection == (int)CutDir.ANY /*&& previousNote.CutDirection != (int)CutDir.ANY*/)
                {
                    desiredAngle = (float)(Math.Atan2(nextNote.PosX - prevNote.PosX, - (nextNote.PosY - prevNote.PosY)) * 180.0 / Math.PI);
                    desiredAngle = Utils.ClosestToZero(desiredAngle, desiredAngle - 180);

                }
                //else if (note.CutDirection == (int)CutDir.ANY && previousNote.CutDirection == (int)CutDir.ANY)
                //{
                //     if (Utils.IsStackOrSlider(previousNote, note)) { return; }
                //}
            }
            return desiredAngle;
        }

        protected virtual float WristRoll(BaseNote prevNote, BaseNote nextNote) => CutAngle(prevNote, nextNote) - wristAngle;

        private void HandleBombGroup()
        {
            BombGroup group = bombGroups[bombGroupIndex++];

            if (group.endNote == null)
            {
                // Map ends with bombs
                return;
            }

            float roll = WristRoll(group.startNote, group.endNote);

            bool saberCollides = group.Any((note) => Collision.SegmentIntersectsCircle(hilt, tip, note.Position(), Simulation.bombRadius));
            //TODO: Check swing path collision (wrist angle sector collides with bomb)
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
            if (parity == Parity.FOREHAND && Mathf.Abs(wristAngle) >= 135f && group.AllConditions(topRowReset) && (shouldResetDueToAngle || shouldResetDueToInline || shouldResetDueToRoll))
            {
                //TODO: try to move out of the way
                Reset(group.bombs[0], Parity.BACKHAND, "Top row reset");
                return;
            }

            // Spiral reset
            if (group.AllConditions(topRowReset) && group.AllConditions(bottomRowReset))
            {
                bool isBottomRow = (from bomb in @group.Where(note => note.TopRow() || note.BottomRow())
                                    orderby bomb.JsonTime descending
                                    select bomb).Last().BottomRow();
                if (isBottomRow) Reset(group.bombs[0], Parity.FOREHAND, "Bottom row spiral reset"); else Reset(group.bombs[0], Parity.BACKHAND, "Top row spiral reset");
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
                }
            }

            // TODO: reset when the only resting position available is covered by bombs, all other slots are walls

        }

        public virtual void ExtractBombGroups()
        {
            bool firstNoteHappened = false;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].Type != (int)NoteType.Bomb) firstNoteHappened = true;
                if (firstNoteHappened && notes[i].Type == (int)NoteType.Bomb)
                {
                    BaseNote start = notes[i - 1];
                    List<BaseNote> bombs = [];
                    while (notes[i].Type == (int)(NoteType.Bomb))
                    {
                        bombs.Add(notes[i++]);
                        if (i == notes.Count - 1)
                        {
                            bombGroups.Add(new(start, bombs, null));
                            goto Finish;
                        }
                    }
                    bombGroups.Add(new (start, bombs, notes[i]));
                }
            }
            Finish:
            notes = notes.Where(note => note.Type != (int)NoteType.Bomb).ToList();

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

        public List<BombGroup> bombGroups { get; private set; } = [];

        private Vector3 offset => transform.up * CutDistance;
        public static readonly float CutDistance = 1f;

        protected abstract float maxClockwiseAngle { get; }
        protected abstract float maxCCAngle { get; }

        public float wristAngle { get; protected set; }

        public static readonly float length = 2.1f;
        public Vector3 hilt => this.transform.position;
        public Vector3 tip => this.transform.position + (length * transform.up);

        public IEnumerator<BaseNote> GetEnumerator() => this.notes.GetEnumerator();
        public IEnumerable<(BaseNote, BaseNote)> GetPairs() => new OverlappingPairIterator<BaseNote>(this.notes, false);

        public static implicit operator List<BaseNote>(Saber saber) => saber.notes;

        protected abstract float DesiredAngle(CutDir dir);
    }
}
