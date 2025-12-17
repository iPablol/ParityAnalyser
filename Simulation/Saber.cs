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

        public virtual SaberSnapshot Swing(BaseNote nextNote) 
        {
            hasReset = false;
            if (nextNote.Type == (int)NoteType.Bomb)
            {
                if (Intersections.Raycast(new Ray(hilt + nextNote.Offset(), transform.up + nextNote.Offset()), out Hit hit, out float distance) && distance <= length && hit.GameObject.GetComponentInParent<BaseNote>().Type == (int)NoteType.Bomb)
                {

                    Reset(nextNote, this.parity.Other());
                }
            }
            else
            {
                this.parity = this.parity.Other();
                RotateTowards(nextNote);
            }
            if (hasReset)
            {
                RotateTowards(nextNote); // In theory this should'n trigger a reset a second time
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

        protected virtual void RotateTowards(BaseNote note)
        {
            float desiredAngle = DesiredAngle((NoteDirection)note.CutDirection);
            float roll = desiredAngle - wristAngle;
            if (roll < maxClockwiseAngle || roll > maxCCAngle)
            {
                Reset(note, this.parity.Other());
                return;
            }
            this.wristAngle = desiredAngle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 180f : 0f, Vector3.right);

            transform.position = new Vector3(note.PosX, note.PosY, transform.position.z);
            
        }
        protected abstract float maxClockwiseAngle { get; }
        protected abstract float maxCCAngle { get; }

        public float wristAngle { get; protected set; }

        public static readonly float length = 1.5f;
        public Vector3 hilt => this.transform.position;
        public Vector3 tip => this.transform.position + (length * transform.up);

        public IEnumerator<BaseNote> GetEnumerator() => this.notes.GetEnumerator();
        public IEnumerable<(BaseNote, BaseNote)> GetPairs() => new OverlappingPairIterator<BaseNote>(this.notes, true);

        public static implicit operator List<BaseNote>(Saber saber) => saber.notes;

        protected abstract float DesiredAngle(CutDir dir);
    }
}
