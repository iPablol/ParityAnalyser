using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Parity = ParityAnalyser.Parity;
using CutDir = ParityAnalyser.NoteDirection;
using Beatmap.Base;
using Beatmap.Enums;
using Unity.Collections;

using Hit = Intersections.IntersectionHit;
using BombCondition = System.Func<Beatmap.Base.BaseNote, bool>;
using static Beatmap.V4.V4CommonData;
using System.Collections;
using System.Net;

namespace ParityAnalyser.Sim
{
    public abstract class Saber
    {
        private Stack<SaberSnapshot> stack = [];

        private void PushToStack(BaseNote note) => stack.Push(TakeSnapshot(note));

        private BaseNote PopStack()
        {
            SaberSnapshot snap = stack.Pop();
            transform.position = snap.position;
            transform.rotation = snap.rotation;
            parity = snap.parity;
            wristAngle = snap.wristAngle;
            hasReset = snap.reset;
            return snap.note;
        }

        private void ClearStack() => stack.Clear();

        public SaberSnapshot TakeSnapshot(BaseNote note) => new(note, transform.position, transform.rotation, parity, wristAngle, hasReset);
        public List<ISimulationObject> notes { get; private set; } = [];
        public List<BombGroup> bombGroups { get; private set; } = [];
        public Saber(List<BaseNote> relevantNotes, Parity start = Parity.FOREHAND)
        {
            this.transform.rotation = Quaternion.identity;
            this.parity = start;
            

            List<ISimulationObject> sliderGroups = ExtractSliderGroups(relevantNotes).ToList();
            for (int i = 0; i < sliderGroups.Count; i++)
            {
                SliderGroup group = (SliderGroup)sliderGroups[i];
                if (group.isStack && !group.isDotStack)
                {
                    sliderGroups[i] = group.Order(group.GetOrder());
                }
            }

            // Replace sliders
            List<ISimulationObject> currentNotes = (from simObject in
                                                (from note in relevantNotes
                                                   where !(sliderGroups.Cast<SliderGroup>().Any(slider => slider.Notes().Contains(note))) 
                                                   select new Note(note) as ISimulationObject).Concat(sliderGroups)
                                             orderby simObject.Time() ascending
                                             select simObject).ToList();

            List<ISimulationObject> bombGroups = ExtractBombGroups(currentNotes).ToList();
            
            // Remove bombs
            currentNotes = (from note in currentNotes
                     where note is SliderGroup || (note is Note n && !n.Value.IsBomb())
                     select note).ToList();



            this.notes = (from simObject in currentNotes.Concat(bombGroups)
                          orderby simObject.Time() ascending
                          select simObject).ToList();

            List<ISimulationObject> merged = [];
            for (int i = 0; i < this.notes.Count - 1; i++)
            {
                if (this.notes[i] is BombGroup group1 &&
                    this.notes[i + 1] is BombGroup group2)
                {
                    var mergedGroup = group1.Merge(group2);

                    // Replace the original group with the merged value
                    this.notes[i] = mergedGroup;

                    // Mark the second group for removal
                    merged.Add(group2);
                }
            }

            this.notes = this.notes.Where(o => !merged.Contains(o)).ToList();

            this.bombGroups = this.notes.Where(o => o is BombGroup).Cast<BombGroup>().ToList();
            bool debug = (this is LeftSaber && ParityAnalyser.options.debugLeftBombCollisions) ||
                        (this is RightSaber && ParityAnalyser.options.debugRightBombCollisions);
            if (debug)
            {
                foreach (var group in this.bombGroups)
                {
                    //Debug.Log("");
                    //Debug.Log("Group " + group.Time());
                    //List<BombCluster> clusters = group.GetClusters().ToList();
                    //foreach (var cluster in clusters)
                    //{
                    //    Debug.Log(cluster.GetHitbox().Count());
                    //    //cluster.Render();
                    //}
                    //Debug.Log("");
                }
            }

            this.wristAngle = 0;
        }
        private GameObject dummyObject = new GameObject("saber dummy object");
        protected Transform transform => dummyObject.transform;
        public Parity parity 
        {  
            get; 
            protected set; 
        }

