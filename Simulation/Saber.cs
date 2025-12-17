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

        public virtual SaberSnapshot? Swing(BaseNote previousNote, BaseNote nextNote) 
        {
			//if (previousNote != null && Utils.IsStackOrSlider(previousNote, nextNote))
			//{
			//    return null;
			//}

			hasReset = false;
            if (nextNote.Type == (int)NoteType.Bomb)
            {
                if (Collision.SegmentIntersectsCircle(hilt, tip, new Vector2(nextNote.PosX, nextNote.PosY), Simulation.bombRadius))
                { 
                    Reset(nextNote, this.parity.Other());
                }
            }
            else
            {
                if (previousNote == null || !Utils.IsStackOrSlider(previousNote, nextNote))
                {
                    this.parity = this.parity.Other();
                }
                RotateTowards(previousNote, nextNote);
            }
            if (hasReset)
            {
                RotateTowards(previousNote, nextNote); // In theory this shouldn't trigger a reset a second time
            }
            return new(nextNote, this.transform.position, this.transform.rotation, this.parity, this.wristAngle, hasReset); 
        }

        public virtual void Reset(BaseNote culprit, Parity parity) 
        { 
            Debug.Log("Reset at beat " + culprit.JsonTime);
            hasReset = true;
            this.parity = parity;
            this.transform.rotation = Quaternion.identity;
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 180f : 0f, Vector3.right);
            this.wristAngle = 0;
        }

        protected virtual void RotateTowards(BaseNote previousNote, BaseNote note)
        {
            float desiredAngle = DesiredAngle((NoteDirection)note.CutDirection);
            if (previousNote != null)
            {
                if (note.CutDirection == (int)CutDir.ANY /*&& previousNote.CutDirection != (int)CutDir.ANY*/)
                {
                    desiredAngle = (float)(Math.Atan2(note.PosX - previousNote.PosX, -(note.PosY - previousNote.PosY)) * 180.0 / Math.PI);
                    desiredAngle = Utils.ClosestToZero(desiredAngle, desiredAngle - 180);
                    
				}
                //else if (note.CutDirection == (int)CutDir.ANY && previousNote.CutDirection == (int)CutDir.ANY)
                //{
                //     if (Utils.IsStackOrSlider(previousNote, note)) { return; }
                //}
            }
            float roll = desiredAngle - wristAngle;
            if (roll < -270 || roll > 270)
            {
                Reset(note, this.parity.Other());
                return;
            }
            this.wristAngle = desiredAngle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 180f : 0f, Vector3.right);

            transform.position = new Vector3(note.PosX, note.PosY, transform.position.z) - offset;
            
        }

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