        private bool hasReset = false;

        public virtual IEnumerable<SaberSnapshot> FirstSwing()
        {
            yield return TakeSnapshot(new Note(new BaseNote() { JsonTime = 0 }));
            foreach (var snapshot in this.Swing(null, notes.First()))
            {
                yield return snapshot;
            }
        }


        public virtual IEnumerable<SaberSnapshot> Swing(ISimulationObject previousObject, ISimulationObject nextObject) 
        {
            //Debug.Log(nextObject.Time() + " " + parity.ToString());

			hasReset = false;
            //Debug.Log($"Prev: {previousObject?.GetType()}, Next: {nextObject?.GetType()}");
            //Debug.Log("");

            if (nextObject is BombGroup group)
            {
                //Debug.Log(group.ToString());
                foreach (var reset in HandleBombGroup(group)) yield return reset;
            }
            else if (nextObject is SliderGroup slider)
            {
                BaseNote firstNote = null, secondNote = null;
                //Debug.Log($"Slider at: {nextObject.Time()} with {slider.noteCount} notes");
                if (slider.isDotStack)
                {
                    slider = slider.OrderFullDotStack(previousObject, wristAngle, parity);
                }
                for (int i = 0; i < slider.noteCount; i++)
                {
                    bool isLast = i == slider.noteCount - 1;
                    if (isLast)
                    {
                        firstNote = slider.Notes().ElementAt(i - 1);
                        secondNote = slider.Notes().ElementAt(i);
                    }
                    else
                    {
                        firstNote = slider.Notes().ElementAt(i);
                        secondNote = slider.Notes().ElementAt(i + 1);
                    }
                    BaseNote nextNote = isLast ? secondNote : firstNote;
                    float angle = CutAngle(firstNote, secondNote, true, true);
                    if (angle == 180f && this is RightSaber)
                    {
                        angle = -180f;
                    }
                    MoveTowardsNote(angle, nextNote);
                    yield return TakeSnapshot(nextNote);
                }
                
                    this.parity = this.parity.Other();
                
            }
            else
            {
                RotateTowards(previousObject, nextObject);
                yield return TakeSnapshot(nextObject.FirstNote());
                
                    this.parity = this.parity.Other();
                
            }
            
        }

        // wristAngle could maybe be a boolean to keep the current angle
        public virtual SaberSnapshot Reset(BaseNote culprit, Parity parity, string reason, float wristAngle = 0f) 
        {
            //ParityAnalyser.outline.AddToCache(culprit, Color.yellow);
            Debug.Log($"Reset at beat {culprit.JsonTime}. Reason: {reason}");
            hasReset = true;
            this.parity = parity;
            this.transform.rotation = Quaternion.identity;
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 0f : 180f, Vector3.right);
            this.wristAngle = wristAngle;

            return TakeSnapshot(culprit);
        }

        protected void MoveTo(Vector2 pos) { this.transform.position = new Vector3(pos.x, pos.y, transform.position.z); }
        protected void MoveTo(Vector3 pos) { this.transform.position = pos; }

        protected virtual void RotateTowards(ISimulationObject previousObject, ISimulationObject nextObject)
        {
            BaseNote previousNote = previousObject is BombGroup group ? group.startNote : previousObject?.LastNote() ?? null;
            BaseNote nextNote = nextObject.FirstNote();
            float desiredAngle = CutAngle(previousNote, nextNote);
            MoveTowardsNote(desiredAngle, nextNote);

        }

        protected virtual void MoveTowardsNote(float angle, BaseNote nextNote)
        {

            // IMPORTANT: check Howl in the night sky ending (it causes resets)
            /*
             * should somehow increase the range of desired angles?
             * get this back up when working on old maps
             */
            float roll = angle - wristAngle;
            //if (roll < -315 || roll > 315)
            //{
            //    Reset(nextNote, this.parity.Other(), "Wristroll too large");
            //    return;
            //}
            this.wristAngle = angle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? -180f : 0f, Vector3.right);
            
            if ((CutDir)nextNote.CutDirection != CutDir.ANY)
            {
                MoveTo((Vector3)nextNote.Position() - cutOffset);
            }
            else
            {
                MoveTo(nextNote.Position());
            }
        }

        protected virtual float CutAngle(BaseNote prevNote, BaseNote nextNote, bool moveToDot = true, bool isSlider = false)
        {
            // IMPORTANT: check Kyuukou dot at 160 (causes reset)
            float desiredAngle = DesiredAngle((NoteDirection)nextNote.CutDirection);
            if (prevNote != null)
            {
                bool shouldKeepAngle = prevNote.IsInlineWith(nextNote) || nextNote.IsInvert(prevNote, wristAngle + parityAngle);
                if (shouldKeepAngle && ParityAnalyser.options.renderInlinesAndInverts)
                {
                    ParityAnalyser.outline.AddToCache(nextNote, Color.cyan);
                }
                if ((nextNote.CutDirection == (int)CutDir.ANY && !shouldKeepAngle) || isSlider)
                {
                    // TODO: maybe check inlines (example: abstruse dilemma) and inverts (example: Bad apple (Bitz) )
                    // also try different directions if the swing collides with bombs
                    if (moveToDot || isSlider)
                        MoveTo(prevNote.Position());
                    Vector2 dir = (nextNote.Position() - (Vector2)transform.position).normalized;
                    if (!parity.Bool()) dir = -dir;
                    float signedAngle = Vector2.SignedAngle(Vector2.down, dir);
                    desiredAngle = Mathf.DeltaAngle(0f, signedAngle);
                    //Debug.Log($"Beat: {nextNote.JsonTime}, Angle: {desiredAngle}, Signed: {signedAngle}");

                }
            }
            return desiredAngle;
        }

        protected virtual float WristRoll(BaseNote prevNote, BaseNote nextNote, bool moveToDot = true) => Mathf.DeltaAngle(wristAngle, CutAngle(prevNote, nextNote, moveToDot));

        private IEnumerable<SaberSnapshot> HandleBombGroup(BombGroup group)
        {

            if (group.endNote == null)
            {
                // Map ends with bombs
                yield break;
            }

            // First, try to resolve with exploration
            bool exploredSuccesfully = false;
            foreach (SaberSnapshot reset in ExploreBombGroup(group, group.startNote.JsonTime, group.endNote.JsonTime))
            {
                yield return reset;
                exploredSuccesfully = true;
            }
            if (exploredSuccesfully)
            {
                yield break;
            }
            // Otherwise try common resets

            // TODO: resets marked by bombs to the sides of the note

            float roll = WristRoll(group.startNote, group.endNote);

            List<BombCondition> bottomRowReset = [
                (note) => note.LeftOuterLane() && note.BottomRow(),
                (note) => note.LeftInnerLane() && note.BottomRow(),
                (note) => note.RightInnerLane() && note.BottomRow(),
                (note) => note.RightOuterLane() && note.BottomRow(),
                ];
            bool isBottomRowReset = group.Satisfy(bottomRowReset).Count() >= 3 /*group.Satisfies(bottomRowReset)*/;
            
            Debug.Log($"Beat: {group.Time()}, Wrist: {wristAngle}, roll: {roll}, comfortable: {RollsComfortably(roll)}");

            bool shouldResetDueToAngle = Mathf.Abs(wristAngle + roll) > 90f && (!RollsComfortably(roll) || Mathf.Abs(roll) >= 135f || Mathf.Abs(wristAngle + roll) >= 180f);
            if (shouldResetDueToAngle && /*don't reset for dots that cause very little roll*/ roll > 30f)
            {
                yield return Reset(group.bombs[0], parity.Other(), "Wrist roll caused too much wrist angle (bombs)");
                yield break;
            }
            bool shouldResetDueToRoll = Mathf.Abs(roll) >= 180f;

            // Dots are not recognised (example: chimera dragons beat 1030, deimos beat 688)
            if ((!group.Any(bomb => bomb.MiddleRow() || bomb.TopRow())) && parity == Parity.BACKHAND && Mathf.Abs(wristAngle) <= 90f && isBottomRowReset && (shouldResetDueToAngle || shouldResetDueToRoll))
            {
                Debug.Log($"Angle: {shouldResetDueToAngle} - wa: {wristAngle} - r: {roll} - c: {RollsComfortably(roll)}\n" +
                    $"Roll: {shouldResetDueToRoll} - r: {roll}");
                yield return Reset(group.bombs[0], Parity.FOREHAND, "Bottom row reset");
                yield break;
            }

            List<BombCondition> topRowReset = [
                (note) => note.LeftOuterLane() && note.TopRow(),
                (note) => note.LeftInnerLane() && note.TopRow(),
                (note) => note.RightInnerLane() && note.TopRow(),
                (note) => note.RightOuterLane() && note.TopRow(),
                ];
            //bool isTopRowReset = group.Satisfy(topRowReset).Count() >= 3;
            if ((!group.Any(bomb => bomb.MiddleRow() || bomb.BottomRow())) && parity == Parity.FOREHAND && Mathf.Abs(wristAngle) <= 90f && group.Satisfies(topRowReset) && (shouldResetDueToAngle || shouldResetDueToRoll))
            {
                yield return Reset(group.bombs[0], Parity.BACKHAND, "Top row reset");
                yield break;
            }

            // Quick bomb reset
            float quickBombThreshold = 1 / 4f;
            IEnumerable<BaseNote> inlineBombs = group.Where(bomb => bomb.PosX == group.startNote.PosX && bomb.PosY == group.startNote.PosY);
            if (inlineBombs.Count() > 0)
            {
                bool quickInlineBomb = inlineBombs.OrderByDescending(bomb => bomb.JsonTime).Last().JsonTime - group.startNote.JsonTime < quickBombThreshold;

                if (quickInlineBomb)
                {
                    yield return Reset(group.bombs[0], parity.Other(), "Inline bomb");
                    yield break;
                }
            }


        }

        protected virtual IEnumerable<SaberSnapshot> ExploreBombGroup(BombGroup group, float startTime, float endTime)
        {
            bool debug = (this is LeftSaber && ParityAnalyser.options.debugLeftBombCollisions) ||
                        (this is RightSaber && ParityAnalyser.options.debugRightBombCollisions);
            MoveTo(transform.position + transform.up);
            if (group.singleBeat)
            {
                // This foreach should only have 1 iteration
                foreach (var cluster in group.GetClusters())
                {
                    DodgeBombs(group, cluster);
                    foreach (var hitbox in cluster.GetHitbox())
                    {
                        float swingOffset = parityAngle, roll = WristRoll(group.startNote, group.endNote, false);
                        float startAngle = wristAngle + swingOffset;

                        bool intersects = Collision.SwingPathIntersects(hilt, startAngle, wristAngle + roll + swingOffset, hitbox, debug, cluster.aBomb);
                        if (intersects)
                        {
                            yield return Reset(cluster.aBomb, parity.Other(), "Bomb projection intersects");
                            break;
                        }

                    }
                }
            }
            else
            {
                bool freeze = (CutDir)group.endNote.CutDirection == CutDir.ANY;
                bool frozen = false;
                float freezeAngle = 0f;
                foreach ((BombCluster cluster1, BombCluster cluster2) in group.GetClusterPairs())
                {
                    Start:
                    float swingOffset = parityAngle, roll = WristRoll(group.startNote, group.endNote, false);
                    float startAngle = wristAngle + swingOffset;
                    // Maybe should make a function to combine single note and slider desired angle
                    // TODO: Hatatagami beat 1350
                    float endAngle =
                        (hasReset ? DesiredAngle((CutDir)group.endNote.CutDirection) + swingOffset :
                        wristAngle + roll + swingOffset);
                    if (freeze && !frozen)
                    {
                        freezeAngle = endAngle;
                        frozen = true;
                    }
                    else if (frozen)
                    {
                        endAngle = freezeAngle;
                    }
                    // Wrist angle exceeds natural rotation limits or why would you reset to then rotate your wrist past 90 degrees
                    float nextAngle = endAngle - swingOffset;
                    bool unnatural = (nextAngle > maxCCAngle || nextAngle < maxClockwiseAngle) || (hasReset && Math.Abs(nextAngle) > 90f);
                    //Debug.Log($"Freeze: {freeze}     {startAngle} - {endAngle}");
                    if (unnatural && nextAngle != wristAngle && !hasReset)
                    {
                        yield return Reset(cluster1.aBomb, parity.Other(), "Unnatural wrist roll (bomb exploration) " + $"{wristAngle} - {endAngle - swingOffset}");
                        freeze = false;
                        goto Start;
                    }
                    float lerpFactor = (endTime - startTime);

                    float bomb1LerpAmount = (cluster1.time - startTime) / lerpFactor;
                    float bomb2LerpAmount = (cluster2.time - startTime) / lerpFactor;
                    foreach (Rect hitbox in cluster1.GetHitbox())
                    {

                        bool swingPathCollides = Collision.SwingPathIntersects(hilt, Mathf.Lerp(startAngle, endAngle, bomb1LerpAmount), Mathf.Lerp(startAngle, endAngle, bomb2LerpAmount), hitbox, debug, cluster1.aBomb);

                        if (swingPathCollides)
                        {
                            // Please don't let the answer be recursive calls
                            startTime = cluster1.time;
                            yield return Reset(cluster1.aBomb, parity.Other(), "Swing path", wristAngle);
                            break;
                        }
                    }
                    // TODO: consider order of dodge and check collision (dodging first is more realistic in the sense that it causes less collision resets but it breaks the diagonal bottom row resets)
                    DodgeBombs(group, cluster1, debug);
                }
            }
            yield break;
        }

        private void DodgeBombs(BombGroup group, BombCluster currentCluster, bool debug = false)
        {
            // Maybe should move towards next note
            Vector2 hiltPos = new Vector2(hilt.x, hilt.y);

            Vector2 totalForce = Vector2.zero;

            int count = 0;
            foreach (BaseNote bomb in group.After(currentCluster.time))
            {
                count++;
                float effectRadius = 1f;
                Vector2 bombPos = new Vector2(bomb.PosX, bomb.PosY);
                float timeDistance = bomb.JsonTime - currentCluster.time;
                float projectionDistance = Vector2.Distance(bombPos, hiltPos);

                Vector2 dir = (hiltPos - bombPos).normalized;

                /* Pink diamond: beat 130 upcoming bombs push the saber into current bombs (maybe take current bombs into account but favour distance over time?)
                 * beat 215 check left saber, also right saber doesn't seem to move
                 * beat 223 forces saber angle (maybe fix with exploration?)
                 */
                float timeScaleFactor = 1f, distanceScaleFactor = 1f;
                float timeMagnitude = Mathf.Exp(-1f * Mathf.Pow((timeDistance - 0.3f) / 0.2f, 2));
                float distanceMagnitude = (1 / (projectionDistance + effectRadius));
                float magnitude = timeScaleFactor * timeMagnitude + distanceScaleFactor * distanceMagnitude;

                totalForce += dir * magnitude;

                //float centerForce = 2f;
                //if (bomb.BottomRow())
                //{
                //    totalForce += Vector2.up * centerForce;
                //}
                //else if (bomb.TopRow())
                //{
                //    totalForce += Vector2.down * centerForce;
                //}
            }
            totalForce /= count;
            totalForce += RestForce();
            MoveTo(transform.position + (Vector3)totalForce);
            if (debug)
                Utils.RenderLine((Vector3)hiltPos, transform.position, Color.yellow, Color.black, sync: currentCluster.aBomb);
            //Debug.Log(transform.position);
        }

        public virtual IEnumerable<ISimulationObject> ExtractBombGroups(List<ISimulationObject> notes)
        {
            // TODO: maybe make a bomb group if map starts with bombs
            bool firstNoteHappened = false;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] is SliderGroup || (notes[i] is Note n && !n.Value.IsBomb())) firstNoteHappened = true;
                if (firstNoteHappened && notes[i] is Note note && note.Value.IsBomb())
                {
                    ISimulationObject start = notes[i - 1];
                    List<Note> bombs = [];
                    while (notes[i] is Note nextNote && nextNote.Value.IsBomb())
                    {
                        bombs.Add(nextNote);
                        if (i == notes.Count - 1)
                        {
                            yield return new BombGroup(start, bombs, null);
                            yield break;
                        }
                        i++;
                    }
                    yield return new BombGroup(start, bombs, notes[i]);
                }
            }

        }

        public virtual IEnumerable<ISimulationObject> ExtractSliderGroups(List<BaseNote> allNotes)
        {
            allNotes = allNotes.Where(note => !note.IsBomb()).ToList();
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
                if (renderStart) Utils.RenderSphere(new Vector3(group.startNote.PosX, 3, 0f), 0.5f, groupColor, group.startNote);
                foreach (BaseNote bomb in group.bombs)
                {
                    ParityAnalyser.outline.AddToCache(bomb, groupColor);
                }
                if (renderEnd && group.endNote != null) Utils.RenderSphere(new Vector3(group.endNote.PosX, 4, 0f), 0.3f, groupColor, group.endNote);
            }
        }

        protected abstract Vector2 restPoint { get; }
        protected Vector2 RestForce()
        {
            Vector2 dir = (restPoint - (Vector2)transform.position).normalized;
            float radius = 1f;
            float scaleFactor = 1f, power = 1f;
            float magnitude = Mathf.Min(scaleFactor * Mathf.Max(0, Mathf.Pow(Vector2.Distance(restPoint, transform.position) - radius, power)), 4f);
            return dir * magnitude;
        }

        public List<SliderGroup> sliderGroups { get; private set; } = [];

        private Vector3 cutOffset => transform.up * CutDistance;
        public static readonly float CutDistance = 0.5f;

        protected abstract float maxClockwiseAngle { get; }
        protected abstract float maxCCAngle { get; }
        protected abstract float preferredRollDirection { get; }

        public bool RollsComfortably(float roll) => (wristAngle != 0f && preferredRollDirection == Mathf.Sign(roll))
                                                || (wristAngle == 0f && roll == 0f);

        public float wristAngle { get; protected set; }

        public float parityAngle => (parity.Bool() ? 180f : 0f);

        public static readonly float length = 2.1f;
        public Vector3 hilt => this.transform.position;
        public Vector3 tip => this.transform.position + (length * transform.up);

        public IEnumerator<ISimulationObject> GetEnumerator() => this.notes.GetEnumerator();
        public IEnumerable<(ISimulationObject, ISimulationObject)> GetPairs() => new OverlappingPairIterator<ISimulationObject>(this.notes, false);

        public static implicit operator List<ISimulationObject>(Saber saber) => saber.notes;

        protected abstract float DesiredAngle(CutDir dir);
    }
}
